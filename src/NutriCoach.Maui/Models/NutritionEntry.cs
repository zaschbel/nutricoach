namespace NutriCoach.App.Models;

/// <summary>
/// Ein Eintrag im Ernährungstagebuch: "100g Salami-Pizza, mittags, am 15.07."
/// AmountGrams * FoodItem-Werte/100 ergibt die tatsächlichen Nährwerte für diesen Eintrag.
/// </summary>
public class NutritionEntry
{
    public int Id { get; set; }
    public int UserProfileId { get; set; }
    public int FoodItemId { get; set; }
    public FoodItem? FoodItem { get; set; }

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public MealType Meal { get; set; }
    public double AmountGrams { get; set; }

    // Berechnete Werte werden zur Laufzeit aus FoodItem * (AmountGrams/100) ermittelt,
    // hier NICHT redundant gespeichert, damit spätere Korrekturen am FoodItem
    // rückwirkend konsistent bleiben.
}
