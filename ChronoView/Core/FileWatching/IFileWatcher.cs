using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ChronoView.Core.FileWatching;

public interface IFileWatcher
{
    event EventHandler<FileSystemEventArgs> FileChanged;
    Task StartWatchingAsync(IEnumerable<string> paths, FileWatcherOptions options);
    Task StopWatchingAsync();
}
