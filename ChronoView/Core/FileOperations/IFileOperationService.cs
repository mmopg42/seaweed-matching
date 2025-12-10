using ChronoView.Models;

namespace ChronoView.Core.FileOperations;

/// <summary>
/// Service interface for file operations (move, delete) on file groups.
/// </summary>
public interface IFileOperationService
{
    /// <summary>
    /// Moves all files in a file group to the specified destination path.
    /// </summary>
    /// <param name="group">The file group to move.</param>
    /// <param name="destinationPath">The destination directory path.</param>
    /// <param name="progress">Progress reporter for the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the move operation.</returns>
    Task<OperationResult> MoveFileGroupAsync(
        FileGroup group,
        string destinationPath,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all files in a file group.
    /// </summary>
    /// <param name="group">The file group to delete.</param>
    /// <param name="progress">Progress reporter for the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the delete operation.</returns>
    Task<OperationResult> DeleteFileGroupAsync(
        FileGroup group,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a file operation.
/// </summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public int FilesProcessed { get; set; }
    public int FilesFailed { get; set; }
}

/// <summary>
/// Progress information for file operations.
/// </summary>
public class OperationProgress
{
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public double PercentComplete => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;
}
