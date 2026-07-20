namespace NutriCoach.App.Models;

/// <summary>Eine komplette Trainingseinheit an einem Tag (z. B. "Full Body" oder "Cardio").</summary>
public class TrainingSession
{
    public int Id { get; set; }
    public int UserProfileId { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Name { get; set; } = string.Empty;   // "Full Body", "Push Day" ...

    public List<StrengthSetEntry> StrengthSets { get; set; } = new();
    public List<CardioEntry> CardioEntries { get; set; } = new();
}

/// <summary>Ein einzelner Kraft-Satz: Übung, Gewicht, Wiederholungen.</summary>
public class StrengthSetEntry
{
    public int Id { get; set; }
    public int TrainingSessionId { get; set; }

    public string ExerciseName { get; set; } = string.Empty;  // verweist auf Exercise.Name (Übungsbibliothek)
    public int SetNumber { get; set; }
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Ein Cardio-Eintrag mit allen von dir geforderten Details (Ort, Widerstand, Distanz ...).</summary>
public class CardioEntry
{
    public int Id { get; set; }
    public int TrainingSessionId { get; set; }

    public string ExerciseName { get; set; } = string.Empty;   // z. B. "Radfahren (draußen)" - für die Anzeige
    public CardioType Type { get; set; }
    public CardioLocation Location { get; set; }
    public int DurationMinutes { get; set; }
    public double? DistanceKm { get; set; }
    public int? ResistanceLevel { get; set; }   // z. B. Spinning-Bike-Widerstand 1-25 (Matrix-Skala als Annahme)
    public double? InclinePercent { get; set; }  // z. B. Laufband-Steigung in %
    public int? AvgHeartRate { get; set; }
    public int? EstimatedKcalBurned { get; set; }
}

/// <summary>Wasseraufnahme pro Tag, in ml, mehrere Einträge möglich.</summary>
public class WaterEntry
{
    public int Id { get; set; }
    public int UserProfileId { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public int AmountMl { get; set; }
    public TimeOnly Time { get; set; }
}

/// <summary>Schrittzahl pro Tag - manuell eingetragen (später ggf. per Health-Anbindung befüllbar), ein Eintrag pro Tag.</summary>
public class StepsEntry
{
    public int Id { get; set; }
    public int UserProfileId { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public int Steps { get; set; }
}
