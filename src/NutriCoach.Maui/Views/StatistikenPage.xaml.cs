using NutriCoach.App.Services;
using NutriCoach.App.ViewModels;

namespace NutriCoach.Maui.Views;

public partial class StatistikenPage : ContentView
{
    private readonly StatistikenViewModel _viewModel;

    public StatistikenPage()
    {
        InitializeComponent();
        _viewModel = new StatistikenViewModel(AppState.ProfileService, new NutritionDiaryService(), new TrainingDiaryService());
        BindingContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.LoadAsync();
    }

    /// <summary>Wird beim Wechsel auf diesen Reiter erneut aufgerufen, damit z. B. gerade geändertes
    /// Gewicht oder neu eingetragenes Essen sofort in den Grafiken auftaucht, nicht erst nach Neustart.</summary>
    public async Task RefreshAsync() => await _viewModel.LoadAsync();
}
