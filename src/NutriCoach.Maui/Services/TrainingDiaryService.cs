using System.Linq;
using Microsoft.EntityFrameworkCore;
using NutriCoach.App.Data;
using NutriCoach.App.Models;

namespace NutriCoach.App.Services;

/// <summary>Eine Gruppe von Übungen innerhalb einer Trainingseinheit (z. B. "Brust" oder "Cardio"), mit eigener Bewertung.</summary>
public class ExerciseGroupDisplay
{
    public string Title { get; set; } = string.Empty;
    public List<string> Lines { get; set; } = new();
    public string Assessment { get; set; } = string.Empty;
}

/// <summary>Eine Trainingseinheit zusammen mit den daraus berechneten Kennzahlen für die Anzeige.</summary>
public class TrainingSessionDisplay
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int TotalSets { get; set; }
    public double TotalVolumeKg { get; set; }
    public bool HasCardio { get; set; }
    public int CardioDurationMinutes { get; set; }
    public double EstimatedKcal { get; set; }
    public List<ExerciseGroupDisplay> Groups { get; set; } = new();
}

/// <summary>Eingabe für einen einzelnen Kraft-Satz beim Anlegen einer neuen Trainingseinheit.</summary>
public record SetInput(double WeightKg, int Reps);

/// <summary>
/// Eingabe für eine Übung beim Anlegen einer neuen Trainingseinheit. Je nach InputType werden
/// entweder Sets (Kraft/Körpergewicht) oder Dauer+Distanz/Widerstand (Cardio) verwendet.
/// </summary>
public record ExerciseInput(
    string ExerciseName,
    ExerciseInputType InputType,
    List<SetInput> Sets,
    int? DurationMinutes,
    double? DistanceKm,
    int? ResistanceLevel,
    double? InclinePercent,
    CardioLocation Location);

/// <summary>Eine einzelne, bereits gespeicherte Übung innerhalb einer Trainingseinheit - fürs Bearbeiten/Entfernen im UI.</summary>
public class ExistingExerciseData
{
    public Exercise Exercise { get; set; } = null!;
    public List<SetInput> Sets { get; set; } = new();
    public int? DurationMinutes { get; set; }
    public double? DistanceKm { get; set; }
    public int? ResistanceLevel { get; set; }
    public double? InclinePercent { get; set; }
    public CardioLocation Location { get; set; }
}

public class TrainingDiaryService
{
    /// <summary>Grobe MET-Werte (Energieumsatz-Vielfaches) für Cardio-Arten, falls keine exakte Übung zugeordnet werden kann.</summary>
    private static double DefaultMetFor(CardioType type) => type switch
    {
        CardioType.Laufen => 9.8,
        CardioType.Radfahren => 7.5,
        CardioType.SpinningBike => 8.5,
        CardioType.Rudern => 7.0,
        CardioType.Walking => 4.5,
        CardioType.Schwimmen => 8.0,
        _ => 6.0
    };

    private static CardioType MapToCardioType(string exerciseName)
    {
        var n = exerciseName.ToLower();
        if (n.Contains("lauf")) return CardioType.Laufen;
        if (n.Contains("rad")) return CardioType.Radfahren;
        if (n.Contains("spinning")) return CardioType.SpinningBike;
        if (n.Contains("ruder")) return CardioType.Rudern;
        if (n.Contains("walk")) return CardioType.Walking;
        if (n.Contains("schwimm")) return CardioType.Schwimmen;
        return CardioType.Sonstiges;
    }

    /// <summary>
    /// Kalorienschätzung nach der Standard-MET-Formel: kcal/min = MET * 3.5 * Körpergewicht(kg) / 200.
    /// Widerstand (Bike-Skala 1-25, wie bei Matrix-Geräten üblich) und Steigung (Laufband, in %)
    /// erhöhen den effektiven MET-Wert spürbar - das war zuvor der Grund, warum höhere Stufen
    /// keinen Unterschied im Kalorienverbrauch gemacht haben.
    /// </summary>
    private static double EstimateKcal(double baseMet, double bodyWeightKg, double minutes,
        int? resistanceLevel = null, double? inclinePercent = null)
    {
        var met = baseMet;
        if (resistanceLevel.HasValue)
            met += Math.Clamp(resistanceLevel.Value, 1, 25) * 0.5; // Stufe 23 ≈ +11,5 MET
        if (inclinePercent.HasValue)
            met += Math.Max(inclinePercent.Value, 0) * 0.3; // jedes % Steigung ≈ +0,3 MET

        return met * 3.5 * bodyWeightKg / 200.0 * minutes;
    }

    public async Task<List<TrainingSessionDisplay>> GetSessionsForDateAsync(int userProfileId, DateOnly date, double bodyWeightKg)
    {
        await using var context = new AppDbContext();

        var sessions = await context.TrainingSessions
            .Include(s => s.StrengthSets)
            .Include(s => s.CardioEntries)
            .Where(s => s.UserProfileId == userProfileId && s.Date == date)
            .ToListAsync();

        if (sessions.Count == 0) return new List<TrainingSessionDisplay>();

        // Übungsdatenbank einmal laden für Muskelgruppen/MET-Zuordnung per Namen
        var exerciseLookup = await context.Exercises.ToListAsync();
        var byName = exerciseLookup.ToDictionary(e => e.Name, e => e, StringComparer.OrdinalIgnoreCase);

        var result = new List<TrainingSessionDisplay>();

        foreach (var s in sessions)
        {
            double totalKcal = 0;
            var groups = new List<ExerciseGroupDisplay>();

            // ---------------- Kraftübungen: nach Muskelgruppe gruppiert, in Reihenfolge des ersten Auftretens ----------------
            var groupOrder = new List<string>();
            var setsByGroup = new Dictionary<string, List<StrengthSetEntry>>();

            foreach (var set in s.StrengthSets)
            {
                var muscleGroup = byName.TryGetValue(set.ExerciseName, out var ex) ? ex.PrimaryMuscleGroup : "Sonstiges";
                if (!setsByGroup.ContainsKey(muscleGroup))
                {
                    setsByGroup[muscleGroup] = new List<StrengthSetEntry>();
                    groupOrder.Add(muscleGroup);
                }
                setsByGroup[muscleGroup].Add(set);

                var met = byName.TryGetValue(set.ExerciseName, out var exForMet) ? exForMet.MetValue : 5.0;
                // Grobe Annahme: ~45 Sekunden aktive Belastung pro Satz für die Kalorienschätzung
                totalKcal += EstimateKcal(met, bodyWeightKg, 0.75);
            }

            foreach (var muscleGroup in groupOrder)
            {
                var sets = setsByGroup[muscleGroup];
                var lines = sets.GroupBy(x => x.ExerciseName)
                    .Select(g => $"{g.Key} — {g.Count()} Sätze · {Math.Round(g.Sum(x => x.WeightKg * x.Reps), 0)} kg Volumen")
                    .ToList();

                var groupAssessment = sets.Count >= 9
                    ? $"💪 Starkes Volumen für {muscleGroup}."
                    : sets.Count >= 4
                        ? $"✅ Solides Volumen für {muscleGroup}."
                        : $"➖ Eher wenig für {muscleGroup} - fürs nächste Mal ruhig noch 1-2 Sätze mehr.";

                groups.Add(new ExerciseGroupDisplay { Title = $"🏋️ {muscleGroup}", Lines = lines, Assessment = groupAssessment });
            }

            // ---------------- Cardio: immer als letzte Gruppe ----------------
            if (s.CardioEntries.Count > 0)
            {
                var cardioLines = new List<string>();
                foreach (var cardio in s.CardioEntries)
                {
                    var baseMet = byName.TryGetValue(cardio.ExerciseName, out var cardioEx) ? cardioEx.MetValue : DefaultMetFor(cardio.Type);
                    var kcalForEntry = EstimateKcal(baseMet, bodyWeightKg, cardio.DurationMinutes, cardio.ResistanceLevel, cardio.InclinePercent);
                    totalKcal += kcalForEntry;

                    var parts = new List<string> { $"{cardio.DurationMinutes} Min" };
                    if (cardio.DistanceKm.HasValue) parts.Add($"{cardio.DistanceKm:0.0} km");
                    parts.Add(cardio.Location == CardioLocation.Drinnen ? "Drinnen" : "Draußen");
                    if (cardio.ResistanceLevel.HasValue) parts.Add($"Stufe {cardio.ResistanceLevel}");
                    if (cardio.InclinePercent.HasValue) parts.Add($"{cardio.InclinePercent:0.#}% Steigung");
                    parts.Add($"~{Math.Round(kcalForEntry, 0)} kcal");

                    cardioLines.Add($"{cardio.ExerciseName} — {string.Join(" · ", parts)}");
                }

                var cardioMinutesTotal = s.CardioEntries.Sum(c => c.DurationMinutes);
                var cardioAssessment = cardioMinutesTotal >= 30
                    ? "💪 Gute Cardio-Dauer für einen echten Ausdauereffekt."
                    : cardioMinutesTotal >= 15
                        ? "✅ Solide Cardio-Einheit."
                        : "➖ Eher kurz - für einen spürbaren Ausdauereffekt ruhig länger.";

                groups.Add(new ExerciseGroupDisplay { Title = "🏃 Cardio", Lines = cardioLines, Assessment = cardioAssessment });
            }

            result.Add(new TrainingSessionDisplay
            {
                Id = s.Id,
                Name = s.Name,
                Date = s.Date,
                TotalSets = s.StrengthSets.Count,
                TotalVolumeKg = Math.Round(s.StrengthSets.Sum(x => x.WeightKg * x.Reps), 0),
                HasCardio = s.CardioEntries.Count > 0,
                CardioDurationMinutes = s.CardioEntries.Sum(c => c.DurationMinutes),
                EstimatedKcal = Math.Round(totalKcal, 0),
                Groups = groups
            });
        }

        return result;
    }

    /// <summary>Legt eine komplette neue Trainingseinheit inkl. aller Übungen/Sätze und Cardio-Anteile auf einmal an.</summary>
    public async Task CreateSessionAsync(int userProfileId, DateOnly date, string name, List<ExerciseInput> exercises)
    {
        await using var context = new AppDbContext();

        var session = new TrainingSession
        {
            UserProfileId = userProfileId,
            Date = date,
            Name = name
        };

        AddExerciseInputsToSession(session, exercises);

        context.TrainingSessions.Add(session);
        await context.SaveChangesAsync();
    }

    /// <summary>Fügt weitere Übungen zu einer bereits bestehenden Trainingseinheit hinzu (z. B. wenn man später noch etwas ergänzen möchte).</summary>
    public async Task AddExercisesToSessionAsync(int sessionId, List<ExerciseInput> exercises)
    {
        await using var context = new AppDbContext();
        var session = await context.TrainingSessions
            .Include(s => s.StrengthSets)
            .Include(s => s.CardioEntries)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null) return;

        AddExerciseInputsToSession(session, exercises);
        await context.SaveChangesAsync();
    }

    /// <summary>Lädt eine bestehende Trainingseinheit als Liste einzelner, editierbarer Übungen (für den Bearbeiten-Dialog).</summary>
    public async Task<(string Name, List<ExistingExerciseData> Exercises)> GetSessionDetailAsync(int sessionId)
    {
        await using var context = new AppDbContext();
        var session = await context.TrainingSessions
            .Include(s => s.StrengthSets)
            .Include(s => s.CardioEntries)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null) return (string.Empty, new List<ExistingExerciseData>());

        var exerciseLookup = await context.Exercises.ToListAsync();
        var byName = exerciseLookup.ToDictionary(e => e.Name, e => e, StringComparer.OrdinalIgnoreCase);

        var result = new List<ExistingExerciseData>();

        // Kraft-/Körpergewichtsübungen: nach Name gruppiert, jede Gruppe ist ein eigener, editierbarer Block
        foreach (var group in session.StrengthSets.GroupBy(x => x.ExerciseName))
        {
            if (!byName.TryGetValue(group.Key, out var exercise))
            {
                // Übung wurde aus der Bibliothek entfernt - Notfall-Eintrag, damit nichts verloren geht
                exercise = new Exercise { Name = group.Key, PrimaryMuscleGroup = "Sonstiges", InputType = ExerciseInputType.GewichtWiederholungen };
            }

            result.Add(new ExistingExerciseData
            {
                Exercise = exercise,
                Sets = group.OrderBy(x => x.SetNumber).Select(x => new SetInput(x.WeightKg, x.Reps)).ToList()
            });
        }

        // Cardio: jeder Eintrag ist bereits ein eigener, editierbarer Block
        foreach (var cardio in session.CardioEntries)
        {
            if (!byName.TryGetValue(cardio.ExerciseName, out var exercise))
            {
                exercise = new Exercise { Name = cardio.ExerciseName, PrimaryMuscleGroup = "Cardio", InputType = ExerciseInputType.StreckeZeit };
            }

            result.Add(new ExistingExerciseData
            {
                Exercise = exercise,
                DurationMinutes = cardio.DurationMinutes,
                DistanceKm = cardio.DistanceKm,
                ResistanceLevel = cardio.ResistanceLevel,
                InclinePercent = cardio.InclinePercent,
                Location = cardio.Location
            });
        }

        return (session.Name, result);
    }

    /// <summary>
    /// Ersetzt alle Übungen einer bestehenden Trainingseinheit durch die übergebene Liste (inkl. Name).
    /// So kann der Nutzer einzelne Übungen bearbeiten oder entfernen, statt nur die ganze Einheit zu löschen.
    /// </summary>
    public async Task ReplaceSessionExercisesAsync(int sessionId, string name, List<ExerciseInput> exercises)
    {
        await using var context = new AppDbContext();
        var session = await context.TrainingSessions
            .Include(s => s.StrengthSets)
            .Include(s => s.CardioEntries)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null) return;

        session.Name = name;

        context.StrengthSets.RemoveRange(session.StrengthSets);
        context.CardioEntries.RemoveRange(session.CardioEntries);
        session.StrengthSets.Clear();
        session.CardioEntries.Clear();

        AddExerciseInputsToSession(session, exercises);
        await context.SaveChangesAsync();
    }

    private static void AddExerciseInputsToSession(TrainingSession session, List<ExerciseInput> exercises)
    {
        foreach (var exercise in exercises)
        {
            if (exercise.InputType is ExerciseInputType.GewichtWiederholungen or ExerciseInputType.NurWiederholungen)
            {
                var setNumber = session.StrengthSets.Count(x => x.ExerciseName == exercise.ExerciseName) + 1;
                foreach (var set in exercise.Sets)
                {
                    session.StrengthSets.Add(new StrengthSetEntry
                    {
                        ExerciseName = exercise.ExerciseName,
                        SetNumber = setNumber++,
                        WeightKg = set.WeightKg,
                        Reps = set.Reps
                    });
                }
            }
            else
            {
                session.CardioEntries.Add(new CardioEntry
                {
                    ExerciseName = exercise.ExerciseName,
                    Type = MapToCardioType(exercise.ExerciseName),
                    Location = exercise.Location,
                    DurationMinutes = exercise.DurationMinutes ?? 0,
                    DistanceKm = exercise.DistanceKm,
                    ResistanceLevel = exercise.ResistanceLevel,
                    InclinePercent = exercise.InclinePercent
                });
            }
        }
    }

    public async Task RemoveSessionAsync(int sessionId)
    {
        await using var context = new AppDbContext();
        var session = await context.TrainingSessions.FindAsync(sessionId);
        if (session is not null)
        {
            context.TrainingSessions.Remove(session);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>Ermittelt, an welchen Tagen im Zeitraum bereits Trainingseinheiten existieren (für die Wochenpunkte).</summary>
    /// <summary>Cardio-Minuten pro Tag für einen Zeitraum - Grundlage für den Cardio-Unterreiter der Statistiken.</summary>
    public async Task<Dictionary<DateOnly, int>> GetDailyCardioMinutesForRangeAsync(int userProfileId, DateOnly start, DateOnly end)
    {
        await using var context = new AppDbContext();
        var sessions = await context.TrainingSessions
            .Include(s => s.CardioEntries)
            .Where(s => s.UserProfileId == userProfileId && s.Date >= start && s.Date <= end)
            .ToListAsync();

        var result = new Dictionary<DateOnly, int>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            result[date] = sessions.Where(s => s.Date == date).SelectMany(s => s.CardioEntries).Sum(c => c.DurationMinutes);
        }
        return result;
    }

    public async Task<HashSet<DateOnly>> GetDatesWithSessionsAsync(int userProfileId, DateOnly start, DateOnly end)
    {
        await using var context = new AppDbContext();
        var dates = await context.TrainingSessions
            .Where(s => s.UserProfileId == userProfileId && s.Date >= start && s.Date <= end)
            .Select(s => s.Date)
            .ToListAsync();
        return dates.ToHashSet();
    }

    /// <summary>Die zuletzt abgeschlossene Trainingseinheit vor dem angegebenen Datum (für "Verlauf der letzten Einheit").</summary>
    public async Task<TrainingSessionDisplay?> GetMostRecentSessionBeforeAsync(int userProfileId, DateOnly beforeDate, double bodyWeightKg)
    {
        await using var context = new AppDbContext();
        var lastDate = await context.TrainingSessions
            .Where(s => s.UserProfileId == userProfileId && s.Date < beforeDate)
            .OrderByDescending(s => s.Date)
            .Select(s => s.Date)
            .FirstOrDefaultAsync();

        if (lastDate == default) return null;

        var sessions = await GetSessionsForDateAsync(userProfileId, lastDate, bodyWeightKg);
        return sessions.FirstOrDefault();
    }

    /// <summary>Zählt die unterschiedlichen Trainingstage in der aktuellen Woche (für "3/5 Trainingstage").</summary>
    public async Task<int> CountTrainingDaysInWeekAsync(int userProfileId, DateOnly weekStart, DateOnly weekEnd)
    {
        var dates = await GetDatesWithSessionsAsync(userProfileId, weekStart, weekEnd);
        return dates.Count;
    }

    /// <summary>
    /// Zählt die aktuelle Trainings-Streak in Tagen: rückwärts ab heute (bzw. ab gestern, falls heute noch
    /// nichts erfasst ist) werden Trainingstage gezählt. Als Ruhetag markierte Tage unterbrechen die Streak
    /// nicht, zählen aber selbst auch nicht mit.
    /// </summary>
    public async Task<int> GetCurrentStreakDaysAsync(int userProfileId)
    {
        await using var context = new AppDbContext();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var trainingDates = await context.TrainingSessions
            .Where(s => s.UserProfileId == userProfileId)
            .Select(s => s.Date)
            .Distinct()
            .ToListAsync();
        var trainingSet = trainingDates.ToHashSet();

        var restDates = await context.RestDays
            .Where(r => r.UserProfileId == userProfileId)
            .Select(r => r.Date)
            .ToListAsync();
        var restSet = restDates.ToHashSet();

        var day = today;
        if (!trainingSet.Contains(day) && !restSet.Contains(day)) day = day.AddDays(-1);

        var streak = 0;
        while (trainingSet.Contains(day) || restSet.Contains(day))
        {
            if (trainingSet.Contains(day)) streak++;
            day = day.AddDays(-1);
        }

        return streak;
    }

    /// <summary>
    /// Streak in Tagen, an denen IRGENDETWAS getrackt wurde (Ernährung, Training oder bewusster Ruhetag) -
    /// im Gegensatz zu GetCurrentStreakDaysAsync, das nur reine Trainingstage zählt.
    /// </summary>
    public async Task<int> GetTrackingStreakDaysAsync(int userProfileId)
    {
        await using var context = new AppDbContext();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoff = today.AddDays(-365);

        var trainingDates = await context.TrainingSessions
            .Where(s => s.UserProfileId == userProfileId && s.Date >= cutoff)
            .Select(s => s.Date)
            .ToListAsync();

        var restDates = await context.RestDays
            .Where(r => r.UserProfileId == userProfileId && r.Date >= cutoff)
            .Select(r => r.Date)
            .ToListAsync();

        var nutritionDates = await context.NutritionEntries
            .Where(n => n.UserProfileId == userProfileId && n.Date >= cutoff)
            .Select(n => n.Date)
            .ToListAsync();

        var waterDates = await context.WaterEntries
            .Where(w => w.UserProfileId == userProfileId && w.Date >= cutoff)
            .Select(w => w.Date)
            .ToListAsync();

        var trackedSet = trainingDates.Concat(restDates).Concat(nutritionDates).Concat(waterDates).ToHashSet();

        var day = today;
        if (!trackedSet.Contains(day)) day = day.AddDays(-1);

        var streak = 0;
        while (trackedSet.Contains(day))
        {
            streak++;
            day = day.AddDays(-1);
        }

        return streak;
    }

    public async Task<List<Exercise>> SearchExercisesAsync(string query)
    {
        await using var context = new AppDbContext();
        if (string.IsNullOrWhiteSpace(query))
            return await context.Exercises.OrderBy(e => e.Name).Take(30).ToListAsync();

        var q = query.Trim().ToLower();
        var results = await context.Exercises
            .Where(e => e.Name.ToLower().Contains(q) || e.PrimaryMuscleGroup.ToLower().Contains(q))
            .OrderBy(e => e.Name)
            .Take(30)
            .ToListAsync();

        // Tippfehler-Korrektur: liefert die Teilstring-Suche nichts (z. B. "Banddrücken" statt
        // "Bankdrücken" eingetippt), den ähnlichsten bekannten Übungsnamen per Levenshtein-Distanz
        // vorschlagen, statt den Nutzer mit einer leeren Trefferliste dastehen zu lassen.
        if (results.Count == 0)
        {
            try
            {
                var allNames = await context.Exercises.Select(e => e.Name).ToListAsync();
                var closest = FuzzyMatch.FindClosest(query, allNames);
                if (closest is not null)
                {
                    var match = await context.Exercises.FirstOrDefaultAsync(e => e.Name == closest);
                    if (match is not null) results.Add(match);
                }
            }
            catch
            {
                // Fehler bei der Tippfehler-Korrektur darf die normale Suche nicht beeinträchtigen.
            }
        }

        return results;
    }

    /// <summary>Lädt mehrere Übungen anhand ihrer exakten Namen (z. B. um eine Trainingsvorlage in Übungs-Blöcke umzuwandeln).</summary>
    public async Task<List<Exercise>> GetExercisesByNamesAsync(List<string> names)
    {
        try
        {
            await using var context = new AppDbContext();
            var lowered = names.Select(n => n.ToLower()).ToHashSet();
            return await context.Exercises.Where(e => lowered.Contains(e.Name.ToLower())).ToListAsync();
        }
        catch
        {
            return new List<Exercise>();
        }
    }

    /// <summary>Speichert eine aus wger.de importierte Übung lokal - danach funktioniert sie wie jede selbst angelegte.</summary>
    public async Task<Exercise> SaveImportedExerciseAsync(Exercise exercise)
    {
        await using var context = new AppDbContext();

        if (exercise.WgerId is not null)
        {
            var existing = await context.Exercises.FirstOrDefaultAsync(e => e.WgerId == exercise.WgerId);
            if (existing is not null) return existing;
        }

        context.Exercises.Add(exercise);
        await context.SaveChangesAsync();
        return exercise;
    }

    // ---------------- Ruhetage ----------------

    public async Task<bool> IsRestDayAsync(int userProfileId, DateOnly date)
    {
        await using var context = new AppDbContext();
        return await context.RestDays.AnyAsync(r => r.UserProfileId == userProfileId && r.Date == date);
    }

    public async Task ToggleRestDayAsync(int userProfileId, DateOnly date)
    {
        await using var context = new AppDbContext();
        var existing = await context.RestDays.FirstOrDefaultAsync(r => r.UserProfileId == userProfileId && r.Date == date);
        if (existing is not null)
        {
            context.RestDays.Remove(existing);
        }
        else
        {
            context.RestDays.Add(new RestDay { UserProfileId = userProfileId, Date = date });
        }
        await context.SaveChangesAsync();
    }

    public async Task<HashSet<DateOnly>> GetRestDaysAsync(int userProfileId, DateOnly start, DateOnly end)
    {
        await using var context = new AppDbContext();
        var dates = await context.RestDays
            .Where(r => r.UserProfileId == userProfileId && r.Date >= start && r.Date <= end)
            .Select(r => r.Date)
            .ToListAsync();
        return dates.ToHashSet();
    }
}
