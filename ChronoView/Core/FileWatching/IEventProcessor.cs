using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ChronoView.Core.FileWatching
{
    /// <summary>
    /// Interface for a service that handles parallel event processing and debouncing
    /// </summary>
    public interface IEventProcessor
    {
        /// <summary>
        /// Start the event processing workers
        /// </summary>
        void Start(int maxWorkers, Func<FileSystemEventArgs, int, CancellationToken, Task> eventHandler);

        /// <summary>
        /// Stop the event processing workers
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Enqueue a file system event for processing
        /// </summary>
        bool TryEnqueueEvent(FileSystemEventArgs e);

        /// <summary>
        /// Check if an event should be skipped (debouncing)
        /// </summary>
        bool ShouldSkipEvent(FileSystemEventArgs eventArgs, Func<string, FileType> fileTypeResolver);

        /// <summary>
        /// Mark a file as processed for debouncing
        /// </summary>
        void MarkFileAsProcessed(string filePath);

        /// <summary>
        /// Reset the processor state
        /// </summary>
        void Reset();
    }
}
