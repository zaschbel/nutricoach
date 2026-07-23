using System.ComponentModel;

namespace NutriCoach.Maui.Views;

public partial class HomePage : ContentView
{
    private CancellationTokenSource? _flameAnimationCts;
    private double _stepsBarWidth;
    private double _caloriesBarWidth;

    public HomePage()
    {
        InitializeComponent();
        BindingContext = AppState.MainViewModel;
        Loaded += (_, _) => StartFlameAnimation();
        Unloaded += (_, _) => _flameAnimationCts?.Cancel();

        // Siebter Versuch fuer die Balken-Animation (siehe Commit-Historie): ProgressBar.ProgressTo
        // und ScaleXTo auf einem BoxView haben beide nicht sichtbar animiert bzw. das Element sogar
        // unsichtbar gemacht. Neuer Ansatz: eine Maske (gleiche Farbe wie der Hintergrund) liegt ueber
        // einem voll eingefaerbten Balken und wird per TranslateTo nach rechts verschoben, um den
        // Balken von links nach rechts "freizulegen" - TranslateTo ist derselbe Animationsmechanismus
        // (Transform-Animation), der bei der Flamme und dem Erfolgs-Haekchen bereits bestaetigt
        // funktioniert (ScaleTo/FadeTo), nur mit Verschieben statt Skalieren. Ausserdem wird die
        // Balkenbreite ueber SizeChanged erfasst statt ueber Loaded anzunehmen, wann die Ansicht bereit ist.
        StepsBarTrack.SizeChanged += (_, _) =>
        {
            _stepsBarWidth = StepsBarTrack.Width;
            UpdateStepsBarMask(animate: false);
        };
        CaloriesBarTrack.SizeChanged += (_, _) =>
        {
            _caloriesBarWidth = CaloriesBarTrack.Width;
            UpdateCaloriesBarMask(animate: false);
        };
        AppState.MainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;
    }

    private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NutriCoach.App.ViewModels.MainViewModel.StepsProgressRatio))
            UpdateStepsBarMask(animate: true);
        else if (e.PropertyName == nameof(NutriCoach.App.ViewModels.MainViewModel.CalorieProgressRatio))
            UpdateCaloriesBarMask(animate: true);
    }

    private void UpdateStepsBarMask(bool animate)
    {
        if (_stepsBarWidth <= 0) return;
        var targetX = _stepsBarWidth * AppState.MainViewModel.StepsProgressRatio;
        if (animate) _ = StepsBarMask.TranslateTo(targetX, 0, 500, Easing.CubicOut);
        else StepsBarMask.TranslationX = targetX;
    }

    private void UpdateCaloriesBarMask(bool animate)
    {
        if (_caloriesBarWidth <= 0) return;
        var targetX = _caloriesBarWidth * AppState.MainViewModel.CalorieProgressRatio;
        if (animate) _ = CaloriesBarMask.TranslateTo(targetX, 0, 500, Easing.CubicOut);
        else CaloriesBarMask.TranslationX = targetX;
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
