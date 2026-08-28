using System.Reflection;
using NutriCoach.App.Services;

namespace NutriCoach.App.ViewModels;

/// <summary>Ein Lebensmittel, das heute zu einem bestimmten Nährstoff beigetragen hat, samt Menge.</summary>
public record NutrientSourceDisplay(string Name, double Kcal, double AmountGrams, double NutrientAmount, string Unit);

/// <summary>Steuert das Ausklapp-Infofenster zu einem einzelnen Nährstoff (z. B. "Ballaststoffe"):
/// heutige Quellen + allgemeine Erklärung, analog zur MCI-App-Vorlage.</summary>
public class NutrientDetailViewModel
{
    public string Name { get; }
    public string Unit { get; }
    public string About { get; }
    public string Effect { get; }
    public List<NutrientSourceDisplay> Sources { get; }
    public bool HasSources => Sources.Count > 0;
    public string SourcesHeader => $"Heutige {Name}-Quellen ({Sources.Count})";

    public NutrientDetailViewModel(NutrientCardDisplay nutrient, IEnumerable<NutritionEntryDisplay> todaysEntries)
    {
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
