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
    /// Extracts line number from a normal folder name based on suffix or path.
    /// </summary>
    /// <param name="normalFolderName">
    /// The normal folder path or name (e.g., "C251204T143052_0").
    /// </param>
    /// <param name="useSuffix">
    /// If true, check _0/_1 suffix. If false, use path-based determination.
    /// </param>
    /// <param name="normal1Path">Optional: Line 1 Normal folder path for path-based determination.</param>
    /// <param name="normal2Path">Optional: Line 2 Normal folder path for path-based determination.</param>
    /// <returns>
    /// 1 for Line 1, 2 for Line 2, 1 as default.
    /// </returns>
    public static int GetLineNumberFromNormalFolder(
        string normalFolderName, 
        bool useSuffix = true, 
        string? normal1Path = null, 
        string? normal2Path = null)
    {
        return Helpers.NormalFolderHelper.DetermineLineNumber(
            normalFolderName, 
            useSuffix, 
            normal1Path, 
            normal2Path);
    }

    // ============================================================
    // Eviction Support Methods
    // ============================================================

    /// <summary>
    /// Creates a deep copy of this FileGroup with a new GroupId.
    /// Used during eviction to move existing data to a new group.
    /// </summary>
    /// <param name="newGroupId">The new group ID for the cloned group.</param>
    /// <returns>A new FileGroup instance with cloned data.</returns>
    public FileGroup CloneWithNewId(string newGroupId)
    {
        return new FileGroup
        {
            GroupId = newGroupId,
            NirKey = this.NirKey,
            NormalFolder = this.NormalFolder,
            MainImagePath = this.MainImagePath,
            NirFilePath = this.NirFilePath,
            CsvFilePath = this.CsvFilePath,
            LineNumber = this.LineNumber,
            HasNir = this.HasNir,
            Timestamp = this.Timestamp,
            CreatedAt = DateTime.UtcNow, // New creation time for evicted group
            Status = this.Status,
            // Deep copy dictionary to prevent reference issues
            CameraFiles = new Dictionary<string, string>(this.CameraFiles),
            // Thumbnails are NOT cloned - will be regenerated by UI
            CameraThumbnails = new Dictionary<string, System.Windows.Media.Imaging.BitmapSource?>()
        };
    }

    /// <summary>
    /// Resets all data fields to allow reuse of this group for new data.
    /// Used during eviction after cloning the existing data.
    /// </summary>
    public void ResetData()
    {
        this.NirKey = string.Empty;
        this.NormalFolder = string.Empty;
        this.MainImagePath = string.Empty;
        this.NirFilePath = string.Empty;
        this.CsvFilePath = string.Empty;
        this.HasNir = false;
        this.Timestamp = DateTime.MinValue;
        this.Status = GroupStatus.Pending;
        this.CameraFiles.Clear();
        // Clear thumbnails
        this.MainImageThumbnail = null;
        this.NirGraphThumbnail = null;
        this.CameraThumbnails.Clear();
        // NOTE: GroupId and LineNumber are intentionally NOT reset
        // as they define the group's identity/position
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
    /// Associated CSV file path for Line2 NIR2 data.
    /// Used for moving CSV files to the "뷰키nir csv파일" folder.
    /// </summary>
    [JsonPropertyName("csv_file_path")]
    public string CsvFilePath { get; set; } = string.Empty;

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
/// Gets all file paths associated with this group (NIR, Cameras).
/// NOTE: MainImagePath is NOT included here because it's inside NormalFolder,
/// which is handled separately by FileOperationService.
/// </summary>
public IEnumerable<string> GetAllFilePaths()
{
    // DON'T return MainImagePath - it's inside NormalFolder which is moved as a whole directory
    // MainImagePath is typically: "C:\...\Normal\C251216T200720_0\stitched_original.png"
    // NormalFolder is:            "C:\...\Normal\C251216T200720_0"
    // If we returned MainImagePath, the file would be moved twice (once with folder, once individually)

    if (HasNir && !string.IsNullOrEmpty(NirFilePath))
    {
        // Primary NIR file (.spc typically) - VERIFY IT EXISTS
        if (File.Exists(NirFilePath))
        {
            yield return NirFilePath;
        }
        
        // .txt file in the NIR file set (NirKey + A.txt pattern)
        var nirDirectory = Path.GetDirectoryName(NirFilePath);
        if (!string.IsNullOrEmpty(nirDirectory))
        {
            var nirKey = Path.GetFileNameWithoutExtension(NirFilePath);
            var txtPathA = Path.Combine(nirDirectory, nirKey + "A.txt");
            
            if (File.Exists(txtPathA))
            {
                yield return txtPathA;
            }
            else
            {
                // Fallback: try plain .txt
                var txtPath = Path.Combine(nirDirectory, nirKey + ".txt");
                if (File.Exists(txtPath))
                {
                    yield return txtPath;
                }
            }
        }
    }

    // Camera files - VERIFY THEY EXIST
    foreach (var path in CameraFiles.Values)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
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
