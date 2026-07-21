using System.Net.Http;
using System.Net.Http.Json;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NutriCoach.App.Data;
using NutriCoach.App.Models;

namespace NutriCoach.App.Services;

/// <summary>
/// Findet Lebensmittel auf drei Wegen: zuerst im lokalen Cache (schnell, offline),
/// dann bei Bedarf online über Open Food Facts (per Name-Suche oder Barcode).
/// Online-Treffer werden automatisch lokal zwischengespeichert, damit sie beim
/// nächsten Mal sofort verfügbar sind, ohne erneut das Internet zu brauchen.
/// </summary>
/// <summary>Ergebnis einer Online-Suche inkl. Erfolgsstatus, damit "nichts gefunden" von "Verbindung fehlgeschlagen" unterscheidbar ist.</summary>
public record OnlineSearchResult(List<FoodItem> Items, bool Success, string? ErrorMessage);

public class FoodLookupService
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://world.openfoodfacts.org"),
        Timeout = TimeSpan.FromSeconds(8)
    };

    static FoodLookupService()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("NutriCoach/1.0 (Desktop-App)");
    }

    /// <summary>Sucht zuerst lokal (per Namens-Teilstring), das reicht für die meisten Eingaben.</summary>
    public async Task<List<FoodItem>> SearchLocalAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<FoodItem>();

        await using var context = new AppDbContext();
        var q = query.Trim().ToLower();
        var results = await context.FoodItems
            .Where(f => f.Name.ToLower().Contains(q))
            .OrderBy(f => f.Name)
            .Take(25)
            .ToListAsync();

        // Tippfehler-Korrektur: kein Teilstring-Treffer? Dann den ähnlichsten bekannten Namen per
        // Levenshtein-Distanz vorschlagen, statt eine leere Liste zurückzugeben.
        if (results.Count == 0)
        {
            try
            {
                var allNames = await context.FoodItems.Select(f => f.Name).ToListAsync();
                var closest = FuzzyMatch.FindClosest(query, allNames);
                if (closest is not null)
                {
                    var match = await context.FoodItems.FirstOrDefaultAsync(f => f.Name == closest);
                    if (match is not null) results.Add(match);
                }
            }
            catch
            {
                // Fehler bei der Tippfehler-Korrektur darf die normale Suche nicht beeinträchtigen.
            }
        }

        return results;
    }

    /// <summary>
    /// Sucht online bei Open Food Facts nach Namen. Ergebnisse werden NICHT automatisch
    /// gespeichert - erst wenn der Nutzer eins davon tatsächlich auswählt (siehe SaveToCacheAsync).
    /// </summary>
    public async Task<OnlineSearchResult> SearchOnlineAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new OnlineSearchResult(new List<FoodItem>(), true, null);

        try
        {
            // sort_by=unique_scans_n: rankt nach echten Nutzer-Scans/Bestätigungen statt beliebiger
            // Teilstring-Reihenfolge - starkes Signal für Datenqualität/Relevanz, bekannte/korrekte
            // Einträge kommen so zuerst. cc=de&lc=de: Länder-/Sprach-Hinweis für bessere Relevanz bei
            // deutschsprachigen Suchbegriffen, ohne andere Treffer komplett auszuschließen.
            var url = $"/cgi/search.pl?search_terms={Uri.EscapeDataString(query)}" +
                      "&search_simple=1&action=process&json=1&page_size=20" +
                      "&sort_by=unique_scans_n&cc=de&lc=de" +
                      "&fields=product_name,brands,code,nutriments";

            var response = await Http.GetFromJsonAsync<OffSearchResponse>(url);
            if (response?.Products is null) return new OnlineSearchResult(new List<FoodItem>(), true, null);

            var items = response.Products
                .Where(p => !string.IsNullOrWhiteSpace(p.ProductName))
                .Select(MapToFoodItem)
                // KcalPer100 <= 0 ist ein starkes Signal für einen unvollständigen/kaputten OFF-Eintrag
                // ohne echte Nährwerte - solche Einträge sind fast immer Rauschen und verschlechtern
                // die Trefferqualität ("schlechte Trefferqualität" war die konkrete Beschwerde).
                .Where(f => f is not null && f.KcalPer100 > 0)
                .Cast<FoodItem>()
                .ToList();

            return new OnlineSearchResult(items, true, null);
        }
        catch (Exception ex)
        {
            // Kein Internet, Timeout, o.ä. - App bleibt trotzdem benutzbar (lokale Suche +
            // manuelle Eingabe funktionieren immer), aber wir geben die Ursache mit zurück,
            // damit sie dem Nutzer angezeigt werden kann statt einfach "nichts gefunden".
            return new OnlineSearchResult(new List<FoodItem>(), false, ex.Message);
        }
    }

    /// <summary>Sucht ein einzelnes Produkt anhand seiner Barcode-Nummer (z. B. beim Scannen der Verpackung).</summary>
    public async Task<FoodItem?> LookupByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;

        // Erst lokal schauen, ob wir das schon kennen
        await using var context = new AppDbContext();
        var cached = await context.FoodItems.FirstOrDefaultAsync(f => f.Barcode == barcode);
        if (cached is not null) return cached;

        try
        {
            var response = await Http.GetFromJsonAsync<OffProductResponse>($"/api/v2/product/{barcode}.json");
            if (response?.Status != 1 || response.Product is null) return null;

            var item = MapToFoodItem(response.Product);
            return item;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Lädt ein Lebensmittel anhand seiner lokalen Id - z. B. um einen bestehenden Tagebuch-Eintrag zu bearbeiten.</summary>
    public async Task<FoodItem?> GetByIdAsync(int id)
    {
        await using var context = new AppDbContext();
        return await context.FoodItems.FindAsync(id);
    }

    /// <summary>Speichert ein (z. B. online gefundenes) Lebensmittel dauerhaft lokal, damit es beim nächsten Mal sofort da ist.</summary>
    public async Task<FoodItem> SaveToCacheAsync(FoodItem item)
    {
        await using var context = new AppDbContext();

        // Falls schon per Barcode oder OpenFoodFacts-Id vorhanden: das existierende nehmen statt Duplikat anlegen
        FoodItem? existing = null;
        if (!string.IsNullOrWhiteSpace(item.Barcode))
            existing = await context.FoodItems.FirstOrDefaultAsync(f => f.Barcode == item.Barcode);
        if (existing is null && !string.IsNullOrWhiteSpace(item.OpenFoodFactsId))
            existing = await context.FoodItems.FirstOrDefaultAsync(f => f.OpenFoodFactsId == item.OpenFoodFactsId);

        if (existing is not null) return existing;

        context.FoodItems.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    /// <summary>Legt ein rein manuell eingegebenes Lebensmittel an (eigene Makros, keine Online-Quelle).</summary>
    public async Task<FoodItem> CreateManualAsync(FoodItem item)
    {
        item.Source = "Manuell";
        await using var context = new AppDbContext();
        context.FoodItems.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    private static FoodItem? MapToFoodItem(OffProduct p)
    {
        if (string.IsNullOrWhiteSpace(p.ProductName)) return null;

        return new FoodItem
        {
            Name = p.ProductName,
            Brand = p.Brands,
            Barcode = p.Code,
            OpenFoodFactsId = p.Code,
            KcalPer100 = p.Nutriments?.EnergyKcal100g ?? 0,
            CarbsPer100 = p.Nutriments?.Carbohydrates100g ?? 0,
            SugarPer100 = p.Nutriments?.Sugars100g ?? 0,
            ProteinPer100 = p.Nutriments?.Proteins100g ?? 0,
            FatPer100 = p.Nutriments?.Fat100g ?? 0,
            SaturatedFatPer100 = p.Nutriments?.SaturatedFat100g ?? 0,
            FiberPer100 = p.Nutriments?.Fiber100g ?? 0,
            SaltPer100 = p.Nutriments?.Salt100g ?? 0,
            Source = "OpenFoodFacts"
        };
    }

    /// <summary>
    /// Erzeugt einen kurzen, automatischen Tipp basierend auf den Nährwerten -
    /// die einfache erste Stufe des "KI-Coach"-Gedankens (echte KI-Bewertung folgt später).
    /// </summary>
    public static string? GenerateTip(FoodItem food)
    {
        var tips = new List<string>();

        if (food.Name.ToLower().Contains("vollkorn"))
            tips.Add("🌾 Vollkorn liefert meist mehr Ballaststoffe und hält länger satt als Weißmehl-Varianten.");

        if (food.ProteinPer100 >= 15)
            tips.Add("💪 Gute Eiweißquelle.");

        if (food.SugarPer100 >= 15)
            tips.Add("⚠️ Relativ zuckerreich - in Maßen genießen.");

        if (food.FiberPer100 >= 5)
            tips.Add("🌿 Reich an Ballaststoffen, gut für die Verdauung.");

        if (food.SaltPer100 >= 1.2)
            tips.Add("🧂 Recht salzhaltig.");

        if (food.KcalPer100 > 0 && food.KcalPer100 <= 50)
            tips.Add("✅ Kalorienarm.");

        return tips.Count == 0 ? null : string.Join("  ", tips.Take(2));
    }

    // ---------------- Open Food Facts API-Antwortformate (nur die Felder, die wir brauchen) ----------------

    private class OffSearchResponse
    {
        [JsonPropertyName("products")]
        public List<OffProduct>? Products { get; set; }
    }

    private class OffProductResponse
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }
        [JsonPropertyName("product")]
        public OffProduct? Product { get; set; }
    }

    private class OffProduct
    {
        [JsonPropertyName("product_name")]
        public string? ProductName { get; set; }
        [JsonPropertyName("brands")]
        public string? Brands { get; set; }
        [JsonPropertyName("code")]
        public string? Code { get; set; }
        [JsonPropertyName("nutriments")]
        public OffNutriments? Nutriments { get; set; }
    }

    private class OffNutriments
    {
        [JsonPropertyName("energy-kcal_100g")]
        public double? EnergyKcal100g { get; set; }
        [JsonPropertyName("carbohydrates_100g")]
        public double? Carbohydrates100g { get; set; }
        [JsonPropertyName("sugars_100g")]
        public double? Sugars100g { get; set; }
        [JsonPropertyName("proteins_100g")]
        public double? Proteins100g { get; set; }
        [JsonPropertyName("fat_100g")]
        public double? Fat100g { get; set; }
        [JsonPropertyName("saturated-fat_100g")]
        public double? SaturatedFat100g { get; set; }
        [JsonPropertyName("fiber_100g")]
        public double? Fiber100g { get; set; }
        [JsonPropertyName("salt_100g")]
        public double? Salt100g { get; set; }
    }
}
