using ChronoView.Models;
using System.IO;

namespace ChronoView.Core.FileOperations.Line1;

/// <summary>
/// Path builder for Line1 file operations.
/// Maintains the existing folder structure with "with NIR/without NIR" distinction.
/// Existing logic extracted from FileGroupOperator.BuildPath().
/// </summary>
public class Line1PathBuilder : IPathBuilder
{
    /// <summary>
    /// Builds the destination path for a normal folder in Line1.
    /// MoveSchema with NIR: {baseRoot}/{subject}/with NIR/일반/folderName
    /// MoveSchema without NIR: {baseRoot}/{subject}/without NIR/일반 카메라/folderName
    /// QuarantineSchema: {baseRoot}/Line1/{subject}/일반1/folderName
    /// </summary>
    public string BuildNormalFolderPath(string basePath, string subject, FileGroup group, PathSchema schema, string? folderName)
    {
        var baseRoot = PathSegmentHelper.EnsureDateRoot(basePath);

        if (schema == PathSchema.QuarantineSchema)
        {
            // Quarantine: baseRoot/Line1/subject/일반1/folderName
            var basePathQuarantine = Path.Combine(baseRoot, "Line1", subject, "일반1");
            return !string.IsNullOrEmpty(folderName)
                ? Path.Combine(basePathQuarantine, folderName)
                : basePathQuarantine;
        }
        else
        {
            // MoveSchema: baseRoot/subject/with NIR or without NIR/...
            var nir = group.HasNir ? "with NIR" : "without NIR";
            var baseRolePath = Path.Combine(baseRoot, subject, nir, group.HasNir ? "일반" : "일반 카메라");
            return !string.IsNullOrEmpty(folderName)
                ? Path.Combine(baseRolePath, folderName)
                : baseRolePath;
        }
    }

    /// <summary>
    /// Builds the destination path for NIR files in Line1.
    /// MoveSchema: {baseRoot}/{subject}/with NIR/nir (or without NIR/nir)
    /// QuarantineSchema: {baseRoot}/Line1/{subject}/nir1/
    /// </summary>
    public string BuildNirFolderPath(string basePath, string subject, FileGroup group, PathSchema schema)
    {
        var baseRoot = PathSegmentHelper.EnsureDateRoot(basePath);

        if (schema == PathSchema.QuarantineSchema)
        {
            // Quarantine: baseRoot/Line1/subject/nir1
            return Path.Combine(baseRoot, "Line1", subject, "nir1");
        }
        else
        {
            // MoveSchema: baseRoot/subject/with NIR/nir or without NIR/nir
            var nir = group.HasNir ? "with NIR" : "without NIR";
            return Path.Combine(baseRoot, subject, nir, "nir");
        }
    }

    /// <summary>
    /// Builds the destination path for camera files in Line1.
    /// MoveSchema: {baseRoot}/{subject}/with NIR/복합 카메라/{cameraKey}
    /// QuarantineSchema: {baseRoot}/Line1/{subject}/{cameraKey}
    /// </summary>
    public string BuildCameraFolderPath(string basePath, string subject, FileGroup group, PathSchema schema, string cameraKey)
    {
        var baseRoot = PathSegmentHelper.EnsureDateRoot(basePath);

        if (schema == PathSchema.QuarantineSchema)
        {
            // Quarantine: baseRoot/Line1/subject/cam1 (or cam2, cam3)
            return Path.Combine(baseRoot, "Line1", subject, cameraKey);
        }
        else
        {
            // MoveSchema: baseRoot/subject/with NIR or without NIR/복합 카메라/cameraKey
            var nir = group.HasNir ? "with NIR" : "without NIR";
            return Path.Combine(baseRoot, subject, nir, "복합 카메라", cameraKey);
        }
    }
}
