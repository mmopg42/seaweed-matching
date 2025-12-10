using ChronoView.Core.Configuration;
using ChronoView.Models;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace ChronoView.Tests.Core.Configuration;

/// <summary>
/// Property-based tests for configuration persistence reliability.
/// Feature: python-gui-to-csharp-migration, Property 2: Configuration Persistence Reliability
/// Validates: Requirements 1.5, 7.4
/// </summary>
public class ConfigurationPersistencePropertyTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ConfigurationManager _configManager;

    public ConfigurationPersistencePropertyTests()
    {
        // Create a unique test directory for each test run
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ChronoViewTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        // Create a test configuration manager
        _configManager = new ConfigurationManager("ChronoViewTest", "Test");
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
    /// Property: For any valid configuration object, serializing and then deserializing 
    /// should produce an equivalent configuration with all settings preserved.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task ConfigurationRoundTripPreservesAllSettings(
        int thumbnailWidth,
        int thumbnailHeight,
        int thumbnailQuality,
        int maxCacheSizeMB,
        bool enableCaching,
        int nirTimeWindow,
        int cameraTimeWindow,
        int normalFolderTimeWindow,
        bool enableAbnormalDetection,
        double zScoreThreshold,
        bool supportMultipleLines,
        bool enableAutoOperations,
        bool enableRollback,
        bool showProgress,
        bool enableRealTimeMonitoring,
        int watcherBufferSize,
        bool enableNetworkDrivePolling,
        int pollingIntervalMs,
        double windowWidth,
        double windowHeight,
        bool rememberPosition)
    {
        // Filter out invalid double values (NaN, Infinity)
        if (double.IsNaN(zScoreThreshold) || double.IsInfinity(zScoreThreshold))
            zScoreThreshold = 2.0;
        if (double.IsNaN(windowWidth) || double.IsInfinity(windowWidth))
            windowWidth = 1200;
        if (double.IsNaN(windowHeight) || double.IsInfinity(windowHeight))
            windowHeight = 800;

        // Constrain inputs to valid ranges
        thumbnailWidth = Math.Clamp(thumbnailWidth, 50, 1000);
        thumbnailHeight = Math.Clamp(thumbnailHeight, 50, 1000);
        thumbnailQuality = Math.Clamp(thumbnailQuality, 1, 100);
        maxCacheSizeMB = Math.Clamp(maxCacheSizeMB, 10, 2000);
        nirTimeWindow = Math.Clamp(nirTimeWindow, 10, 3600);
        cameraTimeWindow = Math.Clamp(cameraTimeWindow, 10, 600);
        normalFolderTimeWindow = Math.Clamp(normalFolderTimeWindow, 10, 600);
        zScoreThreshold = Math.Clamp(zScoreThreshold, 1.0, 5.0);
        watcherBufferSize = Math.Clamp(watcherBufferSize, 4096, 131072);
        pollingIntervalMs = Math.Clamp(pollingIntervalMs, 1000, 30000);
        windowWidth = Math.Clamp(windowWidth, 400, 3840);
        windowHeight = Math.Clamp(windowHeight, 300, 2160);

        var config = new ApplicationConfiguration
        {
            FolderPaths = new Dictionary<string, string>
            {
                ["TestPath1"] = "C:\\Test1",
                ["TestPath2"] = "C:\\Test2"
            },
            ImageSettings = new ImageSettings
            {
                ThumbnailWidth = thumbnailWidth,
                ThumbnailHeight = thumbnailHeight,
                ThumbnailQuality = thumbnailQuality,
                MaxCacheSizeMB = maxCacheSizeMB,
                EnableCaching = enableCaching
            },
            MatchingSettings = new MatchingSettings
            {
                NirTimeWindowSeconds = nirTimeWindow,
                CameraTimeWindowSeconds = cameraTimeWindow,
                NormalFolderTimeWindowSeconds = normalFolderTimeWindow,
                EnableAbnormalDetection = enableAbnormalDetection,
                ZScoreThreshold = zScoreThreshold,
                SupportMultipleLines = supportMultipleLines,
                LineMode = supportMultipleLines ? "separated" : "integrated"
            },
            WorkflowSettings = new WorkflowSettings
            {
                EnableAutoOperations = enableAutoOperations,
                EnableRollback = enableRollback,
                ShowProgress = showProgress,
                EnableRealTimeMonitoring = enableRealTimeMonitoring,
                WatcherBufferSize = watcherBufferSize,
                EnableNetworkDrivePolling = enableNetworkDrivePolling,
                PollingIntervalMs = pollingIntervalMs
            },
            WindowSettings = new WindowSettings
            {
                Width = windowWidth,
                Height = windowHeight,
                Left = 100,
                Top = 100,
                WindowState = "Normal",
                RememberPosition = rememberPosition
            }
        };

        // Save configuration
        await _configManager.SaveConfigurationAsync(config);

        // Load configuration
        var loadedConfig = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();

        // Verify all settings are preserved
        Assert.Equal(config.FolderPaths.Count, loadedConfig.FolderPaths.Count);
        Assert.Equal(config.ImageSettings.ThumbnailWidth, loadedConfig.ImageSettings.ThumbnailWidth);
        Assert.Equal(config.ImageSettings.ThumbnailHeight, loadedConfig.ImageSettings.ThumbnailHeight);
        Assert.Equal(config.ImageSettings.ThumbnailQuality, loadedConfig.ImageSettings.ThumbnailQuality);
        Assert.Equal(config.ImageSettings.MaxCacheSizeMB, loadedConfig.ImageSettings.MaxCacheSizeMB);
        Assert.Equal(config.ImageSettings.EnableCaching, loadedConfig.ImageSettings.EnableCaching);
        Assert.Equal(config.MatchingSettings.NirTimeWindowSeconds, loadedConfig.MatchingSettings.NirTimeWindowSeconds);
        Assert.Equal(config.MatchingSettings.CameraTimeWindowSeconds, loadedConfig.MatchingSettings.CameraTimeWindowSeconds);
        Assert.Equal(config.MatchingSettings.NormalFolderTimeWindowSeconds, loadedConfig.MatchingSettings.NormalFolderTimeWindowSeconds);
        Assert.Equal(config.MatchingSettings.EnableAbnormalDetection, loadedConfig.MatchingSettings.EnableAbnormalDetection);
        Assert.Equal(config.MatchingSettings.ZScoreThreshold, loadedConfig.MatchingSettings.ZScoreThreshold, 0.0001);
        Assert.Equal(config.MatchingSettings.SupportMultipleLines, loadedConfig.MatchingSettings.SupportMultipleLines);
        Assert.Equal(config.MatchingSettings.LineMode, loadedConfig.MatchingSettings.LineMode);
        Assert.Equal(config.WorkflowSettings.EnableAutoOperations, loadedConfig.WorkflowSettings.EnableAutoOperations);
        Assert.Equal(config.WorkflowSettings.EnableRollback, loadedConfig.WorkflowSettings.EnableRollback);
        Assert.Equal(config.WorkflowSettings.ShowProgress, loadedConfig.WorkflowSettings.ShowProgress);
        Assert.Equal(config.WorkflowSettings.EnableRealTimeMonitoring, loadedConfig.WorkflowSettings.EnableRealTimeMonitoring);
        Assert.Equal(config.WorkflowSettings.WatcherBufferSize, loadedConfig.WorkflowSettings.WatcherBufferSize);
        Assert.Equal(config.WorkflowSettings.EnableNetworkDrivePolling, loadedConfig.WorkflowSettings.EnableNetworkDrivePolling);
        Assert.Equal(config.WorkflowSettings.PollingIntervalMs, loadedConfig.WorkflowSettings.PollingIntervalMs);
        Assert.Equal(config.WindowSettings.Width, loadedConfig.WindowSettings.Width, 0.0001);
        Assert.Equal(config.WindowSettings.Height, loadedConfig.WindowSettings.Height, 0.0001);
        Assert.Equal(config.WindowSettings.RememberPosition, loadedConfig.WindowSettings.RememberPosition);
    }

    /// <summary>
    /// Property: For any valid ImageSettings, round-trip should preserve all properties.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task ImageSettingsRoundTripPreservesProperties(
        int width,
        int height,
        int quality,
        int cacheSize,
        bool enableCaching)
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
            MaxCacheSizeMB = cacheSize,
            EnableCaching = enableCaching
        };

        var config = new ApplicationConfiguration { ImageSettings = settings };

        await _configManager.SaveConfigurationAsync(config);
        var loadedConfig = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();

        Assert.Equal(settings.ThumbnailWidth, loadedConfig.ImageSettings.ThumbnailWidth);
        Assert.Equal(settings.ThumbnailHeight, loadedConfig.ImageSettings.ThumbnailHeight);
        Assert.Equal(settings.ThumbnailQuality, loadedConfig.ImageSettings.ThumbnailQuality);
        Assert.Equal(settings.MaxCacheSizeMB, loadedConfig.ImageSettings.MaxCacheSizeMB);
        Assert.Equal(settings.EnableCaching, loadedConfig.ImageSettings.EnableCaching);
    }

    /// <summary>
    /// Property: For any valid MatchingSettings, round-trip should preserve all properties.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task MatchingSettingsRoundTripPreservesProperties(
        int nirWindow,
        int cameraWindow,
        int normalWindow,
        bool enableAbnormal,
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
            EnableAbnormalDetection = enableAbnormal,
            ZScoreThreshold = zScore,
            SupportMultipleLines = multiLine,
            LineMode = multiLine ? "separated" : "integrated"
        };

        var config = new ApplicationConfiguration { MatchingSettings = settings };

        await _configManager.SaveConfigurationAsync(config);
        var loadedConfig = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();

        Assert.Equal(settings.NirTimeWindowSeconds, loadedConfig.MatchingSettings.NirTimeWindowSeconds);
        Assert.Equal(settings.CameraTimeWindowSeconds, loadedConfig.MatchingSettings.CameraTimeWindowSeconds);
        Assert.Equal(settings.NormalFolderTimeWindowSeconds, loadedConfig.MatchingSettings.NormalFolderTimeWindowSeconds);
        Assert.Equal(settings.EnableAbnormalDetection, loadedConfig.MatchingSettings.EnableAbnormalDetection);
        Assert.Equal(settings.ZScoreThreshold, loadedConfig.MatchingSettings.ZScoreThreshold, 0.0001);
        Assert.Equal(settings.SupportMultipleLines, loadedConfig.MatchingSettings.SupportMultipleLines);
        Assert.Equal(settings.LineMode, loadedConfig.MatchingSettings.LineMode);
    }
}
