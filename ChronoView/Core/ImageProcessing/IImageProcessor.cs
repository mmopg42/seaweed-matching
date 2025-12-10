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
    /// <returns>Byte array containing the thumbnail image data in JPEG format.</returns>
    Task<byte[]> GenerateThumbnailAsync(string imagePath, int width, int height, CancellationToken cancellationToken = default);

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
}
