using System.ComponentModel;

namespace NutriCoach.Maui.Views;

public partial class TrainingPage : ContentView
{
    public TrainingPage()
    {
        InitializeComponent();
        BindingContext = AppState.MainViewModel;
        AppState.MainViewModel.EntrySaved += () => _ = AnimateSuccessCheckmarkAsync();

        // ProgressBar.ProgressTo() animiert offenbar gar nicht auf diesem Geraet (siehe HomePage.xaml.cs
        // fuer die vollstaendige Begruendung) - ersetzt durch zwei BoxViews, Fuellbalken per ScaleX/AnchorX.
        AppState.MainViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NutriCoach.App.ViewModels.MainViewModel.TrainingProgressRatioMaui))
                _ = WeeklyGoalFillBar.ScaleXTo(AppState.MainViewModel.TrainingProgressRatioMaui, 500, Easing.CubicOut);
        };

        Loaded += (_, _) =>
            _ = WeeklyGoalFillBar.ScaleXTo(AppState.MainViewModel.TrainingProgressRatioMaui, 500, Easing.CubicOut);
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
