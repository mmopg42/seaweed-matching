using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ChronoView.Core.Analytics;
using ChronoView.Core.Configuration;
using ChronoView.Core.FileMatching;
using ChronoView.Core.Localization;
using ChronoView.Core.NIR.Shared;
using ChronoView.Models;
using Microsoft.Extensions.Logging;
using ChronoView.Helpers;
using ChronoView.UI.ViewModels;

namespace ChronoView.Core.FileWatching
{
    /// <summary>
    /// Orchestrates monitoring workflows including initial scan, file matching, and group creation
    /// </summary>
    public class MonitoringOrchestrator : IMonitoringOrchestrator
    {
        private readonly IFileGroupMatcher _fileGroupMatcher;
        private readonly IFileWatcher _fileWatcher;
        private readonly ILogger<MonitoringOrchestrator> _logger;
        private readonly INirFileResolver _nirFileResolver;
        private readonly ITimestampCache _folderTimestamps;
        private readonly IInitialScanner _initialScanner;
        private readonly IGroupManager _groupManager;
        private readonly IImageCaptureService _imageCache;
        private readonly IEventProcessor _eventProcessor;
        private readonly IConfigurationManager _configManager;
        private readonly IAbnormalDetector _abnormalDetector;
        private Action<LogSeverity, string, string>? _uiLog;
        
        private const int NirPendingTimeoutSeconds = 10;
        private ApplicationConfiguration? _currentConfig;
        private bool _isMonitoring;

        public event EventHandler<FileGroup>? GroupCreated;
        public event EventHandler<string>? GroupRemoved;
        public event EventHandler<FileGroup>? GroupUpdated;
        public event EventHandler<string>? MonitoringError;
        public event EventHandler<FileGroupsCreatedEventArgs>? FileGroupsCreated;
        public event EventHandler<FileGroupsUpdatedEventArgs>? FileGroupsUpdated;

        public MonitoringOrchestrator(
            IFileGroupMatcher fileGroupMatcher,
            IFileWatcher fileWatcher,
            ILogger<MonitoringOrchestrator> logger,
            INirFileResolver nirFileResolver,
            ITimestampCache folderTimestamps,
            IInitialScanner initialScanner,
            IGroupManager groupManager,
            IImageCaptureService imageCache,
            IEventProcessor eventProcessor,
            IConfigurationManager configManager,
            IAbnormalDetector abnormalDetector,
            Action<LogSeverity, string, string>? uiLog = null)
        {
            _fileGroupMatcher = fileGroupMatcher ?? throw new ArgumentNullException(nameof(fileGroupMatcher));
            _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nirFileResolver = nirFileResolver ?? throw new ArgumentNullException(nameof(nirFileResolver));
            _folderTimestamps = folderTimestamps ?? throw new ArgumentNullException(nameof(folderTimestamps));
            _initialScanner = initialScanner ?? throw new ArgumentNullException(nameof(initialScanner));
            _groupManager = groupManager ?? throw new ArgumentNullException(nameof(groupManager));
            _imageCache = imageCache ?? throw new ArgumentNullException(nameof(imageCache));
            _eventProcessor = eventProcessor ?? throw new ArgumentNullException(nameof(eventProcessor));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _abnormalDetector = abnormalDetector ?? throw new ArgumentNullException(nameof(abnormalDetector));
            _uiLog = uiLog;

            // Wire up GroupManager events
            _groupManager.GroupCreated += (s, g) => 
            {
                if (!string.IsNullOrEmpty(g.NormalFolder))
                    _imageCache.PromoteCacheToGroupId(g.NormalFolder, g.GroupId);
                OnGroupCreated(g);
            };
            _groupManager.GroupUpdated += (s, g) => 
            {
                if (!string.IsNullOrEmpty(g.NormalFolder))
                    _imageCache.PromoteCacheToGroupId(g.NormalFolder, g.GroupId);
                OnGroupUpdated(g);
            };
            _groupManager.GroupRemoved += (s, id) => OnGroupRemoved(id);
            // Forward GroupManager logs to UI (preserve severity)
            _groupManager.Log += (sev, msg) => _uiLog?.Invoke(sev, "GroupManager", msg);
        }

        public async Task StartAsync(ApplicationConfiguration config)
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("Monitoring is already active");
                return;
            }

            try
            {
                _logger.LogInformation("Starting monitoring");
                _currentConfig = config ?? throw new ArgumentNullException(nameof(config));

                // Reset state for fresh monitoring session
                _groupManager.ResetState();
                _folderTimestamps.Clear();
                
                // Configure the file group matcher with the new configuration
                _fileGroupMatcher.Configuration = new MatchingConfiguration
                {
                    Nir1Path = config.MatchingSettings.Nir1Path,
                    Normal1Path = config.MatchingSettings.Normal1Path,
                    Nir2Path = config.MatchingSettings.Nir2Path,
                    Normal2Path = config.MatchingSettings.Normal2Path,
                    Camera1Path = config.MatchingSettings.Camera1Path,
                    Camera2Path = config.MatchingSettings.Camera2Path,
                    Camera3Path = config.MatchingSettings.Camera3Path,
                    Camera4Path = config.MatchingSettings.Camera4Path,
                    Camera5Path = config.MatchingSettings.Camera5Path,
                    Camera6Path = config.MatchingSettings.Camera6Path,

                    // Data Sequence Settings for time-based matching
                    DataSequenceSettings = config.DataSequenceSettings,

                    // Camera Subfolder Options
                    UseCameraSubfolderNormal = config.MatchingSettings.UseCameraSubfolderNormal,
                    UseCameraSubfolderNormal2 = config.MatchingSettings.UseCameraSubfolderNormal2,
                    UseFolderSuffix = config.MatchingSettings.UseFolderSuffix
                };

                _logger.LogInformation("Matching Config Loaded: Nir1='{Nir1}', Normal1='{Normal1}', Cam1='{Cam1}'", 
                    config.MatchingSettings.Nir1Path, config.MatchingSettings.Normal1Path, config.MatchingSettings.Camera1Path);

                // Log Data Sequence Order
                if (config.DataSequenceSettings != null && config.DataSequenceSettings.Sequence.Count > 0)
                {
                    var orderedSequence = config.DataSequenceSettings.Sequence
                        .Where(i => i.Enabled)
                        .OrderBy(i => i.Order)
                        .Select(i => $"{i.Type}(Order:{i.Order}, {i.MinDelaySeconds}-{i.MaxDelaySeconds}s)")
                        .ToList();
                    
                    var sequenceStr = string.Join(" → ", orderedSequence);
                    
                    _logger.LogInformation("Data Sequence Order: {Sequence}", sequenceStr);
                    var message = LocalizationManager.GetString("Log_Info_DataSequenceOrder", sequenceStr);
                    _uiLog?.Invoke(LogSeverity.Info, "System", message);
                }

                // Perform initial scan
                var result = await PerformInitialScanAsync();
                
                if (!result.Success)
                {
                    var errorMsg = $"Failed to start monitoring: {string.Join(", ", result.Errors)}";
                    OnMonitoringError(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }

                _isMonitoring = true;
                
                // Collect paths to watch
                var watchPaths = new List<string>();
                var mc = _fileGroupMatcher.Configuration;
                
                if (mc != null)
                {
                    if (!string.IsNullOrEmpty(mc.Nir1Path)) watchPaths.Add(mc.Nir1Path);
                    if (!string.IsNullOrEmpty(mc.Normal1Path)) watchPaths.Add(mc.Normal1Path);
                    if (!string.IsNullOrEmpty(mc.Nir2Path)) watchPaths.Add(mc.Nir2Path);
                    if (!string.IsNullOrEmpty(mc.Normal2Path)) watchPaths.Add(mc.Normal2Path);

                    string[] cams = { mc.Camera1Path, mc.Camera2Path, mc.Camera3Path, mc.Camera4Path, mc.Camera5Path, mc.Camera6Path };
                    foreach (var p in cams) if (!string.IsNullOrEmpty(p)) watchPaths.Add(p);
                }

                if (watchPaths.Count > 0)
                {
                    // Start event processor
                    _eventProcessor.Start(config.WorkflowSettings.MaxEventProcessingWorkers, ProcessSingleEventAsync);
                    
                    _fileWatcher.FileChanged += OnFileChanged;
                    await _fileWatcher.StartWatchingAsync(watchPaths, new FileWatcherOptions 
                    {
                        EnablePolling = config.WorkflowSettings.EnablePolling,
                        DataSequenceSettings = config.DataSequenceSettings
                    });
                    
                    _logger.LogInformation("File watcher started. Watching {Count} paths", watchPaths.Count);
                    var watchMessage = LocalizationManager.GetString("Log_Info_WatchingPaths", watchPaths.Count);
                    _uiLog?.Invoke(LogSeverity.Info, "System", watchMessage);
                }
                else
                {
                    _logger.LogWarning("No valid paths found to watch");
                }



                _logger.LogInformation("Monitoring started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start monitoring");
                OnMonitoringError($"Failed to start monitoring: {ex.Message}");
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (!_isMonitoring)
            {
                _logger.LogWarning("Monitoring is not active");
                return;
            }

            try
            {
                _logger.LogInformation("Stopping monitoring");
                _isMonitoring = false;


                // Stop event processor
                await _eventProcessor.StopAsync();
                
                // Groups are managed by GroupManager, no need to clear here if ResetState was called in StartAsync
                // But if we want to be explicit:
                // _groupManager.Clear();

                // Stop file watcher
                _fileWatcher.FileChanged -= OnFileChanged;
                await _fileWatcher.StopWatchingAsync();


                _logger.LogInformation("Monitoring stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop monitoring");
                OnMonitoringError($"Failed to stop monitoring: {ex.Message}");
                throw;
            }
        }

        private void OnFileChanged(object? sender, FileSystemEventArgs e)
        {
            _eventProcessor.TryEnqueueEvent(e);
        }

        private async Task ProcessSingleEventAsync(FileSystemEventArgs eventArgs, int workerId, CancellationToken ct)
        {
            try
            {
                // Determine file type
                FileType fileType = DetermineFileType(eventArgs.FullPath);
                string processPath = eventArgs.FullPath;

                // ⚡ FAST CAPTURE: Check if this is stitched_original.png
                bool captureSuccess = true;
                if (FileNamingHelper.IsStitchedImage(eventArgs.FullPath))
                {
                    _logger.LogInformation("Fast Capture triggered for {Path}", eventArgs.FullPath);
                    captureSuccess = await _imageCache.HandleStitchedImageCaptureAsync(eventArgs.FullPath, workerId, OnGroupUpdated);
                    
                    // If it's a stitched image, the "real" path we care about for grouping is the parent folder
                    var parentFolder = Path.GetDirectoryName(eventArgs.FullPath);
                    if (!string.IsNullOrEmpty(parentFolder) && FileNamingHelper.IsNormalFolder(Path.GetFileName(parentFolder)))
                    {
                        processPath = parentFolder;
                        fileType = FileType.Normal;
                    }
                }
                
                if (fileType == FileType.Unknown)
                {
                    _logger.LogDebug("Unknown file type: {Path}", eventArgs.FullPath);
                    return;
                }
                
                // Handle different event types
                switch (eventArgs.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                    case WatcherChangeTypes.Changed:
                    case WatcherChangeTypes.Renamed:
                        // Create or update group (thread-safe)
                        FileGroup? group = await CreateOrUpdateGroupAsync(processPath, fileType, captureSuccess);
                        
                        if (group != null)
                        {
                            _eventProcessor.MarkFileAsProcessed(processPath);
                            _logger.LogInformation("Processed group {GroupId} for {Type} at {Path}", group.GroupId, fileType, processPath);
                        }
                        break;

                    case WatcherChangeTypes.Deleted:
                        await RemoveFromGroupAsync(eventArgs.FullPath);
                        break;
                }
            }
            finally
            {
                // No semaphore to release here, as parallelism is managed by IEventProcessor
            }
        }

        public void SetUILog(Action<LogSeverity, string, string>? uiLog)
        {
            _uiLog = uiLog;
        }

        #region Refresh Logic - Deep Reset

        /// <summary>
        /// Collects all configured watch paths from the application configuration.
        /// </summary>
        private IEnumerable<string> GetWatchPaths(ApplicationConfiguration config)
        {
            var paths = new List<string>();
            var s = config.MatchingSettings;
            
            if (s == null) return paths;

            if (!string.IsNullOrEmpty(s.Nir1Path)) paths.Add(s.Nir1Path);
            if (!string.IsNullOrEmpty(s.Normal1Path)) paths.Add(s.Normal1Path);
            if (!string.IsNullOrEmpty(s.Nir2Path)) paths.Add(s.Nir2Path);
            if (!string.IsNullOrEmpty(s.Normal2Path)) paths.Add(s.Normal2Path);
            
            string[] cams = { s.Camera1Path, s.Camera2Path, s.Camera3Path, s.Camera4Path, s.Camera5Path, s.Camera6Path };
            foreach (var p in cams) 
            {
                if (!string.IsNullOrEmpty(p)) paths.Add(p);
            }
            
            return paths.Distinct().Where(p => !string.IsNullOrWhiteSpace(p));
        }

        public event EventHandler? MonitoringStateReset;

        /// <summary>
        /// Core reset and scan logic shared by StartAsync and RefreshAsync.
        /// </summary>
        private async Task<IEnumerable<string>> CoreResetAndScanAsync(CancellationToken ct)
        {
            // 1. Reset all stateful services
            _groupManager.ResetState();
            _fileGroupMatcher.ResetState();
            _folderTimestamps.Clear();
            _imageCache.Clear();
            _abnormalDetector.Reset();
            
            // FIRE EVENT: Signal UI to clear all data
            MonitoringStateReset?.Invoke(this, EventArgs.Empty);
            
            // 2. Perform fresh scan
            var result = await PerformInitialScanAsync(ct);
            
            if (!result.Success)
            {
                throw new InvalidOperationException($"Scan failed: {string.Join(", ", result.Errors)}");
            }
            
            // 3. Return all known files for injection
            return _groupManager.GetAllProcessedFilePaths();
        }

        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("=== DEEP REFRESH START ===");
                var refreshMessage = LocalizationManager.GetString("Log_Info_Refresh_Start");
                _uiLog?.Invoke(LogSeverity.Info, "System", refreshMessage);

                // 1. Stop everything
                await _eventProcessor.StopAsync();
                _fileWatcher.FileChanged -= OnFileChanged;
                await _fileWatcher.StopWatchingAsync();
                _logger.LogInformation("Stopped monitoring services for refresh");

                // 2. Reload Config explicitly
                _currentConfig = _configManager.LoadConfiguration<ApplicationConfiguration>();
                _logger.LogInformation("Configuration reloaded from disk");
                
                // 3. Re-configure Matcher with new settings
                _fileGroupMatcher.Configuration = new MatchingConfiguration
                {
                    Nir1Path = _currentConfig.MatchingSettings.Nir1Path,
                    Normal1Path = _currentConfig.MatchingSettings.Normal1Path,
                    Nir2Path = _currentConfig.MatchingSettings.Nir2Path,
                    Normal2Path = _currentConfig.MatchingSettings.Normal2Path,
                    Camera1Path = _currentConfig.MatchingSettings.Camera1Path,
                    Camera2Path = _currentConfig.MatchingSettings.Camera2Path,
                    Camera3Path = _currentConfig.MatchingSettings.Camera3Path,
                    Camera4Path = _currentConfig.MatchingSettings.Camera4Path,
                    Camera5Path = _currentConfig.MatchingSettings.Camera5Path,
                    Camera6Path = _currentConfig.MatchingSettings.Camera6Path,
                    DataSequenceSettings = _currentConfig.DataSequenceSettings,
                    UseCameraSubfolderNormal = _currentConfig.MatchingSettings.UseCameraSubfolderNormal,
                    UseCameraSubfolderNormal2 = _currentConfig.MatchingSettings.UseCameraSubfolderNormal2,
                    UseFolderSuffix = _currentConfig.MatchingSettings.UseFolderSuffix
                };
                
                // 4. Core Reset & Scan (Shared logic)
                var knownFiles = await CoreResetAndScanAsync(cancellationToken); 
                
                // 5. DO NOT restart watcher - Refresh means "sync and stop"
                // Unlike StartAsync, RefreshAsync is a read-only sync operation
                _isMonitoring = false;
                
                _logger.LogInformation("=== DEEP REFRESH COMPLETE (Monitoring STOPPED) ===");
                var completeMessage = LocalizationManager.GetString("Log_Info_Refresh_Complete");
                _uiLog?.Invoke(LogSeverity.Info, "System", completeMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh");
                OnMonitoringError($"Failed to refresh: {ex.Message}");
                throw;
            }
        }

        #endregion

        public async Task<OrchestrationResult> PerformInitialScanAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new OrchestrationResult { Success = true };

            _logger.LogInformation("=== INITIAL SCAN START ===");
            
            // DEBUG: Check configuration status
            if (_currentConfig == null)
            {
                _logger.LogWarning("DataSequenceSettings: CONFIG IS NULL");
            }
            else if (_currentConfig.DataSequenceSettings == null)
            {
                _logger.LogWarning("DataSequenceSettings: NULL (using legacy batch scan)");
            }
            else
            {
                var seqStr = string.Join(" → ", _currentConfig.DataSequenceSettings.Sequence
                    .OrderBy(x => x.Order)
                    .Select(x => $"{x.Type}[{x.Order}](min={x.MinDelaySeconds}s,max={x.MaxDelaySeconds}s,enabled={x.Enabled})"));
                _logger.LogInformation("DataSequenceSettings: LOADED ({Count} items)", _currentConfig.DataSequenceSettings.Sequence.Count);
                _logger.LogInformation("Sequence order: {Sequence}", seqStr);
            }

            // Use sequential scan with Match 3 if DataSequenceSettings is configured
            if (_currentConfig?.DataSequenceSettings != null)
            {
                _logger.LogInformation("Using sequential scan with Match 3 (DataSequenceSettings detected)");
                return await PerformSequentialInitialScanAsync(cancellationToken);
            }

            // Legacy: Batch matching (no DataSequenceSettings)
            // REFACTOR: Removed legacy batch scan logic as it is dead code in production.
            _logger.LogError("Legacy batch scan is deprecated and has been removed. Please configure DataSequenceSettings.");
            result.Success = false;
            result.Errors.Add("Legacy matching is not supported. Please configure DataSequenceSettings.");


            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }

        #region Sequential Initial Scan with Match 3

        /// <summary>
        /// Perform initial scan using sequential matching with Match 3 logic
        /// Processes files by DataSequenceSettings priority order
        /// </summary>
        private async Task<OrchestrationResult> PerformSequentialInitialScanAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new OrchestrationResult { Success = true };

            _logger.LogInformation("Starting sequential initial scan with timestamp-ordered processing");
            _groupManager.ResetState();
            
            try
            {
                // Step 1: Scan and sort files using extracted InitialScanner
                var sortedFiles = await _initialScanner.ScanAndSortFilesAsync(_currentConfig!, cancellationToken);
                result.FilesScanned = sortedFiles.Count;

                _logger.LogInformation("Processing files in chronological order (earliest to latest)");

                // Step 2: Process files in timestamp order
                foreach (var (filePath, dataType, timestamp) in sortedFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var fileType = DataTypeToFileType(dataType);
                        await CreateOrUpdateGroupAsync(filePath, fileType);
                        
                        if (result.FilesScanned % 10 == 0)
                        {
                            _logger.LogDebug("Processed {Count} files", result.FilesScanned);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process {DataType} file: {Path}", dataType, filePath);
                        result.Errors.Add($"Failed to process {filePath}: {ex.Message}");
                    }
                }

                result.GroupsCreated = _groupManager.GetActiveGroupsCount();
                
                // ⚡ PRE-LOAD NORMAL IMAGES INTO CACHE
                // This eliminates the 50-second delay when UI loads thumbnails
                _logger.LogInformation("Pre-loading Normal images into cache...");
                var cacheLoadStopwatch = Stopwatch.StartNew();
                int cachedCount = 0, failedCount = 0;

                foreach (var group in _groupManager.ActiveGroups)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (!string.IsNullOrEmpty(group.NormalFolder))
                    {
                        var stitchedPath = Path.Combine(group.NormalFolder, "stitched_original.png");
                        if (File.Exists(stitchedPath))
                        {
                            try
                            {
                                var image = await _imageCache.LoadImageIntoMemoryAsync(stitchedPath);
                                if (image != null)
                                {
                                    cachedCount++;
                                }
                                else
                                {
                                    failedCount++;
                                }
                            }
                            catch
                            {
                                failedCount++;
                            }
                        }
                    }
                }

                cacheLoadStopwatch.Stop();
                _logger.LogInformation("✅ Pre-loaded {CachedCount} images in {Ms}ms ({FailedCount} failed)", 
                    cachedCount, cacheLoadStopwatch.ElapsedMilliseconds, failedCount);
                
                _logger.LogInformation("Sequential initial scan complete: {FilesScanned} files, {GroupsCreated} groups",
                    result.FilesScanned, result.GroupsCreated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sequential initial scan failed");
                result.Success = false;
                result.Errors.Add($"Sequential scan failed: {ex.Message}");
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }

        /// <summary>
        /// Convert DataType to FileType
        /// </summary>
        private FileType DataTypeToFileType(DataType dataType)
        {
            return dataType switch
            {
                DataType.NIR => FileType.Nir,
                DataType.Normal => FileType.Normal,
                DataType.Cam1 or DataType.Cam2 or DataType.Cam3 or
                DataType.Cam4 or DataType.Cam5 or DataType.Cam6 => FileType.Camera,
                _ => throw new ArgumentException($"Unknown DataType: {dataType}")
            };
        }

        #endregion

        public void ResetState()
        {
            _eventProcessor.Reset();
        }

        #region Task 1.2: CreateOrUpdateGroupAsync Implementation

        /// <summary>
        /// Create new group or update existing group based on file type and timestamp matching
        /// Now uses FileGroupMatcher for consistency with initial scan
        /// </summary>
        private async Task<FileGroup?> CreateOrUpdateGroupAsync(string filePath, FileType fileType, bool captureSuccess = true)
        {
            if (_currentConfig == null) return null;
            return await _groupManager.CreateOrUpdateGroupAsync(filePath, fileType, _currentConfig, captureSuccess);
        }

        #endregion

        #region Helpers

        public async Task RemoveFromGroupAsync(string filePath)
        {
            await _groupManager.RemoveFileAsync(filePath);
        }


        private FileType DetermineFileType(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            
            // NIR files - ONLY .txt files
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            // NIR Path Match check (with normalization)
            if (_currentConfig?.MatchingSettings != null)
            {
                var settings = _currentConfig.MatchingSettings;
                if (IsPathUnderPrefix(filePath, settings.Nir1Path) || IsPathUnderPrefix(filePath, settings.Nir2Path))
                {
                    if (extension == ".txt" || extension == ".csv") return FileType.Nir;
                }

                // Normal Path Match check
                if (IsPathUnderPrefix(filePath, settings.Normal1Path) || IsPathUnderPrefix(filePath, settings.Normal2Path))
                {
                    // Robust check: No extension usually means folder, OR check Directory.Exists
                    if (!Path.HasExtension(filePath) || Directory.Exists(filePath))
                    {
                        if (FileNamingHelper.IsNormalFolder(fileName)) return FileType.Normal;
                    }
                }
            }

            // Fallback type checks
            if (extension == ".txt") return FileType.Nir;

            if (FileNamingHelper.IsNormalFolder(fileName)) return FileType.Normal;
            
            // Files inside Normal folders
            var parentDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                var parentName = Path.GetFileName(parentDir);
                if (FileNamingHelper.IsNormalFolder(parentName))
                {
                    return FileType.Normal;
                }
            }
            
            // Camera files
            if (extension == ".bmp" || extension == ".jpg" || extension == ".png")
            {
                return FileType.Camera;
            }
            
            return FileType.Unknown;
        }

        private bool IsPathUnderPrefix(string path, string? prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return false;
            var normalizedPath = path.Replace('\\', '/').TrimEnd('/');
            var normalizedPrefix = prefix.Replace('\\', '/').TrimEnd('/');
            return normalizedPath.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public BitmapImage? GetCapturedImage(string groupId)
        {
            return _imageCache.GetCapturedImage(groupId);
        }

        public BitmapImage? GetCapturedImageByFolderPath(string folderPath)
        {
            return _imageCache.GetCapturedImageByFolderPath(folderPath);
        }

        public void PromoteCacheToGroupId(string folderPath, string groupId)
        {
            _imageCache.PromoteCacheToGroupId(folderPath, groupId);
        }

        protected virtual void OnGroupCreated(FileGroup group) => GroupCreated?.Invoke(this, group);
        protected virtual void OnGroupRemoved(string groupId) => GroupRemoved?.Invoke(this, groupId);
        protected virtual void OnGroupUpdated(FileGroup group) => GroupUpdated?.Invoke(this, group);
        protected virtual void OnMonitoringError(string errorMessage) => MonitoringError?.Invoke(this, errorMessage);
        protected virtual void OnFileGroupsCreated(FileGroupsCreatedEventArgs e) => FileGroupsCreated?.Invoke(this, e);
        protected virtual void OnFileGroupsUpdated(FileGroupsUpdatedEventArgs e) => FileGroupsUpdated?.Invoke(this, e);
        #endregion
    }

    public enum FileType
    {
        Unknown,
        Nir,
        Normal,
        Camera
    }
}


