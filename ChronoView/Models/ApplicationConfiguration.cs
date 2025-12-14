namespace ChronoView.Models;

/// <summary>
/// Main application configuration containing all settings.
/// </summary>
public class ApplicationConfiguration
{
    /// <summary>
    /// Base path for automatic path generation (e.g., "D:/Data").
    /// Used by PathManagementService.GeneratePathsFromDate().
    /// </summary>
    public string BasePath { get; set; } = "D:/Data";

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
    public int CameraTimeWindowSeconds { get; set; } = 2;

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

    // ============================================================
    // Matching Algorithm Options
    // ============================================================

    /// \u003csummary\u003e
    /// Enable time-based camera matching.
    /// \u003c/summary\u003e
    public bool UseCamTimeMatching { get; set; } = true;

    /// \u003csummary\u003e
    /// Minimum time difference for camera matching in seconds.
    /// \u003c/summary\u003e
    public double CamMatchMinDiff { get; set; } = 4.0;

    /// \u003csummary\u003e
    /// Maximum time difference for camera matching in seconds.
    /// \u003c/summary\u003e
    public double CamMatchMaxDiff { get; set; } = 6.0;

    /// \u003csummary\u003e
    /// NIR matching time difference in seconds.
    /// \u003c/summary\u003e
    public double NirMatchTimeDiff { get; set; } = 1.0;

    /// <summary>
    /// Enable NIR graph visualization.
    /// </summary>
    public bool EnableNirGraph { get; set; } = true;

    /// <summary>
    /// Maximum number of NIR-containing groups to move.
    /// null = move all, 0 = exclude NIR groups, N = move max N NIR groups.
    /// </summary>
    public int? MoveNir { get; set; } = null;

    /// <summary>
    /// Maximum number of total groups to move.
    /// null = move all, 0 = skip move operation, N = move max N groups.
    /// </summary>
    public int? MoveAllData { get; set; } = null;

    /// <summary>
    /// Use camera subfolder for Normal1 path.
    /// </summary>
    public bool UseCameraSubfolderNormal { get; set; } = false;

    /// \u003csummary\u003e
    /// Use camera subfolder for Normal2 path.
    /// \u003c/summary\u003e
    public bool UseCameraSubfolderNormal2 { get; set; } = false;

    /// \u003csummary\u003e
    /// Use folder suffix in matching.
    /// \u003c/summary\u003e
    public bool UseFolderSuffix { get; set; } = false;

    // ============================================================
    // Line 1 Paths (NIR1, Normal1, Camera 1-3)
    // ============================================================

    /// <summary>
    /// NIR1 path for Line 1 monitoring.
    /// </summary>
    public string Nir1Path { get; set; } = "";

    /// <summary>
    /// Normal1 path for Line 1 monitoring.
    /// Contains folders with _0 suffix (e.g., 20251204_143052_0/).
    /// </summary>
    public string Normal1Path { get; set; } = "";

    /// <summary>
    /// Camera 1 path (Line 1).
    /// </summary>
    public string Camera1Path { get; set; } = "";

    /// <summary>
    /// Camera 2 path (Line 1).
    /// </summary>
    public string Camera2Path { get; set; } = "";

    /// <summary>
    /// Camera 3 path (Line 1).
    /// </summary>
    public string Camera3Path { get; set; } = "";

    // ============================================================
    // Line 2 Paths (NIR2, Normal2, Camera 4-6)
    // ============================================================

    /// <summary>
    /// NIR2 path for Line 2 monitoring.
    /// </summary>
    public string Nir2Path { get; set; } = "";

    /// <summary>
    /// Normal2 path for Line 2 monitoring.
    /// Contains folders with _1 suffix (e.g., 20251204_143052_1/).
    /// </summary>
    public string Normal2Path { get; set; } = "";

    /// <summary>
    /// Camera 4 path (Line 2).
    /// </summary>
    public string Camera4Path { get; set; } = "";

    /// <summary>
    /// Camera 5 path (Line 2).
    /// </summary>
    public string Camera5Path { get; set; } = "";

    /// <summary>
    /// Camera 6 path (Line 2).
    /// </summary>
    public string Camera6Path { get; set; } = "";

    // ============================================================
    // Common Paths
    // ============================================================

    /// <summary>
    /// Output path for moved/processed files.
    /// </summary>
    public string OutputPath { get; set; } = "";

    // ============================================================
    // Helper Methods
    // ============================================================

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

    /// <summary>
    /// Get line number for a camera (1 or 2).
    /// Cameras 1-3 are Line 1, Cameras 4-6 are Line 2.
    /// </summary>
    /// <param name="cameraNumber">Camera number (1-6).</param>
    /// <returns>1 for cameras 1-3, 2 for cameras 4-6.</returns>
    public int GetCameraLineNumber(int cameraNumber)
    {
        return cameraNumber <= 3 ? 1 : 2;
    }

    /// <summary>
    /// Get NIR path by line number.
    /// </summary>
    /// <param name="lineNumber">Line number (1 or 2).</param>
    /// <returns>NIR path for the specified line.</returns>
    public string GetNirPathByLine(int lineNumber)
    {
        return lineNumber switch
        {
            1 => Nir1Path,
            2 => Nir2Path,
            _ => ""
        };
    }

    /// <summary>
    /// Get Normal path by line number.
    /// </summary>
    /// <param name="lineNumber">Line number (1 or 2).</param>
    /// <returns>Normal path for the specified line.</returns>
    public string GetNormalPathByLine(int lineNumber)
    {
        return lineNumber switch
        {
            1 => Normal1Path,
            2 => Normal2Path,
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

    /// <summary>
    /// Quarantine (trash) folder path for soft delete operations.
    /// If empty, a default path should be used by callers.
    /// </summary>
    public string DeleteQuarantinePath { get; set; } = "";
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

    /// <summary>
    /// NIR graph thumbnail width (pixels).
    /// </summary>
    public int NirThumbnailWidth { get; set; } = 200;

    /// <summary>
    /// NIR graph thumbnail height (pixels).
    /// </summary>
    public int NirThumbnailHeight{ get; set; } = 150;

    /// <summary>
    /// NIR graph display width in DataGrid (pixels).
    /// Separate from DisplayImageWidth to allow independent sizing.
    /// </summary>
    public int NirDisplayWidth { get; set; } = 120;

    /// <summary>
    /// NIR graph display height in DataGrid (pixels).
    /// Separate from DisplayImageHeight to allow independent sizing.
    /// </summary>
    public int NirDisplayHeight { get; set; } = 90;
}
