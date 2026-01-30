using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace ChronoView.Core.FileOperations;

/// <summary>
/// Shared utility for file path segment validation and processing.
/// Extracted from FileGroupOperator for reuse across Line1 and Line2 path builders.
/// Note: Distinct from PathHelper in Core/Configuration which handles application config paths.
/// </summary>
public static class PathSegmentHelper
{
    private static readonly Regex DatePattern = new Regex("^(19|20)\\d{6}$", RegexOptions.Compiled);

    /// <summary>
    /// Checks if the path contains a valid yyyyMMdd date segment.
    /// Validates each path segment using DateTime.TryParseExact to ensure it's a real date.
    /// </summary>
    /// <param name="path">Path to check for date segment.</param>
    /// <returns>True if path contains a valid date segment, false otherwise.</returns>
    public static bool HasValidDateSegment(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (var segment in segments)
        {
            // Quick filter: must be 8 characters matching yyyyMMdd pattern
            if (segment.Length == 8 && DatePattern.IsMatch(segment))
            {
                // Final validation: must be a valid date
                if (DateTime.TryParseExact(segment, "yyyyMMdd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Ensures the base path has a date segment. If not, adds today's date.
    /// </summary>
    /// <param name="basePath">Base path to check and modify if needed.</param>
    /// <returns>Path with guaranteed date segment.</returns>
    public static string EnsureDateRoot(string basePath)
    {
        if (HasValidDateSegment(basePath))
        {
            return basePath;
        }

        var today = DateTime.Now.ToString("yyyyMMdd");
        return Path.Combine(basePath, today);
    }
}
