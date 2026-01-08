using ChronoView.Models;
using ChronoView.Core.Analytics;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace ChronoView.UI.ViewModels;

public interface IDashboardViewModel : IDisposable
{
    ObservableCollection<FileGroupViewModel> FileGroups { get; }
    ObservableCollection<FileGroupViewModel> Line1Groups { get; }
    ObservableCollection<FileGroupViewModel> Line2Groups { get; }

    // Statistics - Summary
    int TotalGroups { get; }

    int Failures { get; }
    int AbnormalCount { get; }
    int WithNirCount { get; }
    int WithoutNirCount { get; }
    int FailedCount { get; }

    // Statistics - File Counts
    int NirCount { get; }
    int Nir2Count { get; }
    int NormalCount { get; }
    int Normal2Count { get; }
    int Cam1Count { get; }
    int Cam2Count { get; }
    int Cam3Count { get; }
    int Cam4Count { get; }
    int Cam5Count { get; }
    int Cam6Count { get; }

    // Statistics - Matching (Unified)
    int UnifiedTotalGroups { get; }
    int UnifiedWithNir { get; }
    int UnifiedWithoutNir { get; }
    int UnifiedFailed { get; }

    // Statistics - Matching (Separated)
    int Line1TotalGroups { get; }
    int Line1WithNir { get; }
    int Line1WithoutNir { get; }
    int Line1Failed { get; }
    int Line2TotalGroups { get; }
    int Line2WithNir { get; }
    int Line2WithoutNir { get; }
    int Line2Failed { get; }

    void UpdateStatistics();
    void ClearFileGroups();
    void UpdateFileCountStatistics(FileCountStatistics e);
    void UpdateMatchingStatistics(MatchingStatistics e);
    void UpdateMatchingStatistics(MatchingStatistics line1, MatchingStatistics line2);

    void RemoveGroup(FileGroupViewModel vm);

    // Partial Selection Helpers
    bool CanRemoveGroup(FileGroupViewModel vm);
    void ClearPartialSelection(FileGroupViewModel vm);

    event Action<LogSeverity, string, string> LogRequested;
    event Action<string> StatusChanged;
}
