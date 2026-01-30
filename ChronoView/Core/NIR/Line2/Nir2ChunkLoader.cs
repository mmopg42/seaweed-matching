using ChronoView.Models;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.NIR.Line2;

/// <summary>
/// Service for loading NIR2 chunks based on existing general camera file time ranges.
/// Calculates time range from camera files and loads only relevant chunks.
/// </summary>
public class Nir2ChunkLoader : INir2ChunkLoader
{
    private readonly INir2ChunkFileStorage _chunkStorage;
    private readonly ILogger? _logger;

    public Nir2ChunkLoader(INir2ChunkFileStorage chunkStorage, ILogger? logger = null)
    {
        _chunkStorage = chunkStorage ?? throw new ArgumentNullException(nameof(chunkStorage));
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<List<Nir2Chunk>> LoadChunksForExistingFilesAsync(
        List<(string FilePath, DataType DataType, DateTime Timestamp)> sortedFiles,
        DataSequenceSettings dataSequenceSettings,
        string nir2CsvDirectory)
    {
        return Task.Run(() =>
        {
            // Filter for general camera files (Cam1-6)
            var cameraFiles = sortedFiles
                .Where(f => IsGeneralCameraDataType(f.DataType))
                .ToList();

            // Requirement: Only load when general camera files exist
            if (cameraFiles.Count == 0)
            {
                _logger?.LogDebug("NIR2 chunk loading skipped: no general camera files found");
                return new List<Nir2Chunk>();
            }

            // Calculate time range from camera files
            var minTimestamp = cameraFiles.Min(f => f.Timestamp);
            var maxTimestamp = cameraFiles.Max(f => f.Timestamp);

            // Get NIR max delay for buffer calculation
            double nirMaxDelay = dataSequenceSettings.GetMaxDelay(DataType.NIR);

            // Add extra buffer (30 seconds) for edge cases
            const double extraBufferSeconds = 30.0;
            double bufferSeconds = nirMaxDelay + extraBufferSeconds;

            var startRange = minTimestamp.AddSeconds(-bufferSeconds);
            var endRange = maxTimestamp.AddSeconds(bufferSeconds);

            _logger?.LogInformation("NIR2 chunk time range: {Start:HH:mm:ss} to {End:HH:mm:ss} (buffer: {Buffer}s, camera files: {Count})",
                startRange, endRange, bufferSeconds, cameraFiles.Count);

            // Load chunks within range
            var chunks = _chunkStorage.LoadChunksInRange(nir2CsvDirectory, startRange, endRange);

            _logger?.LogInformation("Loaded {ChunkCount} NIR2 chunks within camera file time range", chunks.Count);

            return chunks;
        });
    }

    /// <summary>
    /// Determines if the data type represents a general camera (Cam1-6).
    /// </summary>
    private static bool IsGeneralCameraDataType(DataType dataType)
    {
        return dataType == DataType.Cam1 ||
               dataType == DataType.Cam2 ||
               dataType == DataType.Cam3 ||
               dataType == DataType.Cam4 ||
               dataType == DataType.Cam5 ||
               dataType == DataType.Cam6;
    }
}
