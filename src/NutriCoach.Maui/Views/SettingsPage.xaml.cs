using NutriCoach.App.ViewModels;

namespace NutriCoach.Maui.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = new SettingsViewModel();
    }

    private async void OnCloseTapped(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        await AppState.MainViewModel.CheckAiApiKeyAsync();
    }

    private async void OnEditProfileClicked(object? sender, EventArgs e) =>
        await Navigation.PushModalAsync(new EditProfilePage());

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Opacity = 0;
        await this.FadeTo(1, 220, Easing.CubicOut);
    }
}
