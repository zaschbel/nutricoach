using System.Linq;
using System.Text.Json;
using Microsoft.Maui.Storage;
using NutriCoach.App.Models;

namespace NutriCoach.App.Services;

/// <summary>
/// Verwaltet favorisierte Rezepte rein über Preferences (als JSON), BEWUSST OHNE eigene
/// SQLite-Tabelle: DatabaseInitializer.EnsureCreated() legt Tabellen nur an, wenn die Datenbankdatei
/// noch gar nicht existiert - bei der bereits bestehenden, befüllten DB des Nutzers würde eine neue
/// Tabelle sonst nie entstehen (genau das Problem, das die Trainingsvorlagen-Tabellen per rohem SQL
/// in DatabaseInitializer umgehen mussten). Für eine kleine Favoriten-Liste reicht Preferences völlig.
/// Gespeichert wird das komplette Recipe-Objekt (nicht nur die Id), damit die Favoriten-Liste ohne
/// erneuten Netzwerkaufruf sofort anzeigbar ist.
/// </summary>
public class RecipeFavoritesService
{
    private const string PrefKey = "favorite_recipes_json";

    public List<Recipe> GetFavorites()
    {
        try
        {
            var json = Preferences.Default.Get(PrefKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json)) return new List<Recipe>();

            return JsonSerializer.Deserialize<List<Recipe>>(json) ?? new List<Recipe>();
        }
        catch
        {
            // Kaputtes/altes JSON-Format o.ä. - lieber leere Liste als Absturz.
            return new List<Recipe>();
        }
    }

    public bool IsFavorite(string mealId) =>
        !string.IsNullOrWhiteSpace(mealId) && GetFavorites().Any(r => r.Id == mealId);

    /// <summary>Fügt das Rezept zu den Favoriten hinzu, oder entfernt es, falls es schon favorisiert ist.</summary>
    public bool ToggleFavorite(Recipe recipe)
    {
        var favorites = GetFavorites();
        var existing = favorites.FirstOrDefault(r => r.Id == recipe.Id);

        bool isNowFavorite;
        if (existing is not null)
        {
            favorites.Remove(existing);
            isNowFavorite = false;
        }
        else
        {
            favorites.Add(recipe);
            isNowFavorite = true;
        }

        Save(favorites);
        return isNowFavorite;
    }

    private static void Save(List<Recipe> favorites)
    {
        try
        {
            Preferences.Default.Set(PrefKey, JsonSerializer.Serialize(favorites));
        }
        catch
        {
            // Speichern fehlgeschlagen - App bleibt benutzbar, nur die Änderung wird nicht dauerhaft übernommen.
        }
    }
}
