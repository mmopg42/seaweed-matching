using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChronoView.Core.FileMatching;
using ChronoView.Models;
using Microsoft.Extensions.Logging;
using ChronoView.Helpers;

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
        private readonly Dictionary<string, DateTime> _processedFiles = new();
        private readonly Dictionary<string, FileGroup> _activeGroups = new();
        private readonly object _lockObject = new();
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
            ILogger<MonitoringOrchestrator> logger)
        {
            _fileGroupMatcher = fileGroupMatcher ?? throw new ArgumentNullException(nameof(fileGroupMatcher));
            _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                    // Matching Algorithm Options
                    UseCamTimeMatching = config.MatchingSettings.UseCamTimeMatching,
                    CamMatchMinDiff = config.MatchingSettings.CamMatchMinDiff,
                    CamMatchMaxDiff = config.MatchingSettings.CamMatchMaxDiff,
                    NirMatchTimeDiff = config.MatchingSettings.NirMatchTimeDiff,
                    NirTimeWindowSeconds = config.MatchingSettings.NirTimeWindowSeconds,
                    CameraTimeWindowSeconds = config.MatchingSettings.CameraTimeWindowSeconds,

                    // Camera Subfolder Options
                    UseCameraSubfolderNormal = config.MatchingSettings.UseCameraSubfolderNormal,
                    UseCameraSubfolderNormal2 = config.MatchingSettings.UseCameraSubfolderNormal2,
                    UseFolderSuffix = config.MatchingSettings.UseFolderSuffix
                };

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
                
                // Add Line 1 paths
                if (!string.IsNullOrEmpty(config.MatchingSettings.Nir1Path) && Directory.Exists(config.MatchingSettings.Nir1Path))
                    watchPaths.Add(config.MatchingSettings.Nir1Path);
                if (!string.IsNullOrEmpty(config.MatchingSettings.Normal1Path) && Directory.Exists(config.MatchingSettings.Normal1Path))
                    watchPaths.Add(config.MatchingSettings.Normal1Path);
                
                // Add Line 2 paths
                if (!string.IsNullOrEmpty(config.MatchingSettings.Nir2Path) && Directory.Exists(config.MatchingSettings.Nir2Path))
                    watchPaths.Add(config.MatchingSettings.Nir2Path);
                if (!string.IsNullOrEmpty(config.MatchingSettings.Normal2Path) && Directory.Exists(config.MatchingSettings.Normal2Path))
                    watchPaths.Add(config.MatchingSettings.Normal2Path);

                for (int i = 1; i <= 6; i++)
                {
                    var camPath = config.MatchingSettings.GetCameraPath(i);
                    if (!string.IsNullOrEmpty(camPath) && Directory.Exists(camPath))
                        watchPaths.Add(camPath);
                }

                // Start file watcher
                if (watchPaths.Count > 0)
                {
                    _fileWatcher.FileChanged += OnFileChanged;
                    await _fileWatcher.StartWatchingAsync(watchPaths);
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

        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            if (!_isMonitoring)
            {
                _logger.LogWarning("Cannot refresh - monitoring is not active");
                return;
            }

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
                }

                // Perform new scan
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

            _logger.LogInformation("Starting initial scan");

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

                // Scan NIR directories for Line 1 (only .spc files)
                if (!string.IsNullOrEmpty(matchingConfig.Nir1Path) && Directory.Exists(matchingConfig.Nir1Path))
                {
                    // Only scan .spc files to avoid duplicate groups (.txt files are associated with .spc)
                    var nirFiles = Directory.GetFiles(matchingConfig.Nir1Path, "*.spc", SearchOption.AllDirectories);
                    var nirDict = new Dictionary<string, string>();
                    foreach (var file in nirFiles)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var key = Path.GetFileNameWithoutExtension(file);
                        nirDict[key] = file;
                        result.FilesScanned++;
                    }
                    unmatchedFiles.NirFiles["nir1"] = nirDict;
                    _logger.LogInformation("Scanned {Count} NIR1 .spc files", nirFiles.Length);
                }

                // Scan NIR directories for Line 2 (only .spc files)
                if (!string.IsNullOrEmpty(matchingConfig.Nir2Path) && Directory.Exists(matchingConfig.Nir2Path))
                {
                    // Only scan .spc files to avoid duplicate groups (.txt files are associated with .spc)
                    var nirFiles = Directory.GetFiles(matchingConfig.Nir2Path, "*.spc", SearchOption.AllDirectories);
                    var nirDict = new Dictionary<string, string>();
                    foreach (var file in nirFiles)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var key = Path.GetFileNameWithoutExtension(file);
                        nirDict[key] = file;
                        result.FilesScanned++;
                    }
                    unmatchedFiles.NirFiles["nir2"] = nirDict;
                    _logger.LogInformation("Scanned {Count} NIR2 .spc files", nirFiles.Length);
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
                        }
                    }

                    timestampedFiles.Add(new TimestampedFile
                    {
                        FileName = fileName,
                        AbsolutePath = file,
                        Timestamp = timestamp
                    });
                                result.FilesScanned++;
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

        public void ResetState()
        {
            lock (_lockObject)
            {
                _processedFiles.Clear();
                _logger.LogInformation("Orchestrator state reset");
            }
        }

        #region Task 1.2: CreateOrUpdateGroupAsync Implementation

        /// <summary>
        /// Create new group or update existing group based on file type and timestamp matching
        /// </summary>
        private async Task<FileGroup?> CreateOrUpdateGroupAsync(string filePath, FileType fileType)
        {
            try
            {
                // 1. Extract timestamp from file
                var timestamp = ExtractTimestamp(filePath, fileType);
                if (timestamp == null)
                {
                    _logger.LogWarning("Could not extract timestamp from {FileType} file: {Path}", fileType, filePath);
                    return null;
                }

                // 2. Find existing group with matching timestamp
                FileGroup? existingGroup = null;
                lock (_lockObject)
                {
                    existingGroup = _activeGroups.Values
                        .FirstOrDefault(g => IsMatchingTimestamp(g, timestamp.Value, fileType));
                }

                // 3-A. Update existing group
                if (existingGroup != null)
                {
                    _logger.LogInformation("Updating existing group {GroupId} with {FileType} file",
                        existingGroup.GroupId, fileType);
                    
                    UpdateGroupWithFile(existingGroup, filePath, fileType);
                    OnGroupUpdated(existingGroup);
                    return existingGroup;
                }

                // 3-B. Create new group
                _logger.LogInformation("Creating new group for {FileType} file: {Path}", fileType, filePath);
                
                var newGroup = await CreateNewGroupAsync(filePath, fileType, timestamp.Value);
                if (newGroup != null)
                {
                    // Add group to active groups immediately (< 200ms target)
                    lock (_lockObject)
                    {
                        _activeGroups[newGroup.GroupId] = newGroup;
                    }
                    OnGroupCreated(newGroup);
                    
                    _logger.LogInformation("New group {GroupId} created and added to GUI", newGroup.GroupId);
                    return newGroup;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating group for {FileType} file: {Path}", 
                    fileType, filePath);
                return null;
            }
        }

        /// <summary>
        /// Extract timestamp based on file type
        /// </summary>
        private DateTime? ExtractTimestamp(string filePath, FileType fileType)
        {
            return fileType switch
            {
                FileType.Nir => ExtractTimestampFromNirFile(filePath),
                FileType.Normal => ExtractTimestampFromFolderName(Path.GetDirectoryName(filePath)),
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
            
            // Pattern: 8 digits (date) + T + 6 digits (time)
            // Example: 20250926T103033
            var match = System.Text.RegularExpressions.Regex.Match(fileName, @"(\d{8}T\d{6})");
            if (match.Success)
            {
                if (DateTime.TryParseExact(
                    match.Groups[1].Value,
                    "yyyyMMddTHHmmss",
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
        /// Check if group timestamp matches within tolerance window
        /// </summary>
        private bool IsMatchingTimestamp(FileGroup group, DateTime timestamp, FileType fileType)
        {
            var tolerance = fileType == FileType.Nir
                ? TimeSpan.FromSeconds(_currentConfig?.MatchingSettings.NirTimeWindowSeconds ?? 300)
                : TimeSpan.FromSeconds(_currentConfig?.MatchingSettings.CameraTimeWindowSeconds ?? 60);

            return Math.Abs((group.Timestamp - timestamp).TotalSeconds) < tolerance.TotalSeconds;
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
                group.NirFilePath = null;
                group.HasNir = false;
            }
            else if (group.MainImagePath == filePath)
            {
                group.MainImagePath = null;
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

            _logger.LogInformation("Processing {Count} file system events", events.Count);

            var updatedGroups = new List<FileGroup>();

            try
            {
                // Group events by file path to handle duplicates
                var uniqueEvents = events
                    .GroupBy(e => e.FullPath)
                    .Select(g => g.Last())
                    .ToList();

                foreach (var eventArgs in uniqueEvents)
                {
                    // Check if we've already processed this file recently (debouncing)
                    if (ShouldSkipEvent(eventArgs))
                    {
                        continue;
                    }

                    // Mark file as processed
                    MarkFileAsProcessed(eventArgs.FullPath);

                    // Determine file type and update appropriate group
                    var fileType = DetermineFileType(eventArgs.FullPath);
                    if (fileType == FileType.Unknown)
                    {
                        continue;
                    }

                    _logger.LogInformation("Processing {ChangeType} event for {FileType}: {Path}",
                        eventArgs.ChangeType, fileType, eventArgs.FullPath);

                    // Handle different event types
                    switch (eventArgs.ChangeType)
                    {
                        case WatcherChangeTypes.Created:
                        case WatcherChangeTypes.Changed:
                            var group = await CreateOrUpdateGroupAsync(eventArgs.FullPath, fileType);
                            if (group != null)
                            {
                                updatedGroups.Add(group);
                            }
                            break;

                        case WatcherChangeTypes.Deleted:
                            await RemoveFromGroupAsync(eventArgs.FullPath);
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
                if (_processedFiles.TryGetValue(eventArgs.FullPath, out var lastProcessed))
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
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var directory = Path.GetDirectoryName(filePath);

            // Check if it's a NIR file
            if (extension == ".spc" || directory?.Contains("NIR", StringComparison.OrdinalIgnoreCase) == true)
            {
                return FileType.Nir;
            }

            // Check if it's a camera file
            if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp")
            {
                if (directory?.Contains("Cam", StringComparison.OrdinalIgnoreCase) == true ||
                    directory?.Contains("Camera", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return FileType.Camera;
                }

                if (directory?.Contains("Normal", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return FileType.Normal;
                }
            }

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
            try
            {
                await ProcessFileEventsAsync(new List<FileSystemEventArgs> { e });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file change event for {Path}", e.FullPath);
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


