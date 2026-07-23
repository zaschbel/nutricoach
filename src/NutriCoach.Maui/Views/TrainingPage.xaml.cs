using System.ComponentModel;

namespace NutriCoach.Maui.Views;

public partial class TrainingPage : ContentView
{
    public TrainingPage()
    {
        InitializeComponent();
        BindingContext = AppState.MainViewModel;
        AppState.MainViewModel.EntrySaved += () => _ = AnimateSuccessCheckmarkAsync();

        // Direkt per PropertyChanged angesteuert statt per XAML-Behavior (siehe HomePage.xaml.cs
        // für die Begründung - das Behavior hat trotz zweier Fixversuche nicht zuverlässig funktioniert).
        AppState.MainViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NutriCoach.App.ViewModels.MainViewModel.TrainingProgressRatioMaui))
                _ = WeeklyGoalProgressBar.ProgressTo(AppState.MainViewModel.TrainingProgressRatioMaui, 500, Easing.CubicOut);
        };
        _ = WeeklyGoalProgressBar.ProgressTo(AppState.MainViewModel.TrainingProgressRatioMaui, 500, Easing.CubicOut);
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
