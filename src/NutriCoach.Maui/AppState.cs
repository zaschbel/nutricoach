using NutriCoach.App.Services;
using NutriCoach.App.ViewModels;

namespace NutriCoach.Maui;

/// <summary>
/// Hält die EINE gemeinsame MainViewModel-Instanz für die ganze App. In WPF gab es ein
/// einziges Fenster mit vier Reitern, die sich automatisch eine ViewModel-Instanz geteilt haben.
/// In MAUI Shell bekommt jeder Tab technisch seine eigene Seite - damit z. B. das ausgewählte
/// Datum oder die Kalorien-Zahlen trotzdem über alle Reiter hinweg synchron bleiben, teilen sich
/// alle Seiten diese eine statische Instanz statt jede ihre eigene zu erzeugen.
/// </summary>
public static class AppState
{
    public static MainViewModel MainViewModel { get; }
    public static UserProfileService ProfileService { get; }

    static AppState()
    {
        ProfileService = new UserProfileService();
        var diaryService = new NutritionDiaryService();
        var lookupService = new FoodLookupService();
        var trainingService = new TrainingDiaryService();
        var planService = new TrainingPlanService();
        var dialogService = new Services.DialogService();

        MainViewModel = new MainViewModel(ProfileService, diaryService, lookupService, trainingService, planService, dialogService);
    }
}
