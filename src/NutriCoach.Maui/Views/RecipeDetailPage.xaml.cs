using NutriCoach.App.ViewModels;

namespace NutriCoach.Maui.Views;

public partial class RecipeDetailPage : ContentPage
{
    public RecipeDetailViewModel ViewModel { get; }
    public event Action? CancelRequested;

    public RecipeDetailPage(RecipeDetailViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        BindingContext = viewModel;
    }

    private void OnCancelTapped(object? sender, EventArgs e) => CancelRequested?.Invoke();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Opacity = 0;
        await this.FadeTo(1, 220, Easing.CubicOut);
    }
}
