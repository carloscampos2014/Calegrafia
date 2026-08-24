using System.Globalization;

namespace Calegrafia.App.Converters;

/// <summary>
/// Returns the logical inverse of a bool value.
/// Used to disable buttons while IsBusy is true.
/// </summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}
