using ChronoView.Models;

namespace ChronoView.Core.FileOperations;

/// <summary>
/// Interface for building file operation paths with line-specific folder structures.
/// Enables modularization of move logic between Line1 and Line2.
/// </summary>
public interface IPathBuilder
{
    /// <summary>
    /// Builds the destination path for a normal folder.
    /// </summary>
    /// <param name="basePath">Base destination path.</param>
    /// <param name="subject">Subject/sample name for grouping.</param>
    /// <param name="group">File group containing line number and other metadata.</param>
    /// <param name="schema">Path schema (MoveSchema or QuarantineSchema).</param>
    /// <param name="folderName">Optional folder name from the normal folder.</param>
    /// <returns>Full destination path for the normal folder.</returns>
    string BuildNormalFolderPath(string basePath, string subject, FileGroup group, PathSchema schema, string? folderName);

    /// <summary>
    /// Builds the destination path for NIR files/CSV files.
    /// </summary>
    /// <param name="basePath">Base destination path.</param>
    /// <param name="subject">Subject/sample name for grouping.</param>
    /// <param name="group">File group containing line number and other metadata.</param>
    /// <param name="schema">Path schema (MoveSchema or QuarantineSchema).</param>
    /// <returns>Full destination path for NIR files.</returns>
    string BuildNirFolderPath(string basePath, string subject, FileGroup group, PathSchema schema);

    /// <summary>
    /// Builds the destination path for camera files.
    /// </summary>
    /// <param name="basePath">Base destination path.</param>
    /// <param name="subject">Subject/sample name for grouping.</param>
    /// <param name="group">File group containing line number and other metadata.</param>
    /// <param name="schema">Path schema (MoveSchema or QuarantineSchema).</param>
    /// <param name="cameraKey">Camera identifier (e.g., "cam1", "cam4").</param>
    /// <returns>Full destination path for the camera file.</returns>
    string BuildCameraFolderPath(string basePath, string subject, FileGroup group, PathSchema schema, string cameraKey);
}
