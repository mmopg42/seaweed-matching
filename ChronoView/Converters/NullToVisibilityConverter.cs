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
        // Default: If value is null, return Visible (e.g. for placeholders/loading)
        bool isVisible = value == null;

        // If parameter is "Inverse", flip the logic: If value is null, return Collapsed (e.g. for actual content)
        if (parameter is string paramStr && paramStr.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
        {
            isVisible = !isVisible;
        }

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
