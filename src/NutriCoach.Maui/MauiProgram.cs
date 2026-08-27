using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Plugin.LocalNotification;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
#if IOS
using UIKit;
#endif

namespace NutriCoach.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .UseLocalNotification()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                fonts.AddFont("Inter-Medium.ttf", "InterMedium");
                fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
                fonts.AddFont("Inter-Bold.ttf", "InterBold");
                fonts.AddFont("fa-solid-900.ttf", "FontAwesomeSolid");
            });

#if IOS
        // iOS' UIWindow ist standardmäßig reines Schwarz (#000000) hinterlegt. Da unsere feste
        // Dunkelmodus-Palette (App.xaml.cs) nur SEHR dunkles Grau nutzt (#121316), fällt genau an
        // den abgerundeten Display-Ecken - wo die eigene Seite nicht ganz bis zum physischen Rand
        // reicht - dieser Farbunterschied als sichtbarer Schwarz-Fleck auf. Fix: Fensterhintergrund
        // explizit auf denselben Farbton wie ColorBackground legen, damit es keinen Farbsprung gibt.
        builder.ConfigureMauiHandlers(handlers =>
        {
            WindowHandler.Mapper.AppendToMapping("DarkWindowBackgroundForRoundedCorners", (handler, _) =>
            {
                handler.PlatformView.BackgroundColor = UIColor.FromRGB(0x12, 0x13, 0x16);
            });
        });
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
