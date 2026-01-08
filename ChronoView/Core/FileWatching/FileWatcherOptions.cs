namespace ChronoView.Core.FileWatching;

public class FileWatcherOptions
{
    public bool EnablePolling { get; set; } = false;
    public int PollingIntervalMs { get; set; } = 1000;
    public bool EnableNetworkOptimization { get; set; } = true;
}
