namespace NutriCoach.Maui.Services;

/// <summary>
/// Platzhalter für Android - Apple Health gibt's dort naturgemäß nicht (Google Health Connect
/// wäre das Android-Äquivalent, aber eine eigene, separate Anbindung, die wir hier nicht bauen).
/// Die App fällt automatisch auf die manuelle Eingabe zurück.
/// </summary>
public class HealthDataService : IHealthDataService
{
    public bool IsSupported => false;
    public Task<bool> RequestAuthorizationAsync() => Task.FromResult(false);
    public Task<int?> GetStepsForDateAsync(DateOnly date) => Task.FromResult<int?>(null);
}
