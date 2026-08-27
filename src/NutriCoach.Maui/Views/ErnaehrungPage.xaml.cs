namespace NutriCoach.Maui.Views;

public partial class ErnaehrungPage : ContentView
{
    public ErnaehrungPage()
    {
        InitializeComponent();
        BindingContext = AppState.MainViewModel;
        AppState.MainViewModel.EntrySaved += () => _ = AnimateSuccessCheckmarkAsync();
    }

    private async void OnOptimizeCalorieCalculationTapped(object? sender, EventArgs e) =>
        await Application.Current!.MainPage!.Navigation.PushModalAsync(new SettingsPage());

    /// <summary>Kurzes Häkchen-Aufblitzen als Bestätigung, dass etwas gespeichert wurde (Essen/Wasser).</summary>
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
