using ChronoView.Core.Analytics;
using ChronoView.Core.FileWatching;
using ChronoView.Core.Configuration;
using ChronoView.Core.ImageProcessing;
using ChronoView.Core.NIR.Interfaces;
using ChronoView.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WpfApplication = System.Windows.Application;

namespace ChronoView.UI.ViewModels;

public class DashboardViewModel : ViewModelBase, IDashboardViewModel
{
    private readonly IMonitoringOrchestrator _orchestrator;
    private readonly IStatisticsService _statsService;
    private readonly IImageProcessor _imageProcessor;
    private readonly IAbnormalDetector _abnormalDetector;
    private readonly IConfigurationManager _configManager;
    private readonly ILogger<DashboardViewModel> _logger;
    private readonly ILogger<FileGroupViewModel> _fileGroupLogger;
    private readonly INirDataProvider? _nirDataProvider;

    public event Action<LogSeverity, string, string>? LogRequested;
    public event Action<string>? StatusChanged;

    public ObservableCollection<FileGroupViewModel> FileGroups { get; } = new();
    public ObservableCollection<FileGroupViewModel> Line1Groups { get; } = new();
    public ObservableCollection<FileGroupViewModel> Line2Groups { get; } = new();

    #region Statistics Properties
    private int _totalGroups; public int TotalGroups { get => _totalGroups; private set => SetProperty(ref _totalGroups, value); }

    private int _failures; public int Failures { get => _failures; private set => SetProperty(ref _failures, value); }
    private int _abnormalCount; public int AbnormalCount { get => _abnormalCount; private set => SetProperty(ref _abnormalCount, value); }

    public int WithNirCount => FileGroups.Count(g => g.HasNir);
    public int WithoutNirCount => FileGroups.Count(g => !g.HasNir && g.Status != GroupStatus.Error);
    public int FailedCount => FileGroups.Count(g => g.Status == GroupStatus.Error);

    public int NirCount { get; private set; }
    public int Nir2Count { get; private set; }
    public int NormalCount { get; private set; }
    public int Normal2Count { get; private set; }
    public int Cam1Count { get; private set; }
    public int Cam2Count { get; private set; }
    public int Cam3Count { get; private set; }
    public int Cam4Count { get; private set; }
    public int Cam5Count { get; private set; }
    public int Cam6Count { get; private set; }

    public int UnifiedTotalGroups { get; private set; }
    public int UnifiedWithNir { get; private set; }
    public int UnifiedWithoutNir { get; private set; }
    public int UnifiedFailed { get; private set; }

    public int Line1TotalGroups { get; private set; }
    public int Line1WithNir { get; private set; }
    public int Line1WithoutNir { get; private set; }
    public int Line1Failed { get; private set; }
    public int Line2TotalGroups { get; private set; }
    public int Line2WithNir { get; private set; }
    public int Line2WithoutNir { get; private set; }
    public int Line2Failed { get; private set; }
    #endregion

    public DashboardViewModel(
        IMonitoringOrchestrator orchestrator,
        IStatisticsService statsService,
        IImageProcessor imageProcessor,
        IAbnormalDetector abnormalDetector,
        IConfigurationManager configManager,
        ILogger<DashboardViewModel> logger,
        ILogger<FileGroupViewModel> fileGroupLogger,
        INirDataProvider? nirDataProvider = null)
    {
        _orchestrator = orchestrator;
        _statsService = statsService;
        _imageProcessor = imageProcessor;
        _abnormalDetector = abnormalDetector;
        _configManager = configManager;
        _logger = logger;
        _fileGroupLogger = fileGroupLogger;
        _nirDataProvider = nirDataProvider;

        _orchestrator.GroupCreated += OnGroupCreated;
        _orchestrator.GroupUpdated += OnGroupUpdated;
        _orchestrator.GroupRemoved += OnGroupRemoved;
        _orchestrator.MonitoringStateReset += (s, e) => WpfApplication.Current.Dispatcher.Invoke(ClearFileGroups);

        _statsService.FileCountsUpdated += (s, e) => UpdateFileCountStatistics(e);
        _statsService.MatchingStatisticsUpdated += (s, e) => UpdateMatchingStatistics(e);
    }

    public void UpdateStatistics()
    {
        TotalGroups = FileGroups.Count;
        var withNir = FileGroups.Count(g => g.HasNir);

        Failures = FileGroups.Count(g => g.Status == GroupStatus.Error);
        AbnormalCount = FileGroups.Count(g => g.IsAbnormal);
        
        var unified = _statsService.CalculateUnifiedStats(FileGroups.Select(g => g.Model));
        UpdateMatchingStatistics(unified);

        var (l1, l2) = _statsService.CalculateSeparatedStats(Line1Groups.Select(g => g.Model), Line2Groups.Select(g => g.Model));
        UpdateMatchingStatistics(l1, l2);

        OnPropertyChanged(string.Empty);
    }

    public void UpdateFileCountStatistics(FileCountStatistics e)
    {
        NirCount = e.NirCount; Nir2Count = e.Nir2Count;
        NormalCount = e.NormalCount; Normal2Count = e.Normal2Count;
        Cam1Count = e.Cam1Count; Cam2Count = e.Cam2Count; Cam3Count = e.Cam3Count;
        Cam4Count = e.Cam4Count; Cam5Count = e.Cam5Count; Cam6Count = e.Cam6Count;
        StatusChanged?.Invoke($"File counts updated: Nir={NirCount}, Normal={NormalCount}"); // Address CS0067
        OnPropertyChanged(string.Empty);
    }

    public void UpdateMatchingStatistics(MatchingStatistics e)
    {
        UnifiedTotalGroups = e.TotalGroups; UnifiedWithNir = e.WithNir; UnifiedWithoutNir = e.WithoutNir; UnifiedFailed = e.Failed;
        OnPropertyChanged(string.Empty);
    }

    public void UpdateMatchingStatistics(MatchingStatistics line1, MatchingStatistics line2)
    {
        Line1TotalGroups = line1.TotalGroups; Line1WithNir = line1.WithNir; Line1WithoutNir = line1.WithoutNir; Line1Failed = line1.Failed;
        Line2TotalGroups = line2.TotalGroups; Line2WithNir = line2.WithNir; Line2WithoutNir = line2.WithoutNir; Line2Failed = line2.Failed;
        OnPropertyChanged(string.Empty);
    }

    public bool CanRemoveGroup(FileGroupViewModel vm) => !vm.HasNir && string.IsNullOrEmpty(vm.NormalFolder);
    public void ClearPartialSelection(FileGroupViewModel vm) { vm.IsNormalSelected = vm.IsNirSelected = false; vm.IsCam1Selected = vm.IsCam2Selected = vm.IsCam3Selected = vm.IsCam4Selected = vm.IsCam5Selected = vm.IsCam6Selected = false; }

    public void ClearFileGroups() { FileGroups.Clear(); Line1Groups.Clear(); Line2Groups.Clear(); UpdateStatistics(); StatusChanged?.Invoke("Dashboard cleared."); }

    private void OnGroupCreated(object? sender, FileGroup group)
    {
        WpfApplication.Current.Dispatcher.InvokeAsync(() => {
            if (FileGroups.Any(g => g.GroupId == group.GroupId)) return;
            var config = _configManager.LoadConfiguration<ChronoView.Models.ApplicationConfiguration>();
            var vm = new FileGroupViewModel(group, _imageProcessor, _orchestrator, _abnormalDetector, config, _fileGroupLogger, (s, src, m) => LogRequested?.Invoke(s, src, m), _nirDataProvider);
            FileGroups.Add(vm);
            if (group.LineNumber == 1) Line1Groups.Add(vm);
            else if (group.LineNumber == 2) Line2Groups.Add(vm);
            _ = vm.LoadThumbnailsAsync();
            UpdateStatistics();
        });
    }

    private void OnGroupUpdated(object? sender, FileGroup group)
    {
        WpfApplication.Current.Dispatcher.InvokeAsync(() => {
            var existingVm = FileGroups.FirstOrDefault(g => g.GroupId == group.GroupId);
            if (existingVm != null)
            {
                existingVm.Refresh();
                _ = existingVm.LoadThumbnailsAsync();
                UpdateStatistics();
            }
            else
            {
                OnGroupCreated(sender, group);
            }
        });
    }

    public void RemoveGroup(FileGroupViewModel vm)
    {
        if (vm == null) return;
        FileGroups.Remove(vm);
        Line1Groups.Remove(vm);
        Line2Groups.Remove(vm);
        UpdateStatistics();
    }

    private void OnGroupRemoved(object? sender, string groupId)
    {
        WpfApplication.Current.Dispatcher.InvokeAsync(() => {
            var vm = FileGroups.FirstOrDefault(g => g.GroupId == groupId);
            if (vm != null) RemoveGroup(vm);
        });
    }

    public void Dispose() { _orchestrator.GroupCreated -= OnGroupCreated; _orchestrator.GroupUpdated -= OnGroupUpdated; _orchestrator.GroupRemoved -= OnGroupRemoved; }
}
