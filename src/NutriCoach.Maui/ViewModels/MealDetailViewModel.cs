using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Maui.Graphics;
using NutriCoach.App.Models;
using NutriCoach.App.Services;
using NutriCoach.Maui.Drawables;

namespace NutriCoach.App.ViewModels;

/// <summary>Steuert die Mahlzeit-Detailseite (z. B. "FRÜHSTÜCK"): Donut-Ring aus den drei Makros
/// und die Liste der für diese Mahlzeit eingetragenen Lebensmittel - analog zur MCI-App-Vorlage.</summary>
public class MealDetailViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public MealType Meal { get; }
    public string Title { get; }
    public ObservableCollection<NutritionEntryDisplay> Entries { get; }

    public MealDetailViewModel(MealType meal, string title, ObservableCollection<NutritionEntryDisplay> entries)
    {
        Meal = meal;
        Title = title.ToUpperInvariant();
        Entries = entries;
        Entries.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(TotalKcal));
            OnPropertyChanged(nameof(TotalProtein));
            OnPropertyChanged(nameof(TotalCarbs));
            OnPropertyChanged(nameof(TotalFat));
            OnPropertyChanged(nameof(RingDrawable));
            OnPropertyChanged(nameof(HasEntries));
        };
    }

    public double TotalKcal => Entries.Sum(e => e.Kcal);
    public double TotalProtein => Entries.Sum(e => e.Protein);
    public double TotalCarbs => Entries.Sum(e => e.Carbs);
    public double TotalFat => Entries.Sum(e => e.Fat);
    public bool HasEntries => Entries.Count > 0;

    public IDrawable RingDrawable => new MacroRingDrawable(TotalProtein, TotalCarbs, TotalFat);
}
