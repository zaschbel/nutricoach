using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NutriCoach.App.Services;

namespace NutriCoach.App.ViewModels;

/// <summary>Ein bearbeitbarer Tag im Wochenplan-Editor.</summary>
public class PlanDayRow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public DayOfWeek DayOfWeek { get; init; }
    public string DayLabel { get; init; } = string.Empty;

    private string _planName = "Frei";
    public string PlanName { get => _planName; set { _planName = value; OnPropertyChanged(); } }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Steuert das Fenster, in dem der Nutzer selbst festlegt, was an welchem Wochentag trainiert wird.</summary>
public class WeeklyPlanViewModel : INotifyPropertyChanged
{
    private readonly TrainingPlanService _planService;
    private readonly int _userProfileId;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Saved;

    public ObservableCollection<PlanDayRow> Days { get; } = new();

    /// <summary>Vorschläge für die Schnellauswahl, damit man nicht jedes Mal frei tippen muss.</summary>
    public List<string> Suggestions { get; } = new()
    {
        "Ganzkörper", "Push", "Pull", "Beine", "Oberkörper", "Unterkörper",
        "Brust", "Rücken", "Schulter", "Arme", "Cardio", "Mobility", "Ruhetag", "Frei"
    };

    public RelayCommand SaveCommand { get; }

    public WeeklyPlanViewModel(TrainingPlanService planService, int userProfileId, Dictionary<DayOfWeek, string> currentPlan)
    {
        _planService = planService;
        _userProfileId = userProfileId;

        var order = new (DayOfWeek Day, string Label)[]
        {
            (DayOfWeek.Monday, "Montag"), (DayOfWeek.Tuesday, "Dienstag"), (DayOfWeek.Wednesday, "Mittwoch"),
            (DayOfWeek.Thursday, "Donnerstag"), (DayOfWeek.Friday, "Freitag"), (DayOfWeek.Saturday, "Samstag"),
            (DayOfWeek.Sunday, "Sonntag")
        };

        foreach (var (day, label) in order)
        {
            Days.Add(new PlanDayRow
            {
                DayOfWeek = day,
                DayLabel = label,
                PlanName = currentPlan.TryGetValue(day, out var name) ? name : "Frei"
            });
        }

        SaveCommand = new RelayCommand(async _ => await SaveAsync());
    }

    private async Task SaveAsync()
    {
        var plan = Days.ToDictionary(d => d.DayOfWeek, d => d.PlanName);
        await _planService.SavePlanAsync(_userProfileId, plan);
        Saved?.Invoke();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
