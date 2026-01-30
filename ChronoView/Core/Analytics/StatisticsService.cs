using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using ChronoView.Core.Configuration;
using ChronoView.Helpers;
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
    private CancellationTokenSource _cancellationTokenSource; // Removed readonly to allow recreation
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
    private ChronoView.Models.ApplicationConfiguration? _currentConfig;

    public event EventHandler<FileCountStatistics>? FileCountsUpdated;
    public event EventHandler<MatchingStatistics>? MatchingStatisticsUpdated;

    public bool IsMonitoring => _isMonitoring;

    private bool _hasLoggedDispatcherInfo = false;

    /// <summary>
    /// UI Dispatcher로 FileCountsUpdated 이벤트를 발행하는 헬퍼 메서드.
    /// 모든 FileCountsUpdated 발행을 이 메서드로 통일하여 UI 스레드 마샬링을 보장합니다.
    /// </summary>
    private void PublishFileCounts(FileCountStatistics stats)
    {
        // Debug 로깅 (첫 1회만)
        if (!_hasLoggedDispatcherInfo)
        {
            _logger.LogDebug("PublishFileCounts: CheckAccess={CheckAccess}, ThreadId={ThreadId}", 
                _dispatcher.CheckAccess(), Environment.CurrentManagedThreadId);
            _hasLoggedDispatcherInfo = true;
        }

        if (_dispatcher.CheckAccess())
        {
            // Already on UI dispatcher thread, invoke synchronously
            FileCountsUpdated?.Invoke(this, stats);
        }
        else
        {
            // Not on UI dispatcher thread, marshal to it
            _dispatcher.BeginInvoke(() =>
            {
                FileCountsUpdated?.Invoke(this, stats);
            }, DispatcherPriority.Normal);
        }
    }

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

    public async Task StartMonitoringAsync(ChronoView.Models.ApplicationConfiguration config)
    {
        // Ensure we are truly stopped before starting
        if (_isMonitoring)
        {
            _logger.LogWarning("Monitoring is already running");
            return;
        }

        // Recreate CancellationTokenSource if it was cancelled or disposed
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            _logger.LogInformation("Recreated CancellationTokenSource for new monitoring session");
        }

        _currentConfig = config ?? throw new ArgumentNullException(nameof(config));
        _lastFileCount = null;
        _isMonitoring = true;
        
        // 첫 번째 업데이트를 UI Dispatcher로 마샬링하여 수행
        var initialStats = await GetFileCountsAsync();
        PublishFileCounts(initialStats);
        _lastFileCount = initialStats;
        _lastFileCountUpdate = DateTime.Now;
        
        // 이후 백그라운드 폴링 시작 (변경 감지만 수행)
        _monitoringTask = Task.Run(async () => await MonitorFileCountsAsync(_cancellationTokenSource.Token));
        
        _logger.LogInformation("Statistics monitoring started with initial counts: Nir={Nir}, Normal={Normal}, Cam1={Cam1}", 
            initialStats.NirCount, initialStats.NormalCount, initialStats.Cam1Count);

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

    public async Task ReloadStatsAsync(ChronoView.Models.ApplicationConfiguration config)
    {
        _currentConfig = config;
        var stats = await GetFileCountsAsync();
        
        // Always raise event on reload (UI Dispatcher로 마샬링)
        PublishFileCounts(stats);
        _lastFileCount = stats;
        _lastFileCountUpdate = DateTime.Now;
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
        var config = _currentConfig ?? await _configManager.LoadConfigurationAsync<ChronoView.Models.ApplicationConfiguration>();
        var stats = new FileCountStatistics();

        try
        {
            // Count NIR1 files (Line 1) - count .txt files only (each set = 1 .spc + 1 .txt)
            if (!string.IsNullOrEmpty(config.MatchingSettings.Nir1Path))
            {
                stats.NirCount = await CountNirFilesAsync(config.MatchingSettings.Nir1Path);
            }

            // Count NIR2 files (Line 2) - count .txt files only
            if (!string.IsNullOrEmpty(config.MatchingSettings.Nir2Path))
            {
                stats.Nir2Count = await CountNirFilesAsync(config.MatchingSettings.Nir2Path);
            }

            // Count Normal1 folders (Line 1)
            if (!string.IsNullOrEmpty(config.MatchingSettings.Normal1Path))
            {
                stats.NormalCount = await CountDirectoriesWithHelperAsync(
                    config.MatchingSettings.Normal1Path, 
                    config.MatchingSettings.UseFolderSuffix, 
                    expectedLine: 1);
            }

            // Count Normal2 folders (Line 2)
            if (!string.IsNullOrEmpty(config.MatchingSettings.Normal2Path))
            {
                stats.Normal2Count = await CountDirectoriesWithHelperAsync(
                    config.MatchingSettings.Normal2Path, 
                    config.MatchingSettings.UseFolderSuffix, 
                    expectedLine: 2);
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
        const int updateIntervalMs = 100; // 100ms for real-time updates
        const int debounceMs = 50; // Reduced debounce for responsiveness

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
                            
                            // UI Dispatcher로 마샬링하여 이벤트 발행 (헬퍼 사용)
                            PublishFileCounts(stats);
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
            return await Task.Run(() => Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories).Count());
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

    private async Task<int> CountDirectoriesInDirectoryAsync(string path, string? suffixFilter = null)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return 0;
        }

        try
        {
            // Use EnumerateDirectories for efficient counting of subdirectories
            return await Task.Run(() =>
            {
                var directories = Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly);
                
                // If suffix filter is provided, only count directories ending with that suffix
                if (!string.IsNullOrEmpty(suffixFilter))
                {
                    directories = directories.Where(dir => Path.GetFileName(dir).EndsWith(suffixFilter, StringComparison.Ordinal));
                }
                
                return directories.Count();
            });
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

    /// <summary>
    /// Counts directories using NormalFolderHelper for validation.
    /// </summary>
    private async Task<int> CountDirectoriesWithHelperAsync(string path, bool useSuffix, int expectedLine)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return 0;
        }

        try
        {
            return await Task.Run(() =>
            {
                var directories = Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly);
                return directories.Count(dir => 
                    NormalFolderHelper.IsValidNormalFolder(Path.GetFileName(dir), useSuffix, expectedLine));
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied to directory: {Path}", path);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting directories with helper in directory: {Path}", path);
            return 0;
        }
    }

    /// <summary>
    /// Counts NIR file sets by counting only .txt files.
    /// Each NIR set consists of one .spc file and one .txt file.
    /// We count .txt files because the program actively uses them.
    /// </summary>
    private async Task<int> CountNirFilesAsync(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return 0;
        }

        try
        {
            return await Task.Run(() =>
            {
                return Directory.EnumerateFiles(path, "*.txt", SearchOption.AllDirectories).Count();
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied to NIR directory: {Path}", path);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting NIR files in directory: {Path}", path);
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
