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
/// dann bei Bedarf online über USDA FoodData Central (per Name-Suche oder Barcode).
/// Online-Treffer werden automatisch lokal zwischengespeichert, damit sie beim
/// nächsten Mal sofort verfügbar sind, ohne erneut das Internet zu brauchen.
/// </summary>
/// <summary>Ergebnis einer Online-Suche inkl. Erfolgsstatus, damit "nichts gefunden" von "Verbindung fehlgeschlagen" unterscheidbar ist.</summary>
public record OnlineSearchResult(List<FoodItem> Items, bool Success, string? ErrorMessage);

public class FoodLookupService
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://api.nal.usda.gov"),
        Timeout = TimeSpan.FromSeconds(8)
    };

    // USDA FoodData Central statt Open Food Facts (2026-08-28, Nutzerwunsch: bisherige Quelle war
    // zu unvollstaendig/inkonsistent). DEMO_KEY ist USDA's oeffentlicher Schluessel - funktioniert
    // sofort ohne Anmeldung, ist aber stark ratenbegrenzt (ca. 30 Anfragen/Stunde). Fuer echten
    // Dauerbetrieb sollte der Nutzer sich einen eigenen kostenlosen Key holen (nur E-Mail noetig,
    // kein Passwort/Account): https://fdc.nal.usda.gov/api-key-signup - und ihn hier eintragen.
    private const string ApiKey = "DEMO_KEY";

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
    /// Sucht online bei USDA FoodData Central nach Namen. Ergebnisse werden NICHT automatisch
    /// gespeichert - erst wenn der Nutzer eins davon tatsächlich auswählt (siehe SaveToCacheAsync).
    /// </summary>
    public async Task<OnlineSearchResult> SearchOnlineAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new OnlineSearchResult(new List<FoodItem>(), true, null);

        try
        {
            var url = $"/fdc/v1/foods/search?api_key={ApiKey}&query={Uri.EscapeDataString(query)}" +
                      "&pageSize=20&dataType=Foundation,SR%20Legacy,Branded";

            var response = await Http.GetFromJsonAsync<FdcSearchResponse>(url);
            if (response?.Foods is null) return new OnlineSearchResult(new List<FoodItem>(), true, null);

            var items = response.Foods
                .Where(f => !string.IsNullOrWhiteSpace(f.Description))
                .Select(MapToFoodItem)
                // KcalPer100 <= 0 ist ein starkes Signal fuer einen unvollstaendigen Eintrag ohne
                // echte Naehrwerte - solche Eintraege sind fast immer Rauschen.
                .Where(f => f.KcalPer100 > 0)
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

    /// <summary>Sucht ein einzelnes Produkt anhand seiner Barcode-Nummer (z. B. beim Scannen der Verpackung).
    /// USDA FoodData Central hat keinen eigenen Barcode-Endpunkt - die GTIN/UPC-Nummer wird stattdessen
    /// als Suchbegriff verwendet und auf einen exakten "gtinUpc"-Treffer geprüft.</summary>
    public async Task<FoodItem?> LookupByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;

        // Erst lokal schauen, ob wir das schon kennen
        await using var context = new AppDbContext();
        var cached = await context.FoodItems.FirstOrDefaultAsync(f => f.Barcode == barcode);
        if (cached is not null) return cached;

        try
        {
            var url = $"/fdc/v1/foods/search?api_key={ApiKey}&query={Uri.EscapeDataString(barcode)}" +
                      "&pageSize=10&dataType=Branded";
            var response = await Http.GetFromJsonAsync<FdcSearchResponse>(url);
            var match = response?.Foods?.FirstOrDefault(f => f.GtinUpc == barcode)
                        ?? response?.Foods?.FirstOrDefault();
            return match is null || string.IsNullOrWhiteSpace(match.Description) ? null : MapToFoodItem(match);
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

        // Falls schon per Barcode oder externer Id vorhanden: das existierende nehmen statt Duplikat anlegen
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

    private static FoodItem MapToFoodItem(FdcFood f)
    {
        double N(int nutrientId) => f.FoodNutrients?.FirstOrDefault(n => n.NutrientId == nutrientId)?.Value ?? 0;

        // USDA fuehrt kein eigenes "Salz" - Standardumrechnung aus Natrium (Kochsalz = Natrium * 2.5).
        var sodiumMg = N(1093);

        return new FoodItem
        {
            Name = f.Description ?? "Unbekannt",
            Brand = f.BrandOwner,
            Barcode = f.GtinUpc,
            // Feld historisch "OpenFoodFactsId" genannt, dient hier generisch als externe Quellen-Id
            // (USDA fdcId) - kein Schema-Wechsel noetig, nur Bedeutung erweitert.
            OpenFoodFactsId = f.FdcId.ToString(),
            KcalPer100 = N(1008),
            CarbsPer100 = N(1005),
            SugarPer100 = N(2000),
            ProteinPer100 = N(1003),
            FatPer100 = N(1004),
            SaturatedFatPer100 = N(1258),
            FiberPer100 = N(1079),
            SaltPer100 = Math.Round(sodiumMg * 2.5 / 1000.0, 3),
            VitaminAPer100 = N(1106),
            VitaminB1Per100 = N(1165),
            VitaminB2Per100 = N(1166),
            VitaminB3Per100 = N(1167),
            VitaminB5Per100 = N(1170),
            VitaminB6Per100 = N(1175),
            VitaminB7Per100 = N(1176),
            VitaminB9Per100 = N(1177),
            VitaminB12Per100 = N(1178),
            VitaminCPer100 = N(1162),
            VitaminDPer100 = N(1114),
            VitaminEPer100 = N(1109),
            VitaminKPer100 = N(1185),
            CalciumPer100 = N(1087),
            MagnesiumPer100 = N(1090),
            PotassiumPer100 = N(1092),
            SodiumPer100 = sodiumMg,
            PhosphorusPer100 = N(1091),
            IronPer100 = N(1089),
            ZincPer100 = N(1095),
            SeleniumPer100 = N(1103),
            CopperPer100 = N(1098),
            ManganesePer100 = N(1101),
            MolybdenumPer100 = N(1102),
            // Naeherung: USDA liefert nur "insgesamt einfach/mehrfach ungesaettigt", keine separaten
            // Omega-6/-9-spezifischen IDs im Suchindex - Omega-3 bleibt daher bei 0 statt geraten.
            Omega6Per100 = N(1293),
            Omega9Per100 = N(1292),
            Source = "USDA FoodData Central"
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

    // ---------------- USDA FoodData Central API-Antwortformate (nur die Felder, die wir brauchen) ----------------

    private class FdcSearchResponse
    {
        [JsonPropertyName("foods")]
        public List<FdcFood>? Foods { get; set; }
    }

    private class FdcFood
    {
        [JsonPropertyName("fdcId")]
        public int FdcId { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        [JsonPropertyName("brandOwner")]
        public string? BrandOwner { get; set; }
        [JsonPropertyName("gtinUpc")]
        public string? GtinUpc { get; set; }
        [JsonPropertyName("foodNutrients")]
        public List<FdcNutrient>? FoodNutrients { get; set; }
    }

    private class FdcNutrient
    {
        [JsonPropertyName("nutrientId")]
        public int NutrientId { get; set; }
        [JsonPropertyName("value")]
        public double Value { get; set; }
    }
}
