using System.Globalization;

namespace NutriCoach.Maui.Converters;

/// <summary>Wandelt einen Prozentwert (0-100) in eine Pixel-Breite für die Makro-Balkengrafik um.</summary>
public class PercentToWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double percent ? Math.Max(4, percent * 2.2) : 4.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
