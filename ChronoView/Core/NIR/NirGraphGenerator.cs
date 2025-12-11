using System.Windows.Media.Imaging;
using ChronoView.Models;
using ScottPlot;

namespace ChronoView.Core.Nir;

/// <summary>
/// Generates NIR spectrum graph images using ScottPlot.
/// </summary>
public class NirGraphGenerator
{
    /// <summary>
    /// Generates a BitmapSource image of the NIR spectrum graph.
    /// </summary>
    /// <param name="spectrum">The NIR spectrum data to visualize</param>
    /// <param name="width">Width of the output image in pixels</param>
    /// <param name="height">Height of the output image in pixels</param>
    /// <returns>Frozen BitmapSource ready for UI binding, or null if generation fails</returns>
    public static BitmapSource? GenerateGraph(NirSpectrum spectrum, int width, int height)
    {
        if (spectrum == null || !spectrum.IsValid())
            return null;

        if (width <= 0 || height <= 0)
            return null;

        try
        {
            var plot = new Plot();

            // Add signal plot for efficient waveform rendering
            var signal = plot.Add.Signal(spectrum.Intensities);
            signal.Color = ScottPlot.Color.FromHex("#0078d4"); // Blue
            signal.LineWidth = 1;

            // Map wavelength range to X-axis
            var wavelengths = spectrum.Wavelengths;
            signal.Data.XOffset = wavelengths.Min();
            signal.Data.Period = (wavelengths.Max() - wavelengths.Min()) / wavelengths.Length;

            // Minimalist styling for thumbnail
            plot.Axes.Frameless();
            plot.HideGrid();
            plot.Layout.Frameless();

            // Background color
            plot.FigureBackground.Color = ScottPlot.Color.FromHex("#f5f5f5"); // Light gray
            plot.DataBackground.Color = ScottPlot.Color.FromHex("#f5f5f5");

            // Fallback: Save to temp file and read bytes if direct byte generation is ambiguous
            string tempFile = System.IO.Path.GetTempFileName();
            try 
            {
                plot.SavePng(tempFile, width, height);
                byte[] bytes = System.IO.File.ReadAllBytes(tempFile);
                
                // Create WPF BitmapImage
                using var stream = new System.IO.MemoryStream(bytes);
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();

                // CRITICAL: Freeze for cross-thread usage
                image.Freeze();

                return image;
            }
            finally
            {
                if (System.IO.File.Exists(tempFile))
                    System.IO.File.Delete(tempFile);
            }
        }
        catch (Exception)
        {
            // Return null on any rendering error
            return null;
        }
    }
}
