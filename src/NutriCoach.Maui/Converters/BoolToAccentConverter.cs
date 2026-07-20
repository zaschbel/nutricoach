using System.Globalization;

namespace NutriCoach.Maui.Converters;

/// <summary>Textfarbe für Unterreiter: Akzentfarbe wenn ausgewählt, sonst gedämpftes Grau.</summary>
public class BoolToAccentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Color.FromArgb("#0058BC") : Color.FromArgb("#717786");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Unterstrich-Farbe für Unterreiter: Akzentfarbe wenn ausgewählt, sonst transparent (unsichtbar).</summary>
public class BoolToAccentOrTransparentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Color.FromArgb("#0058BC") : Colors.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
