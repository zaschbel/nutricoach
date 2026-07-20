using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;
using NutriCoach.App.Data;
using NutriCoach.App.Models;

namespace NutriCoach.App.Services;

/// <summary>
/// Einziger Zugriffspunkt auf das Nutzerprofil. Die App fragt beim Start hier nach:
/// "Gibt es schon ein Profil?" – wenn ja, wird direkt der Hauptbildschirm gezeigt,
/// wenn nein, startet das Onboarding. So muss der Nutzer Basisdaten nur einmal eingeben.
/// </summary>
public class UserProfileService
{
    /// <summary>Aktuell gibt es genau ein aktives Profil pro Installation (Einzelnutzer-Desktop-App).</summary>
    public async Task<UserProfile?> GetActiveProfileAsync()
    {
        await using var context = new AppDbContext();
        return await context.UserProfiles
            .Include(u => u.Injuries)
            .Include(u => u.BodyMeasurements)
            .FirstOrDefaultAsync(u => u.OnboardingCompleted);
    }

    /// <summary>
    /// Aktualisiert die persönlichen Angaben eines BESTEHENDEN Profils (Name, Ziel, Größe usw.) -
    /// im Gegensatz zu CreateProfileAsync wird hier kein neues Profil angelegt. Das aktuelle
    /// Gewicht wird bewusst NICHT hier mit angefasst, dafür gibt's die eigene tagesbezogene
    /// Gewichtsverfolgung (SetWeightForDateAsync).
    /// </summary>
    public async Task UpdateProfileDetailsAsync(int profileId, string name, DateOnly birthDate, Gender gender,
        double heightCm, FitnessGoal goal, ActivityLevel activityLevel, DailyJobActivity jobActivity, ExperienceLevel experience)
    {
        await using var context = new AppDbContext();
        var profile = await context.UserProfiles.FindAsync(profileId);
        if (profile is null) return;

        profile.Name = name;
        profile.BirthDate = birthDate;
        profile.Gender = gender;
        profile.HeightCm = heightCm;
        profile.Goal = goal;
        profile.ActivityLevel = activityLevel;
        profile.JobActivity = jobActivity;
        profile.Experience = experience;
        profile.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    /// <summary>Kopiert ein gewähltes Foto dauerhaft in den App-Datenordner und merkt sich den Pfad im Profil.</summary>
    public async Task<string> SetProfilePictureAsync(int profileId, Stream sourceStream)
    {
        var folder = Path.Combine(FileSystem.AppDataDirectory, "profile_pictures");
        Directory.CreateDirectory(folder);
        var fileName = $"profile_{profileId}.jpg";
        var fullPath = Path.Combine(folder, fileName);

        using (var fileStream = File.Create(fullPath))
        {
            await sourceStream.CopyToAsync(fileStream);
        }

        await using var context = new AppDbContext();
        var profile = await context.UserProfiles.FindAsync(profileId);
        if (profile is not null)
        {
            profile.ProfilePicturePath = fullPath;
            await context.SaveChangesAsync();
        }

        return fullPath;
    }

    public async Task<UserProfile> CreateProfileAsync(UserProfile profile, double? initialBodyFatPercent = null)
    {
        await using var context = new AppDbContext();
        profile.OnboardingCompleted = true;
        profile.CreatedAt = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;

        // Erster Gewichtseintrag direkt aus dem Onboarding, damit der Fortschrittsverlauf
        // sofort einen Startpunkt hat ("in 3 Monaten 4kg abgenommen" braucht einen Tag 0).
        // Wichtig: über profile.BodyMeasurements hinzufügen (nicht context.BodyMeasurements
        // mit manuell gesetzter UserProfileId!) - zum Zeitpunkt dieses Aufrufs hat "profile"
        // noch gar keine echte Id (die vergibt die Datenbank erst beim Speichern). Über die
        // Navigation-Eigenschaft erkennt EF Core die Beziehung automatisch und setzt die
        // richtige Id nach dem Einfügen des Profils - das war der Grund für den FOREIGN KEY-Fehler.
        profile.BodyMeasurements.Add(new BodyMeasurement
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            WeightKg = profile.CurrentWeightKg,
            BodyFatPercent = initialBodyFatPercent
        });

        context.UserProfiles.Add(profile);

        await context.SaveChangesAsync();
        return profile;
    }

    public async Task UpdateWeightAsync(int profileId, double newWeightKg)
    {
        await using var context = new AppDbContext();
        var profile = await context.UserProfiles.FindAsync(profileId);
        if (profile is null) return;

        profile.CurrentWeightKg = newWeightKg;
        profile.UpdatedAt = DateTime.UtcNow;

        context.BodyMeasurements.Add(new BodyMeasurement
        {
            UserProfileId = profileId,
            Date = DateOnly.FromDateTime(DateTime.Today),
            WeightKg = newWeightKg
        });

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Setzt das Gewicht für EINEN bestimmten Tag (nicht immer "heute") - z. B. um eine
    /// vergangene Messung nachträglich zu korrigieren, ohne dass das alle anderen Tage mit
    /// überschreibt. "Aktuelles Gewicht" auf dem Profil wird nur aktualisiert, wenn der bearbeitete
    /// Tag tatsächlich der jüngste bekannte Messpunkt ist.
    /// </summary>
    public async Task SetWeightForDateAsync(int profileId, DateOnly date, double weightKg)
    {
        await using var context = new AppDbContext();

        var existing = await context.BodyMeasurements
            .Where(m => m.UserProfileId == profileId && m.Date == date)
            .OrderByDescending(m => m.Id)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            existing.WeightKg = weightKg;
        }
        else
        {
            context.BodyMeasurements.Add(new BodyMeasurement
            {
                UserProfileId = profileId,
                Date = date,
                WeightKg = weightKg
            });
        }

        await context.SaveChangesAsync();

        // "Aktuelles Gewicht" nur aktualisieren, wenn es wirklich der neueste bekannte Wert ist -
        // eine nachträgliche Korrektur eines vergangenen Tages soll nicht die Anzeige des
        // tatsächlich aktuellen Gewichts überschreiben.
        var latest = await context.BodyMeasurements
            .Where(m => m.UserProfileId == profileId)
            .OrderByDescending(m => m.Date).ThenByDescending(m => m.Id)
            .FirstOrDefaultAsync();

        if (latest is not null)
        {
            var profile = await context.UserProfiles.FindAsync(profileId);
            if (profile is not null)
            {
                profile.CurrentWeightKg = latest.WeightKg;
                profile.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }
    }

    /// <summary>Gewicht für einen bestimmten Tag - exakter Messwert falls vorhanden, sonst der zuletzt bekannte Wert davor (gleiche Logik wie im Statistik-Diagramm).</summary>
    public async Task<double> GetWeightForDateAsync(int profileId, DateOnly date)
    {
        await using var context = new AppDbContext();

        var exact = await context.BodyMeasurements
            .Where(m => m.UserProfileId == profileId && m.Date == date)
            .OrderByDescending(m => m.Id)
            .FirstOrDefaultAsync();
        if (exact is not null) return exact.WeightKg;

        var before = await context.BodyMeasurements
            .Where(m => m.UserProfileId == profileId && m.Date < date)
            .OrderByDescending(m => m.Date).ThenByDescending(m => m.Id)
            .FirstOrDefaultAsync();
        if (before is not null) return before.WeightKg;

        var profile = await context.UserProfiles.FindAsync(profileId);
        return profile?.CurrentWeightKg ?? 0;
    }

    public async Task AddInjuryAsync(int profileId, InjuryRecord injury)
    {
        await using var context = new AppDbContext();
        injury.UserProfileId = profileId;
        context.Injuries.Add(injury);
        await context.SaveChangesAsync();
    }

    /// <summary>Speichert, wie viele Tage pro Woche die Person trainieren möchte (einmal gefragt, dauerhaft gemerkt).</summary>
    public async Task SetWeeklyTrainingGoalAsync(int profileId, int days)
    {
        await using var context = new AppDbContext();
        var profile = await context.UserProfiles.FindAsync(profileId);
        if (profile is null) return;

        profile.WeeklyTrainingGoalDays = days;
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Liefert die Gewichtsveränderung über einen Zeitraum – Grundlage für automatische
    /// Lob-/Fortschrittsmeldungen ("Du hast in den letzten 30 Tagen 2 kg abgenommen!").
    /// </summary>
    public async Task<double?> GetWeightChangeAsync(int profileId, int daysBack)
    {
        await using var context = new AppDbContext();
        var cutoff = DateOnly.FromDateTime(DateTime.Today.AddDays(-daysBack));

        var measurements = await context.BodyMeasurements
            .Where(m => m.UserProfileId == profileId && m.Date >= cutoff)
            .OrderBy(m => m.Date)
            .ToListAsync();

        if (measurements.Count < 2) return null;
        return Math.Round(measurements.Last().WeightKg - measurements.First().WeightKg, 1);
    }

    /// <summary>
    /// Gewicht pro Tag für die letzten N Tage (für die Verlaufsgrafik in den Statistiken).
    /// Tage ohne eigene Messung übernehmen den zuletzt bekannten Wert (genau wie in der Vorlage:
    /// die Linie bleibt flach, bis eine neue Messung eingetragen wird).
    /// </summary>
    /// <summary>
    /// Gewicht pro Tag für die letzten N Tage (für die Verlaufsgrafik in den Statistiken).
    /// WICHTIG: Tage OHNE eigene Messung liefern null zurück - kein "Auffüllen" mit einem
    /// geschätzten Wert mehr. Das hatte sonst dazu geführt, dass eine spätere Korrektur des
    /// heutigen Gewichts rückwirkend wie eine Änderung an Tagen ohne eigene Messung aussah,
    /// weil es (gerade bei wenig Verlauf) keinen zuverlässigen zweiten Bezugspunkt gab.
    /// </summary>
    public async Task<List<(DateOnly Date, double? WeightKg)>> GetWeightHistoryAsync(int profileId, int days)
    {
        await using var context = new AppDbContext();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = today.AddDays(-(days - 1));

        var measurements = await context.BodyMeasurements
            .Where(m => m.UserProfileId == profileId && m.Date >= start && m.Date <= today)
            .OrderBy(m => m.Date).ThenBy(m => m.Id)
            .ToListAsync();

        var result = new List<(DateOnly, double?)>();
        for (var i = 0; i < days; i++)
        {
            var date = start.AddDays(i);
            var todaysMeasurement = measurements.Where(m => m.Date == date).OrderByDescending(m => m.Id).FirstOrDefault();
            result.Add((date, todaysMeasurement?.WeightKg));
        }
        return result;
    }
}
