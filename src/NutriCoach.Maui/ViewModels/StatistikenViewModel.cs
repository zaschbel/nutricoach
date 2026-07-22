using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using NutriCoach.App.Services;
using NutriCoach.Maui.Drawables;
using NutriCoach.Maui.Services;

namespace NutriCoach.App.ViewModels;

/// <summary>
/// Steuert den Statistiken-Reiter: drei Unterreiter (Training/Ernährung/Cardio), jeweils mit
/// Wochen-Karten (Gewichtsverlauf als Linie, Kalorien/Proteine/Kohlenhydrate/Fette als Balken
/// mit Ziel-Linie) - nach der vom Nutzer vorgegebenen Bildschirmvorlage.
/// </summary>
public class StatistikenViewModel : INotifyPropertyChanged
{
    private readonly UserProfileService _profileService;
    private readonly NutritionDiaryService _diaryService;
    private readonly TrainingDiaryService _trainingService;
    private readonly GeminiAiService _aiService = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public StatistikenViewModel(UserProfileService profileService, NutritionDiaryService diaryService, TrainingDiaryService trainingService)
    {
        _profileService = profileService;
        _diaryService = diaryService;
        _trainingService = trainingService;

        SelectSubTabCommand = new RelayCommand(param =>
        {
            if (param is string tab) SubTab = tab;
        });

        ResetFutureBodyCommand = new RelayCommand(_ =>
        {
            HasFutureImage = false;
            FutureImageErrorMessage = "";
        });

        LoadFutureBodyState();
    }

    private string _subTab = "Ernährung";
    public string SubTab
    {
        get => _subTab;
        set
        {
            _subTab = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsErnaehrungTab));
            OnPropertyChanged(nameof(IsCardioTab));
            OnPropertyChanged(nameof(IsZukunftsbildTab));
        }
    }

    public bool IsErnaehrungTab => SubTab == "Ernährung";
    public bool IsCardioTab => SubTab == "Cardio";
    public bool IsZukunftsbildTab => SubTab == "Zukunftsbild";
    public RelayCommand SelectSubTabCommand { get; }

    // ---------------- Zukunftsbild (KI-Vorschau "du in 6 Monaten") ----------------
    private const string FutureBodyOriginalFileName = "future_body_original.jpg";
    private const string FutureBodyGeneratedFileName = "future_body_generated.jpg";
    private const string FutureBodyGeneratedAtPrefKey = "future_body_generated_at";
    private const string FutureBodyProjectedKgChangePrefKey = "future_body_projected_kg_change";

    private static string FutureBodyOriginalPath => Path.Combine(FileSystem.AppDataDirectory, FutureBodyOriginalFileName);
    private static string FutureBodyGeneratedPath => Path.Combine(FileSystem.AppDataDirectory, FutureBodyGeneratedFileName);

    private bool _hasFutureImage;
    public bool HasFutureImage
    {
        get => _hasFutureImage;
        set { _hasFutureImage = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsFutureBodySetupVisible)); OnPropertyChanged(nameof(IsFutureBodyComparisonVisible)); }
    }

    private bool _isGeneratingFutureImage;
    public bool IsGeneratingFutureImage
    {
        get => _isGeneratingFutureImage;
        set { _isGeneratingFutureImage = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsFutureBodySetupVisible)); OnPropertyChanged(nameof(IsFutureBodyComparisonVisible)); }
    }

    /// <summary>Setup-Ansicht (Erklärung + Foto aufnehmen/wählen) - solange noch kein Bild da ist und gerade nichts läuft.</summary>
    public bool IsFutureBodySetupVisible => !HasFutureImage && !IsGeneratingFutureImage;
    /// <summary>Vergleichs-Ansicht (Vorher/Nachher-Schieberegler) - sobald ein Bild vorliegt und gerade nichts (neu) läuft.</summary>
    public bool IsFutureBodyComparisonVisible => HasFutureImage && !IsGeneratingFutureImage;

    private string _futureImageErrorMessage = "";
    public string FutureImageErrorMessage { get => _futureImageErrorMessage; set { _futureImageErrorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasFutureImageError)); } }
    public bool HasFutureImageError => !string.IsNullOrWhiteSpace(FutureImageErrorMessage);

    private string _originalImagePath = "";
    public string OriginalImagePath { get => _originalImagePath; set { _originalImagePath = value; OnPropertyChanged(); } }

    private string _generatedImagePath = "";
    public string GeneratedImagePath { get => _generatedImagePath; set { _generatedImagePath = value; OnPropertyChanged(); } }

    private string _projectedChangeLabel = "";
    public string ProjectedChangeLabel { get => _projectedChangeLabel; set { _projectedChangeLabel = value; OnPropertyChanged(); } }

    public RelayCommand ResetFutureBodyCommand { get; }

    /// <summary>Prüft beim Start, ob bereits ein zuvor generiertes Zukunftsbild auf dem Gerät liegt (Dateien +
    /// Preferences), damit man es direkt sieht statt jedes Mal neu ein Foto aufnehmen zu müssen.</summary>
    private void LoadFutureBodyState()
    {
        var hasFiles = File.Exists(FutureBodyOriginalPath) && File.Exists(FutureBodyGeneratedPath);
        if (!hasFiles)
        {
            HasFutureImage = false;
            return;
        }

        OriginalImagePath = FutureBodyOriginalPath;
        GeneratedImagePath = FutureBodyGeneratedPath;
        var projectedKgChange = Preferences.Default.Get(FutureBodyProjectedKgChangePrefKey, 0.0);
        ProjectedChangeLabel = BuildProjectedChangeLabel(projectedKgChange);
        HasFutureImage = true;
    }

    private static string BuildProjectedChangeLabel(double projectedKgChange)
    {
        if (Math.Abs(projectedKgChange) < 0.1) return "Geschätzte Veränderung in 6 Monaten: keine nennenswerte Veränderung";
        var sign = projectedKgChange > 0 ? "+" : "";
        return $"Geschätzte Veränderung in 6 Monaten: {sign}{projectedKgChange:0.0} kg";
    }

    /// <summary>
    /// Berechnet aus dem Profil (TDEE vs. Kalorienziel) eine realistische Gewichtsprognose für 6 Monate und
    /// schickt das aufgenommene/gewählte Foto zusammen mit dieser Prognose an Gemini, um ein bearbeitetes
    /// "Zukunftsbild" zu erzeugen. Wird NUR durch einen expliziten Button-Tap in StatistikenPage ausgelöst -
    /// kein automatischer Start beim App-Start (Absicht, siehe Lehren aus dem Schrittzähler-Feature).
    /// </summary>
    public async Task GenerateFutureBodyImageAsync(byte[] imageBytes, string mimeType)
    {
        IsGeneratingFutureImage = true;
        FutureImageErrorMessage = "";

        try
        {
            var profile = await _profileService.GetActiveProfileAsync();
            if (profile is null)
            {
                FutureImageErrorMessage = "Kein aktives Profil gefunden. Bitte zuerst ein Profil anlegen.";
                return;
            }

            var tdee = BmrCalculator.CalculateTdee(profile);
            var calorieTarget = BmrCalculator.CalculateCalorieTarget(profile);
            var dailyDeficitOrSurplus = calorieTarget - tdee;
            // 182 Tage ≈ 6 Monate, ~7700 kcal pro kg Körperfett (gleiche Faustregel wie in
            // BmrCalculator.CalculateCalorieTarget schon implizit verwendet).
            var projectedKgChange = Math.Clamp(dailyDeficitOrSurplus * 182 / 7700.0, -15, 15);

            var direction = projectedKgChange < -0.1 ? "healthy weight loss" : projectedKgChange > 0.1 ? "healthy weight/muscle gain" : "body recomposition";
            var absChange = Math.Abs(projectedKgChange);
            var prompt =
                "This is a real photo of a person. Generate a photorealistic edited version of the SAME photo " +
                "showing how this person's body might realistically look after 6 months of consistent healthy " +
                $"eating and exercise, resulting in approximately {(projectedKgChange >= 0 ? "+" : "-")}{absChange:0.0} kg change in body weight. " +
                "Keep the exact same face, identity, pose, camera angle, lighting, background, and clothing style - " +
                $"only adjust visible body composition (fat/muscle) in a realistic, non-exaggerated way appropriate for a {direction} over 6 months. " +
                "Do not add any text, watermark, or overlay to the image.";

            var result = await _aiService.GenerateFutureBodyImageAsync(imageBytes, mimeType, prompt);

            if (result.Error is not null)
            {
                FutureImageErrorMessage = result.Error;
                return;
            }
            if (result.ImageBytes is null || result.ImageBytes.Length == 0)
            {
                FutureImageErrorMessage = "Die KI hat kein verwertbares Bild zurückgegeben.";
                return;
            }

            await File.WriteAllBytesAsync(FutureBodyOriginalPath, imageBytes);
            await File.WriteAllBytesAsync(FutureBodyGeneratedPath, result.ImageBytes);

            Preferences.Default.Set(FutureBodyGeneratedAtPrefKey, DateTime.UtcNow.ToString("O"));
            Preferences.Default.Set(FutureBodyProjectedKgChangePrefKey, projectedKgChange);

            // Pfade kurz auf "" setzen und neu zuweisen, damit die Image-Controls das gerade
            // überschriebene Bild neu laden statt (bei identischem Dateinamen) das alte gecachte Bild
            // weiter anzuzeigen.
            OriginalImagePath = "";
            GeneratedImagePath = "";
            OriginalImagePath = FutureBodyOriginalPath;
            GeneratedImagePath = FutureBodyGeneratedPath;

            ProjectedChangeLabel = BuildProjectedChangeLabel(projectedKgChange);
            HasFutureImage = true;
        }
        catch (Exception ex)
        {
            FutureImageErrorMessage = $"Fehler bei der Zukunftsbild-Erstellung: {ex}";
        }
        finally
        {
            IsGeneratingFutureImage = false;
        }
    }

    // ---------------- Körpergewicht ----------------
    private string _weightCurrentLabel = "";
    public string WeightCurrentLabel { get => _weightCurrentLabel; set { _weightCurrentLabel = value; OnPropertyChanged(); } }

    private string _weightChangeLabel = "";
    public string WeightChangeLabel { get => _weightChangeLabel; set { _weightChangeLabel = value; OnPropertyChanged(); } }

    private IDrawable _weightChartDrawable = new LineChartDrawable(new(), Colors.Green, Colors.Green);
    public IDrawable WeightChartDrawable { get => _weightChartDrawable; set { _weightChartDrawable = value; OnPropertyChanged(); } }

    // ---------------- Kalorien ----------------
    private string _calorieAverageLabel = "";
    public string CalorieAverageLabel { get => _calorieAverageLabel; set { _calorieAverageLabel = value; OnPropertyChanged(); } }
    private IDrawable _calorieChartDrawable = EmptyLine();
    public IDrawable CalorieChartDrawable { get => _calorieChartDrawable; set { _calorieChartDrawable = value; OnPropertyChanged(); } }

    // ---------------- Proteine ----------------
    private string _proteinAverageLabel = "";
    public string ProteinAverageLabel { get => _proteinAverageLabel; set { _proteinAverageLabel = value; OnPropertyChanged(); } }
    private IDrawable _proteinChartDrawable = EmptyLine();
    public IDrawable ProteinChartDrawable { get => _proteinChartDrawable; set { _proteinChartDrawable = value; OnPropertyChanged(); } }

    // ---------------- Kohlenhydrate ----------------
    private string _carbsAverageLabel = "";
    public string CarbsAverageLabel { get => _carbsAverageLabel; set { _carbsAverageLabel = value; OnPropertyChanged(); } }
    private IDrawable _carbsChartDrawable = EmptyLine();
    public IDrawable CarbsChartDrawable { get => _carbsChartDrawable; set { _carbsChartDrawable = value; OnPropertyChanged(); } }

    // ---------------- Fette ----------------
    private string _fatAverageLabel = "";
    public string FatAverageLabel { get => _fatAverageLabel; set { _fatAverageLabel = value; OnPropertyChanged(); } }
    private IDrawable _fatChartDrawable = EmptyLine();
    public IDrawable FatChartDrawable { get => _fatChartDrawable; set { _fatChartDrawable = value; OnPropertyChanged(); } }

    // ---------------- Cardio ----------------
    private string _cardioAverageLabel = "";
    public string CardioAverageLabel { get => _cardioAverageLabel; set { _cardioAverageLabel = value; OnPropertyChanged(); } }
    private IDrawable _cardioChartDrawable = EmptyLine();
    public IDrawable CardioChartDrawable { get => _cardioChartDrawable; set { _cardioChartDrawable = value; OnPropertyChanged(); } }

    private static IDrawable EmptyLine() => new LineChartDrawable(new(), Colors.Gray, Colors.Gray);

    /// <summary>Füllt Tage ohne eigenen Wert mit dem Durchschnitt der Tage, die einen Wert haben - stabil und
    /// nachvollziehbar (ändert sich nur sanft mit neuen echten Daten), im Gegensatz zum früheren
    /// Verhalten, das einen einzelnen, sich ständig ändernden Wert einfach durchgereicht hat.</summary>
    private static List<LinePoint> FillGapsWithAverage(List<(string DayAbbrev, string DayNumber, double? Value)> raw)
    {
        var known = raw.Where(p => p.Value.HasValue).Select(p => p.Value!.Value).ToList();
        var average = known.Count > 0 ? known.Average() : 0;
        return raw.Select(p => new LinePoint(p.DayAbbrev, p.DayNumber, p.Value ?? average)).ToList();
    }

    public async Task LoadAsync()
    {
        var profile = await _profileService.GetActiveProfileAsync();
        if (profile is null) return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = today.AddDays(-6);
        string[] dayAbbrevs = { "Mo", "Di", "Mi", "Do", "Fr", "Sa", "So" };

        // ---------------- Gewicht ----------------
        var weightHistory = await _profileService.GetWeightHistoryAsync(profile.Id, 7);
        var weightRaw = weightHistory.Select(w => (
            dayAbbrevs[(int)w.Date.DayOfWeek == 0 ? 6 : (int)w.Date.DayOfWeek - 1],
            w.Date.Day.ToString(), w.WeightKg)).ToList();
        WeightChartDrawable = new LineChartDrawable(FillGapsWithAverage(weightRaw), Color.FromArgb("#1E9E5A"), Color.FromArgb("#1E9E5A"), "{0:0.0}", useTrendColors: true, targetValue: profile.TargetWeightKg);
        WeightCurrentLabel = $"{profile.CurrentWeightKg:0.0} kg";
        var change = await _profileService.GetWeightChangeAsync(profile.Id, 7);
        WeightChangeLabel = change is null ? "" : $"{(change > 0 ? "+" : "")}{change:0.0} kg";

        // ---------------- Ernährungswerte ----------------
        var totals = await _diaryService.GetDailyTotalsForRangeAsync(profile.Id, start, today);
        var kcalTarget = BmrCalculator.CalculateCalorieTarget(profile);
        var macroTargets = BmrCalculator.CalculateMacroTargets(profile);

        // Tage ohne Eintrag (Wert 0) gelten als "keine Daten" und werden mit dem Durchschnitt aufgefüllt.
        List<(string, string, double?)> BuildRaw(Func<DailyTotals, double> selector) => totals
            .OrderBy(kv => kv.Key)
            .Select(kv => (
                dayAbbrevs[(int)kv.Key.DayOfWeek == 0 ? 6 : (int)kv.Key.DayOfWeek - 1],
                kv.Key.Day.ToString(), selector(kv.Value) > 0 ? (double?)selector(kv.Value) : null))
            .ToList();

        var kcalValues = totals.Values.Select(t => t.Kcal).Where(v => v > 0).ToList();
        CalorieAverageLabel = kcalValues.Count > 0 ? $"Ø {kcalValues.Average():0} kcal / {kcalTarget:0} kcal" : $"– / {kcalTarget:0} kcal";
        CalorieChartDrawable = new LineChartDrawable(FillGapsWithAverage(BuildRaw(t => t.Kcal)), Color.FromArgb("#2D9AA5"), Color.FromArgb("#2D9AA5"), "{0:0}", targetValue: kcalTarget);

        var proteinValues = totals.Values.Select(t => t.Protein).Where(v => v > 0).ToList();
        ProteinAverageLabel = proteinValues.Count > 0 ? $"Ø {proteinValues.Average():0}g / {macroTargets.ProteinG:0}g" : $"– / {macroTargets.ProteinG:0}g";
        ProteinChartDrawable = new LineChartDrawable(FillGapsWithAverage(BuildRaw(t => t.Protein)), Color.FromArgb("#8AA65A"), Color.FromArgb("#8AA65A"), "{0:0}", targetValue: macroTargets.ProteinG);

        var carbsValues = totals.Values.Select(t => t.Carbs).Where(v => v > 0).ToList();
        CarbsAverageLabel = carbsValues.Count > 0 ? $"Ø {carbsValues.Average():0}g / {macroTargets.CarbsG:0}g" : $"– / {macroTargets.CarbsG:0}g";
        CarbsChartDrawable = new LineChartDrawable(FillGapsWithAverage(BuildRaw(t => t.Carbs)), Color.FromArgb("#8FB13E"), Color.FromArgb("#8FB13E"), "{0:0}", targetValue: macroTargets.CarbsG);

        var fatValues = totals.Values.Select(t => t.Fat).Where(v => v > 0).ToList();
        FatAverageLabel = fatValues.Count > 0 ? $"Ø {fatValues.Average():0}g / {macroTargets.FatG:0}g" : $"– / {macroTargets.FatG:0}g";
        FatChartDrawable = new LineChartDrawable(FillGapsWithAverage(BuildRaw(t => t.Fat)), Color.FromArgb("#B08840"), Color.FromArgb("#B08840"), "{0:0}", targetValue: macroTargets.FatG);

        // ---------------- Cardio ----------------
        var cardioMinutes = await _trainingService.GetDailyCardioMinutesForRangeAsync(profile.Id, start, today);
        var cardioValues = cardioMinutes.Values.Where(v => v > 0).ToList();
        CardioAverageLabel = cardioValues.Count > 0 ? $"Ø {cardioValues.Average():0} Min / Tag" : "Noch keine Cardio-Einheiten";
        var cardioRaw = cardioMinutes.OrderBy(kv => kv.Key)
            .Select(kv => (
                dayAbbrevs[(int)kv.Key.DayOfWeek == 0 ? 6 : (int)kv.Key.DayOfWeek - 1],
                kv.Key.Day.ToString(), kv.Value > 0 ? (double?)kv.Value : null))
            .ToList();
        CardioChartDrawable = new LineChartDrawable(FillGapsWithAverage(cardioRaw), Color.FromArgb("#8F6E53"), Color.FromArgb("#8F6E53"), "{0:0}");
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
