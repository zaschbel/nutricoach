using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;
using NutriCoach.App.Models;
using NutriCoach.App.Services;
using NutriCoach.Maui.Drawables;

namespace NutriCoach.App.ViewModels;

/// <summary>
/// Steuert das Detail-Fenster für ein einzelnes Produkt: Makro-Kreisdiagramm, Mengeneingabe
/// mit Live-Neuberechnung, und eine zielbezogene Einschätzung (gut/neutral/ungünstig + Grund + Alternative).
/// </summary>
public class FoodDetailViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<FoodItem, double>? Confirmed;
    public event Action? DeleteRequested;

    public FoodItem Food { get; }
    private readonly FitnessGoal _goal;

    public bool IsEditMode { get; }
    public string ConfirmButtonText => IsEditMode ? "Menge aktualisieren" : "Zum Tagebuch hinzufügen";

    public string Icon => FoodIconHelper.GetIcon(Food.Name);
    public string Name => Food.Name;
    public string? Brand => Food.Brand;
    public bool HasBrand => !string.IsNullOrWhiteSpace(Brand);

    public ObservableCollection<PieSlice> PieSlices { get; } = new();
    public IDrawable ChartDrawable { get; private set; } = new PieChartDrawable(new List<(double, Color)>());
    public IDrawable RingDrawable => new MacroRingDrawable(Protein, Carbs, Fat);

    public FoodDetailViewModel(FoodItem food, FitnessGoal goal, double? initialAmountGrams = null, bool isEditMode = false)
    {
        Food = food;
        _goal = goal;
        IsEditMode = isEditMode;
        if (initialAmountGrams is > 0) _amountGrams = initialAmountGrams.Value;

        ConfirmCommand = new RelayCommand(_ => Confirmed?.Invoke(Food, AmountGrams));
        DeleteCommand = new RelayCommand(_ => DeleteRequested?.Invoke());

        foreach (var slice in PieChartHelper.BuildMacroSlices(food.CarbsPer100, food.ProteinPer100, food.FatPer100))
            PieSlices.Add(slice);

        ChartDrawable = new PieChartDrawable(
            PieSlices.Select(s => (s.Percent, (Color)Color.FromArgb(s.Color))).ToList());

        UpdateAssessment();
    }

    private double _amountGrams = 100;
    public double AmountGrams
    {
        get => _amountGrams;
        set
        {
            if (value <= 0) return;
            _amountGrams = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Kcal));
            OnPropertyChanged(nameof(Protein));
            OnPropertyChanged(nameof(Carbs));
            OnPropertyChanged(nameof(Sugar));
            OnPropertyChanged(nameof(Fat));
            OnPropertyChanged(nameof(SaturatedFat));
            OnPropertyChanged(nameof(Fiber));
            OnPropertyChanged(nameof(Salt));
            OnPropertyChanged(nameof(RingDrawable));
            UpdateAssessment();
        }
    }

    private double Factor => AmountGrams / 100.0;
    public double Kcal => Math.Round(Food.KcalPer100 * Factor, 0);
    public double Protein => Math.Round(Food.ProteinPer100 * Factor, 1);
    public double Carbs => Math.Round(Food.CarbsPer100 * Factor, 1);
    public double Sugar => Math.Round(Food.SugarPer100 * Factor, 1);
    public double Fat => Math.Round(Food.FatPer100 * Factor, 1);
    public double SaturatedFat => Math.Round(Food.SaturatedFatPer100 * Factor, 1);
    public double Fiber => Math.Round(Food.FiberPer100 * Factor, 1);
    public double Salt => Math.Round(Food.SaltPer100 * Factor, 2);

    // Werte pro 100g (unverändert, zum direkten Vergleich neben der eingestellten Menge)
    public double KcalPer100 => Food.KcalPer100;
    public double CarbsPer100 => Food.CarbsPer100;
    public double SugarPer100 => Food.SugarPer100;
    public double ProteinPer100 => Food.ProteinPer100;
    public double FatPer100 => Food.FatPer100;
    public double SaturatedFatPer100 => Food.SaturatedFatPer100;
    public double FiberPer100 => Food.FiberPer100;
    public double SaltPer100 => Food.SaltPer100;

    private string _ratingIcon = "➖";
    public string RatingIcon { get => _ratingIcon; set { _ratingIcon = value; OnPropertyChanged(); } }

    private string _rating = string.Empty;
    public string Rating { get => _rating; set { _rating = value; OnPropertyChanged(); } }

    private string _explanation = string.Empty;
    public string Explanation { get => _explanation; set { _explanation = value; OnPropertyChanged(); } }

    private string? _alternativeHint;
    public string? AlternativeHint { get => _alternativeHint; set { _alternativeHint = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasAlternativeHint)); } }
    public bool HasAlternativeHint => !string.IsNullOrWhiteSpace(AlternativeHint);

    private void UpdateAssessment()
    {
        var assessment = FoodAssessmentService.Assess(Food, _goal, AmountGrams);
        Rating = assessment.Rating;
        RatingIcon = assessment.RatingIcon;
        Explanation = assessment.Explanation;
        AlternativeHint = assessment.AlternativeHint;
    }

    public RelayCommand ConfirmCommand { get; }
    public RelayCommand DeleteCommand { get; }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
