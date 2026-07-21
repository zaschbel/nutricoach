namespace NutriCoach.App.Models;

/// <summary>
/// Eine wiederverwendbare Trainingsvorlage: eine feste, benannte Liste von Übungen (z. B. "Push Day"
/// -> Bankdrücken, Schulterdrücken, Trizepsdrücken), OHNE vorausgefüllte Sätze/Gewichte/Wiederholungen -
/// die trägt der Nutzer weiterhin selbst pro Satz ein, die Vorlage erspart nur das erneute Eintippen
/// der Übungsnamen.
/// </summary>
public class WorkoutTemplate
{
    public int Id { get; set; }
    public int UserProfileId { get; set; }
    public string Name { get; set; } = string.Empty; // z.B. "Push Day"
    public List<WorkoutTemplateExercise> Exercises { get; set; } = new();
}

/// <summary>Eine einzelne Übung innerhalb einer Trainingsvorlage, in fester Reihenfolge (SortOrder).</summary>
public class WorkoutTemplateExercise
{
    public int Id { get; set; }
    public int WorkoutTemplateId { get; set; }
    public string ExerciseName { get; set; } = string.Empty; // verweist auf Exercise.Name, wie StrengthSetEntry.ExerciseName
    public int SortOrder { get; set; }
}
