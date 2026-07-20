using System.Linq;

namespace NutriCoach.App.Data;

/// <summary>
/// Sorgt dafür, dass die lokale SQLite-Datenbank beim ersten Start automatisch angelegt wird.
/// Kein manuelles Setup nötig – der Nutzer merkt davon nichts.
/// </summary>
public static class DatabaseInitializer
{
    public static void EnsureCreated()
    {
        using var context = new AppDbContext();
        context.Database.EnsureCreated();

        // Übungsdatenbank befüllen bzw. um neue Übungen ergänzen - so muss die Datenbank
        // nicht mehr gelöscht werden, nur weil neue Übungen zur Liste hinzukommen.
        var existingNames = context.Exercises.Select(e => e.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = ExerciseSeedData.GetSeedExercises()
            .Where(e => !existingNames.Contains(e.Name))
            .ToList();

        if (missing.Count > 0)
        {
            context.Exercises.AddRange(missing);
            context.SaveChanges();
        }

        // Hinweis für später: Sobald sich das Datenmodell nach dem ersten Release ändert,
        // wird hier auf EF-Core-Migrationen (context.Database.Migrate()) umgestellt,
        // damit bestehende Nutzerdaten beim Update nicht verloren gehen.
    }
}
