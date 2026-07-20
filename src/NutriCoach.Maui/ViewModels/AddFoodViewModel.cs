using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Storage;
using NutriCoach.App.Models;
using NutriCoach.App.Services;
using NutriCoach.Maui.Services;

namespace NutriCoach.App.ViewModels;

/// <summary>
/// Steuert das "Lebensmittel hinzufügen"-Formular. Drei Wege zum Ziel: Suche (lokal + online),
/// Barcode-Nummer eingeben, oder komplett manuell mit eigenen Nährwerten anlegen.
/// Bei Suche/Barcode öffnet ein Treffer die Produkt-Detailseite (Kreisdiagramm + Bewertung)
/// über den IDialogService; von dort kommt das Ergebnis zurück.
/// </summary>
public class AddFoodViewModel : INotifyPropertyChanged
{
    private readonly FoodLookupService _lookupService;
    private readonly NutritionDiaryService _diaryService;
    private readonly IDialogService _dialogService;
    private readonly int _userProfileId;
    private readonly FitnessGoal _goal;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<FoodItem, double>? FoodConfirmed;

    public MealType Meal { get; }
    public string MealLabel => Meal switch
    {
        MealType.Frühstück => "Frühstück",
        MealType.Mittagessen => "Mittagessen",
        MealType.Abendessen => "Abendessen",
        _ => "Snack"
    };

    public AddFoodViewModel(FoodLookupService lookupService, NutritionDiaryService diaryService, IDialogService dialogService,
        int userProfileId, MealType meal, FitnessGoal goal)
    {
        _lookupService = lookupService;
        _diaryService = diaryService;
        _dialogService = dialogService;
        _userProfileId = userProfileId;
        _goal = goal;
        Meal = meal;

        SearchCommand = new RelayCommand(async _ => await SearchAsync());
        SelectResultCommand = new RelayCommand(async param =>
        {
            if (param is FoodItem item) await OpenDetailAsync(item);
        });
        QuickAddCommand = new RelayCommand(param =>
        {
            if (param is RecentFoodDisplay recent) FoodConfirmed?.Invoke(recent.Food, recent.LastAmountGrams);
        });
        LookupBarcodeCommand = new RelayCommand(async _ => await LookupBarcodeAsync());
        CreateManualCommand = new RelayCommand(async _ => await CreateManualAsync(), _ => CanCreateManual());

        SearchResults.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasResults));
    }

    // ---------------- Zuletzt verwendet ----------------
    public ObservableCollection<RecentFoodDisplay> RecentFoods { get; } = new();
    public bool HasRecentFoods => RecentFoods.Count > 0;
    public RelayCommand QuickAddCommand { get; }

    public async Task LoadRecentFoodsAsync()
    {
        var recent = await _diaryService.GetRecentlyUsedFoodsAsync(_userProfileId);
        RecentFoods.Clear();
        foreach (var item in recent) RecentFoods.Add(item);
        OnPropertyChanged(nameof(HasRecentFoods));
    }

    // ---------------- Modus-Umschaltung ----------------
    private string _mode = "Suche";
    public string Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSearchMode));
            OnPropertyChanged(nameof(IsBarcodeMode));
            OnPropertyChanged(nameof(IsManualMode));
            OnPropertyChanged(nameof(IsPhotoMode));
        }
    }
    public bool IsSearchMode => Mode == "Suche";
    public bool IsBarcodeMode => Mode == "Barcode";
    public bool IsManualMode => Mode == "Manuell";
    public bool IsPhotoMode => Mode == "Foto";

    // ---------------- Foto-Erkennung ----------------
    private readonly GeminiAiService _aiService = new();

    private bool _isAnalyzingPhoto;
    public bool IsAnalyzingPhoto { get => _isAnalyzingPhoto; set { _isAnalyzingPhoto = value; OnPropertyChanged(); } }

    private string _photoErrorMessage = string.Empty;
    public string PhotoErrorMessage { get => _photoErrorMessage; set { _photoErrorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPhotoError)); } }
    public bool HasPhotoError => !string.IsNullOrWhiteSpace(PhotoErrorMessage);

    private bool _hasPhotoResult;
    public bool HasPhotoResult { get => _hasPhotoResult; set { _hasPhotoResult = value; OnPropertyChanged(); } }

    public string PhotoNutritionSummary => $"Pro 100g: {_photoKcalPer100:0} kcal, {_photoProteinPer100:0}g Eiweiß, {_photoCarbsPer100:0}g Kohlenhydrate, {_photoFatPer100:0}g Fett";
    private double _photoKcalPer100, _photoProteinPer100, _photoCarbsPer100, _photoFatPer100;

    private string? _pendingPhotoPath;

    public async Task AnalyzePhotoAsync(byte[] imageBytes, string mimeType)
    {
        IsAnalyzingPhoto = true;
        PhotoErrorMessage = "";
        HasPhotoResult = false;
        _pendingPhotoPath = null;

        var result = await _aiService.AnalyzeFoodPhotoAsync(imageBytes, mimeType);

        IsAnalyzingPhoto = false;

        if (result.ErrorMessage is not null)
        {
            PhotoErrorMessage = result.ErrorMessage;
            return;
        }

        // Foto dauerhaft lokal speichern, damit es später am Tagebuch-Eintrag gezeigt werden kann.
        var folder = Path.Combine(FileSystem.AppDataDirectory, "food_photos");
        Directory.CreateDirectory(folder);
        var fileName = $"food_{Guid.NewGuid():N}.jpg";
        var fullPath = Path.Combine(folder, fileName);
        await File.WriteAllBytesAsync(fullPath, imageBytes);
        _pendingPhotoPath = fullPath;

        ManualName = result.Name;
        _photoKcalPer100 = result.KcalPer100;
        _photoProteinPer100 = result.ProteinPer100;
        _photoCarbsPer100 = result.CarbsPer100;
        _photoFatPer100 = result.FatPer100;
        ManualKcal = result.KcalPer100;
        ManualProtein = result.ProteinPer100;
        ManualCarbs = result.CarbsPer100;
        ManualFat = result.FatPer100;
        OnPropertyChanged(nameof(PhotoNutritionSummary));
        HasPhotoResult = true;
    }

    // ---------------- Suche ----------------
    private string _searchText = string.Empty;
    public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); } }

    public ObservableCollection<FoodItem> SearchResults { get; } = new();

    private string? _searchStatusText;
    public string? SearchStatusText { get => _searchStatusText; set { _searchStatusText = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSearchStatus)); } }
    public bool HasSearchStatus => !string.IsNullOrWhiteSpace(SearchStatusText);
    public bool HasResults => SearchResults.Count > 0;

    public RelayCommand SearchCommand { get; }
    public RelayCommand SelectResultCommand { get; }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        SearchStatusText = "Suche läuft …";
        SearchResults.Clear();

        var local = await _lookupService.SearchLocalAsync(SearchText);
        foreach (var item in local) SearchResults.Add(item);

        var online = await _lookupService.SearchOnlineAsync(SearchText);
        var existingNames = SearchResults.Select(r => r.Name.ToLower()).ToHashSet();
        foreach (var item in online.Items)
        {
            if (!existingNames.Contains(item.Name.ToLower()))
                SearchResults.Add(item);
        }

        if (SearchResults.Count > 0)
        {
            SearchStatusText = null;
        }
        else if (!online.Success)
        {
            SearchStatusText = $"Online-Suche nicht erreichbar (evtl. kein Internet): {online.ErrorMessage}. " +
                                "Du kannst das Lebensmittel unten unter 'Manuell' anlegen.";
        }
        else
        {
            SearchStatusText = "Nichts gefunden. Du kannst es unten unter 'Manuell' anlegen.";
        }
    }

    // ---------------- Barcode ----------------
    private string _barcodeText = string.Empty;
    public string BarcodeText { get => _barcodeText; set { _barcodeText = value; OnPropertyChanged(); } }

    private string? _barcodeStatusText;
    public string? BarcodeStatusText { get => _barcodeStatusText; set { _barcodeStatusText = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasBarcodeStatus)); } }
    public bool HasBarcodeStatus => !string.IsNullOrWhiteSpace(BarcodeStatusText);

    public RelayCommand LookupBarcodeCommand { get; }

    private async Task LookupBarcodeAsync()
    {
        if (string.IsNullOrWhiteSpace(BarcodeText)) return;

        BarcodeStatusText = "Suche läuft …";
        var result = await _lookupService.LookupByBarcodeAsync(BarcodeText.Trim());

        if (result is null)
        {
            BarcodeStatusText = "Nichts gefunden unter dieser Nummer. Prüfe die Ziffern oder leg es manuell an.";
            return;
        }

        BarcodeStatusText = null;
        await OpenDetailAsync(result);
    }

    // ---------------- Produkt-Detailseite öffnen ----------------
    private async Task OpenDetailAsync(FoodItem item)
    {
        var result = await _dialogService.ShowFoodDetailAsync(item, _goal);

        if (result.Confirmed && result.Food is not null)
        {
            var food = result.Food.Id == 0
                ? await _lookupService.SaveToCacheAsync(result.Food)
                : result.Food;

            FoodConfirmed?.Invoke(food, result.AmountGrams);
        }
    }

    // ---------------- Manuelle Eingabe ----------------
    private string _manualName = string.Empty;
    public string ManualName { get => _manualName; set { _manualName = value; OnPropertyChanged(); CreateManualCommand.RaiseCanExecuteChanged(); } }

    private double _manualKcal;
    public double ManualKcal { get => _manualKcal; set { _manualKcal = value; OnPropertyChanged(); } }

    private double _manualProtein;
    public double ManualProtein { get => _manualProtein; set { _manualProtein = value; OnPropertyChanged(); } }

    private double _manualCarbs;
    public double ManualCarbs { get => _manualCarbs; set { _manualCarbs = value; OnPropertyChanged(); } }

    private double _manualFat;
    public double ManualFat { get => _manualFat; set { _manualFat = value; OnPropertyChanged(); } }

    private double _manualAmountGrams = 100;
    public double ManualAmountGrams { get => _manualAmountGrams; set { _manualAmountGrams = value; OnPropertyChanged(); CreateManualCommand.RaiseCanExecuteChanged(); } }

    public RelayCommand CreateManualCommand { get; }
    private bool CanCreateManual() => !string.IsNullOrWhiteSpace(ManualName) && ManualAmountGrams > 0;

    private async Task CreateManualAsync()
    {
        // Nutzer gibt Werte für DIE EINGEGEBENE MENGE ein (intuitiver), wir rechnen intern auf 100g um
        var factor = 100.0 / ManualAmountGrams;

        var food = await _lookupService.CreateManualAsync(new FoodItem
        {
            Name = ManualName,
            KcalPer100 = Math.Round(ManualKcal * factor, 1),
            ProteinPer100 = Math.Round(ManualProtein * factor, 1),
            CarbsPer100 = Math.Round(ManualCarbs * factor, 1),
            FatPer100 = Math.Round(ManualFat * factor, 1),
            PhotoPath = _pendingPhotoPath
        });

        FoodConfirmed?.Invoke(food, ManualAmountGrams);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
