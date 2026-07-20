using Microsoft.Maui.Storage;
using NutriCoach.App.Data;
using NutriCoach.Maui.Views;

namespace NutriCoach.Maui;

public partial class App : Application
{
    /// <summary>"System", "Hell" oder "Dunkel" - in den Einstellungen wählbar, wird lokal gemerkt.</summary>
    public const string ThemePreferenceKey = "app_theme_preference";

    public App()
    {
        InitializeComponent();

        LoadThemeResources();

        // Muss vor jedem Datenbankzugriff einmal aufgerufen werden, sonst wirft SQLite unter MAUI
        // einen Laufzeitfehler ("You need to call SQLitePCL.raw.SetProvider()").
        SQLitePCL.Batteries_V2.Init();

        // Datenbank beim allerersten Start anlegen bzw. um neue Übungen/Spalten ergänzen -
        // exakt die gleiche Logik wie in der Windows-Version, nur der Speicherort ist plattformabhängig.
        DatabaseInitializer.EnsureCreated();

        // Platzhalter, während geprüft wird, ob schon ein Profil existiert.
        MainPage = new ContentPage();
        _ = InitializeStartPageAsync();
    }

    /// <summary>
    /// Überschreibt bei Dunkelmodus die einzelnen Farb-Ressourcen direkt im Code (statt eine ganze
    /// zweite Ressourcen-Datei dynamisch nachzuladen - das hatte beim ersten Versuch zum Absturz
    /// beim Start geführt). Colors.xaml (hell) ist über App.xaml weiterhin fest als Basis eingebunden;
    /// hier werden nur die Werte gezielt ersetzt, ein bewährtes, einfacheres Vorgehen.
    /// </summary>
    private void LoadThemeResources()
    {
        var preference = Preferences.Default.Get(ThemePreferenceKey, "System");
        var useDark = preference switch
        {
            "Dunkel" => true,
            "Hell" => false,
            _ => Application.Current!.RequestedTheme == AppTheme.Dark
        };

        if (useDark)
        {
            Resources["ColorBackground"] = Color.FromArgb("#121316");
            Resources["ColorSurface"] = Color.FromArgb("#1E2024");
            Resources["ColorSurfaceElevated"] = Color.FromArgb("#282B31");
            Resources["ColorBorder"] = Color.FromArgb("#3A3D44");
            Resources["ColorAccent"] = Color.FromArgb("#4C8DFF");
            Resources["ColorAccentHover"] = Color.FromArgb("#6BA1FF");
            Resources["ColorAccentPressed"] = Color.FromArgb("#3A6FD1");
            Resources["ColorTextPrimary"] = Color.FromArgb("#F0F1F3");
            Resources["ColorTextSecondary"] = Color.FromArgb("#B8BCC4");
            Resources["ColorTextMuted"] = Color.FromArgb("#8A8F99");
            Resources["ColorSuccess"] = Color.FromArgb("#34C77A");
            Resources["ColorTertiary"] = Color.FromArgb("#B08F6E");
        }

        UserAppTheme = useDark ? AppTheme.Dark : AppTheme.Light;
    }

    private async Task InitializeStartPageAsync()
    {
        var profile = await AppState.ProfileService.GetActiveProfileAsync();

        if (profile is null)
        {
            MainPage = new OnboardingPage();
        }
        else
        {
            await AppState.MainViewModel.LoadAsync();
            MainPage = new MainTabsPage();
        }
    }
}
