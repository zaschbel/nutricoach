using System.Collections.ObjectModel;
using NutriCoach.App.Models;
using NutriCoach.App.Services;
using NutriCoach.App.ViewModels;

namespace NutriCoach.Maui.Views;

public partial class MealDetailPage : ContentPage
{
    public MealDetailPage(MealType meal, string title, ObservableCollection<NutritionEntryDisplay> entries)
    {
        InitializeComponent();
        BindingContext = new MealDetailViewModel(meal, title, entries);
    }

    private async void OnBackTapped(object? sender, EventArgs e) =>
        await Navigation.PopModalAsync();
}
