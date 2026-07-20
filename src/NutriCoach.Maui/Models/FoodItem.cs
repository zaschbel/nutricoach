using NutriCoach.App.Services;

namespace NutriCoach.App.Models;

/// <summary>
/// Ein Lebensmittel mit Nährwerten pro 100g/100ml. Wird entweder manuell angelegt,
/// per Barcode-Scan (Open Food Facts) geladen oder aus der KI-Fotoanalyse befüllt,
/// und danach lokal gecacht damit nicht jedes Mal neu abgefragt werden muss.
/// </summary>
public class FoodItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Barcode { get; set; }               // EAN/UPC vom Scan
    public string? OpenFoodFactsId { get; set; }        // Referenz auf externe Quelle

    // Nährwerte je 100g bzw. 100ml (Standard bei Lebensmitteldatenbanken)
    public double KcalPer100 { get; set; }
    public double CarbsPer100 { get; set; }
    public double SugarPer100 { get; set; }
    public double ProteinPer100 { get; set; }
    public double FatPer100 { get; set; }
    public double SaturatedFatPer100 { get; set; }
    public double FiberPer100 { get; set; }
    public double SaltPer100 { get; set; }

    public bool IsLiquid { get; set; }                  // steuert Anzeige g vs. ml
    public string Source { get; set; } = "Manuell";      // "Manuell" | "OpenFoodFacts" | "KI-Scan"

    /// <summary>Foto des Gerichts, falls per KI-Foto-Erkennung angelegt (lokal gespeicherter Pfad).</summary>
    public string? PhotoPath { get; set; }
    public bool HasPhoto => !string.IsNullOrWhiteSpace(PhotoPath);

    /// <summary>Passendes Emoji-Icon, automatisch aus dem Namen abgeleitet.</summary>
    public string Icon => FoodIconHelper.GetIcon(Name);
}
