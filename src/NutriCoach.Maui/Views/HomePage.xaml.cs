namespace NutriCoach.Maui.Views;

public partial class HomePage : ContentView
{
    public HomePage()
    {
        InitializeComponent();
        BindingContext = AppState.MainViewModel;
    }

    private async void OnSettingsTapped(object? sender, EventArgs e) =>
        await Application.Current!.MainPage!.Navigation.PushModalAsync(new SettingsPage());

    private async void OnOpenSettingsClicked(object? sender, EventArgs e) =>
        await Application.Current!.MainPage!.Navigation.PushModalAsync(new SettingsPage());

    private void OnStepsCardTapped(object? sender, EventArgs e) => MainTabsPage.RequestTabChange(1);
    private void OnCaloriesCardTapped(object? sender, EventArgs e) => MainTabsPage.RequestTabChange(2);
}
