namespace NutriCoach.App.Models;

/// <summary>
/// Ein Fortschritts-Snapshot. Ermöglicht Aussagen wie "in 3 Monaten 4 kg abgenommen"
/// automatisch, weil der Verlauf gespeichert wird statt nur der letzte Wert.
/// </summary>
public class BodyMeasurement
{
    public int Id { get; set; }
    public int UserProfileId { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public double WeightKg { get; set; }

    // Optionale Umfänge (wie im "AI Fitness Coach"-Screenshot: Chest, Waist, Thigh ...)
    public double? ChestCm { get; set; }
    public double? WaistCm { get; set; }
    public double? HipCm { get; set; }
    public double? ThighCm { get; set; }
    public double? ArmCm { get; set; }
    public double? BodyFatPercent { get; set; }

    public string? PhotoPath { get; set; }   // optionales Fortschrittsfoto (lokal gespeichert)
}
