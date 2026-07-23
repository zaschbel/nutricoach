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

    private void OnPageTapped(object? sender, TappedEventArgs e) => DismissKeyboard(this);

    private static void DismissKeyboard(Element element)
    {
        if (element is Entry { IsFocused: true } entry)
        {
            entry.Unfocus();
            return;
        }

        foreach (var child in element.LogicalChildren)
        {
            if (child is Element childElement)
                DismissKeyboard(childElement);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Opacity = 0;
        await this.FadeTo(1, 220, Easing.CubicOut);
    }
}
