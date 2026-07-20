namespace NutriCoach.App.Models;

/// <summary>
/// Ein Tag im persönlichen Wochenplan (z. B. Montag -> "Push", Mittwoch -> "Ruhetag").
/// Wird entweder vom Nutzer selbst zusammengestellt oder automatisch vorgeschlagen.
/// </summary>
public class TrainingPlanDay
{
    public int Id { get; set; }
    public int UserProfileId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string PlanName { get; set; } = "Frei";
}
