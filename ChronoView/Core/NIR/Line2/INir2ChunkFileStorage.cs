namespace ChronoView.Core.NIR.Line2;

/// <summary>
/// Interface for saving and loading NIR2 chunk data to/from disk.
/// </summary>
public interface INir2ChunkFileStorage
{
    /// <summary>
    /// Saves a chunk to disk as a text file.
    /// </summary>
    /// <param name="chunk">The chunk to save. Must have aggregated values.</param>
    /// <param name="baseDirectory">The base directory (NIR2 CSV directory).</param>
    /// <returns>The full path to the saved file, or null if save failed.</returns>
    string? SaveChunk(Nir2Chunk chunk, string baseDirectory);

    /// <summary>
    /// Loads a chunk from a text file.
    /// </summary>
    /// <param name="filePath">The full path to the chunk file.</param>
    /// <returns>The loaded chunk, or null if loading failed.</returns>
    Nir2Chunk? LoadChunk(string filePath);

    /// <summary>
    /// Loads all chunk files from the chunks directory.
    /// </summary>
    /// <param name="baseDirectory">The base directory (NIR2 CSV directory).</param>
    /// <returns>List of all loaded chunks, ordered by timestamp (newest first).</returns>
    List<Nir2Chunk> LoadAllChunks(string baseDirectory);

    /// <summary>
    /// Gets the chunks subdirectory path.
    /// </summary>
    /// <param name="baseDirectory">The base directory (NIR2 CSV directory).</param>
    /// <returns>The full path to the chunks directory.</returns>
    string GetChunksDirectory(string baseDirectory);

    /// <summary>
    /// Loads chunks within a specific time range.
    /// </summary>
    /// <param name="baseDirectory">The base directory (NIR2 CSV directory).</param>
    /// <param name="startInclusive">Start of time range (inclusive).</param>
    /// <param name="endInclusive">End of time range (inclusive).</param>
    /// <returns>List of chunks within the time range, ordered by timestamp.</returns>
    List<Nir2Chunk> LoadChunksInRange(string baseDirectory, DateTime startInclusive, DateTime endInclusive);
}
