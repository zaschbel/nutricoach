using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;
using NutriCoach.App.Services;
using NutriCoach.Maui.Drawables;

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
        }
    }

    public bool IsErnaehrungTab => SubTab == "Ernährung";
    public bool IsCardioTab => SubTab == "Cardio";
    public RelayCommand SelectSubTabCommand { get; }

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
        WeightChartDrawable = new LineChartDrawable(FillGapsWithAverage(weightRaw), Color.FromArgb("#1E9E5A"), Color.FromArgb("#1E9E5A"), "{0:0.0}", useTrendColors: true);
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
        CalorieChartDrawable = new LineChartDrawable(FillGapsWithAverage(BuildRaw(t => t.Kcal)), Color.FromArgb("#2D9AA5"), Color.FromArgb("#2D9AA5"), "{0:0}");

        var proteinValues = totals.Values.Select(t => t.Protein).Where(v => v > 0).ToList();
        ProteinAverageLabel = proteinValues.Count > 0 ? $"Ø {proteinValues.Average():0}g / {macroTargets.ProteinG:0}g" : $"– / {macroTargets.ProteinG:0}g";
        ProteinChartDrawable = new LineChartDrawable(FillGapsWithAverage(BuildRaw(t => t.Protein)), Color.FromArgb("#8AA65A"), Color.FromArgb("#8AA65A"), "{0:0}");

        var carbsValues = totals.Values.Select(t => t.Carbs).Where(v => v > 0).ToList();
        CarbsAverageLabel = carbsValues.Count > 0 ? $"Ø {carbsValues.Average():0}g / {macroTargets.CarbsG:0}g" : $"– / {macroTargets.CarbsG:0}g";
        CarbsChartDrawable = new LineChartDrawable(FillGapsWithAverage(BuildRaw(t => t.Carbs)), Color.FromArgb("#8FB13E"), Color.FromArgb("#8FB13E"), "{0:0}");

        var fatValues = totals.Values.Select(t => t.Fat).Where(v => v > 0).ToList();
        FatAverageLabel = fatValues.Count > 0 ? $"Ø {fatValues.Average():0}g / {macroTargets.FatG:0}g" : $"– / {macroTargets.FatG:0}g";
        FatChartDrawable = new LineChartDrawable(FillGapsWithAverage(BuildRaw(t => t.Fat)), Color.FromArgb("#B08840"), Color.FromArgb("#B08840"), "{0:0}");

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
