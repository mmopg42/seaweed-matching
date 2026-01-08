using ChronoView.Models;

namespace ChronoView.Core.Analytics;

/// <summary>
/// Service interface for calculating and monitoring file and matching statistics.
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// Event raised when file counts are updated.
    /// </summary>
    event EventHandler<FileCountStatistics>? FileCountsUpdated;

    /// <summary>
    /// Event raised when matching statistics are updated.
    /// </summary>
    event EventHandler<MatchingStatistics>? MatchingStatisticsUpdated;

    /// <summary>
    /// Starts monitoring with the specified configuration.
    /// </summary>
    Task StartMonitoringAsync(ApplicationConfiguration config);

    /// <summary>
    /// Stops monitoring.
    /// </summary>
    Task StopMonitoringAsync();
    
    /// <summary>
    /// Forces a reload of statistics using the provided configuration.
    /// </summary>
    Task ReloadStatsAsync(ChronoView.Models.ApplicationConfiguration config);

    /// <summary>
    /// Gets the current file count statistics.
    /// </summary>
    FileCountStatistics GetCurrentFileCounts();

    /// <summary>
    /// Gets the current matching statistics.
    /// </summary>
    MatchingStatistics GetCurrentMatchingStats();

    /// <summary>
    /// Gets the current file count statistics asynchronously.
    /// </summary>
    Task<FileCountStatistics> GetFileCountsAsync();

    /// <summary>
    /// Starts background file count monitoring.
    /// </summary>
    void StartFileCountMonitoring();

    /// <summary>
    /// Stops background file count monitoring.
    /// </summary>
    void StopFileCountMonitoring();

    /// <summary>
    /// Gets whether file count monitoring is currently active.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Calculates matching statistics for unified mode.
    /// </summary>
    MatchingStatistics CalculateUnifiedStats(IEnumerable<FileGroup> groups);

    /// <summary>
    /// Calculates matching statistics for separated mode (line 1 and line 2 independently).
    /// </summary>
    (MatchingStatistics line1, MatchingStatistics line2) CalculateSeparatedStats(
        IEnumerable<FileGroup> line1Groups,
        IEnumerable<FileGroup> line2Groups);

    /// <summary>
    /// Reports an abnormal condition detected in the system.
    /// </summary>
    void ReportAbnormalCondition(string condition, string details);

    /// <summary>
    /// Gets performance metrics for the system.
    /// </summary>
    Dictionary<string, object> GetPerformanceMetrics();
}
