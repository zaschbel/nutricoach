using NutriCoach.App.ViewModels;

namespace NutriCoach.Maui.Views;

public partial class ManageTemplatesPage : ContentPage
{
    private readonly ManageTemplatesViewModel _viewModel;
    public event Action? CancelRequested;

    public ManageTemplatesPage(ManageTemplatesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        Loaded += async (_, _) => await _viewModel.LoadAsync();
    }

    private void OnCloseTapped(object? sender, EventArgs e) => CancelRequested?.Invoke();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Opacity = 0;
        await this.FadeTo(1, 220, Easing.CubicOut);
    }
}
