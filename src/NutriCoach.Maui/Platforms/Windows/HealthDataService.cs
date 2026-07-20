namespace NutriCoach.Maui.Services;

/// <summary>Platzhalter für Windows - kein Apple Health verfügbar, App fällt auf manuelle Eingabe zurück.</summary>
public class HealthDataService : IHealthDataService
{
    public bool IsSupported => false;
    public Task<bool> RequestAuthorizationAsync() => Task.FromResult(false);
    public Task<int?> GetStepsForDateAsync(DateOnly date) => Task.FromResult<int?>(null);
}
