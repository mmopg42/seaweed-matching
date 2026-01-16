namespace ChronoView.Core.ImageProcessing;

/// <summary>
/// Interface for image processing operations including thumbnail generation and metadata extraction.
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// Generates a thumbnail image from the specified image file.
    /// </summary>
    /// <param name="imagePath">Path to the source image file.</param>
    /// <param name="width">Desired thumbnail width in pixels.</param>
    /// <param name="height">Desired thumbnail height in pixels.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <param name="throwOnError">If true, throws exception on error instead of returning placeholder.</param>
    Task<byte[]> GenerateThumbnailAsync(string imagePath, int width, int height, CancellationToken cancellationToken = default, bool throwOnError = false);

    /// <summary>
    /// Extracts metadata from the specified image file.
    /// </summary>
    /// <param name="imagePath">Path to the image file.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>ImageMetadata containing dimensions, file size, format, and other properties.</returns>
    Task<Models.ImageMetadata> GetImageMetadataAsync(string imagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a placeholder image for missing or error states.
    /// </summary>
    /// <param name="width">Desired placeholder width in pixels.</param>
    /// <param name="height">Desired placeholder height in pixels.</param>
    /// <returns>Byte array containing the placeholder image data.</returns>
    byte[] GetPlaceholderImage(int width, int height);

    /// <summary>
    /// Clears the image cache to free memory.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Gets the current cache size in bytes.
    /// </summary>
    long GetCacheSizeBytes();

    /// <summary>
    /// Gets image dimensions without loading the full image (header-only read).
    /// </summary>
    /// <param name="imagePath">Path to the image file.</param>
    /// <returns>Tuple of (Width, Height). Returns (0, 0) if file not found or error.</returns>
    (int Width, int Height) GetImageDimensions(string imagePath);

    /// <summary>
    /// Generates a thumbnail image and returns original dimensions in a single pass.
    /// </summary>
    /// <param name="imagePath">Path to the source image file.</param>
    /// <param name="width">Desired thumbnail width in pixels.</param>
    /// <param name="height">Desired thumbnail height in pixels.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <param name="throwOnError">If true, throws exception on error instead of returning placeholder.</param>
    /// <returns>Result containing thumbnail bytes and original image dimensions.</returns>
    Task<ThumbnailWithDimensionsResult> GenerateThumbnailWithDimensionsAsync(
        string imagePath,
        int width,
        int height,
        CancellationToken cancellationToken = default,
        bool throwOnError = false);
}

/// <summary>
/// Result of thumbnail generation including original image dimensions.
/// </summary>
public record ThumbnailWithDimensionsResult(byte[] ThumbnailBytes, int OriginalWidth, int OriginalHeight);
