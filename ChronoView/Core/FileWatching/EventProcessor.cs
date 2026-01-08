using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ChronoView.Models;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.FileWatching
{
    /// <summary>
    /// Service that handles parallel event processing, debouncing, and queuing
    /// </summary>
    public class EventProcessor : IEventProcessor
    {
        private readonly ILogger<EventProcessor> _logger;
        private Channel<FileSystemEventArgs> _eventChannel = Channel.CreateUnbounded<FileSystemEventArgs>();
        private SemaphoreSlim? _parallelismLimiter;
        private List<Task> _workerTasks = new();
        private CancellationTokenSource? _workerCts;
        private Func<FileSystemEventArgs, int, CancellationToken, Task>? _handler;
        
        private readonly Dictionary<string, DateTime> _processedFiles = new();
        private readonly object _debounceLock = new();

        public EventProcessor(ILogger<EventProcessor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start(int maxWorkers, Func<FileSystemEventArgs, int, CancellationToken, Task> eventHandler)
        {
            if (_workerCts != null)
            {
                _logger.LogWarning("Event processor is already running");
                return;
            }

            _handler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));
            
            // Recreate channel if completed
            if (_eventChannel.Reader.Completion.IsCompleted)
            {
                _eventChannel = Channel.CreateUnbounded<FileSystemEventArgs>();
            }

            // 순서 보장을 위해 단일 워커로 강제
            const int forcedWorkers = 1;
            _parallelismLimiter = new SemaphoreSlim(forcedWorkers);
            _workerCts = new CancellationTokenSource();

            _workerTasks = new List<Task> { ProcessEventsWorkerAsync(0, _workerCts.Token) };

            _logger.LogInformation("Event processor started with {Count} worker (sequential mode for order guarantee)", forcedWorkers);
        }

        public async Task StopAsync()
        {
            if (_workerCts == null) return;

            _logger.LogInformation("Stopping event processor");
            _workerCts.Cancel();
            _eventChannel.Writer.Complete();

            try
            {
                await Task.WhenAll(_workerTasks);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error waiting for event workers to complete");
            }

            _workerTasks.Clear();
            _workerCts.Dispose();
            _workerCts = null;
            _parallelismLimiter?.Dispose();
            _parallelismLimiter = null;
            
            _logger.LogInformation("Event processor stopped");
        }

        public bool TryEnqueueEvent(FileSystemEventArgs e)
        {
            if (_eventChannel.Writer.TryWrite(e))
            {
                _logger.LogDebug("Event queued: {Path}", e.FullPath);
                return true;
            }
            
            _logger.LogWarning("Failed to queue event: {Path}", e.FullPath);
            return false;
        }

        public bool ShouldSkipEvent(FileSystemEventArgs eventArgs, Func<string, FileType> fileTypeResolver)
        {
            lock (_debounceLock)
            {
                var checkPath = eventArgs.FullPath;
                var fileType = fileTypeResolver(eventArgs.FullPath);
                
                if (fileType == FileType.Normal && !Directory.Exists(eventArgs.FullPath))
                {
                    var parentFolder = Path.GetDirectoryName(eventArgs.FullPath);
                    if (!string.IsNullOrEmpty(parentFolder))
                    {
                        checkPath = parentFolder;
                    }
                }
                
                if (_processedFiles.TryGetValue(checkPath, out var lastProcessed))
                {
                    if ((DateTime.UtcNow - lastProcessed).TotalSeconds < 2)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void MarkFileAsProcessed(string filePath)
        {
            lock (_debounceLock)
            {
                _processedFiles[filePath] = DateTime.UtcNow;

                // Cleanup (keep entries from last 5 minutes)
                var cutoff = DateTime.UtcNow.AddMinutes(-5);
                var oldEntries = _processedFiles.Where(kvp => kvp.Value < cutoff).Select(kvp => kvp.Key).ToList();
                foreach (var key in oldEntries)
                {
                    _processedFiles.Remove(key);
                }
            }
        }

        public void Reset()
        {
            lock (_debounceLock)
            {
                _processedFiles.Clear();
            }
            _logger.LogInformation("Event processor state reset");
        }

        private async Task ProcessEventsWorkerAsync(int workerId, CancellationToken ct)
        {
            try
            {
                await foreach (var eventArgs in _eventChannel.Reader.ReadAllAsync(ct))
                {
                    if (_parallelismLimiter == null || _handler == null) break;

                    await _parallelismLimiter.WaitAsync(ct);
                    try
                    {
                        await _handler(eventArgs, workerId, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Worker {Id} failed processing {Path}", workerId, eventArgs.FullPath);
                    }
                    finally
                    {
                        _parallelismLimiter.Release();
                    }
                }
            }
            catch (OperationCanceledException) { /* Normal shutdown */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker {Id} critical error", workerId);
            }
        }
    }
}
