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
    public string BasePath { get; set; } = "";

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

    /// <summary>
    /// Data sequence configuration for file arrival order and matching.
    /// </summary>
    public DataSequenceSettings DataSequenceSettings { get; set; } = DataSequencePresets.NormalFirst();

    /// <summary>
    /// External program settings.
    /// </summary>
    public ExternalProgramSettings ExternalProgramSettings { get; set; } = new();
}

/// <summary>
/// Image processing configuration.
/// </summary>
public class ImageSettings
{
    /// <summary>
    /// Thumbnail width in pixels.
    /// </summary>
    public int ThumbnailWidth { get; set; } = 120;

    /// <summary>
    /// Thumbnail height in pixels.
    /// </summary>
    public int ThumbnailHeight { get; set; } = 90;

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
    /// DEPRECATED: Use DataSequenceSettings.GetMaxDelay(DataType.NIR) instead.
    /// </summary>
    [Obsolete("Use DataSequenceSettings.GetMaxDelay(DataType.NIR) instead")]
    public int NirTimeWindowSeconds { get; set; } = 300;

    /// <summary>
    /// Time window for camera file matching in seconds.
    /// DEPRECATED: Use DataSequenceSettings.GetMaxDelay(DataType.Cam1) instead.
    /// </summary>
    [Obsolete("Use DataSequenceSettings.GetMaxDelay(DataType.Cam1) instead")]
    public int CameraTimeWindowSeconds { get; set; } = 2;

    /// <summary>
    /// Time window for normal folder matching in seconds.
    /// DEPRECATED: Use DataSequenceSettings.GetMaxDelay(DataType.Normal) instead.
    /// </summary>
    [Obsolete("Use DataSequenceSettings.GetMaxDelay(DataType.Normal) instead")]
    public int NormalFolderTimeWindowSeconds { get; set; } = 120;

    /// <summary>
    /// Enable abnormal condition detection.
    /// </summary>
    public bool EnableAbnormalDetection { get; set; } = true;

    /// <summary>
    /// Absolute ratio difference threshold for abnormal detection.
    /// If |CurrentRatio - MedianRatio| > Threshold, flag as abnormal.
    /// Default: 0.3 (e.g., detects 1.33 vs 1.0 ratio difference).
    /// </summary>
    public double AbnormalRatioThreshold { get; set; } = 0.1;

    /// <summary>
    /// Window size for abnormal detection history (per context).
    /// Default: 40 samples.
    /// </summary>
    public int AbnormalDetectionWindowSize { get; set; } = 40;

    /// <summary>
    /// Support multiple monitoring lines.
    /// </summary>
    public bool SupportMultipleLines { get; set; } = true;

    /// <summary>
    /// Line mode: "integrated" or "separated".
    /// </summary>
    public string LineMode { get; set; } = "integrated";

    // ============================================================
    // Matching Algorithm Options (DEPRECATED - Use DataSequenceSettings)
    // ============================================================
    // All time-based matching is now controlled by DataSequenceSettings

    /// <summary>
    /// Enable NIR graph visualization.
    /// </summary>
    public bool EnableNirGraph { get; set; } = true;

    /// <summary>
    /// Maximum number of NIR-containing groups to move.
    /// DEPRECATED: Use Line1Settings.MoveNir or Line2Settings.MoveNir instead.
    /// null = move all, 0 = exclude NIR groups, N = move max N NIR groups.
    /// </summary>
    [Obsolete("Use Line1Settings.MoveNir or Line2Settings.MoveNir instead")]
    public int? MoveNir { get; set; } = null;

    /// <summary>
    /// Maximum number of total groups to move.
    /// DEPRECATED: Use Line1Settings.MoveAllData or Line2Settings.MoveAllData instead.
    /// null = move all, 0 = skip move operation, N = move max N groups.
    /// </summary>
    [Obsolete("Use Line1Settings.MoveAllData or Line2Settings.MoveAllData instead")]
    public int? MoveAllData { get; set; } = null;

    /// <summary>
    /// Sample name/subject for file operations.
    /// DEPRECATED: Use Line1Settings.SampleName or Line2Settings.SampleName instead.
    /// </summary>
    [Obsolete("Use Line1Settings.SampleName or Line2Settings.SampleName instead")]
    public string? SampleName { get; set; } = null;

    /// <summary>
    /// Line 1 specific sample move settings.
    /// </summary>
    public LineMoveSettings Line1Settings { get; set; } = new();

    /// <summary>
    /// Line 2 specific sample move settings.
    /// </summary>
    public LineMoveSettings Line2Settings { get; set; } = new();

    /// <summary>
    /// Use camera subfolder for Normal1 path.
    /// </summary>
    public bool UseCameraSubfolderNormal { get; set; } = false;

    /// <summary>
    /// Use camera subfolder for Normal2 path.
    /// </summary>
    public bool UseCameraSubfolderNormal2 { get; set; } = false;

    /// <summary>
    /// Use folder suffix (_0/_1) to determine line number.
    /// When true: Folders must end with _0 (Line 1) or _1 (Line 2).
    /// When false: Line is determined by parent path (Normal1Path vs Normal2Path).
    /// </summary>
    public bool UseFolderSuffix { get; set; } = true;

    // ============================================================
    // Line 1 Paths (NIR1, Normal1, Camera 1-3)
    // ============================================================

    /// <summary>
    /// NIR1 path for Line 1 monitoring.
    /// </summary>
    public string Nir1Path { get; set; } = "";

    /// <summary>
    /// Normal1 path for Line 1 monitoring.
    /// When UseFolderSuffix=true: Expects folders ending with _0 suffix.
    /// When UseFolderSuffix=false: All folders in this path are treated as Line 1.
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
    /// When UseFolderSuffix=true: Expects folders ending with _1 suffix.
    /// When UseFolderSuffix=false: All folders in this path are treated as Line 2.
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
    /// Time-to-live for folder timestamp cache entries in seconds.
    /// Default is 300 seconds (5 minutes).
    /// </summary>
    public int FolderTimestampCacheTTL { get; set; } = 300;

    /// <summary>
    /// Maximum number of parallel workers for event processing.
    /// Default is 3 workers. Valid range: 1-8.
    /// </summary>
    public int MaxEventProcessingWorkers { get; set; } = 3;

    /// <summary>
    /// Enable periodic polling to ensure file detection on network drives.
    /// </summary>
    public bool EnablePolling { get; set; } = true;

    /// <summary>
    /// Polling interval in milliseconds.
    /// </summary>
    public int PollingIntervalMs { get; set; } = 200;

    /// <summary>
    /// Enable parallel event processing for improved throughput.
    /// When enabled, multiple file events are processed concurrently.
    /// Default is true.
    /// </summary>
    public bool EnableParallelProcessing { get; set; } = true;

    /// <summary>
    /// Quarantine (trash) folder path for soft delete operations.
    /// If empty, a default path should be used by callers.
    /// </summary>
    public string DeleteQuarantinePath { get; set; } = "";

    /// <summary>
    /// Number of days to retain log files. Logs older than this will be deleted.
    /// Default is 30 days. Set to 0 or less to disable.
    /// </summary>
    public int LogRetentionDays { get; set; } = 30;

    /// <summary>
    /// Use line-specific group ID sequences.
    /// When true: Line 1 uses group_1_001, group_1_002... Line 2 uses group_2_001...
    /// When false: Global sequence (group_001, group_002...) shared across lines.
    /// </summary>
    public bool UseLineSpecificGroupId { get; set; } = true;
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
    /// Show tooltips on hover.
    /// </summary>
    public bool ShowTooltips { get; set; } = true;

    /// <summary>
    /// NIR graph thumbnail width (pixels).
    /// </summary>
    public int NirThumbnailWidth { get; set; } = 120;

    /// <summary>
    /// NIR graph thumbnail height (pixels).
    /// </summary>
    public int NirThumbnailHeight{ get; set; } = 90;

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

    /// <summary>
    /// Font size for labels in the DataGrid.
    /// </summary>
    public double DisplayFontSize { get; set; } = 10.0;
}

/// <summary>
/// External program configuration for Setup window.
/// </summary>
public class ExternalProgramSettings
{
    /// <summary>
    /// Path to General Camera program executable.
    /// </summary>
    public string GeneralCameraProgramPath { get; set; } = "";

    /// <summary>
    /// Path to NIR Program 1 executable.
    /// </summary>
    public string Nir1ProgramPath { get; set; } = "";

    /// <summary>
    /// Path to NIR Program 2 executable.
    /// </summary>
    public string Nir2ProgramPath { get; set; } = "";

    /// <summary>
    /// Path to monitor for new NIR spectrum files (.txt) for filtering.
    /// </summary>
    public string Nir2FilterMonitorPath { get; set; } = "";

    /// <summary>
    /// Destination path for NIR files that pass the filter criteria.
    /// </summary>
    public string Nir2FilterDestinationPath { get; set; } = "";
}

/// <summary>
/// Per-line sample move settings.
/// Contains settings for sample naming and move limits specific to each production line.
/// </summary>
public class LineMoveSettings
{
    /// <summary>
    /// Sample name/subject for this line's file operations.
    /// Used as folder name when moving files.
    /// </summary>
    public string? SampleName { get; set; } = null;

    /// <summary>
    /// Maximum number of NIR-containing groups to move for this line.
    /// null = move all, 0 = exclude NIR groups, N = move max N NIR groups.
    /// </summary>
    public int? MoveNir { get; set; } = null;

    /// <summary>
    /// Maximum number of total groups to move for this line.
    /// null = move all, 0 = skip move operation, N = move max N groups.
    /// </summary>
    public int? MoveAllData { get; set; } = null;
}
