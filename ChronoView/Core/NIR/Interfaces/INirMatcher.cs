using ChronoView.Models;

namespace ChronoView.Core.NIR.Interfaces;

/// <summary>
/// Interface for NIR file matching operations.
/// Enables abstraction between different NIR implementations (file-based vs API-based).
/// </summary>
public interface INirMatcher
{
    /// <summary>
    /// Match NIR files to existing file groups based on timestamp proximity.
    /// </summary>
    /// <param name="groups">Existing file groups to match against</param>
    /// <param name="nirFiles">NIR files to match (Key, Path, Timestamp)</param>
    /// <param name="nirMaxDiffSeconds">Maximum time difference for matching in seconds</param>
    /// <param name="lineNumber">Line number (1 or 2)</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>Updated file groups with NIR attachments</returns>
    List<FileGroup> MatchNirToGroups(
        List<FileGroup> groups,
        List<(string Key, string Path, DateTime Timestamp)> nirFiles,
        double nirMaxDiffSeconds,
        int lineNumber,
        Microsoft.Extensions.Logging.ILogger? logger = null);

    /// <summary>
    /// Get available NIR files from unmatched files, excluding already consumed keys.
    /// </summary>
    /// <param name="unmatchedFiles">Unmatched files container</param>
    /// <param name="nirKey">NIR key (e.g., "nir1" or "nir2")</param>
    /// <param name="consumedNirKeys">Set of already consumed NIR keys</param>
    /// <returns>List of available NIR files with timestamps</returns>
    List<(string Key, string Path, DateTime Timestamp)> GetAvailableNirFiles(
        Models.UnmatchedFiles unmatchedFiles,
        string nirKey,
        System.Collections.Generic.HashSet<string> consumedNirKeys);
}
