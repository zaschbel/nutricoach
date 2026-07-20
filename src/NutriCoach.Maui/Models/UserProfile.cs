namespace NutriCoach.App.Models;

/// <summary>
/// Das "Gedächtnis" der App. Wird einmal beim Onboarding befüllt und danach nur noch
/// ergänzt/aktualisiert (z. B. neues Gewicht) — der Nutzer muss Basisdaten nie erneut eingeben.
/// </summary>
public class UserProfile
{
    public int Id { get; set; }

    // --- Stammdaten ---
    public string Name { get; set; } = string.Empty;

    /// <summary>Pfad zum lokal gespeicherten Profilbild, falls eins gewählt wurde (relativ zum App-Datenordner).</summary>
    public string? ProfilePicturePath { get; set; }
    public DateOnly BirthDate { get; set; }
    public Gender Gender { get; set; }
    public double HeightCm { get; set; }

    // --- Ziel & Zeitrahmen ---
    public FitnessGoal Goal { get; set; }
    public DateOnly? TargetDate { get; set; }          // "wie schnell willst du dein Ziel erreichen"
    public double? TargetWeightKg { get; set; }

    // --- Erfahrung & Historie ---
    public ExperienceLevel Experience { get; set; }
    public DateOnly? TrainingSince { get; set; }        // "seit wann trainierst du"
    public RecentTrend RecentTrend { get; set; }
    public string? OtherActivities { get; set; }         // z. B. "spiele Fußball, 1x/Woche"

    // --- Alltag & Aktivität ---
    public DailyJobActivity JobActivity { get; set; }
    public ActivityLevel ActivityLevel { get; set; }

    /// <summary>Wie viele Tage pro Woche möchte die Person trainieren (0 = noch nicht festgelegt).</summary>
    public int WeeklyTrainingGoalDays { get; set; }

    // --- Aktueller Zustand ---
    public double CurrentWeightKg { get; set; }

    // --- Onboarding-Status ---
    public bool OnboardingCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // --- Navigationseigenschaften ---
    public List<InjuryRecord> Injuries { get; set; } = new();
    public List<BodyMeasurement> BodyMeasurements { get; set; } = new();
    public List<NutritionEntry> NutritionEntries { get; set; } = new();
    public List<TrainingSession> TrainingSessions { get; set; } = new();

    /// <summary>Alter wird immer live berechnet statt gespeichert – bleibt automatisch korrekt.</summary>
    public int Age
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - BirthDate.Year;
            if (BirthDate > today.AddYears(-age)) age--;
            return age;
        }
    }

    /// <summary>BMI wird live aus aktuellem Gewicht/Größe berechnet – kein doppelt gepflegter Wert.</summary>
    public double Bmi => HeightCm <= 0 ? 0 : CurrentWeightKg / Math.Pow(HeightCm / 100.0, 2);
}
