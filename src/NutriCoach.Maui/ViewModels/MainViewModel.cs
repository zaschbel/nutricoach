using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using NutriCoach.App.Models;
using NutriCoach.App.Services;
using NutriCoach.Maui.Drawables;
using NutriCoach.Maui.Services;

namespace NutriCoach.App.ViewModels;

/// <summary>
/// Steuert das Haupt-Dashboard: Profil-Kennzahlen, Tagesziel vs. bisher gegessen,
/// und das nach Mahlzeiten aufgeteilte Ernährungstagebuch für den gewählten Tag.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly UserProfileService _profileService;
    private readonly NutritionDiaryService _diaryService;
    private readonly FoodLookupService _lookupService;
    private readonly TrainingDiaryService _trainingService;
    private readonly TrainingPlanService _planService;
    private readonly WorkoutTemplateService _templateService;
    private readonly RecipeLookupService _recipeService;
    private readonly RecipeFavoritesService _favoritesService;
    private readonly IDialogService _dialogService;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Feuert nach jedem erfolgreich gespeicherten Eintrag (Essen, Wasser, Training) - Seiten
    /// abonnieren das für ein kurzes Erfolgs-Feedback (Häkchen-Animation), unabhängig davon, welche
    /// Aktion konkret gespeichert hat.</summary>
    public event Action? EntrySaved;

    private UserProfile? _profile;

    private double _currentWeightKg;
    /// <summary>
    /// Körpergewicht für den aktuell ausgewählten Tag (SelectedDate) - genau wie bei Wasser/Schritten,
    /// nicht mehr fest "heute". Eine nachträgliche Korrektur eines vergangenen Tages überschreibt
    /// dadurch nicht mehr automatisch alle anderen Tage.
    /// </summary>
    public double CurrentWeightKg
    {
        get => _currentWeightKg;
        set
        {
            if (Math.Abs(_currentWeightKg - value) < 0.01) return;
            _currentWeightKg = value;
            OnPropertyChanged();
            _ = SaveWeightAsync(value);
        }
    }

    private async Task SaveWeightAsync(double weightKg)
    {
        if (_profile is null) return;
        await _profileService.SetWeightForDateAsync(_profile.Id, SelectedDate, weightKg);
        _profile.CurrentWeightKg = await _profileService.GetWeightForDateAsync(_profile.Id, DateOnly.FromDateTime(DateTime.Today));
    }

    public MainViewModel(UserProfileService profileService, NutritionDiaryService diaryService,
        FoodLookupService lookupService, TrainingDiaryService trainingService, TrainingPlanService planService,
        IDialogService dialogService, WorkoutTemplateService? templateService = null,
        RecipeLookupService? recipeService = null, RecipeFavoritesService? favoritesService = null)
    {
        _profileService = profileService;
        _diaryService = diaryService;
        _lookupService = lookupService;
        _trainingService = trainingService;
        _planService = planService;
        _dialogService = dialogService;
        _templateService = templateService ?? new WorkoutTemplateService();
        _recipeService = recipeService ?? new RecipeLookupService();
        _favoritesService = favoritesService ?? new RecipeFavoritesService();

        AddFoodCommand = new RelayCommand(async param =>
        {
            try
            {
                if (param is MealType meal) await AddFoodAsync(meal);
            }
            catch (Exception ex)
            {
                // Vorruebergehende Diagnose-Absicherung: zeigt den echten Fehler an, statt die App
                // stillschweigend abstuerzen zu lassen (App.MainPage kann in .NET 9 MAUI noch direkt
                // verwendet werden, auch wenn als obsolet markiert).
                await Application.Current!.MainPage!.DisplayAlert("Fehler beim Hinzufügen", ex.ToString(), "OK");
            }
        });
        RemoveEntryCommand = new RelayCommand(async param =>
        {
            if (param is NutritionEntryDisplay entry) await RemoveEntryAsync(entry);
        });
        EditEntryCommand = new RelayCommand(async param =>
        {
            if (param is NutritionEntryDisplay entry) await EditEntryAsync(entry);
        });
        AddTrainingCommand = new RelayCommand(async _ => await AddTrainingAsync());
        AddTodayTrainingCommand = new RelayCommand(async _ => await AddTodayTrainingAsync());
        LoadAiSuggestionCommand = new RelayCommand(async _ => await LoadAiSuggestionAsync());
        AskAiQuestionCommand = new RelayCommand(async _ => await AskAiQuestionAsync(), _ => !string.IsNullOrWhiteSpace(AiQuestionText));
        ConnectHealthKitCommand = new RelayCommand(async _ => await ConnectHealthKitAsync());
        EditTrainingCommand = new RelayCommand(async param =>
        {
            if (param is TrainingSessionDisplay session) await EditTrainingAsync(session);
        });
        RemoveTrainingCommand = new RelayCommand(async param =>
        {
            if (param is TrainingSessionDisplay session) await RemoveTrainingAsync(session);
        });
        ToggleRestDayCommand = new RelayCommand(async _ => await ToggleRestDayAsync());
        SetWeeklyGoalCommand = new RelayCommand(async param =>
        {
            if (param is string s && int.TryParse(s, out var days)) await SetWeeklyGoalAsync(days);
        });
        AddWaterQuickCommand = new RelayCommand(param =>
        {
            if (param is string s && int.TryParse(s, out var ml)) { WaterMl += ml; EntrySaved?.Invoke(); }
        });
        AddStepsQuickCommand = new RelayCommand(param =>
        {
            if (param is string s && int.TryParse(s, out var steps)) StepsForSelectedDate += steps;
        });
        OpenPlanEditorCommand = new RelayCommand(async _ => await OpenPlanEditorAsync());
        GenerateAutoPlanCommand = new RelayCommand(async _ => await GenerateAutoPlanAsync());
        StartTodayPlannedTrainingCommand = new RelayCommand(async _ => await StartTodayPlannedTrainingAsync());
        ManageTemplatesCommand = new RelayCommand(async _ => await ManageTemplatesAsync());
        OpenRecipesCommand = new RelayCommand(async _ => await OpenRecipesAsync());

        TrainingSessions.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(TrainingKcalBurnedToday));
            OnPropertyChanged(nameof(HasTrainingKcalToday));
            OnPropertyChanged(nameof(TrainingKcalProgressRatio));
            OnPropertyChanged(nameof(AdjustedCalorieTarget));
            OnPropertyChanged(nameof(GaugeForegroundPath));
            OnPropertyChanged(nameof(RemainingKcal));
            OnPropertyChanged(nameof(IsOverTarget));
            OnPropertyChanged(nameof(CalorieRemainingLabel));
            OnPropertyChanged(nameof(CalorieProgressLabel));
            OnPropertyChanged(nameof(CalorieRingForegroundPath));
            OnPropertyChanged(nameof(CalorieProgressRatio)); OnPropertyChanged(nameof(CalorieGaugeDrawable)); OnPropertyChanged(nameof(HomeCalorieGaugeDrawable));
        };
        Breakfast.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasBreakfastEntries));
        Lunch.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasLunchEntries));
        Dinner.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasDinnerEntries));
        Snacks.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSnackEntries));
        PreviousWeekCommand = new RelayCommand(async _ => await ShiftWeekAsync(-7));
        NextWeekCommand = new RelayCommand(async _ => await ShiftWeekAsync(7));
        SelectDayCommand = new RelayCommand(async param =>
        {
            if (param is DayInfo day) await GoToDateAsync(day.Date);
        });
        SelectTabCommand = new RelayCommand(param =>
        {
            if (param is string tab) ActiveTab = tab;
        });

        SelectedDate = DateOnly.FromDateTime(DateTime.Today);
        // Rollierendes 7-Tage-Fenster, das immer mit SelectedDate (normalerweise heute) ganz rechts endet -
        // NICHT mehr an feste Mo-So-Kalenderwochen gekoppelt (siehe ShiftWeekAsync/GoToDateAsync unten).
        _currentWeekStart = SelectedDate.AddDays(-6);
    }

    // ---------------- Tab-Navigation ----------------
    private string _activeTab = "Home";
    public string ActiveTab
    {
        get => _activeTab;
        set
        {
            _activeTab = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsHomeTab));
            OnPropertyChanged(nameof(IsTrainingTab));
            OnPropertyChanged(nameof(IsErnaehrungTab));
            OnPropertyChanged(nameof(IsStatistikenTab));
        }
    }
    public bool IsHomeTab => ActiveTab == "Home";
    public bool IsTrainingTab => ActiveTab == "Training";
    public bool IsErnaehrungTab => ActiveTab == "Ernährung";
    public bool IsStatistikenTab => ActiveTab == "Statistiken";

    public RelayCommand SelectTabCommand { get; }

    // ---------------- Datum ----------------
    private DateOnly _selectedDate;
    public DateOnly SelectedDate
    {
        get => _selectedDate;
        set { _selectedDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedDateLabel)); }
    }
    public string SelectedDateLabel => SelectedDate == DateOnly.FromDateTime(DateTime.Today)
        ? "Heute"
        : SelectedDate.ToString("dd.MM.yyyy");

    private DateOnly _currentWeekStart;
    public string WeekRangeLabel => $"{_currentWeekStart:dd.MM.} – {_currentWeekStart.AddDays(6):dd.MM.yyyy}";

    public ObservableCollection<DayInfo> WeekDays { get; } = new();
    public ObservableCollection<WeeklyActivityBar> WeeklyActivity { get; } = new();

    /// <summary>Zeichnung für das Säulendiagramm der Wochenaktivität auf dem Dashboard.</summary>
    public IDrawable WeeklyActivityDrawable => new BarChartDrawable(
        WeeklyActivity.Select(b => new Bar3DData(b.DayAbbrev, Math.Clamp(b.HeightRatio / 80.0, 0, 1), b.IsToday)).ToList(),
        Color.FromArgb("#E2E2E4"),
        Color.FromArgb("#0058BC"),
        Color.FromArgb("#717786"),
        Color.FromArgb("#0058BC"));

    private int _averageStepsThisWeek;
    public int AverageStepsThisWeek { get => _averageStepsThisWeek; set { _averageStepsThisWeek = value; OnPropertyChanged(); } }

    public RelayCommand PreviousWeekCommand { get; }
    public RelayCommand NextWeekCommand { get; }
    public RelayCommand SelectDayCommand { get; }

    /// <summary>Wochentags-Kürzel für ein Datum, unabhängig davon, an welchem Wochentag das rollierende Fenster beginnt.</summary>
    private static readonly string[] DayAbbrevsMondayFirst = { "Mo", "Di", "Mi", "Do", "Fr", "Sa", "So" };
    private static string DayAbbrevFor(DateOnly date) => DayAbbrevsMondayFirst[((int)date.DayOfWeek + 6) % 7];

    private async Task ShiftWeekAsync(int days)
    {
        _currentWeekStart = _currentWeekStart.AddDays(days);
        OnPropertyChanged(nameof(WeekRangeLabel));
        await RefreshWeekAsync();
    }

    private async Task GoToDateAsync(DateOnly date)
    {
        SelectedDate = date;
        // Rollierendes Fenster: endet immer mit dem neu ausgewählten Tag ganz rechts (nicht mehr an
        // feste Mo-So-Kalenderwochen gekoppelt).
        _currentWeekStart = date.AddDays(-6);
        OnPropertyChanged(nameof(WeekRangeLabel));
        await RefreshDiaryAsync();
        await RefreshWeekAsync();
    }

    private async Task RefreshWeekAsync()
    {
        if (_profile is null) return;

        var weekEnd = _currentWeekStart.AddDays(6);
        var datesWithEntries = await _diaryService.GetDatesWithEntriesAsync(_profile.Id, _currentWeekStart, weekEnd);
        var datesWithTraining = await _trainingService.GetDatesWithSessionsAsync(_profile.Id, _currentWeekStart, weekEnd);
        datesWithEntries.UnionWith(datesWithTraining);
        var restDays = await _trainingService.GetRestDaysAsync(_profile.Id, _currentWeekStart, weekEnd);
        var today = DateOnly.FromDateTime(DateTime.Today);

        WeekDays.Clear();
        for (var i = 0; i < 7; i++)
        {
            var date = _currentWeekStart.AddDays(i);
            WeekDays.Add(new DayInfo(date, DayAbbrevFor(date), date.Day, date == SelectedDate, date == today,
                datesWithEntries.Contains(date), restDays.Contains(date)));
        }

        var stepsForWeek = await _diaryService.GetStepsForRangeAsync(_profile.Id, _currentWeekStart, weekEnd);
        var maxSteps = stepsForWeek.Count > 0 ? Math.Max(stepsForWeek.Values.Max(), 1) : 1;

        WeeklyActivity.Clear();
        for (var i = 0; i < 7; i++)
        {
            var date = _currentWeekStart.AddDays(i);
            var steps = stepsForWeek.TryGetValue(date, out var s) ? s : 0;
            var heightPx = Math.Max(4, (double)steps / maxSteps * 80);
            WeeklyActivity.Add(new WeeklyActivityBar(DayAbbrevFor(date), steps, heightPx, date == today));
        }

        AverageStepsThisWeek = stepsForWeek.Count > 0 ? (int)stepsForWeek.Values.Average() : 0;
        OnPropertyChanged(nameof(WeeklyActivityDrawable));
    }

    // ---------------- Profil-Kopfzeile ----------------
    private string _welcomeText = string.Empty;
    public string WelcomeText { get => _welcomeText; set { _welcomeText = value; OnPropertyChanged(); } }

    private string? _profilePicturePath;
    public string? ProfilePicturePath { get => _profilePicturePath; set { _profilePicturePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasProfilePicture)); } }
    public bool HasProfilePicture => !string.IsNullOrWhiteSpace(ProfilePicturePath);

    /// <summary>Zeigt das Ziel des Nutzers als kurzes Pill-Label (z. B. "ABNEHMEN") in der Ernährungs-Übersicht.</summary>
    public string GoalBadgeText => _profile?.Goal switch
    {
        FitnessGoal.Abnehmen => "ABNEHMEN",
        FitnessGoal.MuskelAufbau => "MUSKELAUFBAU",
        FitnessGoal.GewichtHalten => "GEWICHT HALTEN",
        FitnessGoal.KraftSteigern => "KRAFT STEIGERN",
        FitnessGoal.AusdauerVerbessern => "AUSDAUER",
        _ => "GESUNDHEIT"
    };

    private double _calorieTarget;
    public double CalorieTarget { get => _calorieTarget; set { _calorieTarget = value; OnPropertyChanged(); OnPropertyChanged(nameof(CalorieProgressLabel)); OnPropertyChanged(nameof(CalorieRemainingLabel)); OnPropertyChanged(nameof(GaugeForegroundPath)); OnPropertyChanged(nameof(RemainingKcal)); OnPropertyChanged(nameof(IsOverTarget)); OnPropertyChanged(nameof(AdjustedCalorieTarget)); OnPropertyChanged(nameof(HasTrainingKcalToday)); OnPropertyChanged(nameof(CalorieRingForegroundPath));
            OnPropertyChanged(nameof(CalorieProgressRatio)); OnPropertyChanged(nameof(CalorieGaugeDrawable)); OnPropertyChanged(nameof(HomeCalorieGaugeDrawable)); } }

    // ---------------- Dashboard (Home): Streak, Schritte, heutiges Training ----------------

    private int _currentStreakDays;
    /// <summary>Aktuelle Trainings-Streak in Tagen (Ruhetage unterbrechen sie nicht), für die Flamme oben im Dashboard.</summary>
    public int CurrentStreakDays { get => _currentStreakDays; set { _currentStreakDays = value; OnPropertyChanged(); } }

    private int _trackingStreakDays;
    /// <summary>Streak in Tagen, an denen irgendetwas getrackt wurde (Ernährung, Training oder Ruhetag).</summary>
    public int TrackingStreakDays { get => _trackingStreakDays; set { _trackingStreakDays = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasTrackingStreak)); } }
    public bool HasTrackingStreak => TrackingStreakDays > 0;

    /// <summary>Tagesziel für Schritte - aktuell ein fester Richtwert, keine individuelle Einstellung.</summary>
    public int StepsGoal { get; } = 10000;

    private int _stepsForSelectedDate;
    // ---------------- Apple Health / HealthKit ----------------
    private readonly IHealthDataService _healthService = new HealthDataService();

    public bool IsHealthKitSupported => _healthService.IsSupported;

    private bool _isHealthKitConnected;
    public bool IsHealthKitConnected
    {
        get => _isHealthKitConnected;
        set { _isHealthKitConnected = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowHealthKitConnectButton)); }
    }

    /// <summary>
    /// Zeigt den Verbinden-Button immer, solange noch nicht verbunden - auch wenn IsHealthKitSupported
    /// false liefert. Der Button war zuvor komplett unsichtbar, wenn IsSupported (fälschlich oder aus
    /// unbekanntem Grund) false war, ohne jede Rückmeldung. Jetzt bekommt man beim Tippen in jedem Fall
    /// eine klare Meldung statt eines Buttons, der einfach nie auftaucht.
    /// </summary>
    public bool ShowHealthKitConnectButton => !IsHealthKitConnected;

    public RelayCommand ConnectHealthKitCommand { get; }

    private async Task ConnectHealthKitAsync()
    {
        if (!IsHealthKitSupported)
        {
            await Application.Current!.MainPage!.DisplayAlert("Schrittzähler nicht verfügbar",
                "Dieses Gerät unterstützt keine automatische Schrittzählung über die Bewegungssensoren.", "OK");
            return;
        }

        var granted = await _healthService.RequestAuthorizationAsync();
        IsHealthKitConnected = granted;
        Preferences.Default.Set("healthkit_connected", granted);

        if (granted)
        {
            await SyncStepsFromHealthKitAsync();
        }
        else
        {
            await Application.Current!.MainPage!.DisplayAlert("Zugriff nicht erlaubt",
                "Ohne Erlaubnis für Bewegung & Fitness kann NutriCoach die Schritte nicht automatisch auslesen. Du kannst sie weiterhin manuell eintragen.", "OK");
        }
    }

    /// <summary>Holt die Schrittzahl aus Apple Health für den aktuell ausgewählten Tag und übernimmt sie (überschreibt die manuelle Eingabe).</summary>
    private async Task SyncStepsFromHealthKitAsync()
    {
        if (!IsHealthKitConnected) return;

        try
        {
            // Läuft automatisch bei jedem App-Start (siehe RefreshDiaryAsync) - ein Fehler hier darf
            // die App unter keinen Umständen abstürzen lassen, sonst kann man die App nach einer
            // fehlgeschlagenen Verbindung nie wieder öffnen (Absturzschleife).
            var steps = await _healthService.GetStepsForDateAsync(SelectedDate);
            if (steps is null) return; // z. B. keine Daten für den Tag - dann bleibt der bisherige Wert stehen

            _stepsForSelectedDate = steps.Value;
            OnPropertyChanged(nameof(StepsForSelectedDate));
            OnPropertyChanged(nameof(StepsRemainingLabel));
            OnPropertyChanged(nameof(StepsProgressRatio));
            _ = SaveStepsAsync(steps.Value);
        }
        catch
        {
            // still stehen lassen, der bisherige (manuelle) Wert bleibt gültig
        }
    }

    /// <summary>
    /// Schrittzahl für den aktuell ausgewählten Tag (SelectedDate) - manuell eingetragen, speichert bei
    /// jeder Änderung sofort, genau wie beim Wasser-Schieberegler. Wechselst du den Tag im Kalender,
    /// wechselt auch dieser Wert mit (genau wie Kalorien/Wasser), nicht mehr fest auf "heute" fixiert.
    /// </summary>
    public int StepsForSelectedDate
    {
        get => _stepsForSelectedDate;
        set
        {
            if (_stepsForSelectedDate == value) return;
            _stepsForSelectedDate = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StepsRemainingLabel));
            OnPropertyChanged(nameof(StepsProgressRatio));
            _ = SaveStepsAsync(value);
        }
    }

    public string StepsRemainingLabel
    {
        get
        {
            var missing = StepsGoal - StepsForSelectedDate;
            return missing <= 0 ? "🎉 Tagesziel erreicht!" : $"noch {missing:N0} Schritte bis zum Ziel";
        }
    }

    /// <summary>0-1-Verhältnis für MAUI-ProgressBar (die erwartet einen Wert zwischen 0 und 1, keine absolute Zahl).</summary>
    public double StepsProgressRatio => StepsGoal > 0 ? Math.Clamp((double)StepsForSelectedDate / StepsGoal, 0, 1) : 0;

    /// <summary>0-1-Verhältnis für die Kalorien-ProgressBar in MAUI.</summary>
    public double CalorieProgressRatio => AdjustedCalorieTarget > 0 ? Math.Clamp(Totals.Kcal / AdjustedCalorieTarget, 0, 1) : 0;

    /// <summary>Zeichnung für den Kalorien-Bogen in der Ernährungs-Übersicht (aktualisiert sich bei jeder relevanten Änderung neu).</summary>
    public IDrawable CalorieGaugeDrawable => new GaugeDrawable(
        CalorieProgressRatio * 100,
        Color.FromArgb("#E2E2E4"),
        Color.FromArgb("#0058BC"));

    /// <summary>Kompakter, nahezu voller Ring für die Kalorien-Karte im Dashboard (Home-Reiter),
    /// analog zur MCI-App-Vorlage. Nutzt die tatsächlichen Dunkelmodus-Farbwerte aus App.xaml.cs,
    /// da die App dauerhaft im Dunkelmodus läuft.</summary>
    public IDrawable HomeCalorieGaugeDrawable => new GaugeDrawable(
        CalorieProgressRatio * 100,
        Color.FromArgb("#282B31"),
        Color.FromArgb("#4C8DFF"),
        startDeg: -90, sweepDeg: 270, centered: true);

    /// <summary>Voller Hintergrund-Ring für die kompakte Kalorien-Anzeige im Dashboard (Home-Reiter).</summary>
    public string CalorieRingBackgroundPath => PieChartHelper.BuildArc(-90, 269.999, 60, 64, 64);

    /// <summary>Farbiger Fortschritts-Ring bis zum aktuellen Kalorien-Stand, beginnt oben (12-Uhr-Position).</summary>
    public string CalorieRingForegroundPath
    {
        get
        {
            var percent = AdjustedCalorieTarget > 0 ? Math.Clamp(Totals.Kcal / AdjustedCalorieTarget * 100, 0, 100) : 0;
            return PieChartHelper.BuildArc(-90, -90 + percent * 3.6, 60, 64, 64);
        }
    }

    /// <summary>Trainingseinheiten des HEUTIGEN Tages (unabhängig vom ausgewählten Datum) - für die Dashboard-Karte "Heutiges Training".</summary>
    public ObservableCollection<TrainingSessionDisplay> TodayTrainingSessions { get; } = new();
    public bool HasTodayTraining => TodayTrainingSessions.Count > 0;
    public bool HasNoTodayTraining => !HasTodayTraining;

    // ---------------- KI-Optimierung (echte Claude-API-Anbindung) ----------------
    private readonly GeminiAiService _aiService = new();

    private bool _hasAiApiKey;
    public bool HasAiApiKey { get => _hasAiApiKey; set { _hasAiApiKey = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowAiGetSuggestionButton)); } }

    private bool _isLoadingAiSuggestion;
    public bool IsLoadingAiSuggestion { get => _isLoadingAiSuggestion; set { _isLoadingAiSuggestion = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowAiGetSuggestionButton)); } }

    private AiSwapSuggestion? _aiSuggestion;
    public bool HasAiSuggestion => _aiSuggestion is not null && _aiSuggestion.HasImprovement;
    public bool HasAiOptimalMessage => _aiSuggestion is not null && !_aiSuggestion.HasImprovement;
    public string AiSuggestionIntro => $"Vorschlag, um bei \"{GoalBadgeText}\" im Plan zu bleiben:";
    public string AiSuggestionOriginal => _aiSuggestion?.Original ?? "";
    public string AiSuggestionAlternative => _aiSuggestion?.Alternative ?? "";
    public string AiSuggestionKcalSaved => _aiSuggestion is null ? "" : (_aiSuggestion.KcalSaved > 0 ? $"-{_aiSuggestion.KcalSaved}" : $"{_aiSuggestion.KcalSaved}");
    public string AiSuggestionReason => _aiSuggestion?.Reason ?? "";

    private string _aiErrorMessage = "";
    public string AiErrorMessage { get => _aiErrorMessage; set { _aiErrorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasAiError)); } }
    public bool HasAiError => !string.IsNullOrWhiteSpace(AiErrorMessage);

    /// <summary>Zeigt den "Vorschlag holen"-Button nur, wenn ein Key da ist, gerade nichts lädt und noch kein Vorschlag/Fehler vorliegt.</summary>
    public bool ShowAiGetSuggestionButton => HasAiApiKey && !IsLoadingAiSuggestion && !HasAiSuggestion && !HasAiOptimalMessage && !HasAiError;

    public RelayCommand LoadAiSuggestionCommand { get; }

    // ---------------- Freie Frage an die KI ----------------
    private string _aiQuestionText = "";
    public string AiQuestionText { get => _aiQuestionText; set { _aiQuestionText = value; OnPropertyChanged(); AskAiQuestionCommand.RaiseCanExecuteChanged(); } }

    private bool _isAskingAiQuestion;
    public bool IsAskingAiQuestion { get => _isAskingAiQuestion; set { _isAskingAiQuestion = value; OnPropertyChanged(); } }

    private string _aiAnswerText = "";
    public string AiAnswerText { get => _aiAnswerText; set { _aiAnswerText = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasAiAnswer)); } }
    public bool HasAiAnswer => !string.IsNullOrWhiteSpace(AiAnswerText);

    public RelayCommand AskAiQuestionCommand { get; }

    /// <summary>
    /// Baut eine Zusammenfassung aus Trainingsplan, Wochenziel-Fortschritt und Makro-Zielen -
    /// damit die KI nicht nur sieht, was heute gegessen wurde, sondern den ganzen Plan kennt und
    /// darauf aufbauend einen wirklich passenden Vorschlag machen kann.
    /// </summary>
    private string BuildAiPlanContext()
    {
        var trainingPart = HasTodayPlan
            ? $"Laut Wochenplan ist heute \"{TodayPlanName}\" dran."
            : "Für heute ist im Wochenplan kein bestimmter Fokus vorgesehen (Ruhetag/frei).";

        var weeklyGoalPart = HasWeeklyGoal
            ? $"Wochenziel: {WeeklyTrainingGoal}x Training, bisher {TrainingDaysThisWeek}x diese Woche geschafft."
            : "Noch kein Wochen-Trainingsziel festgelegt.";

        var macroPart = $"Makro-Ziele für heute: Eiweiß {ProteinTargetG:0}g (bisher {Totals.Protein:0}g), " +
                         $"Kohlenhydrate {CarbsTargetG:0}g (bisher {Totals.Carbs:0}g), Fett {FatTargetG:0}g (bisher {Totals.Fat:0}g).";

        return $"{trainingPart} {weeklyGoalPart} {macroPart}";
    }

    private async Task AskAiQuestionAsync()
    {
        if (_profile is null || string.IsNullOrWhiteSpace(AiQuestionText)) return;

        IsAskingAiQuestion = true;
        AiAnswerText = "";
        AiErrorMessage = "";

        var todaysFoods = Breakfast.Concat(Lunch).Concat(Dinner).Concat(Snacks).Select(e => e.Name).ToList();
        var foodsSummary = todaysFoods.Count > 0 ? string.Join(", ", todaysFoods) : "noch nichts";

        var (answer, error) = await _aiService.AskQuestionAsync(AiQuestionText, foodsSummary, GoalBadgeText, RemainingKcal, BuildAiPlanContext());

        IsAskingAiQuestion = false;
        AiAnswerText = answer ?? "";
        AiErrorMessage = error ?? "";
    }

    public async Task CheckAiApiKeyAsync()
    {
        var key = await GeminiAiService.GetApiKeyAsync();
        HasAiApiKey = !string.IsNullOrWhiteSpace(key);
    }

    private async Task LoadAiSuggestionAsync()
    {
        if (_profile is null) return;

        IsLoadingAiSuggestion = true;
        AiErrorMessage = "";
        _aiSuggestion = null;
        OnPropertyChanged(nameof(HasAiSuggestion)); OnPropertyChanged(nameof(HasAiOptimalMessage));
        OnPropertyChanged(nameof(ShowAiGetSuggestionButton));

        var todaysFoods = Breakfast.Concat(Lunch).Concat(Dinner).Concat(Snacks).Select(e => e.Name).ToList();
        var foodsSummary = todaysFoods.Count > 0 ? string.Join(", ", todaysFoods) : "noch nichts";

        var result = await _aiService.GetMealSwapSuggestionAsync(foodsSummary, GoalBadgeText, RemainingKcal, BuildAiPlanContext());

        IsLoadingAiSuggestion = false;
        _aiSuggestion = result.Suggestion;
        AiErrorMessage = result.ErrorMessage ?? "";
        OnPropertyChanged(nameof(HasAiSuggestion)); OnPropertyChanged(nameof(HasAiOptimalMessage));
        OnPropertyChanged(nameof(AiSuggestionIntro));
        OnPropertyChanged(nameof(AiSuggestionOriginal));
        OnPropertyChanged(nameof(AiSuggestionAlternative));
        OnPropertyChanged(nameof(AiSuggestionKcalSaved));
        OnPropertyChanged(nameof(AiSuggestionReason));
        OnPropertyChanged(nameof(ShowAiGetSuggestionButton));
    }
    public RelayCommand AddTodayTrainingCommand { get; }

    /// <summary>Wie viel kcal heute laut Trainings-Einheiten geschätzt verbrannt wurden - erhöht das Tagesziel.</summary>
    public double TrainingKcalBurnedToday => TrainingSessions.Sum(s => s.EstimatedKcal);
    public bool HasTrainingKcalToday => TrainingKcalBurnedToday > 0;

    /// <summary>Grober Fortschrittsbalken-Wert fuer die Trainings-Kachel (kein festes Tagesziel im
    /// Domainmodell vorhanden, daher 500 kcal als plausible Referenz fuer "viel trainiert").</summary>
    public double TrainingKcalProgressRatio => Math.Clamp(TrainingKcalBurnedToday / 500.0, 0, 1);

    /// <summary>Das tatsächlich angezeigte Tagesziel: Basis-Kalorienziel plus heute verbrannte Trainings-Kalorien.</summary>
    public double AdjustedCalorieTarget => Math.Round(CalorieTarget + TrainingKcalBurnedToday, 0);

    private double _proteinTargetG;
    public double ProteinTargetG { get => _proteinTargetG; set { _proteinTargetG = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProteinLabel)); OnPropertyChanged(nameof(ProteinProgressRatio)); } }

    private double _carbsTargetG;
    public double CarbsTargetG { get => _carbsTargetG; set { _carbsTargetG = value; OnPropertyChanged(); OnPropertyChanged(nameof(CarbsLabel)); OnPropertyChanged(nameof(CarbsProgressRatio)); } }

    private double _fatTargetG;
    public double FatTargetG { get => _fatTargetG; set { _fatTargetG = value; OnPropertyChanged(); OnPropertyChanged(nameof(FatLabel)); OnPropertyChanged(nameof(FatProgressRatio)); } }

    public string ProteinLabel => $"{Totals.Protein:0} / {ProteinTargetG:0} g";
    public string CarbsLabel => $"{Totals.Carbs:0} / {CarbsTargetG:0} g";
    public string FatLabel => $"{Totals.Fat:0} / {FatTargetG:0} g";

    public double ProteinProgressRatio => ProteinTargetG > 0 ? Math.Clamp(Totals.Protein / ProteinTargetG, 0, 1) : 0;
    public double CarbsProgressRatio => CarbsTargetG > 0 ? Math.Clamp(Totals.Carbs / CarbsTargetG, 0, 1) : 0;
    public double FatProgressRatio => FatTargetG > 0 ? Math.Clamp(Totals.Fat / FatTargetG, 0, 1) : 0;

    /// <summary>Fester Hintergrund-Bogen des Kalorien-Gauges (der komplette Halbkreis, dezent).</summary>
    public string GaugeBackgroundPath => PieChartHelper.BuildArc(180, 360, 80, 90, 90);

    /// <summary>Farbiger Bogen bis zum aktuellen Fortschritt (0-100%, wächst mit den gegessenen Kalorien).</summary>
    public string GaugeForegroundPath
    {
        get
        {
            var percent = AdjustedCalorieTarget > 0 ? Math.Clamp(Totals.Kcal / AdjustedCalorieTarget * 100, 0, 100) : 0;
            return PieChartHelper.BuildArc(180, 180 + percent * 1.8, 80, 90, 90);
        }
    }

    // ---------------- Tageswerte ----------------
    private DailyTotals _totals = new();
    public DailyTotals Totals
    {
        get => _totals;
        set
        {
            _totals = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CalorieProgressLabel));
            OnPropertyChanged(nameof(CalorieRemainingLabel));
            OnPropertyChanged(nameof(WaterLabel));
            OnPropertyChanged(nameof(ProteinLabel));
            OnPropertyChanged(nameof(CarbsLabel));
            OnPropertyChanged(nameof(FatLabel));
            OnPropertyChanged(nameof(ProteinProgressRatio));
            OnPropertyChanged(nameof(CarbsProgressRatio));
            OnPropertyChanged(nameof(FatProgressRatio));
            OnPropertyChanged(nameof(CalorieProgressRatio)); OnPropertyChanged(nameof(CalorieGaugeDrawable)); OnPropertyChanged(nameof(HomeCalorieGaugeDrawable));
            OnPropertyChanged(nameof(GaugeForegroundPath));
            OnPropertyChanged(nameof(RemainingKcal));
            OnPropertyChanged(nameof(IsOverTarget));
            OnPropertyChanged(nameof(CalorieRingForegroundPath));
            OnPropertyChanged(nameof(CalorieProgressRatio)); OnPropertyChanged(nameof(CalorieGaugeDrawable)); OnPropertyChanged(nameof(HomeCalorieGaugeDrawable));

            // Direkt ins Feld schreiben statt über die Property, damit das Laden
            // aus der Datenbank nicht sofort wieder einen Speichervorgang auslöst.
            _waterMl = value.WaterMl;
            OnPropertyChanged(nameof(WaterMl));
            OnPropertyChanged(nameof(WaterFillHeight));
            OnPropertyChanged(nameof(WaterLabel));
            OnPropertyChanged(nameof(WaterRemainingLabel));
            OnPropertyChanged(nameof(WaterProgressRatio));
            OnPropertyChanged(nameof(WaterBottleDrawable));
            OnPropertyChanged(nameof(WaterWaveHeight));
            OnPropertyChanged(nameof(WaterPercentLabel));
        }
    }

    public string CalorieProgressLabel => $"{Totals.Kcal:0} / {AdjustedCalorieTarget:0} kcal";

    public double RemainingKcal => Math.Abs(Math.Round(AdjustedCalorieTarget - Totals.Kcal, 0));
    public bool IsOverTarget => Totals.Kcal > AdjustedCalorieTarget;

    public string CalorieRemainingLabel
    {
        get
        {
            var remaining = AdjustedCalorieTarget - Totals.Kcal;
            return remaining >= 0
                ? $"Noch {remaining:0} kcal übrig"
                : $"{-remaining:0} kcal über dem Ziel";
        }
    }

    public string WaterLabel => $"{Totals.WaterMl} ml";

    /// <summary>Individuelles Tages-Wasserziel, aus Gewicht/Aktivität berechnet (siehe BmrCalculator).</summary>
    private int _waterTargetMl = 2500;
    public int WaterTargetMl { get => _waterTargetMl; set { _waterTargetMl = value; OnPropertyChanged(); OnPropertyChanged(nameof(WaterFillHeight)); OnPropertyChanged(nameof(WaterRemainingLabel)); OnPropertyChanged(nameof(WaterTargetLabel)); OnPropertyChanged(nameof(WaterProgressRatio)); OnPropertyChanged(nameof(WaterBottleDrawable)); OnPropertyChanged(nameof(WaterWaveHeight)); OnPropertyChanged(nameof(WaterPercentLabel)); } }
    public string WaterTargetLabel => $"Ziel: {FormatMl(WaterTargetMl)}";

    /// <summary>Wert des Schiebereglers - beim Ändern wird der Tageswert direkt gespeichert.</summary>
    private int _waterMl;
    public int WaterMl
    {
        get => _waterMl;
        set
        {
            if (_waterMl == value) return;
            _waterMl = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(WaterFillHeight));
            OnPropertyChanged(nameof(WaterRemainingLabel));
            OnPropertyChanged(nameof(WaterProgressRatio)); OnPropertyChanged(nameof(WaterBottleDrawable)); OnPropertyChanged(nameof(WaterWaveHeight)); OnPropertyChanged(nameof(WaterPercentLabel));
            _ = SaveWaterAsync(value);
        }
    }

    /// <summary>Füllhöhe der Flaschengrafik in Pixeln (0 bis 105, passend zur Flaschenkörper-Höhe im XAML).</summary>
    public double WaterFillHeight => Math.Clamp((double)WaterMl / Math.Max(WaterTargetMl, 1) * 105.0, 0, 105);
    public double WaterProgressRatio => WaterTargetMl > 0 ? Math.Clamp((double)WaterMl / WaterTargetMl, 0, 1) : 0;
    public IDrawable WaterBottleDrawable => new WaterBottleDrawable(WaterProgressRatio);

    /// <summary>Füllhöhe in Pixeln für die Hydration-Grafik in MAUI (Container ist 90px hoch).</summary>
    public double WaterWaveHeight => WaterProgressRatio * 90;
    public string WaterPercentLabel => $"{WaterProgressRatio * 100:0}%";

    public RelayCommand AddWaterQuickCommand { get; private set; } = null!;
    public RelayCommand AddStepsQuickCommand { get; private set; } = null!;

    /// <summary>Zeigt an, wie viel bis zum persönlichen Tagesziel noch fehlt (z. B. "noch 1 L bis zum Ziel").</summary>
    public string WaterRemainingLabel
    {
        get
        {
            var missing = WaterTargetMl - WaterMl;
            return missing <= 0 ? "🎉 Tagesziel erreicht!" : $"noch {FormatMl(missing)} bis zum Ziel";
        }
    }

    private static string FormatMl(int ml) => ml >= 1000 ? $"{ml / 1000.0:0.#} L" : $"{ml} ml";

    // ---------------- Mahlzeiten ----------------
    public ObservableCollection<NutritionEntryDisplay> Breakfast { get; } = new();
    public ObservableCollection<NutritionEntryDisplay> Lunch { get; } = new();
    public ObservableCollection<NutritionEntryDisplay> Dinner { get; } = new();
    public ObservableCollection<NutritionEntryDisplay> Snacks { get; } = new();

    public bool HasBreakfastEntries => Breakfast.Count > 0;
    public bool HasLunchEntries => Lunch.Count > 0;
    public bool HasDinnerEntries => Dinner.Count > 0;
    public bool HasSnackEntries => Snacks.Count > 0;

    public RelayCommand AddFoodCommand { get; }
    public RelayCommand RemoveEntryCommand { get; }
    public RelayCommand EditEntryCommand { get; }
    public RelayCommand OpenRecipesCommand { get; }

    /// <summary>Öffnet die Rezepte-Seite (Suche bei TheMealDB + lokale Favoriten) - eigenständiges
    /// Browsing-Feature, unabhängig vom Ernährungstagebuch (siehe RecipesPage/RecipesViewModel).</summary>
    private async Task OpenRecipesAsync()
    {
        try
        {
            // Ziel + Kalorien-/Makro-Ziel als Kontext für die KI-Bewertung der Rezepte mitgeben, plus
            // eine zum Ziel passende TheMealDB-Kategorie für die "Für dein Ziel"-Vorschläge (die
            // Datenbank hat keine Nährwerte, deshalb nur eine grobe, plausible Kategorie-Zuordnung).
            string? goalContext = null;
            string? suggestedCategory = null;
            if (_profile is not null)
            {
                var kcalTarget = BmrCalculator.CalculateCalorieTarget(_profile);
                var macros = BmrCalculator.CalculateMacroTargets(_profile);
                goalContext = $"Ziel: {_profile.Goal}. Tägliches Kalorienziel: {kcalTarget:0} kcal, " +
                    $"Eiweiß {macros.ProteinG:0}g, Kohlenhydrate {macros.CarbsG:0}g, Fett {macros.FatG:0}g.";
                suggestedCategory = _profile.Goal switch
                {
                    FitnessGoal.Abnehmen => "Seafood",
                    FitnessGoal.MuskelAufbau => "Chicken",
                    FitnessGoal.KraftSteigern => "Beef",
                    FitnessGoal.AusdauerVerbessern => "Pasta",
                    _ => "Chicken"
                };
            }

            await _dialogService.ShowRecipesAsync(_recipeService, _favoritesService, _aiService, goalContext, suggestedCategory);
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Fehler beim Öffnen der Rezepte", ex.ToString(), "OK");
        }
    }

    // ---------------- Training ----------------
    public ObservableCollection<TrainingSessionDisplay> TrainingSessions { get; } = new();
    public RelayCommand AddTrainingCommand { get; }

    private TrainingSessionDisplay? _lastSession;
    public TrainingSessionDisplay? LastSession { get => _lastSession; set { _lastSession = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasLastSession)); } }
    public bool HasLastSession => LastSession is not null;

    private DateOnly? _lastSessionDate;
    public DateOnly? LastSessionDate { get => _lastSessionDate; set { _lastSessionDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastSessionDateLabel)); } }
    public string LastSessionDateLabel
    {
        get
        {
            if (LastSessionDate is null) return "";
            var diff = DateOnly.FromDateTime(DateTime.Today).DayNumber - LastSessionDate.Value.DayNumber;
            return diff switch
            {
                1 => "Gestern",
                0 => "Heute",
                _ when diff > 1 && diff <= 6 => $"vor {diff} Tagen",
                _ => LastSessionDate.Value.ToString("dd.MM.yyyy")
            };
        }
    }
    public RelayCommand EditTrainingCommand { get; }
    public RelayCommand RemoveTrainingCommand { get; }
    public RelayCommand ToggleRestDayCommand { get; }
    public RelayCommand SetWeeklyGoalCommand { get; }

    private int _weeklyTrainingGoal;
    public int WeeklyTrainingGoal
    {
        get => _weeklyTrainingGoal;
        set { _weeklyTrainingGoal = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasWeeklyGoal)); OnPropertyChanged(nameof(HasNoWeeklyGoal)); OnPropertyChanged(nameof(TrainingProgressLabel)); OnPropertyChanged(nameof(TrainingProgressRatio)); OnPropertyChanged(nameof(TrainingProgressRatioMaui)); OnPropertyChanged(nameof(NeedsPlanTypeChoice)); OnPropertyChanged(nameof(ShowTrainingProgress)); }
    }
    public bool HasWeeklyGoal => WeeklyTrainingGoal > 0;
    public bool HasNoWeeklyGoal => WeeklyTrainingGoal <= 0;

    private int _trainingDaysThisWeek;
    public int TrainingDaysThisWeek
    {
        get => _trainingDaysThisWeek;
        set { _trainingDaysThisWeek = value; OnPropertyChanged(); OnPropertyChanged(nameof(TrainingProgressLabel)); OnPropertyChanged(nameof(TrainingProgressRatio)); OnPropertyChanged(nameof(TrainingProgressRatioMaui)); OnPropertyChanged(nameof(TrainingThisWeekHeatmapLabel)); OnPropertyChanged(nameof(TrainingThisWeekHeatmapRatio)); }
    }

    public string TrainingProgressLabel => $"{TrainingDaysThisWeek}/{WeeklyTrainingGoal} Trainingstage";
    public double TrainingProgressRatio => WeeklyTrainingGoal > 0 ? Math.Min((double)TrainingDaysThisWeek / WeeklyTrainingGoal, 1.0) * 100 : 0;

    /// <summary>0-1-Variante von TrainingProgressRatio für MAUI-ProgressBar (die WPF-Version nutzt 0-100).</summary>
    public double TrainingProgressRatioMaui => TrainingProgressRatio / 100.0;
    public bool ShowTrainingProgress => HasWeeklyGoal && HasWeeklyPlan;

    // ---------------- Dashboard-Kalenderraster (letzte 30 Tage), nach MCI-App-Vorbild ----------------
    private bool[] _weighInLast30Days = new bool[30];
    private bool[] _trainingLast30Days = new bool[30];

    public IDrawable WeighInHeatmapDrawable => new HeatmapDrawable(
        _weighInLast30Days, Color.FromArgb("#34C77A"), Color.FromArgb("#212123"));
    public IDrawable TrainingHeatmapDrawable => new HeatmapDrawable(
        _trainingLast30Days, Color.FromArgb("#34C77A"), Color.FromArgb("#212123"));

    private int _weighInDaysThisWeek;
    public int WeighInDaysThisWeek
    {
        get => _weighInDaysThisWeek;
        set { _weighInDaysThisWeek = value; OnPropertyChanged(); OnPropertyChanged(nameof(WeighInThisWeekLabel)); OnPropertyChanged(nameof(WeighInProgressRatio)); }
    }
    public string WeighInThisWeekLabel => $"{WeighInDaysThisWeek}/7 diese Woche";
    public double WeighInProgressRatio => WeighInDaysThisWeek / 7.0;

    public string TrainingThisWeekHeatmapLabel => $"{TrainingDaysThisWeek}/7 diese Woche";
    public double TrainingThisWeekHeatmapRatio => TrainingDaysThisWeek / 7.0;

    // ---------------- Wochenplan ----------------
    private bool _hasWeeklyPlan;
    public bool HasWeeklyPlan { get => _hasWeeklyPlan; set { _hasWeeklyPlan = value; OnPropertyChanged(); OnPropertyChanged(nameof(NeedsPlanTypeChoice)); OnPropertyChanged(nameof(ShowTrainingProgress)); } }

    /// <summary>Zeigt die "Selbst erstellen / Vorschlagen"-Auswahl, sobald ein Wochenziel gesetzt, aber noch kein Plan vorhanden ist.</summary>
    public bool NeedsPlanTypeChoice => HasWeeklyGoal && !HasWeeklyPlan;

    public RelayCommand OpenPlanEditorCommand { get; }
    public RelayCommand GenerateAutoPlanCommand { get; }
    public RelayCommand StartTodayPlannedTrainingCommand { get; }
    public RelayCommand ManageTemplatesCommand { get; }

    private string _todayPlanName = "Frei";
    public string TodayPlanName { get => _todayPlanName; set { _todayPlanName = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasTodayPlan)); OnPropertyChanged(nameof(HasNoTodayPlan)); OnPropertyChanged(nameof(TodayPlanLabel)); } }

    /// <summary>Ob für heute ein konkreter Trainings-Fokus geplant ist (nicht "Frei"/"Ruhetag").</summary>
    public bool HasTodayPlan => !string.IsNullOrWhiteSpace(TodayPlanName)
        && TodayPlanName != "Frei" && TodayPlanName != "Ruhetag";
    public bool HasNoTodayPlan => !HasTodayPlan;

    public string TodayPlanLabel => HasTodayPlan ? $"📅 Heute geplant: {TodayPlanName}" : $"📅 Heute: {TodayPlanName}";

    private bool _isSelectedDateRestDay;
    public bool IsSelectedDateRestDay { get => _isSelectedDateRestDay; set { _isSelectedDateRestDay = value; OnPropertyChanged(); } }

    // ---------------- Laden ----------------
    public async Task LoadAsync()
    {
        _profile = await _profileService.GetActiveProfileAsync();
        if (_profile is null) return;

        await CheckAiApiKeyAsync();
        IsHealthKitConnected = Preferences.Default.Get("healthkit_connected", false) && IsHealthKitSupported;

        WelcomeText = $"Hallo, {_profile.Name}!";
        OnPropertyChanged(nameof(GoalBadgeText));
        ProfilePicturePath = _profile.ProfilePicturePath;
        CalorieTarget = BmrCalculator.CalculateCalorieTarget(_profile);
        WaterTargetMl = BmrCalculator.CalculateWaterTargetMl(_profile);

        var (proteinG, carbsG, fatG) = BmrCalculator.CalculateMacroTargets(_profile);
        ProteinTargetG = proteinG;
        CarbsTargetG = carbsG;
        FatTargetG = fatG;

        WeeklyTrainingGoal = _profile.WeeklyTrainingGoalDays;
        HasWeeklyPlan = await _planService.HasPlanAsync(_profile.Id);

        await RefreshDiaryAsync();
        await RefreshWeekAsync();
        await RefreshTodayAsync();
    }

    /// <summary>
    /// Lädt alles, was auf der Dashboard-Startseite immer den HEUTIGEN Tag zeigt - unabhängig davon,
    /// welches Datum im Training-/Ernährungs-Reiter gerade ausgewählt ist (Streak, Schritte, heutiges Training).
    /// </summary>
    private async Task RefreshTodayAsync()
    {
        if (_profile is null) return;

        var today = DateOnly.FromDateTime(DateTime.Today);

        var sessions = await _trainingService.GetSessionsForDateAsync(_profile.Id, today, _profile.CurrentWeightKg);
        TodayTrainingSessions.Clear();
        foreach (var session in sessions) TodayTrainingSessions.Add(session);
        OnPropertyChanged(nameof(HasTodayTraining));
        OnPropertyChanged(nameof(HasNoTodayTraining));

        CurrentStreakDays = await _trainingService.GetCurrentStreakDaysAsync(_profile.Id);
        TrackingStreakDays = await _trainingService.GetTrackingStreakDaysAsync(_profile.Id);

        TodayPlanName = await _planService.GetPlanForDayAsync(_profile.Id, today.DayOfWeek);

        var lastSession = await _trainingService.GetMostRecentSessionBeforeAsync(_profile.Id, today, _profile.CurrentWeightKg);
        LastSession = lastSession;
        LastSessionDate = lastSession is not null ? lastSession.Date : null;

        await RefreshDashboardHeatmapsAsync(today);
    }

    /// <summary>Lädt die Kalenderraster für die Dashboard-Karten "Weigh-In" und "Trainings" (letzte 30 Tage).</summary>
    private async Task RefreshDashboardHeatmapsAsync(DateOnly today)
    {
        if (_profile is null) return;
        var thirtyDaysAgo = today.AddDays(-29);

        var weightHistory = await _profileService.GetWeightHistoryAsync(_profile.Id, 30);
        _weighInLast30Days = weightHistory.Select(w => w.WeightKg.HasValue).ToArray();
        WeighInDaysThisWeek = weightHistory.TakeLast(7).Count(w => w.WeightKg.HasValue);
        OnPropertyChanged(nameof(WeighInHeatmapDrawable));

        var datesWithTraining = await _trainingService.GetDatesWithSessionsAsync(_profile.Id, thirtyDaysAgo, today);
        _trainingLast30Days = Enumerable.Range(0, 30).Select(i => datesWithTraining.Contains(thirtyDaysAgo.AddDays(i))).ToArray();
        OnPropertyChanged(nameof(TrainingHeatmapDrawable));
    }

    private async Task RefreshDiaryAsync()
    {
        if (_profile is null) return;

        var entries = await _diaryService.GetEntriesForDateAsync(_profile.Id, SelectedDate);

        Breakfast.Clear();
        Lunch.Clear();
        Dinner.Clear();
        Snacks.Clear();

        foreach (var entry in entries)
        {
            var target = entry.Meal switch
            {
                MealType.Frühstück => Breakfast,
                MealType.Mittagessen => Lunch,
                MealType.Abendessen => Dinner,
                _ => Snacks
            };
            target.Add(entry);
        }

        Totals = await _diaryService.GetDailyTotalsAsync(_profile.Id, SelectedDate);

        // Direkt ins Feld schreiben statt über die Property, damit das Laden nicht sofort wieder speichert.
        _stepsForSelectedDate = await _diaryService.GetStepsForDateAsync(_profile.Id, SelectedDate);
        OnPropertyChanged(nameof(StepsForSelectedDate));
        OnPropertyChanged(nameof(StepsRemainingLabel));
        OnPropertyChanged(nameof(StepsProgressRatio));

        if (IsHealthKitConnected) await SyncStepsFromHealthKitAsync();

        // Direkt ins Feld schreiben statt über die Property, damit das Laden nicht sofort wieder speichert.
        _currentWeightKg = await _profileService.GetWeightForDateAsync(_profile.Id, SelectedDate);
        OnPropertyChanged(nameof(CurrentWeightKg));

        await RefreshTrainingAsync();
    }

    private async Task RefreshTrainingAsync()
    {
        if (_profile is null) return;

        var sessions = await _trainingService.GetSessionsForDateAsync(_profile.Id, SelectedDate, _profile.CurrentWeightKg);
        TrainingSessions.Clear();
        foreach (var session in sessions) TrainingSessions.Add(session);

        IsSelectedDateRestDay = await _trainingService.IsRestDayAsync(_profile.Id, SelectedDate);
        TrainingDaysThisWeek = await _trainingService.CountTrainingDaysInWeekAsync(_profile.Id, _currentWeekStart, _currentWeekStart.AddDays(6));
    }

    private async Task AddTrainingAsync()
    {
        if (_profile is null) return;

        var wasSaved = await _dialogService.ShowAddTrainingAsync(_trainingService, _profile.Id, SelectedDate);

        if (wasSaved)
        {
            EntrySaved?.Invoke();
            await RefreshTrainingAsync();
            await RefreshWeekAsync();
            await RefreshTodayAsync();
        }
    }

    /// <summary>Wie AddTrainingAsync, aber immer für den heutigen Tag - für den "+ Eintragen"-Button im Dashboard.</summary>
    private async Task AddTodayTrainingAsync()
    {
        if (_profile is null) return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var wasSaved = await _dialogService.ShowAddTrainingAsync(_trainingService, _profile.Id, today);

        if (wasSaved)
        {
            EntrySaved?.Invoke();
            await RefreshTodayAsync();
            if (SelectedDate == today)
            {
                await RefreshTrainingAsync();
            }
            await RefreshWeekAsync();
        }
    }

    private async Task EditTrainingAsync(TrainingSessionDisplay session)
    {
        if (_profile is null) return;

        var wasSaved = await _dialogService.ShowAddTrainingAsync(_trainingService, _profile.Id, SelectedDate, session.Id, session.Name);

        if (wasSaved)
        {
            EntrySaved?.Invoke();
            await RefreshTrainingAsync();
            await RefreshWeekAsync();
            await RefreshTodayAsync();
        }
    }

    private async Task RemoveTrainingAsync(TrainingSessionDisplay session)
    {
        await _trainingService.RemoveSessionAsync(session.Id);
        await RefreshTrainingAsync();
        await RefreshWeekAsync();
        await RefreshTodayAsync();
    }

    private async Task ToggleRestDayAsync()
    {
        if (_profile is null) return;
        await _trainingService.ToggleRestDayAsync(_profile.Id, SelectedDate);
        IsSelectedDateRestDay = await _trainingService.IsRestDayAsync(_profile.Id, SelectedDate);
        await RefreshTodayAsync();
        await RefreshWeekAsync();
    }

    private async Task SetWeeklyGoalAsync(int days)
    {
        if (_profile is null) return;
        await _profileService.SetWeeklyTrainingGoalAsync(_profile.Id, days);
        _profile.WeeklyTrainingGoalDays = days;
        WeeklyTrainingGoal = days;
        HasWeeklyPlan = await _planService.HasPlanAsync(_profile.Id);
    }

    private async Task OpenPlanEditorAsync()
    {
        if (_profile is null) return;

        var currentPlan = await _planService.GetPlanAsync(_profile.Id);
        var wasSaved = await _dialogService.ShowWeeklyPlanAsync(_planService, _profile.Id, currentPlan);

        if (wasSaved)
        {
            HasWeeklyPlan = true;
            await RefreshTodayAsync();
        }
    }

    private async Task GenerateAutoPlanAsync()
    {
        if (_profile is null) return;

        var autoPlan = _planService.GenerateAutoPlan(WeeklyTrainingGoal);
        await _planService.SavePlanAsync(_profile.Id, autoPlan);
        HasWeeklyPlan = true;
        await RefreshTodayAsync();
    }

    /// <summary>
    /// Öffnet die Vorlagen-Verwaltung ("Vorlage verwenden"): Nutzer kann dort eine bestehende
    /// Trainingsvorlage anlegen/löschen oder eine auswählen, um direkt eine neue Trainingseinheit
    /// mit deren Übungen vorbefüllt zu starten (nur Übungsnamen, Sätze/Gewicht bleiben manuell).
    /// </summary>
    private async Task ManageTemplatesAsync()
    {
        if (_profile is null) return;

        var template = await _dialogService.ShowManageTemplatesAsync(_templateService, _trainingService, _profile.Id);
        if (template is null || template.ExerciseNames.Count == 0) return;

        // Vorlagenname als Trainings-Vorschlag übernehmen - ohne das blieb SessionName leer und der
        // Speichern-Button (braucht einen nicht-leeren Namen) war dadurch unbemerkt deaktiviert.
        var wasSaved = await _dialogService.ShowAddTrainingAsync(_trainingService, _profile.Id, SelectedDate,
            suggestedName: template.Name, prefilledExerciseNames: template.ExerciseNames);

        if (wasSaved)
        {
            EntrySaved?.Invoke();
            await RefreshTrainingAsync();
            await RefreshWeekAsync();
            await RefreshTodayAsync();
        }
    }

    private async Task StartTodayPlannedTrainingAsync()
    {
        if (_profile is null) return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var wasSaved = await _dialogService.ShowAddTrainingAsync(_trainingService, _profile.Id, today, suggestedName: TodayPlanName);

        if (wasSaved)
        {
            EntrySaved?.Invoke();
            await RefreshTodayAsync();
            if (SelectedDate == today) await RefreshTrainingAsync();
            await RefreshWeekAsync();
        }
    }

    private async Task SaveStepsAsync(int steps)
    {
        if (_profile is null) return;
        await _diaryService.SetStepsForDateAsync(_profile.Id, SelectedDate, steps);
    }

    private async Task AddFoodAsync(MealType meal)
    {
        if (_profile is null) return;

        var result = await _dialogService.ShowAddFoodAsync(_lookupService, _diaryService, _profile.Id, meal, _profile.Goal);

        if (result.Confirmed && result.Food is not null)
        {
            await _diaryService.AddEntryAsync(_profile.Id, result.Food.Id, result.AmountGrams, meal, SelectedDate);
            EntrySaved?.Invoke();
            await RefreshDiaryAsync();
            await RefreshWeekAsync();
        }
    }

    private async Task RemoveEntryAsync(NutritionEntryDisplay entry)
    {
        await _diaryService.RemoveEntryAsync(entry.Id);
        await RefreshDiaryAsync();
        await RefreshWeekAsync();
    }

    private async Task EditEntryAsync(NutritionEntryDisplay entry)
    {
        if (_profile is null) return;

        var food = await _lookupService.GetByIdAsync(entry.FoodItemId);
        if (food is null) return;

        var result = await _dialogService.ShowFoodDetailAsync(food, _profile.Goal, entry.AmountGrams, isEditMode: true);

        if (!result.Confirmed) return;

        if (result.WasDeleted)
        {
            await _diaryService.RemoveEntryAsync(entry.Id);
        }
        else if (result.Food is not null)
        {
            await _diaryService.UpdateEntryAmountAsync(entry.Id, result.AmountGrams);
        }

        await RefreshDiaryAsync();
        await RefreshWeekAsync();
    }

    private async Task SaveWaterAsync(int amountMl)
    {
        if (_profile is null) return;
        await _diaryService.SetWaterForDateAsync(_profile.Id, SelectedDate, amountMl);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        // HealthKit-Callbacks (u.a. SyncStepsFromHealthKitAsync) laufen auf einem Hintergrund-Thread;
        // ohne MainThread-Dispatch aktualisieren manche Bindings (z.B. Label.Text) auf iOS lautlos
        // nicht, waehrend andere (z.B. ProgressBar) trotzdem durchkommen - daher hier immer ueber
        // MainThread.BeginInvokeOnMainThread schicken statt PropertyChanged direkt aufzurufen.
        if (MainThread.IsMainThread)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)));
        }
    }
}
