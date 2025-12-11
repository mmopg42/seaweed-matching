using System.IO;
using ChronoView.Models;

namespace ChronoView.Core.Nir;

/// <summary>
/// Parses NIR spectrum data from .txt files.
/// </summary>
public class NirSpectrumParser
{
    /// <summary>
    /// Parses a NIR spectrum .txt file and returns a NirSpectrum object.
    /// </summary>
    /// <param name="filePath">Full path to the .txt file</param>
    /// <returns>Parsed NirSpectrum object, or null if parsing fails or insufficient data</returns>
    public static NirSpectrum? Parse(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return null;

        var wavelengths = new List<double>();
        var intensities = new List<double>();

        try
        {
            foreach (var line in File.ReadLines(filePath))
            {
                var trimmed = line.Trim();

                // Skip empty lines and header lines starting with #
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                    continue;

                // Parse space/tab/comma-separated values
                var parts = trimmed.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 2 &&
                    double.TryParse(parts[0], out double wavelength) &&
                    double.TryParse(parts[1], out double intensity))
                {
                    wavelengths.Add(wavelength);
                    intensities.Add(intensity);
                }
            }

            // Validation: Minimum 10 data points required
            if (wavelengths.Count < 10)
                return null;

            return new NirSpectrum
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Timestamp = File.GetLastWriteTime(filePath),
                Wavelengths = wavelengths.ToArray(),
                Intensities = intensities.ToArray()
            };
        }
        catch (Exception)
        {
            // Return null on any parsing error
            return null;
        }
    }
}
