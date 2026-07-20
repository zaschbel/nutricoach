using NutriCoach.App.ViewModels;

namespace NutriCoach.Maui.Views;

public partial class WeeklyPlanPage : ContentPage
{
    private readonly WeeklyPlanViewModel _viewModel;
    public event Action? CancelRequested;

    public WeeklyPlanPage(WeeklyPlanViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private void OnCancelClicked(object? sender, EventArgs e) => CancelRequested?.Invoke();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Opacity = 0;
        await this.FadeTo(1, 220, Easing.CubicOut);
    }
}
