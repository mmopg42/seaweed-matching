using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.FileWatching;

public class FileWatcherService : IFileWatcher, IDisposable
{
    private readonly ILogger<FileWatcherService> _logger;
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly HashSet<string> _knownFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lockObject = new();
    private bool _isWatching;
    private System.Threading.Timer? _pollingTimer;
    private IEnumerable<string> _paths = Enumerable.Empty<string>();
    private FileWatcherOptions? _options;

    public event EventHandler<FileSystemEventArgs>? FileChanged;

    public FileWatcherService(ILogger<FileWatcherService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                _pollingTimer = new System.Threading.Timer(OnPollTick, null, options.PollingIntervalMs, options.PollingIntervalMs);
                _logger.LogInformation("Polling enabled every {Interval}ms", options.PollingIntervalMs);
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
                    var dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
                    foreach (var dir in dirs) _knownFiles.Add(dir);
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
        var sortedItems = newItems.OrderBy(item => Path.GetFileName(item.Path)).ToList();

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

    private bool IsNormalFolderName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        // Basic pattern check (C...T...)
        return name.StartsWith("C", StringComparison.OrdinalIgnoreCase) && name.Contains("T");
    }

    private void HandleEvent(FileSystemEventArgs e)
    {
        var eventToFire = e;

        // Perform conversion if it's stitched_original.png
        if (Path.GetFileName(e.FullPath).Equals("stitched_original.png", StringComparison.OrdinalIgnoreCase))
        {
            var parentDir = Path.GetDirectoryName(e.FullPath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                var parentName = Path.GetFileName(parentDir);
                if (IsNormalFolderName(parentName))
                {
                    var grandParent = Path.GetDirectoryName(parentDir);
                    if (!string.IsNullOrEmpty(grandParent))
                    {
                        _logger.LogInformation("Converting stitched image event to folder event for: {Folder}", parentName);
                        eventToFire = new FileSystemEventArgs(WatcherChangeTypes.Created, grandParent, parentName);
                    }
                }
            }
        }

        lock (_lockObject)
        {
            if (_knownFiles.Contains(eventToFire.FullPath)) return;
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
