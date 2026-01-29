using System.IO;
using ChronoView.Models;

namespace ChronoView.Core.NIR.Shared;

/// <summary>
/// Parses NIR spectrum data from .txt files.
/// </summary>
public static class NirSpectrumParser
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
        double? protein = null;
        double? moisture = null;

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

                // Parse Metadata (Protein/Moisture)
                // Expected format: "Protein: 12.3" or "Moisture\t45.2"
                if (line.Contains("Protein", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Moisture", StringComparison.OrdinalIgnoreCase))
                {
                    // Simple heuristic: find the first number in the line
                    var numberMatch = System.Text.RegularExpressions.Regex.Match(line, @"[+-]?\d+(\.\d+)?");
                    if (numberMatch.Success && double.TryParse(numberMatch.Value, out double val))
                    {
                        if (line.Contains("Protein", StringComparison.OrdinalIgnoreCase))
                            protein = val;
                        else if (line.Contains("Moisture", StringComparison.OrdinalIgnoreCase))
                            moisture = val;
                    }
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
                Intensities = intensities.ToArray(),
                Protein = protein,
                Moisture = moisture
            };
        }
        catch (Exception)
        {
            // Return null on any parsing error
            return null;
        }
    }
}
