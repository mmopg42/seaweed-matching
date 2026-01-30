using ChronoView.Models;

namespace ChronoView.Core.FileOperations;

/// <summary>
/// Interface for moving CSV files during file group operations.
/// Used primarily for Line2 BukiKye NIR CSV files.
/// </summary>
public interface ICsvMoveManager
{
    /// <summary>
    /// Moves CSV files associated with file groups to the designated CSV folder.
    /// </summary>
    /// <param name="groups">File groups that may have associated CSV files.</param>
    /// <param name="destinationBase">Base destination path for the move operation.</param>
    /// <param name="subject">Subject/sample name for grouping.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>Operation result indicating success or failure with error details.</returns>
    System.Threading.Tasks.Task<OperationResult> MoveCsvFilesAsync(
        System.Collections.Generic.IEnumerable<FileGroup> groups,
        string destinationBase,
        string subject,
        System.Threading.CancellationToken ct = default);
}
