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
}
