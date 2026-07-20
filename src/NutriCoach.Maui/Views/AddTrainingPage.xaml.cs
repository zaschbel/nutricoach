using NutriCoach.App.ViewModels;

namespace NutriCoach.Maui.Views;

public partial class AddTrainingPage : ContentPage
{
    private readonly AddTrainingViewModel _viewModel;
    public event Action? CancelRequested;

    public AddTrainingPage(AddTrainingViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        Loaded += async (_, _) => await _viewModel.LoadExistingExercisesAsync();
    }

    private void OnCancelTapped(object? sender, EventArgs e) => CancelRequested?.Invoke();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Opacity = 0;
        await this.FadeTo(1, 220, Easing.CubicOut);
    }
}
