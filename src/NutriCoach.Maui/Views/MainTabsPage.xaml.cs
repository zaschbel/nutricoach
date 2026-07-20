using NutriCoach.Maui.Drawables;

namespace NutriCoach.Maui.Views;

public partial class MainTabsPage : ContentPage
{
    private List<View> _pages = null!;

    /// <summary>Erlaubt anderen Seiten (z. B. Home), einen Reiter-Wechsel anzustoßen - etwa wenn man
    /// auf die Kalorien-Kachel tippt und direkt zu den Mahlzeiten weitergeleitet werden soll.</summary>
    public static event Action<int>? NavigateToTabRequested;

    /// <summary>Löst NavigateToTabRequested aus - Ereignisse dürfen von außerhalb der Klasse nur
    /// abonniert (+=), aber nicht direkt aufgerufen werden, deshalb dieser kleine Umweg über eine Methode.</summary>
    public static void RequestTabChange(int index) => NavigateToTabRequested?.Invoke(index);

    public MainTabsPage()
    {
        InitializeComponent();

        _pages = new List<View>
        {
            new HomePage(),
            new TrainingPage(),
            new ErnaehrungPage(),
            new StatistikenPage()
        };
        Carousel.ItemsSource = _pages;

        NavigateToTabRequested += index => Carousel.Position = index;

        UpdateTabVisuals(0);
    }

    private void OnTabHomeTapped(object? sender, EventArgs e) => Carousel.Position = 0;
    private void OnTabTrainingTapped(object? sender, EventArgs e) => Carousel.Position = 1;
    private void OnTabErnaehrungTapped(object? sender, EventArgs e) => Carousel.Position = 2;
    private void OnTabStatistikenTapped(object? sender, EventArgs e) => Carousel.Position = 3;

    /// <summary>
    /// Feuert sowohl beim Antippen der Leiste unten (wir setzen Carousel.Position) als auch beim
    /// Wischen selbst - dadurch ist "Reiter neu laden" (z. B. Statistiken mit dem aktuellen
    /// Gewicht) jetzt für BEIDE Wege abgedeckt, nicht nur fürs Antippen wie vorher.
    /// </summary>
    private async void OnCarouselPositionChanged(object? sender, PositionChangedEventArgs e)
    {
        UpdateTabVisuals(e.CurrentPosition);

        if (e.CurrentPosition == 3 && _pages[3] is StatistikenPage statsPage)
            await statsPage.RefreshAsync();
    }

    private void UpdateTabVisuals(int index)
    {
        var accent = (Color)Application.Current!.Resources["ColorAccent"];
        var muted = (Color)Application.Current!.Resources["ColorTextSecondary"];

        LabelHome.TextColor = index == 0 ? accent : muted;
        LabelTraining.TextColor = index == 1 ? accent : muted;
        LabelErnaehrung.TextColor = index == 2 ? accent : muted;
        LabelStatistiken.TextColor = index == 3 ? accent : muted;

        IconHome.Drawable = new TabIconDrawable(TabIconType.Home, index == 0 ? accent : muted);
        IconTraining.Drawable = new TabIconDrawable(TabIconType.Training, index == 1 ? accent : muted);
        IconErnaehrung.Drawable = new TabIconDrawable(TabIconType.Food, index == 2 ? accent : muted);
        IconStatistiken.Drawable = new TabIconDrawable(TabIconType.Stats, index == 3 ? accent : muted);
        IconHome.Invalidate();
        IconTraining.Invalidate();
        IconErnaehrung.Invalidate();
        IconStatistiken.Invalidate();
    }
}
