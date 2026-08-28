using NutriCoach.App.Models;
using NutriCoach.App.Services;

namespace NutriCoach.Maui.Views;

public partial class ErnaehrungPage : ContentView
{
    public ErnaehrungPage()
    {
        InitializeComponent();
        BindingContext = AppState.MainViewModel;
        AppState.MainViewModel.EntrySaved += () => _ = AnimateSuccessCheckmarkAsync();
    }

    private async void OnBreakfastHeaderTapped(object? sender, EventArgs e) =>
        await Application.Current!.MainPage!.Navigation.PushModalAsync(
            new MealDetailPage(MealType.Frühstück, "Frühstück", AppState.MainViewModel.Breakfast));

    private async void OnLunchHeaderTapped(object? sender, EventArgs e) =>
        await Application.Current!.MainPage!.Navigation.PushModalAsync(
            new MealDetailPage(MealType.Mittagessen, "Mittagessen", AppState.MainViewModel.Lunch));

    private async void OnDinnerHeaderTapped(object? sender, EventArgs e) =>
        await Application.Current!.MainPage!.Navigation.PushModalAsync(
            new MealDetailPage(MealType.Abendessen, "Abendessen", AppState.MainViewModel.Dinner));

    private async void OnSnackHeaderTapped(object? sender, EventArgs e) =>
        await Application.Current!.MainPage!.Navigation.PushModalAsync(
            new MealDetailPage(MealType.Snack, "Snack", AppState.MainViewModel.Snacks));

    private async void OnNutrientCardTapped(object? sender, EventArgs e)
    {
        if (sender is not Frame { BindingContext: NutrientCardDisplay nutrient }) return;

        var vm = AppState.MainViewModel;
        var todaysEntries = vm.Breakfast.Concat(vm.Lunch).Concat(vm.Dinner).Concat(vm.Snacks);
        await Application.Current!.MainPage!.Navigation.PushModalAsync(new NutrientDetailPage(nutrient, todaysEntries));
    }

    /// <summary>Kurzes Häkchen-Aufblitzen als Bestätigung, dass etwas gespeichert wurde (Essen/Wasser).</summary>
    private async Task AnimateSuccessCheckmarkAsync()
    {
        SuccessCheckmark.Opacity = 0;
        SuccessCheckmark.Scale = 0.6;
        await Task.WhenAll(
            SuccessCheckmark.FadeTo(1, 180, Easing.CubicOut),
            SuccessCheckmark.ScaleTo(1.1, 180, Easing.CubicOut));
        await Task.Delay(350);
        await Task.WhenAll(
            SuccessCheckmark.FadeTo(0, 220, Easing.CubicIn),
            SuccessCheckmark.ScaleTo(0.8, 220, Easing.CubicIn));
    }
}
