using NutriCoach.App.Models;

namespace NutriCoach.App.Services;

/// <summary>
/// Berechnet Grundumsatz (BMR) und Gesamtumsatz (TDEE) aus dem Profil.
/// Wird später durch echte Schrittzähler-Daten präzisiert (siehe StepCounterService, geplant).
/// </summary>
public static class BmrCalculator
{
    /// <summary>Grundumsatz nach Mifflin-St Jeor – Standardformel, gilt als genauer als Harris-Benedict.</summary>
    public static double CalculateBmr(UserProfile profile)
    {
        var weight = profile.CurrentWeightKg;
        var height = profile.HeightCm;
        var age = profile.Age;

        double bmr = profile.Gender switch
        {
            Gender.Männlich => 10 * weight + 6.25 * height - 5 * age + 5,
            Gender.Weiblich => 10 * weight + 6.25 * height - 5 * age - 161,
            // Bei "Divers" wird der Mittelwert beider Formeln verwendet, bis genauere
            // individuelle Messwerte (z. B. Körperfettanteil) vorliegen.
            _ => 10 * weight + 6.25 * height - 5 * age - 78
        };

        return Math.Round(bmr, 0);
    }

    /// <summary>PAL-Faktor aus dem selbst gewählten Aktivitätslevel.</summary>
    private static double ActivityFactor(ActivityLevel level) => level switch
    {
        ActivityLevel.Sitzend => 1.2,
        ActivityLevel.LeichtAktiv => 1.375,
        ActivityLevel.MäßigAktiv => 1.55,
        ActivityLevel.SehrAktiv => 1.725,
        ActivityLevel.ExtremAktiv => 1.9,
        _ => 1.375
    };

    /// <summary>
    /// Zusätzlicher Aufschlag für den Job-Alltag (NEAT), oben auf den PAL-Faktor,
    /// weil "Bürojob" vs. "Baustelle" bei gleichem selbstgewähltem Aktivitätslevel
    /// trotzdem sehr unterschiedliche Kalorienverbräuche bedeutet.
    /// </summary>
    private static double JobActivityBonus(DailyJobActivity job) => job switch
    {
        DailyJobActivity.ÜberwiegendSitzend => 0.0,
        DailyJobActivity.ÜberwiegendStehend => 0.05,
        DailyJobActivity.KörperlichAktiv => 0.12,
        DailyJobActivity.SehrKörperlichAnstrengend => 0.25,
        _ => 0.0
    };

    /// <summary>Gesamtumsatz (TDEE) = BMR * (PAL-Faktor + Job-Bonus).</summary>
    public static double CalculateTdee(UserProfile profile)
    {
        var bmr = CalculateBmr(profile);
        var factor = ActivityFactor(profile.ActivityLevel) + JobActivityBonus(profile.JobActivity);
        return Math.Round(bmr * factor, 0);
    }

    /// <summary>
    /// Kalorienziel basierend auf Fitnessziel und gewünschter Geschwindigkeit
    /// (aus TargetDate/TargetWeightKg abgeleitet, falls vorhanden).
    /// </summary>
    public static double CalculateCalorieTarget(UserProfile profile)
    {
        var tdee = CalculateTdee(profile);

        // Falls konkretes Zielgewicht + Datum vorhanden: Defizit/Überschuss daraus ableiten
        if (profile.TargetWeightKg.HasValue && profile.TargetDate.HasValue)
        {
            var weightDiffKg = profile.TargetWeightKg.Value - profile.CurrentWeightKg;
            var daysUntilTarget = (profile.TargetDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;

            if (daysUntilTarget > 0)
            {
                // 1 kg Körperfett ≈ 7700 kcal
                var totalKcalDiff = weightDiffKg * 7700;
                var dailyKcalDiff = totalKcalDiff / daysUntilTarget;

                // Sicherheitsgrenzen: nie mehr als ±1000 kcal/Tag vom TDEE abweichen
                dailyKcalDiff = Math.Clamp(dailyKcalDiff, -1000, 1000);
                return Math.Round(tdee + dailyKcalDiff, 0);
            }
        }

        // Fallback: Standard-Defizit/-Überschuss nach Ziel-Typ
        return profile.Goal switch
        {
            FitnessGoal.Abnehmen => Math.Round(tdee - 500, 0),
            FitnessGoal.MuskelAufbau => Math.Round(tdee + 300, 0),
            FitnessGoal.KraftSteigern => Math.Round(tdee + 200, 0),
            _ => tdee
        };
    }

    /// <summary>
    /// Persönliches Tages-Wasserziel in ml: Grundfaustregel ~33ml pro kg Körpergewicht,
    /// plus Aufschlag bei hoher Aktivität (mehr Flüssigkeitsverlust durch Schwitzen).
    /// </summary>
    public static int CalculateWaterTargetMl(UserProfile profile)
    {
        var baseMl = profile.CurrentWeightKg * 33;

        var activityBonus = profile.ActivityLevel switch
        {
            ActivityLevel.SehrAktiv => 400,
            ActivityLevel.ExtremAktiv => 700,
            ActivityLevel.MäßigAktiv => 200,
            _ => 0
        };

        var jobBonus = profile.JobActivity switch
        {
            DailyJobActivity.SehrKörperlichAnstrengend => 300,
            DailyJobActivity.KörperlichAktiv => 150,
            _ => 0
        };

        var total = baseMl + activityBonus + jobBonus;

        // Auf 100ml runden, das lässt sich am Schieberegler angenehmer einstellen
        return (int)(Math.Round(total / 100.0) * 100);
    }

    /// <summary>
    /// Makro-Ziele in Gramm, abgeleitet aus dem Kalorienziel und typischen Verteilungen je nach Ziel-Typ.
    /// (Protein/Kohlenhydrate = 4 kcal/g, Fett = 9 kcal/g)
    /// </summary>
    public static (double ProteinG, double CarbsG, double FatG) CalculateMacroTargets(UserProfile profile)
    {
        var kcalTarget = CalculateCalorieTarget(profile);

        var (proteinPct, carbsPct, fatPct) = profile.Goal switch
        {
            FitnessGoal.Abnehmen => (0.30, 0.40, 0.30),
            FitnessGoal.MuskelAufbau => (0.30, 0.45, 0.25),
            FitnessGoal.KraftSteigern => (0.28, 0.45, 0.27),
            FitnessGoal.AusdauerVerbessern => (0.20, 0.55, 0.25),
            _ => (0.25, 0.50, 0.25)
        };

        var proteinG = Math.Round(kcalTarget * proteinPct / 4, 0);
        var carbsG = Math.Round(kcalTarget * carbsPct / 4, 0);
        var fatG = Math.Round(kcalTarget * fatPct / 9, 0);

        return (proteinG, carbsG, fatG);
    }
}
