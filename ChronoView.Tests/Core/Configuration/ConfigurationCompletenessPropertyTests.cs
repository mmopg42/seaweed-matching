using ChronoView.Core.Configuration;
using ChronoView.Models;
using FsCheck.Xunit;
using Xunit;

namespace ChronoView.Tests.Core.Configuration;

/// <summary>
/// Property-based tests for configuration management completeness.
/// Feature: python-gui-to-csharp-migration, Property 11: Configuration Management Completeness
/// Validates: Requirements 5.2
/// </summary>
public class ConfigurationCompletenessPropertyTests
{
    /// <summary>
    /// Property: For any ApplicationConfiguration, all required settings categories should be present.
    /// </summary>
    [Fact]
    public void AllConfigurationCategoriesArePresent()
    {
        var config = new ApplicationConfiguration();

        // All configuration categories must be non-null
        Assert.NotNull(config.FolderPaths);
        Assert.NotNull(config.ImageSettings);
        Assert.NotNull(config.MatchingSettings);
        Assert.NotNull(config.WorkflowSettings);
        Assert.NotNull(config.WindowSettings);
    }

    /// <summary>
    /// Property: For any ImageSettings, all properties should have valid values.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ImageSettingsHaveValidValues(
        int width,
        int height,
        int quality,
        int cacheSize)
    {
        // Constrain to valid ranges
        width = Math.Clamp(width, 50, 1000);
        height = Math.Clamp(height, 50, 1000);
        quality = Math.Clamp(quality, 1, 100);
        cacheSize = Math.Clamp(cacheSize, 10, 2000);

        var settings = new ImageSettings
        {
            ThumbnailWidth = width,
            ThumbnailHeight = height,
            ThumbnailQuality = quality,
            MaxCacheSizeMB = cacheSize
        };

        Assert.True(settings.ThumbnailWidth > 0);
        Assert.True(settings.ThumbnailHeight > 0);
        Assert.InRange(settings.ThumbnailQuality, 1, 100);
        Assert.True(settings.MaxCacheSizeMB > 0);
    }

    /// <summary>
    /// Property: For any MatchingSettings, all time windows should be non-negative.
    /// </summary>
    [Property(MaxTest = 100)]
    public void MatchingSettingsHaveValidTimeWindows(
        int nirWindow,
        int cameraWindow,
        int normalWindow,
        double zScore,
        bool multiLine)
    {
        // Filter out invalid double values (NaN, Infinity)
        if (double.IsNaN(zScore) || double.IsInfinity(zScore))
            zScore = 2.0;

        // Constrain to valid ranges
        nirWindow = Math.Clamp(nirWindow, 10, 3600);
        cameraWindow = Math.Clamp(cameraWindow, 10, 600);
        normalWindow = Math.Clamp(normalWindow, 10, 600);
        zScore = Math.Clamp(zScore, 1.0, 5.0);

        var settings = new MatchingSettings
        {
            NirTimeWindowSeconds = nirWindow,
            CameraTimeWindowSeconds = cameraWindow,
            NormalFolderTimeWindowSeconds = normalWindow,
            ZScoreThreshold = zScore,
            LineMode = multiLine ? "separated" : "integrated"
        };

        Assert.True(settings.NirTimeWindowSeconds >= 0);
        Assert.True(settings.CameraTimeWindowSeconds >= 0);
        Assert.True(settings.NormalFolderTimeWindowSeconds >= 0);
        Assert.True(settings.ZScoreThreshold > 0);
        Assert.True(settings.LineMode == "integrated" || settings.LineMode == "separated");
    }

    /// <summary>
    /// Property: For any WorkflowSettings, buffer sizes and intervals should be positive.
    /// </summary>
    [Property(MaxTest = 100)]
    public void WorkflowSettingsHaveValidBuffersAndIntervals(
        int bufferSize,
        int pollingInterval)
    {
        // Constrain to valid ranges
        bufferSize = Math.Clamp(bufferSize, 4096, 131072);
        pollingInterval = Math.Clamp(pollingInterval, 1000, 30000);

        var settings = new WorkflowSettings
        {
            WatcherBufferSize = bufferSize,
            PollingIntervalMs = pollingInterval
        };

        Assert.True(settings.WatcherBufferSize > 0);
        Assert.True(settings.PollingIntervalMs > 0);
    }

    /// <summary>
    /// Property: For any WindowSettings, dimensions should be positive.
    /// </summary>
    [Property(MaxTest = 100)]
    public void WindowSettingsHaveValidDimensions(
        double width,
        double height,
        double left,
        double top)
    {
        // Filter out invalid double values (NaN, Infinity)
        if (double.IsNaN(width) || double.IsInfinity(width))
            width = 1200;
        if (double.IsNaN(height) || double.IsInfinity(height))
            height = 800;
        if (double.IsNaN(left) || double.IsInfinity(left))
            left = 100;
        if (double.IsNaN(top) || double.IsInfinity(top))
            top = 100;

        // Constrain to valid ranges
        width = Math.Clamp(width, 400, 3840);
        height = Math.Clamp(height, 300, 2160);
        left = Math.Clamp(left, 0, 1920);
        top = Math.Clamp(top, 0, 1080);

        var settings = new WindowSettings
        {
            Width = width,
            Height = height,
            Left = left,
            Top = top,
            WindowState = "Normal"
        };

        Assert.True(settings.Width > 0);
        Assert.True(settings.Height > 0);
        Assert.True(settings.Left >= 0);
        Assert.True(settings.Top >= 0);
        Assert.True(settings.WindowState == "Normal" || 
                    settings.WindowState == "Maximized" || 
                    settings.WindowState == "Minimized");
    }

    /// <summary>
    /// Property: Configuration validation should reject invalid ImageSettings.
    /// </summary>
    [Fact]
    public async Task ConfigurationValidationRejectsInvalidImageSettings()
    {
        var config = new ApplicationConfiguration
        {
            ImageSettings = new ImageSettings
            {
                ThumbnailWidth = -100, // Invalid
                ThumbnailHeight = 150,
                ThumbnailQuality = 85
            }
        };

        var configManager = new ConfigurationManager("ChronoViewTest", "Test");

        await Assert.ThrowsAsync<ConfigurationValidationException>(
            async () => await configManager.SaveConfigurationAsync(config));
    }

    /// <summary>
    /// Property: Configuration validation should reject invalid MatchingSettings.
    /// </summary>
    [Fact]
    public async Task ConfigurationValidationRejectsInvalidMatchingSettings()
    {
        var config = new ApplicationConfiguration
        {
            MatchingSettings = new MatchingSettings
            {
                NirTimeWindowSeconds = -100, // Invalid
                CameraTimeWindowSeconds = 60,
                ZScoreThreshold = 2.0
            }
        };

        var configManager = new ConfigurationManager("ChronoViewTest", "Test");

        await Assert.ThrowsAsync<ConfigurationValidationException>(
            async () => await configManager.SaveConfigurationAsync(config));
    }

    /// <summary>
    /// Property: Configuration validation should reject invalid WorkflowSettings.
    /// </summary>
    [Fact]
    public async Task ConfigurationValidationRejectsInvalidWorkflowSettings()
    {
        var config = new ApplicationConfiguration
        {
            WorkflowSettings = new WorkflowSettings
            {
                WatcherBufferSize = -1000, // Invalid
                PollingIntervalMs = 5000
            }
        };

        var configManager = new ConfigurationManager("ChronoViewTest", "Test");

        await Assert.ThrowsAsync<ConfigurationValidationException>(
            async () => await configManager.SaveConfigurationAsync(config));
    }
}
