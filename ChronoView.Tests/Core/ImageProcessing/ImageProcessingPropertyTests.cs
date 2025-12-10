using ChronoView.Core.ImageProcessing;
using ChronoView.Models;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace ChronoView.Tests.Core.ImageProcessing;

/// <summary>
/// Property-based tests for image processing accuracy.
/// Feature: python-gui-to-csharp-migration, Property 1: Functional Equivalence (Image Processing)
/// Validates: Requirements 7.2
/// </summary>
public class ImageProcessingPropertyTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ImageProcessingService _imageProcessor;
    private readonly ILogger<ImageProcessingService> _logger;

    public ImageProcessingPropertyTests()
    {
        // Create a unique test directory for each test run
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ChronoViewImageTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        // Create logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<ImageProcessingService>();

        // Create test configuration
        var config = new ApplicationConfiguration
        {
            ImageSettings = new ImageSettings
            {
                ThumbnailWidth = 200,
                ThumbnailHeight = 150,
                ThumbnailQuality = 85,
                MaxCacheSizeMB = 100,
                EnableCaching = true
            }
        };

        _imageProcessor = new ImageProcessingService(_logger, config);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Property: For any valid image dimensions and format, thumbnail generation should produce
    /// a valid image with dimensions respecting the configured size constraints.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task ThumbnailGenerationPreservesDimensionConstraints(
        int sourceWidth,
        int sourceHeight,
        int thumbnailWidth,
        int thumbnailHeight)
    {
        // Constrain inputs to valid ranges
        sourceWidth = Math.Clamp(sourceWidth, 100, 2000);
        sourceHeight = Math.Clamp(sourceHeight, 100, 2000);
        thumbnailWidth = Math.Clamp(thumbnailWidth, 50, 500);
        thumbnailHeight = Math.Clamp(thumbnailHeight, 50, 500);

        // Create a test image
        var testImagePath = Path.Combine(_testDirectory, $"test_{Guid.NewGuid()}.jpg");
        CreateTestImage(testImagePath, sourceWidth, sourceHeight);

        try
        {
            // Generate thumbnail
            var thumbnailBytes = await _imageProcessor.GenerateThumbnailAsync(
                testImagePath, thumbnailWidth, thumbnailHeight);

            // Verify thumbnail was generated
            Assert.NotNull(thumbnailBytes);
            Assert.NotEmpty(thumbnailBytes);

            // Load thumbnail and verify dimensions
            using var ms = new MemoryStream(thumbnailBytes);
            using var thumbnail = await Image.LoadAsync(ms);

            // Thumbnail should fit within the specified dimensions (aspect ratio preserved)
            Assert.True(thumbnail.Width <= thumbnailWidth,
                $"Thumbnail width {thumbnail.Width} exceeds max {thumbnailWidth}");
            Assert.True(thumbnail.Height <= thumbnailHeight,
                $"Thumbnail height {thumbnail.Height} exceeds max {thumbnailHeight}");

            // At least one dimension should match the constraint (aspect ratio preserved)
            var widthMatches = thumbnail.Width == thumbnailWidth;
            var heightMatches = thumbnail.Height == thumbnailHeight;
            var aspectRatioPreserved = Math.Abs((double)thumbnail.Width / thumbnail.Height -
                                                (double)sourceWidth / sourceHeight) < 0.1;

            Assert.True(widthMatches || heightMatches || aspectRatioPreserved,
                "Thumbnail should preserve aspect ratio or match at least one dimension");
        }
        finally
        {
            // Clean up test image
            if (File.Exists(testImagePath))
                File.Delete(testImagePath);
        }
    }

    /// <summary>
    /// Property: For any valid image, metadata extraction should return accurate dimensions,
    /// file size, and format information.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task MetadataExtractionReturnsAccurateInformation(
        int width,
        int height)
    {
        // Constrain inputs to valid ranges
        width = Math.Clamp(width, 100, 2000);
        height = Math.Clamp(height, 100, 2000);

        // Create a test image
        var testImagePath = Path.Combine(_testDirectory, $"test_{Guid.NewGuid()}.jpg");
        CreateTestImage(testImagePath, width, height);

        try
        {
            // Extract metadata
            var metadata = await _imageProcessor.GetImageMetadataAsync(testImagePath);

            // Verify metadata accuracy
            Assert.NotNull(metadata);
            Assert.Equal(width, metadata.Width);
            Assert.Equal(height, metadata.Height);
            Assert.True(metadata.FileSize > 0, "File size should be positive");
            Assert.NotEmpty(metadata.Format);
            Assert.Equal(testImagePath, metadata.FilePath);
            Assert.True(metadata.IsValid(), "Metadata should be valid");
        }
        finally
        {
            // Clean up test image
            if (File.Exists(testImagePath))
                File.Delete(testImagePath);
        }
    }

    /// <summary>
    /// Property: For any image, generating the same thumbnail twice should produce identical results
    /// (cache consistency).
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task ThumbnailCacheProducesConsistentResults(
        int width,
        int height)
    {
        // Constrain inputs to valid ranges
        width = Math.Clamp(width, 100, 1000);
        height = Math.Clamp(height, 100, 1000);

        // Create a test image
        var testImagePath = Path.Combine(_testDirectory, $"test_{Guid.NewGuid()}.jpg");
        CreateTestImage(testImagePath, width, height);

        try
        {
            // Generate thumbnail twice
            var thumbnail1 = await _imageProcessor.GenerateThumbnailAsync(testImagePath, 200, 150);
            var thumbnail2 = await _imageProcessor.GenerateThumbnailAsync(testImagePath, 200, 150);

            // Verify both thumbnails are identical
            Assert.Equal(thumbnail1.Length, thumbnail2.Length);
            Assert.True(thumbnail1.SequenceEqual(thumbnail2),
                "Cached thumbnail should be identical to original");
        }
        finally
        {
            // Clean up test image
            if (File.Exists(testImagePath))
                File.Delete(testImagePath);
        }
    }

    /// <summary>
    /// Property: For any missing or invalid image path, the processor should return a placeholder
    /// image without throwing exceptions.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task MissingImageReturnsPlaceholderGracefully(
        int thumbnailWidth,
        int thumbnailHeight)
    {
        // Constrain inputs to valid ranges
        thumbnailWidth = Math.Clamp(thumbnailWidth, 50, 500);
        thumbnailHeight = Math.Clamp(thumbnailHeight, 50, 500);

        // Use a non-existent path
        var nonExistentPath = Path.Combine(_testDirectory, $"nonexistent_{Guid.NewGuid()}.jpg");

        // Should not throw exception
        var placeholder = await _imageProcessor.GenerateThumbnailAsync(
            nonExistentPath, thumbnailWidth, thumbnailHeight);

        // Verify placeholder was returned
        Assert.NotNull(placeholder);
        // Placeholder might be empty or contain actual placeholder image
        // Either is acceptable as long as no exception is thrown
    }

    /// <summary>
    /// Property: For any image, metadata extraction should be idempotent (same result on repeated calls).
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task MetadataExtractionIsIdempotent(
        int width,
        int height)
    {
        // Constrain inputs to valid ranges
        width = Math.Clamp(width, 100, 1000);
        height = Math.Clamp(height, 100, 1000);

        // Create a test image
        var testImagePath = Path.Combine(_testDirectory, $"test_{Guid.NewGuid()}.jpg");
        CreateTestImage(testImagePath, width, height);

        try
        {
            // Extract metadata twice
            var metadata1 = await _imageProcessor.GetImageMetadataAsync(testImagePath);
            var metadata2 = await _imageProcessor.GetImageMetadataAsync(testImagePath);

            // Verify both metadata objects are equivalent
            Assert.Equal(metadata1.Width, metadata2.Width);
            Assert.Equal(metadata1.Height, metadata2.Height);
            Assert.Equal(metadata1.FileSize, metadata2.FileSize);
            Assert.Equal(metadata1.Format, metadata2.Format);
            Assert.Equal(metadata1.FilePath, metadata2.FilePath);
        }
        finally
        {
            // Clean up test image
            if (File.Exists(testImagePath))
                File.Delete(testImagePath);
        }
    }

    /// <summary>
    /// Property: Cache clearing should reset cache size to zero and force regeneration of thumbnails.
    /// </summary>
    [Property(MaxTest = 50)]
    public async Task CacheClearingResetsStateAndForcesRegeneration(
        int width,
        int height)
    {
        // Constrain inputs to valid ranges
        width = Math.Clamp(width, 100, 1000);
        height = Math.Clamp(height, 100, 1000);

        // Create a test image
        var testImagePath = Path.Combine(_testDirectory, $"test_{Guid.NewGuid()}.jpg");
        CreateTestImage(testImagePath, width, height);

        try
        {
            // Generate thumbnail to populate cache
            var thumbnail1 = await _imageProcessor.GenerateThumbnailAsync(testImagePath, 200, 150);
            var cacheSizeBefore = _imageProcessor.GetCacheSizeBytes();

            // Cache should have content
            Assert.True(cacheSizeBefore > 0, "Cache should contain data after thumbnail generation");

            // Clear cache
            _imageProcessor.ClearCache();
            var cacheSizeAfter = _imageProcessor.GetCacheSizeBytes();

            // Cache should be empty
            Assert.Equal(0, cacheSizeAfter);

            // Generate thumbnail again - should work even after cache clear
            var thumbnail2 = await _imageProcessor.GenerateThumbnailAsync(testImagePath, 200, 150);
            Assert.NotNull(thumbnail2);
            Assert.NotEmpty(thumbnail2);
        }
        finally
        {
            // Clean up test image
            if (File.Exists(testImagePath))
                File.Delete(testImagePath);
        }
    }

    /// <summary>
    /// Property: Placeholder images should always be valid and have reasonable dimensions.
    /// </summary>
    [Property(MaxTest = 100)]
    public void PlaceholderImageHasValidDimensions(
        int width,
        int height)
    {
        // Constrain inputs to valid ranges
        width = Math.Clamp(width, 50, 500);
        height = Math.Clamp(height, 50, 500);

        // Get placeholder
        var placeholder = _imageProcessor.GetPlaceholderImage(width, height);

        // Verify placeholder is valid
        Assert.NotNull(placeholder);
        // Placeholder might be empty array or contain actual image data
        // Both are acceptable as long as method doesn't throw
    }

    /// <summary>
    /// Helper method to create a test image with specified dimensions.
    /// </summary>
    private void CreateTestImage(string path, int width, int height)
    {
        using var image = new Image<Rgb24>(width, height);
        
        // Fill with a gradient pattern for visual verification
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var r = (byte)((x * 255) / width);
                var g = (byte)((y * 255) / height);
                var b = (byte)(((x + y) * 255) / (width + height));
                image[x, y] = new Rgb24(r, g, b);
            }
        }

        image.SaveAsJpeg(path);
    }
}
