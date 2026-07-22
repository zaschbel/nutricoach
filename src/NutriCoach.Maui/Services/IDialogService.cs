using NutriCoach.App.Models;
using NutriCoach.App.Services;
using NutriCoach.App.ViewModels;

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
        int? existingSessionId = null, string? existingSessionName = null, string? suggestedName = null,
        List<string>? prefilledExerciseNames = null);

    Task<bool> ShowWeeklyPlanAsync(TrainingPlanService planService, int userProfileId, Dictionary<DayOfWeek, string> currentPlan);

    /// <summary>
    /// Zeigt die Trainingsvorlagen-Verwaltung (anlegen/löschen/auswählen). Liefert die Übungsnamen
    /// der ausgewählten Vorlage zurück (für die anschließende Vorbefüllung von AddTrainingPage),
    /// oder null, wenn der Nutzer nur verwaltet hat oder abgebrochen ist.
    /// </summary>
    Task<List<string>?> ShowManageTemplatesAsync(WorkoutTemplateService templateService, TrainingDiaryService trainingService, int userProfileId);

    Task<AddFoodDialogResult> ShowAddFoodAsync(FoodLookupService lookupService, NutritionDiaryService diaryService,
        int userProfileId, MealType meal, FitnessGoal goal);

    Task<FoodDetailDialogResult> ShowFoodDetailAsync(FoodItem food, FitnessGoal goal,
        double? initialAmountGrams = null, bool isEditMode = false);

    /// <summary>Zeigt die Rezepte-Seite (Suche + Favoriten). Schließt sich selbst, kein Rückgabewert nötig.</summary>
    Task ShowRecipesAsync(RecipeLookupService recipeService, RecipeFavoritesService favoritesService, GeminiAiService aiService);

    /// <summary>Zeigt die Detailseite eines einzelnen Rezepts (Zutaten, Zubereitung, Favorisieren).</summary>
    Task ShowRecipeDetailAsync(Recipe recipe, RecipeFavoritesService favoritesService);
}
