using NutriCoach.App.ViewModels;

namespace NutriCoach.Maui.Views;

public partial class EditProfilePage : ContentPage
{
    public EditProfilePage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            var profile = await AppState.ProfileService.GetActiveProfileAsync();
            if (profile is null) return;

            var viewModel = new EditProfileViewModel(AppState.ProfileService, profile);
            viewModel.Saved += async () => await AppState.MainViewModel.LoadAsync();
            BindingContext = viewModel;
        };
    }

    private async void OnCloseTapped(object? sender, EventArgs e) => await Navigation.PopModalAsync();

    private async void OnChangePictureTapped(object? sender, EventArgs e)
    {
        if (BindingContext is not EditProfileViewModel viewModel) return;

        var action = await DisplayActionSheet("Profilbild ändern", "Abbrechen", null, "Foto aufnehmen", "Aus Galerie wählen");
        FileResult? photo = action switch
        {
            "Foto aufnehmen" => MediaPicker.Default.IsCaptureSupported ? await MediaPicker.Default.CapturePhotoAsync() : null,
            "Aus Galerie wählen" => await MediaPicker.Default.PickPhotoAsync(),
            _ => null
        };

        if (photo is null) return;

        using var stream = await photo.OpenReadAsync();
        await viewModel.SetProfilePictureAsync(stream);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Opacity = 0;
        await this.FadeTo(1, 220, Easing.CubicOut);
    }
}
