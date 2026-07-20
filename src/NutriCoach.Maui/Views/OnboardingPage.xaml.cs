using NutriCoach.App.ViewModels;

namespace NutriCoach.Maui.Views;

public partial class OnboardingPage : ContentPage
{
    private readonly OnboardingViewModel _viewModel;

    public OnboardingPage()
    {
        InitializeComponent();
        _viewModel = new OnboardingViewModel(AppState.ProfileService);
        BindingContext = _viewModel;
        _viewModel.OnboardingCompleted += async () =>
        {
            await AppState.MainViewModel.LoadAsync();
            Application.Current!.MainPage = new MainTabsPage();
        };
    }

    private void OnNoInjuriesClicked(object? sender, EventArgs e) => _viewModel.HasNoInjuries = true;
    private void OnHasInjuriesClicked(object? sender, EventArgs e) => _viewModel.HasInjuries = true;
}
