using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NutriCoach.App.Models;
using NutriCoach.App.Services;

namespace NutriCoach.App.ViewModels;

/// <summary>Eine Trainingsvorlage zur Anzeige in der Verwaltungsliste, inkl. ihrer Übungsnamen in Reihenfolge.</summary>
public record TemplateDisplay(int Id, string Name, List<string> ExerciseNames)
{
    public string ExerciseNamesLabel => ExerciseNames.Count > 0 ? string.Join(", ", ExerciseNames) : "Keine Übungen";
}

/// <summary>
/// Steuert die Trainingsvorlagen-Verwaltung: Liste bestehender Vorlagen (verwenden/löschen) sowie
/// ein einklappbares Formular zum Anlegen einer neuen Vorlage (Name + Übungen aus der bestehenden
/// Übungsbibliothek per Suche auswählen - gleiches Grundmuster wie die Übungssuche in AddTrainingPage,
/// nur ohne Sätze/Gewicht, da eine Vorlage nur die feste Übungsliste speichert).
/// </summary>
public class ManageTemplatesViewModel : INotifyPropertyChanged
{
    private readonly WorkoutTemplateService _templateService;
    private readonly TrainingDiaryService _trainingService;
    private readonly int _userProfileId;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Wird ausgelöst, wenn der Nutzer eine Vorlage tatsächlich "verwenden" möchte - liefert deren Übungsnamen.</summary>
    public event Action<List<string>>? TemplateSelected;

    public ObservableCollection<TemplateDisplay> Templates { get; } = new();
    public bool HasTemplates => Templates.Count > 0;

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

    // ---------------- Neue Vorlage anlegen ----------------
    private bool _showCreatePanel;
    public bool ShowCreatePanel { get => _showCreatePanel; set { _showCreatePanel = value; OnPropertyChanged(); } }

    private string _newTemplateName = string.Empty;
    public string NewTemplateName
    {
        get => _newTemplateName;
        set { _newTemplateName = value; OnPropertyChanged(); SaveTemplateCommand.RaiseCanExecuteChanged(); }
    }

    private string _exerciseSearchText = string.Empty;
    public string ExerciseSearchText { get => _exerciseSearchText; set { _exerciseSearchText = value; OnPropertyChanged(); } }

    public ObservableCollection<Exercise> ExerciseSearchResults { get; } = new();
    public ObservableCollection<string> SelectedExerciseNames { get; } = new();
    public bool HasSelectedExercises => SelectedExerciseNames.Count > 0;

    private bool _isSearchingExercises;
    public bool IsSearchingExercises { get => _isSearchingExercises; set { _isSearchingExercises = value; OnPropertyChanged(); } }

    public RelayCommand ToggleCreatePanelCommand { get; }
    public RelayCommand SearchExercisesCommand { get; }
    public RelayCommand AddExerciseNameCommand { get; }
    public RelayCommand RemoveExerciseNameCommand { get; }
    public RelayCommand SaveTemplateCommand { get; }
    public RelayCommand UseTemplateCommand { get; }
    public RelayCommand DeleteTemplateCommand { get; }

    public ManageTemplatesViewModel(WorkoutTemplateService templateService, TrainingDiaryService trainingService, int userProfileId)
    {
        _templateService = templateService;
        _trainingService = trainingService;
        _userProfileId = userProfileId;

        ToggleCreatePanelCommand = new RelayCommand(_ => ShowCreatePanel = !ShowCreatePanel);
        SearchExercisesCommand = new RelayCommand(async _ => await SearchExercisesAsync());
        AddExerciseNameCommand = new RelayCommand(param =>
        {
            if (param is Exercise exercise && !SelectedExerciseNames.Contains(exercise.Name))
            {
                SelectedExerciseNames.Add(exercise.Name);
                OnPropertyChanged(nameof(HasSelectedExercises));
                SaveTemplateCommand.RaiseCanExecuteChanged();
            }
        });
        RemoveExerciseNameCommand = new RelayCommand(param =>
        {
            if (param is string name)
            {
                SelectedExerciseNames.Remove(name);
                OnPropertyChanged(nameof(HasSelectedExercises));
                SaveTemplateCommand.RaiseCanExecuteChanged();
            }
        });
        SaveTemplateCommand = new RelayCommand(async _ => await SaveTemplateAsync(), _ => CanSaveTemplate());
        UseTemplateCommand = new RelayCommand(param =>
        {
            if (param is TemplateDisplay template) TemplateSelected?.Invoke(template.ExerciseNames);
        });
        DeleteTemplateCommand = new RelayCommand(async param =>
        {
            if (param is TemplateDisplay template) await DeleteTemplateAsync(template);
        });
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var templates = await _templateService.GetTemplatesAsync(_userProfileId);

            Templates.Clear();
            foreach (var t in templates)
                Templates.Add(new TemplateDisplay(t.Id, t.Name, t.Exercises.Select(e => e.ExerciseName).ToList()));
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Vorlagen konnten nicht geladen werden", ex.ToString(), "OK");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasTemplates));
        }
    }

    private bool CanSaveTemplate() => !string.IsNullOrWhiteSpace(NewTemplateName) && SelectedExerciseNames.Count > 0;

    private async Task SearchExercisesAsync()
    {
        IsSearchingExercises = true;
        ExerciseSearchResults.Clear();

        var results = await _trainingService.SearchExercisesAsync(ExerciseSearchText);
        foreach (var r in results) ExerciseSearchResults.Add(r);

        IsSearchingExercises = false;
    }

    private async Task SaveTemplateAsync()
    {
        try
        {
            await _templateService.CreateTemplateAsync(_userProfileId, NewTemplateName, SelectedExerciseNames.ToList());
        }
        catch (Exception ex)
        {
            // Nur bei echtem Erfolg das Formular zuruecksetzen/schliessen - vorher wurde das
            // bedingungslos gemacht, wodurch ein stiller Speicherfehler aussah wie ein Erfolg.
            await Application.Current!.MainPage!.DisplayAlert("Trainingsvorlage konnte nicht gespeichert werden", ex.ToString(), "OK");
            return;
        }

        NewTemplateName = string.Empty;
        SelectedExerciseNames.Clear();
        ExerciseSearchResults.Clear();
        ExerciseSearchText = string.Empty;
        ShowCreatePanel = false;
        OnPropertyChanged(nameof(HasSelectedExercises));

        await LoadAsync();
    }

    private async Task DeleteTemplateAsync(TemplateDisplay template)
    {
        await _templateService.DeleteTemplateAsync(template.Id);
        await LoadAsync();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
