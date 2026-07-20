using System.Globalization;

namespace NutriCoach.Maui.Converters;

/// <summary>Vergleicht einen gebundenen int-Wert mit dem ConverterParameter - für "nur sichtbar, wenn CurrentStep == X".</summary>
public class IntEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int i && parameter is string s && int.TryParse(s, out var target) && i == target;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
