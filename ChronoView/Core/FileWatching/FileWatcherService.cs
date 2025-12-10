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
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Timer _healthCheckTimer;
        private Task? _processingTask;
        private bool _isWatching;
        private WatcherHealthStatus _healthStatus = WatcherHealthStatus.Healthy;
        private DateTime _lastEventTime = DateTime.UtcNow;
        private int _eventCount;
        private readonly object _lockObject = new();

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
            _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public async Task StartWatchingAsync(IEnumerable<string> paths)
        {
            if (IsWatching)
            {
                _logger.LogWarning("File watcher is already running");
                return;
            }

            _logger.LogInformation("Starting file watcher for {PathCount} paths", paths.Count());

            try
            {
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

                // Signal cancellation and wait for processing to complete
                _cancellationTokenSource.Cancel();
                _eventChannel.Writer.Complete();

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
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
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
                // Write to channel for buffering (non-blocking)
                if (_eventChannel.Writer.TryWrite(e))
                {
                    Interlocked.Increment(ref _eventCount);
                    _lastEventTime = DateTime.UtcNow;
                }
                else
                {
                    _logger.LogWarning("Failed to buffer file system event: {Path}", e.FullPath);
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
                    try
                    {
                        // Filter out temporary files and duplicates
                        if (ShouldProcessEvent(eventArgs))
                        {
                            // Raise event on background thread
                            await Task.Run(() => FileChanged?.Invoke(this, eventArgs), cancellationToken);
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

            return true;
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
