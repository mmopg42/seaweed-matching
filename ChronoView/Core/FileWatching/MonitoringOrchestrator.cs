using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChronoView.Core.FileMatching;
using ChronoView.Models;
using Microsoft.Extensions.Logging;

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
                    NirPath = config.MatchingSettings.NirPath,
                    NormalPath = config.MatchingSettings.NormalPath,
                    Camera1Path = config.MatchingSettings.Camera1Path,
                    Camera2Path = config.MatchingSettings.Camera2Path,
                    Camera3Path = config.MatchingSettings.Camera3Path,
                    Camera4Path = config.MatchingSettings.Camera4Path,
                    Camera5Path = config.MatchingSettings.Camera5Path,
                    Camera6Path = config.MatchingSettings.Camera6Path
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
                if (!string.IsNullOrEmpty(config.MatchingSettings.NirPath) && Directory.Exists(config.MatchingSettings.NirPath))
                    watchPaths.Add(config.MatchingSettings.NirPath);
                
                if (!string.IsNullOrEmpty(config.MatchingSettings.NormalPath) && Directory.Exists(config.MatchingSettings.NormalPath))
                    watchPaths.Add(config.MatchingSettings.NormalPath);

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

        public async Task RefreshAsync()
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
                var result = await PerformInitialScanAsync();
                
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

        public async Task<OrchestrationResult> PerformInitialScanAsync()
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

                // Scan NIR directories
                if (!string.IsNullOrEmpty(matchingConfig.NirPath) && Directory.Exists(matchingConfig.NirPath))
                {
                    var nirFiles = Directory.GetFiles(matchingConfig.NirPath, "*.*", SearchOption.AllDirectories);
                    var nirDict = new Dictionary<string, string>();
                    foreach (var file in nirFiles)
                    {
                        var key = Path.GetFileNameWithoutExtension(file);
                        nirDict[key] = file;
                        result.FilesScanned++;
                    }
                    unmatchedFiles.NirFiles["nir"] = nirDict;
                    _logger.LogInformation("Scanned {Count} NIR files", nirFiles.Length);
                }

                // Scan normal directories
                if (!string.IsNullOrEmpty(matchingConfig.NormalPath) && Directory.Exists(matchingConfig.NormalPath))
                {
                    var normalFolders = Directory.GetDirectories(matchingConfig.NormalPath);
                    var normalDict = new Dictionary<string, string>();
                    foreach (var folder in normalFolders)
                    {
                        var key = Path.GetFileName(folder);
                        normalDict[key] = folder;
                        result.FilesScanned++;
                    }
                    unmatchedFiles.NormalFolders["normal"] = normalDict;
                    _logger.LogInformation("Scanned {Count} normal folders", normalFolders.Length);
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
                            try
                            {
                                var fileInfo = new FileInfo(file);
                                timestampedFiles.Add(new TimestampedFile
                                {
                                    FileName = Path.GetFileName(file),
                                    AbsolutePath = file,
                                    Timestamp = fileInfo.LastWriteTime
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
                            unmatchedFiles.CameraFiles[$"Cam{i}"] = timestampedFiles;
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

        public async Task<List<FileGroup>> ProcessFileEventsAsync(List<FileSystemEventArgs> events)
        {
            if (events == null || events.Count == 0)
            {
                return new List<FileGroup>();
            }

            _logger.LogDebug("Processing {Count} file system events", events.Count);

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

                    _logger.LogDebug("Processing {ChangeType} event for {FileType}: {Path}",
                        eventArgs.ChangeType, fileType, eventArgs.FullPath);

                    // Handle file deletion
                    if (eventArgs.ChangeType == WatcherChangeTypes.Deleted)
                    {
                        // Find groups that contain this file and update them
                        lock (_lockObject)
                        {
                            var affectedGroups = _activeGroups.Values
                                .Where(g => ContainsFile(g, eventArgs.FullPath))
                                .ToList();

                            foreach (var group in affectedGroups)
                            {
                                _activeGroups.Remove(group.GroupId);
                                OnGroupRemoved(group.GroupId);
                            }
                        }
                    }
                    // For now, trigger a re-scan to update groups for other events
                    // In a more sophisticated implementation, we would update specific groups
                    // based on the file path and type
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
