using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ChronoView.Helpers;
using ChronoView.Models;

namespace ChronoView.Core.FileWatching;

public class FileWatcherService : IFileWatcher, IDisposable
{
    private readonly ILogger<FileWatcherService> _logger;
    private readonly ApplicationConfiguration _config;
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly HashSet<string> _knownFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lockObject = new();
    private bool _isWatching;
    private System.Threading.Timer? _pollingTimer;
    private IEnumerable<string> _paths = Enumerable.Empty<string>();
    private FileWatcherOptions? _options;

    public event EventHandler<FileSystemEventArgs>? FileChanged;

    public FileWatcherService(ILogger<FileWatcherService> logger, ApplicationConfiguration config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public Task StartWatchingAsync(IEnumerable<string> paths, FileWatcherOptions options)
    {
        if (_isWatching)
        {
            _logger.LogWarning("Watcher is already running.");
            return Task.CompletedTask;
        }

        _paths = paths;
        _options = options;
        _logger.LogInformation("Starting hybrid file watcher (Polling: {EnablePolling})", options.EnablePolling);

        try
        {
            // 1. Silent Baseline Scan (Don't fire events, just populate known files)
            PerformSilentScan(paths);

            // 2. Setup Watchers
            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    _logger.LogWarning("Directory does not exist or invalid: {Path}", path);
                    continue;
                }

                var watcher = new FileSystemWatcher(path)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime,
                    Filter = "*.*",
                    IncludeSubdirectories = true, // We need to see contents of Normal folders
                    EnableRaisingEvents = true
                };

                watcher.Created += (s, e) => ProcessWatcherEvent(e);
                watcher.Renamed += (s, e) => ProcessWatcherEvent(e);

                _watchers.Add(watcher);
                _logger.LogInformation("Started watching {Path}", path);
            }

            // 3. Setup Polling
            if (options.EnablePolling)
            {
                // Use injected config for polling interval
                var interval = _config.WorkflowSettings.PollingIntervalMs;
                _pollingTimer = new System.Threading.Timer(OnPollTick, null, interval, interval);
                _logger.LogInformation("Polling enabled every {Interval}ms", interval);
            }

            _isWatching = true;
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error starting file system watchers");
             StopWatchingAsync().Wait();
             throw;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts watching with pre-populated known files, skipping silent scan for optimization.
    /// </summary>
    public Task StartWatchingAsync(IEnumerable<string> paths, FileWatcherOptions options, IEnumerable<string> knownFiles)
    {
        if (_isWatching)
        {
            _logger.LogWarning("Watcher is already running.");
            return Task.CompletedTask;
        }

        _paths = paths;
        _options = options;
        _logger.LogInformation("Starting hybrid file watcher with injected files (Polling: {EnablePolling})", options.EnablePolling);

        try
        {
            // OPTIMIZATION: Use injected files instead of PerformSilentScan
            lock (_lockObject)
            {
                _knownFiles.Clear();
                if (knownFiles != null)
                {
                    foreach (var f in knownFiles) _knownFiles.Add(f);
                    _logger.LogInformation("Initialized with {Count} known files (Skipped silent scan)", _knownFiles.Count);
                }
                else
                {
                    _logger.LogWarning("knownFiles was null, falling back to silent scan");
                    PerformSilentScan(paths);
                }
            }

            // Setup Watchers
            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    _logger.LogWarning("Directory does not exist or invalid: {Path}", path);
                    continue;
                }

                var watcher = new FileSystemWatcher(path)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime,
                    Filter = "*.*",
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true
                };

                watcher.Created += (s, e) => ProcessWatcherEvent(e);
                watcher.Renamed += (s, e) => ProcessWatcherEvent(e);

                _watchers.Add(watcher);
                _logger.LogInformation("Started watching {Path}", path);
            }

            // Setup Polling
            if (options.EnablePolling)
            {
                // Use injected config for polling interval
                var interval = _config.WorkflowSettings.PollingIntervalMs;
                _pollingTimer = new System.Threading.Timer(OnPollTick, null, interval, interval);
                _logger.LogInformation("Polling enabled every {Interval}ms", interval);
            }

            _isWatching = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting file system watchers");
            StopWatchingAsync().Wait();
            throw;
        }

        return Task.CompletedTask;
    }

    private void PerformSilentScan(IEnumerable<string> paths)
    {
        lock (_lockObject)
        {
            _knownFiles.Clear();
            foreach (var path in paths)
            {
                if (!Directory.Exists(path)) continue;

                try
                {
                    // Scan files
                    var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files) _knownFiles.Add(file);

                    // Scan directories (Normal folders)
                    // Only add Normal folders to _knownFiles if they have stitched_original.png
                    // This allows polling to keep checking folders without images
                    var dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
                    foreach (var dir in dirs)
                    {
                        var dirName = Path.GetFileName(dir);
                        if (IsNormalFolderName(dirName))
                        {
                            var stitchedPath = Path.Combine(dir, "stitched_original.png");
                            if (File.Exists(stitchedPath))
                            {
                                _knownFiles.Add(dir);
                            }
                            // Skip adding to _knownFiles if image doesn't exist - polling will check again
                        }
                        else
                        {
                            // Non-Normal folders: add as usual
                            _knownFiles.Add(dir);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Silent scan error for {Path}: {Message}", path, ex.Message);
                }
            }
            _logger.LogDebug("Silent baseline scan completed: {Count} items found", _knownFiles.Count);
        }
    }

    private void ProcessWatcherEvent(FileSystemEventArgs e)
    {
        if (ShouldProcessEvent(e))
        {
            HandleEvent(e);
        }
    }

    private void OnPollTick(object? state)
    {
        if (!_isWatching) return;

        // 1. 모든 새 파일/폴더 수집
        var newItems = new List<(string Path, bool IsDirectory)>();

        foreach (var path in _paths)
        {
            if (!Directory.Exists(path)) continue;

            try
            {
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (ShouldProcessPath(file))
                        newItems.Add((file, false));
                }

                var dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
                foreach (var dir in dirs)
                {
                    if (ShouldProcessPath(dir))
                        newItems.Add((dir, true));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Poller tick error for {Path}: {Message}", path, ex.Message);
            }
        }

        // 2. 파일명 기준 정렬 (타임스탬프가 파일명에 포함됨)
        var sortedItems = newItems
            .Select(item => 
            {
                var timestamp = FileNamingHelper.ExtractTimestampAuto(item.Path);
                var priority = GetSensorPriority(item.Path);
                return (item, timestamp: timestamp ?? DateTime.MaxValue, priority);
            })
            .OrderBy(x => x.timestamp) // Sort by timestamp (ascending)
            .ThenBy(x => x.priority)   // Then by priority (ascending)
            .Select(x => x.item)
            .ToList();

        if (sortedItems.Any())
        {
             _logger.LogDebug("Polled items sort order: {Items}", 
                 string.Join(", ", sortedItems.Take(5).Select(x => Path.GetFileName(x.Path))));
        }

        // 3. 정렬된 순서로 이벤트 발생
        foreach (var (itemPath, isDir) in sortedItems)
        {
            var args = new FileSystemEventArgs(
                WatcherChangeTypes.Created,
                Path.GetDirectoryName(itemPath)!,
                Path.GetFileName(itemPath));
            HandleEvent(args);
        }
    }

    private int GetSensorPriority(string path)
    {
        var detectedType = FileNamingHelper.IdentifyDataType(path);
        
        if (detectedType == null)
            return int.MaxValue;
            
        var sequenceSettings = _options?.DataSequenceSettings;
        
        if (sequenceSettings == null)
        {
            // Fallback: NIR=1, Normal=2, Camera=3
            return detectedType switch
            {
                DataType.NIR => 1,
                DataType.Normal => 2,
                _ => 3
            };
        }
        
        return sequenceSettings.GetOrder(detectedType.Value);
    }

    private bool ShouldProcessPath(string path)
    {
        lock (_lockObject)
        {
            if (_knownFiles.Contains(path)) return false;
            
            // Special case for stitched_original.png
            if (Path.GetFileName(path).Equals("stitched_original.png", StringComparison.OrdinalIgnoreCase))
                return true;

            // Apply filters similar to ShouldProcessEvent
            var fileName = Path.GetFileName(path);
            
            // Filter files inside normal folders
            var parentDir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parentDir))
            {
                var parentName = Path.GetFileName(parentDir);
                if (IsNormalFolderName(parentName))
                    return false;
            }

            return true;
        }
    }

    private bool ShouldProcessEvent(FileSystemEventArgs e)
    {
        lock (_lockObject)
        {
            var path = e.FullPath;
            
            // Special Case: stitched_original.png conversion
            var fileName = Path.GetFileName(path);
            if (fileName.Equals("stitched_original.png", StringComparison.OrdinalIgnoreCase))
            {
                return true; 
            }

            // Deduplication
            if (_knownFiles.Contains(path)) return false;

            // Normal folder detection
            bool isFolder = !Path.HasExtension(path) || Directory.Exists(path);
            if (isFolder && IsNormalFolderName(fileName))
            {
                return true;
            }

            // Filter files inside normal folders (except stitched_original.png which we handled above)
            var parentDir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parentDir))
            {
                var parentName = Path.GetFileName(parentDir);
                if (IsNormalFolderName(parentName))
                {
                    _logger.LogDebug("Filtering file inside Normal folder: {Path}", path);
                    return false;
                }
            }

            return true;
        }
    }

    private bool IsNormalFolderName(string name, bool useSuffix = false, int? expectedLine = null)
    {
        return Helpers.NormalFolderHelper.IsValidNormalFolder(name, useSuffix, expectedLine);
    }

    private void HandleEvent(FileSystemEventArgs e)
    {
        var eventToFire = e;



        lock (_lockObject)
        {
            if (_knownFiles.Contains(eventToFire.FullPath)) return;
            
            // For Normal folders, only add to _knownFiles if stitched_original.png exists
            // This allows polling to keep checking folders without images until they appear
            var folderName = Path.GetFileName(eventToFire.FullPath);
            if (IsNormalFolderName(folderName) && Directory.Exists(eventToFire.FullPath))
            {
                var stitchedPath = Path.Combine(eventToFire.FullPath, "stitched_original.png");
                if (!File.Exists(stitchedPath))
                {
                    _logger.LogDebug("Normal folder without image, skipping _knownFiles addition: {Folder}", folderName);
                    // Still fire the event, but don't add to _knownFiles so polling can check again
                    FileChanged?.Invoke(this, eventToFire);
                    return;
                }
            }
            
            _knownFiles.Add(eventToFire.FullPath);
        }

        FileChanged?.Invoke(this, eventToFire);
    }

    public Task StopWatchingAsync()
    {
        if (!_isWatching) return Task.CompletedTask;

        _pollingTimer?.Dispose();
        _pollingTimer = null;

        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
        
        lock (_lockObject)
        {
            _knownFiles.Clear();
        }

        _isWatching = false;
        _logger.LogInformation("Stopped hybrid file watcher");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopWatchingAsync().Wait();
    }
}
