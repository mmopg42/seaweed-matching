using System.IO;
using System.Text.Json.Serialization;

namespace ChronoView.Models;

/// <summary>
/// Represents a group of related files (NIR, camera images, normal folders) matched by timestamp correlation.
/// </summary>
public class FileGroup : IEquatable<FileGroup>
{
    /// <summary>
    /// Unique identifier for this file group.
    /// </summary>
    [JsonPropertyName("group_id")]
    public string GroupId { get; set; } = string.Empty;

    /// <summary>
    /// NIR file key/identifier.
    /// </summary>
    [JsonPropertyName("nir_key")]
    public string NirKey { get; set; } = string.Empty;

    /// <summary>
    /// Path to the normal folder associated with this group.
    /// </summary>
    [JsonPropertyName("normal_folder")]
    public string NormalFolder { get; set; } = string.Empty;

    /// <summary>
    /// Full path to the main image (e.g., stitched_original.png) for display.
    /// </summary>
    [JsonPropertyName("main_image_path")]
    public string MainImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Dictionary of camera files, keyed by camera identifier.
    /// </summary>
    [JsonPropertyName("camera_files")]
    public Dictionary<string, string> CameraFiles { get; set; } = new();

    /// <summary>
    /// Line number for multi-line monitoring scenarios (1 or 2).
    /// Line 1 corresponds to NIR1/Normal1/Cam1-3.
    /// Line 2 corresponds to NIR2/Normal2/Cam4-6.
    /// </summary>
    [JsonPropertyName("line_number")]
    public int LineNumber { get; set; } = 1;

    /// <summary>
    /// Indicates whether this group has an associated NIR file.
    /// </summary>
    [JsonPropertyName("has_nir")]
    public bool HasNir { get; set; }

    /// <summary>
    /// Timestamp extracted from files for matching purposes.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Timestamp when this group was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current status of this file group.
    /// </summary>
    [JsonPropertyName("status")]
    public GroupStatus Status { get; set; } = GroupStatus.Pending;

    // ============================================================
    // Thumbnail Properties (Progressive Loading - Phase 2)
    // ============================================================

    /// <summary>
    /// Thumbnail for main image (stitched_original.png) for UI display.
    /// Not serialized to JSON.
    /// </summary>
    [JsonIgnore]
    public System.Windows.Media.Imaging.BitmapSource? MainImageThumbnail { get; set; }

    /// <summary>
    /// Thumbnail for NIR graph visualization for UI display.
    /// Not serialized to JSON.
    /// </summary>
    [JsonIgnore]
    public System.Windows.Media.Imaging.BitmapSource? NirGraphThumbnail { get; set; }

    /// <summary>
    /// Thumbnails for camera images (cam1-cam6) for UI display.
    /// Key: "cam1", "cam2", etc.
    /// Not serialized to JSON.
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, System.Windows.Media.Imaging.BitmapSource?> CameraThumbnails { get; set; } = new();

    // ============================================================
    // Static Helper Methods
    // ============================================================

    /// <summary>
    /// Extracts line number from a normal folder name based on suffix.
    /// </summary>
    /// <param name="normalFolderName">
    /// The normal folder name (e.g., "20251204_143052_0" or "20251204_143052_1").
    /// Format: YYYYMMDD_HHMMSS_L where L is the line indicator (0 or 1).
    /// </param>
    /// <returns>
    /// 1 for folders ending with "_0" (Line 1),
    /// 2 for folders ending with "_1" (Line 2),
    /// 1 as default if pattern doesn't match.
    /// </returns>
    public static int GetLineNumberFromNormalFolder(string normalFolderName)
    {
        if (string.IsNullOrEmpty(normalFolderName))
            return 1;

        // Extract just the folder name if a full path is provided
        var folderName = Path.GetFileName(normalFolderName);
        
        if (folderName.EndsWith("_0"))
            return 1;
        if (folderName.EndsWith("_1"))
            return 2;
        
        return 1; // Default to Line 1
    }

    // ============================================================
    // Instance Methods
    // ============================================================

    /// <summary>
    /// Path to the NIR file associated with this group.
    /// </summary>
    [JsonPropertyName("nir_file_path")]
    public string NirFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Validates the integrity of this file group.
    /// </summary>
    /// <returns>True if the file group is valid, false otherwise.</returns>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(GroupId))
            return false;

        if (LineNumber < 1 || LineNumber > 2)
            return false;

        if (HasNir && (string.IsNullOrWhiteSpace(NirKey) || string.IsNullOrWhiteSpace(NirFilePath)))
            return false;

        return true;
    }

    /// <summary>
    /// Gets all file paths associated with this group (MainImage, NIR, Cameras).
    /// </summary>
    public IEnumerable<string> GetAllFilePaths()
    {
        if (!string.IsNullOrEmpty(MainImagePath))
            yield return MainImagePath;

        if (HasNir && !string.IsNullOrEmpty(NirFilePath))
            yield return NirFilePath;

        foreach (var path in CameraFiles.Values)
        {
            if (!string.IsNullOrEmpty(path))
                yield return path;
        }
    }

    /// <summary>
    /// Gets a safe filename with prefix to avoid collisions.
    /// </summary>
    public string GetSafeFileName(string originalPath)
    {
        if (string.IsNullOrEmpty(originalPath)) return string.Empty;

        var fileName = Path.GetFileName(originalPath);
        
        // Return original name as default, prefixing will be handled by FileOperationService
        return fileName;
    }

    #region Equality Implementation

    public bool Equals(FileGroup? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return GroupId == other.GroupId &&
               NirKey == other.NirKey &&
               NormalFolder == other.NormalFolder &&
               LineNumber == other.LineNumber &&
               HasNir == other.HasNir &&
               Status == other.Status &&
               CreatedAt.Equals(other.CreatedAt) &&
               CameraFiles.Count == other.CameraFiles.Count &&
               CameraFiles.All(kvp => other.CameraFiles.TryGetValue(kvp.Key, out var value) && value == kvp.Value);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as FileGroup);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(GroupId);
        hash.Add(NirKey);
        hash.Add(NormalFolder);
        hash.Add(LineNumber);
        hash.Add(HasNir);
        hash.Add(Status);
        hash.Add(CreatedAt);
        
        // Hash camera files in a deterministic way
        foreach (var kvp in CameraFiles.OrderBy(x => x.Key))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }
        
        return hash.ToHashCode();
    }

    public static bool operator ==(FileGroup? left, FileGroup? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(FileGroup? left, FileGroup? right)
    {
        return !Equals(left, right);
    }

    #endregion
}

/// <summary>
/// Status of a file group in the processing pipeline.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GroupStatus
{
    /// <summary>
    /// Group is pending processing.
    /// </summary>
    Pending,

    /// <summary>
    /// Group is complete and ready for operations.
    /// </summary>
    Complete,

    /// <summary>
    /// Group is being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Group has been moved/archived.
    /// </summary>
    Moved,

    /// <summary>
    /// Group has an error condition.
    /// </summary>
    Error,

    /// <summary>
    /// Group has abnormal characteristics detected.
    /// </summary>
    Abnormal
}
