using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NutriCoach.App.Models;
using NutriCoach.App.Services;

namespace NutriCoach.App.ViewModels;

/// <summary>Erlaubt das nachträgliche Bearbeiten der bei der Ersteinrichtung festgelegten Angaben.</summary>
public class EditProfileViewModel : INotifyPropertyChanged
{
    private readonly UserProfileService _profileService;
    private readonly int _profileId;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Saved;

    public EditProfileViewModel(UserProfileService profileService, UserProfile profile, double currentWeightKg)
    {
        _profileService = profileService;
        _profileId = profile.Id;

        _name = profile.Name;
        _birthDate = profile.BirthDate.ToDateTime(TimeOnly.MinValue);
        _gender = profile.Gender;
        _heightCm = profile.HeightCm;
        _goal = profile.Goal;
        _activityLevel = profile.ActivityLevel;
        _jobActivity = profile.JobActivity;
        _experience = profile.Experience;
        _profilePicturePath = profile.ProfilePicturePath;
        _currentWeightKg = currentWeightKg;

        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
    }

    /// <summary>Wie in MainViewModel: sofort speichern, damit's mit dem Ernährungs-Reiter synchron bleibt.</summary>
    private double _currentWeightKg;
    public double CurrentWeightKg
    {
        get => _currentWeightKg;
        set
        {
            if (_currentWeightKg == value) return;
            _currentWeightKg = value;
            OnPropertyChanged();
            _ = _profileService.SetWeightForDateAsync(_profileId, DateOnly.FromDateTime(DateTime.Today), value);
        }
    }

    private string? _profilePicturePath;
    public string? ProfilePicturePath { get => _profilePicturePath; set { _profilePicturePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasProfilePicture)); } }
    public bool HasProfilePicture => !string.IsNullOrWhiteSpace(ProfilePicturePath);

    public async Task SetProfilePictureAsync(Stream imageStream)
    {
        ProfilePicturePath = await _profileService.SetProfilePictureAsync(_profileId, imageStream);
    }

    public List<Gender> GenderOptions { get; } = Enum.GetValues<Gender>().ToList();
    public List<FitnessGoal> GoalOptions { get; } = Enum.GetValues<FitnessGoal>().ToList();
    public List<ActivityLevel> ActivityOptions { get; } = Enum.GetValues<ActivityLevel>().ToList();
    public List<DailyJobActivity> JobActivityOptions { get; } = Enum.GetValues<DailyJobActivity>().ToList();
    public List<ExperienceLevel> ExperienceOptions { get; } = Enum.GetValues<ExperienceLevel>().ToList();

    private string _name;
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); SaveCommand.RaiseCanExecuteChanged(); } }

    private DateTime _birthDate;
    public DateTime BirthDate { get => _birthDate; set { _birthDate = value; OnPropertyChanged(); } }

    private Gender _gender;
    public Gender Gender { get => _gender; set { _gender = value; OnPropertyChanged(); } }

    private double _heightCm;
    public double HeightCm { get => _heightCm; set { _heightCm = value; OnPropertyChanged(); SaveCommand.RaiseCanExecuteChanged(); } }

    private FitnessGoal _goal;
    public FitnessGoal Goal { get => _goal; set { _goal = value; OnPropertyChanged(); } }

    private ActivityLevel _activityLevel;
    public ActivityLevel ActivityLevel { get => _activityLevel; set { _activityLevel = value; OnPropertyChanged(); } }

    private DailyJobActivity _jobActivity;
    public DailyJobActivity JobActivity { get => _jobActivity; set { _jobActivity = value; OnPropertyChanged(); } }

    private ExperienceLevel _experience;
    public ExperienceLevel Experience { get => _experience; set { _experience = value; OnPropertyChanged(); } }

    private string _statusText = string.Empty;
    public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasStatus)); } }
    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusText);

    public RelayCommand SaveCommand { get; }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name) && HeightCm > 0;

    private async Task SaveAsync()
    {
        await _profileService.UpdateProfileDetailsAsync(_profileId, Name, DateOnly.FromDateTime(BirthDate), Gender,
            HeightCm, Goal, ActivityLevel, JobActivity, Experience);
        StatusText = "Gespeichert.";
        Saved?.Invoke();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
