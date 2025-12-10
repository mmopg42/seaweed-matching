using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using ChronoView.Core.Configuration;
using ChronoView.Models;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.Analytics;

/// <summary>
/// Service for calculating and monitoring file and matching statistics.
/// Implements background file counting with thread-safe UI updates.
/// </summary>
public class StatisticsService : IStatisticsService, IDisposable
{
    private readonly IConfigurationManager _configManager;
    private readonly ILogger<StatisticsService> _logger;
    private readonly Dispatcher _dispatcher;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ConcurrentQueue<string> _abnormalConditions;
    private readonly Stopwatch _performanceStopwatch;
    private readonly SemaphoreSlim _debounceSemaphore;
    
    private Task? _monitoringTask;
    private FileCountStatistics? _lastFileCount;
    private MatchingStatistics? _lastMatchingStats;
    private bool _isMonitoring;
    private int _fileCountUpdateCount;
    private int _statsCalculationCount;
    private DateTime _lastFileCountUpdate;
    private ApplicationConfiguration? _currentConfig;

    public event EventHandler<FileCountStatistics>? FileCountsUpdated;
    public event EventHandler<MatchingStatistics>? MatchingStatisticsUpdated;

    public bool IsMonitoring => _isMonitoring;

    public StatisticsService(
        IConfigurationManager configManager,
        ILogger<StatisticsService> logger,
        Dispatcher dispatcher)
    {
        _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _cancellationTokenSource = new CancellationTokenSource();
        _abnormalConditions = new ConcurrentQueue<string>();
        _performanceStopwatch = Stopwatch.StartNew();
        _debounceSemaphore = new SemaphoreSlim(1, 1);
        _fileCountUpdateCount = 0;
        _statsCalculationCount = 0;
        _lastFileCountUpdate = DateTime.MinValue;
        _lastFileCount = new FileCountStatistics();
        _lastMatchingStats = null; // Start with null so first calculation always raises event
    }

    public async Task StartMonitoringAsync(ApplicationConfiguration config)
    {
        if (_isMonitoring)
        {
            _logger.LogWarning("Monitoring is already running");
            return;
        }

        _currentConfig = config ?? throw new ArgumentNullException(nameof(config));
        _isMonitoring = true;
        
        // Start background file counting worker
        _monitoringTask = Task.Run(async () => await MonitorFileCountsAsync(_cancellationTokenSource.Token));
        
        _logger.LogInformation("Statistics monitoring started");
        await Task.CompletedTask;
    }

    public async Task StopMonitoringAsync()
    {
        if (!_isMonitoring)
        {
            return;
        }

        _isMonitoring = false;
        _cancellationTokenSource.Cancel();
        
        try
        {
            if (_monitoringTask != null)
            {
                await _monitoringTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Monitoring task did not complete within timeout");
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }
        
        _logger.LogInformation("Statistics monitoring stopped");
    }

    public FileCountStatistics GetCurrentFileCounts()
    {
        return _lastFileCount ?? new FileCountStatistics();
    }

    public MatchingStatistics GetCurrentMatchingStats()
    {
        return _lastMatchingStats ?? new MatchingStatistics();
    }

    public void StartFileCountMonitoring()
    {
        if (_isMonitoring)
        {
            _logger.LogWarning("File count monitoring is already running");
            return;
        }

        _isMonitoring = true;
        _monitoringTask = Task.Run(async () => await MonitorFileCountsAsync(_cancellationTokenSource.Token));
        _logger.LogInformation("File count monitoring started");
    }

    public void StopFileCountMonitoring()
    {
        if (!_isMonitoring)
        {
            return;
        }

        _isMonitoring = false;
        _cancellationTokenSource.Cancel();
        
        try
        {
            _monitoringTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
        {
            // Expected when cancelling
        }
        
        _logger.LogInformation("File count monitoring stopped");
    }

    public async Task<FileCountStatistics> GetFileCountsAsync()
    {
        // Use current config if available, otherwise load from disk
        var config = _currentConfig ?? await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
        var stats = new FileCountStatistics();

        try
        {
            // Count NIR1 files (Line 1)
            if (!string.IsNullOrEmpty(config.MatchingSettings.Nir1Path))
            {
                stats.NirCount = await CountFilesInDirectoryAsync(config.MatchingSettings.Nir1Path);
            }

            // Count NIR2 files (Line 2)
            if (!string.IsNullOrEmpty(config.MatchingSettings.Nir2Path))
            {
                stats.Nir2Count = await CountFilesInDirectoryAsync(config.MatchingSettings.Nir2Path);
            }

            // Count Normal1 folders (Line 1) - Normal paths contain directories, not files
            if (!string.IsNullOrEmpty(config.MatchingSettings.Normal1Path))
            {
                stats.NormalCount = await CountDirectoriesInDirectoryAsync(config.MatchingSettings.Normal1Path);
            }

            // Count Normal2 folders (Line 2) - Normal paths contain directories, not files
            if (!string.IsNullOrEmpty(config.MatchingSettings.Normal2Path))
            {
                stats.Normal2Count = await CountDirectoriesInDirectoryAsync(config.MatchingSettings.Normal2Path);
            }

            // Count individual camera files
            if (!string.IsNullOrEmpty(config.MatchingSettings.Camera1Path))
            {
                stats.Cam1Count = await CountFilesInDirectoryAsync(config.MatchingSettings.Camera1Path);
            }
            
            if (!string.IsNullOrEmpty(config.MatchingSettings.Camera2Path))
            {
                stats.Cam2Count = await CountFilesInDirectoryAsync(config.MatchingSettings.Camera2Path);
            }
            
            if (!string.IsNullOrEmpty(config.MatchingSettings.Camera3Path))
            {
                stats.Cam3Count = await CountFilesInDirectoryAsync(config.MatchingSettings.Camera3Path);
            }
            
            if (!string.IsNullOrEmpty(config.MatchingSettings.Camera4Path))
            {
                stats.Cam4Count = await CountFilesInDirectoryAsync(config.MatchingSettings.Camera4Path);
            }
            
            if (!string.IsNullOrEmpty(config.MatchingSettings.Camera5Path))
            {
                stats.Cam5Count = await CountFilesInDirectoryAsync(config.MatchingSettings.Camera5Path);
            }
            
            if (!string.IsNullOrEmpty(config.MatchingSettings.Camera6Path))
            {
                stats.Cam6Count = await CountFilesInDirectoryAsync(config.MatchingSettings.Camera6Path);
            }

            stats.LastUpdated = DateTime.Now;
            _fileCountUpdateCount++;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file counts");
        }

        return stats;
    }

    public MatchingStatistics CalculateUnifiedStats(IEnumerable<FileGroup> groups)
    {
        _statsCalculationCount++;
        
        var groupList = groups.ToList();
        var stats = new MatchingStatistics
        {
            TotalGroups = groupList.Count,
            WithNir = groupList.Count(g => g.HasNir),
            WithoutNir = groupList.Count(g => !g.HasNir && g.Status != GroupStatus.Error),
            Failed = groupList.Count(g => g.Status == GroupStatus.Error)
        };

        _logger.LogDebug("Calculated unified stats: {Stats}", stats);
        
        // Update cached stats and raise event if changed
        UpdateMatchingStatistics(stats);
        
        return stats;
    }

    private void UpdateMatchingStatistics(MatchingStatistics newStats)
    {
        if (_lastMatchingStats == null || !_lastMatchingStats.Equals(newStats))
        {
            _lastMatchingStats = newStats;
            
            // Marshal to UI thread for event notification
            if (_dispatcher.CheckAccess())
            {
                // Already on dispatcher thread, invoke synchronously
                MatchingStatisticsUpdated?.Invoke(this, newStats);
            }
            else
            {
                // Not on dispatcher thread, marshal to it
                _dispatcher.InvokeAsync(() =>
                {
                    MatchingStatisticsUpdated?.Invoke(this, newStats);
                }, DispatcherPriority.Normal);
            }
        }
    }

    public (MatchingStatistics line1, MatchingStatistics line2) CalculateSeparatedStats(
        IEnumerable<FileGroup> line1Groups,
        IEnumerable<FileGroup> line2Groups)
    {
        _statsCalculationCount++;
        
        var line1Stats = CalculateStatsForLine(line1Groups, 1);
        var line2Stats = CalculateStatsForLine(line2Groups, 2);

        _logger.LogDebug("Calculated separated stats - Line1: {Line1Stats}, Line2: {Line2Stats}", 
            line1Stats, line2Stats);

        return (line1Stats, line2Stats);
    }

    public void ReportAbnormalCondition(string condition, string details)
    {
        var message = $"{condition}: {details}";
        _abnormalConditions.Enqueue(message);
        _logger.LogWarning("Abnormal condition reported: {Message}", message);

        // Keep only last 100 abnormal conditions
        while (_abnormalConditions.Count > 100)
        {
            _abnormalConditions.TryDequeue(out _);
        }
    }

    public Dictionary<string, object> GetPerformanceMetrics()
    {
        return new Dictionary<string, object>
        {
            { "UptimeSeconds", _performanceStopwatch.Elapsed.TotalSeconds },
            { "FileCountUpdates", _fileCountUpdateCount },
            { "StatsCalculations", _statsCalculationCount },
            { "AbnormalConditionsReported", _abnormalConditions.Count },
            { "IsMonitoring", _isMonitoring }
        };
    }

    private async Task MonitorFileCountsAsync(CancellationToken cancellationToken)
    {
        const int updateIntervalMs = 2000; // 2 seconds
        const int debounceMs = 500; // Debounce to 500ms

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var stats = await GetFileCountsAsync();

                // Only notify if counts have changed
                if (_lastFileCount == null || !_lastFileCount.Equals(stats))
                {
                    // Debounce: only update if 500ms has passed since last update
                    var timeSinceLastUpdate = DateTime.Now - _lastFileCountUpdate;
                    if (timeSinceLastUpdate.TotalMilliseconds >= debounceMs)
                    {
                        await _debounceSemaphore.WaitAsync(cancellationToken);
                        try
                        {
                            _lastFileCount = stats;
                            _lastFileCountUpdate = DateTime.Now;
                            
                            // Marshal to UI thread for event notification
                            if (_dispatcher.CheckAccess())
                            {
                                // Already on dispatcher thread, invoke synchronously
                                FileCountsUpdated?.Invoke(this, stats);
                            }
                            else
                            {
                                // Not on dispatcher thread, marshal to it
                                await _dispatcher.InvokeAsync(() =>
                                {
                                    FileCountsUpdated?.Invoke(this, stats);
                                }, DispatcherPriority.Normal);
                            }
                        }
                        finally
                        {
                            _debounceSemaphore.Release();
                        }
                    }
                }

                await Task.Delay(updateIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in file count monitoring loop");
                await Task.Delay(updateIntervalMs, cancellationToken);
            }
        }
    }

    private async Task<int> CountFilesInDirectoryAsync(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return 0;
        }

        try
        {
            // Use EnumerateFiles for efficient counting
            return await Task.Run(() => Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly).Count());
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied to directory: {Path}", path);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting files in directory: {Path}", path);
            return 0;
        }
    }

    private async Task<int> CountDirectoriesInDirectoryAsync(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return 0;
        }

        try
        {
            // Use EnumerateDirectories for efficient counting of subdirectories
            return await Task.Run(() => Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly).Count());
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied to directory: {Path}", path);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting directories in directory: {Path}", path);
            return 0;
        }
    }

    private MatchingStatistics CalculateStatsForLine(IEnumerable<FileGroup> groups, int lineNumber)
    {
        var groupList = groups.Where(g => g.LineNumber == lineNumber).ToList();
        
        return new MatchingStatistics
        {
            TotalGroups = groupList.Count,
            WithNir = groupList.Count(g => g.HasNir),
            WithoutNir = groupList.Count(g => !g.HasNir && g.Status != GroupStatus.Error),
            Failed = groupList.Count(g => g.Status == GroupStatus.Error)
        };
    }

    private void SetCameraCount(FileCountStatistics stats, int cameraNumber, int count)
    {
        switch (cameraNumber)
        {
            case 1: stats.Cam1Count = count; break;
            case 2: stats.Cam2Count = count; break;
            case 3: stats.Cam3Count = count; break;
            case 4: stats.Cam4Count = count; break;
            case 5: stats.Cam5Count = count; break;
            case 6: stats.Cam6Count = count; break;
        }
    }

    public void Dispose()
    {
        StopFileCountMonitoring();
        _cancellationTokenSource.Dispose();
        _debounceSemaphore.Dispose();
        _performanceStopwatch.Stop();
    }
}
