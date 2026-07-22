using Microsoft.EntityFrameworkCore;
using NutriCoach.App.Data;
using NutriCoach.App.Models;

namespace NutriCoach.App.Services;

/// <summary>
/// Verwaltet Trainingsvorlagen (feste Übungslisten wie "Push Day"), damit man beim Anlegen einer
/// neuen Trainingseinheit nicht jedes Mal dieselben Übungsnamen neu eintippen/suchen muss.
/// Alle Methoden sind bewusst fehlertolerant (try/catch, leere/no-op Rückgabe statt Exception) -
/// das Vorlagen-Feature ist neu und darf im Fehlerfall niemals die ganze App zum Absturz bringen.
/// </summary>
public class WorkoutTemplateService
{
    /// <summary>
    /// Lädt alle Vorlagen eines Nutzers inkl. ihrer Übungen (nach SortOrder sortiert), alphabetisch nach Name.
    /// Wirft absichtlich weiter, statt Fehler zu verschlucken (frühere Version tat das und liess das
    /// Feature dadurch "stillschweigend kaputt" aussehen) - der Aufrufer (ManageTemplatesViewModel)
    /// zeigt den echten Fehler jetzt sichtbar an.
    /// </summary>
    public async Task<List<WorkoutTemplate>> GetTemplatesAsync(int userProfileId)
    {
        await using var context = new AppDbContext();
        var templates = await context.WorkoutTemplates
            .Include(t => t.Exercises)
            .Where(t => t.UserProfileId == userProfileId)
            .OrderBy(t => t.Name)
            .ToListAsync();

        foreach (var template in templates)
            template.Exercises = template.Exercises.OrderBy(e => e.SortOrder).ToList();

        return templates;
    }

    /// <summary>
    /// Legt eine neue Vorlage mit den übergebenen Übungsnamen an (Reihenfolge = Liste-Reihenfolge).
    /// Wirft absichtlich weiter statt Fehler zu verschlucken (siehe GetTemplatesAsync).
    /// </summary>
    public async Task CreateTemplateAsync(int userProfileId, string name, List<string> exerciseNames)
    {
        if (string.IsNullOrWhiteSpace(name) || exerciseNames.Count == 0) return;

        await using var context = new AppDbContext();
        var template = new WorkoutTemplate { UserProfileId = userProfileId, Name = name.Trim() };

        var order = 0;
        foreach (var exerciseName in exerciseNames)
        {
            template.Exercises.Add(new WorkoutTemplateExercise { ExerciseName = exerciseName, SortOrder = order++ });
        }

        context.WorkoutTemplates.Add(template);
        await context.SaveChangesAsync();
    }

    /// <summary>Löscht eine Vorlage (und über die Kaskade automatisch alle ihre Übungs-Einträge).</summary>
    public async Task DeleteTemplateAsync(int templateId)
    {
        try
        {
            await using var context = new AppDbContext();
            var template = await context.WorkoutTemplates.FindAsync(templateId);
            if (template is not null)
            {
                context.WorkoutTemplates.Remove(template);
                await context.SaveChangesAsync();
            }
        }
        catch
        {
        }
    }
}
