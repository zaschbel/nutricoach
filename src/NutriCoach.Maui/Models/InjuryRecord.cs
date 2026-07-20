namespace NutriCoach.App.Models;

/// <summary>
/// Eine einzelne Verletzung/Einschränkung. Mehrere pro Profil möglich (z. B. Knie + Schulter).
/// Wird vom Trainingsplan-Generator genutzt, um Übungen auszuschließen oder anzupassen.
/// </summary>
public class InjuryRecord
{
    public int Id { get; set; }
    public int UserProfileId { get; set; }

    public string BodyArea { get; set; } = string.Empty;   // z. B. "Rechtes Knie", "Untere Rückenmuskulatur"
    public InjurySeverity Severity { get; set; }
    public string? Description { get; set; }               // Freitext für Details
    public DateOnly? OnsetDate { get; set; }                // seit wann bestehen die Beschwerden (ungefähr)
    public DateOnly ReportedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public bool IsActive { get; set; } = true;              // kann später als "ausgeheilt" markiert werden
}
