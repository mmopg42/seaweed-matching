using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ChronoView.Models;
using Brushes = System.Windows.Media.Brushes;

namespace ChronoView.UI.Converters;

/// <summary>
/// Converts CameraState to a brush color for the status indicator (ellipse).
/// </summary>
public class CameraStateToIndicatorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CameraState state)
        {
            return state switch
            {
                CameraState.Stopped => Brushes.Gray,
                CameraState.Starting => Brushes.Gold,
                CameraState.Running => Brushes.LimeGreen,
                CameraState.Stopping => Brushes.Gold,
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts CameraState to the button display text.
/// </summary>
public class CameraStateToButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CameraState state)
        {
            return state switch
            {
                CameraState.Stopped => "Start",
                CameraState.Starting => "Wait...",
                CameraState.Running => "Stop",
                CameraState.Stopping => "Wait...",
                _ => "Start"
            };
        }
        return "Start";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts CameraState to the button background brush.
/// </summary>
public class CameraStateToButtonBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CameraState state)
        {
            return state switch
            {
                CameraState.Stopped => Brushes.ForestGreen,
                CameraState.Starting => Brushes.Orange,
                CameraState.Running => Brushes.Crimson,
                CameraState.Stopping => Brushes.Orange,
                _ => Brushes.ForestGreen
            };
        }
        return Brushes.ForestGreen;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts CameraState to a boolean for enabling/disabling UI elements.
/// </summary>
public class CameraStateToIsEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Always return true to preserve button styling (color/text). 
        // Click prevention should be handled by Command CanExecute or ViewModel logic.
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
