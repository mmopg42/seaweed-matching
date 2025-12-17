using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace ChronoView.Core.FileWatching
{
    /// <summary>
    /// File system monitoring service with buffering and health monitoring
    /// </summary>
    public class FileWatcherService : IFileWatcher, IDisposable
    {
        private readonly ILogger<FileWatcherService> _logger;
        private readonly List<FileSystemWatcher> _watchers = new();
        private readonly Channel<FileSystemEventArgs> _eventChannel;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly System.Threading.Timer _healthCheckTimer;
        private Task? _processingTask;
        private bool _isWatching;
        private WatcherHealthStatus _healthStatus = WatcherHealthStatus.Healthy;
        private DateTime _lastEventTime = DateTime.UtcNow;
        private int _eventCount;

        private readonly object _lockObject = new();
        private readonly HashSet<string> _knownFiles = new();
        private System.Threading.Timer? _pollingTimer;
        private FileWatcherOptions _options = new();
        private readonly List<string> _watchedPaths = new(); // To keep track of paths for polling

        public event EventHandler<FileSystemEventArgs>? FileChanged;
        public event EventHandler<WatcherHealthEventArgs>? HealthStatusChanged;

        public bool IsWatching
        {
            get
            {
                lock (_lockObject)
                {
                    return _isWatching;
                }
            }
            private set
            {
                lock (_lockObject)
                {
                    _isWatching = value;
                }
            }
        }

        public WatcherHealthStatus HealthStatus
        {
            get
            {
                lock (_lockObject)
                {
                    return _healthStatus;
                }
            }
            private set
            {
                lock (_lockObject)
                {
                    if (_healthStatus != value)
                    {
                        _healthStatus = value;
                        OnHealthStatusChanged(new WatcherHealthEventArgs
                        {
                            Status = value,
                            Message = $"Watcher health status changed to {value}",
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
            }
        }

        public FileWatcherService(ILogger<FileWatcherService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Create unbounded channel for event buffering
            _eventChannel = Channel.CreateUnbounded<FileSystemEventArgs>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            // Set up health check timer (30 seconds)
            _healthCheckTimer = new System.Threading.Timer(PerformHealthCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public async Task StartWatchingAsync(IEnumerable<string> paths, FileWatcherOptions? options = null)
        {
            if (IsWatching)
            {
                _logger.LogWarning("File watcher is already running");
                return;
            }

            _logger.LogInformation("Starting file watcher for {PathCount} paths", paths.Count());

            try
            {
                // Store options
                _options = options ?? new FileWatcherOptions();
                
                // Clear and store watched paths
                _watchedPaths.Clear();
                _watchedPaths.AddRange(paths);

                // Create a new cancellation token source for this start cycle
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();

                // SILENT BASELINE SCAN: Populate _knownFiles without raising events
                lock (_lockObject)
                {
                    _knownFiles.Clear();
                    foreach (var path in paths)
                    {
                        if (Directory.Exists(path))
                        {
                            try
                            {
                                // Scan existing files to establish baseline
                                var existingFiles = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);
                                foreach (var file in existingFiles)
                                {
                                    _knownFiles.Add(file);
                                }
                                _logger.LogInformation("Baseline scan for {Path}: Added {Count} files to known set", path, existingFiles.Count());
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to perform baseline scan for {Path}", path);
                            }
                        }
                    }
                }

                foreach (var path in paths)
                {
                    if (!Directory.Exists(path))
                    {
                        _logger.LogWarning("Path does not exist: {Path}", path);
                        continue;
                    }

                    var watcher = CreateWatcher(path);
                    _watchers.Add(watcher);
                    watcher.EnableRaisingEvents = true;
                    _logger.LogInformation("Started watching: {Path}", path);
                }

                if (_watchers.Count == 0)
                {
                    throw new InvalidOperationException("No valid paths to watch");
                }

                // Start Polling Timer if enabled
                if (_options.EnablePolling)
                {
                    _pollingTimer = new System.Threading.Timer(PollDirectories, null, _options.PollingIntervalMs, _options.PollingIntervalMs);
                    _logger.LogInformation("Polling started with interval {Interval}ms", _options.PollingIntervalMs);
                }

                IsWatching = true;
                HealthStatus = WatcherHealthStatus.Healthy;

                // Start the event processing task
                _processingTask = ProcessEventsAsync(_cancellationTokenSource.Token);

                _logger.LogInformation("File watcher started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start file watcher");
                await StopWatchingAsync();
                throw;
            }
        }

        public async Task StopWatchingAsync()
        {
            if (!IsWatching)
            {
                return;
            }

            _logger.LogInformation("Stopping file watcher");

            try
            {
                IsWatching = false;

                // Stop all watchers
                foreach (var watcher in _watchers)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }
                _watchers.Clear();

                // Stop polling timer
                _pollingTimer?.Dispose();
                _pollingTimer = null;
                
                lock (_lockObject)
                {
                    _knownFiles.Clear();
                    _watchedPaths.Clear();
                }

                // Signal cancellation and wait for processing to complete
                // The channel will complete naturally when the reader finishes
                _cancellationTokenSource?.Cancel();

                if (_processingTask != null)
                {
                    await _processingTask;
                }

                HealthStatus = WatcherHealthStatus.Unhealthy;
                _logger.LogInformation("File watcher stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping file watcher");
                throw;
            }
        }

        private FileSystemWatcher CreateWatcher(string path)
        {
            var watcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                IncludeSubdirectories = true,
                InternalBufferSize = 64 * 1024 // 64KB buffer
            };

            // Subscribe to all events
            watcher.Created += OnFileSystemEvent;
            watcher.Changed += OnFileSystemEvent;
            watcher.Deleted += OnFileSystemEvent;
            watcher.Renamed += OnFileSystemEvent;
            watcher.Error += OnWatcherError;

            return watcher;
        }

        private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Diagnostic logging: confirm FileSystemWatcher is firing
                _logger.LogDebug("FileSystemEvent detected: {ChangeType} - {Path}", e.ChangeType, e.FullPath);

                // CRITICAL FIX: Normal folder detection via stitched_original.png
                // When stitched_original.png is created, report parent folder as Normal folder instead
                var fileName = Path.GetFileName(e.FullPath);
                FileSystemEventArgs eventToBuffer = e;

                if (!string.IsNullOrEmpty(fileName) &&
                    fileName.Equals("stitched_original.png", StringComparison.OrdinalIgnoreCase) &&
                    e.ChangeType == WatcherChangeTypes.Created)
                {
                    var parentDir = Path.GetDirectoryName(e.FullPath);
                    if (!string.IsNullOrEmpty(parentDir))
                    {
                        var parentDirName = Path.GetFileName(parentDir);

                        if (!string.IsNullOrEmpty(parentDirName) &&
                            parentDirName.StartsWith("C", StringComparison.OrdinalIgnoreCase) &&
                            parentDirName.Contains('T'))
                        {
                            // Convert to parent folder event
                            var grandParentDir = Path.GetDirectoryName(parentDir) ?? parentDir;
                            eventToBuffer = new FileSystemEventArgs(
                                WatcherChangeTypes.Created,
                                grandParentDir,
                                parentDirName);

                            _logger.LogInformation("Detected stitched_original.png → Normal folder event: {Folder}", parentDir);
                        }
                    }
                }

                // Write to channel for buffering (non-blocking)
                if (_eventChannel.Writer.TryWrite(eventToBuffer))
                {
                    Interlocked.Increment(ref _eventCount);
                    _lastEventTime = DateTime.UtcNow;
                    _logger.LogDebug("Event buffered successfully");
                }
                else
                {
                    _logger.LogWarning("Failed to buffer file system event: {Path}", eventToBuffer.FullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling file system event: {Path}", e.FullPath);
            }
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            _logger.LogError(e.GetException(), "File system watcher error");
            HealthStatus = WatcherHealthStatus.Degraded;
        }

        private async Task ProcessEventsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Event processing task started");

            try
            {
                await foreach (var eventArgs in _eventChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    // Diagnostic logging: track event flow through channel
                    _logger.LogDebug("Reading event from channel: {ChangeType} - {Path}", eventArgs.ChangeType, eventArgs.FullPath);
                    
                    try
                    {
                        // Filter out temporary files and duplicates
                        if (ShouldProcessEvent(eventArgs))
                        {
                            _logger.LogDebug("Event passed ShouldProcessEvent filter, raising FileChanged");
                            // Raise event on background thread
                            await Task.Run(() => FileChanged?.Invoke(this, eventArgs), cancellationToken);
                        }
                        else
                        {
                            _logger.LogDebug("Event filtered out by ShouldProcessEvent: {Path}", eventArgs.FullPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing file system event: {Path}", eventArgs.FullPath);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Event processing cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event processing task failed");
                HealthStatus = WatcherHealthStatus.Unhealthy;
            }

            _logger.LogInformation("Event processing task completed");
        }

        private bool ShouldProcessEvent(FileSystemEventArgs e)
        {
            // Filter out temporary files
            var fileName = Path.GetFileName(e.FullPath);
            if (fileName.StartsWith("~") || fileName.StartsWith(".tmp") || fileName.EndsWith(".tmp"))
            {
                return false;
            }

            // Filter out system files
            if (fileName.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // CRITICAL FIX: Filter out files inside Normal folders
            // But PRESERVE stitched_original.png detection which was converted to folder event
            var pathFileName = Path.GetFileName(e.FullPath);

            // Check if this is a FOLDER event (no extension) that looks like a Normal folder
            bool isNormalFolderEvent = !Path.HasExtension(e.FullPath) &&
                                        !string.IsNullOrEmpty(pathFileName) &&
                                        pathFileName.StartsWith("C", StringComparison.OrdinalIgnoreCase) &&
                                        pathFileName.Contains('T');

            if (isNormalFolderEvent)
            {
                // This is a Normal folder event - ALLOW IT
                _logger.LogDebug("Detected Normal folder event: {Path}", e.FullPath);
                // Don't return, continue to deduplication logic
            }
            else if (!string.IsNullOrEmpty(pathFileName) && Path.HasExtension(e.FullPath))
            {
                // This is a FILE (has extension), check if it's inside a Normal folder
                var parentDirName = Path.GetFileName(Path.GetDirectoryName(e.FullPath));
                if (!string.IsNullOrEmpty(parentDirName) &&
                    parentDirName.StartsWith("C", StringComparison.OrdinalIgnoreCase) &&
                    parentDirName.Contains('T'))
                {
                    // This is a file inside a Normal folder
                    // SPECIAL CASE: stitched_original.png should have been converted to folder event
                    // If we see it here, it means conversion happened in OnFileSystemEvent
                    // and we should never see it here (it becomes a folder event)
                    _logger.LogDebug("Skipping file inside Normal folder: {Path}", e.FullPath);
                    return false;
                }
            }

            // De-duplication Logic
            // Changes and Deletes generally pass through. Created events need checks.
            lock (_lockObject)
            {
                if (e.ChangeType == WatcherChangeTypes.Created)
                {
                    if (_knownFiles.Contains(e.FullPath))
                    {
                        // Already known (duplicate event), skip
                        return false;
                    }
                    _knownFiles.Add(e.FullPath);
                }
                else if (e.ChangeType == WatcherChangeTypes.Deleted)
                {
                    _knownFiles.Remove(e.FullPath);
                }
                else if (e.ChangeType == WatcherChangeTypes.Renamed)
                {
                    var re = e as RenamedEventArgs;
                    if (re != null)
                    {
                        _knownFiles.Remove(re.OldFullPath);
                        _knownFiles.Add(re.FullPath);
                    }
                }
            }

            return true;
        }

        private void PollDirectories(object? state)
        {
            if (!IsWatching) return;

            int newFilesDetected = 0;
            _logger.LogDebug("Polling iteration started for {Count} paths", _watchedPaths.Count);

            try
            {
                foreach (var path in _watchedPaths)
                {
                    if (!Directory.Exists(path)) continue;

                    // Use EnumerateFiles for lower memory usage
                    // Note: EnumerateFiles can throw if permission denied, handle gracefully
                    try 
                    {
                        foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                        {
                            bool isNew = false;
                            lock (_lockObject)
                            {
                                if (!_knownFiles.Contains(file))
                                {
                                    // Found a new file!
                                    // NOTE: We don't add to _knownFiles here. 
                                    // We let ProcessEventsAsync -> ShouldProcessEvent handle the add.
                                    // This prevents race conditions where we add here but event is delayed.
                                    isNew = true; 
                                }
                            }

                            if (isNew)
                            {
                                // Create event and push to channel
                                var directory = Path.GetDirectoryName(file);
                                var fileName = Path.GetFileName(file);
                                if (directory != null)
                                {
                                    FileSystemEventArgs args;

                                    // CRITICAL: Apply same stitched_original.png → folder conversion as OnFileSystemEvent
                                    if (fileName.Equals("stitched_original.png", StringComparison.OrdinalIgnoreCase))
                                    {
                                        var parentDirName = Path.GetFileName(directory);
                                        if (!string.IsNullOrEmpty(parentDirName) &&
                                            parentDirName.StartsWith("C", StringComparison.OrdinalIgnoreCase) &&
                                            parentDirName.Contains('T'))
                                        {
                                            // Convert to parent folder event
                                            var grandParentDir = Path.GetDirectoryName(directory) ?? directory;
                                            args = new FileSystemEventArgs(
                                                WatcherChangeTypes.Created,
                                                grandParentDir,
                                                parentDirName);

                                            _logger.LogInformation("Polling: Detected stitched_original.png → Normal folder event: {Folder}", directory);
                                        }
                                        else
                                        {
                                            // Not a Normal folder pattern, use regular file event
                                            args = new FileSystemEventArgs(WatcherChangeTypes.Created, directory, fileName);
                                            _logger.LogDebug("Polling detected new file: {Path}", file);
                                        }
                                    }
                                    else
                                    {
                                        // Regular file event
                                        args = new FileSystemEventArgs(WatcherChangeTypes.Created, directory, fileName);
                                        _logger.LogDebug("Polling detected new file: {Path}", file);
                                    }

                                    newFilesDetected++;

                                    // Try write to channel (reusing existing mechanism)
                                    _eventChannel.Writer.TryWrite(args);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error polling directory: {Path}", path);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in polling task");
            }
            
            _logger.LogDebug("Polling iteration completed. New files detected: {Count}", newFilesDetected);
        }

        private void PerformHealthCheck(object? state)
        {
            try
            {
                if (!IsWatching)
                {
                    return;
                }

                var timeSinceLastEvent = DateTime.UtcNow - _lastEventTime;
                var eventCount = Interlocked.Exchange(ref _eventCount, 0);

                // Check if watchers are still alive
                var aliveWatchers = _watchers.Count(w => w.EnableRaisingEvents);
                if (aliveWatchers == 0)
                {
                    HealthStatus = WatcherHealthStatus.Unhealthy;
                    _logger.LogWarning("No active file watchers detected");
                }
                else if (aliveWatchers < _watchers.Count)
                {
                    HealthStatus = WatcherHealthStatus.Degraded;
                    _logger.LogWarning("Some file watchers are not active: {Active}/{Total}", aliveWatchers, _watchers.Count);
                }
                else
                {
                    HealthStatus = WatcherHealthStatus.Healthy;
                }

                _logger.LogDebug("Health check: Status={Status}, Events={EventCount}, TimeSinceLastEvent={TimeSinceLastEvent}",
                    HealthStatus, eventCount, timeSinceLastEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing health check");
            }
        }

        protected virtual void OnHealthStatusChanged(WatcherHealthEventArgs e)
        {
            HealthStatusChanged?.Invoke(this, e);
        }

        public void Dispose()
        {
            _healthCheckTimer?.Dispose();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            foreach (var watcher in _watchers)
            {
                watcher?.Dispose();
            }
            _watchers.Clear();
        }
    }
}
