using System.Globalization;

namespace Calegrafia.App.Converters;

/// <summary>
/// Returns true when a string is non-null and non-empty.
/// Used to show/hide error and success message labels.
/// </summary>
public sealed class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrEmpty(s);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
