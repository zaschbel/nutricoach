using System.Globalization;

namespace NutriCoach.Maui.Converters;

/// <summary>Wandelt zwischen int? und dem Text eines Entry-Feldes um - für die getrennten Tag/Monat/Jahr-Felder.</summary>
public class NullableIntConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int i ? i.ToString() : "";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && int.TryParse(s, out var i) ? i : (int?)null;
}
