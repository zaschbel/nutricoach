using System.Net.Http;
using System.Text.Json;
using NutriCoach.App.Models;

namespace NutriCoach.App.Services;

/// <summary>Ergebnis einer Rezept-Suche inkl. Erfolgsstatus, damit "nichts gefunden" von "Verbindung fehlgeschlagen" unterscheidbar ist (gleiches Muster wie OnlineSearchResult in FoodLookupService).</summary>
public record RecipeSearchResult(List<Recipe> Items, bool Success, string? ErrorMessage);

/// <summary>Ergebnis beim Laden GENAU EINES Rezepts (per Id oder zufällig).</summary>
public record RecipeResult(Recipe? Item, bool Success, string? ErrorMessage);

/// <summary>
/// Holt Rezepte von TheMealDB (https://www.themealdb.com) - einer kostenlosen Rezept-Datenbank ohne
/// Registrierung. Die "1" im Pfad ist der von TheMealDB selbst veröffentlichte, geteilte Test-API-Key
/// für genau diese Art der Nutzung (kein eigener Account, kein Key-Management nötig, anders als bei
/// der Gemini-Integration, wo der Nutzer einen eigenen Key hinterlegen muss).
/// </summary>
public class RecipeLookupService
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://www.themealdb.com/api/json/v1/1/"),
        Timeout = TimeSpan.FromSeconds(8)
    };

    /// <summary>Sucht Rezepte anhand eines Namens (search.php?s=...). TheMealDB ist englischsprachig, deutsche Suchbegriffe liefern daher oft nichts.</summary>
    public async Task<RecipeSearchResult> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new RecipeSearchResult(new List<Recipe>(), true, null);

        try
        {
            var url = $"search.php?s={Uri.EscapeDataString(query.Trim())}";
            var recipes = await FetchMealsAsync(url);
            return new RecipeSearchResult(recipes, true, null);
        }
        catch (Exception ex)
        {
            // Kein Internet, Timeout, o.ä. - wird von der UI als "Suche fehlgeschlagen: ..." angezeigt,
            // statt die App abstürzen zu lassen oder stillschweigend "nichts gefunden" vorzutäuschen.
            return new RecipeSearchResult(new List<Recipe>(), false, ex.Message);
        }
    }

    /// <summary>Lädt ein einzelnes, zufälliges Rezept (random.php) - für den "Überrasch mich"-Button.</summary>
    public async Task<RecipeResult> GetRandomAsync()
    {
        try
        {
            var recipes = await FetchMealsAsync("random.php");
            return new RecipeResult(recipes.FirstOrDefault(), true, null);
        }
        catch (Exception ex)
        {
            return new RecipeResult(null, false, ex.Message);
        }
    }

    /// <summary>Lädt die vollen Details eines Rezepts anhand seiner TheMealDB-Id (lookup.php?i=...).</summary>
    public async Task<RecipeResult> GetByIdAsync(string mealId)
    {
        if (string.IsNullOrWhiteSpace(mealId)) return new RecipeResult(null, true, null);

        try
        {
            var url = $"lookup.php?i={Uri.EscapeDataString(mealId)}";
            var recipes = await FetchMealsAsync(url);
            return new RecipeResult(recipes.FirstOrDefault(), true, null);
        }
        catch (Exception ex)
        {
            return new RecipeResult(null, false, ex.Message);
        }
    }

    /// <summary>Ruft eine TheMealDB-URL ab und parst das gemeinsame "{ \"meals\": [...] oder null }"-Antwortformat.</summary>
    private static async Task<List<Recipe>> FetchMealsAsync(string relativeUrl)
    {
        await using var stream = await Http.GetStreamAsync(relativeUrl);
        using var doc = await JsonDocument.ParseAsync(stream);

        var result = new List<Recipe>();
        if (!doc.RootElement.TryGetProperty("meals", out var meals) || meals.ValueKind != JsonValueKind.Array)
            return result; // "meals": null (nichts gefunden) oder Feld fehlt - beides ein normaler leerer Zustand

        foreach (var meal in meals.EnumerateArray())
            result.Add(MapToRecipe(meal));

        return result;
    }

    /// <summary>
    /// Baut ein Recipe aus einem TheMealDB-Meal-JSON-Objekt. Die 20 nummerierten Zutaten/Mengen-Felder
    /// (strIngredient1..20 / strMeasure1..20) sind der fehleranfälligste Teil: viele Rezepte nutzen
    /// weniger als 20 und lassen den Rest als leeren String ODER null stehen - beides muss übersprungen werden.
    /// </summary>
    private static Recipe MapToRecipe(JsonElement meal)
    {
        var recipe = new Recipe
        {
            Id = GetString(meal, "idMeal") ?? string.Empty,
            Name = GetString(meal, "strMeal") ?? string.Empty,
            Category = GetString(meal, "strCategory"),
            Area = GetString(meal, "strArea"),
            ThumbnailUrl = GetString(meal, "strMealThumb"),
            Instructions = GetString(meal, "strInstructions")
        };

        for (var i = 1; i <= 20; i++)
        {
            var ingredient = GetString(meal, $"strIngredient{i}");
            if (string.IsNullOrWhiteSpace(ingredient)) continue; // leerer String oder null - Feld ungenutzt

            var measure = GetString(meal, $"strMeasure{i}")?.Trim();
            var line = string.IsNullOrWhiteSpace(measure)
                ? ingredient.Trim()
                : $"{measure} {ingredient.Trim()}";

            recipe.IngredientLines.Add(line);
        }

        return recipe;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value)) return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
