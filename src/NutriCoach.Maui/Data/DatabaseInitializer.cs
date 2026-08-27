using System.Linq;
using Microsoft.EntityFrameworkCore;

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

        // WICHTIG: EnsureCreated() legt Tabellen NUR an, wenn die Datenbankdatei noch gar nicht existiert.
        // Bei bereits bestehenden Installationen (echte Nutzerdaten!) passiert dabei GAR NICHTS mehr -
        // neue Tabellen für neue Features (hier: Trainingsvorlagen) würden dann nie entstehen und der
        // erste Zugriff darauf würde mit "SQLite Error: no such table" abstürzen. Deshalb hier zusätzlich
        // per rohem SQL sicherstellen, dass die neuen Tabellen in JEDEM Fall existieren - das ist ein
        // reiner No-Op, falls sie (z. B. bei einer frischen Installation über EnsureCreated) schon da sind.
        try
        {
            context.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS ""WorkoutTemplates"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_WorkoutTemplates"" PRIMARY KEY AUTOINCREMENT,
    ""UserProfileId"" INTEGER NOT NULL,
    ""Name"" TEXT NOT NULL
);");
            context.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS ""WorkoutTemplateExercises"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_WorkoutTemplateExercises"" PRIMARY KEY AUTOINCREMENT,
    ""WorkoutTemplateId"" INTEGER NOT NULL,
    ""ExerciseName"" TEXT NOT NULL,
    ""SortOrder"" INTEGER NOT NULL,
    CONSTRAINT ""FK_WorkoutTemplateExercises_WorkoutTemplates_WorkoutTemplateId"" FOREIGN KEY (""WorkoutTemplateId"") REFERENCES ""WorkoutTemplates"" (""Id"") ON DELETE CASCADE
);");
        }
        catch
        {
            // Absichtlich verschluckt: sollte diese Absicherung selbst fehlschlagen, darf das die App
            // trotzdem nicht am Start hindern - lieber Vorlagen-Feature vorübergehend kaputt als
            // ein erneuter Absturz-Loop wie beim Schrittzähler-Feature zuvor in dieser Session.
        }

        // Neue Naehrwert-Spalten (Vitamine, Mineralstoffe, Weitere) fuer Bestandsinstallationen
        // nachruesten - SQLite kennt kein "ADD COLUMN IF NOT EXISTS", daher jede Spalte einzeln
        // versuchen und einen Fehler (Spalte existiert schon) stillschweigend ignorieren.
        string[] newFoodItemColumns =
        {
            "VitaminAPer100", "VitaminB1Per100", "VitaminB2Per100", "VitaminB3Per100", "VitaminB5Per100",
            "VitaminB6Per100", "VitaminB7Per100", "VitaminB9Per100", "VitaminB12Per100", "VitaminCPer100",
            "VitaminDPer100", "VitaminEPer100", "VitaminKPer100",
            "CalciumPer100", "MagnesiumPer100", "PotassiumPer100", "SodiumPer100", "PhosphorusPer100",
            "ChloridePer100", "SulfurPer100", "IronPer100", "ZincPer100", "SeleniumPer100", "CopperPer100",
            "ManganesePer100", "IodinePer100", "FluoridePer100", "ChromiumPer100", "MolybdenumPer100",
            "CobaltPer100", "SiliconPer100",
            "SugarAlcoholsPer100", "AlcoholPer100", "Omega3Per100", "Omega6Per100", "Omega9Per100"
        };
        foreach (var column in newFoodItemColumns)
        {
            try
            {
                context.Database.ExecuteSqlRaw($@"ALTER TABLE ""FoodItems"" ADD COLUMN ""{column}"" REAL NOT NULL DEFAULT 0;");
            }
            catch
            {
                // Spalte existiert bereits aus einem frueheren Start - erwarteter No-Op-Fall.
            }
        }

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
