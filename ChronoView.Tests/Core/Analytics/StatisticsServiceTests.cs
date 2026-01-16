using System.Windows.Threading;
using ChronoView.Core.Analytics;
using ChronoView.Core.Configuration;
using ChronoView.Models;
using ChronoView.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChronoView.Tests.Core.Analytics;

/// <summary>
/// Unit tests for StatisticsService.
/// Tests statistical algorithms, aggregation, and reporting.
/// Requirements: 5.4
/// </summary>
public class StatisticsServiceTests : IDisposable
{
    private readonly Mock<IConfigurationManager> _mockConfigManager;
    private readonly Mock<ILogger<StatisticsService>> _mockLogger;
    private readonly Dispatcher _dispatcher;
    private readonly StatisticsService _service;

    public StatisticsServiceTests()
    {
        _mockConfigManager = new Mock<IConfigurationManager>();
        _mockLogger = new Mock<ILogger<StatisticsService>>();
        
        // Create a dispatcher for the current thread
        _dispatcher = Dispatcher.CurrentDispatcher;
        
        _service = new StatisticsService(_mockConfigManager.Object, _mockLogger.Object, _dispatcher);
    }

    [Fact]
    public void CalculateUnifiedStats_EmptyGroups_ReturnsZeroStats()
    {
        // Arrange
        var groups = new List<FileGroup>();

        // Act
        var stats = _service.CalculateUnifiedStats(groups);

        // Assert
        Assert.Equal(0, stats.TotalGroups);
        Assert.Equal(0, stats.WithNir);
        Assert.Equal(0, stats.WithoutNir);
        Assert.Equal(0, stats.Failed);
        Assert.Equal(0, StatisticsTestHelper.CalcMatchRate(stats.TotalGroups, stats.WithNir));
    }


    [Fact]
    public void CalculateUnifiedStats_AllGroupsWithNir_ReturnsCorrectStats()
    {
        // Arrange
        var groups = new List<FileGroup>
        {
            new FileGroup { GroupId = "1", HasNir = true, Status = GroupStatus.Complete },
            new FileGroup { GroupId = "2", HasNir = true, Status = GroupStatus.Complete },
            new FileGroup { GroupId = "3", HasNir = true, Status = GroupStatus.Complete }
        };

        // Act
        var stats = _service.CalculateUnifiedStats(groups);

        // Assert
        Assert.Equal(3, stats.TotalGroups);
        Assert.Equal(3, stats.WithNir);
        Assert.Equal(0, stats.WithoutNir);
        Assert.Equal(0, stats.Failed);
        Assert.Equal(100.0, StatisticsTestHelper.CalcMatchRate(stats.TotalGroups, stats.WithNir));
    }

    [Fact]
    public void CalculateUnifiedStats_MixedGroups_ReturnsCorrectStats()
    {
        // Arrange
        var groups = new List<FileGroup>
        {
            new FileGroup { GroupId = "1", HasNir = true, Status = GroupStatus.Complete },
            new FileGroup { GroupId = "2", HasNir = false, Status = GroupStatus.Pending },
            new FileGroup { GroupId = "3", HasNir = true, Status = GroupStatus.Complete },
            new FileGroup { GroupId = "4", HasNir = false, Status = GroupStatus.Error }
        };

        // Act
        var stats = _service.CalculateUnifiedStats(groups);

        // Assert
        Assert.Equal(4, stats.TotalGroups);
        Assert.Equal(2, stats.WithNir);
        Assert.Equal(1, stats.WithoutNir);
        Assert.Equal(1, stats.Failed);
        Assert.Equal(50.0, StatisticsTestHelper.CalcMatchRate(stats.TotalGroups, stats.WithNir));
    }

    [Fact]
    public void CalculateUnifiedStats_AllFailed_ReturnsCorrectStats()
    {
        // Arrange
        var groups = new List<FileGroup>
        {
            new FileGroup { GroupId = "1", HasNir = false, Status = GroupStatus.Error },
            new FileGroup { GroupId = "2", HasNir = false, Status = GroupStatus.Error }
        };

        // Act
        var stats = _service.CalculateUnifiedStats(groups);

        // Assert
        Assert.Equal(2, stats.TotalGroups);
        Assert.Equal(0, stats.WithNir);
        Assert.Equal(0, stats.WithoutNir);
        Assert.Equal(2, stats.Failed);
        Assert.Equal(0, StatisticsTestHelper.CalcMatchRate(stats.TotalGroups, stats.WithNir));
    }


    [Fact]
    public void CalculateSeparatedStats_EmptyGroups_ReturnsZeroStats()
    {
        // Arrange
        var line1Groups = new List<FileGroup>();
        var line2Groups = new List<FileGroup>();

        // Act
        var (line1Stats, line2Stats) = _service.CalculateSeparatedStats(line1Groups, line2Groups);

        // Assert
        Assert.Equal(0, line1Stats.TotalGroups);
        Assert.Equal(0, line2Stats.TotalGroups);
    }

    [Fact]
    public void CalculateSeparatedStats_MixedLines_ReturnsCorrectStats()
    {
        // Arrange
        var line1Groups = new List<FileGroup>
        {
            new FileGroup { GroupId = "1", LineNumber = 1, HasNir = true, Status = GroupStatus.Complete },
            new FileGroup { GroupId = "2", LineNumber = 1, HasNir = false, Status = GroupStatus.Pending }
        };
        
        var line2Groups = new List<FileGroup>
        {
            new FileGroup { GroupId = "3", LineNumber = 2, HasNir = true, Status = GroupStatus.Complete },
            new FileGroup { GroupId = "4", LineNumber = 2, HasNir = true, Status = GroupStatus.Complete },
            new FileGroup { GroupId = "5", LineNumber = 2, HasNir = false, Status = GroupStatus.Error }
        };

        // Act
        var (line1Stats, line2Stats) = _service.CalculateSeparatedStats(line1Groups, line2Groups);

        // Assert
        Assert.Equal(2, line1Stats.TotalGroups);
        Assert.Equal(1, line1Stats.WithNir);
        Assert.Equal(1, line1Stats.WithoutNir);
        Assert.Equal(0, line1Stats.Failed);
        Assert.Equal(50.0, TestHelpers.StatisticsTestHelper.CalcMatchRate(line1Stats.TotalGroups, line1Stats.WithNir));

        Assert.Equal(3, line2Stats.TotalGroups);
        Assert.Equal(2, line2Stats.WithNir);
        Assert.Equal(0, line2Stats.WithoutNir);
        Assert.Equal(1, line2Stats.Failed);
        Assert.Equal(66.666666666666671, TestHelpers.StatisticsTestHelper.CalcMatchRate(line2Stats.TotalGroups, line2Stats.WithNir), 0.0001);
    }

    [Fact]
    public void ReportAbnormalCondition_AddsToQueue()
    {
        // Arrange
        var condition = "HighMemoryUsage";
        var details = "Memory usage exceeded 80%";

        // Act
        _service.ReportAbnormalCondition(condition, details);

        // Assert
        var metrics = _service.GetPerformanceMetrics();
        Assert.True((int)metrics["AbnormalConditionsReported"] > 0);
    }


    [Fact]
    public void GetPerformanceMetrics_ReturnsValidMetrics()
    {
        // Act
        var metrics = _service.GetPerformanceMetrics();

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.ContainsKey("UptimeSeconds"));
        Assert.True(metrics.ContainsKey("FileCountUpdates"));
        Assert.True(metrics.ContainsKey("StatsCalculations"));
        Assert.True(metrics.ContainsKey("AbnormalConditionsReported"));
        Assert.True(metrics.ContainsKey("IsMonitoring"));
        
        Assert.True((double)metrics["UptimeSeconds"] >= 0);
        Assert.False((bool)metrics["IsMonitoring"]);
    }

    [Fact]
    public void StartFileCountMonitoring_SetsIsMonitoringTrue()
    {
        // Act
        _service.StartFileCountMonitoring();

        // Assert
        Assert.True(_service.IsMonitoring);

        // Cleanup
        _service.StopFileCountMonitoring();
    }

    [Fact]
    public void StopFileCountMonitoring_SetsIsMonitoringFalse()
    {
        // Arrange
        _service.StartFileCountMonitoring();

        // Act
        _service.StopFileCountMonitoring();

        // Assert
        Assert.False(_service.IsMonitoring);
    }

    [Fact]
    public void MatchingStatistics_MatchRate_CalculatesCorrectly()
    {
        // Arrange
        var stats = new MatchingStatistics
        {
            TotalGroups = 10,
            WithNir = 7,
            WithoutNir = 2,
            Failed = 1
        };

        // Act
        var matchRate = TestHelpers.StatisticsTestHelper.CalcMatchRate(stats.TotalGroups, stats.WithNir);

        // Assert
        Assert.Equal(70.0, matchRate);
    }

    [Fact]
    public void MatchingStatistics_MatchRate_ZeroGroups_ReturnsZero()
    {
        // Arrange
        var stats = new MatchingStatistics
        {
            TotalGroups = 0,
            WithNir = 0,
            WithoutNir = 0,
            Failed = 0
        };

        // Act
        var matchRate = TestHelpers.StatisticsTestHelper.CalcMatchRate(stats.TotalGroups, stats.WithNir);

        // Assert
        Assert.Equal(0, matchRate);
    }

    [Fact]
    public void FileCountStatistics_Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var stats1 = new FileCountStatistics
        {
            NirCount = 10,
            Cam1Count = 20,
            Cam2Count = 30
        };
        
        var stats2 = new FileCountStatistics
        {
            NirCount = 10,
            Cam1Count = 20,
            Cam2Count = 30
        };

        // Act & Assert
        Assert.True(stats1.Equals(stats2));
    }

    [Fact]
    public void FileCountStatistics_Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var stats1 = new FileCountStatistics { NirCount = 10 };
        var stats2 = new FileCountStatistics { NirCount = 20 };

        // Act & Assert
        Assert.False(stats1.Equals(stats2));
    }

    [Fact]
    public void CalculateUnifiedStats_LargeDataset_HandlesCorrectly()
    {
        // Arrange - Create a large dataset with 1000 groups
        var groups = new List<FileGroup>();
        for (int i = 0; i < 1000; i++)
        {
            groups.Add(new FileGroup
            {
                GroupId = $"group_{i}",
                HasNir = i % 3 == 0, // Every 3rd group has NIR (0, 3, 6, 9, ...)
                Status = i % 10 == 0 ? GroupStatus.Error : GroupStatus.Complete // Every 10th group is error (0, 10, 20, ...)
            });
        }

        // Act
        var stats = _service.CalculateUnifiedStats(groups);

        // Assert
        Assert.Equal(1000, stats.TotalGroups);
        Assert.Equal(334, stats.WithNir); // Groups where i % 3 == 0: 334 groups
        Assert.Equal(600, stats.WithoutNir); // Groups without NIR and not error: 1000 - 334 (with NIR) - 100 (error) + 34 (overlap) = 600
        Assert.Equal(100, stats.Failed); // Groups where i % 10 == 0: 100 groups
        Assert.Equal(33.4, TestHelpers.StatisticsTestHelper.CalcMatchRate(stats.TotalGroups, stats.WithNir), 0.1);
    }

    [Fact]
    public void CalculateSeparatedStats_CorrectlyFiltersLineNumbers()
    {
        // Arrange - Mix groups with different line numbers
        var allGroups = new List<FileGroup>
        {
            new FileGroup { GroupId = "1", LineNumber = 1, HasNir = true, Status = GroupStatus.Complete },
            new FileGroup { GroupId = "2", LineNumber = 2, HasNir = true, Status = GroupStatus.Complete },
            new FileGroup { GroupId = "3", LineNumber = 1, HasNir = false, Status = GroupStatus.Pending },
            new FileGroup { GroupId = "4", LineNumber = 2, HasNir = false, Status = GroupStatus.Error },
            new FileGroup { GroupId = "5", LineNumber = 1, HasNir = true, Status = GroupStatus.Complete }
        };

        // Act - Pass all groups to both parameters (service should filter by LineNumber)
        var (line1Stats, line2Stats) = _service.CalculateSeparatedStats(allGroups, allGroups);

        // Assert - Line 1 should have 3 groups
        Assert.Equal(3, line1Stats.TotalGroups);
        Assert.Equal(2, line1Stats.WithNir);
        Assert.Equal(1, line1Stats.WithoutNir);
        Assert.Equal(0, line1Stats.Failed);

        // Assert - Line 2 should have 2 groups
        Assert.Equal(2, line2Stats.TotalGroups);
        Assert.Equal(1, line2Stats.WithNir);
        Assert.Equal(0, line2Stats.WithoutNir);
        Assert.Equal(1, line2Stats.Failed);
    }

    [Fact]
    public void CalculateUnifiedStats_OnlyErrorGroups_CorrectlyCategorizes()
    {
        // Arrange
        var groups = new List<FileGroup>
        {
            new FileGroup { GroupId = "1", HasNir = true, Status = GroupStatus.Error },
            new FileGroup { GroupId = "2", HasNir = false, Status = GroupStatus.Error },
            new FileGroup { GroupId = "3", HasNir = true, Status = GroupStatus.Error }
        };

        // Act
        var stats = _service.CalculateUnifiedStats(groups);

        // Assert - All should be counted as failed, not as WithNir or WithoutNir
        Assert.Equal(3, stats.TotalGroups);
        Assert.Equal(2, stats.WithNir); // HasNir is still counted
        Assert.Equal(0, stats.WithoutNir); // Error groups are not counted as WithoutNir
        Assert.Equal(3, stats.Failed);
    }

    [Fact]
    public void CalculateUnifiedStats_VariousStatuses_CorrectlyCategorizes()
    {
        // Arrange
        var groups = new List<FileGroup>
        {
            new FileGroup { GroupId = "1", HasNir = true, Status = GroupStatus.Complete },
            new FileGroup { GroupId = "2", HasNir = false, Status = GroupStatus.Pending },
            new FileGroup { GroupId = "3", HasNir = false, Status = GroupStatus.Processing },
            new FileGroup { GroupId = "4", HasNir = true, Status = GroupStatus.Abnormal },
            new FileGroup { GroupId = "5", HasNir = false, Status = GroupStatus.Error }
        };

        // Act
        var stats = _service.CalculateUnifiedStats(groups);

        // Assert
        Assert.Equal(5, stats.TotalGroups);
        Assert.Equal(2, stats.WithNir);
        Assert.Equal(2, stats.WithoutNir); // Pending and Processing, not Error
        Assert.Equal(1, stats.Failed);
        Assert.Equal(40.0, TestHelpers.StatisticsTestHelper.CalcMatchRate(stats.TotalGroups, stats.WithNir));
    }

    [Fact]
    public void ReportAbnormalCondition_MultipleConditions_MaintainsQueue()
    {
        // Arrange & Act - Report multiple conditions
        for (int i = 0; i < 10; i++)
        {
            _service.ReportAbnormalCondition($"Condition{i}", $"Details for condition {i}");
        }

        // Assert
        var metrics = _service.GetPerformanceMetrics();
        Assert.Equal(10, (int)metrics["AbnormalConditionsReported"]);
    }

    [Fact]
    public void ReportAbnormalCondition_ExceedsMaxQueue_KeepsLast100()
    {
        // Arrange & Act - Report more than 100 conditions
        for (int i = 0; i < 150; i++)
        {
            _service.ReportAbnormalCondition($"Condition{i}", $"Details {i}");
        }

        // Assert - Should only keep last 100
        var metrics = _service.GetPerformanceMetrics();
        Assert.Equal(100, (int)metrics["AbnormalConditionsReported"]);
    }

    [Fact]
    public void GetPerformanceMetrics_AfterCalculations_TracksCount()
    {
        // Arrange
        var groups = new List<FileGroup>
        {
            new FileGroup { GroupId = "1", HasNir = true, Status = GroupStatus.Complete }
        };

        // Act - Perform multiple calculations
        _service.CalculateUnifiedStats(groups);
        _service.CalculateUnifiedStats(groups);
        _service.CalculateSeparatedStats(groups, groups);

        // Assert
        var metrics = _service.GetPerformanceMetrics();
        Assert.Equal(3, (int)metrics["StatsCalculations"]);
    }

    [Fact]
    public void MatchingStatistics_MatchRate_VariousScenarios()
    {
        // Test 100% match rate
        var stats1 = new MatchingStatistics(10, 10, 0, 0);
        Assert.Equal(100.0, TestHelpers.StatisticsTestHelper.CalcMatchRate(stats1.TotalGroups, stats1.WithNir));

        // Test 0% match rate
        var stats2 = new MatchingStatistics(10, 0, 10, 0);
        Assert.Equal(0.0, TestHelpers.StatisticsTestHelper.CalcMatchRate(stats2.TotalGroups, stats2.WithNir));

        // Test 50% match rate
        var stats3 = new MatchingStatistics(10, 5, 5, 0);
        Assert.Equal(50.0, TestHelpers.StatisticsTestHelper.CalcMatchRate(stats3.TotalGroups, stats3.WithNir));

        // Test fractional match rate
        var stats4 = new MatchingStatistics(3, 1, 2, 0);
        Assert.Equal(33.333333333333336, TestHelpers.StatisticsTestHelper.CalcMatchRate(stats4.TotalGroups, stats4.WithNir), 0.0001);
    }

    [Fact]
    public void MatchingStatistics_Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var stats1 = new MatchingStatistics(10, 5, 3, 2);
        var stats2 = new MatchingStatistics(10, 5, 3, 2);

        // Act & Assert
        Assert.True(stats1.Equals(stats2));
        Assert.Equal(stats1.GetHashCode(), stats2.GetHashCode());
    }

    [Fact]
    public void MatchingStatistics_Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var stats1 = new MatchingStatistics(10, 5, 3, 2);
        var stats2 = new MatchingStatistics(10, 6, 3, 1);

        // Act & Assert
        Assert.False(stats1.Equals(stats2));
    }

    [Fact]
    public void MatchingStatistics_ToString_FormatsCorrectly()
    {
        // Arrange
        var stats = new MatchingStatistics(10, 7, 2, 1);

        // Act
        var result = stats.ToString();

        // Assert
        Assert.Contains("Total: 10", result);
        Assert.Contains("With NIR: 7", result);
        Assert.Contains("Without NIR: 2", result);
        Assert.Contains("Failed: 1", result);
        // 현재 메인 ToString()은 MatchRate를 포함하지 않는다. (테스트는 포맷 안정성만 확인)
    }

    [Fact]
    public void FileCountStatistics_GetHashCode_ConsistentForEqualObjects()
    {
        // Arrange
        var stats1 = new FileCountStatistics
        {
            NirCount = 10,
            Cam1Count = 20,
            Cam2Count = 30,
            Cam3Count = 40
        };
        
        var stats2 = new FileCountStatistics
        {
            NirCount = 10,
            Cam1Count = 20,
            Cam2Count = 30,
            Cam3Count = 40
        };

        // Act & Assert
        Assert.Equal(stats1.GetHashCode(), stats2.GetHashCode());
    }

    [Fact]
    public void FileCountStatistics_AllCameraFields_SetCorrectly()
    {
        // Arrange & Act
        var stats = new FileCountStatistics
        {
            NirCount = 1,
            Nir2Count = 2,
            NormalCount = 3,
            Normal2Count = 4,
            Cam1Count = 5,
            Cam2Count = 6,
            Cam3Count = 7,
            Cam4Count = 8,
            Cam5Count = 9,
            Cam6Count = 10
        };

        // Assert - Verify all fields are set correctly
        Assert.Equal(1, stats.NirCount);
        Assert.Equal(2, stats.Nir2Count);
        Assert.Equal(3, stats.NormalCount);
        Assert.Equal(4, stats.Normal2Count);
        Assert.Equal(5, stats.Cam1Count);
        Assert.Equal(6, stats.Cam2Count);
        Assert.Equal(7, stats.Cam3Count);
        Assert.Equal(8, stats.Cam4Count);
        Assert.Equal(9, stats.Cam5Count);
        Assert.Equal(10, stats.Cam6Count);
    }

    [Fact]
    public void CalculateSeparatedStats_BothLinesEmpty_ReturnsZeroStats()
    {
        // Arrange
        var line1Groups = new List<FileGroup>();
        var line2Groups = new List<FileGroup>();

        // Act
        var (line1Stats, line2Stats) = _service.CalculateSeparatedStats(line1Groups, line2Groups);

        // Assert
        Assert.Equal(0, line1Stats.TotalGroups);
        Assert.Equal(0, line1Stats.WithNir);
        Assert.Equal(0, line1Stats.WithoutNir);
        Assert.Equal(0, line1Stats.Failed);
        
        Assert.Equal(0, line2Stats.TotalGroups);
        Assert.Equal(0, line2Stats.WithNir);
        Assert.Equal(0, line2Stats.WithoutNir);
        Assert.Equal(0, line2Stats.Failed);
    }

    [Fact]
    public void CalculateSeparatedStats_OnlyLine1HasData_Line2IsZero()
    {
        // Arrange
        var line1Groups = new List<FileGroup>
        {
            new FileGroup { GroupId = "1", LineNumber = 1, HasNir = true, Status = GroupStatus.Complete },
            new FileGroup { GroupId = "2", LineNumber = 1, HasNir = false, Status = GroupStatus.Pending }
        };
        var line2Groups = new List<FileGroup>();

        // Act
        var (line1Stats, line2Stats) = _service.CalculateSeparatedStats(line1Groups, line2Groups);

        // Assert
        Assert.Equal(2, line1Stats.TotalGroups);
        Assert.Equal(1, line1Stats.WithNir);
        Assert.Equal(1, line1Stats.WithoutNir);
        
        Assert.Equal(0, line2Stats.TotalGroups);
    }

    [Fact]
    public void StartFileCountMonitoring_CalledTwice_LogsWarning()
    {
        // Arrange
        _service.StartFileCountMonitoring();

        // Act
        _service.StartFileCountMonitoring();

        // Assert
        Assert.True(_service.IsMonitoring);
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already running")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Cleanup
        _service.StopFileCountMonitoring();
    }

    [Fact]
    public void StopFileCountMonitoring_WhenNotMonitoring_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _service.StopFileCountMonitoring();
        Assert.False(_service.IsMonitoring);
    }

    [Fact]
    public void GetPerformanceMetrics_ContainsAllRequiredKeys()
    {
        // Act
        var metrics = _service.GetPerformanceMetrics();

        // Assert - Verify all required keys exist
        Assert.True(metrics.ContainsKey("UptimeSeconds"));
        Assert.True(metrics.ContainsKey("FileCountUpdates"));
        Assert.True(metrics.ContainsKey("StatsCalculations"));
        Assert.True(metrics.ContainsKey("AbnormalConditionsReported"));
        Assert.True(metrics.ContainsKey("IsMonitoring"));
        
        // Verify types
        Assert.IsType<double>(metrics["UptimeSeconds"]);
        Assert.IsType<int>(metrics["FileCountUpdates"]);
        Assert.IsType<int>(metrics["StatsCalculations"]);
        Assert.IsType<int>(metrics["AbnormalConditionsReported"]);
        Assert.IsType<bool>(metrics["IsMonitoring"]);
    }

    [Fact]
    public async Task StartMonitoringAsync_StartsMonitoring()
    {
        // Arrange
        var config = new ApplicationConfiguration();

        // Act
        await _service.StartMonitoringAsync(config);

        // Assert
        Assert.True(_service.IsMonitoring);

        // Cleanup
        await _service.StopMonitoringAsync();
    }

    [Fact]
    public async Task StartMonitoringAsync_NullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.StartMonitoringAsync(null!));
    }

    [Fact]
    public async Task StartMonitoringAsync_CalledTwice_LogsWarning()
    {
        // Arrange
        var config = new ApplicationConfiguration();
        await _service.StartMonitoringAsync(config);

        // Act
        await _service.StartMonitoringAsync(config);

        // Assert
        Assert.True(_service.IsMonitoring);
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already running")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Cleanup
        await _service.StopMonitoringAsync();
    }

    [Fact]
    public async Task StopMonitoringAsync_StopsMonitoring()
    {
        // Arrange
        var config = new ApplicationConfiguration();
        await _service.StartMonitoringAsync(config);

        // Act
        await _service.StopMonitoringAsync();

        // Assert
        Assert.False(_service.IsMonitoring);
    }

    [Fact]
    public async Task StopMonitoringAsync_WhenNotMonitoring_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _service.StopMonitoringAsync();
        Assert.False(_service.IsMonitoring);
    }

    [Fact]
    public void GetCurrentFileCounts_ReturnsLastFileCount()
    {
        // Act
        var stats = _service.GetCurrentFileCounts();

        // Assert
        Assert.NotNull(stats);
        Assert.IsType<FileCountStatistics>(stats);
    }

    [Fact]
    public void GetCurrentMatchingStats_ReturnsLastMatchingStats()
    {
        // Act
        var stats = _service.GetCurrentMatchingStats();

        // Assert
        Assert.NotNull(stats);
        Assert.IsType<MatchingStatistics>(stats);
    }

    [Fact]
    public async Task CalculateUnifiedStats_RaisesMatchingStatisticsUpdatedEvent()
    {
        // Arrange
        var groups = new List<FileGroup>
        {
            new FileGroup { GroupId = "1", HasNir = true, Status = GroupStatus.Complete },
            new FileGroup { GroupId = "2", HasNir = false, Status = GroupStatus.Pending }
        };
        
        var tcs = new TaskCompletionSource<MatchingStatistics>();
        _service.MatchingStatisticsUpdated += (sender, stats) => tcs.TrySetResult(stats);

        // Act
        var result = _service.CalculateUnifiedStats(groups);

        // Wait for event with timeout
        var receivedStats = await Task.WhenAny(tcs.Task, Task.Delay(1000)) == tcs.Task 
            ? await tcs.Task 
            : null;

        // Assert
        Assert.NotNull(receivedStats);
        Assert.Equal(2, receivedStats.TotalGroups);
        Assert.Equal(1, receivedStats.WithNir);
        Assert.Equal(1, receivedStats.WithoutNir);
    }

    [Fact]
    public async Task CalculateUnifiedStats_SameStats_DoesNotRaiseEventTwice()
    {
        // Arrange
        var groups = new List<FileGroup>
        {
            new FileGroup { GroupId = "1", HasNir = true, Status = GroupStatus.Complete }
        };
        
        int eventCount = 0;
        _service.MatchingStatisticsUpdated += (sender, stats) => eventCount++;

        // Act - Calculate same stats twice
        _service.CalculateUnifiedStats(groups);
        await Task.Delay(200);
        _service.CalculateUnifiedStats(groups);
        await Task.Delay(200);

        // Assert - Event should only be raised once
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void CalculateUnifiedStats_DifferentStats_RaisesEventMultipleTimes()
    {
        // Arrange
        var groups1 = new List<FileGroup>
        {
            new FileGroup { GroupId = "1", HasNir = true, Status = GroupStatus.Complete }
        };
        
        var groups2 = new List<FileGroup>
        {
            new FileGroup { GroupId = "1", HasNir = true, Status = GroupStatus.Complete },
            new FileGroup { GroupId = "2", HasNir = false, Status = GroupStatus.Pending }
        };
        
        var receivedStats = new List<MatchingStatistics>();
        _service.MatchingStatisticsUpdated += (sender, stats) => receivedStats.Add(stats);

        // Act - Calculate different stats
        var stats1 = _service.CalculateUnifiedStats(groups1);
        var stats2 = _service.CalculateUnifiedStats(groups2);

        // Assert - Event should be raised twice (once for each different stat)
        Assert.Equal(2, receivedStats.Count);
        Assert.Equal(1, receivedStats[0].TotalGroups);
        Assert.Equal(2, receivedStats[1].TotalGroups);
    }

    [Fact]
    public async Task FileCountsUpdated_EventCanBeSubscribed()
    {
        // Arrange
        var config = new ApplicationConfiguration
        {
            FolderPaths = new Dictionary<string, string>()
        };
        
        bool eventReceived = false;
        _service.FileCountsUpdated += (sender, stats) => eventReceived = true;

        // Act
        await _service.StartMonitoringAsync(config);
        
        // Give it a moment to start
        await Task.Delay(100);

        // Assert - Event handler should be subscribed (we can't easily test if it fires without real file system changes)
        Assert.True(_service.IsMonitoring);

        // Cleanup
        await _service.StopMonitoringAsync();
    }

    [Fact]
    public async Task GetCurrentFileCounts_AfterCalculation_ReturnsUpdatedStats()
    {
        // Arrange
        var groups = new List<FileGroup>
        {
            new FileGroup { GroupId = "1", HasNir = true, Status = GroupStatus.Complete }
        };

        // Act
        _service.CalculateUnifiedStats(groups);
        await Task.Delay(200);
        var currentStats = _service.GetCurrentMatchingStats();

        // Assert
        Assert.Equal(1, currentStats.TotalGroups);
        Assert.Equal(1, currentStats.WithNir);
    }

    [Fact]
    public async Task MatchingStatisticsUpdated_EventArgs_ContainCorrectData()
    {
        // Arrange
        var groups = new List<FileGroup>
        {
            new FileGroup { GroupId = "1", HasNir = true, Status = GroupStatus.Complete },
            new FileGroup { GroupId = "2", HasNir = true, Status = GroupStatus.Complete },
            new FileGroup { GroupId = "3", HasNir = false, Status = GroupStatus.Pending }
        };
        
        var tcs = new TaskCompletionSource<MatchingStatistics>();
        _service.MatchingStatisticsUpdated += (sender, stats) => tcs.TrySetResult(stats);

        // Act
        _service.CalculateUnifiedStats(groups);

        // Wait for event with timeout
        var receivedStats = await Task.WhenAny(tcs.Task, Task.Delay(1000)) == tcs.Task 
            ? await tcs.Task 
            : null;

        // Assert
        Assert.NotNull(receivedStats);
        Assert.Equal(3, receivedStats.TotalGroups);
        Assert.Equal(2, receivedStats.WithNir);
        Assert.Equal(1, receivedStats.WithoutNir);
        Assert.Equal(0, receivedStats.Failed);
        Assert.Equal(66.666666666666671, TestHelpers.StatisticsTestHelper.CalcMatchRate(receivedStats.TotalGroups, receivedStats.WithNir), 0.0001);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}
