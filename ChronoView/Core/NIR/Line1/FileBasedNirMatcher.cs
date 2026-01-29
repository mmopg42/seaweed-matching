using ChronoView.Core.NIR.Interfaces;
using ChronoView.Models;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.NIR.Line1;

/// <summary>
/// File-based NIR matcher for Line 1.
/// Matches NIR files to file groups based on timestamp proximity.
/// </summary>
public class FileBasedNirMatcher : INirMatcher
{
    private readonly ILogger? _logger;

    public FileBasedNirMatcher(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Match NIR files to existing file groups based on timestamp proximity.
    /// NIR files are matched to the closest group within the time window.
    /// </summary>
    /// <param name="groups">Existing file groups to match against</param>
    /// <param name="nirFiles">NIR files to match (Key, Path, Timestamp)</param>
    /// <param name="nirMaxDiffSeconds">Maximum time difference for matching in seconds</param>
    /// <param name="lineNumber">Line number (1 or 2)</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>Updated file groups with NIR attachments</returns>
    public List<FileGroup> MatchNirToGroups(
        List<FileGroup> groups,
        List<(string Key, string Path, DateTime Timestamp)> nirFiles,
        double nirMaxDiffSeconds,
        int lineNumber,
        ILogger? logger = null)
    {
        var log = logger ?? _logger;

        foreach (var nir in nirFiles)
        {
            int? targetIdx = null;
            double? minDiff = null;

            log?.LogDebug("[MATCH-NIR] NIR {NirKey} ts={NirTs:HH:mm:ss.fff}: Searching {GroupCount} groups (maxDiff={Max}s)",
                nir.Key, nir.Timestamp, groups.Count, nirMaxDiffSeconds);

            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].HasNir)
                {
                    log?.LogDebug("[MATCH-NIR]   Group[{Index}]: Already has NIR, skip", i);
                    continue; // Already has NIR
                }

                var timeDiff = (nir.Timestamp - groups[i].CreatedAt).TotalSeconds;
                var absDiff = System.Math.Abs(timeDiff);

                log?.LogDebug("[MATCH-NIR]   Group[{Index}]: ts={GroupTs:HH:mm:ss.fff} diff={Diff:F3}s (abs={AbsDiff:F3}s)",
                    i, groups[i].CreatedAt, timeDiff, absDiff);

                // Must be within configured time difference
                if (absDiff <= nirMaxDiffSeconds)
                {
                    if (!minDiff.HasValue || absDiff < minDiff.Value)
                    {
                        minDiff = absDiff;
                        targetIdx = i;
                    }
                }
                else
                {
                    log?.LogDebug("[MATCH-NIR]   ✗ REJECTED: absDiff={AbsDiff:F3}s > maxDiff={Max}s", absDiff, nirMaxDiffSeconds);
                }
            }

            if (targetIdx.HasValue)
            {
                // Attach to existing group
                groups[targetIdx.Value].NirKey = nir.Key;
                groups[targetIdx.Value].NirFilePath = nir.Path;
                groups[targetIdx.Value].HasNir = true;
                log?.LogDebug("[MATCH-NIR] ✓ MATCHED: NIR {NirKey} → Group[{Idx}] absDiff={Diff:F3}s file={File}",
                    nir.Key, targetIdx.Value, minDiff, System.IO.Path.GetFileName(nir.Path));
            }
            else
            {
                // Create NIR-only group
                log?.LogDebug("[MATCH-NIR] ✗ NO MATCH: Creating NIR-only group for {NirKey}", nir.Key);
                var nirOnlyGroup = new FileGroup
                {
                    GroupId = "",
                    NirKey = nir.Key,
                    NirFilePath = nir.Path,
                    CreatedAt = nir.Timestamp,
                    Timestamp = nir.Timestamp,
                    Status = GroupStatus.Complete,
                    HasNir = true,
                    LineNumber = lineNumber,
                    CameraFiles = new System.Collections.Generic.Dictionary<string, string>()
                };
                groups.Add(nirOnlyGroup);
                log?.LogDebug("Created NIR-only group for {NirKey} ts={Ts} path={Path}",
                    nir.Key, nir.Timestamp, nir.Path);
            }
        }

        return groups;
    }

    /// <summary>
    /// Get available NIR files from unmatched files, excluding already consumed keys.
    /// </summary>
    /// <param name="unmatchedFiles">Unmatched files container</param>
    /// <param name="nirKey">NIR key (e.g., "nir1" or "nir2")</param>
    /// <param name="consumedNirKeys">Set of already consumed NIR keys</param>
    /// <returns>List of available NIR files with timestamps</returns>
    public List<(string Key, string Path, DateTime Timestamp)> GetAvailableNirFiles(
        UnmatchedFiles unmatchedFiles,
        string nirKey,
        System.Collections.Generic.HashSet<string> consumedNirKeys)
    {
        var nirFiles = unmatchedFiles.NirFiles.ContainsKey(nirKey)
            ? unmatchedFiles.NirFiles[nirKey]
            : new System.Collections.Generic.Dictionary<string, string>();

        var availableNirs = new List<(string Key, string Path, DateTime Timestamp)>();

        foreach (var kvp in nirFiles)
        {
            if (consumedNirKeys.Contains(kvp.Key))
                continue;

            var timestamp = ExtractTimestampFromNirKey(kvp.Key);

            // Fallback to LastWriteTime if name extraction fails
            if (!timestamp.HasValue && System.IO.File.Exists(kvp.Value))
            {
                try { timestamp = System.IO.File.GetLastWriteTime(kvp.Value); }
                catch { }
            }

            if (timestamp.HasValue)
            {
                availableNirs.Add((kvp.Key, kvp.Value, timestamp.Value));
            }
        }

        // Sort by timestamp
        availableNirs.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

        return availableNirs;
    }

    /// <summary>
    /// Extract timestamp from NIR key.
    /// Supports multiple formats: run_120251201T140542, 20251201T140542, etc.
    /// </summary>
    /// <param name="nirKey">NIR file key</param>
    /// <returns>Extracted timestamp or null</returns>
    private static DateTime? ExtractTimestampFromNirKey(string nirKey)
    {
        if (string.IsNullOrEmpty(nirKey))
            return null;

        // Pattern 1: run_N prefix followed by 8 digits (YYYYMMDD) + T + 6 digits (time)
        // Example: run_120251201T140542 -> extract 20251201T140542
        var match = System.Text.RegularExpressions.Regex.Match(nirKey, @"run_\d(\d{8}T\d{6})");
        if (match.Success)
        {
            if (DateTime.TryParseExact(
                match.Groups[1].Value,
                "yyyyMMddTHHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var result))
            {
                return result;
            }
        }

        // Pattern 2: 8 digits (YYYYMMDD) + T + 6 digits (time) without prefix
        // Example: 20250926T103033
        match = System.Text.RegularExpressions.Regex.Match(nirKey, @"(\d{8}T\d{6})");
        if (match.Success)
        {
            if (DateTime.TryParseExact(
                match.Groups[1].Value,
                "yyyyMMddTHHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var result))
            {
                return result;
            }
        }

        return null;
    }
}
