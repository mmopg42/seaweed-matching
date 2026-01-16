using System.Text.Json.Serialization;

namespace ChronoView.Models;

/// <summary>
/// Metadata extracted from image files for processing and analysis.
/// </summary>
public class ImageMetadata : IEquatable<ImageMetadata>
{
    /// <summary>
    /// Image width in pixels.
    /// </summary>
    [JsonPropertyName("width")]
    public int Width { get; set; }

    /// <summary>
    /// Image height in pixels.
    /// </summary>
    [JsonPropertyName("height")]
    public int Height { get; set; }

    /// <summary>
    /// File creation timestamp.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    [JsonPropertyName("file_size")]
    public long FileSize { get; set; }

    /// <summary>
    /// Image format (e.g., "JPEG", "PNG", "TIFF", "BMP").
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this image has been flagged as abnormal.
    /// </summary>
    [JsonPropertyName("is_abnormal")]
    public bool IsAbnormal { get; set; }



    /// <summary>
    /// Full file path to the image.
    /// </summary>
    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Optional additional metadata.
    /// </summary>
    [JsonPropertyName("additional_metadata")]
    public Dictionary<string, object>? AdditionalMetadata { get; set; }

    /// <summary>
    /// Validates the integrity of this image metadata.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    public bool IsValid()
    {
        if (Width <= 0 || Height <= 0)
            return false;

        if (FileSize < 0)
            return false;

        if (string.IsNullOrWhiteSpace(Format))
            return false;

        if (string.IsNullOrWhiteSpace(FilePath))
            return false;

        return true;
    }

    /// <summary>
    /// Gets the aspect ratio of the image.
    /// </summary>
    [JsonIgnore]
    public double AspectRatio => Height > 0 ? (double)Width / Height : 0;

    #region Equality Implementation

    public bool Equals(ImageMetadata? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Width == other.Width &&
               Height == other.Height &&
               CreatedAt.Equals(other.CreatedAt) &&
               FileSize == other.FileSize &&
               Format == other.Format &&
               IsAbnormal == other.IsAbnormal &&
               FilePath == other.FilePath;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ImageMetadata);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Width);
        hash.Add(Height);
        hash.Add(CreatedAt);
        hash.Add(FileSize);
        hash.Add(Format);
        hash.Add(IsAbnormal);
        hash.Add(FilePath);
        return hash.ToHashCode();
    }

    public static bool operator ==(ImageMetadata? left, ImageMetadata? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ImageMetadata? left, ImageMetadata? right)
    {
        return !Equals(left, right);
    }

    #endregion
}
