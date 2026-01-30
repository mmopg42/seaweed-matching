using System.Windows.Media.Imaging;
using ChronoView.Core.NIR.Interfaces;
using ChronoView.Models;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.NIR.Line2;

/// <summary>
/// API-based NIR data provider for Line 2.
/// Loads NIR2 data from collected chunks (protein/moisture) rather than spectrum files.
/// </summary>
public class ApiBasedNirProvider : INirDataProvider
{
    private readonly ILogger? _logger;
    private readonly Dictionary<string, NirSpectrum> _chunkCache;
    private readonly INir2ChunkFileStorage? _chunkStorage;

    /// <summary>
    /// Initializes a new instance of the ApiBasedNirProvider.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <param name="chunkStorage">Optional chunk file storage for loading chunk files</param>
    public ApiBasedNirProvider(ILogger? logger = null, INir2ChunkFileStorage? chunkStorage = null)
    {
        _logger = logger;
        _chunkStorage = chunkStorage;
        _chunkCache = new Dictionary<string, NirSpectrum>();
    }

    /// <summary>
    /// Registers a chunk for later retrieval by FileGroup matching.
    /// </summary>
    /// <param name="chunkId">Unique chunk identifier</param>
    /// <param name="chunk">The chunk data to cache</param>
    public void RegisterChunk(string chunkId, Nir2Chunk chunk)
    {
        var spectrum = CreateSpectrumFromChunk(chunk);
        if (spectrum != null)
        {
            _chunkCache[chunkId] = spectrum;
            _logger?.LogDebug("[NIR2-Provider] Registered chunk {ChunkId} with P={Protein:F2}%, M={Moisture:F2}%",
                chunkId, chunk.AggregatedProtein, chunk.AggregatedMoisture);
        }
    }

    /// <summary>
    /// Loads NIR2 data from a cached chunk by chunk ID or from a chunk file.
    /// The filePath parameter can be:
    /// - "chunk:{chunkId}" - cached chunk reference
    /// - Path to a .txt chunk file (e.g., "chunks/2026-01-29/2026-01-29T15-30-45.txt")
    /// </summary>
    /// <param name="filePath">Chunk ID with "chunk:" prefix or chunk file path</param>
    /// <returns>NirSpectrum with protein/moisture data, or null if not found</returns>
    public NirSpectrum? LoadNirData(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        // Handle chunk reference format: "chunk:{chunkId}"
        if (filePath.StartsWith("chunk:", StringComparison.Ordinal))
        {
            var chunkId = filePath.Substring(6); // Remove "chunk:" prefix
            if (_chunkCache.TryGetValue(chunkId, out var spectrum))
            {
                return spectrum;
            }

            _logger?.LogWarning("[NIR2-Provider] Chunk {ChunkId} not found in cache", chunkId);
            return null;
        }

        // Handle chunk file path ending with .txt
        if (filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) && _chunkStorage != null)
        {
            return LoadChunkFromFile(filePath);
        }

        _logger?.LogDebug("[NIR2-Provider] Cannot load NIR2 data from path: {FilePath}", filePath);
        return null;
    }

    /// <summary>
    /// Loads a chunk from a .txt file.
    /// </summary>
    private NirSpectrum? LoadChunkFromFile(string filePath)
    {
        if (_chunkStorage == null)
            return null;

        try
        {
            var chunk = _chunkStorage.LoadChunk(filePath);
            if (chunk != null)
            {
                var spectrum = CreateSpectrumFromChunk(chunk);
                if (spectrum != null)
                {
                    // Update file path to point to the actual file
                    spectrum.FilePath = filePath;
                    _logger?.LogDebug("[NIR2-Provider] Loaded chunk from file: {FilePath}", filePath);
                    return spectrum;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[NIR2-Provider] Failed to load chunk file: {FilePath}", filePath);
        }

        return null;
    }

    /// <summary>
    /// Loads NIR2 data asynchronously from a cached chunk.
    /// </summary>
    /// <param name="filePath">Chunk ID with "chunk:" prefix or CSV file path</param>
    /// <returns>NirSpectrum with protein/moisture data, or null if not found</returns>
    public System.Threading.Tasks.Task<NirSpectrum?> LoadNirDataAsync(string filePath)
    {
        return System.Threading.Tasks.Task.FromResult(LoadNirData(filePath));
    }

    /// <summary>
    /// Generates a thumbnail for NIR2 data visualization.
    /// Since NIR2 only provides protein/moisture values, generates a simple text-based visualization.
    /// </summary>
    /// <param name="data">NIR spectrum data</param>
    /// <param name="width">Thumbnail width in pixels</param>
    /// <param name="height">Thumbnail height in pixels</param>
    /// <returns>BitmapSource for the thumbnail (placeholder text image)</returns>
    public System.Threading.Tasks.Task<BitmapSource?> GenerateThumbnailAsync(
        NirSpectrum data,
        int width,
        int height)
    {
        return System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                // For NIR2, create a simple text-based visualization
                // showing protein and moisture values
                _logger?.LogDebug("[NIR2-Provider] Generating thumbnail for {FileName}: P={Protein}%, M={Moisture}%",
                    data.FileName, data.Protein, data.Moisture);

                // Return null - UI will handle NIR2 text display separately
                // NIR2 uses Nir2DisplayTemplate for text-based display
                return (BitmapSource?)null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to generate NIR2 thumbnail for {FilePath}", data.FilePath);
                return (BitmapSource?)null;
            }
        });
    }

    /// <summary>
    /// Creates a NirSpectrum from a NIR2 chunk for compatibility with existing NIR infrastructure.
    /// </summary>
    /// <param name="chunk">The NIR2 chunk</param>
    /// <returns>NirSpectrum with chunk data in metadata</returns>
    private static NirSpectrum? CreateSpectrumFromChunk(Nir2Chunk chunk)
    {
        if (chunk.SampleCount == 0)
            return null;

        var spectrum = new NirSpectrum
        {
            FileName = $"nir2_{chunk.ChunkId}",
            FilePath = $"chunk:{chunk.ChunkId}",
            Timestamp = chunk.AggregatedTimestamp ?? chunk.StartedAt,
            Protein = chunk.AggregatedProtein,
            Moisture = chunk.AggregatedMoisture,
            Wavelengths = Array.Empty<double>(), // NIR2 doesn't have spectral data
            Intensities = Array.Empty<double>(), // NIR2 doesn't have spectral data
            Metadata = new Dictionary<string, object>
            {
                ["chunk_id"] = chunk.ChunkId,
                ["line_number"] = chunk.LineNumber,
                ["sample_count"] = chunk.SampleCount,
                ["started_at"] = chunk.StartedAt,
                ["ended_at"] = chunk.EndedAt,
                ["aggregation_strategy"] = "applied"
            }
        };

        return spectrum;
    }

    /// <summary>
    /// Gets all cached chunk IDs.
    /// </summary>
    /// <returns>Collection of cached chunk IDs</returns>
    public IEnumerable<string> GetCachedChunkIds()
    {
        return _chunkCache.Keys;
    }

    /// <summary>
    /// Removes a chunk from the cache.
    /// </summary>
    /// <param name="chunkId">Chunk ID to remove</param>
    /// <returns>True if removed, false if not found</returns>
    public bool RemoveChunk(string chunkId)
    {
        return _chunkCache.Remove(chunkId);
    }

    /// <summary>
    /// Clears all cached chunks.
    /// </summary>
    public void ClearCache()
    {
        var count = _chunkCache.Count;
        _chunkCache.Clear();
        _logger?.LogDebug("[NIR2-Provider] Cleared {Count} cached chunks", count);
    }

    /// <summary>
    /// Gets the count of cached chunks.
    /// </summary>
    public int CachedChunkCount => _chunkCache.Count;
}
