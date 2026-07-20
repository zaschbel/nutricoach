using NutriCoach.App.Models;
using NutriCoach.App.Services;

namespace NutriCoach.Maui.Services;

public record AddFoodDialogResult(bool Confirmed, FoodItem? Food, double AmountGrams);
public record FoodDetailDialogResult(bool Confirmed, FoodItem? Food, double AmountGrams, bool WasDeleted);

/// <summary>
/// Ersetzt das WPF-Muster "new Views.XyzWindow(...).ShowDialog()" durch etwas, das unter MAUI
/// funktioniert: Seiten werden modal auf den Navigations-Stapel gelegt, das Ergebnis kommt
/// asynchron zurück (TaskCompletionSource), statt dass der Aufruf blockiert wie bei WPF.
/// Die ViewModels selbst (AddFoodViewModel, AddTrainingViewModel, ...) bleiben dabei unverändert -
/// sie kennen weiterhin nur ihre eigenen "Confirmed"/"Saved"-Events, unabhängig vom UI-Framework.
/// </summary>
public interface IDialogService
{
    Task<bool> ShowAddTrainingAsync(TrainingDiaryService trainingService, int userProfileId, DateOnly date,
        int? existingSessionId = null, string? existingSessionName = null, string? suggestedName = null);

    Task<bool> ShowWeeklyPlanAsync(TrainingPlanService planService, int userProfileId, Dictionary<DayOfWeek, string> currentPlan);

    Task<AddFoodDialogResult> ShowAddFoodAsync(FoodLookupService lookupService, NutritionDiaryService diaryService,
        int userProfileId, MealType meal, FitnessGoal goal);

    Task<FoodDetailDialogResult> ShowFoodDetailAsync(FoodItem food, FitnessGoal goal,
        double? initialAmountGrams = null, bool isEditMode = false);
}
