using ChronoView.Models;

namespace ChronoView.Core.FileWatching;

public class FileWatcherOptions
{
    public bool EnablePolling { get; set; } = false;
    public bool EnableNetworkOptimization { get; set; } = true;
    public DataSequenceSettings? DataSequenceSettings { get; set; }
}
