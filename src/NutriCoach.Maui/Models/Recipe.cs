namespace NutriCoach.App.Models;

/// <summary>
/// Ein Rezept von TheMealDB (externe, kostenlose Rezept-Datenbank - siehe RecipeLookupService).
/// Reines DTO für Anzeige/Favoriten, KEINE EF-Core-Entity (kein DbSet in AppDbContext) - Favoriten
/// werden bewusst über Preferences als JSON gespeichert, nicht über eine neue Datenbank-Tabelle
/// (siehe RecipeFavoritesService für die Begründung).
/// </summary>
public class Recipe
{
    /// <summary>TheMealDB-Id ("idMeal") - eindeutig, dient als Schlüssel für Favoriten.</summary>
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Area { get; set; }
    public string? ThumbnailUrl { get; set; }

    /// <summary>Vollständiger Zubereitungstext, so wie von TheMealDB geliefert (inkl. Zeilenumbrüchen).</summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Zutaten bereits fertig kombiniert aus TheMealDB's strIngredientN/strMeasureN-Feldern
    /// (z. B. "200g Mehl") - siehe RecipeLookupService.MapToRecipe für die Zusammenführung.
    /// </summary>
    public List<string> IngredientLines { get; set; } = new();

    public bool HasThumbnail => !string.IsNullOrWhiteSpace(ThumbnailUrl);

    /// <summary>Kategorie und Herkunftsregion kombiniert für eine kompakte Anzeige (z. B. "Dessert · Italian").</summary>
    public string CategoryAreaLabel
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Category)) parts.Add(Category);
            if (!string.IsNullOrWhiteSpace(Area)) parts.Add(Area);
            return string.Join(" · ", parts);
        }
    }
    public bool HasCategoryAreaLabel => !string.IsNullOrWhiteSpace(CategoryAreaLabel);
}
