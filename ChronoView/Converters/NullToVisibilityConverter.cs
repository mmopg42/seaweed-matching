using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ChronoView;

/// <summary>
/// Converts null values to Visibility.Visible (for loading indicators) and non-null to Collapsed.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // If value is null, show the loading indicator (Visible)
        // If value is not null, hide the loading indicator (Collapsed)
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
