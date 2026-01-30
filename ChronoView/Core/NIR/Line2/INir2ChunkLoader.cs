using ChronoView.Models;

namespace ChronoView.Core.NIR.Line2;

/// <summary>
/// Service for loading NIR2 chunks based on existing file time ranges.
/// </summary>
public interface INir2ChunkLoader
{
    /// <summary>
    /// Loads NIR2 chunks that match the time range of existing general camera files.
    /// Only loads chunks when general camera files (Cam1-6) exist.
    /// </summary>
    /// <param name="sortedFiles">List of scanned files with timestamps.</param>
    /// <param name="dataSequenceSettings">Data sequence settings for buffer calculation.</param>
    /// <param name="nir2CsvDirectory">NIR2 CSV directory containing chunks.</param>
    /// <returns>List of loaded chunks within the calculated time range.</returns>
    Task<List<Nir2Chunk>> LoadChunksForExistingFilesAsync(
        List<(string FilePath, DataType DataType, DateTime Timestamp)> sortedFiles,
        DataSequenceSettings dataSequenceSettings,
        string nir2CsvDirectory);
}
