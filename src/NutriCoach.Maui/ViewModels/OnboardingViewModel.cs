using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NutriCoach.App.Models;
using NutriCoach.App.Services;

namespace NutriCoach.App.ViewModels;

/// <summary>
/// Kapselt ein Datum als getrennte Tag/Monat/Jahr-Auswahl (statt eines DatePicker-Steuerelements,
/// das sich als unzuverlässig herausgestellt hat). Berechnet automatisch ein kombiniertes DateTime?,
/// sobald alle drei Teile ausgewählt sind.
/// </summary>
public class DateParts : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? DateChanged;

    private int? _day;
    public int? Day { get => _day; set { _day = value; OnPropertyChanged(); RaiseDateChanged(); } }

    private int? _month;
    public int? Month { get => _month; set { _month = value; OnPropertyChanged(); RaiseDateChanged(); } }

    private int? _year;
    public int? Year { get => _year; set { _year = value; OnPropertyChanged(); RaiseDateChanged(); } }

    public DateTime? Date
    {
        get
        {
            if (Day is int d && Month is int m && Year is int y)
            {
                try { return new DateTime(y, m, d); }
                catch { return null; } // z.B. 31. Februar - einfach noch kein gültiges Datum
            }
            return null;
        }
    }

    private void RaiseDateChanged()
    {
        OnPropertyChanged(nameof(Date));
        DateChanged?.Invoke();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Steuert den mehrstufigen Onboarding-Fragebogen. Jeder Schritt entspricht genau
/// einer der geforderten Fragen (Ziel, Erfahrung, Verletzungen, Alltag, Zeitrahmen ...).
/// Am Ende wird EIN UserProfile erzeugt und dauerhaft gespeichert.
/// </summary>
public class OnboardingViewModel : INotifyPropertyChanged
{
    private readonly UserProfileService _profileService;
    public event PropertyChangedEventHandler? PropertyChanged;

    // ---------------- Auswahl-Listen für Tag/Monat/Jahr-Dropdowns ----------------
    public List<int> DayOptions { get; } = Enumerable.Range(1, 31).ToList();

    public List<KeyValuePair<int, string>> MonthOptions { get; } = new()
    {
        new(1, "Januar"), new(2, "Februar"), new(3, "März"), new(4, "April"),
        new(5, "Mai"), new(6, "Juni"), new(7, "Juli"), new(8, "August"),
        new(9, "September"), new(10, "Oktober"), new(11, "November"), new(12, "Dezember")
    };

    /// <summary>Für Geburtsdatum / seit wann trainierst du / seit wann bestehen Beschwerden - Jahre in der Vergangenheit.</summary>
    public List<int> YearOptionsPast { get; } =
        Enumerable.Range(DateTime.Today.Year - 100, 101).Reverse().ToList();

    /// <summary>Für das Wunschdatum - Jahre in naher Zukunft.</summary>
    public List<int> YearOptionsFuture { get; } =
        Enumerable.Range(DateTime.Today.Year, 16).ToList();

    // ---------------- Auswahl-Listen für die Picker ----------------
    public List<Gender> GenderOptions { get; } = Enum.GetValues<Gender>().ToList();
    public List<FitnessGoal> GoalOptions { get; } = Enum.GetValues<FitnessGoal>().ToList();
    public List<ExperienceLevel> ExperienceOptions { get; } = Enum.GetValues<ExperienceLevel>().ToList();
    public List<RecentTrend> RecentTrendOptions { get; } = Enum.GetValues<RecentTrend>().ToList();
    public List<InjurySeverity> InjurySeverityOptions { get; } = Enum.GetValues<InjurySeverity>().ToList();
    public List<DailyJobActivity> JobActivityOptions { get; } = Enum.GetValues<DailyJobActivity>().ToList();
    public List<ActivityLevel> ActivityOptions { get; } = Enum.GetValues<ActivityLevel>().ToList();

    public OnboardingViewModel(UserProfileService profileService)
    {
        _profileService = profileService;
        NextCommand = new RelayCommand(_ => GoNext(), _ => CanGoNext());
        BackCommand = new RelayCommand(_ => GoBack(), _ => CurrentStep > 0);
        AddInjuryCommand = new RelayCommand(_ => AddInjury());
        RemoveInjuryCommand = new RelayCommand(param =>
        {
            if (param is InjuryRecord injury) Injuries.Remove(injury);
        });
        FinishCommand = new RelayCommand(async _ => await FinishAsync(), _ => CurrentStep == TotalSteps - 1);

        BirthDateParts.DateChanged += () => NextCommand.RaiseCanExecuteChanged();
    }

    // ---------------- Schritt-Navigation ----------------

    public const int TotalSteps = 9;
    private int _currentStep;
    public int CurrentStep
    {
        get => _currentStep;
        set
        {
            _currentStep = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StepLabel));
            OnPropertyChanged(nameof(StepProgress));
            OnPropertyChanged(nameof(IsLastStep));
            OnPropertyChanged(nameof(IsNotLastStep));
            BackCommand.RaiseCanExecuteChanged();
            FinishCommand.RaiseCanExecuteChanged();
        }
    }

    public string StepLabel => $"Schritt {CurrentStep + 1} von {TotalSteps}";
    public double StepProgress => (double)(CurrentStep + 1) / TotalSteps;
    public bool IsLastStep => CurrentStep == TotalSteps - 1;
    public bool IsNotLastStep => !IsLastStep;

    public RelayCommand NextCommand { get; }
    public RelayCommand BackCommand { get; }
    public RelayCommand FinishCommand { get; }
    public RelayCommand AddInjuryCommand { get; }
    public RelayCommand RemoveInjuryCommand { get; }

    private void GoNext() { if (CurrentStep < TotalSteps - 1) CurrentStep++; }
    private void GoBack() { if (CurrentStep > 0) CurrentStep--; }

    /// <summary>Pro Schritt wird geprüft, ob die Pflichtfelder ausgefüllt sind, bevor "Weiter" aktiv wird.</summary>
    private bool CanGoNext() => CurrentStep switch
    {
        0 => !string.IsNullOrWhiteSpace(Name) && BirthDateParts.Date.HasValue,
        1 => HeightCm > 0 && CurrentWeightKg > 0,
        // Schritte 2 (Ziel), 3 (Zeitrahmen), 4 (Erfahrung), 5 (letzte Entwicklung),
        // 6 (Verletzungen - optional), 7 (Alltag/Job), 8 (Zusammenfassung) haben
        // sinnvolle Vorauswahlen und blockieren daher nicht.
        _ => true
    };

    // ---------------- Schritt 0: Stammdaten ----------------
    private string _name = string.Empty;
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); NextCommand.RaiseCanExecuteChanged(); } }

    private DateParts _birthDateParts = new();
    public DateParts BirthDateParts { get => _birthDateParts; set { _birthDateParts = value; OnPropertyChanged(); } }

    private Gender _gender = Gender.Männlich;
    public Gender Gender { get => _gender; set { _gender = value; OnPropertyChanged(); } }

    // ---------------- Schritt 1: Körperdaten ----------------
    private double _heightCm;
    public double HeightCm { get => _heightCm; set { _heightCm = value; OnPropertyChanged(); NextCommand.RaiseCanExecuteChanged(); } }

    private double _currentWeightKg;
    public double CurrentWeightKg { get => _currentWeightKg; set { _currentWeightKg = value; OnPropertyChanged(); NextCommand.RaiseCanExecuteChanged(); } }

    private double? _initialBodyFatPercent;
    public double? InitialBodyFatPercent { get => _initialBodyFatPercent; set { _initialBodyFatPercent = value; OnPropertyChanged(); } }

    // ---------------- Schritt 2: Ziel ----------------
    private FitnessGoal _goal = FitnessGoal.Abnehmen;
    public FitnessGoal Goal { get => _goal; set { _goal = value; OnPropertyChanged(); } }

    // ---------------- Schritt 3: Zeitrahmen / Zielgewicht ----------------
    private double? _targetWeightKg;
    public double? TargetWeightKg { get => _targetWeightKg; set { _targetWeightKg = value; OnPropertyChanged(); } }

    private DateParts _targetDateParts = new();
    public DateParts TargetDateParts { get => _targetDateParts; set { _targetDateParts = value; OnPropertyChanged(); } }

    // ---------------- Schritt 4: Erfahrung ----------------
    private ExperienceLevel _experience = ExperienceLevel.Anfänger;
    public ExperienceLevel Experience { get => _experience; set { _experience = value; OnPropertyChanged(); } }

    private DateParts _trainingSinceParts = new();
    public DateParts TrainingSinceParts { get => _trainingSinceParts; set { _trainingSinceParts = value; OnPropertyChanged(); } }

    // ---------------- Schritt 5: Letzte Entwicklung ----------------
    private RecentTrend _recentTrend = RecentTrend.KeineVeränderung;
    public RecentTrend RecentTrend { get => _recentTrend; set { _recentTrend = value; OnPropertyChanged(); } }

    private string _otherActivities = string.Empty;
    public string OtherActivities { get => _otherActivities; set { _otherActivities = value; OnPropertyChanged(); } }

    // ---------------- Schritt 6: Verletzungen / Einschränkungen ----------------
    public ObservableCollection<InjuryRecord> Injuries { get; } = new();

    private bool _hasInjuries;
    public bool HasInjuries
    {
        get => _hasInjuries;
        set { _hasInjuries = value; if (value) _hasNoInjuries = false; OnPropertyChanged(); OnPropertyChanged(nameof(HasNoInjuries)); }
    }

    private bool _hasNoInjuries = true;
    public bool HasNoInjuries
    {
        get => _hasNoInjuries;
        set
        {
            _hasNoInjuries = value;
            if (value) { _hasInjuries = false; Injuries.Clear(); }
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasInjuries));
        }
    }

    private string _newInjuryArea = string.Empty;
    public string NewInjuryArea { get => _newInjuryArea; set { _newInjuryArea = value; OnPropertyChanged(); } }

    private InjurySeverity _newInjurySeverity = InjurySeverity.LeichtesZiehen;
    public InjurySeverity NewInjurySeverity { get => _newInjurySeverity; set { _newInjurySeverity = value; OnPropertyChanged(); } }

    private DateParts _newInjuryOnsetDateParts = new();
    public DateParts NewInjuryOnsetDateParts { get => _newInjuryOnsetDateParts; set { _newInjuryOnsetDateParts = value; OnPropertyChanged(); } }

    private string _newInjuryDescription = string.Empty;
    public string NewInjuryDescription { get => _newInjuryDescription; set { _newInjuryDescription = value; OnPropertyChanged(); } }

    private void AddInjury()
    {
        if (string.IsNullOrWhiteSpace(NewInjuryArea)) return;

        Injuries.Add(new InjuryRecord
        {
            BodyArea = NewInjuryArea,
            Severity = NewInjurySeverity,
            OnsetDate = NewInjuryOnsetDateParts.Date.HasValue ? DateOnly.FromDateTime(NewInjuryOnsetDateParts.Date.Value) : null,
            Description = string.IsNullOrWhiteSpace(NewInjuryDescription) ? null : NewInjuryDescription
        });

        NewInjuryArea = string.Empty;
        NewInjuryOnsetDateParts = new DateParts();
        NewInjuryDescription = string.Empty;
    }

    // ---------------- Schritt 7: Alltag ----------------
    private DailyJobActivity _jobActivity = DailyJobActivity.ÜberwiegendSitzend;
    public DailyJobActivity JobActivity { get => _jobActivity; set { _jobActivity = value; OnPropertyChanged(); } }

    private ActivityLevel _activityLevel = ActivityLevel.LeichtAktiv;
    public ActivityLevel ActivityLevel { get => _activityLevel; set { _activityLevel = value; OnPropertyChanged(); } }

    // ---------------- Abschluss ----------------
    public event Action? OnboardingCompleted;

    public string SummaryText =>
        $"Ziel: {Goal}\n" +
        $"Größe: {HeightCm:0} cm, Gewicht: {CurrentWeightKg:0.0} kg\n" +
        (TargetWeightKg.HasValue ? $"Zielgewicht: {TargetWeightKg:0.0} kg\n" : "") +
        $"Erfahrung: {Experience}\n" +
        $"Alltag: {JobActivity}, Aktivität: {ActivityLevel}\n" +
        (Injuries.Count > 0 ? $"Einschränkungen: {Injuries.Count} erfasst" : "Keine Einschränkungen erfasst");

    private async Task FinishAsync()
    {
        var profile = new UserProfile
        {
            Name = Name,
            BirthDate = DateOnly.FromDateTime(BirthDateParts.Date ?? DateTime.Today.AddYears(-25)),
            Gender = Gender,
            HeightCm = HeightCm,
            CurrentWeightKg = CurrentWeightKg,
            Goal = Goal,
            TargetWeightKg = TargetWeightKg,
            TargetDate = TargetDateParts.Date.HasValue ? DateOnly.FromDateTime(TargetDateParts.Date.Value) : null,
            Experience = Experience,
            TrainingSince = TrainingSinceParts.Date.HasValue ? DateOnly.FromDateTime(TrainingSinceParts.Date.Value) : null,
            RecentTrend = RecentTrend,
            OtherActivities = string.IsNullOrWhiteSpace(OtherActivities) ? null : OtherActivities,
            JobActivity = JobActivity,
            ActivityLevel = ActivityLevel,
            Injuries = Injuries.ToList()
        };

        await _profileService.CreateProfileAsync(profile, InitialBodyFatPercent);
        OnboardingCompleted?.Invoke();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
