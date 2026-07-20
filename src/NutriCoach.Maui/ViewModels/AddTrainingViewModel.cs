using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NutriCoach.App.Models;
using NutriCoach.App.Services;

namespace NutriCoach.App.ViewModels;

/// <summary>Ein einzelner Satz innerhalb einer Übung (Gewicht x Wiederholungen bzw. nur Wiederholungen).</summary>
public class SetRow
{
    public double WeightKg { get; set; }
    public int Reps { get; set; } = 10;
}

/// <summary>
/// Eine ausgewählte Übung innerhalb der neuen Trainingseinheit. Welche Felder angezeigt werden,
/// hängt vom InputType der zugrunde liegenden Übung ab (Gewicht/Wdh., nur Wdh., Strecke/Zeit, Widerstand/Zeit).
/// </summary>
public class ExerciseBlockViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public Exercise Exercise { get; }
    public string ExerciseName => Exercise.Name;
    public string MuscleInfo => Exercise.SecondaryMuscleGroup is null
        ? Exercise.PrimaryMuscleGroup
        : $"{Exercise.PrimaryMuscleGroup} · {Exercise.SecondaryMuscleGroup}";
    public string? Tip => Exercise.Tip;
    public bool HasTip => !string.IsNullOrWhiteSpace(Tip);

    public bool IsSetsBased => Exercise.InputType is ExerciseInputType.GewichtWiederholungen or ExerciseInputType.NurWiederholungen;
    public bool ShowWeight => Exercise.InputType == ExerciseInputType.GewichtWiederholungen;
    public bool IsCardioBased => !IsSetsBased;
    public bool ShowDistance => Exercise.InputType == ExerciseInputType.StreckeZeit;

    /// <summary>
    /// Widerstand wird bei "Widerstand/Zeit"-Übungen immer abgefragt (z. B. Rudergerät),
    /// UND zusätzlich bei Strecke/Zeit-Übungen wie Radfahren, sobald "Drinnen" gewählt ist
    /// UND die Übung laut Datenbank einen Widerstand vorsieht (z. B. Spinning-Bike).
    /// </summary>
    public bool ShowResistance => Exercise.InputType == ExerciseInputType.WiderstandZeit
        || (Exercise.InputType == ExerciseInputType.StreckeZeit && Location == CardioLocation.Drinnen
            && Exercise.IndoorMetric == IndoorCardioMetric.Widerstand);

    /// <summary>Steigung wird nur bei Strecke/Zeit-Übungen drinnen abgefragt, deren Indoor-Parameter Steigung ist (z. B. Laufband).</summary>
    public bool ShowIncline => Exercise.InputType == ExerciseInputType.StreckeZeit && Location == CardioLocation.Drinnen
        && Exercise.IndoorMetric == IndoorCardioMetric.Steigung;

    // ---------------- Sets-basiert (Kraft/Körpergewicht) ----------------
    public ObservableCollection<SetRow> Sets { get; } = new();
    public RelayCommand AddSetCommand { get; }
    public RelayCommand RemoveSetCommand { get; }

    // ---------------- Cardio-basiert (Strecke/Zeit oder Widerstand/Zeit) ----------------
    private int _durationMinutes = 30;
    public int DurationMinutes { get => _durationMinutes; set { _durationMinutes = value; OnPropertyChanged(); } }

    private double? _distanceKm;
    public double? DistanceKm { get => _distanceKm; set { _distanceKm = value; OnPropertyChanged(); } }

    private int? _resistanceLevel;
    public int? ResistanceLevel { get => _resistanceLevel; set { _resistanceLevel = value; OnPropertyChanged(); } }

    private double? _inclinePercent;
    public double? InclinePercent { get => _inclinePercent; set { _inclinePercent = value; OnPropertyChanged(); } }

    private CardioLocation _location = CardioLocation.Drinnen;
    public CardioLocation Location { get => _location; set { _location = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowResistance)); OnPropertyChanged(nameof(ShowIncline)); } }

    public Array LocationOptions => Enum.GetValues(typeof(CardioLocation));

    public ExerciseBlockViewModel(Exercise exercise, ExistingExerciseData? existing = null)
    {
        Exercise = exercise;

        AddSetCommand = new RelayCommand(_ =>
        {
            var lastWeight = Sets.Count > 0 ? Sets[^1].WeightKg : 0;
            var lastReps = Sets.Count > 0 ? Sets[^1].Reps : 10;
            Sets.Add(new SetRow { WeightKg = lastWeight, Reps = lastReps });
        });
        RemoveSetCommand = new RelayCommand(param =>
        {
            if (param is SetRow row) Sets.Remove(row);
        });

        if (IsSetsBased)
        {
            if (existing is { Sets.Count: > 0 })
            {
                foreach (var set in existing.Sets)
                    Sets.Add(new SetRow { WeightKg = set.WeightKg, Reps = set.Reps });
            }
            else
            {
                Sets.Add(new SetRow());
            }
        }
        else if (existing is not null)
        {
            _durationMinutes = existing.DurationMinutes ?? 30;
            _distanceKm = existing.DistanceKm;
            _resistanceLevel = existing.ResistanceLevel;
            _inclinePercent = existing.InclinePercent;
            _location = existing.Location;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Steuert das "Training hinzufügen"-Fenster: freier Name für die Einheit (z. B. "Push Day"),
/// Übungen werden aus der Datenbank gesucht/ausgewählt statt frei getippt, Eingabefelder passen
/// sich automatisch dem jeweiligen Übungstyp an. Alles wird erst beim Speichern übernommen.
/// </summary>
public class AddTrainingViewModel : INotifyPropertyChanged
{
    private readonly TrainingDiaryService _trainingService;
    private readonly int _userProfileId;
    private readonly DateOnly _date;
    private readonly int? _existingSessionId;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Saved;

    public bool IsEditMode => _existingSessionId is not null;
    public bool IsNotEditMode => !IsEditMode;
    public string WindowHeadline => IsEditMode ? $"\"{SessionName}\" bearbeiten" : "Training hinzufügen";
    public string SaveButtonText => "Speichern";

    public AddTrainingViewModel(TrainingDiaryService trainingService, int userProfileId, DateOnly date,
        int? existingSessionId = null, string? existingSessionName = null, string? suggestedName = null)
    {
        _trainingService = trainingService;
        _userProfileId = userProfileId;
        _date = date;
        _existingSessionId = existingSessionId;
        if (existingSessionName is not null) _sessionName = existingSessionName;
        else if (suggestedName is not null) _sessionName = suggestedName;

        SearchCommand = new RelayCommand(async _ => await SearchAsync());
        SelectExerciseCommand = new RelayCommand(async param =>
        {
            if (param is Exercise exercise) await SelectExerciseAsync(exercise);
        });
        RemoveExerciseCommand = new RelayCommand(param =>
        {
            if (param is ExerciseBlockViewModel block) Exercises.Remove(block);
        });
        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());

        Exercises.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasExercises));
            SaveCommand.RaiseCanExecuteChanged();
        };
    }

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

    /// <summary>Lädt im Bearbeiten-Modus die bereits gespeicherten Übungen der Trainingseinheit als editierbare Blöcke.</summary>
    public async Task LoadExistingExercisesAsync()
    {
        if (_existingSessionId is null) return;

        IsLoading = true;
        var (name, existingExercises) = await _trainingService.GetSessionDetailAsync(_existingSessionId.Value);
        SessionName = name;

        foreach (var data in existingExercises)
            Exercises.Add(new ExerciseBlockViewModel(data.Exercise, data));

        IsLoading = false;
        OnPropertyChanged(nameof(WindowHeadline));
    }

    private string _sessionName = string.Empty;
    public string SessionName { get => _sessionName; set { _sessionName = value; OnPropertyChanged(); SaveCommand.RaiseCanExecuteChanged(); } }

    // ---------------- Übungssuche ----------------
    private string _searchText = string.Empty;
    public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); } }

    public ObservableCollection<Exercise> SearchResults { get; } = new();
    public RelayCommand SearchCommand { get; }
    public RelayCommand SelectExerciseCommand { get; }

    private bool _isSearching;
    public bool IsSearching { get => _isSearching; set { _isSearching = value; OnPropertyChanged(); } }

    private readonly ExerciseApiService _exerciseApiService = new();

    private async Task SearchAsync()
    {
        IsSearching = true;
        SearchResults.Clear();

        var localResults = await _trainingService.SearchExercisesAsync(SearchText);
        foreach (var r in localResults) SearchResults.Add(r);

        // Zusätzlich live bei wger.de suchen - Treffer, die wir lokal noch nicht kennen, werden als
        // Platzhalter (Id 0, WgerId gesetzt) angezeigt und erst bei Auswahl vollständig nachgeladen.
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var localNames = localResults.Select(e => e.Name.ToLowerInvariant()).ToHashSet();
            var onlineHits = await _exerciseApiService.SearchAsync(SearchText);
            foreach (var hit in onlineHits)
            {
                if (localNames.Contains(hit.Name.ToLowerInvariant())) continue;
                SearchResults.Add(new Exercise
                {
                    WgerId = hit.WgerId,
                    Name = hit.Name,
                    PrimaryMuscleGroup = string.IsNullOrWhiteSpace(hit.CategoryName) ? "Online-Datenbank" : hit.CategoryName,
                    Equipment = "wird beim Auswählen geladen..."
                });
            }
        }

        IsSearching = false;
    }

    private async Task SelectExerciseAsync(Exercise exercise)
    {
        // Online-Treffer (noch nicht lokal gespeichert, erkennbar an Id 0 + gesetzter WgerId):
        // erst die vollen Details nachladen und lokal speichern, bevor die Übung genutzt werden kann.
        if (exercise.Id == 0 && exercise.WgerId is not null)
        {
            IsSearching = true;
            var details = await _exerciseApiService.GetDetailsAsync(exercise.WgerId.Value);
            IsSearching = false;

            if (details is null) return; // Netzwerkfehler o. Ä. - lieber nichts hinzufügen als kaputte Daten

            exercise = await _trainingService.SaveImportedExerciseAsync(details);
        }

        Exercises.Add(new ExerciseBlockViewModel(exercise));
        SearchText = string.Empty;
        SearchResults.Clear();
    }

    public ObservableCollection<ExerciseBlockViewModel> Exercises { get; } = new();
    public RelayCommand RemoveExerciseCommand { get; }
    public bool HasExercises => Exercises.Count > 0;

    public RelayCommand SaveCommand { get; }

    private bool CanSave() => !string.IsNullOrWhiteSpace(SessionName) && Exercises.Count > 0;

    private async Task SaveAsync()
    {
        var exerciseInputs = Exercises.Select(block =>
        {
            if (block.IsSetsBased)
            {
                return new ExerciseInput(
                    block.ExerciseName, block.Exercise.InputType,
                    block.Sets.Select(s => new SetInput(s.WeightKg, s.Reps)).ToList(),
                    null, null, null, null, CardioLocation.Drinnen);
            }

            return new ExerciseInput(
                block.ExerciseName, block.Exercise.InputType,
                new List<SetInput>(),
                block.DurationMinutes, block.DistanceKm, block.ResistanceLevel, block.InclinePercent, block.Location);
        }).ToList();

        if (IsEditMode)
        {
            await _trainingService.ReplaceSessionExercisesAsync(_existingSessionId!.Value, SessionName, exerciseInputs);
        }
        else
        {
            await _trainingService.CreateSessionAsync(_userProfileId, _date, SessionName, exerciseInputs);
        }

        Saved?.Invoke();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
