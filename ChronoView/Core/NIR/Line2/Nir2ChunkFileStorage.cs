using System.Globalization;
using System.IO;
using ChronoView.Core.NIR.Shared;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.NIR.Line2;

/// <summary>
/// Saves and loads NIR2 chunk data to/from disk as text files.
/// Files are organized in date-based subdirectories under the "chunks" folder.
/// </summary>
public class Nir2ChunkFileStorage : INir2ChunkFileStorage
{
    private const string ChunksFolderName = "chunks";
    private readonly object _lock = new();
    private readonly ILogger? _logger;

    public Nir2ChunkFileStorage(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public string? SaveChunk(Nir2Chunk chunk, string baseDirectory)
    {
        if (chunk == null)
            return null;

        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            _logger?.LogWarning("Cannot save chunk {ChunkId}: chunk storage path is not configured", chunk.ChunkId);
            return null;
        }

        if (chunk.AggregatedProtein == null || chunk.AggregatedMoisture == null)
        {
            _logger?.LogWarning("Cannot save chunk {ChunkId}: missing aggregated values", chunk.ChunkId);
            return null;
        }

        lock (_lock)
        {
            try
            {
                var chunksDir = GetChunksDirectory(baseDirectory);
                var dateDir = GetDateDirectory(chunksDir, chunk.AggregatedTimestamp ?? chunk.StartedAt);
                var fileName = GetFileName(chunk.AggregatedTimestamp ?? chunk.StartedAt);
                var filePath = Path.Combine(dateDir, fileName);

                // Create directory if it doesn't exist
                Directory.CreateDirectory(dateDir);

                // Write chunk data to file
                var content = FormatChunkContent(chunk);
                File.WriteAllText(filePath, content);

                _logger?.LogDebug("Saved chunk {ChunkId} to {FilePath}", chunk.ChunkId, filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save chunk {ChunkId}", chunk.ChunkId);
                return null;
            }
        }
    }

    /// <inheritdoc/>
    public Nir2Chunk? LoadChunk(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return null;

        lock (_lock)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                return ParseChunkFromFile(lines);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load chunk from {FilePath}", filePath);
                return null;
            }
        }
    }

    /// <inheritdoc/>
    public List<Nir2Chunk> LoadAllChunks(string baseDirectory)
    {
        var chunks = new List<Nir2Chunk>();

        lock (_lock)
        {
            try
            {
                var chunksDir = GetChunksDirectory(baseDirectory);
                if (!Directory.Exists(chunksDir))
                    return chunks;

                // Load from all date subdirectories
                foreach (var dateDir in Directory.GetDirectories(chunksDir))
                {
                    foreach (var file in Directory.GetFiles(dateDir, "*.txt"))
                    {
                        var chunk = LoadChunk(file);
                        if (chunk != null)
                        {
                            chunks.Add(chunk);
                        }
                    }
                }

                // Order by timestamp descending (newest first)
                chunks = chunks.OrderByDescending(c => c.AggregatedTimestamp ?? c.StartedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load chunks from {BaseDirectory}", baseDirectory);
            }
        }

        return chunks;
    }

    /// <inheritdoc/>
    public string GetChunksDirectory(string baseDirectory)
    {
        return Path.Combine(baseDirectory, ChunksFolderName);
    }

    /// <inheritdoc/>
    public List<Nir2Chunk> LoadChunksInRange(string baseDirectory, DateTime startInclusive, DateTime endInclusive)
    {
        var chunks = new List<Nir2Chunk>();

        lock (_lock)
        {
            try
            {
                var chunksDir = GetChunksDirectory(baseDirectory);
                if (!Directory.Exists(chunksDir))
                {
                    _logger?.LogDebug("Chunks directory does not exist: {Dir}", chunksDir);
                    return chunks;
                }

                // Iterate through date subdirectories
                foreach (var dateDir in Directory.GetDirectories(chunksDir))
                {
                    // Early termination: if date directory is completely outside range, skip
                    var dirDate = ParseDateFromDirectory(dateDir);
                    if (dirDate.HasValue && IsDateOutsideWindow(dirDate.Value, startInclusive, endInclusive))
                    {
                        continue; // Optimization: skip directories outside window
                    }

                    foreach (var file in Directory.GetFiles(dateDir, "*.txt"))
                    {
                        // Parse timestamp from filename: yyyyMMddTHHmmss.txt
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        if (DateTime.TryParseExact(fileName, "yyyyMMddTHHmmss",
                            null, DateTimeStyles.None, out var fileTimestamp))
                        {
                            // Check if within range
                            if (fileTimestamp >= startInclusive && fileTimestamp <= endInclusive)
                            {
                                var chunk = LoadChunk(file);
                                if (chunk != null)
                                {
                                    chunks.Add(chunk);
                                }
                            }
                        }
                    }
                }

                // Order by timestamp ascending
                chunks = chunks.OrderBy(c => c.AggregatedTimestamp ?? c.StartedAt).ToList();

                _logger?.LogDebug("Loaded {ChunkCount} chunks in range {Start:yyyy-MM-dd HH:mm:ss} to {End:yyyy-MM-dd HH:mm:ss}",
                    chunks.Count, startInclusive, endInclusive);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load chunks in range from {BaseDirectory}", baseDirectory);
            }
        }

        return chunks;
    }

    /// <summary>
    /// Checks if a date is completely outside the time window (for directory-level filtering).
    /// </summary>
    private static bool IsDateOutsideWindow(DateTime date, DateTime windowStart, DateTime windowEnd)
    {
        var dateOnly = date.Date;
        return dateOnly < windowStart.Date.AddDays(-1) || dateOnly > windowEnd.Date.AddDays(1);
    }

    /// <summary>
    /// Parses a date from a directory name (yyyy-MM-dd format).
    /// </summary>
    private static DateTime? ParseDateFromDirectory(string directoryPath)
    {
        var dirName = Path.GetFileName(directoryPath);
        if (DateTime.TryParseExact(dirName, "yyyy-MM-dd", null, DateTimeStyles.None, out var date))
        {
            return date;
        }
        return null;
    }

    /// <summary>
    /// Gets the date-specific subdirectory path.
    /// </summary>
    private static string GetDateDirectory(string chunksDir, DateTime timestamp)
    {
        var dateStr = timestamp.ToString("yyyy-MM-dd");
        return Path.Combine(chunksDir, dateStr);
    }

    /// <summary>
    /// Generates the filename for a chunk based on its timestamp.
    /// Format: yyyyMMddTHHmmss.txt (e.g., 20260130T152121.txt)
    /// </summary>
    private static string GetFileName(DateTime timestamp)
    {
        return $"{timestamp:yyyyMMddTHHmmss}.txt";
    }

    /// <summary>
    /// Formats the chunk data as a string for file storage.
    /// </summary>
    private static string FormatChunkContent(Nir2Chunk chunk)
    {
        return $"ChunkId: {chunk.ChunkId}\n" +
               $"Timestamp: {(chunk.AggregatedTimestamp ?? chunk.StartedAt):o}\n" +
               $"StartedAt: {chunk.StartedAt:o}\n" +
               $"EndedAt: {chunk.EndedAt:o}\n" +
               $"SampleCount: {chunk.SampleCount}\n" +
               $"LineNumber: {chunk.LineNumber}\n" +
               $"AggregationStrategy: {chunk.AggregationStrategy}\n" +
               $"Protein: {chunk.AggregatedProtein.Value:F4}\n" +
               $"Moisture: {chunk.AggregatedMoisture.Value:F4}\n";
    }

    /// <summary>
    /// Parses a chunk from file lines.
    /// </summary>
    private static Nir2Chunk? ParseChunkFromFile(string[] lines)
    {
        var chunk = new Nir2Chunk();
        NirAggregationStrategy strategy = NirAggregationStrategy.First;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
            if (parts.Length != 2)
                continue;

            var key = parts[0];
            var value = parts[1];

            switch (key)
            {
                case "ChunkId":
                    chunk.ChunkId = value;
                    break;
                case "Timestamp":
                    if (DateTime.TryParse(value, out var aggTimestamp))
                        chunk.AggregatedTimestamp = aggTimestamp;
                    break;
                case "StartedAt":
                    if (DateTime.TryParse(value, out var startedAt))
                        chunk.StartedAt = startedAt;
                    break;
                case "EndedAt":
                    if (DateTime.TryParse(value, out var endedAt))
                        chunk.EndedAt = endedAt;
                    break;
                case "SampleCount":
                    if (int.TryParse(value, out var count))
                    {
                        // Pre-allocate samples list for consistency
                        for (int i = 0; i < count; i++)
                            chunk.Samples.Add(new Nir2Sample());
                    }
                    break;
                case "LineNumber":
                    if (int.TryParse(value, out var lineNumber))
                        chunk.LineNumber = lineNumber;
                    break;
                case "AggregationStrategy":
                    if (Enum.TryParse<NirAggregationStrategy>(value, out var strat))
                        strategy = strat;
                    break;
                case "Protein":
                    if (double.TryParse(value, out var protein))
                        chunk.AggregatedProtein = protein;
                    break;
                case "Moisture":
                    if (double.TryParse(value, out var moisture))
                        chunk.AggregatedMoisture = moisture;
                    break;
            }
        }

        // Validate essential fields
        if (string.IsNullOrEmpty(chunk.ChunkId) ||
            chunk.AggregatedProtein == null ||
            chunk.AggregatedMoisture == null)
        {
            return null;
        }

        return chunk;
    }
}
