using ChronoView.Core.FileOperations;
using ChronoView.Core.Configuration;
using ChronoView.Models;
using ChronoView.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChronoView.UI.ViewModels;

public class FileOperationViewModel : ViewModelBase, IFileOperationViewModel
{
    private readonly IMoveService _moveService;
    private readonly IDeleteService _deleteService;
    private readonly IConfigurationManager _configManager;
    private readonly IDashboardViewModel _dashboard;
    private readonly ILogger<FileOperationViewModel> _logger;

    public event Action<LogSeverity, string, string>? LogRequested;
    public event Action<string>? StatusChanged;

    private bool _isOperationInProgress; public bool IsOperationInProgress { get => _isOperationInProgress; private set { if (SetProperty(ref _isOperationInProgress, value)) { (MoveCommand as RelayCommand)?.RaiseCanExecuteChanged(); (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged(); } } }
    private int _progressValue; public int ProgressValue { get => _progressValue; set => SetProperty(ref _progressValue, value); }

    public ICommand MoveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }

    public FileOperationViewModel(IMoveService moveService, IDeleteService deleteService, IConfigurationManager configManager, IDashboardViewModel dashboard, ILogger<FileOperationViewModel> logger)
    {
        _moveService = moveService; _deleteService = deleteService; _configManager = configManager; _dashboard = dashboard; _logger = logger;
        
        MoveCommand = new RelayCommand(() => { /* Move is called directly from MainWindowViewModel */ }, () => !IsOperationInProgress && _dashboard.FileGroups.Any());
        DeleteCommand = new RelayCommand(() => _ = ExecuteDeleteAsync(_dashboard.FileGroups.Where(g => g.IsSelected || g.IsAnyPartialSelected)), () => !IsOperationInProgress && _dashboard.FileGroups.Any(g => g.IsSelected || g.IsAnyPartialSelected));
        RefreshCommand = new RelayCommand(() => _ = RefreshDataAsync());

        _dashboard.FileGroups.CollectionChanged += (s, e) => { (MoveCommand as RelayCommand)?.RaiseCanExecuteChanged(); (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged(); };
    }

    public async Task ExecuteMoveAsync(IEnumerable<FileGroupViewModel> selectedGroups, string moveNirCount, string moveAllDataCount, string? subject = null)
    {
        if (IsOperationInProgress) return;
        try {
            IsOperationInProgress = true;

            await SaveLimitsAsync(moveNirCount, moveAllDataCount);

            var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
            
            // Validate output path before proceeding
            var outputPath = config.MatchingSettings.OutputPath;
            
            // If output path is empty, try to use default path based on BasePath
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                var basePath = !string.IsNullOrWhiteSpace(config.BasePath) ? config.BasePath : "D:/Data";
                outputPath = System.IO.Path.Combine(basePath, "Output");
                _logger.LogInformation("OutputPath is empty, using default: {Path}", outputPath);
                
                // Ask user if they want to use default path or cancel
                var message = $"출력 경로가 설정되지 않았습니다.\n\n기본 경로를 사용하시겠습니까?\n{outputPath}\n\n(아니오를 선택하면 이동 작업이 취소됩니다.)";
                var dialogResult = System.Windows.MessageBox.Show(message, "출력 경로 미지정", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                
                if (dialogResult == System.Windows.MessageBoxResult.No)
                {
                    var errorMessage = "출력 경로가 설정되지 않아 이동 작업을 취소했습니다.\n설정 다이얼로그에서 출력 경로를 지정해주세요.";
                    _logger.LogWarning("Move operation cancelled by user: OutputPath is empty");
                    LogRequested?.Invoke(LogSeverity.Warning, "FileOperation", errorMessage);
                    StatusChanged?.Invoke("이동 취소: 출력 경로 미지정");
                    return;
                }
                
                // Save the default path to configuration for future use
                config.MatchingSettings.OutputPath = outputPath;
                await _configManager.SaveConfigurationAsync(config);
                _logger.LogInformation("Saved default OutputPath to configuration: {Path}", outputPath);
            }

            // Check if output directory exists, if not, try to create it
            if (!System.IO.Directory.Exists(outputPath))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(outputPath);
                    _logger.LogInformation("Created output directory: {Path}", outputPath);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"출력 경로를 생성할 수 없습니다.\n경로: {outputPath}\n오류: {ex.Message}";
                    _logger.LogError(ex, "Failed to create output directory: {Path}", outputPath);
                    LogRequested?.Invoke(LogSeverity.Error, "FileOperation", errorMessage);
                    StatusChanged?.Invoke("이동 실패: 출력 경로 생성 불가");
                    System.Windows.MessageBox.Show(errorMessage, "이동 실패", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
            }

            int.TryParse(moveNirCount, out int nirLimit);
            int.TryParse(moveAllDataCount, out int totalLimit);

            var result = await _moveService.BatchMoveAsync(
                selectedGroups.Select(g => g.Model),
                outputPath,
                totalLimit, nirLimit, subject,
                new Progress<OperationProgress>(p => { ProgressValue = (int)p.PercentComplete; StatusChanged?.Invoke(p.Status); }));

            LogRequested?.Invoke(LogSeverity.Info, "FileOperation", result.Success ? "Move completed successfully." : "Move partial/failed.");
        } finally { IsOperationInProgress = false; }
    }

    public async Task ExecuteDeleteAsync(IEnumerable<FileGroupViewModel> selectedGroups, string? subject = null)
    {
        if (IsOperationInProgress) return;
        try {
            IsOperationInProgress = true;
            StatusChanged?.Invoke("Deleting files...");

            var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
            var quarantinePath = config.WorkflowSettings.DeleteQuarantinePath;
            if (string.IsNullOrEmpty(quarantinePath)) quarantinePath = System.IO.Path.Combine(config.BasePath, "Quarantine");

            var groupList = selectedGroups.ToList();
            var groupsToRemove = new List<FileGroupViewModel>();
            _logger.LogInformation("ExecuteDeleteAsync starting for {Count} items, Subject={Subject}", groupList.Count, subject ?? "null");

            foreach (var vm in groupList) {
                // IsSelected 여부와 관계없이, 항상 선택된 컴포넌트만 삭제
                if (!vm.IsAnyPartialSelected) continue;

                var comps = vm.GetSelectedComponents();
                if (comps.Count == 0) continue;

                var result = await _deleteService.DeleteComponentsAsync(vm.Model, comps, quarantinePath, subject);
                if (result.Success) {
                    _dashboard.ClearPartialSelection(vm);
                    vm.Refresh();

                    // 그룹에 남은 데이터가 없으면 UI에서 제거
                    if (!vm.HasAnyRemainingData()) {
                        groupsToRemove.Add(vm);
                        LogRequested?.Invoke(LogSeverity.Info, "Delete", $"그룹 {vm.GroupId} 전체 삭제 완료");
                    } else {
                        LogRequested?.Invoke(LogSeverity.Info, "Delete", $"{vm.GroupId}: {string.Join(", ", comps)} 삭제");
                    }
                } else {
                    LogRequested?.Invoke(LogSeverity.Warning, "Delete", $"{vm.GroupId} 삭제 실패: {result.ErrorMessage}");
                }
            }

            // Remove deleted groups from UI collections
            foreach (var vm in groupsToRemove) {
                _dashboard.RemoveGroup(vm);
            }

            if (groupsToRemove.Any()) {
                LogRequested?.Invoke(LogSeverity.Info, "Delete", $"{groupsToRemove.Count} groups successfully deleted.");
            }

            StatusChanged?.Invoke("Delete completed.");

            // Refresh는 MainWindowViewModel에서 일원화하여 처리
            // 중복 갱신 방지를 위해 여기서는 제거

        } catch (Exception ex) {
            _logger.LogError(ex, "Error during delete operation");
            LogRequested?.Invoke(LogSeverity.Error, "Delete", $"Error: {ex.Message}");
            StatusChanged?.Invoke($"Delete failed: {ex.Message}");
        } finally {
            IsOperationInProgress = false;
        }
    }

    private async Task SaveLimitsAsync(string nir, string all)
    {
        var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
        if (int.TryParse(nir, out int n)) config.MatchingSettings.MoveNir = n; else config.MatchingSettings.MoveNir = null;
        if (int.TryParse(all, out int a)) config.MatchingSettings.MoveAllData = a; else config.MatchingSettings.MoveAllData = null;
        await _configManager.SaveConfigurationAsync(config);
    }

    public async Task RefreshDataAsync() { _dashboard.ClearFileGroups(); StatusChanged?.Invoke("Data refreshed."); }
    public void Dispose() { }
}
