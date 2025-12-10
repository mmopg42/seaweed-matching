# UI Service Integration Gaps - 구현 설계 문서

## 개요

이 문서는 `requirements.md`에서 정의된 누락 항목들에 대한 구체적인 구현 가이드를 제공합니다.

---

## 0. Line 1 / Line 2 구분 스펙

### 라인별 구성

| 구분 | Line 1 (첫번째 라인) | Line 2 (두번째 라인) |
|------|---------------------|---------------------|
| **NIR** | NIR1 | NIR2 |
| **Normal** | Normal1 (`_0` 접미사) | Normal2 (`_1` 접미사) |
| **Camera** | Cam1, Cam2, Cam3 | Cam4, Cam5, Cam6 |

### Normal 폴더 구조

Normal 폴더는 다른 폴더와 달리 특별한 구조를 가집니다:

```
Normal 경로/
├── 20251204_143052_0/    ← Line 1 (Normal1) - 타임스탬프_0
│   └── image.jpg
├── 20251204_143052_1/    ← Line 2 (Normal2) - 타임스탬프_1
│   └── image.jpg
├── 20251204_143127_0/
│   └── image.jpg
└── ...
```

- **타임스탬프 형식**: `YYYYMMDD_HHMMSS`
- **라인 구분자**: `_0` = Line 1 (Normal1), `_1` = Line 2 (Normal2)

### 설정 경로 구조 (ApplicationConfiguration 확장)

```csharp
public class MatchingSettings
{
    // 기존 경로들을 라인별로 구분

    // === Line 1 경로 ===
    public string Nir1Path { get; set; } = "";      // NIR1 폴더
    public string Normal1Path { get; set; } = "";   // Normal1 (폴더들의 상위 경로, _0 접미사)
    // Camera1Path, Camera2Path, Camera3Path 기존 유지
    
    // === Line 2 경로 ===
    public string Nir2Path { get; set; } = "";      // NIR2 폴더
    public string Normal2Path { get; set; } = "";   // Normal2 (폴더들의 상위 경로, _1 접미사)
    // Camera4Path, Camera5Path, Camera6Path 기존 유지

    // 라인 구분 헬퍼 메서드
    public int GetCameraLineNumber(int cameraNumber)
    {
        return cameraNumber <= 3 ? 1 : 2;  // Cam1-3=Line1, Cam4-6=Line2
    }
}
```

### FileGroup 모델 확장

```csharp
// ChronoView/Models/FileGroup.cs 수정
public class FileGroup
{
    // 기존 속성들...

    /// <summary>
    /// 라인 번호 (1 또는 2)
    /// </summary>
    public int LineNumber { get; set; } = 1;

    /// <summary>
    /// Normal 폴더명에서 라인 번호 추출
    /// </summary>
    public static int GetLineNumberFromNormalFolder(string normalFolderName)
    {
        // 예: "20251204_143052_0" → Line 1
        //     "20251204_143052_1" → Line 2
        if (string.IsNullOrEmpty(normalFolderName)) return 1;
        
        if (normalFolderName.EndsWith("_0")) return 1;
        if (normalFolderName.EndsWith("_1")) return 2;
        return 1; // 기본값
    }
}
```

### MainWindowViewModel 컬렉션 확장

```csharp
// 라인별 컬렉션 추가
public ObservableCollection<FileGroupViewModel> Line1Groups { get; } = new();
public ObservableCollection<FileGroupViewModel> Line2Groups { get; } = new();

// AddFileGroup 메서드 수정
private void AddFileGroup(FileGroupViewModel viewModel)
{
    FileGroups.Add(viewModel);
    
    // 라인별 컬렉션에도 추가
    if (viewModel.LineNumber == 1)
        Line1Groups.Add(viewModel);
    else if (viewModel.LineNumber == 2)
        Line2Groups.Add(viewModel);
}

// RemoveFileGroup 메서드 수정  
private void RemoveFileGroup(FileGroupViewModel viewModel)
{
    FileGroups.Remove(viewModel);
    Line1Groups.Remove(viewModel);
    Line2Groups.Remove(viewModel);
}

// ClearFileGroups 메서드 수정
private void ClearFileGroups()
{
    FileGroups.Clear();
    Line1Groups.Clear();
    Line2Groups.Clear();
}
```

### SettingsDialog 경로 필드 추가

Settings Dialog에 다음 경로들 추가 필요:

| 기존/신규 | 필드명 | 라인 | 설명 |
|-----------|--------|------|------|
| 기존 | NirPath | → Nir1Path | Line 1 NIR |
| **신규** | Nir2Path | Line 2 | Line 2 NIR |
| 기존 | NormalPath | → Normal1Path | Line 1 Normal (폴더들 상위 경로) |
| **신규** | Normal2Path | Line 2 | Line 2 Normal (폴더들 상위 경로) |
| 기존 | Camera1Path~Camera3Path | Line 1 | Line 1 카메라들 |
| 기존 | Camera4Path~Camera6Path | Line 2 | Line 2 카메라들 |

---

### 0.1 공통 인프라 패턴

#### CancellationToken 관리 전략

```csharp
// MainWindowViewModel.cs
public class MainWindowViewModel : ViewModelBase, IDisposable
{
    // 윈도우 전체 공유 CancellationTokenSource
    private CancellationTokenSource _windowCts = new();
    
    // 개별 작업용 (Move/Delete 등)
    private CancellationTokenSource? _operationCts;
    
    /// <summary>
    /// 개별 작업 시작 시 새 CancellationTokenSource 생성
    /// </summary>
    private CancellationToken BeginOperation()
    {
        _operationCts?.Cancel();
        _operationCts?.Dispose();
        _operationCts = CancellationTokenSource.CreateLinkedTokenSource(_windowCts.Token);
        return _operationCts.Token;
    }
    
    /// <summary>
    /// 진행 중 작업 취소
    /// </summary>
    public void CancelCurrentOperation()
    {
        _operationCts?.Cancel();
    }
    
    /// <summary>
    /// Window 종료 시 모든 작업 취소
    /// </summary>
    public void Dispose()
    {
        _windowCts.Cancel();
        _windowCts.Dispose();
        _operationCts?.Dispose();
        // 이벤트 구독 해제...
    }
}
```

#### Progress 표시 전략

**StatusBar + ProgressBar 조합** (모달 대신 Non-blocking 방식):

```csharp
// MainWindowViewModel.cs - Progress 관련 속성
private double _progressValue;
private bool _isOperationInProgress;

public double ProgressValue
{
    get => _progressValue;
    set => SetProperty(ref _progressValue, value);
}

public bool IsOperationInProgress
{
    get => _isOperationInProgress;
    set => SetProperty(ref _isOperationInProgress, value);
}
// 동시 실행 방지: Move/Delete 등 작업 중에는 Start/Stop/Refresh 버튼 비활성화 (CanExecute에서 IsOperationInProgress 사용)

// Progress 생성 헬퍼
private IProgress<OperationProgress> CreateProgressReporter()
{
    return new Progress<OperationProgress>(p =>
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ProgressValue = p.PercentComplete;
            StatusMessage = $"{p.CurrentFile} ({p.ProcessedFiles}/{p.TotalFiles})";
        });
    });
}
```

```xml
<!-- MainWindow.xaml - StatusBar에 ProgressBar 추가 -->
<StatusBar>
    <StatusBarItem>
        <ProgressBar Width="150" Height="16" 
                     Value="{Binding ProgressValue}" 
                     Visibility="{Binding IsOperationInProgress, Converter={StaticResource BoolToVisibility}}"
                     Maximum="100"/>
    </StatusBarItem>
    <StatusBarItem>
        <TextBlock Text="{Binding StatusMessage}"/>
    </StatusBarItem>
</StatusBar>
```

#### IsSelected 바인딩 동기화

```csharp
// FileGroupViewModel.cs - IsSelected 속성
private bool _isSelected;
public bool IsSelected
{
    get => _isSelected;
    set => SetProperty(ref _isSelected, value);
}
```

```xml
<!-- MainWindow.xaml - DataGridRow와 IsSelected 양방향 바인딩 -->
<DataGrid.RowStyle>
    <Style TargetType="DataGridRow" BasedOn="{StaticResource FileGroupRowStyle}">
        <Setter Property="IsSelected" Value="{Binding IsSelected, Mode=TwoWay}"/>
    </Style>
</DataGrid.RowStyle>
```

#### 탭별 SelectedGroup 처리 전략

**선택한 전략: 탭별 독립 관리**

```csharp
// MainWindowViewModel.cs
private int _activeTabIndex;
private FileGroupViewModel? _selectedLine1Group;
private FileGroupViewModel? _selectedLine2Group;

public int ActiveTabIndex
{
    get => _activeTabIndex;
    set
    {
        if (SetProperty(ref _activeTabIndex, value))
            OnPropertyChanged(nameof(SelectedGroup));
    }
}

public FileGroupViewModel? SelectedLine1Group
{
    get => _selectedLine1Group;
    set => SetProperty(ref _selectedLine1Group, value);
}

public FileGroupViewModel? SelectedLine2Group
{
    get => _selectedLine2Group;
    set => SetProperty(ref _selectedLine2Group, value);
}

/// <summary>
/// 현재 탭 기준 SelectedGroup (커맨드에서 사용)
/// </summary>
public FileGroupViewModel? SelectedGroup => ActiveTabIndex switch
{
    0 => SelectedLine1Group,
    1 => SelectedLine2Group,
    2 => SelectedLine1Group ?? SelectedLine2Group, // Combined는 둘 중 하나
    _ => null
};

// 현재 탭 기준 선택된 그룹들 가져오기
private IReadOnlyList<FileGroupViewModel> GetSelectedGroups()
{
    var sourceCollection = ActiveTabIndex switch
    {
        0 => Line1Groups,
        1 => Line2Groups,
        _ => FileGroups
    };
    return sourceCollection.Where(g => g.IsSelected).ToList();
}
```

```xml
<!-- MainWindow.xaml -->
<TabControl SelectedIndex="{Binding ActiveTabIndex}">
    <TabItem Header="Line 1">
        <DataGrid SelectedItem="{Binding SelectedLine1Group}" .../>
    </TabItem>
    <TabItem Header="Line 2">
        <DataGrid SelectedItem="{Binding SelectedLine2Group}" .../>
    </TabItem>
    <TabItem Header="Combined">
        <!-- Combined 탭에서는 양쪽 DataGrid 모두 표시 -->
    </TabItem>
</TabControl>
```

---

## 1. Command 구현


### 1.1 ExecuteMove 구현

#### 현재 상태
```csharp
// MainWindowViewModel.cs (Line 665-671)
private void ExecuteMove()
{
    if (SelectedGroup == null) return;
    AddLogMessage(LogSeverity.Info, "FileOperation", $"Moving group {SelectedGroup.GroupId}");
    // TODO: Implement move operation
}
```

#### 설계 변경

##### 1.1.1 비동기 메서드로 변경
```csharp
private async void ExecuteMove()
{
    await ExecuteMoveAsync();
}

private async Task ExecuteMoveAsync()
{
    if (SelectedGroup == null) return;
    
    try
    {
        var selectedGroups = GetSelectedGroups(); // 다중 선택 지원
        var destinationPath = _configManager.GetConfiguration().OutputPath;
        
        if (string.IsNullOrEmpty(destinationPath))
        {
            await ShowErrorMessageAsync("Move Failed", "Output path is not configured.");
            return;
        }
        
        // Progress 리포팅
        var progress = new Progress<OperationProgress>(p =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"Moving: {p.CurrentFile} ({p.PercentComplete:F0}%)";
            });
        });
        
        foreach (var group in selectedGroups)
        {
            AddLogMessage(LogSeverity.Info, "FileOperation", $"Moving group {group.GroupId}");
            
            var result = await _fileOperationService.MoveFileGroupAsync(
                group.UnderlyingFileGroup,
                destinationPath,
                progress,
                _cancellationToken);
            
            if (result.Success)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    RemoveFileGroup(group);
                });
                AddLogMessage(LogSeverity.Info, "FileOperation", 
                    $"Successfully moved {result.FilesProcessed} files");
            }
            else
            {
                AddLogMessage(LogSeverity.Error, "FileOperation", 
                    $"Failed to move: {result.ErrorMessage}");
            }
        }
        
        StatusMessage = "Ready";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error executing move operation");
        AddLogMessage(LogSeverity.Error, "FileOperation", $"Move failed: {ex.Message}");
    }
}
```

##### 1.1.2 필요한 헬퍼 메서드 추가
```csharp
private IEnumerable<FileGroupViewModel> GetSelectedGroups()
{
    return FileGroups.Where(g => g.IsSelected).ToList();
}

private async Task ShowErrorMessageAsync(string title, string message)
{
    await Application.Current.Dispatcher.InvokeAsync(() =>
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    });
}
```

---

### 1.2 ExecuteDelete 구현

#### 설계 변경

```csharp
private async void ExecuteDelete()
{
    await ExecuteDeleteAsync();
}

private async Task ExecuteDeleteAsync()
{
    if (SelectedGroup == null) return;
    
    try
    {
        var selectedGroups = GetSelectedGroups();
        
        // 삭제 확인 다이얼로그
        var result = MessageBox.Show(
            $"Are you sure you want to delete {selectedGroups.Count()} file group(s)?\n\n" +
            "This action cannot be undone.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        
        if (result != MessageBoxResult.Yes) return;
        
        var progress = new Progress<OperationProgress>(p =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"Deleting: {p.CurrentFile} ({p.PercentComplete:F0}%)";
            });
        });
        
        foreach (var group in selectedGroups)
        {
            AddLogMessage(LogSeverity.Warning, "FileOperation", 
                $"Deleting group {group.GroupId}");
            
            var opResult = await _fileOperationService.DeleteFileGroupAsync(
                group.UnderlyingFileGroup,
                progress,
                _cancellationToken);
            
            if (opResult.Success)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    RemoveFileGroup(group);
                });
                AddLogMessage(LogSeverity.Info, "FileOperation", 
                    $"Deleted {opResult.FilesProcessed} files");
            }
            else
            {
                AddLogMessage(LogSeverity.Error, "FileOperation", 
                    $"Delete failed: {opResult.ErrorMessage}");
            }
        }
        
        StatusMessage = "Ready";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error executing delete operation");
        AddLogMessage(LogSeverity.Error, "FileOperation", $"Delete failed: {ex.Message}");
    }
}
```

---

### 1.3 ExecuteRefresh 구현

#### 설계 변경

```csharp
private async void ExecuteRefresh()
{
    await ExecuteRefreshAsync();
}

private async Task ExecuteRefreshAsync()
{
    try
    {
        StatusMessage = "Refreshing...";
        AddLogMessage(LogSeverity.Info, "System", "Refreshing data...");

        // 모니터링 비활성 시 즉시 중단 (UI 초기화 금지)
        if (!IsMonitoring)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show("Monitoring is not running. Start monitoring before refresh.",
                    "Refresh unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
            AddLogMessage(LogSeverity.Warning, "System", "Refresh skipped - monitoring is not active");
            return;
        }

        // 취소 토큰 생성
        var cancellationToken = BeginOperation();

        // Orchestrator를 통한 전체 스캔 (토큰 전달)
        await _orchestrator.RefreshAsync(cancellationToken);

        // 새 그룹들은 GroupRemoved/GroupCreated 이벤트를 통해 자동 정리/추가됨

        StatusMessage = "Refresh complete";
        AddLogMessage(LogSeverity.Info, "System", "Refresh completed successfully");
    }
    catch (OperationCanceledException)
    {
        StatusMessage = "Refresh cancelled";
        AddLogMessage(LogSeverity.Info, "System", "Refresh cancelled by user");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during refresh");
        StatusMessage = "Refresh failed";
        AddLogMessage(LogSeverity.Error, "System", $"Refresh failed: {ex.Message}");
    }
    finally
    {
        EndOperation();
    }
}
```

#### Command Guard 업데이트
- `IsOperationInProgress` 변경 시 `Start/Stop/Move/Delete/Refresh`의 `RaiseCanExecuteChanged()` 호출로 즉시 버튼 상태 갱신
- `CanExecuteStart/Stop/Refresh`는 `!IsOperationInProgress` 조건을 추가해 장기 작업 중 중복 실행을 차단

---

### 1.4 ExecutePathAutoConfig 구현

#### 설계 변경 (설정 실제 반영 포함)

```csharp
private async void ExecutePathAutoConfig()
{
    await ExecutePathAutoConfigAsync();
}

private async Task ExecutePathAutoConfigAsync()
{
    try
    {
        if (string.IsNullOrWhiteSpace(DateInput))
        {
            MessageBox.Show("Please enter a date in YYYYMMDD format.",
                "Invalid Date", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // 날짜 형식 검증
        if (!DateTime.TryParseExact(DateInput, "yyyyMMdd", 
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            MessageBox.Show("Invalid date format. Please use YYYYMMDD format.",
                "Invalid Date", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var config = _configManager.GetConfiguration();
        var paths = _pathManagementService.GeneratePathsFromDate(DateInput, config);
        
        if (paths.Count == 0)
        {
            AddLogMessage(LogSeverity.Warning, "Configuration", 
                "No paths generated. Check path patterns in settings.");
            return;
        }
        
        // ====== 핵심: 설정에 경로 실제 반영 ======
        ApplyGeneratedPaths(config.MatchingSettings, paths);
        
        // 설정 저장
        await _configManager.SaveConfigurationAsync(config);
        
        AddLogMessage(LogSeverity.Info, "Configuration", 
            $"Applied {paths.Count} paths for date {DateInput}");
        
        // 모니터링 중이면 경로 변경 알림
        if (IsMonitoring)
        {
            var restart = MessageBox.Show(
                "Paths have been updated. Restart monitoring with new paths?",
                "Paths Updated", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (restart == MessageBoxResult.Yes)
            {
                await ExecuteStopAsync();
                await ExecuteStartAsync();
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error auto-configuring paths");
        AddLogMessage(LogSeverity.Error, "Configuration", $"Path auto-config failed: {ex.Message}");
    }
}

/// <summary>
/// 생성된 경로들을 MatchingSettings에 적용
/// </summary>
private void ApplyGeneratedPaths(MatchingSettings settings, Dictionary<string, string> paths)
{
    // Line 1 경로
    if (paths.TryGetValue("NIR1", out var nir1))
        settings.Nir1Path = nir1;
    if (paths.TryGetValue("Normal1", out var normal1))
        settings.Normal1Path = normal1;
    if (paths.TryGetValue("Cam1", out var cam1))
        settings.Camera1Path = cam1;
    if (paths.TryGetValue("Cam2", out var cam2))
        settings.Camera2Path = cam2;
    if (paths.TryGetValue("Cam3", out var cam3))
        settings.Camera3Path = cam3;
    
    // Line 2 경로
    if (paths.TryGetValue("NIR2", out var nir2))
        settings.Nir2Path = nir2;
    if (paths.TryGetValue("Normal2", out var normal2))
        settings.Normal2Path = normal2;
    if (paths.TryGetValue("Cam4", out var cam4))
        settings.Camera4Path = cam4;
    if (paths.TryGetValue("Cam5", out var cam5))
        settings.Camera5Path = cam5;
    if (paths.TryGetValue("Cam6", out var cam6))
        settings.Camera6Path = cam6;
    
    // Output 경로
    if (paths.TryGetValue("Output", out var output))
        settings.OutputPath = output;  // 또는 config.OutputPath에 별도 저장
}
```

---

### 1.5 ExecuteCreateSampleFolder 구현

#### 설계 변경

```csharp
private async void ExecuteCreateSampleFolder()
{
    await ExecuteCreateSampleFolderAsync();
}

private async Task ExecuteCreateSampleFolderAsync()
{
    try
    {
        if (string.IsNullOrWhiteSpace(SampleFolderName))
        {
            MessageBox.Show("Please enter a sample folder name.",
                "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // 폴더명 유효성 검사
        var invalidChars = Path.GetInvalidFileNameChars();
        if (SampleFolderName.IndexOfAny(invalidChars) >= 0)
        {
            MessageBox.Show("Sample folder name contains invalid characters.",
                "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var config = _configManager.GetConfiguration();
        
        StatusMessage = $"Creating sample folder: {SampleFolderName}...";
        AddLogMessage(LogSeverity.Info, "FileOperation", 
            $"Creating sample folder: {SampleFolderName}");
        
        var success = await _pathManagementService.CreateSampleFoldersAsync(
            SampleFolderName, config, _cancellationToken);
        
        if (success)
        {
            AddLogMessage(LogSeverity.Info, "FileOperation", 
                $"Sample folder '{SampleFolderName}' created successfully");
            StatusMessage = "Sample folder created";
        }
        else
        {
            AddLogMessage(LogSeverity.Error, "FileOperation", 
                "Failed to create sample folder");
            StatusMessage = "Failed to create sample folder";
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating sample folder");
        AddLogMessage(LogSeverity.Error, "FileOperation", 
            $"Sample folder creation failed: {ex.Message}");
    }
}
```

---

## 2. Abnormal Detection UI 통합

### 2.1 FileGroupViewModel에 IsAbnormal 속성 추가

#### 수정 파일: `ChronoView/UI/ViewModels/FileGroupViewModel.cs`

```csharp
public class FileGroupViewModel : ViewModelBase, IDisposable
{
    private readonly FileGroup _fileGroup;
    private readonly IImageProcessor _imageProcessor;
    private readonly IAbnormalDetector? _abnormalDetector;
    
    private bool _isAbnormal;
    private string? _abnormalReason;
    
    // 생성자 수정 - IAbnormalDetector 주입
    public FileGroupViewModel(
        FileGroup fileGroup, 
        IImageProcessor imageProcessor,
        IAbnormalDetector? abnormalDetector = null)
    {
        _fileGroup = fileGroup;
        _imageProcessor = imageProcessor;
        _abnormalDetector = abnormalDetector;
        
        InitializeImagePaths();
        CheckAbnormalStatus();
        LoadThumbnailsAsync();
    }
    
    /// <summary>
    /// Indicates whether this file group is detected as abnormal.
    /// </summary>
    public bool IsAbnormal
    {
        get => _isAbnormal;
        private set => SetProperty(ref _isAbnormal, value);
    }
    
    /// <summary>
    /// Reason for abnormal status (if applicable).
    /// </summary>
    public string? AbnormalReason
    {
        get => _abnormalReason;
        private set => SetProperty(ref _abnormalReason, value);
    }
    
    /// <summary>
    /// Status text including abnormal indicator.
    /// </summary>
    public string StatusText
    {
        get
        {
            var baseStatus = HasNirImage ? "With NIR" : "Without NIR";
            return IsAbnormal ? $"{baseStatus} (Abnormal)" : baseStatus;
        }
    }
    
    private void CheckAbnormalStatus()
    {
        if (_abnormalDetector == null)
        {
            IsAbnormal = false;
            return;
        }
        
        try
        {
            IsAbnormal = _abnormalDetector.IsGroupAbnormal(_fileGroup);
            if (IsAbnormal)
            {
                AbnormalReason = "Detected by z-score analysis";
            }
        }
        catch (Exception)
        {
            IsAbnormal = false;
        }
    }
    
    // 기존 속성들...
}
```

---

### 2.2 MainWindowViewModel에 AbnormalCount 속성 추가

#### 수정 파일: `ChronoView/UI/ViewModels/MainWindowViewModel.cs`

```csharp
// 필드 추가
private int _abnormalCount;

// 속성 추가
public int AbnormalCount
{
    get => _abnormalCount;
    set => SetProperty(ref _abnormalCount, value);
}

// UpdateStatistics 메서드 수정
private void UpdateStatistics()
{
    TotalGroups = FileGroups.Count;
    WithNirCount = FileGroups.Count(g => g.HasNirImage);
    WithoutNirCount = FileGroups.Count(g => !g.HasNirImage);
    FailedCount = FileGroups.Count(g => g.IsFailed);
    AbnormalCount = FileGroups.Count(g => g.IsAbnormal);  // 추가
    
    MatchRate = TotalGroups > 0 
        ? (double)WithNirCount / TotalGroups * 100 
        : 0;
}
```

---

### 2.3 FileGroupViewModel 생성 시 IAbnormalDetector 주입

#### 수정 파일: `ChronoView/UI/ViewModels/MainWindowViewModel.cs`

```csharp
// 필드 추가
private readonly IAbnormalDetector _abnormalDetector;

// 생성자 수정
public MainWindowViewModel(
    IMonitoringOrchestrator orchestrator,
    IStatisticsService statisticsService,
    IConfigurationManager configManager,
    IFileOperationService fileOperationService,
    IPathManagementService pathManagementService,
    IImageProcessor imageProcessor,
    IAbnormalDetector abnormalDetector,  // 추가
    ILogger<MainWindowViewModel> logger)
{
    // ...
    _abnormalDetector = abnormalDetector;
    // ...
}

// OnGroupCreated 이벤트 핸들러 수정
private void OnGroupCreated(object? sender, FileGroup group)
{
    Application.Current.Dispatcher.InvokeAsync(() =>
    {
        // FileGroupViewModel 생성 시 abnormalDetector 전달
        var viewModel = new FileGroupViewModel(
            group, 
            _imageProcessor, 
            _abnormalDetector);
        
        AddFileGroup(viewModel);
        UpdateStatistics();
    });
}
```

---

### 2.4 DI 컨테이너 등록 수정

#### 수정 파일: `ChronoView/App.xaml.cs`

```csharp
private void ConfigureServices(IServiceCollection services)
{
    // 기존 서비스들...
    
    // Abnormal Detector 추가
    services.AddSingleton<IAbnormalDetector, AbnormalDetectorService>();
    
    // ...
}
```

---

## 3. DragSelectBehavior 연결

### 3.1 MainWindow.xaml 수정

#### 수정 파일: `ChronoView/MainWindow.xaml`

```xml
<Window x:Class="ChronoView.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:i="http://schemas.microsoft.com/xaml/behaviors"
        xmlns:behaviors="clr-namespace:ChronoView.UI.Behaviors"
        ...>
    
    <!-- DataGrid에 Behavior 추가 -->
    <DataGrid ItemsSource="{Binding FileGroups}" 
              SelectedItem="{Binding SelectedGroup}"
              SelectionMode="Extended"
              Style="{StaticResource FileGroupDataGridStyle}"
              RowStyle="{StaticResource FileGroupRowStyle}">
        
        <i:Interaction.Behaviors>
            <behaviors:DragSelectBehavior/>
        </i:Interaction.Behaviors>
        
        <DataGrid.Columns>
            <!-- columns... -->
        </DataGrid.Columns>
    </DataGrid>
    
</Window>
```

### 3.2 NuGet 패키지 확인

`Microsoft.Xaml.Behaviors.Wpf` 패키지가 설치되어 있는지 확인:

```xml
<!-- ChronoView.csproj -->
<PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.77" />
```

---

## 4. Settings Dialog Browse 버튼 구현

### 4.1 SettingsDialogViewModel 수정

#### 수정 파일: `ChronoView/UI/ViewModels/SettingsDialogViewModel.cs`

```csharp
public class SettingsDialogViewModel : ViewModelBase
{
    // Browse 커맨드들 추가
    public ICommand BrowseNirPathCommand { get; }
    public ICommand BrowseNormalPathCommand { get; }
    public ICommand BrowseCamera1PathCommand { get; }
    public ICommand BrowseCamera2PathCommand { get; }
    public ICommand BrowseCamera3PathCommand { get; }
    public ICommand BrowseOutputPathCommand { get; }
    
    public SettingsDialogViewModel(IConfigurationManager configManager, ILogger<SettingsDialogViewModel> logger)
    {
        // ...
        
        BrowseNirPathCommand = new RelayCommand(() => BrowseFolder(path => NirPath = path));
        BrowseNormalPathCommand = new RelayCommand(() => BrowseFolder(path => NormalPath = path));
        BrowseCamera1PathCommand = new RelayCommand(() => BrowseFolder(path => Camera1Path = path));
        BrowseCamera2PathCommand = new RelayCommand(() => BrowseFolder(path => Camera2Path = path));
        BrowseCamera3PathCommand = new RelayCommand(() => BrowseFolder(path => Camera3Path = path));
        BrowseOutputPathCommand = new RelayCommand(() => BrowseFolder(path => OutputPath = path));
    }
    
    private void BrowseFolder(Action<string> setPath)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select folder",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };
        
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            setPath(dialog.SelectedPath);
        }
    }
}
```

### 4.2 SettingsDialog.xaml 수정

#### 수정 파일: `ChronoView/UI/Views/SettingsDialog.xaml`

```xml
<StackPanel Orientation="Horizontal" Margin="0,5">
    <Label Content="NIR Path:" Style="{StaticResource LabelStyle}"/>
    <TextBox Text="{Binding NirPath}" Width="400" Margin="0,0,5,0"/>
    <Button Content="Browse..." Width="80" Command="{Binding BrowseNirPathCommand}"/>
</StackPanel>

<StackPanel Orientation="Horizontal" Margin="0,5">
    <Label Content="Normal Path:" Style="{StaticResource LabelStyle}"/>
    <TextBox Text="{Binding NormalPath}" Width="400" Margin="0,0,5,0"/>
    <Button Content="Browse..." Width="80" Command="{Binding BrowseNormalPathCommand}"/>
</StackPanel>

<!-- 나머지 경로들도 동일하게 수정 -->
```

### 4.3 NuGet 패키지 또는 어셈블리 참조

WinForms FolderBrowserDialog 사용을 위해:
```xml
<!-- ChronoView.csproj -->
<UseWindowsForms>true</UseWindowsForms>
```

또는 순수 WPF 방식 (CommonOpenFileDialog):
```xml
<PackageReference Include="Microsoft.WindowsAPICodePack-Shell" Version="1.1.0" />
```

---

## 5. Line2 및 Combined 탭 구현

### 5.1 Line2 탭 구현

#### 수정 파일: `ChronoView/MainWindow.xaml`

```xml
<TabItem Header="Line 2">
    <DataGrid ItemsSource="{Binding Line2Groups}" 
              SelectedItem="{Binding SelectedGroup}"
              SelectionMode="Extended"
              Style="{StaticResource FileGroupDataGridStyle}"
              RowStyle="{StaticResource FileGroupRowStyle}">
        
        <i:Interaction.Behaviors>
            <behaviors:DragSelectBehavior/>
        </i:Interaction.Behaviors>
        
        <DataGrid.Columns>
            <!-- Line1과 동일한 컬럼 정의 -->
            <DataGridCheckBoxColumn Header="" Width="40" Binding="{Binding IsSelected}"/>
            <DataGridTextColumn Header="Index" Width="80" Binding="{Binding GroupId}"/>
            <DataGridTextColumn Header="Status" Width="100" Binding="{Binding StatusText}"/>
            <!-- ... 나머지 이미지 컬럼들 ... -->
        </DataGrid.Columns>
    </DataGrid>
</TabItem>
```

### 5.2 Combined 탭 구현

```xml
<TabItem Header="Combined">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="5"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        
        <!-- Line 1 DataGrid -->
        <DataGrid Grid.Column="0"
                  ItemsSource="{Binding Line1Groups}" 
                  SelectedItem="{Binding SelectedGroup}"
                  Style="{StaticResource FileGroupDataGridStyle}"
                  RowStyle="{StaticResource FileGroupRowStyle}">
            <DataGrid.Columns>
                <!-- Line1 컬럼들 -->
            </DataGrid.Columns>
        </DataGrid>
        
        <GridSplitter Grid.Column="1" HorizontalAlignment="Stretch" 
                      Background="{StaticResource BorderBrush}"/>
        
        <!-- Line 2 DataGrid -->
        <DataGrid Grid.Column="2"
                  ItemsSource="{Binding Line2Groups}" 
                  SelectedItem="{Binding SelectedGroup}"
                  Style="{StaticResource FileGroupDataGridStyle}"
                  RowStyle="{StaticResource FileGroupRowStyle}">
            <DataGrid.Columns>
                <!-- Line2 컬럼들 -->
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
```
}
```

---

### 6.1 MoveFileGroupAsync 구현 (Refactored)

> **Refactor 요구사항 (2025-12-10):**
> - **중복 파일명 처리**: 자동 이름 변경 대신 사용자 결정(Overwrite/Skip) 지원.
> - **OperationResult**: 실패한 파일 목록(`FailedFiles`) 및 정확한 실패 카운트 제공.
> - **Normal 폴더**: `FileGroup.GetAllFilePaths()`가 반환하는 개별 파일 단위 이동 유지.

#### 1. Data Structures

```csharp
public enum ConflictResolution
{
    Overwrite,
    Skip,
    Abort // 선택적
}

public class OperationResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public int FilesProcessed { get; set; }
    public int FilesFailed { get; set; }
    public List<string> FailedFiles { get; } = new(); // 추가
}
```

#### 2. Service Interface

```csharp
Task<OperationResult> MoveFileGroupAsync(
    FileGroup group,
    string destinationPath,
    IProgress<OperationProgress>? progress = null,
    Func<string, ConflictResolution>? onConflict = null, // 추가
    CancellationToken cancellationToken = default);
```

#### 3. Implementation Logic

```csharp
public async Task<OperationResult> MoveFileGroupAsync(
    FileGroup group,
    string destinationPath,
    IProgress<OperationProgress>? progress = null,
    Func<string, ConflictResolution>? onConflict = null, // 추가
    CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Moving file group {GroupId} to {Destination}", 
        group.GroupId, destinationPath);
    
    var result = new OperationResult();
    var allFiles = group.GetAllFilePaths().Where(File.Exists).ToList();
    
    if (!allFiles.Any())
    {
        result.Success = false;
        result.ErrorMessage = "No files to move";
        return result;
    }
    
    // 대상 폴더 생성
    Directory.CreateDirectory(destinationPath);
    
    var movedFiles = new List<(string source, string dest)>();
#### 3. Implementation Logic (Refined - Copy-then-Delete 방식)

```csharp
public async Task<OperationResult> MoveFileGroupAsync(...) {
    // 1. Conflict State
    ConflictResolution? stickyResolution = null; // Stores "Apply to All" decision

    // 2. Normal Folder Handling (Copy-then-Delete for Stability)
    if (!string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder)) {
        string destNormalPath = Path.Combine(destinationPath, Path.GetFileName(group.NormalFolder));

        if (Directory.Exists(destNormalPath)) {
            // Conflict handling for directory
            var resolution = stickyResolution ?? onConflict?.Invoke(destNormalPath) ?? ConflictResolution.Skip;
            if (resolution == ConflictResolution.Overwrite) {
                 // Delete existing and copy
                 Directory.Delete(destNormalPath, recursive: true);
            } else if (resolution == ConflictResolution.Skip) {
                 // Skip directory copy
                 goto SkipNormalFolder;
            }
            if (resolution != ConflictResolution.Abort && stickyResolution == null) stickyResolution = resolution;
        }

        // Copy directory recursively
        await CopyDirectoryAsync(group.NormalFolder, destNormalPath, cancellationToken);

        // Verify copy success
        if (VerifyDirectoryCopy(group.NormalFolder, destNormalPath)) {
            // Delete original after successful copy
            Directory.Delete(group.NormalFolder, recursive: true);
        } else {
            throw new IOException("Directory copy verification failed");
        }
    }
SkipNormalFolder:
    ;
    }

    // 3. File Handling (Loop - Copy-then-Delete)
    foreach (var file in files) {
        // ...
        if (File.Exists(destPath)) {
            var resolution = stickyResolution ?? onConflict?.Invoke(destPath) ?? ConflictResolution.Skip;

            // "Apply to All" logic: Store the resolution if it's the first conflict
            if (stickyResolution == null) stickyResolution = resolution;

            if (resolution == ConflictResolution.Skip) {
                // Skips are counted as processed (success)
                result.FilesProcessed++;
                continue;
            }
            else if (resolution == ConflictResolution.Overwrite) {
                // Proceed to overwrite (will be deleted before copy)
                File.Delete(destPath);
            }
            // ...
        }

        // Copy file then delete original
        File.Copy(file, destPath, overwrite: false); // Already handled above

        // Verify copy
        if (new FileInfo(file).Length == new FileInfo(destPath).Length) {
            File.Delete(file); // Delete original after verification
        } else {
            throw new IOException($"File copy verification failed: {file}");
        }
    }
}
```

> **Key Changes:**
> - **Sticky Resolution**: 첫 번째 충돌 해결책을 저장하여 이후 충돌에 자동 적용.
> - **Skip as Processed**: Skip된 파일도 처리된 것으로 집계.
> - **Normal Folder**: 별도 처리 로직 추가 (Copy-then-Delete 방식으로 안정성 강화).
> - **Copy-then-Delete**: 모든 파일/폴더 이동 시 복사 → 검증 → 삭제 순서로 안전하게 처리.
        
        // ====== 롤백 (개선: 실패해도 계속 시도) ======
        var rollbackFailed = new List<string>();
        foreach (var (source, dest) in movedFiles)
        {
            try
            {
                if (File.Exists(dest) && !File.Exists(source))
                {
                    File.Move(dest, source, overwrite: true); // Rollback should overwrite if original exists
                }
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Rollback failed for {File}", dest);
                rollbackFailed.Add(dest);
                result.FailedFiles.Add(dest); // Add to failed files
            }
        }
        
        // Calculate FilesFailed more accurately after rollback attempts
        result.FilesFailed += allFiles.Count - result.FilesProcessed - movedFiles.Count; // Files that failed before rollback
        result.FilesFailed += rollbackFailed.Count; // Files that failed during rollback
        
        if (rollbackFailed.Any())
        {
            result.ErrorMessage += $" (Rollback failed for {rollbackFailed.Count} files)";
        }
    }
    
    return result;
}

/// <summary>
/// 중복 파일명 방지를 위한 Unique 파일명 생성
/// </summary>
private string GetUniqueDestFileName(string fileName, string sourceFolder, HashSet<string> usedNames)
{
    // 첫 시도: 원본 파일명
    if (!usedNames.Contains(fileName))
        return fileName;
    
    // 두 번째 시도: 폴더명_파일명 (예: NIR_image.jpg, Cam1_image.jpg)
    var prefixedName = $"{sourceFolder}_{fileName}";
    if (!usedNames.Contains(prefixedName))
        return prefixedName;
    
    // 세 번째 시도: 숫자 suffix
    var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
    var ext = Path.GetExtension(fileName);
    for (int i = 1; i < 100; i++)
    {
        var suffixedName = $"{sourceFolder}_{nameWithoutExt}_{i}{ext}";
        if (!usedNames.Contains(suffixedName))
            return suffixedName;
    }
    
    // 극단적 경우: GUID 사용
    return $"{sourceFolder}_{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
}
```

### 6.2 DeleteFileGroupAsync 구현

> **소프트 삭제(Trash/Quarantine 이동) 정책**
> - 물리 삭제 대신 설정된 보관 폴더(`DeleteQuarantinePath`)로 이동한다.
> - 보관 폴더 경로는 설정 UI를 통해 사용자가 선택/저장할 수 있어야 하며, `WorkflowSettings.DeleteQuarantinePath`에 저장한다. 비어 있으면 기본값(BasePath/Trash 등)을 사용한다.
> - Normal 폴더는 디렉터리 단위로 `Directory.Move(source, trashDestUnique)` 처리하고, 보관 폴더 내 충돌 시 Overwrite/Skip/Abort(Apply-to-All) 정책을 동일하게 적용한다.
> - 개별 파일도 `File.Move(source, trashDestUnique, overwrite?)`로 이동하며, Overwrite/Skip은 처리로 집계하고 실패로 기록하지 않는다.
> - 성공 시 `FilesProcessed`에 포함, 실패 시 `FailedFiles`/`FilesFailed`에 추가한다.
> - 보관 폴더 경로는 설정(`MatchingSettings` 또는 `WorkflowSettings`)의 `DeleteQuarantinePath`에서 읽는다(없으면 기본값 사용).

```csharp
public async Task<OperationResult> DeleteFileGroupAsync(
    FileGroup group,
    IProgress<OperationProgress>? progress = null,
    CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Deleting file group {GroupId}", group.GroupId);
    
    var result = new OperationResult();
    var allFiles = group.GetAllFilePaths().Where(File.Exists).ToList();
    
    if (!allFiles.Any())
    {
        result.Success = true;
        return result;
    }
    
    try
    {
        for (int i = 0; i < allFiles.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var filePath = allFiles[i];
            var fileName = Path.GetFileName(filePath);
            
            progress?.Report(new OperationProgress
            {
                TotalFiles = allFiles.Count,
                ProcessedFiles = i,
                CurrentFile = fileName
            });
            
            await Task.Run(() => File.Delete(filePath), cancellationToken);
            result.FilesProcessed++;
        }
        
        result.Success = true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting files");
        result.ErrorMessage = ex.Message;
        result.FilesFailed = allFiles.Count - result.FilesProcessed;
    }
    
    return result;
}
```

---

## 7. PathManagementService 구현 (Line1/Line2 일관성 적용)

### 7.1 GeneratePathsFromDate 구현

#### 수정 파일: `ChronoView/Core/FileOperations/PathManagementService.cs`

> **Line1/Line2 일관성:**
> - Line 1: NIR1, Normal1, Cam1-3
> - Line 2: NIR2, Normal2, Cam4-6
> - Output 폴더 공통

```csharp
public Dictionary<string, string> GeneratePathsFromDate(
    string dateString, 
    ApplicationConfiguration config)
{
    _logger.LogInformation("Generating paths from date: {Date}", dateString);
    
    var paths = new Dictionary<string, string>();
    
    if (!DateTime.TryParseExact(dateString, "yyyyMMdd", 
        CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
    {
        _logger.LogWarning("Invalid date format: {Date}", dateString);
        return paths;
    }
    
    // 기본 경로 패턴: BasePath/{YYYY}/{MM}/{DD}/
    var basePath = config.BasePath ?? "D:/Data";
    var datePath = Path.Combine(basePath, 
        date.Year.ToString(), 
        date.Month.ToString("D2"), 
        date.Day.ToString("D2"));
    
    // ====== Line 1 경로 ======
    paths["NIR1"] = Path.Combine(datePath, "NIR1");
    paths["Normal1"] = Path.Combine(datePath, "Normal1");
    paths["Cam1"] = Path.Combine(datePath, "Cam1");
    paths["Cam2"] = Path.Combine(datePath, "Cam2");
    paths["Cam3"] = Path.Combine(datePath, "Cam3");
    
    // ====== Line 2 경로 ======
    paths["NIR2"] = Path.Combine(datePath, "NIR2");
    paths["Normal2"] = Path.Combine(datePath, "Normal2");
    paths["Cam4"] = Path.Combine(datePath, "Cam4");
    paths["Cam5"] = Path.Combine(datePath, "Cam5");
    paths["Cam6"] = Path.Combine(datePath, "Cam6");
    
    // ====== 공통 경로 ======
    paths["Output"] = Path.Combine(datePath, "Output");
    
    return paths;
}
```

### 7.2 CreateSampleFoldersAsync 구현 (Line1/Line2 전체 지원)

> **Line1/Line2 일관성:**
> - 모든 라인 경로에 샘플 폴더 생성 (NIR1/2, Normal1/2, Cam1-6)

```csharp
public async Task<bool> CreateSampleFoldersAsync(
    string sampleName,
    ApplicationConfiguration config,
    CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Creating sample folders for: {SampleName}", sampleName);
    
    try
    {
        var settings = config.MatchingSettings;
        
        // ====== Line 1 + Line 2 전체 경로 수집 ======
        var basePaths = new[]
        {
            // Line 1
            settings.Nir1Path,
            settings.Normal1Path,
            settings.Camera1Path,
            settings.Camera2Path,
            settings.Camera3Path,
            // Line 2
            settings.Nir2Path,
            settings.Normal2Path,
            settings.Camera4Path,
            settings.Camera5Path,
            settings.Camera6Path
        }.Where(p => !string.IsNullOrEmpty(p));
        
        var createdCount = 0;
        foreach (var basePath in basePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var samplePath = Path.Combine(basePath, sampleName);
            
            if (!Directory.Exists(samplePath))
            {
                await Task.Run(() => Directory.CreateDirectory(samplePath), 
                    cancellationToken);
                createdCount++;
                _logger.LogInformation("Created folder: {Path}", samplePath);
            }
            else
            {
                _logger.LogDebug("Folder already exists: {Path}", samplePath);
            }
        }
        
        _logger.LogInformation("Created {Count} sample folders for '{Name}'", 
            createdCount, sampleName);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create sample folders");
        return false;
    }
}
```

---

## 구현 순서 권장

1. **Phase 1 - 데이터 바인딩 수정** (필수)
   - FileGroupViewModel에 IsAbnormal 속성 추가
   - MainWindowViewModel에 AbnormalCount 속성 추가
   - DI 등록 수정

2. **Phase 2 - 서비스 구현**
   - FileOperationService.MoveFileGroupAsync 구현
   - FileOperationService.DeleteFileGroupAsync 구현
   - PathManagementService 구현

3. **Phase 3 - Command 연결**
   - ExecuteMove 비동기 구현
   - ExecuteDelete 비동기 구현
   - ExecuteRefresh 비동기 구현
   - ExecutePathAutoConfig 구현
   - ExecuteCreateSampleFolder 구현

4. **Phase 4 - UI 개선**
   - DragSelectBehavior 연결
   - Browse 버튼 기능 구현
   - Line2/Combined 탭 완성

---

## 테스트 요구사항

각 구현 항목에 대해 다음 테스트 필요:

| 항목 | 테스트 유형 | 검증 내용 |
|------|-------------|-----------|
| IsAbnormal | Unit Test | 속성 바인딩 정상 작동 |
| AbnormalCount | Unit Test | 카운트 정확성 |
| ExecuteMove | Integration Test | 파일 이동 및 롤백 |
| ExecuteDelete | Integration Test | 파일 삭제 및 확인 다이얼로그 |
| ExecuteRefresh | Integration Test | 컬렉션 갱신 |
| DragSelectBehavior | Manual Test | 다중 선택 UX |
| Browse 버튼 | Manual Test | 폴더 선택 다이얼로그 |
