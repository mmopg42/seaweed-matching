using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChronoView.Core.FileMatching;
using ChronoView.Core.NIR;
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
        private Action<LogSeverity, string, string>? _uiLog;
        private readonly Dictionary<string, DateTime> _processedFiles = new();
        private readonly Dictionary<string, FileGroup> _activeGroups = new();
        // Pending NIR files waiting for matching groups
        private readonly List<(string FilePath, DateTime Timestamp, DateTime ReceivedAt)> _pendingNirFiles = new();
        private const int NirPendingTimeoutSeconds = 10; // Create new group if no match after 10 seconds
        private readonly object _lockObject = new();
        private ApplicationConfiguration? _currentConfig;
        private bool _isMonitoring;
        private int _nextGroupId = 1; // Counter for assigning unique GroupIds

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
            Action<LogSeverity, string, string>? uiLog = null)
        {
            _fileGroupMatcher = fileGroupMatcher ?? throw new ArgumentNullException(nameof(fileGroupMatcher));
            _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nirFileResolver = nirFileResolver ?? throw new ArgumentNullException(nameof(nirFileResolver));
            _uiLog = uiLog;
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
                        .Select(i => $"{i.Type}(Order:{i.Order})")
                        .ToList();
                    
                    var sequenceStr = string.Join(" → ", orderedSequence);
                    
                    _logger.LogInformation("Data Sequence Order: {Sequence}", sequenceStr);
                    _uiLog?.Invoke(LogSeverity.Info, "System", $"데이터 시퀀스 순서: {sequenceStr}");
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
                
                // Collect paths to watch - use same configuration as initial scan
                var watchPaths = new List<string>();
                var matchingConfig = _fileGroupMatcher.Configuration;
                
                _logger.LogError("DEBUG: Collecting paths to watch from FileGroupMatcher.Configuration...");
                _logger.LogError("DEBUG: matchingConfig is null? {IsNull}", matchingConfig == null);
                
                if (matchingConfig == null)
                {
                    _logger.LogError("DEBUG: FileGroupMatcher.Configuration is NULL - cannot start FileWatcher");
                }
                else
                {
                    // Add Line 1 paths
                    if (!string.IsNullOrEmpty(matchingConfig.Nir1Path))
                    {
                        watchPaths.Add(matchingConfig.Nir1Path);
                        _logger.LogError("DEBUG: Added Nir1Path: {Path}", matchingConfig.Nir1Path);
                    }
                    
                    if (!string.IsNullOrEmpty(matchingConfig.Normal1Path))
                    {
                        watchPaths.Add(matchingConfig.Normal1Path);
                        _logger.LogError("DEBUG: Added Normal1Path: {Path}", matchingConfig.Normal1Path);
                    }
                    
                    // Add Line 2 paths
                    if (!string.IsNullOrEmpty(matchingConfig.Nir2Path))
                    {
                        watchPaths.Add(matchingConfig.Nir2Path);
                        _logger.LogError("DEBUG: Added Nir2Path: {Path}", matchingConfig.Nir2Path);
                    }
                    if (!string.IsNullOrEmpty(matchingConfig.Normal2Path))
                    {
                        watchPaths.Add(matchingConfig.Normal2Path);
                        _logger.LogError("DEBUG: Added Normal2Path: {Path}", matchingConfig.Normal2Path);
                    }

                    // Add Camera paths
                    for (int i = 1; i <= 6; i++)
                    {
                        var camPath = i switch
                        {
                            1 => matchingConfig.Camera1Path,
                            2 => matchingConfig.Camera2Path,
                            3 => matchingConfig.Camera3Path,
                            4 => matchingConfig.Camera4Path,
                            5 => matchingConfig.Camera5Path,
                            6 => matchingConfig.Camera6Path,
                            _ => null
                        };
                        
                        if (!string.IsNullOrEmpty(camPath))
                        {
                            watchPaths.Add(camPath);
                            _logger.LogError("DEBUG: Added Camera{CamNum}Path: {Path}", i, camPath);
                        }
                    }
                }
                
                _logger.LogError("DEBUG: Total paths collected for watching: {Count}", watchPaths.Count);

                // Start file watcher
                if (watchPaths.Count > 0)
                {
                    _fileWatcher.FileChanged += OnFileChanged;
                    
                    var watcherOptions = new FileWatcherOptions
                    {
                        EnablePolling = config.WorkflowSettings.EnableNetworkDrivePolling,
                        PollingIntervalMs = config.WorkflowSettings.PollingIntervalMs
                    };

                    _logger.LogInformation("Starting file watcher with Polling={Polling}, Interval={Interval}", 
                        watcherOptions.EnablePolling, watcherOptions.PollingIntervalMs);

                    await _fileWatcher.StartWatchingAsync(watchPaths, watcherOptions);
                    
                    // Diagnostic logging: confirm watcher started and paths being watched
                    _logger.LogInformation("File watcher started. Registered FileChanged event handler. Watching {Count} paths", watchPaths.Count);
                    foreach (var path in watchPaths)
                    {
                        _logger.LogInformation("  Watching: {Path}", path);
                    }
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
                
                // Clear active groups
                lock (_lockObject)
                {
                    _activeGroups.Clear();
                }

                // Stop file watcher
                _fileWatcher.FileChanged -= OnFileChanged;
                await _fileWatcher.StopWatchingAsync();

                _logger.LogInformation("Monitoring stopped successfully");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop monitoring");
                OnMonitoringError($"Failed to stop monitoring: {ex.Message}");
                throw;
            }
        }

        public void SetUILog(Action<LogSeverity, string, string>? uiLog)
        {
            _uiLog = uiLog;
        }

        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Refreshing file groups");
                
                // Clear existing groups
                lock (_lockObject)
                {
                    var groupIds = _activeGroups.Keys.ToList();
                    foreach (var groupId in groupIds)
                    {
                        _activeGroups.Remove(groupId);
                        OnGroupRemoved(groupId);
                    }
                    
                    // Reset group ID counter for real-time additions
                    _nextGroupId = 1;
                }

                // Reset FileGroupMatcher state (group counter and consumed NIR keys)
                _fileGroupMatcher.ResetState();

                // Perform new scan (works whether monitoring is active or not)
                var result = await PerformInitialScanAsync(cancellationToken);
                
                if (!result.Success)
                {
                    OnMonitoringError($"Refresh failed: {string.Join(", ", result.Errors)}");
                }

                _logger.LogInformation("Refresh completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh");
                OnMonitoringError($"Failed to refresh: {ex.Message}");
                throw;
            }
        }

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
            _logger.LogWarning("Using legacy batch scan (no DataSequenceSettings)");

            try
            {
                // Get configuration paths from the file group matcher
                var matchingConfig = _fileGroupMatcher.Configuration;
                if (matchingConfig == null)
                {
                    throw new InvalidOperationException("File group matcher configuration is not set");
                }

                var unmatchedFiles = new UnmatchedFiles
                {
                    NirFiles = new Dictionary<string, Dictionary<string, string>>(),
                    NormalFolders = new Dictionary<string, Dictionary<string, string>>(),
                    CameraFiles = new Dictionary<string, List<TimestampedFile>>()
                };

                // Scan NIR directories for Line 1 (using INirFileResolver)
                if (!string.IsNullOrEmpty(matchingConfig.Nir1Path) && Directory.Exists(matchingConfig.Nir1Path))
                {
                    var scanPattern = _nirFileResolver.GetScanPattern();
                    var nirFiles = Directory.GetFiles(matchingConfig.Nir1Path, scanPattern, SearchOption.AllDirectories);
                    var nirDict = new Dictionary<string, string>();
                    
                    foreach (var file in nirFiles)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var nirKey = _nirFileResolver.GetNirKey(file);
                        var nirDirectory = Path.GetDirectoryName(file) ?? string.Empty;
                        var primaryPath = _nirFileResolver.GetPrimaryFilePath(nirKey, nirDirectory);
                        
                        // Store primary file path (.spc typically), fallback to scanned file
                        nirDict[nirKey] = File.Exists(primaryPath) ? primaryPath : file;
                        result.FilesScanned++;
                    }
                    
                    unmatchedFiles.NirFiles["nir1"] = nirDict;
                    _logger.LogInformation("Scanned {Count} NIR1 file sets", nirDict.Count);
                }

                // Scan NIR directories for Line 2 (using INirFileResolver)
                if (!string.IsNullOrEmpty(matchingConfig.Nir2Path) && Directory.Exists(matchingConfig.Nir2Path))
                {
                    var scanPattern = _nirFileResolver.GetScanPattern();
                    var nirFiles = Directory.GetFiles(matchingConfig.Nir2Path, scanPattern, SearchOption.AllDirectories);
                    var nirDict = new Dictionary<string, string>();
                    
                    foreach (var file in nirFiles)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var nirKey = _nirFileResolver.GetNirKey(file);
                        var nirDirectory = Path.GetDirectoryName(file) ?? string.Empty;
                        var primaryPath = _nirFileResolver.GetPrimaryFilePath(nirKey, nirDirectory);
                        
                        // Store primary file path (.spc typically), fallback to scanned file
                        nirDict[nirKey] = File.Exists(primaryPath) ? primaryPath : file;
                        result.FilesScanned++;
                    }
                    
                    unmatchedFiles.NirFiles["nir2"] = nirDict;
                    _logger.LogInformation("Scanned {Count} NIR2 file sets", nirDict.Count);
                }

                // Scan normal directories for Line 1
                if (!string.IsNullOrEmpty(matchingConfig.Normal1Path) && Directory.Exists(matchingConfig.Normal1Path))
                {
                    var normalFolders = Directory.GetDirectories(matchingConfig.Normal1Path);
                    var normalDict = new Dictionary<string, string>();
                    foreach (var folder in normalFolders)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var key = Path.GetFileName(folder);
                        normalDict[key] = folder;
                        result.FilesScanned++;
                    }
                    unmatchedFiles.NormalFolders["normal1"] = normalDict;
                    _logger.LogInformation("Scanned {Count} Normal1 folders", normalFolders.Length);
                }

                // Scan normal directories for Line 2
                if (!string.IsNullOrEmpty(matchingConfig.Normal2Path) && Directory.Exists(matchingConfig.Normal2Path))
                {
                    var normalFolders = Directory.GetDirectories(matchingConfig.Normal2Path);
                    var normalDict = new Dictionary<string, string>();
                    foreach (var folder in normalFolders)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var key = Path.GetFileName(folder);
                        normalDict[key] = folder;
                        result.FilesScanned++;
                    }
                    unmatchedFiles.NormalFolders["normal2"] = normalDict;
                    _logger.LogInformation("Scanned {Count} Normal2 folders", normalFolders.Length);
                }

                // Scan camera directories
                for (int i = 1; i <= 6; i++)
                {
                    var cameraPath = matchingConfig.GetCameraPath(i);
                    if (!string.IsNullOrEmpty(cameraPath) && Directory.Exists(cameraPath))
                    {
                        var cameraFiles = Directory.GetFiles(cameraPath, "*.*", SearchOption.AllDirectories);
                        var timestampedFiles = new List<TimestampedFile>();

                        foreach (var file in cameraFiles)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            try
                            {
                                var fileInfo = new FileInfo(file);
                    var fileName = Path.GetFileName(file);
                    var timestamp = fileInfo.LastWriteTime;

                            // Try to extract timestamp from filename (YYYYMMDD_HHMMSS)
                            // Example: 20250120_143052_001.jpg -> 2025-01-20 14:30:52
                            var match = System.Text.RegularExpressions.Regex.Match(fileName, @"(\d{8})_(\d{6})");
                            if (match.Success)
                            {
                                if (DateTime.TryParseExact(
                                    $"{match.Groups[1].Value}_{match.Groups[2].Value}", 
                                    "yyyyMMdd_HHmmss", 
                                    null, 
                                    System.Globalization.DateTimeStyles.None, 
                                    out var dt))
                                {
                                    timestamp = dt;

                                    // Only add files that match the naming convention
                                    timestampedFiles.Add(new TimestampedFile
                                    {
                                        FileName = fileName,
                                        AbsolutePath = file,
                                        Timestamp = timestamp
                                    });
                                    result.FilesScanned++;
                                }
                            }
                            else 
                            { 
                                _logger.LogDebug("Skipping file with invalid format: {FileName}", fileName);
                            }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to process camera file: {File}", file);
                                result.Errors.Add($"Failed to process {file}: {ex.Message}");
                            }
                        }

                        if (timestampedFiles.Count > 0)
                        {
                            unmatchedFiles.CameraFiles[$"cam{i}"] = timestampedFiles;
                        }
                        _logger.LogInformation("Scanned {Count} files from Camera {CameraNumber}", cameraFiles.Length, i);
                    }
                }

                // Perform file matching
                var groups = await _fileGroupMatcher.MatchFilesAsync(unmatchedFiles);
                var groupList = groups.ToList();
                result.GroupsCreated = groupList.Count;

                _logger.LogInformation("Initial scan completed: {FilesScanned} files scanned, {GroupsCreated} groups created",
                    result.FilesScanned, result.GroupsCreated);

                // Store groups and raise individual events
                lock (_lockObject)
                {
                    foreach (var group in groupList)
                    {
                        // Check if group already exists to prevent duplicate events
                        if (_activeGroups.ContainsKey(group.GroupId))
                        {
                            _logger.LogDebug("Group {GroupId} already exists in active groups - skipping event", 
                                group.GroupId);
                            continue;
                        }
                        
                        _activeGroups[group.GroupId] = group;
                        OnGroupCreated(group);
                    }
                }

                // Also raise batch event for backward compatibility
                if (groupList.Count > 0)
                {
                    // Update _nextGroupId based on highest existing group ID to prevent collisions
                    // FileGroupMatcher generates IDs starting from 1 for each batch
                    var maxId = groupList
                        .Select(g => 
                        {
                            // Parse "group_XXX" to extract XXX
                            var idPart = g.GroupId.Replace("group_", "");
                            if (int.TryParse(idPart, out int id)) return id;
                            return 0;
                        })
                        .Max();
                    _nextGroupId = maxId + 1;
                    _logger.LogInformation("Updated next GroupId counter to {NextGroupId}", _nextGroupId);

                    OnFileGroupsCreated(new FileGroupsCreatedEventArgs
                    {
                        Groups = groupList,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Initial scan failed");
                result.Success = false;
                result.Errors.Add($"Initial scan failed: {ex.Message}");
                OnMonitoringError($"Initial scan failed: {ex.Message}");
            }

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

            try
            {
                var orderedTypes = _currentConfig?.DataSequenceSettings?.GetOrderedTypes();
                if (orderedTypes == null || orderedTypes.Count == 0)
                {
                    _logger.LogWarning("No ordered types configured in DataSequenceSettings");
                    result.Success = false;
                    result.Errors.Add("DataSequenceSettings has no enabled types");
                    stopwatch.Stop();
                    result.Duration = stopwatch.Elapsed;
                    return result;
                }

                _logger.LogInformation("Scanning {Count} data types", orderedTypes.Count);

                // Step 1: Scan all files and extract timestamps
                var allFilesWithTimestamps = new List<(string FilePath, DataType DataType, DateTime Timestamp)>();

                foreach (var dataType in orderedTypes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var files = await ScanFilesForDataTypeAsync(dataType, cancellationToken);
                    _logger.LogInformation("Found {Count} {DataType} files", files.Count, dataType);

                    foreach (var filePath in files)
                    {
                        try
                        {
                            var fileType = DataTypeToFileType(dataType);
                            var timestamp = ExtractTimestamp(filePath, fileType);

                            if (timestamp.HasValue && timestamp.Value != DateTime.MinValue)
                            {
                                allFilesWithTimestamps.Add((filePath, dataType, timestamp.Value));
                            }
                            else
                            {
                                _logger.LogWarning("Could not extract timestamp from {DataType} file: {Path}", dataType, filePath);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to extract timestamp from {DataType} file: {Path}", dataType, filePath);
                        }
                    }
                }

                _logger.LogInformation("Total files with valid timestamps: {Count}", allFilesWithTimestamps.Count);

                // Step 2: Sort by timestamp (chronological order)
                var sortedFiles = allFilesWithTimestamps.OrderBy(f => f.Timestamp).ToList();
                _logger.LogInformation("Processing files in chronological order (earliest to latest)");

                // Step 3: Process files in timestamp order
                foreach (var (filePath, dataType, timestamp) in sortedFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var fileType = DataTypeToFileType(dataType);
                        await CreateOrUpdateGroupAsync(filePath, fileType);
                        result.FilesScanned++;

                        if (result.FilesScanned % 10 == 0)
                        {
                            _logger.LogDebug("Processed {Count}/{Total} files", result.FilesScanned, sortedFiles.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process {DataType} file: {Path}", dataType, filePath);
                        result.Errors.Add($"Failed to process {filePath}: {ex.Message}");
                    }
                }

                result.GroupsCreated = _activeGroups.Count;
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
        /// Scan filesystem for files of specific DataType
        /// </summary>
        private async Task<List<string>> ScanFilesForDataTypeAsync(DataType dataType, CancellationToken cancellationToken)
        {
            var files = new List<string>();
            var config = _fileGroupMatcher.Configuration;

            if (config == null)
            {
                _logger.LogWarning("MatchingSettings configuration is null");
                return files;
            }

            await Task.Run(() =>
            {
                switch (dataType)
                {
                    case DataType.NIR:
                        // Scan NIR directories using resolver
                        if (!string.IsNullOrEmpty(config.Nir1Path) && Directory.Exists(config.Nir1Path))
                        {
                            var scanPattern = _nirFileResolver.GetScanPattern();
                            var nirFiles = Directory.GetFiles(config.Nir1Path, scanPattern, SearchOption.AllDirectories);
                            files.AddRange(nirFiles);
                        }
                        if (!string.IsNullOrEmpty(config.Nir2Path) && Directory.Exists(config.Nir2Path))
                        {
                            var scanPattern = _nirFileResolver.GetScanPattern();
                            var nirFiles = Directory.GetFiles(config.Nir2Path, scanPattern, SearchOption.AllDirectories);
                            files.AddRange(nirFiles);
                        }
                        break;

                    case DataType.Normal:
                        // Scan Normal directories
                        if (!string.IsNullOrEmpty(config.Normal1Path) && Directory.Exists(config.Normal1Path))
                        {
                            var folders = Directory.GetDirectories(config.Normal1Path);
                            files.AddRange(folders);
                        }
                        if (!string.IsNullOrEmpty(config.Normal2Path) && Directory.Exists(config.Normal2Path))
                        {
                            var folders = Directory.GetDirectories(config.Normal2Path);
                            files.AddRange(folders);
                        }
                        break;

                    case DataType.Cam1:
                        AddCameraFilesForScan(files, config.Camera1Path);
                        break;
                    case DataType.Cam2:
                        AddCameraFilesForScan(files, config.Camera2Path);
                        break;
                    case DataType.Cam3:
                        AddCameraFilesForScan(files, config.Camera3Path);
                        break;
                    case DataType.Cam4:
                        AddCameraFilesForScan(files, config.Camera4Path);
                        break;
                    case DataType.Cam5:
                        AddCameraFilesForScan(files, config.Camera5Path);
                        break;
                    case DataType.Cam6:
                        AddCameraFilesForScan(files, config.Camera6Path);
                        break;
                }
            }, cancellationToken);

            return files;
        }

        /// <summary>
        /// Helper to add camera files with naming convention filtering
        /// </summary>
        private void AddCameraFilesForScan(List<string> files, string? cameraPath)
        {
            if (string.IsNullOrEmpty(cameraPath) || !Directory.Exists(cameraPath))
                return;

            var cameraFiles = Directory.GetFiles(cameraPath, "*.*", SearchOption.AllDirectories);
            foreach (var file in cameraFiles)
            {
                var fileName = Path.GetFileName(file);
                // Only add files matching YYYYMMDD_HHMMSS naming convention
                if (System.Text.RegularExpressions.Regex.IsMatch(fileName, @"\d{8}_\d{6}"))
                {
                    files.Add(file);
                }
            }
        }

        /// <summary>
        /// Create new group for first-priority files during initial scan
        /// </summary>
        private async Task CreateNewGroupForInitialScanAsync(string filePath, DataType dataType, CancellationToken cancellationToken)
        {
            var fileType = DataTypeToFileType(dataType);

            // Use FileGroupMatcher to create group structure
            var unmatchedFiles = CreateUnmatchedFilesForSingleFile(filePath, fileType);
            if (unmatchedFiles == null)
            {
                _logger.LogWarning("Could not create UnmatchedFiles for {DataType} file: {Path}", dataType, filePath);
                return;
            }

            var matchedGroups = await _fileGroupMatcher.MatchFilesAsync(unmatchedFiles);
            var newGroup = matchedGroups.FirstOrDefault();

            if (newGroup == null)
            {
                _logger.LogWarning("FileGroupMatcher returned no groups for {DataType} file: {Path}", dataType, filePath);
                return;
            }

            // Assign unique GroupId
            lock (_lockObject)
            {
                var newGroupId = $"group_{_nextGroupId:D3}";
                _nextGroupId++;
                newGroup.GroupId = newGroupId;
                _activeGroups[newGroup.GroupId] = newGroup;
            }

            OnGroupCreated(newGroup);
            _logger.LogDebug("Created new group {GroupId} for {DataType} file", newGroup.GroupId, dataType);
        }

        /// <summary>
        /// Convert DataType to FileType for legacy methods
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
            lock (_lockObject)
            {
                _logger.LogInformation("Orchestrator state reset");
            }
        }

        #region Task 1.2: CreateOrUpdateGroupAsync Implementation

        /// <summary>
        /// Create new group or update existing group based on file type and timestamp matching
        /// Now uses FileGroupMatcher for consistency with initial scan
        /// </summary>
        private async Task<FileGroup?> CreateOrUpdateGroupAsync(string filePath, FileType fileType)
        {
            try
            {
                // FIX: Normal data is folder-based, but real-time events give us file paths
                // Extract parent folder for Normal files (matching initial scan behavior)
                string processPath = filePath;
                if (fileType == FileType.Normal && !string.IsNullOrEmpty(Path.GetExtension(filePath)))
                {
                    var parentFolder = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(parentFolder))
                    {
                        processPath = parentFolder;
                        _logger.LogInformation("Normal file detected, using parent folder: {Folder}", processPath);
                    }
                }

                _logger.LogInformation("Processing {FileType} file: {Path}", fileType, processPath);

                // Create FileGroup directly from single file (bypassing FileMatchingEngine)
                var newGroup = CreateGroupFromSingleFile(processPath, fileType);
                if (newGroup == null)
                {
                    _logger.LogWarning("Could not create FileGroup for {FileType} file: {Path}", fileType, filePath);
                    return null;
                }

                _logger.LogInformation("Created group from single file: GroupId={GroupId}, Timestamp={Timestamp}",
                    newGroup.GroupId, newGroup.Timestamp);
                
                _logger.LogDebug("Group identifiers: NormalFolder={NormalFolder}, NirKey={NirKey}, HasNir={HasNir}, CamCount={CamCount}",
                    newGroup.NormalFolder ?? "null",
                    newGroup.NirKey ?? "null",
                    newGroup.HasNir,
                    newGroup.CameraFiles.Count);

                // CRITICAL FIX: Lock the entire check-and-add operation to prevent race conditions
                // Without this, multiple images from the same Normal folder could both see "no existing group"
                // and create duplicate groups
                FileGroup? groupToReturn;
                lock (_lockObject)
                {
                    // Check if this matches an existing group
                    FileGroup? existingGroup = FindMatchingExistingGroup(newGroup);

                    if (existingGroup != null)
                    {
                        // Merge new data into existing group
                        _logger.LogInformation("Merging into existing group {GroupId} (matched from {NewGroupId})", 
                            existingGroup.GroupId, newGroup.GroupId);
                        MergeGroups(existingGroup, newGroup);
                        groupToReturn = existingGroup;
                    }
                    else
                    {
                        // Add as new group
                        // Assign valid unique GroupId (FileGroupMatcher always gives group_001 for single files)
                        var newGroupId = $"group_{_nextGroupId:D3}";
                        _nextGroupId++;
                        _logger.LogInformation("Assigning new unique GroupId {NewGroupId} (matched was {OldGroupId})", 
                            newGroupId, newGroup.GroupId);
                        newGroup.GroupId = newGroupId;
                        
                        _activeGroups[newGroup.GroupId] = newGroup;
                        groupToReturn = newGroup;
                    }
                }

                // Raise events outside the lock to avoid potential deadlocks
                if (groupToReturn == newGroup)
                {
                    _logger.LogInformation("Creating new group {GroupId}", newGroup.GroupId);
                    
                    // GUI Log: New group created
                    var newFileName = GetRepresentativeFileName(newGroup);
                    var dataType = DetermineDataTypeForGroup(newGroup);
                    _uiLog?.Invoke(LogSeverity.Debug, dataType.ToString(), 
                        $"[{newFileName}] 새 그룹 생성 → {newGroup.GroupId}");
                    
                    OnGroupCreated(newGroup);
                }
                else
                {
                    OnGroupUpdated(groupToReturn);
                }
                
                // Try to match pending NIR files to this group (if not already has NIR)
                if (!groupToReturn.HasNir && fileType != FileType.Nir)
                {
                    TryMatchPendingNirToGroup(groupToReturn);
                }
                
                return groupToReturn;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating group for {FileType} file: {Path}", 
                    fileType, filePath);
                return null;
            }
        }

        /// <summary>
        /// Create UnmatchedFiles structure for a single file
        /// Extracted from CreateNewGroupAsync for reuse
        /// </summary>
        private UnmatchedFiles? CreateUnmatchedFilesForSingleFile(string filePath, FileType fileType)
        {
            var unmatchedFiles = new UnmatchedFiles();

            // Determine line number based on path
            int lineNumber = 1; // Default
            if (_currentConfig != null)
            {
                if ((!string.IsNullOrEmpty(_currentConfig.MatchingSettings.Nir2Path) && filePath.StartsWith(_currentConfig.MatchingSettings.Nir2Path, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(_currentConfig.MatchingSettings.Normal2Path) && filePath.StartsWith(_currentConfig.MatchingSettings.Normal2Path, StringComparison.OrdinalIgnoreCase)))
                {
                    lineNumber = 2;
                }
                else
                {
                    // Check camera paths for line 2 cams (4, 5, 6)
                    for (int i = 4; i <= 6; i++)
                    {
                        var camPath = _currentConfig.MatchingSettings.GetCameraPath(i);
                        if (!string.IsNullOrEmpty(camPath) && filePath.StartsWith(camPath, StringComparison.OrdinalIgnoreCase))
                        {
                            lineNumber = 2;
                            break;
                        }
                    }
                }
            }

            switch (fileType)
            {
                case FileType.Nir:
                    // NIR files are stored in nested dictionary: line -> nirKey -> path
                    var nirKey = Path.GetFileNameWithoutExtension(filePath);
                    var nirLine = lineNumber == 2 ? "nir2" : "nir1";
                    
                    if (!unmatchedFiles.NirFiles.ContainsKey(nirLine))
                    {
                        unmatchedFiles.NirFiles[nirLine] = new Dictionary<string, string>();
                    }
                    unmatchedFiles.NirFiles[nirLine][nirKey] = filePath;
                    break;

                case FileType.Normal:
                    // Normal: filePath is already the folder path (not a file inside it)
                    // Example: Z:\path\normal\C251201T140543_0
                    var folderKey = Path.GetFileName(filePath);  // Extract folder name directly
                    var normalLine = lineNumber == 2 ? "normal2" : "normal1";
                    
                    if (!unmatchedFiles.NormalFolders.ContainsKey(normalLine))
                    {
                        unmatchedFiles.NormalFolders[normalLine] = new Dictionary<string, string>();
                    }
                    unmatchedFiles.NormalFolders[normalLine][folderKey] = filePath;  // Use filePath (the folder itself)
                    break;

                case FileType.Camera:
                    // Camera files: extract timestamp and add to list
                    var timestamp = ExtractTimestamp(filePath, fileType);
                    if (timestamp == null)
                    {
                        _logger.LogWarning("TIMESTAMP FAILED: Camera file={FileName}", Path.GetFileName(filePath));
                        return null;
                    }
                    
                    _logger.LogDebug("TIMESTAMP OK: Camera file={FileName} → {Timestamp:yyyy-MM-dd HH:mm:ss}", 
                        Path.GetFileName(filePath), timestamp.Value);

                    // Determine camera number from path
                    for (int i = 1; i <= 6; i++)
                    {
                        var camPath = _currentConfig?.MatchingSettings.GetCameraPath(i);
                        if (!string.IsNullOrEmpty(camPath) && filePath.StartsWith(camPath, StringComparison.OrdinalIgnoreCase))
                        {
                            var cameraKey = $"cam{i}";
                            if (!unmatchedFiles.CameraFiles.ContainsKey(cameraKey))
                            {
                                unmatchedFiles.CameraFiles[cameraKey] = new List<TimestampedFile>();
                            }
                            
                            unmatchedFiles.CameraFiles[cameraKey].Add(new TimestampedFile
                            {
                                FileName = Path.GetFileName(filePath),
                                AbsolutePath = filePath,
                                Timestamp = timestamp.Value
                            });
                            break;
                        }
                    }
                    break;

                default:
                    return null;
            }

            return unmatchedFiles;
        }

        /// <summary>
        /// Create FileGroup directly from a single file (bypassing FileMatchingEngine)
        /// This ensures DataSequenceSettings-based matching works correctly
        /// </summary>
        private FileGroup? CreateGroupFromSingleFile(string filePath, FileType fileType)
        {
            try
            {
                // Determine line number based on path
                int lineNumber = 1; // Default
                if (_currentConfig != null)
                {
                    if ((!string.IsNullOrEmpty(_currentConfig.MatchingSettings.Nir2Path) && filePath.StartsWith(_currentConfig.MatchingSettings.Nir2Path, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(_currentConfig.MatchingSettings.Normal2Path) && filePath.StartsWith(_currentConfig.MatchingSettings.Normal2Path, StringComparison.OrdinalIgnoreCase)))
                    {
                        lineNumber = 2;
                    }
                    else
                    {
                        // Check camera paths for line 2 cams (4, 5, 6)
                        for (int i = 4; i <= 6; i++)
                        {
                            var camPath = _currentConfig.MatchingSettings.GetCameraPath(i);
                            if (!string.IsNullOrEmpty(camPath) && filePath.StartsWith(camPath, StringComparison.OrdinalIgnoreCase))
                            {
                                lineNumber = 2;
                                break;
                            }
                        }
                    }
                }

                var group = new FileGroup
                {
                    GroupId = "temp", // Will be assigned unique ID later
                    LineNumber = lineNumber,
                    CreatedAt = DateTime.UtcNow,
                    Status = GroupStatus.Pending,
                    CameraFiles = new Dictionary<string, string>()
                };

                switch (fileType)
                {
                    case FileType.Nir:
                        // NIR files are deferred - add to pending queue
                        // They will be matched when Non-NIR groups are created
                        var nirTimestamp = ExtractTimestamp(filePath, fileType);
                        
                        if (!nirTimestamp.HasValue)
                        {
                            _logger.LogWarning("Could not extract timestamp from NIR file: {Path}", filePath);
                            return null;
                        }

                        lock (_lockObject)
                        {
                            _pendingNirFiles.Add((filePath, nirTimestamp.Value, DateTime.UtcNow));
                            _logger.LogInformation("NIR file added to pending queue: {Path}, Timestamp={Timestamp}, QueueSize={Size}",
                                filePath, nirTimestamp.Value.ToString("HH:mm:ss"), _pendingNirFiles.Count);
                        }
                        
                        // Return null - NIR will be processed when matched to a group
                        return null;

                    case FileType.Normal:
                        // Extract folder name and timestamp
                        var folderKey = Path.GetFileName(filePath);
                        var normalTimestamp = ExtractTimestamp(filePath, fileType);

                        if (!normalTimestamp.HasValue)
                        {
                            _logger.LogWarning("Could not extract timestamp from Normal folder: {Path}", filePath);
                            return null;
                        }

                        group.NormalFolder = folderKey;
                        group.MainImagePath = Path.Combine(filePath, "stitched_original.png");
                        group.Timestamp = normalTimestamp.Value;
                        group.CreatedAt = normalTimestamp.Value;
                        group.Status = GroupStatus.Complete;
                        break;

                    case FileType.Camera:
                        // Extract camera number and timestamp
                        var cameraTimestamp = ExtractTimestamp(filePath, fileType);

                        if (!cameraTimestamp.HasValue)
                        {
                            _logger.LogWarning("Could not extract timestamp from Camera file: {Path}", filePath);
                            return null;
                        }

                        // Determine camera number from path
                        for (int i = 1; i <= 6; i++)
                        {
                            var camPath = _currentConfig?.MatchingSettings.GetCameraPath(i);
                            if (!string.IsNullOrEmpty(camPath) && filePath.StartsWith(camPath, StringComparison.OrdinalIgnoreCase))
                            {
                                var cameraKey = $"cam{i}";
                                group.CameraFiles[cameraKey] = filePath;
                                group.Timestamp = cameraTimestamp.Value;
                                group.CreatedAt = cameraTimestamp.Value;
                                break;
                            }
                        }

                        if (group.CameraFiles.Count == 0)
                        {
                            _logger.LogWarning("Could not determine camera number for file: {Path}", filePath);
                            return null;
                        }
                        break;

                    default:
                        _logger.LogWarning("Unsupported file type: {FileType}", fileType);
                        return null;
                }

                _logger.LogDebug("Created group from single file: Type={FileType}, LineNumber={LineNumber}, Timestamp={Timestamp}",
                    fileType, group.LineNumber, group.Timestamp);

                return group;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group from single file: {Path}", filePath);
                return null;
            }
        }

        /// <summary>
        /// Find existing group that matches the new group by stable identifiers (NormalFolder, NirKey)
        /// </summary>
        private FileGroup? FindMatchingExistingGroup(FileGroup newGroup)
        {
            lock (_lockObject)
            {
                _logger.LogDebug("FindMatchingExistingGroup: NormalFolder={NormalFolder}, NirKey={NirKey}, LineNumber={LineNumber}",
                    newGroup.NormalFolder ?? "null", newGroup.NirKey ?? "null", newGroup.LineNumber);

                // Match 1: By NormalFolder (primary stable identifier for Normal-based groups)
                if (!string.IsNullOrEmpty(newGroup.NormalFolder))
                {
                    var newGroupDataType = DetermineDataTypeForGroup(newGroup);
                    
                    var match = _activeGroups.Values
                        .Where(g => g.NormalFolder == newGroup.NormalFolder)
                        .Where(g => !HasDataType(g, newGroupDataType))  // Non-Duplicate Filter
                        .FirstOrDefault();
                    
                    if (match != null)
                    {
                        _logger.LogInformation("Match 1: Found by NormalFolder={Folder}, DataType={Type}", 
                            newGroup.NormalFolder, newGroupDataType);
                        
                        // GUI Log
                        var newFileName = GetRepresentativeFileName(newGroup);
                        var targetDataType = DetermineDataTypeForGroup(match);
                        var targetFileName = GetFileNameForDataType(match, targetDataType);
                        var newTime = newGroup.Timestamp.ToString("HH:mm:ss");
                        var targetTime = match.Timestamp.ToString("HH:mm:ss");
                        var timeDiff = Math.Abs((newGroup.Timestamp - match.Timestamp).TotalSeconds);
                        
                        _uiLog?.Invoke(LogSeverity.Debug, newGroupDataType.ToString(), 
                            $"[{newFileName}] 그룹 {match.GroupId}의 {targetDataType} 파일({targetFileName}, {targetTime})과 매칭 시도 → {timeDiff:F0}초 차이로 성공");
                        
                        return match;
                    }
                    else if (_activeGroups.Values.Any(g => g.NormalFolder == newGroup.NormalFolder))
                    {
                        _logger.LogDebug("Match 1: NormalFolder={Folder} exists but already has {Type}", 
                            newGroup.NormalFolder, newGroupDataType);
                    }
                }

                // Match 2: By NirKey (stable identifier for NIR-only groups)
                if (!string.IsNullOrEmpty(newGroup.NirKey))
                {
                    var newGroupDataType = DetermineDataTypeForGroup(newGroup);
                    
                    var match = _activeGroups.Values
                        .Where(g => g.NirKey == newGroup.NirKey)
                        .Where(g => !HasDataType(g, newGroupDataType))  // Non-Duplicate Filter
                        .FirstOrDefault();
                    
                    if (match != null)
                    {
                        _logger.LogInformation("Match 2: Found by NirKey={Key}, DataType={Type}", 
                            newGroup.NirKey, newGroupDataType);
                        
                        // GUI Log
                        var newFileName = GetRepresentativeFileName(newGroup);
                        var targetDataType = DetermineDataTypeForGroup(match);
                        var targetFileName = GetFileNameForDataType(match, targetDataType);
                        var newTime = newGroup.Timestamp.ToString("HH:mm:ss");
                        var targetTime = match.Timestamp.ToString("HH:mm:ss");
                        var timeDiff = Math.Abs((newGroup.Timestamp - match.Timestamp).TotalSeconds);
                        
                        _uiLog?.Invoke(LogSeverity.Debug, newGroupDataType.ToString(), 
                            $"[{newFileName}] 그룹 {match.GroupId}의 {targetDataType} 파일({targetFileName}, {targetTime})과 매칭 시도 → {timeDiff:F0}초 차이로 성공");
                        
                        return match;
                    }
                    else if (_activeGroups.Values.Any(g => g.NirKey == newGroup.NirKey))
                    {
                        _logger.LogDebug("Match 2: NirKey={Key} exists but already has {Type}", 
                            newGroup.NirKey, newGroupDataType);
                    }
                }

                // ============================================================
                // Match 3: By Timestamp + LineNumber + Priority (with Temporal Ordering Constraint)
                // ============================================================
                if (newGroup.Timestamp != DateTime.MinValue && _currentConfig?.DataSequenceSettings != null)
                {
                    try
                    {
                        var orderedTypes = _currentConfig.DataSequenceSettings.GetOrderedTypes();
                        if (orderedTypes.Count == 0)
                        {
                            _logger.LogDebug("DataSequenceSettings has no enabled types, skipping Match 3");
                        }
                        else
                        {
                            var newGroupType = DetermineDataTypeForGroup(newGroup);
                            var newGroupOrder = GetPriority(newGroupType);

                            _logger.LogDebug("Match 3: Searching for timestamp match. NewGroup Type={Type}, Order={Order}, Timestamp={Timestamp}",
                                newGroupType, newGroupOrder, newGroup.Timestamp);

                            // Find the immediate predecessor type (next lower Order value)
                            DataType? predecessorType = null;
                            int predecessorOrder = -1;
                            
                            foreach (var dataType in orderedTypes)
                            {
                                var order = GetPriority(dataType);
                                if (order < newGroupOrder && order > predecessorOrder)
                                {
                                    predecessorType = dataType;
                                    predecessorOrder = order;
                                }
                            }

                            if (predecessorType == null)
                            {
                                _logger.LogDebug("Match 3: No predecessor type found for {NewGroupType} (Order={Order})", 
                                    newGroupType, newGroupOrder);
                            }
                            else
                            {
                                var dataType = predecessorType.Value;
                                var candidateOrder = predecessorOrder;
                                
                                var minDelay = _currentConfig.DataSequenceSettings.GetMinDelay(dataType);
                                var maxDelay = _currentConfig.DataSequenceSettings.GetMaxDelay(dataType);

                                _logger.LogDebug("Match 3: Comparing {NewGroupType} (Order={NewOrder}) with predecessor {PredecessorType} (Order={PredOrder}), Range={MinDelay}~{MaxDelay}s",
                                    newGroupType, newGroupOrder, dataType, candidateOrder, minDelay, maxDelay);

                                // Find candidates within MinDelay~MaxDelay range
                                var newGroupDataType = DetermineDataTypeForGroup(newGroup);

                                // Get all potential candidates (without temporal ordering check)
                                var potentialCandidates = _activeGroups.Values
                                    .Where(g => g.LineNumber == newGroup.LineNumber)
                                    .OrderBy(g => g.GroupId)  // Try in creation order
                                    .ToList();

                                // Iterate through candidates and find first valid match
                                FileGroup? closest = null;
                                int attemptCount = 0;
                                
                                foreach (var candidate in potentialCandidates)
                                {
                                    // Check if candidate already has the new data type (duplicate prevention)
                                    if (HasDataType(candidate, newGroupDataType))
                                    {
                                        // Skip silently - would overwrite existing data
                                        continue;
                                    }
                                    
                                    // OPTIONAL predecessor check: prefer groups with predecessor, but allow without
                                    bool hasPredecessor = HasDataType(candidate, dataType);
                                    
                                    attemptCount++;
                                    var timeDiff = (newGroup.Timestamp - candidate.Timestamp).TotalSeconds;
                                    
                                    // TEMPORAL ORDERING CHECK: newGroup must arrive AFTER candidate
                                    // EXCEPTION: If predecessor is NIR and candidate doesn't have NIR,
                                    // skip temporal ordering check since NIR is optional
                                    bool skipOrderingCheck = (dataType == DataType.NIR && !hasPredecessor);
                                    
                                    if (timeDiff < 0 && !skipOrderingCheck)
                                    {
                                        // Order violation - skip this candidate and try next
                                        var newFileName = GetRepresentativeFileName(newGroup);
                                        var candidateFileName = hasPredecessor 
                                            ? GetFileNameForDataType(candidate, dataType)
                                            : GetRepresentativeFileName(candidate);
                                        var newTime = newGroup.Timestamp.ToString("HH:mm:ss");
                                        var candidateTime = candidate.Timestamp.ToString("HH:mm:ss");
                                        
                                        var predecessorLabel = hasPredecessor ? dataType.ToString() : "그룹";
                                        _uiLog?.Invoke(LogSeverity.Debug, newGroupDataType.ToString(), 
                                            $"[{newFileName}] {candidate.GroupId} 순서 위반 ({newGroupDataType} {newTime} < {predecessorLabel} {candidateTime}) → 건너뜀");
                                        
                                        continue; // Skip to next candidate
                                    }
                                    
                                    // Check if timestamps are within tolerance
                                    var absDiff = Math.Abs(timeDiff);
                                    if (absDiff >= minDelay && absDiff <= maxDelay)
                                    {
                                        closest = candidate;
                                        break; // Found valid match
                                    }
                                    else
                                    {
                                        // Log tolerance exceeded
                                        var newFileName = GetRepresentativeFileName(newGroup);
                                        var candidateFileName = hasPredecessor
                                            ? GetFileNameForDataType(candidate, dataType)
                                            : GetRepresentativeFileName(candidate);
                                        
                                        _uiLog?.Invoke(LogSeverity.Debug, newGroupDataType.ToString(), 
                                            $"[{newFileName}] {candidate.GroupId} 범위 초과 ({absDiff:F0}초, 허용범위: {minDelay}~{maxDelay}초) → 건너뜀");
                                    }
                                }

                                if (closest != null)
                                {
                                    var timeDelta = (newGroup.Timestamp - closest.Timestamp).TotalSeconds;

                                    _logger.LogInformation(
                                        "Match 3: Found timestamp match! GroupId={GroupId}, MatchedType={DataType}, " +
                                        "TimeDelta={TimeDelta:F1}s, Range={MinDelay}~{MaxDelay}s, " +
                                        "CandidateOrder={CandidateOrder}, NewGroupOrder={NewGroupOrder}",
                                        closest.GroupId, dataType, timeDelta, minDelay, maxDelay, candidateOrder, newGroupOrder);
                                    
                                    // GUI Log
                                    var newFileName = GetRepresentativeFileName(newGroup);
                                    var targetFileName = GetFileNameForDataType(closest, dataType);
                                    var newTime = newGroup.Timestamp.ToString("HH:mm:ss");
                                    var targetTime = closest.Timestamp.ToString("HH:mm:ss");
                                    
                                    _uiLog?.Invoke(LogSeverity.Debug, newGroupDataType.ToString(), 
                                        $"[{newFileName}] 그룹 {closest.GroupId}의 {dataType} 파일({targetFileName}, {targetTime})과 매칭 시도 → {timeDelta:F0}초 차이로 성공");

                                    return closest;
                                }
                            }

                            _logger.LogDebug("Match 3: No timestamp matches found within configured tolerances");
                            
                            // GUI Log - only if we have a valid timestamp
                            if (newGroup.Timestamp != DateTime.MinValue)
                            {
                                var newFileName = GetRepresentativeFileName(newGroup);
                                var newTime = newGroup.Timestamp.ToString("HH:mm:ss");
                                
                                _uiLog?.Invoke(LogSeverity.Debug, newGroupType.ToString(), 
                                    $"[{newFileName}] 타임스탬프 매칭 실패 (설정된 tolerance 범위 내에 적합한 그룹 없음) → 새 그룹 생성");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error during Match 3 (timestamp matching), falling back to creating new group");
                    }
                }
                else
                {
                    if (newGroup.Timestamp == DateTime.MinValue)
                        _logger.LogDebug("Skipping Match 3: newGroup has no valid timestamp");
                    else
                        _logger.LogDebug("Skipping Match 3: DataSequenceSettings not available");
                }

                // No match found - will create new group
                _logger.LogInformation("No match found for NormalFolder={NormalFolder}, NirKey={NirKey} - creating new group",
                    newGroup.NormalFolder ?? "null", newGroup.NirKey ?? "null");
                return null;
            }
        }

        /// <summary>
        /// Merge new group data into existing group
        /// Preserves existing data and adds new file paths
        /// </summary>
        private void MergeGroups(FileGroup existingGroup, FileGroup newGroup)
        {
            // Merge stable identifiers (CRITICAL for Non-Duplicate Filter to work)
            if (!string.IsNullOrEmpty(newGroup.NormalFolder) && string.IsNullOrEmpty(existingGroup.NormalFolder))
            {
                existingGroup.NormalFolder = newGroup.NormalFolder;
                _logger.LogDebug("Merged NormalFolder: {NormalFolder}", newGroup.NormalFolder);
            }

            if (!string.IsNullOrEmpty(newGroup.NirKey) && string.IsNullOrEmpty(existingGroup.NirKey))
            {
                existingGroup.NirKey = newGroup.NirKey;
                _logger.LogDebug("Merged NirKey: {NirKey}", newGroup.NirKey);
            }

            // Merge NIR file
            if (!string.IsNullOrEmpty(newGroup.NirFilePath))
            {
                existingGroup.NirFilePath = newGroup.NirFilePath;
                existingGroup.HasNir = true;
            }

            // Merge Main Image
            if (!string.IsNullOrEmpty(newGroup.MainImagePath))
            {
                existingGroup.MainImagePath = newGroup.MainImagePath;
            }

            // Merge camera files (skip if already exists to prevent unnecessary GroupUpdated events)
            foreach (var camera in newGroup.CameraFiles)
            {
                if (!string.IsNullOrEmpty(camera.Value) && !existingGroup.CameraFiles.ContainsKey(camera.Key))
                {
                    existingGroup.CameraFiles[camera.Key] = camera.Value;
                }
            }

            _logger.LogDebug("Merged group data: NIR={HasNir}, Main={HasMain}, Cameras={CameraCount}",
                existingGroup.HasNir,
                !string.IsNullOrEmpty(existingGroup.MainImagePath),
                existingGroup.CameraFiles.Count(kvp => !string.IsNullOrEmpty(kvp.Value)));
        }

        /// <summary>
        /// Extract timestamp based on file type
        /// </summary>
        private DateTime? ExtractTimestamp(string filePath, FileType fileType)
        {
            return fileType switch
            {
                FileType.Nir => ExtractTimestampFromNirFile(filePath),
                FileType.Normal => ExtractTimestampFromFolderName(filePath),  // filePath is already the folder
                FileType.Camera => ExtractTimestampFromCameraFile(filePath),
                _ => null
            };
        }

        /// <summary>
        /// Extract timestamp from NIR file (from filename in path)
        /// </summary>
        private DateTime? ExtractTimestampFromNirFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            // Pattern 1: With run_N prefix (e.g., run_120251201T140542)
            // Match "run_" + single digit + timestamp (YYYYMMDDTHHMMSS)
            var match = System.Text.RegularExpressions.Regex.Match(fileName, @"run_\d(\d{8}T\d{6})");
            if (match.Success)
            {
                if (DateTime.TryParseExact(
                    match.Groups[1].Value,
                    "yyyyMMddTHHmmss",
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out var dt))
                {
                    _logger.LogDebug("Extracted timestamp from NIR file (run_N format): {FileName} → {Timestamp}", 
                        fileName, dt);
                    return dt;
                }
            }
            
            // Pattern 2: Direct timestamp without prefix (e.g., 20250926T103033)
            match = System.Text.RegularExpressions.Regex.Match(fileName, @"(\d{8}T\d{6})");
            if (match.Success)
            {
                if (DateTime.TryParseExact(
                    match.Groups[1].Value,
                    "yyyyMMddTHHmmss",
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out var dt))
                {
                    _logger.LogDebug("Extracted timestamp from NIR file (direct format): {FileName} → {Timestamp}", 
                        fileName, dt);
                    return dt;
                }
            }

            _logger.LogWarning("Could not extract timestamp from NIR filename: {FileName}", fileName);
            return null;
        }

        /// <summary>
        /// Extract timestamp from folder name
        /// </summary>
        private DateTime? ExtractTimestampFromFolderName(string? folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                return null;

            var folderName = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(folderName) || !folderName.StartsWith("C"))
                return null;

            // Pattern 1: C + 6 digits (date) + T + 6 digits (time)
            // Example: C251204T111028
            var match = System.Text.RegularExpressions.Regex.Match(folderName, @"C(\d{6}T\d{6})");
            if (match.Success)
            {
                if (DateTime.TryParseExact(
                    match.Groups[1].Value,
                    "yyMMddTHHmmss",
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out var dt))
                {
                    return dt;
                }
            }

            // Pattern 2: C + 8 digits (date) + _ + 6 digits (time)
            // Example: C20240115_143022
            match = System.Text.RegularExpressions.Regex.Match(folderName, @"C(\d{8}_\d{6})");
            if (match.Success)
            {
                if (DateTime.TryParseExact(
                    match.Groups[1].Value,
                    "yyyyMMdd_HHmmss",
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out var dt))
                {
                    return dt;
                }
            }

            return null;
        }

        /// <summary>
        /// Extract timestamp from camera file (from filename)
        /// </summary>
        private DateTime? ExtractTimestampFromCameraFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            // Pattern: YYYYMMDD_HHMMSS
            // Example: 20241211_143022
            var match = System.Text.RegularExpressions.Regex.Match(fileName, @"(\d{8}_\d{6})");
            if (match.Success)
            {
                if (DateTime.TryParseExact(
                    match.Groups[1].Value,
                    "yyyyMMdd_HHmmss",
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out var dt))
                {
                    return dt;
                }
            }

            return null;
        }

        /// <summary>
        /// Get matching tolerance in seconds for a specific file type.
        /// Uses DataSequenceSettings if available, otherwise falls back to deprecated properties.
        /// </summary>
        private double GetMatchingTolerance(FileType fileType)
        {
            // If DataSequenceSettings is configured, use it
            if (_currentConfig?.DataSequenceSettings != null)
            {
                var dataType = MapFileTypeToDataType(fileType);
                return _currentConfig.DataSequenceSettings.GetMaxDelay(dataType);
            }

            // Fallback to deprecated properties
            return fileType switch
            {
                FileType.Nir => _currentConfig?.MatchingSettings.NirTimeWindowSeconds ?? 300,
                FileType.Camera => _currentConfig?.MatchingSettings.CameraTimeWindowSeconds ?? 60,
                FileType.Normal => _currentConfig?.MatchingSettings.NormalFolderTimeWindowSeconds ?? 120,
                _ => 60 // Default fallback
            };
        }

        /// <summary>
        /// Map FileType enum to DataType enum for DataSequenceSettings lookup.
        /// </summary>
        private DataType MapFileTypeToDataType(FileType fileType)
        {
            return fileType switch
            {
                FileType.Nir => DataType.NIR,
                FileType.Normal => DataType.Normal,
                FileType.Camera => DataType.Cam1, // Default to Cam1 for generic camera type
                _ => DataType.Normal // Default fallback
            };
        }

        /// <summary>
        /// Check if group timestamp matches within tolerance window
        /// </summary>
        private bool IsMatchingTimestamp(FileGroup group, DateTime timestamp, FileType fileType)
        {
            var toleranceSeconds = GetMatchingTolerance(fileType);
            return Math.Abs((group.Timestamp - timestamp).TotalSeconds) < toleranceSeconds;
        }

        /// <summary>
        /// Update group with new file based on file type
        /// </summary>
        private void UpdateGroupWithFile(FileGroup group, string filePath, FileType fileType)
        {
            switch (fileType)
            {
                case FileType.Nir:
                    group.NirFilePath = filePath;
                    group.HasNir = true;
                    break;
                
                case FileType.Normal:
                    group.MainImagePath = filePath;
                    break;
                
                case FileType.Camera:
                    AddCameraFileToGroup(group, filePath);
                    break;
            }
        }

        /// <summary>
        /// Add camera file to group (determine which camera based on path)
        /// </summary>
        private void AddCameraFileToGroup(FileGroup group, string filePath)
        {
            var directory = Path.GetDirectoryName(filePath)?.ToLowerInvariant() ?? "";
            
            // Determine camera number from directory
            for (int i = 1; i <= 6; i++)
            {
                if (directory.Contains($"cam{i}") || directory.Contains($"camera{i}"))
                {
                    // Add to CameraFiles dictionary
                    var cameraKey = $"cam{i}";
                    group.CameraFiles[cameraKey] = filePath;
                    _logger.LogInformation("Added Camera{CamNum} file to group {GroupId}", i, group.GroupId);
                    return;
                }
            }
            
            _logger.LogWarning("Could not determine camera number from path: {Path}", filePath);
        }

        /// <summary>
        /// Create new group using FileGroupMatcher
        /// </summary>
        private async Task<FileGroup?> CreateNewGroupAsync(string filePath, FileType fileType, DateTime timestamp)
        {
            // Create UnmatchedFiles with single file
            var unmatchedFiles = new UnmatchedFiles();

            // Determine line number based on path
            int lineNumber = 1; // Default
            if (_currentConfig != null)
            {
                if ((!string.IsNullOrEmpty(_currentConfig.MatchingSettings.Nir2Path) && filePath.StartsWith(_currentConfig.MatchingSettings.Nir2Path, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(_currentConfig.MatchingSettings.Normal2Path) && filePath.StartsWith(_currentConfig.MatchingSettings.Normal2Path, StringComparison.OrdinalIgnoreCase)))
                {
                    lineNumber = 2;
                }
                else
                {
                    // Check camera paths for line 2 cams (4, 5, 6)
                    for (int i = 4; i <= 6; i++)
                    {
                        var camPath = _currentConfig.MatchingSettings.GetCameraPath(i);
                        if (!string.IsNullOrEmpty(camPath) && filePath.StartsWith(camPath, StringComparison.OrdinalIgnoreCase))
                        {
                            lineNumber = 2;
                            break;
                        }
                    }
                }
            }

            switch (fileType)
            {
                case FileType.Nir:
                    // NIR files are stored in nested dictionary: line -> nirKey -> path
                    var nirKey = Path.GetFileNameWithoutExtension(filePath);
                    var nirLine = lineNumber == 2 ? "nir2" : "nir1";
                    
                    if (!unmatchedFiles.NirFiles.ContainsKey(nirLine))
                    {
                        unmatchedFiles.NirFiles[nirLine] = new Dictionary<string, string>();
                    }
                    unmatchedFiles.NirFiles[nirLine][nirKey] = filePath;
                    break;

                case FileType.Normal:
                    // Normal folders are stored in nested dictionary: line -> folderKey -> path
                    var folderPath = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(folderPath))
                    {
                        var folderKey = Path.GetFileName(folderPath);
                        var normalLine = lineNumber == 2 ? "normal2" : "normal1";
                        
                        if (!unmatchedFiles.NormalFolders.ContainsKey(normalLine))
                        {
                            unmatchedFiles.NormalFolders[normalLine] = new Dictionary<string, string>();
                        }
                        unmatchedFiles.NormalFolders[normalLine][folderKey] = folderPath;
                    }
                    break;

                case FileType.Camera:
                    // Camera files usually attach to existing groups
                    // Create a minimal group for orphaned camera files
                    var cameraGroup = new FileGroup
                    {
                        GroupId = timestamp.ToString("yyyyMMddTHHmmss"),
                        Timestamp = timestamp,
                        LineNumber = lineNumber
                    };
                    AddCameraFileToGroup(cameraGroup, filePath);
                    return cameraGroup;
            }

            // Use FileGroupMatcher to create group
            var groups = await _fileGroupMatcher.MatchFilesAsync(unmatchedFiles);
            return groups.FirstOrDefault();
        }

        #endregion

        #region Task 1.3: RemoveFromGroupAsync Implementation

        /// <summary>
        /// Remove file from group or delete group if empty
        /// </summary>
        private async Task RemoveFromGroupAsync(string filePath)
        {
            lock (_lockObject)
            {
                var affectedGroups = _activeGroups.Values
                    .Where(g => ContainsFile(g, filePath))
                    .ToList();

                foreach (var group in affectedGroups)
                {
                    _logger.LogInformation("Removing file from group {GroupId}: {Path}", 
                        group.GroupId, filePath);
                    
                    // Remove file from group
                    RemoveFileFromGroup(group, filePath);

                    // Check if group is now empty
                    if (IsGroupEmpty(group))
                    {
                        _activeGroups.Remove(group.GroupId);
                        OnGroupRemoved(group.GroupId);
                        _logger.LogInformation("Group {GroupId} removed (empty)", group.GroupId);
                    }
                    else
                    {
                        OnGroupUpdated(group);
                        _logger.LogInformation("Group {GroupId} updated after file removal", group.GroupId);
                    }
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Remove specific file from group
        /// </summary>
        private void RemoveFileFromGroup(FileGroup group, string filePath)
        {
            if (group.NirFilePath == filePath)
            {
                group.NirFilePath = string.Empty;
                group.HasNir = false;
            }
            else if (group.MainImagePath == filePath)
            {
                group.MainImagePath = string.Empty;
            }
            else
            {
                // Check if it's a camera file
                var cameraKey = group.CameraFiles.FirstOrDefault(kvp => kvp.Value == filePath).Key;
                if (!string.IsNullOrEmpty(cameraKey))
                {
                    group.CameraFiles.Remove(cameraKey);
                }
            }
        }

        /// <summary>
        /// Check if group has no files
        /// </summary>
        private bool IsGroupEmpty(FileGroup group)
        {
            return string.IsNullOrEmpty(group.NirFilePath)
                && string.IsNullOrEmpty(group.MainImagePath)
                && (group.CameraFiles == null || group.CameraFiles.Count == 0);
        }

        #endregion


        public async Task<List<FileGroup>> ProcessFileEventsAsync(List<FileSystemEventArgs> events)
        {
            if (events == null || events.Count == 0)
            {
                return new List<FileGroup>();
            }

            // Diagnostic logging: show first few events being processed
            _logger.LogInformation("ProcessFileEventsAsync called with {Count} events", events.Count);
            if (events.Count > 0)
            {
                foreach (var evt in events.Take(5))
                {
                    _logger.LogDebug("  Event: {ChangeType} - {Path}", evt.ChangeType, evt.FullPath);
                }
            }
            
            _logger.LogInformation("Processing {Count} file system events", events.Count);

            var updatedGroups = new List<FileGroup>();

            try
            {
                // For Normal folders: multiple image files trigger separate events
                // We need to deduplicate by folder path, not file path
                var processedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                foreach (var eventArgs in events)
                {
                    var filePath = eventArgs.FullPath;
                    
                    // Check if we've already processed this file recently (debouncing)
                    if (ShouldSkipEvent(eventArgs))
                    {
                        continue;
                    }

                    // Determine file type
                    var fileType = DetermineFileType(filePath);
                    if (fileType == FileType.Unknown)
                    {
                        continue;
                    }

                    // For Normal files: use folder path as key to prevent duplicates
                    string processKey;
                    if (fileType == FileType.Normal && !Directory.Exists(filePath))
                    {
                        // This is an image file inside a Normal folder
                        // Use the parent folder as the process key
                        var parentFolder = Path.GetDirectoryName(filePath);
                        processKey = parentFolder ?? filePath;
                    }
                    else
                    {
                        // For other types, use the file path itself
                        processKey = filePath;
                    }
                    
                    // Skip if we've already processed this key in this batch
                    if (processedPaths.Contains(processKey))
                    {
                        _logger.LogDebug("Skipping duplicate event for key: {Key}", processKey);
                        continue;
                    }
                    
                    processedPaths.Add(processKey);

                    // Mark file as processed (use processKey for Normal folders to prevent duplicates across batches)
                    MarkFileAsProcessed(processKey);

                    _logger.LogInformation("Processing {ChangeType} event for {FileType}: {Path}",
                        eventArgs.ChangeType, fileType, filePath);

                    // Handle different event types
                    switch (eventArgs.ChangeType)
                    {
                        case WatcherChangeTypes.Created:
                        case WatcherChangeTypes.Changed:
                            // For Camera files, Changed events often fire immediately after Created
                            // causing file access conflicts. CreateOrUpdateGroupAsync will detect
                            // if the file is already in a group and skip duplicate processing.
                            var group = await CreateOrUpdateGroupAsync(filePath, fileType);
                            if (group != null)
                            {
                                updatedGroups.Add(group);
                            }
                            break;

                        case WatcherChangeTypes.Deleted:
                            await RemoveFromGroupAsync(filePath);
                            break;
                    }
                }

                // Raise event for updated groups
                if (updatedGroups.Count > 0)
                {
                    OnFileGroupsUpdated(new FileGroupsUpdatedEventArgs
                    {
                        UpdatedGroups = updatedGroups,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file events");
                OnMonitoringError($"Error processing file events: {ex.Message}");
            }

            return updatedGroups;
        }

        private bool ContainsFile(FileGroup group, string filePath)
        {
            // Normalize paths for comparison
            var normalizedFilePath = Path.GetFullPath(filePath);

            // Check if the file path matches NIR key (could be full path or just key)
            if (!string.IsNullOrEmpty(group.NirKey))
            {
                try
                {
                    var nirFullPath = Path.GetFullPath(group.NirKey);
                    if (nirFullPath.Equals(normalizedFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                    // If NirKey is not a valid path, compare as-is
                    if (group.NirKey.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            // Check normal folder
            if (!string.IsNullOrEmpty(group.NormalFolder))
            {
                try
                {
                    var normalFullPath = Path.GetFullPath(group.NormalFolder);
                    if (normalFullPath.Equals(normalizedFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Ignore invalid paths
                }
            }

            // Check camera files
            foreach (var cameraPath in group.CameraFiles.Values)
            {
                if (!string.IsNullOrEmpty(cameraPath))
                {
                    try
                    {
                        var cameraFullPath = Path.GetFullPath(cameraPath);
                        if (cameraFullPath.Equals(normalizedFilePath, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // Ignore invalid paths
                    }
                }
            }

            return false;
        }

        private bool ShouldSkipEvent(FileSystemEventArgs eventArgs)
        {
            lock (_lockObject)
            {
                // For Normal folder files, use parent folder path for deduplication
                var checkPath = eventArgs.FullPath;
                var fileType = DetermineFileType(eventArgs.FullPath);
                
                if (fileType == FileType.Normal && !Directory.Exists(eventArgs.FullPath))
                {
                    // This is an image file inside a Normal folder
                    var parentFolder = Path.GetDirectoryName(eventArgs.FullPath);
                    if (!string.IsNullOrEmpty(parentFolder))
                    {
                        checkPath = parentFolder;
                    }
                }
                
                if (_processedFiles.TryGetValue(checkPath, out var lastProcessed))
                {
                    // Skip if processed within last 2 seconds (debouncing)
                    if ((DateTime.UtcNow - lastProcessed).TotalSeconds < 2)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private void MarkFileAsProcessed(string filePath)
        {
            lock (_lockObject)
            {
                _processedFiles[filePath] = DateTime.UtcNow;

                // Clean up old entries (keep only last 5 minutes)
                var cutoff = DateTime.UtcNow.AddMinutes(-5);
                var oldEntries = _processedFiles.Where(kvp => kvp.Value < cutoff).Select(kvp => kvp.Key).ToList();
                foreach (var key in oldEntries)
                {
                    _processedFiles.Remove(key);
                }
            }
        }

        private FileType DetermineFileType(string filePath)
        {
            if (_currentConfig == null) return FileType.Unknown;

            var settings = _currentConfig.MatchingSettings;
            var directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directory)) return FileType.Unknown;

            // Check Normal paths
            _logger.LogInformation("Checking Normal: Path={Path}, N1={N1}", filePath, settings.Normal1Path);

            if ((!string.IsNullOrEmpty(settings.Normal1Path) && filePath.StartsWith(settings.Normal1Path, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(settings.Normal2Path) && filePath.StartsWith(settings.Normal2Path, StringComparison.OrdinalIgnoreCase)))
            {
                // Check if this is a Normal folder itself (e.g., Z:\normal\C251201T140543_0)
                if (Directory.Exists(filePath))
                {
                    // This is a folder - treat as Normal folder event
                    var folderName = Path.GetFileName(filePath);
                    if (!string.IsNullOrEmpty(folderName) && folderName.StartsWith("C"))
                    {
                        _logger.LogInformation("Folder Identified as Normal: {Path}", filePath);
                        return FileType.Normal;
                    }
                }

                // Only consider image files as triggers for Normal groups
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
                {
                    _logger.LogInformation("File Identified as Normal: {Path}", filePath);
                    return FileType.Normal;
                }
                // Ignore other files in Normal folder (cleaning logs, stitching temp files, etc.)
                return FileType.Unknown;
            }

            // Check NIR paths
            if ((!string.IsNullOrEmpty(settings.Nir1Path) && filePath.StartsWith(settings.Nir1Path, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(settings.Nir2Path) && filePath.StartsWith(settings.Nir2Path, StringComparison.OrdinalIgnoreCase)))
            {
                // Accept .txt and .csv files for NIR
                // .spc files are excluded to avoid duplicate groups (they're paired with .txt files)
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext == ".txt" || ext == ".csv")
                {
                    _logger.LogInformation("File Identified as NIR: {Path}", filePath);
                    return FileType.Nir;
                }
                return FileType.Unknown;
            }

            // Check Camera paths
            for (int i = 1; i <= 6; i++)
            {
                var camPath = settings.GetCameraPath(i);
                if (!string.IsNullOrEmpty(camPath) && filePath.StartsWith(camPath, StringComparison.OrdinalIgnoreCase))
                {
                    var ext = Path.GetExtension(filePath).ToLowerInvariant();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp")
                    {
                        _logger.LogInformation("File Identified as Camera: {Path}", filePath);
                        return FileType.Camera;
                    }
                    return FileType.Unknown;
                }
            }

            _logger.LogWarning("File Unknown: {Path}. Dir={Dir}, N1={N1}", filePath, directory, settings.Normal1Path);
            return FileType.Unknown;
        }

        protected virtual void OnGroupCreated(FileGroup group)
        {
            GroupCreated?.Invoke(this, group);
        }

        protected virtual void OnGroupRemoved(string groupId)
        {
            GroupRemoved?.Invoke(this, groupId);
        }

        protected virtual void OnGroupUpdated(FileGroup group)
        {
            GroupUpdated?.Invoke(this, group);
        }

        protected virtual void OnMonitoringError(string errorMessage)
        {
            MonitoringError?.Invoke(this, errorMessage);
        }

        protected virtual void OnFileGroupsCreated(FileGroupsCreatedEventArgs e)
        {
            FileGroupsCreated?.Invoke(this, e);
        }

        protected virtual void OnFileGroupsUpdated(FileGroupsUpdatedEventArgs e)
        {
            FileGroupsUpdated?.Invoke(this, e);
        }

        private async void OnFileChanged(object? sender, FileSystemEventArgs e)
        {
            // Diagnostic logging: confirm events reach orchestrator
            _logger.LogInformation("OnFileChanged triggered: {ChangeType} - {Path}", e.ChangeType, e.FullPath);
            
            try
            {
                await ProcessFileEventsAsync(new List<FileSystemEventArgs> { e });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file change event for {Path}", e.FullPath);
            }
        }

        /// <summary>
        /// Get the filename for a specific DataType from a FileGroup
        /// Used for accurate logging of which file was matched
        /// </summary>
        private string GetFileNameForDataType(FileGroup group, DataType dataType)
        {
            switch (dataType)
            {
                case DataType.NIR:
                    if (!string.IsNullOrEmpty(group.NirFilePath))
                        return Path.GetFileName(group.NirFilePath);
                    break;

                case DataType.Normal:
                    if (!string.IsNullOrEmpty(group.MainImagePath))
                    {
                        var folderPath = Path.GetDirectoryName(group.MainImagePath);
                        if (!string.IsNullOrEmpty(folderPath))
                            return Path.GetFileName(folderPath);
                        return Path.GetFileName(group.MainImagePath);
                    }
                    break;

                case DataType.Cam1:
                    if (group.CameraFiles.TryGetValue("cam1", out var cam1))
                        return Path.GetFileName(cam1);
                    if (group.CameraFiles.TryGetValue("cam4", out var cam4))
                        return Path.GetFileName(cam4);
                    break;

                case DataType.Cam2:
                    if (group.CameraFiles.TryGetValue("cam2", out var cam2))
                        return Path.GetFileName(cam2);
                    if (group.CameraFiles.TryGetValue("cam5", out var cam5))
                        return Path.GetFileName(cam5);
                    break;

                case DataType.Cam3:
                    if (group.CameraFiles.TryGetValue("cam3", out var cam3))
                        return Path.GetFileName(cam3);
                    if (group.CameraFiles.TryGetValue("cam6", out var cam6))
                        return Path.GetFileName(cam6);
                    break;
            }

            return "unknown";
        }

        /// <summary>
        /// Determine the primary DataType for a FileGroup based on what files it contains
        /// </summary>
        private DataType DetermineDataTypeForGroup(FileGroup group)
        {
            // Priority order: NIR > Normal > Cameras
            if (group.HasNir) return DataType.NIR;
            if (!string.IsNullOrEmpty(group.NormalFolder)) return DataType.Normal;

            // Check cameras (mapped to Cam1-3 regardless of line)
            if (group.CameraFiles.ContainsKey("cam1") || group.CameraFiles.ContainsKey("cam4"))
                return DataType.Cam1;
            if (group.CameraFiles.ContainsKey("cam2") || group.CameraFiles.ContainsKey("cam5"))
                return DataType.Cam2;
            if (group.CameraFiles.ContainsKey("cam3") || group.CameraFiles.ContainsKey("cam6"))
                return DataType.Cam3;

            return DataType.Normal; // Default fallback
        }

        /// <summary>
        /// Check if a FileGroup has a specific DataType
        /// </summary>
        private bool HasDataType(FileGroup group, DataType dataType)
        {
            return dataType switch
            {
                DataType.Normal => !string.IsNullOrEmpty(group.NormalFolder),
                DataType.NIR => group.HasNir,
                DataType.Cam1 => group.CameraFiles.ContainsKey("cam1") || group.CameraFiles.ContainsKey("cam4"),
                DataType.Cam2 => group.CameraFiles.ContainsKey("cam2") || group.CameraFiles.ContainsKey("cam5"),
                DataType.Cam3 => group.CameraFiles.ContainsKey("cam3") || group.CameraFiles.ContainsKey("cam6"),
                _ => false
            };
        }

        /// <summary>
        /// Get priority (Order) for a DataType from configuration
        /// Lower Order = Higher Priority (arrives first)
        /// </summary>
        private int GetPriority(DataType dataType)
        {
            var item = _currentConfig?.DataSequenceSettings?.GetByType(dataType);
            return item?.Order ?? int.MaxValue; // Lowest priority if not configured
        }

        /// <summary>
        /// Get representative filename for a FileGroup for logging purposes
        /// </summary>
        private string GetRepresentativeFileName(FileGroup group)
        {
            // Priority: NIR > Normal > Camera
            if (!string.IsNullOrEmpty(group.NirFilePath))
            {
                return Path.GetFileName(group.NirFilePath);
            }
            
            if (!string.IsNullOrEmpty(group.MainImagePath))
            {
                // For Normal folders, use the folder name instead of stitched_original.png
                var folderPath = Path.GetDirectoryName(group.MainImagePath);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    return Path.GetFileName(folderPath);
                }
                return Path.GetFileName(group.MainImagePath);
            }
            
            // Fallback to first camera file
            var firstCam = group.CameraFiles.Values.FirstOrDefault();
            if (!string.IsNullOrEmpty(firstCam))
            {
                return Path.GetFileName(firstCam);
            }
            
            return "unknown";
        }
        
        /// <summary>
        /// Try to match pending NIR files to a group based on timestamp
        /// </summary>
        private void TryMatchPendingNirToGroup(FileGroup group)
        {
            lock (_lockObject)
            {
                if (_pendingNirFiles.Count == 0)
                    return;

                // Get NIR tolerance from data sequence settings
                var nirItem = _currentConfig?.DataSequenceSettings.Sequence
                    .FirstOrDefault(ds => ds.Type == DataType.NIR);
                    
                if (nirItem == null)
                    return;

                var minDelay = nirItem.MinDelaySeconds;
                var maxDelay = nirItem.MaxDelaySeconds;
                
                // Find matching NIR (closest timestamp within tolerance)
                (string FilePath, DateTime Timestamp, DateTime ReceivedAt)? matchedNir = null;
                double closestDiff = double.MaxValue;
                
                foreach (var pendingNir in _pendingNirFiles)
                {
                    if (group.LineNumber != 1 && group.LineNumber != 2)
                        continue;
                        
                    var timeDiff = Math.Abs((pendingNir.Timestamp - group.Timestamp).TotalSeconds);
                    
                    if (timeDiff >= minDelay && timeDiff <= maxDelay && timeDiff < closestDiff)
                    {
                        closestDiff = timeDiff;
                        matchedNir = pendingNir;
                    }
                }
                
                // If found a match, add NIR to group
                if (matchedNir.HasValue)
                {
                    var nirKey = Path.GetFileNameWithoutExtension(matchedNir.Value.FilePath);
                    var nirFilePath = matchedNir.Value.FilePath;
                    
                    group.NirFilePath = nirFilePath;
                    group.NirKey = nirKey;
                    group.HasNir = true;
                    
                    _logger.LogInformation("Matched pending NIR to group {GroupId}: {NirPath}, TimeDiff={Diff}s",
                        group.GroupId, matchedNir.Value.FilePath, closestDiff);
                    
                    _uiLog?.Invoke(LogSeverity.Debug, "NIR",
                        $"[{Path.GetFileName(matchedNir.Value.FilePath)}] 대기 중 → {group.GroupId}에 매칭 ({closestDiff:F0}초 차이)");
                    
                    _pendingNirFiles.Remove(matchedNir.Value);
                }
                
                // Clean up expired pending NIR files (timeout)
                var now = DateTime.UtcNow;
                var expired = _pendingNirFiles
                    .Where(p => (now - p.ReceivedAt).TotalSeconds > NirPendingTimeoutSeconds)
                    .ToList();
                    
                foreach (var expiredNir in expired)
                {
                    _logger.LogWarning("NIR file timed out in pending queue: {Path}", expiredNir.FilePath);
                    _pendingNirFiles.Remove(expiredNir);
                }
            }
        }
    }

    /// <summary>
    /// File type enumeration
    /// </summary>
    public enum FileType
    {
        Unknown,
        Nir,
        Normal,
        Camera
    }
}


