using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Storage;
using NutriCoach.Maui.Services;

namespace NutriCoach.App.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly ReminderService _reminderService = new();

    public List<string> ThemeOptions { get; } = new() { "System", "Hell", "Dunkel" };

    private string _selectedTheme = Preferences.Default.Get(NutriCoach.Maui.App.ThemePreferenceKey, "System");
    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            _selectedTheme = value;
            OnPropertyChanged();
            Preferences.Default.Set(NutriCoach.Maui.App.ThemePreferenceKey, value);
            OnPropertyChanged(nameof(ThemeChangeHint));
        }
    }
    public string ThemeChangeHint => "Wird beim nächsten App-Start übernommen.";

    private string _apiKey = string.Empty;
    public string ApiKey { get => _apiKey; set { _apiKey = value; OnPropertyChanged(); } }

    private string _statusText = string.Empty;
    public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasStatus)); } }
    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusText);

    private bool _hasKeyStored;
    public bool HasKeyStored { get => _hasKeyStored; set { _hasKeyStored = value; OnPropertyChanged(); } }

    // ---------------- Erinnerungen ----------------
    private bool _breakfastReminder;
    public bool BreakfastReminder
    {
        get => _breakfastReminder;
        set { _breakfastReminder = value; OnPropertyChanged(); _ = _reminderService.SetBreakfastReminderAsync(value); }
    }

    private bool _lunchReminder;
    public bool LunchReminder
    {
        get => _lunchReminder;
        set { _lunchReminder = value; OnPropertyChanged(); _ = _reminderService.SetLunchReminderAsync(value); }
    }

    private bool _dinnerReminder;
    public bool DinnerReminder
    {
        get => _dinnerReminder;
        set { _dinnerReminder = value; OnPropertyChanged(); _ = _reminderService.SetDinnerReminderAsync(value); }
    }

    private bool _waterReminder;
    public bool WaterReminder
    {
        get => _waterReminder;
        set { _waterReminder = value; OnPropertyChanged(); _ = _reminderService.SetWaterReminderAsync(value); }
    }

    private bool _trainingReminder;
    public bool TrainingReminder
    {
        get => _trainingReminder;
        set { _trainingReminder = value; OnPropertyChanged(); _ = _reminderService.SetTrainingReminderAsync(value); }
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand RemoveCommand { get; }

    public SettingsViewModel()
    {
        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        RemoveCommand = new RelayCommand(_ => Remove());
        _ = LoadAsync();

        _breakfastReminder = _reminderService.IsEnabled("breakfast");
        _lunchReminder = _reminderService.IsEnabled("lunch");
        _dinnerReminder = _reminderService.IsEnabled("dinner");
        _waterReminder = _reminderService.IsEnabled("water");
        _trainingReminder = _reminderService.IsEnabled("training");
    }

    private async Task LoadAsync()
    {
        var existing = await GeminiAiService.GetApiKeyAsync();
        HasKeyStored = !string.IsNullOrWhiteSpace(existing);
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusText = "Bitte einen API-Key eingeben.";
            return;
        }

        await GeminiAiService.SetApiKeyAsync(ApiKey.Trim());
        HasKeyStored = true;
        ApiKey = string.Empty;
        StatusText = "Gespeichert. Die KI-Karte auf dem Dashboard nutzt ihn ab jetzt.";
    }

    private void Remove()
    {
        GeminiAiService.ClearApiKey();
        HasKeyStored = false;
        StatusText = "API-Key entfernt.";
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
