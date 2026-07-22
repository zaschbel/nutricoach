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
    /// Unregelmäßiges Flackern der Streak-Flamme (Größe/Drehung/Transparenz zufällig, kurze
    /// unterschiedliche Dauer je Schritt), solange eine aktive Streak besteht - soll wie echtes
    /// Feuer wirken statt wie ein mechanisches, gleichmäßiges Pulsieren. Der Frame drumherum hat
    /// jetzt eine feste HeightRequest/MinimumWidthRequest (siehe HomePage.xaml), kann also nicht
    /// mehr wie beim letzten Versuch auf Null zusammenfallen, unabhängig davon was hier animiert wird.
    /// </summary>
    private void StartFlameAnimation()
    {
        _flameAnimationCts?.Cancel();
        var cts = new CancellationTokenSource();
        _flameAnimationCts = cts;
        _ = FlickerFlameLoopAsync(cts.Token);
    }

    private async Task FlickerFlameLoopAsync(CancellationToken token)
    {
        var random = new Random();
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (AppState.MainViewModel.CurrentStreakDays > 0)
                {
                    var scale = 0.92 + random.NextDouble() * 0.28;
                    var rotation = (random.NextDouble() - 0.5) * 10;
                    var opacity = 0.82 + random.NextDouble() * 0.18;
                    var duration = (uint)(90 + random.Next(120));

                    await Task.WhenAll(
                        StreakFlameIcon.ScaleTo(scale, duration, Easing.SinInOut),
                        StreakFlameIcon.RotateTo(rotation, duration, Easing.SinInOut),
                        StreakFlameIcon.FadeTo(opacity, duration, Easing.SinInOut));
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
