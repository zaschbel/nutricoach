using NutriCoach.App.ViewModels;

namespace NutriCoach.Maui.Views;

public partial class FoodDetailPage : ContentPage
{
    public FoodDetailViewModel ViewModel { get; }
    public event Action? CancelRequested;

    public FoodDetailPage(FoodDetailViewModel viewModel)
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
