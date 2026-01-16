using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ChronoView.Core.FileWatching;

public interface IFileWatcher
{
    event EventHandler<FileSystemEventArgs> FileChanged;
    Task StartWatchingAsync(IEnumerable<string> paths, FileWatcherOptions options);
    
    /// <summary>
    /// Starts watching with pre-populated known files, skipping silent scan for optimization.
    /// </summary>
    Task StartWatchingAsync(IEnumerable<string> paths, FileWatcherOptions options, IEnumerable<string> knownFiles);
    
    Task StopWatchingAsync();
}
