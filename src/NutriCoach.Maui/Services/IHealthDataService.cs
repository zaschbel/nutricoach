namespace NutriCoach.Maui.Services;

/// <summary>
/// Zugriff auf die Schrittzahl aus der Gesundheits-App des Geräts. Nur auf iOS gibt es eine
/// echte Umsetzung (Apple Health / HealthKit) - unter Android und Windows liefert die
/// Platzhalter-Version einfach "nicht unterstützt", die App fällt dann automatisch auf die
/// bisherige manuelle Eingabe per Regler zurück.
/// </summary>
public interface IHealthDataService
{
    bool IsSupported { get; }
    Task<bool> RequestAuthorizationAsync();
    Task<int?> GetStepsForDateAsync(DateOnly date);
}
