using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NutriCoach.App.Models;
using NutriCoach.App.Services;
using NutriCoach.Maui.Services;

namespace NutriCoach.App.ViewModels;

/// <summary>
/// Steuert die Rezepte-Seite: Suche bei TheMealDB (per Name), ein "Überrasch mich"-Button für ein
/// zufälliges Rezept, sowie eine Favoriten-Ansicht der lokal gespeicherten Rezepte (siehe
/// RecipeFavoritesService). Tippen auf ein Ergebnis öffnet die Detailseite über den IDialogService -
/// gleiches Grundmuster wie AddFoodViewModel.OpenDetailAsync für FoodDetailPage.
/// </summary>
public class RecipesViewModel : INotifyPropertyChanged
{
    private readonly RecipeLookupService _recipeService;
    private readonly RecipeFavoritesService _favoritesService;
    private readonly IDialogService _dialogService;
    private readonly GeminiAiService _aiService;
    private readonly string? _goalContext;
    private readonly string? _suggestedCategory;

    public event PropertyChangedEventHandler? PropertyChanged;

    public RecipesViewModel(RecipeLookupService recipeService, RecipeFavoritesService favoritesService, IDialogService dialogService,
        GeminiAiService aiService, string? goalContext = null, string? suggestedCategory = null)
    {
        _recipeService = recipeService;
        _favoritesService = favoritesService;
        _dialogService = dialogService;
        _aiService = aiService;
        _goalContext = goalContext;
        _suggestedCategory = suggestedCategory;

        SearchCommand = new RelayCommand(async _ => await SearchAsync());
        SurpriseMeCommand = new RelayCommand(async _ => await SurpriseMeAsync());
        SelectRecipeCommand = new RelayCommand(async param =>
        {
            if (param is Recipe recipe) await OpenRecipeAsync(recipe);
        });
        SetModeCommand = new RelayCommand(param =>
        {
            if (param is string mode) Mode = mode;
        });

        SearchResults.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSearchResults));
        Favorites.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFavorites));
        SuggestedRecipes.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSuggestedRecipes));
    }

    // ---------------- Vorschläge "Für dein Ziel" ----------------
    public ObservableCollection<Recipe> SuggestedRecipes { get; } = new();
    public bool HasSuggestedRecipes => SuggestedRecipes.Count > 0;

    private bool _isLoadingSuggestions;
    public bool IsLoadingSuggestions { get => _isLoadingSuggestions; set { _isLoadingSuggestions = value; OnPropertyChanged(); } }

    private string? _suggestionsStatusText;
    public string? SuggestionsStatusText { get => _suggestionsStatusText; set { _suggestionsStatusText = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSuggestionsStatus)); } }
    public bool HasSuggestionsStatus => !string.IsNullOrWhiteSpace(SuggestionsStatusText);

    /// <summary>
    /// Lädt ein paar Rezepte aus einer zum Nutzerziel passenden Kategorie (z. B. "Chicken" bei
    /// Muskelaufbau) und bewertet/übersetzt sie sofort mit - wird beim Öffnen der Seite einmal
    /// automatisch aufgerufen (siehe RecipesPage.xaml.cs).
    /// </summary>
    public async Task LoadSuggestedRecipesAsync()
    {
        if (string.IsNullOrWhiteSpace(_suggestedCategory)) return;

        IsLoadingSuggestions = true;
        SuggestionsStatusText = null;
        SuggestedRecipes.Clear();

        var result = await _recipeService.GetByCategoryAsync(_suggestedCategory);

        string? translationError = null;
        if (result.Success && result.Items.Count > 0)
        {
            var (translateSuccess, error) = await _aiService.TranslateRecipesToGermanAsync(result.Items, _goalContext);
            if (!translateSuccess) translationError = error;
        }

        IsLoadingSuggestions = false;

        if (!result.Success)
        {
            SuggestionsStatusText = $"Vorschläge konnten nicht geladen werden: {result.ErrorMessage}";
            return;
        }

        foreach (var recipe in result.Items) SuggestedRecipes.Add(recipe);

        if (translationError is not null)
            SuggestionsStatusText = $"Vorschläge konnten nicht übersetzt werden, werden auf Englisch angezeigt: {translationError}";
    }

    // ---------------- Modus-Umschaltung ----------------
    private string _mode = "Suche";
    public string Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSearchMode));
            OnPropertyChanged(nameof(IsFavoritesMode));
            if (IsFavoritesMode) LoadFavorites();
        }
    }
    public bool IsSearchMode => Mode == "Suche";
    public bool IsFavoritesMode => Mode == "Favoriten";
    public RelayCommand SetModeCommand { get; }

    // ---------------- Suche ----------------
    private string _searchText = string.Empty;
    public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); } }

    public ObservableCollection<Recipe> SearchResults { get; } = new();
    public bool HasSearchResults => SearchResults.Count > 0;

    private bool _isSearching;
    public bool IsSearching { get => _isSearching; set { _isSearching = value; OnPropertyChanged(); } }

    private string? _searchStatusText;
    public string? SearchStatusText { get => _searchStatusText; set { _searchStatusText = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSearchStatus)); } }
    public bool HasSearchStatus => !string.IsNullOrWhiteSpace(SearchStatusText);

    public RelayCommand SearchCommand { get; }
    public RelayCommand SurpriseMeCommand { get; }
    public RelayCommand SelectRecipeCommand { get; }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        IsSearching = true;
        SearchStatusText = "Übersetze Suchbegriff …";
        SearchResults.Clear();

        // TheMealDB versteht nur Englisch - deutsche Suchbegriffe erst übersetzen, sonst kommt fast
        // immer "nichts gefunden" zurück. Schlägt die Übersetzung fehl (z. B. kein API-Key hinterlegt),
        // wird trotzdem mit dem Original-Suchbegriff weitergesucht, statt die Suche ganz zu blockieren.
        var (translatedQuery, translateError) = await _aiService.TranslateSearchQueryToEnglishAsync(SearchText);
        var queryToUse = translatedQuery ?? SearchText;

        SearchStatusText = "Suche läuft …";
        var result = await _recipeService.SearchAsync(queryToUse);

        string? translationError = null;
        if (result.Success && result.Items.Count > 0)
        {
            SearchStatusText = "Übersetze Ergebnisse …";
            var (translateSuccess, error) = await _aiService.TranslateRecipesToGermanAsync(result.Items, _goalContext);
            if (!translateSuccess) translationError = error;
        }

        IsSearching = false;
        foreach (var recipe in result.Items) SearchResults.Add(recipe);

        if (!result.Success)
        {
            SearchStatusText = $"Suche fehlgeschlagen (evtl. kein Internet): {result.ErrorMessage}";
        }
        else if (result.Items.Count == 0)
        {
            SearchStatusText = translateError is not null
                ? $"Keine Rezepte gefunden. Übersetzung des Suchbegriffs hat nicht geklappt ({translateError}), gesucht wurde nach \"{queryToUse}\"."
                : "Keine Rezepte gefunden. Versuch es mit einem anderen Suchbegriff.";
        }
        else if (translationError is not null)
        {
            // Suche/Anzeige hat trotzdem geklappt (Ergebnisse bleiben auf Englisch) - Fehler sichtbar
            // machen statt stillschweigend unübersetzt anzuzeigen, wie es zuvor der Fall war.
            SearchStatusText = $"Ergebnisse konnten nicht übersetzt werden, werden auf Englisch angezeigt: {translationError}";
        }
        else
        {
            SearchStatusText = null;
        }
    }

    private async Task SurpriseMeAsync()
    {
        IsSearching = true;
        SearchStatusText = "Suche ein zufälliges Rezept …";

        var result = await _recipeService.GetRandomAsync();

        string? translationError = null;
        if (result.Success && result.Item is not null)
        {
            SearchStatusText = "Übersetze Rezept …";
            var (translateSuccess, error) = await _aiService.TranslateRecipesToGermanAsync(new List<Recipe> { result.Item }, _goalContext);
            if (!translateSuccess) translationError = error;
        }

        IsSearching = false;

        if (!result.Success || result.Item is null)
        {
            SearchStatusText = $"Zufälliges Rezept konnte nicht geladen werden: {result.ErrorMessage ?? "unbekannter Fehler"}";
            return;
        }

        SearchStatusText = translationError is not null
            ? $"Übersetzung hat nicht geklappt, Rezept wird auf Englisch angezeigt: {translationError}"
            : null;
        await OpenRecipeAsync(result.Item);
    }

    // ---------------- Favoriten ----------------
    public ObservableCollection<Recipe> Favorites { get; } = new();
    public bool HasFavorites => Favorites.Count > 0;

    public void LoadFavorites()
    {
        Favorites.Clear();
        foreach (var recipe in _favoritesService.GetFavorites()) Favorites.Add(recipe);
    }

    // ---------------- Detailseite ----------------
    private async Task OpenRecipeAsync(Recipe recipe)
    {
        await _dialogService.ShowRecipeDetailAsync(recipe, _favoritesService);

        // Favoriten-Status kann sich auf der Detailseite geändert haben (hinzugefügt/entfernt) -
        // Ansicht bei Rückkehr auffrischen, falls sie gerade sichtbar ist.
        if (IsFavoritesMode) LoadFavorites();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
