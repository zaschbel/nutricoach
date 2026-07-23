using System.ComponentModel;

namespace NutriCoach.Maui.Views;

public partial class TrainingPage : ContentView
{
    private double _weeklyGoalBarWidth;

    public TrainingPage()
    {
        InitializeComponent();
        BindingContext = AppState.MainViewModel;
        AppState.MainViewModel.EntrySaved += () => _ = AnimateSuccessCheckmarkAsync();

        // Siebter Versuch fuer die Balken-Animation - siehe HomePage.xaml.cs fuer die vollstaendige
        // Begruendung (Maske per TranslateTo statt Scale/ProgressTo).
        WeeklyGoalBarTrack.SizeChanged += (_, _) =>
        {
            _weeklyGoalBarWidth = WeeklyGoalBarTrack.Width;
            UpdateWeeklyGoalBarMask(animate: false);
        };
        AppState.MainViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NutriCoach.App.ViewModels.MainViewModel.TrainingProgressRatioMaui))
                UpdateWeeklyGoalBarMask(animate: true);
        };
    }

    private void UpdateWeeklyGoalBarMask(bool animate)
    {
        if (_weeklyGoalBarWidth <= 0) return;
        var targetX = _weeklyGoalBarWidth * AppState.MainViewModel.TrainingProgressRatioMaui;
        if (animate) _ = WeeklyGoalBarMask.TranslateTo(targetX, 0, 500, Easing.CubicOut);
        else WeeklyGoalBarMask.TranslationX = targetX;
    }

    /// <summary>Kurzes Häkchen-Aufblitzen als Bestätigung, dass ein Training gespeichert wurde.</summary>
    private async Task AnimateSuccessCheckmarkAsync()
    {
        SuccessCheckmark.Opacity = 0;
        SuccessCheckmark.Scale = 0.6;
        await Task.WhenAll(
            SuccessCheckmark.FadeTo(1, 180, Easing.CubicOut),
            SuccessCheckmark.ScaleTo(1.1, 180, Easing.CubicOut));
        await Task.Delay(350);
        await Task.WhenAll(
            SuccessCheckmark.FadeTo(0, 220, Easing.CubicIn),
            SuccessCheckmark.ScaleTo(0.8, 220, Easing.CubicIn));
    }
}
