using CoreMotion;
using Foundation;

namespace NutriCoach.Maui.Services;

/// <summary>
/// Schrittzahl über Core Motion (CMPedometer) statt HealthKit: HealthKit braucht ein
/// kostenpflichtiges Apple-Developer-Konto für das entsprechende Entitlement, Core Motion
/// dagegen funktioniert auch mit einer kostenlosen Apple-ID (Personal Team) - es braucht nur den
/// NSMotionUsageDescription-Eintrag im Info.plist, sonst keine besonderen Rechte.
/// </summary>
public class HealthDataService : IHealthDataService
{
    private readonly CMPedometer _pedometer = new();

    public bool IsSupported => CMPedometer.IsStepCountingAvailable;

    public Task<bool> RequestAuthorizationAsync()
    {
        if (!IsSupported) return Task.FromResult(false);

        // CMPedometer hat keine explizite "Request"-API wie HealthKit - die iOS-Berechtigungsabfrage
        // erscheint automatisch beim ersten QueryPedometerData-Aufruf. Eine kurze Testabfrage stößt
        // also gleichzeitig den Systemdialog an und liefert das Ergebnis (erlaubt/abgelehnt).
        var tcs = new TaskCompletionSource<bool>();
        var now = NSDate.Now;
        var from = now.AddSeconds(-60);
        _pedometer.QueryPedometerData(from, now, (data, error) => tcs.TrySetResult(error is null));
        return tcs.Task;
    }

    public Task<int?> GetStepsForDateAsync(DateOnly date)
    {
        if (!IsSupported) return Task.FromResult<int?>(null);

        try
        {
            // DateOnly.ToDateTime() liefert Kind=Unspecified - der explizite NSDate-Cast wirft dafür
            // eine ArgumentException ("must be Utc or Local"), unabgefangen direkt bei jedem
            // Automatik-Sync beim App-Start, was die ganze App zum Absturz bringt. Deshalb Kind explizit
            // auf Local setzen, bevor irgendetwas nach NSDate konvertiert wird.
            var startOfDay = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Local);
            var now = DateTime.Now;
            if (startOfDay > now) return Task.FromResult<int?>(null);

            var endExclusive = startOfDay.AddDays(1);
            var end = endExclusive < now ? endExclusive : now;

            var tcs = new TaskCompletionSource<int?>();
            _pedometer.QueryPedometerData((NSDate)startOfDay, (NSDate)end, (data, error) =>
            {
                if (error is not null || data is null) tcs.TrySetResult(null);
                else tcs.TrySetResult(data.NumberOfSteps.Int32Value);
            });
            return tcs.Task;
        }
        catch
        {
            // Dieser Aufruf läuft automatisch bei jedem App-Start, sobald einmal verbunden wurde -
            // ein Fehler hier darf niemals die ganze App abstürzen lassen (Absturzschleife!).
            return Task.FromResult<int?>(null);
        }
    }
}
