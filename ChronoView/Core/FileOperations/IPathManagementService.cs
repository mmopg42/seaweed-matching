using ChronoView.Models;

namespace ChronoView.Core.FileOperations;

/// <summary>
/// Service interface for path management operations.
/// </summary>
public interface IPathManagementService
{
    /// <summary>
    /// Generates folder paths from a date string using configured patterns.
    /// </summary>
    /// <param name="dateString">Date string in YYYYMMDD format.</param>
    /// <param name="config">Application configuration.</param>
    /// <returns>Dictionary mapping folder types to generated paths.</returns>
    Dictionary<string, string> GeneratePathsFromDate(string dateString, ApplicationConfiguration config);

    /// <summary>
    /// Validates that the specified paths are valid directory paths.
    /// </summary>
    /// <param name="paths">Dictionary of paths to validate.</param>
    /// <returns>True if all paths are valid, false otherwise.</returns>
    bool ValidatePaths(Dictionary<string, string> paths);

    /// <summary>
    /// Creates sample folder structure in configured locations.
    /// </summary>
    /// <param name="sampleName">Name of the sample folder to create.</param>
    /// <param name="config">Application configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if folders were created successfully, false otherwise.</returns>
    Task<bool> CreateSampleFoldersAsync(
        string sampleName,
        ApplicationConfiguration config,
        CancellationToken cancellationToken = default);
}
