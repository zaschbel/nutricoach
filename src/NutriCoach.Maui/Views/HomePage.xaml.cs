namespace NutriCoach.Maui.Views;

public partial class HomePage : ContentView
{
    private CancellationTokenSource? _flameAnimationCts;

    public HomePage()
    {
        InitializeComponent();
        BindingContext = AppState.MainViewModel;
        Loaded += (_, _) => StartFlameAnimation();
        Unloaded += (_, _) => _flameAnimationCts?.Cancel();
    }

    /// <summary>
    /// Sanftes Pulsieren der Streak-Flamme, solange eine aktive Streak besteht - macht sie "lebendig"
    /// statt eines starren Icons. Läuft in einer eigenen Schleife statt einer festen Animation, damit
    /// sie automatisch pausiert/weiterläuft, wenn CurrentStreakDays sich ändert (z. B. Tageswechsel).
    /// </summary>
    private void StartFlameAnimation()
    {
        _flameAnimationCts?.Cancel();
        var cts = new CancellationTokenSource();
        _flameAnimationCts = cts;
        _ = AnimateFlameLoopAsync(cts.Token);
    }

    private async Task AnimateFlameLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (AppState.MainViewModel.CurrentStreakDays > 0)
                {
                    await StreakFlameIcon.ScaleTo(1.18, 420, Easing.SinInOut);
                    await StreakFlameIcon.ScaleTo(1.0, 420, Easing.SinInOut);
                }
                else
                {
                    await Task.Delay(500, token);
                }
            }
        }
        catch (TaskCanceledException)
        {
            // Seite geschlossen/verlassen, während die Animation lief - kein Fehler.
        }
    }

    private async void OnSettingsTapped(object? sender, EventArgs e) =>
        await Application.Current!.MainPage!.Navigation.PushModalAsync(new SettingsPage());

    private async void OnOpenSettingsClicked(object? sender, EventArgs e) =>
        await Application.Current!.MainPage!.Navigation.PushModalAsync(new SettingsPage());

    private void OnStepsCardTapped(object? sender, EventArgs e) => MainTabsPage.RequestTabChange(1);
    private void OnCaloriesCardTapped(object? sender, EventArgs e) => MainTabsPage.RequestTabChange(2);
}
