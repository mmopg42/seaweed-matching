using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ChronoView.Helpers;

namespace ChronoView.Core.FileWatching
{
    /// <summary>
    /// File system monitoring service optimized for WSL/Network paths using high-frequency polling
    /// </summary>
    public class FileWatcherService : IFileWatcher, IDisposable
    {
        private readonly ILogger<FileWatcherService> _logger;
        private readonly FolderTimestampCache _folderTimestamps;
        private readonly List<FileSystemWatcher> _watchers = new();
        private readonly PriorityEventChannel _eventChannel;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly System.Threading.Timer _healthCheckTimer;
        private System.Timers.Timer? _pollingTimer;
        private Task? _processingTask;
        private bool _isWatching;
        private WatcherHealthStatus _healthStatus = WatcherHealthStatus.Healthy;
        private DateTime _lastEventTime = DateTime.UtcNow;
        private int _eventCount;

        private readonly object _lockObject = new();
        private readonly HashSet<string> _knownItems = new();
        private IEnumerable<string> _watchPaths = Enumerable.Empty<string>();
        private FileWatcherOptions _options = new();

        public event EventHandler<FileSystemEventArgs>? FileChanged;
        public event EventHandler<WatcherHealthEventArgs>? HealthStatusChanged;

        public bool IsWatching
        {
            get { lock (_lockObject) return _isWatching; }
            private set { lock (_lockObject) _isWatching = value; }
        }

        public WatcherHealthStatus HealthStatus
        {
            get { lock (_lockObject) return _healthStatus; }
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

        public FileWatcherService(
            ILogger<FileWatcherService> logger,
            FolderTimestampCache folderTimestamps)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _folderTimestamps = folderTimestamps ?? throw new ArgumentNullException(nameof(folderTimestamps));
            _eventChannel = new PriorityEventChannel(logger as ILogger<PriorityEventChannel>);
            _healthCheckTimer = new System.Threading.Timer(PerformHealthCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public async Task StartWatchingAsync(IEnumerable<string> paths, FileWatcherOptions? options = null)
        {
            if (IsWatching)
            {
                _logger.LogWarning("File watcher is already running");
                return;
            }

            _watchPaths = paths.ToList();
            _options = options ?? new FileWatcherOptions();
            _logger.LogInformation("Starting high-frequency polling (0.1s) for {PathCount} paths", _watchPaths.Count());

            try
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();

                // 1. Silent Baseline Scan
                lock (_lockObject)
                {
                    _knownItems.Clear();
                    foreach (var path in _watchPaths)
                    {
                        if (Directory.Exists(path))
                        {
                            try
                            {
                                // Add directories
                                foreach (var dir in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories))
                                    _knownItems.Add(dir);
                                
                                // Add files
                                foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                                    _knownItems.Add(file);
                            }
                            catch (Exception ex) { _logger.LogWarning(ex, "Baseline scan failed for {Path}", path); }
                        }
                    }
                    _logger.LogInformation("Baseline scan complete: {Count} items registered", _knownItems.Count);
                }

                // 2. Setup Polling Timer (Primary Detection)
                _pollingTimer = new System.Timers.Timer(100); // 100ms as requested
                _pollingTimer.Elapsed += async (s, e) => await PollFileSystemAsync();
                _pollingTimer.AutoReset = true;
                _pollingTimer.Start();

                // 3. Setup FileSystemWatcher (Secondary/Best Effort)
                foreach (var path in _watchPaths)
                {
                    if (Directory.Exists(path))
                    {
                        var watcher = CreateWatcher(path);
                        _watchers.Add(watcher);
                        watcher.EnableRaisingEvents = true;
                    }
                }

                IsWatching = true;
                HealthStatus = WatcherHealthStatus.Healthy;
                _processingTask = ProcessEventsAsync(_cancellationTokenSource.Token);

                _logger.LogInformation("File watcher (Polling + Events) started successfully");
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
            if (!IsWatching) return;

            _logger.LogInformation("Stopping file watcher");
            try
            {
                IsWatching = false;
                _pollingTimer?.Stop();
                _pollingTimer?.Dispose();
                _pollingTimer = null;

                foreach (var watcher in _watchers)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }
                _watchers.Clear();

                lock (_lockObject) { _knownItems.Clear(); }

                _cancellationTokenSource?.Cancel();
                if (_processingTask != null) await _processingTask;

                HealthStatus = WatcherHealthStatus.Unhealthy;
                _logger.LogInformation("File watcher stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping file watcher");
                throw;
            }
        }

        private async Task PollFileSystemAsync()
        {
            if (!IsWatching) return;

            foreach (var path in _watchPaths)
            {
                if (!Directory.Exists(path)) continue;

                try
                {
                    // Scan for new folders
                    foreach (var dir in Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly))
                    {
                        CheckAndNotifyItem(dir, true);
                    }

                    // Scan for new files
                    foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly))
                    {
                        CheckAndNotifyItem(file, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogTrace("Error during polling path {Path}: {Message}", path, ex.Message);
                }
            }
            await Task.CompletedTask;
        }

        private void CheckAndNotifyItem(string fullPath, bool isFolder)
        {
            bool isNew = false;
            lock (_lockObject)
            {
                if (!_knownItems.Contains(fullPath))
                {
                    _knownItems.Add(fullPath);
                    isNew = true;
                }
            }

            if (isNew)
            {
                var args = new FileSystemEventArgs(WatcherChangeTypes.Created, 
                    Path.GetDirectoryName(fullPath) ?? string.Empty, 
                    Path.GetFileName(fullPath));
                
                _logger.LogDebug("Polling detected new {Type}: {Path}", isFolder ? "Folder" : "File", fullPath);
                
                if (isFolder) HandleFolderCreatedEvent(args);
                else HandleFileCreatedEvent(args);
            }
        }

        private FileSystemWatcher CreateWatcher(string path)
        {
            var watcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                IncludeSubdirectories = true,
                InternalBufferSize = 64 * 1024
            };

            watcher.Created += (s, e) => { lock(_lockObject) { if(_knownItems.Contains(e.FullPath)) return; } OnFileSystemEvent(s, e); };
            watcher.Error += OnWatcherError;
            return watcher;
        }

        private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Created) return;
            
            bool isFolder = Directory.Exists(e.FullPath);
            lock (_lockObject) { _knownItems.Add(e.FullPath); }

            if (isFolder) HandleFolderCreatedEvent(e);
            else HandleFileCreatedEvent(e);
        }

        private void HandleFolderCreatedEvent(FileSystemEventArgs e)
        {
            string folderName = Path.GetFileName(e.FullPath);
            if (!FileNamingHelper.IsNormalFolder(folderName)) return;

            DateTime? timestamp = FileNamingHelper.ExtractTimestampFromFolderName(e.FullPath);
            if (timestamp.HasValue)
            {
                _folderTimestamps.Add(e.FullPath, timestamp.Value);
                _logger.LogInformation("Normal folder detected: {Name}, Timestamp: {Ts}", folderName, timestamp.Value);
            }

            EnqueueEvent(e, EventPriority.High);
        }

        private void HandleFileCreatedEvent(FileSystemEventArgs e)
        {
            string fileName = Path.GetFileName(e.FullPath);

            // Fast Capture for stitched_original.png
            if (FileNamingHelper.IsStitchedImage(e.FullPath))
            {
                _logger.LogInformation("⚡ FAST CAPTURE: Detected stitched_original.png: {Path}", e.FullPath);
                EnqueueEvent(e, EventPriority.High);
                return;
            }

            EventPriority priority = DetermineEventPriority(e);
            EnqueueEvent(e, priority);
        }

        private void EnqueueEvent(FileSystemEventArgs e, EventPriority priority)
        {
            if (_eventChannel.Writer.TryWrite(e, priority))
            {
                Interlocked.Increment(ref _eventCount);
                _lastEventTime = DateTime.UtcNow;
            }
        }

        private EventPriority DetermineEventPriority(FileSystemEventArgs e)
        {
            if (Directory.Exists(e.FullPath) && FileNamingHelper.IsNormalFolder(Path.GetFileName(e.FullPath)))
                return EventPriority.High;

            string extension = Path.GetExtension(e.FullPath).ToLowerInvariant();
            if (extension == ".csv" || extension == ".spc" || extension == ".txt") return EventPriority.Medium;
            if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp") return EventPriority.Low;

            return EventPriority.Medium;
        }

        private async Task ProcessEventsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var eventArgs in _eventChannel.ReadAllAsync(cancellationToken))
                {
                    if (ShouldProcessEvent(eventArgs))
                    {
                        await Task.Run(() => FileChanged?.Invoke(this, eventArgs), cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { _logger.LogError(ex, "Event processing failed"); HealthStatus = WatcherHealthStatus.Unhealthy; }
        }

        private bool ShouldProcessEvent(FileSystemEventArgs e)
        {
            var fileName = Path.GetFileName(e.FullPath);
            if (fileName.StartsWith("~") || fileName.StartsWith(".tmp") || fileName.EndsWith(".tmp")) return false;
            
            // Allow Normal folders and stitched images
            if (FileNamingHelper.IsNormalFolder(fileName)) return true;
            if (FileNamingHelper.IsStitchedImage(fileName)) return true;

            // Filter out files inside Normal folders (except stitched_original.png)
            var parentDir = Path.GetDirectoryName(e.FullPath);
            if (parentDir != null && FileNamingHelper.IsNormalFolder(Path.GetFileName(parentDir)))
            {
                return FileNamingHelper.IsStitchedImage(fileName);
            }

            return true;
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            var ex = e.GetException();
            if (ex is System.ComponentModel.Win32Exception winEx && winEx.NativeErrorCode == 1)
            {
                // ERROR_INVALID_FUNCTION (1): common on WSL/Network mounts that don't support ReadDirectoryChangesW
                // Since we have Polling enabled, we can safely ignore this watcher failure.
                var watcher = sender as FileSystemWatcher;
                _logger.LogWarning("FileSystemWatcher not supported for path '{Path}' (Incorrect Function). Disabling watcher and relying on Polling.", watcher?.Path);
                
                try 
                {
                    watcher?.Dispose();
                    lock (_watchers) { _watchers.Remove(watcher); }
                }
                catch { /* Ignore dispose errors */ }
                
                return;
            }

            _logger.LogError(ex, "File system watcher error");
            HealthStatus = WatcherHealthStatus.Degraded;
        }

        private void PerformHealthCheck(object? state)
        {
            if (!IsWatching) return;
            Interlocked.Exchange(ref _eventCount, 0);
        }

        protected virtual void OnHealthStatusChanged(WatcherHealthEventArgs e) => HealthStatusChanged?.Invoke(this, e);

        public void Dispose()
        {
            _healthCheckTimer?.Dispose();
            _pollingTimer?.Dispose();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            foreach (var watcher in _watchers) watcher?.Dispose();
        }
    }
}
