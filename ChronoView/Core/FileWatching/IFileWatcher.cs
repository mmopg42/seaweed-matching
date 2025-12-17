using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ChronoView.Core.FileWatching
{
    /// <summary>
    /// Interface for file system monitoring service
    /// </summary>
    public interface IFileWatcher
    {
        /// <summary>
        /// Event raised when a file system change is detected
        /// </summary>
        event EventHandler<FileSystemEventArgs> FileChanged;

        /// <summary>
        /// Event raised when the watcher health status changes
        /// </summary>
        event EventHandler<WatcherHealthEventArgs> HealthStatusChanged;

        /// <summary>
        /// Start monitoring the specified paths
        /// </summary>
        /// <summary>
        /// Start monitoring the specified paths with options
        /// </summary>
        Task StartWatchingAsync(IEnumerable<string> paths, FileWatcherOptions? options = null);
        /// <summary>
        /// Stop monitoring all paths
        /// </summary>
        Task StopWatchingAsync();

        /// <summary>
        /// Gets whether the watcher is currently active
        /// </summary>
        bool IsWatching { get; }

        /// <summary>
        /// Gets the current health status of the watcher
        /// </summary>
        WatcherHealthStatus HealthStatus { get; }
    }

    /// <summary>
    /// Options for file watcher behavior
    /// </summary>
    public class FileWatcherOptions
    {
        /// <summary>
        /// Enable periodic polling for network drives
        /// </summary>
        public bool EnablePolling { get; set; } = false;

        /// <summary>
        /// Polling interval in milliseconds
        /// </summary>
        public int PollingIntervalMs { get; set; } = 5000;
    }

    /// <summary>
    /// Health status of the file watcher
    /// </summary>
    public enum WatcherHealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy
    }

    /// <summary>
    /// Event args for watcher health status changes
    /// </summary>
    public class WatcherHealthEventArgs : EventArgs
    {
        public WatcherHealthStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
