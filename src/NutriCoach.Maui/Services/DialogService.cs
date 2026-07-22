using NutriCoach.App.Models;
using NutriCoach.App.Services;
using NutriCoach.App.ViewModels;

namespace NutriCoach.Maui.Services;

/// <summary>
/// Ersetzt WPFs "new Views.XyzWindow(...).ShowDialog()" durch MAUI-taugliche modale Navigation:
/// Seite wird auf den Navigations-Stapel gelegt, das Ergebnis kommt über eine
/// TaskCompletionSource zurück, sobald der Nutzer fertig ist (bestätigt/abgebrochen).
/// </summary>
public class DialogService : IDialogService
{
    private static INavigation Navigation => Application.Current!.MainPage!.Navigation;

    public async Task<FoodDetailDialogResult> ShowFoodDetailAsync(FoodItem food, FitnessGoal goal,
        double? initialAmountGrams = null, bool isEditMode = false)
    {
        var viewModel = new FoodDetailViewModel(food, goal, initialAmountGrams, isEditMode);
        var page = new Maui.Views.FoodDetailPage(viewModel);
        var tcs = new TaskCompletionSource<FoodDetailDialogResult>();

        viewModel.Confirmed += (confirmedFood, amount) => tcs.TrySetResult(new FoodDetailDialogResult(true, confirmedFood, amount, false));
        viewModel.DeleteRequested += () => tcs.TrySetResult(new FoodDetailDialogResult(true, food, 0, true));
        page.CancelRequested += () => tcs.TrySetResult(new FoodDetailDialogResult(false, null, 0, false));

        await Navigation.PushModalAsync(page);
        var result = await tcs.Task;
        await Navigation.PopModalAsync();
        return result;
    }

    public async Task<AddFoodDialogResult> ShowAddFoodAsync(FoodLookupService lookupService, NutritionDiaryService diaryService,
        int userProfileId, MealType meal, FitnessGoal goal)
    {
        var viewModel = new AddFoodViewModel(lookupService, diaryService, this, userProfileId, meal, goal);
        var page = new Maui.Views.AddFoodPage(viewModel);
        var tcs = new TaskCompletionSource<AddFoodDialogResult>();

        viewModel.FoodConfirmed += (food, amount) => tcs.TrySetResult(new AddFoodDialogResult(true, food, amount));
        page.CancelRequested += () => tcs.TrySetResult(new AddFoodDialogResult(false, null, 0));

        await Navigation.PushModalAsync(page);
        var result = await tcs.Task;
        await Navigation.PopModalAsync();
        return result;
    }

    public async Task<bool> ShowAddTrainingAsync(TrainingDiaryService trainingService, int userProfileId, DateOnly date,
        int? existingSessionId = null, string? existingSessionName = null, string? suggestedName = null,
        List<string>? prefilledExerciseNames = null)
    {
        var viewModel = new AddTrainingViewModel(trainingService, userProfileId, date, existingSessionId, existingSessionName, suggestedName, prefilledExerciseNames);
        var page = new Maui.Views.AddTrainingPage(viewModel);
        var tcs = new TaskCompletionSource<bool>();

        viewModel.Saved += () => tcs.TrySetResult(true);
        page.CancelRequested += () => tcs.TrySetResult(false);

        await Navigation.PushModalAsync(page);
        var result = await tcs.Task;
        await Navigation.PopModalAsync();
        return result;
    }

    public async Task<List<string>?> ShowManageTemplatesAsync(WorkoutTemplateService templateService, TrainingDiaryService trainingService, int userProfileId)
    {
        var viewModel = new ManageTemplatesViewModel(templateService, trainingService, userProfileId);
        var page = new Maui.Views.ManageTemplatesPage(viewModel);
        var tcs = new TaskCompletionSource<List<string>?>();

        viewModel.TemplateSelected += names => tcs.TrySetResult(names);
        page.CancelRequested += () => tcs.TrySetResult(null);

        await Navigation.PushModalAsync(page);
        var result = await tcs.Task;
        await Navigation.PopModalAsync();
        return result;
    }

    public async Task<bool> ShowWeeklyPlanAsync(TrainingPlanService planService, int userProfileId, Dictionary<DayOfWeek, string> currentPlan)
    {
        var viewModel = new WeeklyPlanViewModel(planService, userProfileId, currentPlan);
        var page = new Maui.Views.WeeklyPlanPage(viewModel);
        var tcs = new TaskCompletionSource<bool>();

        viewModel.Saved += () => tcs.TrySetResult(true);
        page.CancelRequested += () => tcs.TrySetResult(false);

        await Navigation.PushModalAsync(page);
        var result = await tcs.Task;
        await Navigation.PopModalAsync();
        return result;
    }

    public async Task ShowRecipesAsync(RecipeLookupService recipeService, RecipeFavoritesService favoritesService)
    {
        var viewModel = new RecipesViewModel(recipeService, favoritesService, this);
        var page = new Maui.Views.RecipesPage(viewModel);
        var tcs = new TaskCompletionSource();

        page.CancelRequested += () => tcs.TrySetResult();

        await Navigation.PushModalAsync(page);
        await tcs.Task;
        await Navigation.PopModalAsync();
    }

    public async Task ShowRecipeDetailAsync(Recipe recipe, RecipeFavoritesService favoritesService)
    {
        var viewModel = new RecipeDetailViewModel(recipe, favoritesService);
        var page = new Maui.Views.RecipeDetailPage(viewModel);
        var tcs = new TaskCompletionSource();

        page.CancelRequested += () => tcs.TrySetResult();

        await Navigation.PushModalAsync(page);
        await tcs.Task;
        await Navigation.PopModalAsync();
    }
}
