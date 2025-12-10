namespace ChronoView.Models;

/// <summary>
/// Main application configuration containing all settings.
/// </summary>
public class ApplicationConfiguration
{
    /// <summary>
    /// Folder paths for monitoring and file operations.
    /// </summary>
    public Dictionary<string, string> FolderPaths { get; set; } = new();

    /// <summary>
    /// Image processing settings.
    /// </summary>
    public ImageSettings ImageSettings { get; set; } = new();

    /// <summary>
    /// File matching settings.
    /// </summary>
    public MatchingSettings MatchingSettings { get; set; } = new();

    /// <summary>
    /// Workflow settings.
    /// </summary>
    public WorkflowSettings WorkflowSettings { get; set; } = new();

    /// <summary>
    /// Window state settings.
    /// </summary>
    public WindowSettings WindowSettings { get; set; } = new();

    /// <summary>
    /// UI display settings.
    /// </summary>
    public UISettings UISettings { get; set; } = new();
}

/// <summary>
/// Image processing configuration.
/// </summary>
public class ImageSettings
{
    /// <summary>
    /// Thumbnail width in pixels.
    /// </summary>
    public int ThumbnailWidth { get; set; } = 200;

    /// <summary>
    /// Thumbnail height in pixels.
    /// </summary>
    public int ThumbnailHeight { get; set; } = 150;

    /// <summary>
    /// JPEG quality for thumbnails (1-100).
    /// </summary>
    public int ThumbnailQuality { get; set; } = 85;

    /// <summary>
    /// Maximum cache size in MB.
    /// </summary>
    public int MaxCacheSizeMB { get; set; } = 500;

    /// <summary>
    /// Enable image caching.
    /// </summary>
    public bool EnableCaching { get; set; } = true;
}

/// <summary>
/// File matching configuration.
/// </summary>
public class MatchingSettings
{
    /// <summary>
    /// Time window for NIR file matching in seconds.
    /// </summary>
    public int NirTimeWindowSeconds { get; set; } = 300;

    /// <summary>
    /// Time window for camera file matching in seconds.
    /// </summary>
    public int CameraTimeWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Time window for normal folder matching in seconds.
    /// </summary>
    public int NormalFolderTimeWindowSeconds { get; set; } = 120;

    /// <summary>
    /// Enable abnormal condition detection.
    /// </summary>
    public bool EnableAbnormalDetection { get; set; } = true;

    /// <summary>
    /// Z-score threshold for abnormal detection.
    /// </summary>
    public double ZScoreThreshold { get; set; } = 2.0;

    /// <summary>
    /// Support multiple monitoring lines.
    /// </summary>
    public bool SupportMultipleLines { get; set; } = true;

    /// <summary>
    /// Line mode: "integrated" or "separated".
    /// </summary>
    public string LineMode { get; set; } = "integrated";

    /// <summary>
    /// NIR path for monitoring.
    /// </summary>
    public string NirPath { get; set; } = "";

    /// <summary>
    /// Normal path for monitoring.
    /// </summary>
    public string NormalPath { get; set; } = "";

    /// <summary>
    /// Camera 1 path.
    /// </summary>
    public string Camera1Path { get; set; } = "";

    /// <summary>
    /// Camera 2 path.
    /// </summary>
    public string Camera2Path { get; set; } = "";

    /// <summary>
    /// Camera 3 path.
    /// </summary>
    public string Camera3Path { get; set; } = "";

    /// <summary>
    /// Camera 4 path.
    /// </summary>
    public string Camera4Path { get; set; } = "";

    /// <summary>
    /// Camera 5 path.
    /// </summary>
    public string Camera5Path { get; set; } = "";

    /// <summary>
    /// Camera 6 path.
    /// </summary>
    public string Camera6Path { get; set; } = "";

    /// <summary>
    /// Get camera path by number (1-6).
    /// </summary>
    public string GetCameraPath(int cameraNumber)
    {
        return cameraNumber switch
        {
            1 => Camera1Path,
            2 => Camera2Path,
            3 => Camera3Path,
            4 => Camera4Path,
            5 => Camera5Path,
            6 => Camera6Path,
            _ => ""
        };
    }
}

/// <summary>
/// Workflow configuration.
/// </summary>
public class WorkflowSettings
{
    /// <summary>
    /// Enable automatic file operations.
    /// </summary>
    public bool EnableAutoOperations { get; set; } = false;

    /// <summary>
    /// Enable file operation rollback on failure.
    /// </summary>
    public bool EnableRollback { get; set; } = true;

    /// <summary>
    /// Show progress for file operations.
    /// </summary>
    public bool ShowProgress { get; set; } = true;

    /// <summary>
    /// Enable real-time monitoring.
    /// </summary>
    public bool EnableRealTimeMonitoring { get; set; } = true;

    /// <summary>
    /// File system watcher buffer size.
    /// </summary>
    public int WatcherBufferSize { get; set; } = 65536;

    /// <summary>
    /// Enable network drive polling fallback.
    /// </summary>
    public bool EnableNetworkDrivePolling { get; set; } = true;

    /// <summary>
    /// Polling interval in milliseconds for network drives.
    /// </summary>
    public int PollingIntervalMs { get; set; } = 5000;
}

/// <summary>
/// Window state configuration.
/// </summary>
public class WindowSettings
{
    /// <summary>
    /// Window width.
    /// </summary>
    public double Width { get; set; } = 1200;

    /// <summary>
    /// Window height.
    /// </summary>
    public double Height { get; set; } = 800;

    /// <summary>
    /// Window left position.
    /// </summary>
    public double Left { get; set; } = 100;

    /// <summary>
    /// Window top position.
    /// </summary>
    public double Top { get; set; } = 100;

    /// <summary>
    /// Window state: "Normal", "Maximized", "Minimized".
    /// </summary>
    public string WindowState { get; set; } = "Normal";

    /// <summary>
    /// Remember window position on restart.
    /// </summary>
    public bool RememberPosition { get; set; } = true;
}

/// <summary>
/// UI display settings.
/// </summary>
public class UISettings
{
    /// <summary>
    /// Image display width in DataGrid (pixels).
    /// </summary>
    public int DisplayImageWidth { get; set; } = 120;

    /// <summary>
    /// Image display height in DataGrid (pixels).
    /// </summary>
    public int DisplayImageHeight { get; set; } = 90;

    /// <summary>
    /// DataGrid row height (pixels).
    /// </summary>
    public int DataGridRowHeight { get; set; } = 100;

    /// <summary>
    /// Enable legacy UI mode.
    /// </summary>
    public bool LegacyUiMode { get; set; } = false;

    /// <summary>
    /// Show tooltips on hover.
    /// </summary>
    public bool ShowTooltips { get; set; } = true;
}
