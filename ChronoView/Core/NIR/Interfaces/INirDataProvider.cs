using System.Windows.Media.Imaging;
using ChronoView.Models;

namespace ChronoView.Core.NIR.Interfaces;

/// <summary>
/// Interface for NIR data loading and processing operations.
/// Handles loading NIR spectrum data and generating visualization thumbnails.
/// </summary>
public interface INirDataProvider
{
    /// <summary>
    /// Load NIR spectrum data from a file.
    /// </summary>
    /// <param name="filePath">Path to the NIR file</param>
    /// <returns>NirSpectrum data if successful, null otherwise</returns>
    NirSpectrum? LoadNirData(string filePath);

    /// <summary>
    /// Load NIR spectrum data asynchronously.
    /// </summary>
    /// <param name="filePath">Path to the NIR file</param>
    /// <returns>NirSpectrum data if successful, null otherwise</returns>
    System.Threading.Tasks.Task<NirSpectrum?> LoadNirDataAsync(string filePath);

    /// <summary>
    /// Generate a thumbnail image for NIR spectrum visualization.
    /// </summary>
    /// <param name="data">NIR spectrum data</param>
    /// <param name="width">Thumbnail width in pixels</param>
    /// <param name="height">Thumbnail height in pixels</param>
    /// <returns>BitmapSource for the thumbnail</returns>
    System.Threading.Tasks.Task<BitmapSource?> GenerateThumbnailAsync(
        NirSpectrum data,
        int width,
        int height);
}
