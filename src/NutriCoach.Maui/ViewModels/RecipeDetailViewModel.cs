using System.ComponentModel;
using System.Runtime.CompilerServices;
using NutriCoach.App.Models;
using NutriCoach.App.Services;

namespace NutriCoach.App.ViewModels;

/// <summary>
/// Steuert die Rezept-Detailseite: zeigt Foto, Zutaten und Zubereitung eines TheMealDB-Rezepts,
/// und erlaubt es, das Rezept lokal zu favorisieren/zu entfavorisieren (siehe RecipeFavoritesService -
/// Preferences-JSON, keine neue Datenbank-Tabelle).
/// </summary>
public class RecipeDetailViewModel : INotifyPropertyChanged
{
    private readonly RecipeFavoritesService _favoritesService;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Recipe Recipe { get; }

    public string Name => Recipe.Name;
    public string? ThumbnailUrl => Recipe.ThumbnailUrl;
    public bool HasThumbnail => Recipe.HasThumbnail;
    public string CategoryAreaLabel => Recipe.CategoryAreaLabel;
    public bool HasCategoryAreaLabel => Recipe.HasCategoryAreaLabel;
    public List<string> IngredientLines => Recipe.IngredientLines;
    public bool HasIngredients => IngredientLines.Count > 0;
    public string InstructionsText => string.IsNullOrWhiteSpace(Recipe.Instructions)
        ? "Keine Zubereitungsschritte verfügbar."
        : Recipe.Instructions;

    private bool _isFavorite;
    public bool IsFavorite
    {
        get => _isFavorite;
        set { _isFavorite = value; OnPropertyChanged(); OnPropertyChanged(nameof(FavoriteButtonText)); }
    }
    public string FavoriteButtonText => IsFavorite ? "Favorit entfernen" : "Als Favorit speichern";

    public RelayCommand ToggleFavoriteCommand { get; }

    public RecipeDetailViewModel(Recipe recipe, RecipeFavoritesService favoritesService)
    {
        Recipe = recipe;
        _favoritesService = favoritesService;
        _isFavorite = favoritesService.IsFavorite(recipe.Id);

        ToggleFavoriteCommand = new RelayCommand(_ =>
        {
            IsFavorite = _favoritesService.ToggleFavorite(Recipe);
        });
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
