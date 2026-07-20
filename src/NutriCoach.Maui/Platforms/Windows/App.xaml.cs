using Microsoft.UI.Xaml;

// "Blank Application" Template-Standardcode für MAUI unter Windows (WinUI 3).

namespace NutriCoach.Maui.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
