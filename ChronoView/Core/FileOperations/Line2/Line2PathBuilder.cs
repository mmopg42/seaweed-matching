using ChronoView.Models;
using System.IO;

namespace ChronoView.Core.FileOperations.Line2;

/// <summary>
/// Path builder for Line2 file operations.
/// Implements the new folder structure without "with NIR/without NIR" distinction.
/// Folder structure: {subject}/일반카메라/, {subject}/뷰키nir csv파일/, {subject}/복합 카메라/
/// </summary>
public class Line2PathBuilder : IPathBuilder
{
    /// <summary>
    /// Builds the destination path for a normal folder in Line2.
    /// MoveSchema: {baseRoot}/{subject}/일반카메라/folderName
    /// QuarantineSchema: {baseRoot}/Line2/{subject}/일반2/folderName
    /// </summary>
    public string BuildNormalFolderPath(string basePath, string subject, FileGroup group, PathSchema schema, string? folderName)
    {
        var baseRoot = PathSegmentHelper.EnsureDateRoot(basePath);

        if (schema == PathSchema.QuarantineSchema)
        {
            // Quarantine: baseRoot/Line2/subject/일반2/folderName
            return Path.Combine(baseRoot, "Line2", subject, "일반2", folderName ?? "");
        }
        else
        {
            // MoveSchema: baseRoot/subject/일반카메라/folderName
            // "with NIR/without NIR" 구분 제거
            var baseRolePath = Path.Combine(baseRoot, subject, "일반카메라");
            return !string.IsNullOrEmpty(folderName)
                ? Path.Combine(baseRolePath, folderName)
                : baseRolePath;
        }
    }

    /// <summary>
    /// Builds the destination path for NIR/CSV files in Line2.
    /// MoveSchema: {baseRoot}/{subject}/뷰키nir csv파일/
    /// QuarantineSchema: {baseRoot}/Line2/{subject}/nir2/
    /// </summary>
    public string BuildNirFolderPath(string basePath, string subject, FileGroup group, PathSchema schema)
    {
        var baseRoot = PathSegmentHelper.EnsureDateRoot(basePath);

        if (schema == PathSchema.QuarantineSchema)
        {
            // Quarantine: baseRoot/Line2/subject/nir2
            return Path.Combine(baseRoot, "Line2", subject, "nir2");
        }
        else
        {
            // MoveSchema: baseRoot/subject/뷰키nir csv파일
            return Path.Combine(baseRoot, subject, "뷰키nir csv파일");
        }
    }

    /// <summary>
    /// Builds the destination path for camera files in Line2.
    /// MoveSchema: {baseRoot}/{subject}/복합 카메라/{cameraKey}
    /// QuarantineSchema: {baseRoot}/Line2/{subject}/{cameraKey}
    /// </summary>
    public string BuildCameraFolderPath(string basePath, string subject, FileGroup group, PathSchema schema, string cameraKey)
    {
        var baseRoot = PathSegmentHelper.EnsureDateRoot(basePath);

        if (schema == PathSchema.QuarantineSchema)
        {
            // Quarantine: baseRoot/Line2/subject/cam4 (or cam5, cam6)
            return Path.Combine(baseRoot, "Line2", subject, cameraKey);
        }
        else
        {
            // MoveSchema: baseRoot/subject/복합 카메라/cam4  ← 공백 포함!
            // "with NIR/without NIR" 구분 제거
            return Path.Combine(baseRoot, subject, "복합 카메라", cameraKey);
        }
    }
}
