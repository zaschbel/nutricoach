using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NutriCoach.App.Models;
using NutriCoach.App.Services;
using NutriCoach.App.ViewModels;

namespace NutriCoach.Maui.ViewModels;

/// <summary>
/// Kompakte Ersteinrichtung für den MAUI-Start: sammelt die Pflichtfelder für die
/// Kalorienberechnung in einem Formular statt der ausführlichen 9-Schritte-Version aus der
/// Windows-App. Die vollständige, geführte Variante (mit Verletzungen, Zielgewicht-Zeitplan usw.)
/// folgt als eigener Ausbauschritt.
/// </summary>
public class QuickSetupViewModel : INotifyPropertyChanged
{
    private readonly UserProfileService _profileService;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Completed;

    public QuickSetupViewModel(UserProfileService profileService)
    {
        _profileService = profileService;
        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
    }

    public List<Gender> GenderOptions { get; } = Enum.GetValues<Gender>().ToList();
    public List<FitnessGoal> GoalOptions { get; } = Enum.GetValues<FitnessGoal>().ToList();
    public List<ActivityLevel> ActivityOptions { get; } = Enum.GetValues<ActivityLevel>().ToList();
    public List<DailyJobActivity> JobActivityOptions { get; } = Enum.GetValues<DailyJobActivity>().ToList();
    public List<ExperienceLevel> ExperienceOptions { get; } = Enum.GetValues<ExperienceLevel>().ToList();

    private string _name = string.Empty;
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); SaveCommand.RaiseCanExecuteChanged(); } }

    private DateTime _birthDate = DateTime.Today.AddYears(-25);
    public DateTime BirthDate { get => _birthDate; set { _birthDate = value; OnPropertyChanged(); } }

    private Gender _gender = Gender.Männlich;
    public Gender Gender { get => _gender; set { _gender = value; OnPropertyChanged(); } }

    private double _heightCm = 175;
    public double HeightCm { get => _heightCm; set { _heightCm = value; OnPropertyChanged(); SaveCommand.RaiseCanExecuteChanged(); } }

    private double _weightKg = 75;
    public double WeightKg { get => _weightKg; set { _weightKg = value; OnPropertyChanged(); SaveCommand.RaiseCanExecuteChanged(); } }

    private FitnessGoal _goal = FitnessGoal.AllgemeineGesundheit;
    public FitnessGoal Goal { get => _goal; set { _goal = value; OnPropertyChanged(); } }

    private ActivityLevel _activityLevel = ActivityLevel.MäßigAktiv;
    public ActivityLevel ActivityLevel { get => _activityLevel; set { _activityLevel = value; OnPropertyChanged(); } }

    private DailyJobActivity _jobActivity = DailyJobActivity.ÜberwiegendSitzend;
    public DailyJobActivity JobActivity { get => _jobActivity; set { _jobActivity = value; OnPropertyChanged(); } }

    private ExperienceLevel _experience = ExperienceLevel.Anfänger;
    public ExperienceLevel Experience { get => _experience; set { _experience = value; OnPropertyChanged(); } }

    public RelayCommand SaveCommand { get; }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name) && HeightCm > 0 && WeightKg > 0;

    private async Task SaveAsync()
    {
        var profile = new UserProfile
        {
            Name = Name,
            BirthDate = DateOnly.FromDateTime(BirthDate),
            Gender = Gender,
            HeightCm = HeightCm,
            CurrentWeightKg = WeightKg,
            Goal = Goal,
            ActivityLevel = ActivityLevel,
            JobActivity = JobActivity,
            Experience = Experience,
            RecentTrend = RecentTrend.KeineVeränderung,
            OnboardingCompleted = true
        };

        await _profileService.CreateProfileAsync(profile);
        Completed?.Invoke();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
