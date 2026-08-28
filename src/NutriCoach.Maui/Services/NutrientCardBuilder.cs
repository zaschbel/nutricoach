namespace NutriCoach.App.Services;

/// <summary>Eine einzelne Nährwert-Karte (Vitamin/Mineralstoff/etc.) mit Tageswert, Referenzwert
/// und Einheit - fertig aufbereitet für die Kalender-artigen Karten auf der Ernährungs-Seite,
/// analog zur MCI-App-Vorlage.</summary>
public class NutrientCardDisplay
{
    public string Icon { get; set; } = "";
    public string Name { get; set; } = "";
    public double Current { get; set; }
    public double Goal { get; set; }
    public string Unit { get; set; } = "";
    public double ProgressRatio => Goal > 0 ? Math.Clamp(Current / Goal, 0, 1) : 0;

    /// <summary>Property-Name auf <see cref="NutritionEntryDisplay"/>, der diesem Nährstoff entspricht
    /// (z. B. "Fiber" für Ballaststoffe) - wird per Reflection genutzt, um die "Heutige Quellen"-Liste
    /// in der Detailansicht zu berechnen, ohne für jeden der 40 Nährstoffe eigenen Code zu brauchen.</summary>
    public string Key { get; set; } = "";
}

/// <summary>
/// Baut die Nährwert-Kartenlisten für die Ernährungs-Seite aus den Tagesgesamtwerten.
/// Referenzwerte sind, wo aus der MCI-Vorlage ablesbar, exakt übernommen; nicht ablesbare
/// Werte (durch Bildschirmrand abgeschnitten) sind mit den offiziellen EU-Referenzmengen
/// (NRV, Richtlinie 1169/2011 Anhang XIII) aufgefüllt.
/// </summary>
public static class NutrientCardBuilder
{
    private const string VitaminIcon = "";  // capsules
    private const string MineralIcon = "";  // vial
    private const string OtherIcon = "";    // sparkles (schon in der App für KI genutzt)
    private const string MacroIcon = "";    // utensils (schon in der App genutzt)

    public static List<NutrientCardDisplay> BuildExtraMacros(DailyTotals t) => new()
    {
        new() { Icon = MacroIcon, Name = "Zucker", Current = t.Sugar, Goal = 20, Unit = "g", Key = "Sugar" },
        new() { Icon = MacroIcon, Name = "Ballaststoffe", Current = t.Fiber, Goal = 40, Unit = "g", Key = "Fiber" },
        new() { Icon = MacroIcon, Name = "Salz", Current = t.Salt, Goal = 6, Unit = "g", Key = "Salt" },
        new() { Icon = MacroIcon, Name = "Gesättigte Fettsäuren", Current = t.SaturatedFat, Goal = 20, Unit = "g", Key = "SaturatedFat" },
    };

    public static List<NutrientCardDisplay> BuildVitamins(DailyTotals t) => new()
    {
        new() { Icon = VitaminIcon, Name = "Vitamin A", Current = t.VitaminA, Goal = 800, Unit = "µg", Key = "VitaminA" },
        new() { Icon = VitaminIcon, Name = "Vitamin B1", Current = t.VitaminB1, Goal = 1.1, Unit = "mg", Key = "VitaminB1" },
        new() { Icon = VitaminIcon, Name = "Vitamin B2", Current = t.VitaminB2, Goal = 1.4, Unit = "mg", Key = "VitaminB2" },
        new() { Icon = VitaminIcon, Name = "Vitamin B3", Current = t.VitaminB3, Goal = 16, Unit = "mg", Key = "VitaminB3" },
        new() { Icon = VitaminIcon, Name = "Vitamin B5", Current = t.VitaminB5, Goal = 5, Unit = "mg", Key = "VitaminB5" },
        new() { Icon = VitaminIcon, Name = "Vitamin B6", Current = t.VitaminB6, Goal = 1.4, Unit = "mg", Key = "VitaminB6" },
        new() { Icon = VitaminIcon, Name = "Vitamin B7", Current = t.VitaminB7, Goal = 50, Unit = "µg", Key = "VitaminB7" },
        new() { Icon = VitaminIcon, Name = "Vitamin B9", Current = t.VitaminB9, Goal = 400, Unit = "µg", Key = "VitaminB9" },
        new() { Icon = VitaminIcon, Name = "Vitamin B12", Current = t.VitaminB12, Goal = 2.5, Unit = "µg", Key = "VitaminB12" },
        new() { Icon = VitaminIcon, Name = "Vitamin C", Current = t.VitaminC, Goal = 80, Unit = "mg", Key = "VitaminC" },
        new() { Icon = VitaminIcon, Name = "Vitamin D", Current = t.VitaminD, Goal = 20, Unit = "µg", Key = "VitaminD" },
        new() { Icon = VitaminIcon, Name = "Vitamin E", Current = t.VitaminE, Goal = 12, Unit = "mg", Key = "VitaminE" },
        new() { Icon = VitaminIcon, Name = "Vitamin K", Current = t.VitaminK, Goal = 75, Unit = "µg", Key = "VitaminK" },
    };

    public static List<NutrientCardDisplay> BuildMinerals(DailyTotals t) => new()
    {
        new() { Icon = MineralIcon, Name = "Kalzium", Current = t.Calcium, Goal = 800, Unit = "mg", Key = "Calcium" },
        new() { Icon = MineralIcon, Name = "Magnesium", Current = t.Magnesium, Goal = 375, Unit = "mg", Key = "Magnesium" },
        new() { Icon = MineralIcon, Name = "Kalium", Current = t.Potassium, Goal = 2000, Unit = "mg", Key = "Potassium" },
        new() { Icon = MineralIcon, Name = "Natrium", Current = t.Sodium, Goal = 2300, Unit = "mg", Key = "Sodium" },
        new() { Icon = MineralIcon, Name = "Phosphor", Current = t.Phosphorus, Goal = 700, Unit = "mg", Key = "Phosphorus" },
        new() { Icon = MineralIcon, Name = "Chlorid", Current = t.Chloride, Goal = 2300, Unit = "mg", Key = "Chloride" },
        new() { Icon = MineralIcon, Name = "Schwefel", Current = t.Sulfur, Goal = 1000, Unit = "mg", Key = "Sulfur" },
        new() { Icon = MineralIcon, Name = "Eisen", Current = t.Iron, Goal = 14, Unit = "mg", Key = "Iron" },
        new() { Icon = MineralIcon, Name = "Zink", Current = t.Zinc, Goal = 10, Unit = "mg", Key = "Zinc" },
        new() { Icon = MineralIcon, Name = "Selen", Current = t.Selenium, Goal = 55, Unit = "µg", Key = "Selenium" },
        new() { Icon = MineralIcon, Name = "Kupfer", Current = t.Copper, Goal = 1, Unit = "mg", Key = "Copper" },
        new() { Icon = MineralIcon, Name = "Mangan", Current = t.Manganese, Goal = 2, Unit = "mg", Key = "Manganese" },
        new() { Icon = MineralIcon, Name = "Jod", Current = t.Iodine, Goal = 150, Unit = "µg", Key = "Iodine" },
        new() { Icon = MineralIcon, Name = "Fluorid", Current = t.Fluoride, Goal = 3.5, Unit = "mg", Key = "Fluoride" },
        new() { Icon = MineralIcon, Name = "Chrom", Current = t.Chromium, Goal = 50, Unit = "µg", Key = "Chromium" },
        new() { Icon = MineralIcon, Name = "Molybdän", Current = t.Molybdenum, Goal = 70, Unit = "µg", Key = "Molybdenum" },
        new() { Icon = MineralIcon, Name = "Kobalt", Current = t.Cobalt, Goal = 0.3, Unit = "µg", Key = "Cobalt" },
        new() { Icon = MineralIcon, Name = "Silizium", Current = t.Silicon, Goal = 25, Unit = "mg", Key = "Silicon" },
    };

    public static List<NutrientCardDisplay> BuildOthers(DailyTotals t) => new()
    {
        new() { Icon = OtherIcon, Name = "Zuckeralkohole", Current = t.SugarAlcohols, Goal = 0, Unit = "g", Key = "SugarAlcohols" },
        new() { Icon = OtherIcon, Name = "Alkohol", Current = t.Alcohol, Goal = 0, Unit = "g", Key = "Alcohol" },
        new() { Icon = OtherIcon, Name = "Omega-3-Fettsäuren", Current = t.Omega3, Goal = 500, Unit = "mg", Key = "Omega3" },
        new() { Icon = OtherIcon, Name = "Omega-6-Fettsäuren", Current = t.Omega6, Goal = 15, Unit = "g", Key = "Omega6" },
        new() { Icon = OtherIcon, Name = "Omega-9-Fettsäuren", Current = t.Omega9, Goal = 15, Unit = "g", Key = "Omega9" },
    };
}
