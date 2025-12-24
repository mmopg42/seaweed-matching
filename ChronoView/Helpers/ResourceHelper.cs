using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ChronoView.Helpers;

/// <summary>
/// Helper class for loading embedded resources.
/// </summary>
public static class ResourceHelper
{
    /// <summary>
    /// Loads a BitmapSource from embedded resource.
    /// </summary>
    /// <param name="resourcePath">Pack URI path to the resource (e.g., "Resources/Images/nir_placeholder.png")</param>
    /// <returns>Frozen BitmapSource, or null if loading fails</returns>
    public static BitmapSource? LoadImageFromResource(string resourcePath)
    {
        try
        {
            // Create pack URI for the resource
            var uri = new Uri($"pack://application:,,,/{resourcePath}", UriKind.Absolute);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = uri;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            // Freeze for cross-thread usage
            bitmap.Freeze();

            return bitmap;
        }
        catch (Exception)
        {
            // Return null if resource not found or loading fails
            return null;
        }
    }

    /// <summary>
    /// Gets the NIR placeholder image.
    /// </summary>
    public static BitmapSource? GetNirPlaceholder()
    {
        return LoadImageFromResource("Resources/Images/nir_placeholder.png");
    }

    /// <summary>
    /// Gets the settings icon image.
    /// </summary>
    public static BitmapSource? GetSettingsIcon()
    {
        return LoadImageFromResource("Resources/Images/settings_icon.png");
    }

    /// <summary>
    /// Gets the start icon image.
    /// </summary>
    public static BitmapSource? GetStartIcon()
    {
        return LoadImageFromResource("Resources/Images/start_icon.png");
    }

    /// <summary>
    /// Gets the stop icon image.
    /// </summary>
    public static BitmapSource? GetStopIcon()
    {
        return LoadImageFromResource("Resources/Images/stop_icon.png");
    }

    /// <summary>
    /// Gets the refresh icon image.
    /// </summary>
    public static BitmapSource? GetRefreshIcon()
    {
        return LoadImageFromResource("Resources/Images/refresh_icon.png");
    }

    /// <summary>
    /// Gets the next icon image.
    /// </summary>
    public static BitmapSource? GetNextIcon()
    {
        return LoadImageFromResource("Resources/Images/next_icon.png");
    }
}
