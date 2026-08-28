using NutriCoach.App.Services;
using NutriCoach.App.ViewModels;

namespace NutriCoach.Maui.Views;

public partial class NutrientDetailPage : ContentPage
{
    public NutrientDetailPage(NutrientCardDisplay nutrient, IEnumerable<NutritionEntryDisplay> todaysEntries)
    {
        InitializeComponent();
        BindingContext = new NutrientDetailViewModel(nutrient, todaysEntries);
    }

    private async void OnCloseTapped(object? sender, EventArgs e) =>
        await Navigation.PopModalAsync();
}
