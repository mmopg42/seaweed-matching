using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ChronoView.Models;

namespace ChronoView.Core.FileWatching
{
    /// <summary>
    /// Interface for coordinating monitoring workflows
    /// </summary>
    public interface IMonitoringOrchestrator
    {
        /// <summary>
        /// Start monitoring with the specified configuration
        /// </summary>
        Task StartAsync(ApplicationConfiguration config);

        /// <summary>
        /// Stop monitoring
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Refresh file groups by performing a full scan
        /// </summary>
        Task RefreshAsync();

        /// <summary>
        /// Perform initial scan of monitored directories
        /// </summary>
        Task<OrchestrationResult> PerformInitialScanAsync();

        /// <summary>
        /// Reset the orchestrator state
        /// </summary>
        void ResetState();

        /// <summary>
        /// Process file system events and update groups
        /// </summary>
        Task<List<FileGroup>> ProcessFileEventsAsync(List<FileSystemEventArgs> events);

        /// <summary>
        /// Event raised when a new file group is created
        /// </summary>
        event EventHandler<FileGroup> GroupCreated;

        /// <summary>
        /// Event raised when a file group is removed
        /// </summary>
        event EventHandler<string> GroupRemoved;

        /// <summary>
        /// Event raised when a file group is updated
        /// </summary>
        event EventHandler<FileGroup> GroupUpdated;

        /// <summary>
        /// Event raised when a monitoring error occurs
        /// </summary>
        event EventHandler<string> MonitoringError;

        /// <summary>
        /// Event raised when new file groups are created (batch)
        /// </summary>
        event EventHandler<FileGroupsCreatedEventArgs> FileGroupsCreated;

        /// <summary>
        /// Event raised when file groups are updated (batch)
        /// </summary>
        event EventHandler<FileGroupsUpdatedEventArgs> FileGroupsUpdated;
    }

    /// <summary>
    /// Result of orchestration operation
    /// </summary>
    public class OrchestrationResult
    {
        public bool Success { get; set; }
        public int FilesScanned { get; set; }
        public int GroupsCreated { get; set; }
        public List<string> Errors { get; set; } = new();
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Event args for file groups created
    /// </summary>
    public class FileGroupsCreatedEventArgs : EventArgs
    {
        public List<FileGroup> Groups { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Event args for file groups updated
    /// </summary>
    public class FileGroupsUpdatedEventArgs : EventArgs
    {
        public List<FileGroup> UpdatedGroups { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }
}
