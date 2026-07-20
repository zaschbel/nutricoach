using Plugin.LocalNotification;
using Microsoft.Maui.Storage;

namespace NutriCoach.Maui.Services;

/// <summary>
/// Verwaltet tägliche Erinnerungen (Mahlzeiten, Wasser, Training) über lokale Push-Benachrichtigungen.
/// Feste Uhrzeiten pro Erinnerungsart, jede einzeln an-/abschaltbar. Der Ein/Aus-Zustand wird lokal
/// gespeichert (Preferences), damit die Einstellung den App-Neustart übersteht.
/// </summary>
public class ReminderService
{
    // Feste IDs, damit sich bestehende Erinnerungen beim erneuten Planen sauber überschreiben/löschen lassen.
    private const int BreakfastId = 9001;
    private const int LunchId = 9002;
    private const int DinnerId = 9003;
    private const int WaterId = 9004;
    private const int TrainingId = 9005;

    public async Task<bool> RequestPermissionAsync()
    {
        if (await LocalNotificationCenter.Current.AreNotificationsEnabled()) return true;
        return await LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    public bool IsEnabled(string key) => Preferences.Default.Get($"reminder_{key}", false);

    public async Task SetReminderAsync(string key, int notificationId, string title, string description, int hour, int minute, bool enabled)
    {
        Preferences.Default.Set($"reminder_{key}", enabled);

        if (!enabled)
        {
            LocalNotificationCenter.Current.Cancel(notificationId);
            return;
        }

        var granted = await RequestPermissionAsync();
        if (!granted) return;

        var now = DateTime.Now;
        var notifyTime = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
        if (notifyTime < now) notifyTime = notifyTime.AddDays(1);

        var request = new NotificationRequest
        {
            NotificationId = notificationId,
            Title = title,
            Description = description,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = notifyTime,
                NotifyRepeatInterval = TimeSpan.FromDays(1)
            }
        };

        await LocalNotificationCenter.Current.Show(request);
    }

    public Task SetBreakfastReminderAsync(bool enabled) =>
        SetReminderAsync("breakfast", BreakfastId, "🍳 Frühstück eingetragen?", "Vergiss nicht, dein Frühstück in NutriCoach einzutragen.", 9, 0, enabled);

    public Task SetLunchReminderAsync(bool enabled) =>
        SetReminderAsync("lunch", LunchId, "🥗 Mittagessen eingetragen?", "Zeit, dein Mittagessen einzutragen.", 13, 30, enabled);

    public Task SetDinnerReminderAsync(bool enabled) =>
        SetReminderAsync("dinner", DinnerId, "🍽️ Abendessen eingetragen?", "Nicht vergessen: dein Abendessen in NutriCoach eintragen.", 19, 30, enabled);

    public Task SetWaterReminderAsync(bool enabled) =>
        SetReminderAsync("water", WaterId, "💧 Zeit zu trinken", "Wie sieht's mit deinem Wasserziel für heute aus?", 15, 0, enabled);

    public Task SetTrainingReminderAsync(bool enabled) =>
        SetReminderAsync("training", TrainingId, "🏋️ Training heute?", "Schau kurz in deinen Trainingsplan für heute.", 18, 0, enabled);
}
