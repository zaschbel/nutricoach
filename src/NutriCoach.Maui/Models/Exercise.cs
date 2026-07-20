namespace NutriCoach.App.Models;

/// <summary>
/// Stammdaten einer Übung inkl. Animationsreferenz (wie in den MCI-Screenshots).
/// Wird in einem späteren Schritt mit echten Inhalten (GIF/Lottie-Animationen) befüllt –
/// hier erstmal das Datenmodell, damit der Trainingsplan-Generator darauf aufbauen kann.
/// </summary>
public class Exercise
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string PrimaryMuscleGroup { get; set; } = string.Empty; // z. B. "Beine", "Brust", "Rücken"
    public string? SecondaryMuscleGroup { get; set; }
    public string Equipment { get; set; } = string.Empty;          // "Maschine", "Langhantel", "Körpergewicht" ...
    public string? AnimationPath { get; set; }                     // lokaler Pfad zur Animation (später mit echten Bildern/GIFs befüllbar)

    /// <summary>Kommagetrennte Körperbereiche, bei denen diese Übung gemieden/angepasst werden sollte.</summary>
    public string? ContraindicatedForBodyAreas { get; set; }

    public ExperienceLevel MinimumExperience { get; set; } = ExperienceLevel.Anfänger;

    /// <summary>Steuert, welche Eingabefelder beim Eintragen sinnvoll sind (Gewicht/Wdh., nur Wdh., Strecke/Zeit, Widerstand/Zeit).</summary>
    public ExerciseInputType InputType { get; set; } = ExerciseInputType.GewichtWiederholungen;

    /// <summary>MET-Wert (Energieumsatz-Vielfaches) für die Kalorien-Schätzung, grobe Richtwerte je Übungsart.</summary>
    public double MetValue { get; set; } = 5.0;

    /// <summary>Bei "Drinnen": zusätzlich Widerstand (Bike) oder Steigung (Laufband) abfragen - oder keins von beidem.</summary>
    public IndoorCardioMetric IndoorMetric { get; set; } = IndoorCardioMetric.Keine;

    /// <summary>Kurzer Tipp/Hinweis zur Übung (Ausführung, worauf achten).</summary>
    public string? Tip { get; set; }

    /// <summary>ID der Übung in der wger.de-Online-Datenbank, falls von dort importiert (verhindert Duplikate).</summary>
    public int? WgerId { get; set; }
}
