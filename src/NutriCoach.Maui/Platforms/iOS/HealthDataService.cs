namespace NutriCoach.Maui.Services;

/// <summary>
/// Platzhalter für iOS ohne Apple Developer Program: HealthKit-Entitlement kann mit einer
/// kostenlosen Apple-ID (Personal Team) nicht signiert werden, daher fällt die App auf die
/// manuelle Eingabe zurück, genau wie unter Android/Windows. Sobald ein zahlendes Developer-Konto
/// vorhanden ist, kann diese Datei durch die echte HealthKit-Anbindung ersetzt und das
/// com.apple.developer.healthkit-Entitlement in Platforms/iOS/Entitlements.plist wieder aktiviert werden.
/// </summary>
public class HealthDataService : IHealthDataService
{
    public bool IsSupported => false;
    public Task<bool> RequestAuthorizationAsync() => Task.FromResult(false);
    public Task<int?> GetStepsForDateAsync(DateOnly date) => Task.FromResult<int?>(null);
}
