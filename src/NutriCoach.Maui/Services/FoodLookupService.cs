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
///
/// Hinweis (2026-08-29): Ein Versuch, hier auf USDA FoodData Central umzusteigen (bessere
/// Vitamin-/Mineralstoff-Abdeckung), wurde rueckgaengig gemacht - USDA ist eine rein
/// englischsprachige US-Datenbank und findet bei deutschen Suchbegriffen praktisch nichts,
/// was die Suche fuer diesen Anwendungsfall unbrauchbar gemacht haette.
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
            // Vitamine/Mineralstoffe: Open Food Facts liefert sie in g (nicht mg/µg wie auf dem
            // Etikett üblich) - deshalb hier auf die in der App verwendeten Einheiten umgerechnet.
            VitaminAPer100 = (p.Nutriments?.VitaminA100g ?? 0) * 1_000_000,
            VitaminB1Per100 = (p.Nutriments?.VitaminB1100g ?? 0) * 1_000,
            VitaminB2Per100 = (p.Nutriments?.VitaminB2100g ?? 0) * 1_000,
            VitaminB3Per100 = (p.Nutriments?.VitaminPP100g ?? 0) * 1_000,
            VitaminB5Per100 = (p.Nutriments?.PantothenicAcid100g ?? 0) * 1_000,
            VitaminB6Per100 = (p.Nutriments?.VitaminB6100g ?? 0) * 1_000,
            VitaminB7Per100 = (p.Nutriments?.Biotin100g ?? 0) * 1_000_000,
            VitaminB9Per100 = (p.Nutriments?.VitaminB9100g ?? 0) * 1_000_000,
            VitaminB12Per100 = (p.Nutriments?.VitaminB12100g ?? 0) * 1_000_000,
            VitaminCPer100 = (p.Nutriments?.VitaminC100g ?? 0) * 1_000,
            VitaminDPer100 = (p.Nutriments?.VitaminD100g ?? 0) * 1_000_000,
            VitaminEPer100 = (p.Nutriments?.VitaminE100g ?? 0) * 1_000,
            VitaminKPer100 = (p.Nutriments?.VitaminK100g ?? 0) * 1_000_000,
            CalciumPer100 = (p.Nutriments?.Calcium100g ?? 0) * 1_000,
            MagnesiumPer100 = (p.Nutriments?.Magnesium100g ?? 0) * 1_000,
            PotassiumPer100 = (p.Nutriments?.Potassium100g ?? 0) * 1_000,
            SodiumPer100 = (p.Nutriments?.Sodium100g ?? 0) * 1_000,
            PhosphorusPer100 = (p.Nutriments?.Phosphorus100g ?? 0) * 1_000,
            IronPer100 = (p.Nutriments?.Iron100g ?? 0) * 1_000,
            ZincPer100 = (p.Nutriments?.Zinc100g ?? 0) * 1_000,
            SeleniumPer100 = (p.Nutriments?.Selenium100g ?? 0) * 1_000_000,
            CopperPer100 = (p.Nutriments?.Copper100g ?? 0) * 1_000,
            ManganesePer100 = (p.Nutriments?.Manganese100g ?? 0) * 1_000,
            IodinePer100 = (p.Nutriments?.Iodine100g ?? 0) * 1_000_000,
            AlcoholPer100 = p.Nutriments?.Alcohol100g ?? 0,
            Omega3Per100 = (p.Nutriments?.Omega3Fat100g ?? 0) * 1_000,
            Omega6Per100 = p.Nutriments?.Omega6Fat100g ?? 0,
            Omega9Per100 = p.Nutriments?.Omega9Fat100g ?? 0,
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

        [JsonPropertyName("vitamin-a_100g")]
        public double? VitaminA100g { get; set; }
        [JsonPropertyName("vitamin-b1_100g")]
        public double? VitaminB1100g { get; set; }
        [JsonPropertyName("vitamin-b2_100g")]
        public double? VitaminB2100g { get; set; }
        [JsonPropertyName("vitamin-pp_100g")]
        public double? VitaminPP100g { get; set; }
        [JsonPropertyName("pantothenic-acid_100g")]
        public double? PantothenicAcid100g { get; set; }
        [JsonPropertyName("vitamin-b6_100g")]
        public double? VitaminB6100g { get; set; }
        [JsonPropertyName("biotin_100g")]
        public double? Biotin100g { get; set; }
        [JsonPropertyName("vitamin-b9_100g")]
        public double? VitaminB9100g { get; set; }
        [JsonPropertyName("vitamin-b12_100g")]
        public double? VitaminB12100g { get; set; }
        [JsonPropertyName("vitamin-c_100g")]
        public double? VitaminC100g { get; set; }
        [JsonPropertyName("vitamin-d_100g")]
        public double? VitaminD100g { get; set; }
        [JsonPropertyName("vitamin-e_100g")]
        public double? VitaminE100g { get; set; }
        [JsonPropertyName("vitamin-k_100g")]
        public double? VitaminK100g { get; set; }
        [JsonPropertyName("calcium_100g")]
        public double? Calcium100g { get; set; }
        [JsonPropertyName("magnesium_100g")]
        public double? Magnesium100g { get; set; }
        [JsonPropertyName("potassium_100g")]
        public double? Potassium100g { get; set; }
        [JsonPropertyName("sodium_100g")]
        public double? Sodium100g { get; set; }
        [JsonPropertyName("phosphorus_100g")]
        public double? Phosphorus100g { get; set; }
        [JsonPropertyName("iron_100g")]
        public double? Iron100g { get; set; }
        [JsonPropertyName("zinc_100g")]
        public double? Zinc100g { get; set; }
        [JsonPropertyName("selenium_100g")]
        public double? Selenium100g { get; set; }
        [JsonPropertyName("copper_100g")]
        public double? Copper100g { get; set; }
        [JsonPropertyName("manganese_100g")]
        public double? Manganese100g { get; set; }
        [JsonPropertyName("iodine_100g")]
        public double? Iodine100g { get; set; }
        [JsonPropertyName("alcohol_100g")]
        public double? Alcohol100g { get; set; }
        [JsonPropertyName("omega-3-fat_100g")]
        public double? Omega3Fat100g { get; set; }
        [JsonPropertyName("omega-6-fat_100g")]
        public double? Omega6Fat100g { get; set; }
        [JsonPropertyName("omega-9-fat_100g")]
        public double? Omega9Fat100g { get; set; }
    }
}
