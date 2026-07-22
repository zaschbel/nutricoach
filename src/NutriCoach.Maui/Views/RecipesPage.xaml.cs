using NutriCoach.App.ViewModels;

namespace NutriCoach.Maui.Views;

public partial class RecipesPage : ContentPage
{
    private readonly RecipesViewModel _viewModel;
    public event Action? CancelRequested;

    public RecipesPage(RecipesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        Loaded += async (_, _) =>
        {
            try
            {
                await _viewModel.LoadSuggestedRecipesAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Vorschläge konnten nicht geladen werden", ex.ToString(), "OK");
            }
        };
    }

    private void OnCancelTapped(object? sender, EventArgs e) => CancelRequested?.Invoke();
    private void OnModeSearch(object? sender, EventArgs e) => _viewModel.Mode = "Suche";
    private void OnModeFavoriten(object? sender, EventArgs e) => _viewModel.Mode = "Favoriten";

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Opacity = 0;
        await this.FadeTo(1, 220, Easing.CubicOut);
    }
}
