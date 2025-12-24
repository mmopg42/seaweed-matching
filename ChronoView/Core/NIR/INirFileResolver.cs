namespace ChronoView.Core.Nir;

/// <summary>
/// Resolves NIR file paths and keys for different NIR systems.
/// Allows easy replacement for different NIR file formats and naming conventions.
/// </summary>
public interface INirFileResolver
{
    /// <summary>
    /// Get NirKey from a NIR file path.
    /// </summary>
    /// <param name="nirFilePath">Full path to NIR file (.spc, .txt, etc.)</param>
    /// <returns>NirKey (common identifier for the file set)</returns>
    string GetNirKey(string nirFilePath);
    
    /// <summary>
    /// Get the primary NIR file path from a NirKey.
    /// Primary file is what gets stored in FileGroup.NirFilePath.
    /// </summary>
    /// <param name="nirKey">NirKey identifier</param>
    /// <param name="nirDirectory">Directory containing NIR files</param>
    /// <returns>Full path to primary NIR file (.spc typically)</returns>
    string GetPrimaryFilePath(string nirKey, string nirDirectory);
    
    /// <summary>
    /// Get the text file path for graph generation.
    /// </summary>
    /// <param name="nirKey">NirKey identifier</param>
    /// <param name="nirDirectory">Directory containing NIR files</param>
    /// <returns>Full path to .txt file for graph generation, or null if not found</returns>
    string? GetTextFilePath(string nirKey, string nirDirectory);
    
    /// <summary>
    /// Get all file paths in the NIR file set.
    /// </summary>
    /// <param name="nirKey">NirKey identifier</param>
    /// <param name="nirDirectory">Directory containing NIR files</param>
    /// <returns>Enumerable of all file paths in the set</returns>
    IEnumerable<string> GetAllFilePaths(string nirKey, string nirDirectory);
    
    /// <summary>
    /// Get file search pattern for initial scan.
    /// </summary>
    /// <returns>File pattern (e.g., "*.txt")</returns>
    string GetScanPattern();
}
