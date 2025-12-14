using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChronoView.Helpers;

/// <summary>
/// Helper class for generating placeholder and error icon images for progressive loading.
/// </summary>
public static class PlaceholderImageHelper
{
    private static readonly Lazy<BitmapSource> _placeholderImage = 
        new Lazy<BitmapSource>(CreatePlaceholder);
    
    private static readonly Lazy<BitmapSource> _errorIcon = 
        new Lazy<BitmapSource>(CreateErrorIcon);
    
    /// <summary>
    /// Gets a light gray placeholder image (120x90).
    /// Thread-safe, immutable (frozen), and lazily initialized.
    /// </summary>
    public static BitmapSource PlaceholderImage => _placeholderImage.Value;
    
    /// <summary>
    /// Gets a red error icon with white X (120x90).
    /// Thread-safe, immutable (frozen), and lazily initialized.
    /// </summary>
    public static BitmapSource ErrorIcon => _errorIcon.Value;
    
    /// <summary>
    /// Creates a light gray placeholder image.
    /// </summary>
    private static BitmapSource CreatePlaceholder()
    {
        const int width = 120;
        const int height = 90;
        
        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
        
        // Light gray background (#E6E6E6 = RGB 230,230,230)
        var pixels = new byte[width * height * 3];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = 230;
        
        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 3, 0);
        bitmap.Freeze(); // Make thread-safe and immutable
        return bitmap;
    }
    
    /// <summary>
    /// Creates a red error icon with a white X.
    /// </summary>
    private static BitmapSource CreateErrorIcon()
    {
        const int width = 120;
        const int height = 90;
        
        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
        
        // Red background with white X
        var pixels = new byte[width * height * 4];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = (y * width + x) * 4;
                
                // Red background (BGR format)
                pixels[idx] = 80;      // Blue
                pixels[idx + 1] = 80;  // Green
                pixels[idx + 2] = 200; // Red
                pixels[idx + 3] = 255; // Alpha
                
                // Draw white X (thickness = 3 pixels)
                bool isOnDiagonal1 = Math.Abs(x - y) < 3;
                bool isOnDiagonal2 = Math.Abs(x - (height - y)) < 3;
                
                if (isOnDiagonal1 || isOnDiagonal2)
                {
                    pixels[idx] = 255;     // Blue
                    pixels[idx + 1] = 255; // Green
                    pixels[idx + 2] = 255; // Red
                    pixels[idx + 3] = 255; // Alpha
                }
            }
        }
        
        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
        bitmap.Freeze(); // Make thread-safe and immutable
        return bitmap;
    }
}
