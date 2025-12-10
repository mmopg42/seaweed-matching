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
    /// Line number for multi-line monitoring scenarios.
    /// </summary>
    [JsonPropertyName("line_number")]
    public int LineNumber { get; set; }

    /// <summary>
    /// Indicates whether this group has an associated NIR file.
    /// </summary>
    [JsonPropertyName("has_nir")]
    public bool HasNir { get; set; }

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

    /// <summary>
    /// Validates the integrity of this file group.
    /// </summary>
    /// <returns>True if the file group is valid, false otherwise.</returns>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(GroupId))
            return false;

        if (LineNumber < 0)
            return false;

        if (HasNir && string.IsNullOrWhiteSpace(NirKey))
            return false;

        return true;
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
