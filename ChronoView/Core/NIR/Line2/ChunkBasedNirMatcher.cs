using ChronoView.Core.NIR.Interfaces;
using ChronoView.Models;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.NIR.Line2;

/// <summary>
/// Chunk-based NIR matcher for Line 2.
/// Matches NIR2 chunks to file groups based on timestamp proximity.
/// </summary>
public class ChunkBasedNirMatcher : INirMatcher
{
    private readonly ILogger? _logger;
    private readonly Dictionary<string, Nir2Chunk> _pendingChunks;
    private readonly HashSet<string> _consumedChunkIds;

    /// <summary>
    /// Initializes a new instance of the ChunkBasedNirMatcher.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics</param>
    public ChunkBasedNirMatcher(ILogger? logger = null)
    {
        _logger = logger;
        _pendingChunks = new Dictionary<string, Nir2Chunk>();
        _consumedChunkIds = new HashSet<string>();
    }

    /// <summary>
    /// Registers a completed chunk for matching.
    /// </summary>
    /// <param name="chunk">The completed chunk to register</param>
    public void RegisterChunk(Nir2Chunk chunk)
    {
        if (chunk == null || string.IsNullOrEmpty(chunk.ChunkId))
            return;

        _pendingChunks[chunk.ChunkId] = chunk;
        _logger?.LogInformation("[NIR2-Matcher] Registered chunk {ChunkId}: {SampleCount} samples, " +
            "P={Protein:F2}%, M={Moisture:F2}%, ts={Timestamp:HH:mm:ss.fff}",
            chunk.ChunkId, chunk.SampleCount, chunk.AggregatedProtein,
            chunk.AggregatedMoisture, chunk.AggregatedTimestamp);
    }

    /// <summary>
    /// Matches NIR2 chunks to existing file groups based on timestamp proximity.
    /// Chunks are matched to the closest group within the time window.
    /// </summary>
    /// <param name="groups">Existing file groups to match against</param>
    /// <param name="nirFiles">NIR files to match (format: "chunk:{chunkId}", path, timestamp)</param>
    /// <param name="nirMaxDiffSeconds">Maximum time difference for matching in seconds</param>
    /// <param name="lineNumber">Line number (1 or 2)</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>Updated file groups with NIR2 attachments</returns>
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
            // Extract chunk ID from key format "chunk:{chunkId}"
            string chunkId = nir.Key.StartsWith("chunk:", StringComparison.Ordinal)
                ? nir.Key.Substring(6)
                : nir.Key;

            // Skip if chunk not found or already consumed
            if (!_pendingChunks.TryGetValue(chunkId, out var chunk))
            {
                log?.LogWarning("[NIR2-Matcher] Chunk {ChunkId} not found in pending chunks", chunkId);
                continue;
            }

            if (_consumedChunkIds.Contains(chunkId))
            {
                log?.LogDebug("[NIR2-Matcher] Chunk {ChunkId} already consumed, skipping", chunkId);
                continue;
            }

            int? targetIdx = null;
            double? minDiff = null;

            log?.LogDebug("[NIR2-MATCH] Chunk {ChunkId} ts={ChunkTs:HH:mm:ss.fff}: Searching {GroupCount} groups (maxDiff={Max}s)",
                chunkId, chunk.AggregatedTimestamp ?? chunk.StartedAt, groups.Count, nirMaxDiffSeconds);

            // Find the best matching group
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].HasNir)
                {
                    log?.LogDebug("[NIR2-MATCH]   Group[{Index}]: Already has NIR, skip", i);
                    continue; // Already has NIR
                }

                var chunkTimestamp = chunk.AggregatedTimestamp ?? chunk.StartedAt;
                var timeDiff = (chunkTimestamp - groups[i].CreatedAt).TotalSeconds;
                var absDiff = System.Math.Abs(timeDiff);

                log?.LogDebug("[NIR2-MATCH]   Group[{Index}]: ts={GroupTs:HH:mm:ss.fff} diff={Diff:F3}s (abs={AbsDiff:F3}s)",
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
                    log?.LogDebug("[NIR2-MATCH]   ✗ REJECTED: absDiff={AbsDiff:F3}s > maxDiff={Max}s", absDiff, nirMaxDiffSeconds);
                }
            }

            if (targetIdx.HasValue)
            {
                // Attach to existing group
                groups[targetIdx.Value].NirKey = nir.Key;
                groups[targetIdx.Value].NirFilePath = nir.Path;
                groups[targetIdx.Value].HasNir = true;

                // NIR2 data (protein, moisture, chunk_id, sample_count) is stored in the chunk
                // which is cached in ApiBasedNirProvider. The NirKey contains the chunk ID
                // for lookup (format: "chunk:{chunkId}").

                log?.LogDebug("[NIR2-MATCH] ✓ MATCHED: Chunk {ChunkId} → Group[{Idx}] absDiff={Diff:F3}s P={Protein:F2}% M={Moisture:F2}%",
                    chunkId, targetIdx.Value, minDiff, chunk.AggregatedProtein, chunk.AggregatedMoisture);
            }
            else
            {
                // Create NIR-only group for this chunk
                log?.LogDebug("[NIR2-MATCH] ✗ NO MATCH: Creating NIR-only group for chunk {ChunkId}", chunkId);

                var chunkTimestamp = chunk.AggregatedTimestamp ?? chunk.StartedAt;
                var nirOnlyGroup = new FileGroup
                {
                    GroupId = "",
                    NirKey = nir.Key,
                    NirFilePath = nir.Path,
                    CreatedAt = chunkTimestamp,
                    Timestamp = chunkTimestamp,
                    Status = GroupStatus.Complete,
                    HasNir = true,
                    LineNumber = lineNumber,
                    CameraFiles = new Dictionary<string, string>()
                    // NIR2 data (protein, moisture, etc.) is stored in the chunk
                    // via ApiBasedNirProvider.RegisterChunk()
                };
                groups.Add(nirOnlyGroup);

                log?.LogDebug("Created NIR-only group for chunk {ChunkId} ts={Ts:HH:mm:ss.fff} P={Protein:F2}% M={Moisture:F2}%",
                    chunkId, chunkTimestamp, chunk.AggregatedProtein, chunk.AggregatedMoisture);
            }

            // Mark chunk as consumed
            _consumedChunkIds.Add(chunkId);
            _pendingChunks.Remove(chunkId);
        }

        return groups;
    }

    /// <summary>
    /// Gets available NIR2 chunks from the matcher.
    /// For NIR2, this returns pending chunks that haven't been consumed yet.
    /// </summary>
    /// <param name="unmatchedFiles">Unmatched files container (not used for NIR2)</param>
    /// <param name="nirKey">NIR key (e.g., "nir2")</param>
    /// <param name="consumedNirKeys">Set of already consumed NIR keys</param>
    /// <returns>List of available NIR2 chunks with timestamps</returns>
    public List<(string Key, string Path, DateTime Timestamp)> GetAvailableNirFiles(
        UnmatchedFiles unmatchedFiles,
        string nirKey,
        HashSet<string> consumedNirKeys)
    {
        var availableChunks = new List<(string Key, string Path, DateTime Timestamp)>();

        foreach (var kvp in _pendingChunks)
        {
            var chunkId = kvp.Key;
            var chunk = kvp.Value;

            // Skip if already consumed
            if (consumedNirKeys.Contains($"chunk:{chunkId}") || _consumedChunkIds.Contains(chunkId))
                continue;

            var timestamp = chunk.AggregatedTimestamp ?? chunk.StartedAt;
            availableChunks.Add(($"chunk:{chunkId}", $"chunk:{chunkId}", timestamp));
        }

        // Sort by timestamp
        availableChunks.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

        return availableChunks;
    }

    /// <summary>
    /// Gets the count of pending chunks waiting to be matched.
    /// </summary>
    public int PendingChunkCount => _pendingChunks.Count;

    /// <summary>
    /// Gets the count of consumed chunks.
    /// </summary>
    public int ConsumedChunkCount => _consumedChunkIds.Count;

    /// <summary>
    /// Clears all pending and consumed chunks.
    /// </summary>
    public void ClearChunks()
    {
        var pendingCount = _pendingChunks.Count;
        var consumedCount = _consumedChunkIds.Count;

        _pendingChunks.Clear();
        _consumedChunkIds.Clear();

        _logger?.LogDebug("[NIR2-Matcher] Cleared {Pending} pending and {Consumed} consumed chunks",
            pendingCount, consumedCount);
    }

    /// <summary>
    /// Gets diagnostic information about the matcher state.
    /// </summary>
    /// <returns>Diagnostic string</returns>
    public string GetDiagnosticInfo()
    {
        return $"NIR2 Matcher: Pending={PendingChunkCount}, Consumed={ConsumedChunkCount}";
    }
}
