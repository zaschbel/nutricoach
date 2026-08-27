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

    // Vitamine je 100g - v.a. bei OpenFoodFacts-Produkten mit vollem Naehrwert-Panel verfuegbar,
    // bei manuell angelegten oder unvollstaendigen Quellen bleibt es bei 0 (= unbekannt/keine Angabe).
    public double VitaminAPer100 { get; set; }      // µg
    public double VitaminB1Per100 { get; set; }     // mg
    public double VitaminB2Per100 { get; set; }     // mg
    public double VitaminB3Per100 { get; set; }     // mg
    public double VitaminB5Per100 { get; set; }     // mg
    public double VitaminB6Per100 { get; set; }     // mg
    public double VitaminB7Per100 { get; set; }     // µg
    public double VitaminB9Per100 { get; set; }     // µg
    public double VitaminB12Per100 { get; set; }    // µg
    public double VitaminCPer100 { get; set; }      // mg
    public double VitaminDPer100 { get; set; }      // µg
    public double VitaminEPer100 { get; set; }      // mg
    public double VitaminKPer100 { get; set; }      // µg

    // Mineralstoffe je 100g
    public double CalciumPer100 { get; set; }       // mg
    public double MagnesiumPer100 { get; set; }     // mg
    public double PotassiumPer100 { get; set; }     // mg
    public double SodiumPer100 { get; set; }        // mg
    public double PhosphorusPer100 { get; set; }    // mg
    public double ChloridePer100 { get; set; }      // mg
    public double SulfurPer100 { get; set; }        // mg
    public double IronPer100 { get; set; }          // mg
    public double ZincPer100 { get; set; }          // mg
    public double SeleniumPer100 { get; set; }      // µg
    public double CopperPer100 { get; set; }        // mg
    public double ManganesePer100 { get; set; }     // mg
    public double IodinePer100 { get; set; }        // µg
    public double FluoridePer100 { get; set; }      // mg
    public double ChromiumPer100 { get; set; }      // µg
    public double MolybdenumPer100 { get; set; }    // µg
    public double CobaltPer100 { get; set; }        // µg
    public double SiliconPer100 { get; set; }       // mg

    // Weitere je 100g
    public double SugarAlcoholsPer100 { get; set; } // g
    public double AlcoholPer100 { get; set; }       // g
    public double Omega3Per100 { get; set; }        // mg
    public double Omega6Per100 { get; set; }        // g
    public double Omega9Per100 { get; set; }        // g

    public bool IsLiquid { get; set; }                  // steuert Anzeige g vs. ml
    public string Source { get; set; } = "Manuell";      // "Manuell" | "OpenFoodFacts" | "KI-Scan"

    /// <summary>Foto des Gerichts, falls per KI-Foto-Erkennung angelegt (lokal gespeicherter Pfad).</summary>
    public string? PhotoPath { get; set; }
    public bool HasPhoto => !string.IsNullOrWhiteSpace(PhotoPath);

    /// <summary>Passendes Emoji-Icon, automatisch aus dem Namen abgeleitet.</summary>
    public string Icon => FoodIconHelper.GetIcon(Name);
}
