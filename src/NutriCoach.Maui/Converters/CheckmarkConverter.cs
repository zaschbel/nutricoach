using System.Globalization;

namespace NutriCoach.Maui.Converters;

/// <summary>Wandelt "hat die Mahlzeit schon Einträge?" in ein Häkchen- bzw. Kreis-Symbol um (rein optisch, wie in der Stitch-Vorlage).</summary>
public class CheckmarkConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "✓" : "○";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
