using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using NutriCoach.App.Services;

namespace NutriCoach.App.ViewModels;

/// <summary>Ein Lebensmittel, das heute zu einem bestimmten Nährstoff beigetragen hat, samt Menge.</summary>
public record NutrientSourceDisplay(string Name, double Kcal, double AmountGrams, double NutrientAmount, string Unit);

/// <summary>Steuert das Ausklapp-Infofenster zu einem einzelnen Nährstoff (z. B. "Ballaststoffe"):
/// heutige Quellen + allgemeine Erklärung, analog zur MCI-App-Vorlage. "Über X" und "Wie wirkt es auf
/// mich?" sind einzeln auf-/zuklappbar, genau wie im Vorbild (Frage antippen, Antwort erscheint).</summary>
public class NutrientDetailViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public string Name { get; }
    public string Unit { get; }
    public string About { get; }
    public string Effect { get; }
    public List<NutrientSourceDisplay> Sources { get; }
    public bool HasSources => Sources.Count > 0;
    public string SourcesHeader => $"Heutige {Name}-Quellen ({Sources.Count})";

    private bool _isAboutExpanded;
    public bool IsAboutExpanded
    {
        get => _isAboutExpanded;
        set { _isAboutExpanded = value; OnPropertyChanged(); OnPropertyChanged(nameof(AboutChevron)); }
    }
    public string AboutChevron => IsAboutExpanded ? "" : "";

    private bool _isEffectExpanded;
    public bool IsEffectExpanded
    {
        get => _isEffectExpanded;
        set { _isEffectExpanded = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectChevron)); }
    }
    public string EffectChevron => IsEffectExpanded ? "" : "";

    public RelayCommand ToggleAboutCommand { get; }
    public RelayCommand ToggleEffectCommand { get; }

    public NutrientDetailViewModel(NutrientCardDisplay nutrient, IEnumerable<NutritionEntryDisplay> todaysEntries)
    {
        ToggleAboutCommand = new RelayCommand(_ => IsAboutExpanded = !IsAboutExpanded);
        ToggleEffectCommand = new RelayCommand(_ => IsEffectExpanded = !IsEffectExpanded);
        Name = nutrient.Name;
        Unit = nutrient.Unit;
        var info = NutrientInfoData.Get(nutrient.Name);
        About = info.About;
        Effect = info.Effect;

        // Reflection statt 40x Handschrift: jede NutritionEntryDisplay-Instanz hat eine Property mit
        // demselben Namen wie NutrientCardDisplay.Key (z. B. "Fiber"), die den Anteil dieses Eintrags
        // an dem jeweiligen Naehrstoff enthaelt.
        var property = typeof(NutritionEntryDisplay).GetProperty(nutrient.Key, BindingFlags.Public | BindingFlags.Instance);

        Sources = property is null
            ? new List<NutrientSourceDisplay>()
            : todaysEntries
                .Select(e => (Entry: e, Amount: (double)(property.GetValue(e) ?? 0.0)))
                .Where(x => x.Amount > 0.01)
                .OrderByDescending(x => x.Amount)
                .Select(x => new NutrientSourceDisplay(x.Entry.Name, x.Entry.Kcal, x.Entry.AmountGrams, x.Amount, nutrient.Unit))
                .ToList();
    }
}
