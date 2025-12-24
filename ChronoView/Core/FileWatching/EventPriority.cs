namespace ChronoView.Core.FileWatching
{
    /// <summary>
    /// Priority levels for file system events.
    /// Higher priority events are processed before lower priority ones.
    /// </summary>
    public enum EventPriority
    {
        /// <summary>
        /// Highest priority - Normal camera folders (race condition critical).
        /// These folders must be detected and timestamped immediately to avoid
        /// race conditions with external programs that may delete files.
        /// </summary>
        High = 0,
        
        /// <summary>
        /// Medium priority - NIR (Near-Infrared) files.
        /// Important data files that should be processed promptly.
        /// </summary>
        Medium = 1,
        
        /// <summary>
        /// Lowest priority - Camera image files (Cam1-6).
        /// These files can be processed after critical Normal folders and NIR files.
        /// </summary>
        Low = 2
    }
}
