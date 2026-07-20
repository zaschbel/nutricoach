using NutriCoach.Maui.ViewModels;

namespace NutriCoach.Maui.Views;

public partial class QuickSetupPage : ContentPage
{
    private readonly QuickSetupViewModel _viewModel;

    public QuickSetupPage()
    {
        InitializeComponent();
        _viewModel = new QuickSetupViewModel(AppState.ProfileService);
        _viewModel.Completed += async () =>
        {
            await AppState.MainViewModel.LoadAsync();
            Application.Current!.MainPage = new MainTabsPage();
        };
        BindingContext = _viewModel;
    }
}
