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
    /// <param name="subject">Subject/sample name for structured path. Null uses default.</param>
    /// <param name="progress">Progress reporter for the operation.</param>
    /// <param name="onConflict">Callback for resolving name conflicts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the move operation.</returns>
    Task<OperationResult> MoveFileGroupAsync(
        FileGroup group,
        string destinationPath,
        string? subject = null,
        IProgress<OperationProgress>? progress = null,
        Func<string, ConflictResolution>? onConflict = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all files in a file group.
    /// </summary>
    /// <param name="group">The file group to delete.</param>
    /// <param name="quarantinePath">Destination quarantine (trash) path for soft delete.</param>
    /// <param name="subject">Subject/sample name for structured path. Null uses default.</param>
    /// <param name="progress">Progress reporter for the operation.</param>
    /// <param name="onConflict">Callback for resolving name conflicts in quarantine.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the delete operation.</returns>
    Task<OperationResult> DeleteFileGroupAsync(
        FileGroup group,
        string quarantinePath,
        string? subject = null,
        IProgress<OperationProgress>? progress = null,
        Func<string, ConflictResolution>? onConflict = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes specific components of a file group (Partial Delete) using structured paths.
    /// </summary>
    /// <param name="group">The file group.</param>
    /// <param name="componentsToDelete">List of component keys ("Normal", "Nir", "Cam1"..."Cam6") to delete.</param>
    /// <param name="quarantinePath">Quarantine root path.</param>
    /// <param name="subject">Subject/sample name.</param>
    Task<OperationResult> DeleteComponentsAsync(
        FileGroup group,
        List<string> componentsToDelete,
        string quarantinePath,
        string? subject = null,
        IProgress<OperationProgress>? progress = null,
        Func<string, ConflictResolution>? onConflict = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolution strategy for file conflicts.
/// </summary>
public enum ConflictResolution
{
    /// <summary>Overwrite the existing file.</summary>
    Overwrite,
    /// <summary>Skip the file (counts as processed).</summary>
    Skip,
    /// <summary>Abort the entire operation.</summary>
    Abort
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
    
    /// <summary>
    /// List of files that failed to process (due to errors, not skips).
    /// </summary>
    public List<string> FailedFiles { get; } = new();
}

/// <summary>
/// Progress information for file operations.
/// </summary>
public class OperationProgress
{
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double PercentComplete => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;
}
