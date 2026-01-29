using System.Windows.Media.Imaging;
using ChronoView.Core.NIR.Interfaces;
using ChronoView.Core.NIR.Shared;
using ChronoView.Models;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.NIR.Line1;

/// <summary>
/// File-based NIR data provider for Line 1.
/// Loads NIR spectrum data from .txt/.spc files and generates visualization thumbnails.
/// </summary>
public class FileBasedNirProvider : INirDataProvider
{
    private readonly ILogger? _logger;

    public FileBasedNirProvider(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Load NIR spectrum data from a file synchronously.
    /// </summary>
    /// <param name="filePath">Path to the NIR file (.txt or .spc)</param>
    /// <returns>NirSpectrum data if successful, null otherwise</returns>
    public NirSpectrum? LoadNirData(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        // For .spc files, try to find corresponding .txt file
        string? txtFilePath = GetTextFilePath(filePath);
        if (txtFilePath != null)
        {
            var result = NirSpectrumParser.Parse(txtFilePath);
            if (result != null)
                return result;
        }

        // Fallback: try parsing the file directly
        return NirSpectrumParser.Parse(filePath);
    }

    /// <summary>
    /// Load NIR spectrum data asynchronously.
    /// </summary>
    /// <param name="filePath">Path to the NIR file</param>
    /// <returns>NirSpectrum data if successful, null otherwise</returns>
    public System.Threading.Tasks.Task<NirSpectrum?> LoadNirDataAsync(string filePath)
    {
        return System.Threading.Tasks.Task.Run(() => LoadNirData(filePath));
    }

    /// <summary>
    /// Generate a thumbnail image for NIR spectrum visualization.
    /// </summary>
    /// <param name="data">NIR spectrum data</param>
    /// <param name="width">Thumbnail width in pixels</param>
    /// <param name="height">Thumbnail height in pixels</param>
    /// <returns>BitmapSource for the thumbnail</returns>
    public System.Threading.Tasks.Task<BitmapSource?> GenerateThumbnailAsync(
        NirSpectrum data,
        int width,
        int height)
    {
        return System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                return NirGraphGenerator.GenerateGraph(data, width, height);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to generate NIR thumbnail for {FilePath}", data.FilePath);
                return null;
            }
        });
    }

    /// <summary>
    /// Get the corresponding .txt file path for a given NIR file.
    /// Handles the .spc + .txt file set pattern with 'A' suffix.
    /// </summary>
    /// <param name="filePath">Path to the NIR file</param>
    /// <returns>Path to the .txt file, or null if not found</returns>
    private static string? GetTextFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        var directory = System.IO.Path.GetDirectoryName(filePath);
        var fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(filePath);

        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileNameWithoutExt))
            return null;

        // Try A.txt suffix first (priority)
        var txtPathA = System.IO.Path.Combine(directory, fileNameWithoutExt + "A.txt");
        if (System.IO.File.Exists(txtPathA))
            return txtPathA;

        // Fallback to plain .txt
        var txtPath = System.IO.Path.Combine(directory, fileNameWithoutExt + ".txt");
        if (System.IO.File.Exists(txtPath))
            return txtPath;

        return null;
    }
}
