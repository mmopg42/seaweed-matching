# 로그 저장 기능 개선 계획

## 현재 상황

### 로그 시스템 구조 (두 가지 독립적인 시스템)

1. **터미널 로그 (개발용 - Microsoft.Extensions.Logging)**
   - 목적: 개발자를 위한 상세한 디버그 정보
   - 출력: 콘솔 창 또는 디버그 출력 창
   - 설정: App.xaml.cs에서 `configure.AddConsole()`, `configure.AddDebug()` 설정
   - 현재 상태: 파일 저장 기능 없음
   - 사용 예: `_logger.LogInformation()`, `_logger.LogError()` 등

2. **UI 로그 (사용자용 - MainWindowViewModel.LogMessages)**
   - 목적: 사용자가 현재 상태를 확인하기 위한 정보
   - 출력: UI 패널 (LogPanel)
   - 설정: MainWindowViewModel.AddLogMessage()로 추가
   - 현재 상태: 파일 저장 기능 없음
   - 사용 예: "모니터링 시작", "파일 이동 완료" 등 사용자 친화적 메시지

### 중요: 두 로그는 목적이 다르므로 저장도 따로 되어야 함

3. **수동 Export 기능** (LogPanel.xaml.cs Line 182-211)
   - Export 버튼 클릭 시 파일 저장 대화상자 표시
   - CSV 또는 TXT 형식으로 저장 가능
   - 필터링된 로그만 저장

4. **자동 저장 기능 (현재 작동하지 않음)**
   - LogPanel.AddLogMessage()에 WriteToLogFile() 메서드가 있지만
   - 실제로는 MainWindowViewModel.AddLogMessage()가 사용됨
   - MainWindowViewModel.AddLogMessage()는 파일 저장을 하지 않음
   - 따라서 자동 저장이 작동하지 않음

### 문제점

1. **터미널 로그 파일 저장 기능 없음**
   - 개발용 상세 로그가 파일로 저장되지 않음
   - 디버깅 시 로그를 확인하기 어려움

2. **UI 로그 파일 저장 기능 없음**
   - MainWindowViewModel.AddLogMessage()에 파일 저장 기능이 없음
   - 사용자가 확인한 상태 정보가 저장되지 않음

3. **자동 저장 경로가 UI에 표시되지 않음**
   - 저장된 로그 파일에 접근하기 어려움
   - 자동 저장 경로를 열 수 있는 기능 없음

## 개선 목표

1. **터미널 로그 파일 저장 기능 구현** (개발용)
   - Microsoft.Extensions.Logging에 파일 로거 추가
   - 개발용 상세 로그를 파일로 저장
   - 저장 경로: `%AppData%\ChronoView\Logs\ChronoView_Debug_YYYYMMDD.log`
   - 날짜별로 자동 분리 저장
   - **세션 시작 시간 기록**: 파일 시작 부분에 실행 시간 기록

2. **UI 로그 파일 저장 기능 구현** (사용자용)
   - MainWindowViewModel.AddLogMessage()에 파일 저장 기능 추가
   - 사용자 상태 정보를 파일로 저장
   - 저장 경로: `%AppData%\ChronoView\Logs\ChronoView_UI_YYYYMMDD.log`
   - 날짜별로 자동 분리 저장
   - **세션 시작 시간 기록**: 파일 시작 부분에 실행 시간 기록

3. **로그 파일 자동 삭제 기능** (필수)
   - 오래된 로그 파일 자동 삭제
   - 기본값: 30일 (한달)
   - 설정에서 보관 기간 조정 가능

4. **로그 보관 기간 설정** (필수)
   - SettingsDialog에 로그 보관 기간 설정 추가
   - WorkflowSettings에 LogRetentionDays 속성 추가
   - 사용자가 원하는 기간(일 단위)으로 설정 가능

5. **로그 폴더 열기 기능** (필수)
   - 로그 폴더 열기 버튼 추가
   - 터미널 로그(`ChronoView_Debug_*.log`)와 UI 로그(`ChronoView_UI_*.log`) 모두 포함된 폴더 열기
   - 저장 경로: `%AppData%\ChronoView\Logs\`

6. **빠른 저장 기능** (선택사항)
   - 현재 필터된 UI 로그를 기본 경로에 바로 저장
   - 파일명: `ChronoView_UI_Export_YYYYMMDD_HHmmss.txt`

## 변경 사항

### 1. LogPanel.xaml 수정

**위치**: Line 59-67 (버튼 영역)

**현재 구조**:
```xml
<StackPanel Grid.Column="2" Orientation="Horizontal">
    <CheckBox .../>
    <Button Content="{x:Static res:Strings.Log_Clear}" .../>
    <Button Content="{x:Static res:Strings.Log_Export}" .../>
    <Button Content="✕" .../>
</StackPanel>
```

**변경 후 구조**:
```xml
<StackPanel Grid.Column="2" Orientation="Horizontal">
    <CheckBox .../>
    <Button Content="{x:Static res:Strings.Log_Clear}" .../>
    <Button Content="{x:Static res:Strings.Log_Export}" .../>
    <Button Content="{x:Static res:Strings.Log_QuickSave}" 
            Click="QuickSave_Click" 
            ToolTip="{x:Static res:Strings.Tooltip_QuickSaveLog}"
            Padding="8,4" Margin="4,0"/>
    <Button Content="✕" .../>
</StackPanel>
```

**로그 폴더 열기 버튼 추가**:
```xml
<StackPanel Grid.Column="2" Orientation="Horizontal">
    <Button Content="📁" 
            Click="OpenLogFolder_Click"
            ToolTip="로그 폴더 열기"
            Width="30" Height="30"
            Background="Transparent"
            BorderThickness="0"
            Padding="0"
            Margin="4,0"
            Cursor="Hand"/>
    <CheckBox .../>
    <Button Content="{x:Static res:Strings.Log_Clear}" .../>
    <Button Content="{x:Static res:Strings.Log_Export}" .../>
    <Button Content="✕" .../>
</StackPanel>
```

**참고**: 로그 폴더에는 다음 파일들이 포함됨
- `ChronoView_Debug_YYYYMMDD.log` (터미널 로그 - 개발용)
- `ChronoView_UI_YYYYMMDD.log` (UI 로그 - 사용자용)
- `ChronoView_UI_Export_YYYYMMDD_HHmmss.txt` (수동 저장 파일)

### 2. ApplicationConfiguration.cs 수정 (로그 보관 기간 설정 추가)

**위치**: WorkflowSettings 클래스 (Line 305-366)

**추가할 속성**:
```csharp
/// <summary>
/// 로그 파일 보관 기간 (일 단위).
/// 이 기간이 지난 로그 파일은 자동으로 삭제됩니다.
/// 기본값: 30일 (한달)
/// </summary>
public int LogRetentionDays { get; set; } = 30;
```

### 3. App.xaml.cs 수정 (터미널 로그 파일 저장)

**위치**: Line 167-173 (ConfigureServices 메서드)

**현재 코드**:
```csharp
services.AddLogging(configure =>
{
    configure.AddConsole();
    configure.AddDebug();
    configure.SetMinimumLevel(LogLevel.Debug);
});
```

**변경 후 코드**:
```csharp
services.AddLogging(configure =>
{
    configure.AddConsole();
    configure.AddDebug();
    configure.SetMinimumLevel(LogLevel.Debug);
    
    // 개발용 로그 파일 저장 추가
    var logDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChronoView",
        "Logs");
    Directory.CreateDirectory(logDir);
    
    var logFile = Path.Combine(logDir, $"ChronoView_Debug_{DateTime.Now:yyyyMMdd}.log");
    
    // 세션 시작 시간 기록 (파일이 새로 생성될 때만)
    // 로직: 파일이 존재하지 않거나 크기가 0이면 새 파일이므로 세션 시작 시간 기록
    // 주의: ConfigureServices는 애플리케이션 시작 시 한 번만 실행되므로,
    // 같은 날짜에 여러 번 실행되어도 같은 파일에 추가됨 (Serilog의 rollingInterval이 날짜별로 분리)
    if (!File.Exists(logFile) || new FileInfo(logFile).Length == 0)
    {
        var sessionStartTime = DateTime.Now;
        var sessionHeader = $"\n{'='.PadRight(80, '=')}\n" +
                           $"Session Started: {sessionStartTime:yyyy-MM-dd HH:mm:ss}\n" +
                           $"{'='.PadRight(80, '=')}\n\n";
        File.AppendAllText(logFile, sessionHeader);
    }
    
    // Serilog 또는 FileLoggerProvider 사용 (아래 구현 방법 참고)
});
```

**필요한 using 추가**:
```csharp
using System.IO;
using Microsoft.Extensions.Logging;
```

**참고**: `AddFile` 확장 메서드는 Microsoft.Extensions.Logging에 기본 제공되지 않습니다.

**구현 방법**:

**옵션 1: Serilog 사용 (권장)**
- NuGet 패키지 추가: `Serilog.Extensions.Logging` 및 `Serilog.Sinks.File`
- 코드 예시:
```csharp
using Serilog;
using Serilog.Extensions.Logging;

// ConfigureServices 메서드 내
var serilogLogger = new LoggerConfiguration()
    .WriteTo.File(logFile, 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: null, // LogCleanupService에서 관리
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

configure.AddSerilog(serilogLogger);
```

**옵션 2: 직접 FileLoggerProvider 구현**
- 간단한 래퍼 클래스로 파일에 직접 쓰기
- 기존 패턴을 따라 직접 구현
- 코드 예시:
```csharp
// 새 파일: ChronoView/Infrastructure/Logging/FileLoggerProvider.cs
public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logFilePath;

    public FileLoggerProvider(string logFilePath)
    {
        _logFilePath = logFilePath;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _logFilePath);
    }

    public void Dispose() { }
}

// 사용:
configure.AddProvider(new FileLoggerProvider(logFile));
```

### 4. MainWindowViewModel.cs 수정 (UI 로그 파일 저장)

**위치**: Line 171-177 (AddLogMessage 메서드)

**현재 코드**:
```csharp
public void AddLogMessage(LogSeverity severity, string source, string message)
{
    WpfApplication.Current.Dispatcher.Invoke(() => {
        LogMessages.Add(new LogMessage(severity, source, message));
        while (LogMessages.Count > 1000) LogMessages.RemoveAt(0);
    });
}
```

**변경 후 코드**:
```csharp
public void AddLogMessage(LogSeverity severity, string source, string message)
{
    var logMessage = new LogMessage(severity, source, message);
    
    WpfApplication.Current.Dispatcher.Invoke(() => {
        LogMessages.Add(logMessage);
        while (LogMessages.Count > 1000) LogMessages.RemoveAt(0);
    });
    
    // UI 로그 파일 저장 (사용자용)
    WriteToUILogFile(logMessage);
}

private static DateTime? _sessionStartTime = null;
private static readonly object _sessionLock = new object();

private void WriteToUILogFile(LogMessage message)
{
    try
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ChronoView",
            "Logs");

        Directory.CreateDirectory(logDir);

        var logFile = Path.Combine(logDir, $"ChronoView_UI_{DateTime.Now:yyyyMMdd}.log");
        
        // 세션 시작 시간 기록 (파일이 새로 생성될 때만)
        // 로직 설명:
        // 1. _sessionStartTime이 null이면 첫 실행이므로 세션 시작 시간 기록
        // 2. 파일이 존재하지 않으면 새 날짜 파일이므로 세션 시작 시간 업데이트 및 기록
        // 3. 파일이 존재하지만 크기가 0이면 빈 파일이므로 세션 시작 시간 기록
        // 4. lock을 사용하여 멀티스레드 환경에서 중복 기록 방지
        lock (_sessionLock)
        {
            bool shouldWriteSessionHeader = false;
            
            if (_sessionStartTime == null)
            {
                // 첫 실행: 세션 시작 시간 설정
                _sessionStartTime = DateTime.Now;
                shouldWriteSessionHeader = true;
            }
            else if (!File.Exists(logFile))
            {
                // 파일이 존재하지 않음: 새 날짜 파일이므로 세션 시작 시간 업데이트
                _sessionStartTime = DateTime.Now;
                shouldWriteSessionHeader = true;
            }
            else
            {
                // 파일이 존재: 크기 확인
                var fileInfo = new FileInfo(logFile);
                if (fileInfo.Length == 0)
                {
                    // 빈 파일: 세션 시작 시간 업데이트 및 기록
                    _sessionStartTime = DateTime.Now;
                    shouldWriteSessionHeader = true;
                }
            }
            
            if (shouldWriteSessionHeader && _sessionStartTime.HasValue)
            {
                var sessionHeader = $"\n{'='.PadRight(80, '=')}\n" +
                                   $"Session Started: {_sessionStartTime.Value:yyyy-MM-dd HH:mm:ss}\n" +
                                   $"{'='.PadRight(80, '=')}\n\n";
                File.AppendAllText(logFile, sessionHeader);
            }
        }

        var logEntry = $"[{message.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{message.Severity}] [{message.Source}] {message.Message}";

        File.AppendAllText(logFile, logEntry + Environment.NewLine);
    }
    catch
    {
        // Silently fail if we can't write to log file
    }
}
```

**필요한 using 추가**:
```csharp
using System.IO;
```

**참고**: 
- 파일명을 `ChronoView_UI_`로 변경하여 터미널 로그(`ChronoView_Debug_`)와 구분
- UI 로그는 사용자가 보는 상태 정보만 저장
- `_sessionStartTime`이 static이므로 같은 날짜에 여러 세션이 실행되면 첫 번째 세션 시간만 기록됨
  - 이는 의도된 동작: 같은 날짜의 첫 세션 시작 시간만 기록하여 파일을 깔끔하게 유지
  - 각 세션마다 기록하려면 날짜별로 세션 시작 시간을 관리하는 로직 추가 필요 (선택사항)

### 5. 로그 파일 자동 삭제 서비스 추가

**새 파일 생성**: `ChronoView/Core/Logging/LogCleanupService.cs`

```csharp
using System.IO;
using Microsoft.Extensions.Logging;
using ChronoView.Models;
using ChronoView.Core.Configuration;

namespace ChronoView.Core.Logging;

/// <summary>
/// 로그 파일 자동 삭제 서비스
/// </summary>
public class LogCleanupService
{
    private readonly IConfigurationManager _configManager;
    private readonly ILogger<LogCleanupService> _logger;

    public LogCleanupService(
        IConfigurationManager configManager,
        ILogger<LogCleanupService> logger)
    {
        _configManager = configManager;
        _logger = logger;
    }

    /// <summary>
    /// 오래된 로그 파일을 삭제합니다.
    /// </summary>
    public void CleanupOldLogFiles()
    {
        try
        {
            var config = _configManager.LoadConfiguration<ApplicationConfiguration>();
            var retentionDays = config.WorkflowSettings?.LogRetentionDays ?? 30;
            var cutoffDate = DateTime.Now.AddDays(-retentionDays);

            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ChronoView",
                "Logs");

            if (!Directory.Exists(logDir))
                return;

            var logFiles = Directory.GetFiles(logDir, "ChronoView_*.log");
            int deletedCount = 0;

            foreach (var filePath in logFiles)
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.LastWriteTime < cutoffDate)
                {
                    try
                    {
                        File.Delete(filePath);
                        deletedCount++;
                        _logger.LogInformation("Deleted old log file: {FilePath} (LastWriteTime: {LastWriteTime})", 
                            filePath, fileInfo.LastWriteTime);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete log file: {FilePath}", filePath);
                    }
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation("Log cleanup completed: {DeletedCount} files deleted (retention: {RetentionDays} days)", 
                    deletedCount, retentionDays);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during log cleanup");
        }
    }
}
```

**App.xaml.cs에 서비스 등록 및 초기화**:

**위치 1: ConfigureServices 메서드 (서비스 등록)**
```csharp
// Line 165 근처, ConfigureServices 메서드 내
services.AddSingleton<LogCleanupService>();
```

**위치 2: OnStartup 메서드 (초기화 및 실행)**
```csharp
// Line 33 근처, OnStartup 메서드 내
// _serviceProvider가 생성된 후 (Line 68 이후)
protected override async void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    
    // ... 기존 코드 ...
    
    // 서비스 초기화 후 (Line 68 이후)
    await System.Threading.Tasks.Task.Run(async () =>
    {
        // ... 기존 서비스 구성 코드 ...
        _serviceProvider = services.BuildServiceProvider();
        
        // 로그 파일 정리 실행 (애플리케이션 시작 시)
        var cleanupService = _serviceProvider.GetRequiredService<LogCleanupService>();
        cleanupService.CleanupOldLogFiles();
        
        // ... 나머지 코드 ...
    });
}
```

**초기화 시점**:
- 애플리케이션 시작 시 한 번 실행
- 서비스 프로바이더가 생성된 직후 실행
- 로그 파일 저장 기능이 작동하기 전에 오래된 파일 정리
- 비동기로 실행하여 UI 블로킹 방지

### 6. SettingsDialog.xaml 수정 (로그 보관 기간 설정 UI 추가)

**위치**: Advanced Tab 또는 새로운 Logging Tab 추가

**Advanced Tab에 추가하는 경우** (Line 141-154):
```xml
<TextBlock Text="로그 설정" Style="{StaticResource SectionHeaderStyle}"/>
<StackPanel Orientation="Horizontal" Margin="0,5">
    <Label Content="로그 보관 기간 (일):" Style="{StaticResource LabelStyle}"/>
    <TextBox Text="{Binding LogRetentionDays}" Width="100"/>
    <TextBlock Text="(기본값: 30일)" VerticalAlignment="Center" Margin="10,0,0,0" Foreground="Gray"/>
</StackPanel>
```

### 7. SettingsDialogViewModel.cs 수정

**추가할 속성**:
```csharp
private int _logRetentionDays = 30;

public int LogRetentionDays
{
    get => _logRetentionDays;
    set => SetProperty(ref _logRetentionDays, value);
}
```

**LoadFromConfiguration() 메서드에 추가**:
```csharp
LogRetentionDays = _configuration.WorkflowSettings?.LogRetentionDays ?? 30;
```

**SaveToConfiguration() 메서드에 추가**:
```csharp
_configuration.WorkflowSettings.LogRetentionDays = LogRetentionDays;
```

### 8. LogPanel.xaml.cs 수정

**추가할 메서드**:

1. **QuickSave_Click**: 빠른 저장 기능
   - 기존 ExportToFile() 메서드 재사용 (Line 213-246)
   ```csharp
   private void QuickSave_Click(object sender, RoutedEventArgs e)
   {
       try
       {
           var logDir = Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
               "ChronoView",
               "Logs");
           
           Directory.CreateDirectory(logDir);
           
           var fileName = $"ChronoView_UI_Export_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
           var filePath = Path.Combine(logDir, fileName);
           
           ExportToFile(filePath);
           
           WpfMessageBox.Show(
               $"로그가 저장되었습니다:\n{filePath}",
               "저장 완료",
               MessageBoxButton.OK,
               MessageBoxImage.Information);
       }
       catch (Exception ex)
       {
           WpfMessageBox.Show(
               $"저장 중 오류가 발생했습니다:\n{ex.Message}",
               "저장 오류",
               MessageBoxButton.OK,
               MessageBoxImage.Error);
       }
   }
   ```

2. **OpenLogFolder_Click**: 로그 폴더 열기 (필수)
   - 터미널 로그와 UI 로그가 모두 저장된 폴더를 엽니다
   ```csharp
   private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
   {
       try
       {
           var logDir = Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
               "ChronoView",
               "Logs");
           
           Directory.CreateDirectory(logDir);
           
           // Windows 탐색기에서 폴더 열기
           System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
           {
               FileName = logDir,
               UseShellExecute = true
           });
       }
       catch (Exception ex)
       {
           WpfMessageBox.Show(
               $"폴더를 열 수 없습니다:\n{ex.Message}",
               "오류",
               MessageBoxButton.OK,
               MessageBoxImage.Error);
       }
   }
   ```
   
   **참고**: 
   - 폴더 경로: `%AppData%\ChronoView\Logs\`
   - 이 폴더에는 터미널 로그(`ChronoView_Debug_*.log`)와 UI 로그(`ChronoView_UI_*.log`)가 모두 저장됨
   - 사용자가 두 종류의 로그 파일을 모두 확인할 수 있음

### 5. Strings.resx 리소스 추가 (선택사항)

**참고**: 실제 코드는 하드코딩된 문자열을 사용하므로 리소스 추가는 선택사항입니다.

**추가할 리소스 (UI 버튼용 - 선택사항)**:
- `Tooltip_OpenLogFolder`: "로그 파일이 저장된 폴더를 엽니다" (하드코딩도 가능)
- `Log_QuickSave`: "빠른 저장" (빠른 저장 기능 사용 시)

**참고**: 
- 메시지 박스의 메시지는 하드코딩된 문자열 사용 (기존 코드 패턴과 일관성 유지)
- 로그 폴더 열기 버튼의 Tooltip도 하드코딩 가능 ("로그 폴더 열기")

## 구현 단계

### Phase 1: 로그 보관 기간 설정 추가
1. ApplicationConfiguration.WorkflowSettings에 LogRetentionDays 속성 추가 (기본값: 30일)
2. SettingsDialog.xaml에 로그 보관 기간 설정 UI 추가
3. SettingsDialogViewModel에 LogRetentionDays 속성 추가
4. 설정 저장/로드 기능 구현

### Phase 2: 터미널 로그 파일 저장 (개발용)
1. App.xaml.cs에 파일 로거 추가
2. Microsoft.Extensions.Logging.File 또는 Serilog 사용
3. 저장 경로: `ChronoView_Debug_YYYYMMDD.log`
4. 세션 시작 시간 기록 (파일 시작 부분)
5. 테스트: 개발 로그가 파일에 저장되는지 확인

### Phase 3: UI 로그 파일 저장 (사용자용)
1. MainWindowViewModel.AddLogMessage()에 WriteToUILogFile() 호출 추가
2. WriteToUILogFile() 메서드 구현
3. 저장 경로: `ChronoView_UI_YYYYMMDD.log`
4. 세션 시작 시간 기록 (파일이 새로 생성될 때만)
5. 테스트: UI 로그 메시지 추가 시 파일에 저장되는지 확인

### Phase 4: 로그 파일 자동 삭제 기능
1. LogCleanupService 클래스 생성
2. App.xaml.cs에 서비스 등록 및 초기화
3. 애플리케이션 시작 시 오래된 로그 파일 자동 삭제
4. 설정된 보관 기간보다 오래된 파일 삭제

### Phase 5: 로그 폴더 열기 기능 (필수)
1. LogPanel.xaml에 로그 폴더 열기 버튼 추가
2. OpenLogFolder_Click() 메서드 구현
3. 터미널 로그와 UI 로그가 모두 포함된 폴더 열기

### Phase 6: UI 개선 (선택)
1. 빠른 저장 버튼 추가 (선택사항)

## 예상 결과

### Phase 1 완료 후 (로그 보관 기간 설정)
- SettingsDialog에서 로그 보관 기간 설정 가능
- 기본값: 30일 (한달)
- 사용자가 원하는 기간으로 조정 가능

### Phase 2 완료 후 (터미널 로그)
- 개발용 상세 로그가 자동으로 파일에 저장됨
- 저장 경로: `%AppData%\ChronoView\Logs\ChronoView_Debug_YYYYMMDD.log`
- 날짜별로 자동 분리 저장
- **파일 시작 부분에 세션 시작 시간 기록**: "Session Started: YYYY-MM-DD HH:mm:ss"
- 디버깅 시 상세 로그 확인 가능

### Phase 3 완료 후 (UI 로그)
- 사용자 상태 정보가 자동으로 파일에 저장됨
- 저장 경로: `%AppData%\ChronoView\Logs\ChronoView_UI_YYYYMMDD.log`
- 날짜별로 자동 분리 저장
- **파일 시작 부분에 세션 시작 시간 기록**: "Session Started: YYYY-MM-DD HH:mm:ss"
- 사용자가 확인한 상태 정보 보존

### Phase 4 완료 후 (자동 삭제)
- 애플리케이션 시작 시 오래된 로그 파일 자동 삭제
- 설정된 보관 기간(기본 30일)보다 오래된 파일 삭제
- 디스크 공간 자동 관리

### Phase 5 완료 후 (로그 폴더 열기)
- 로그 폴더 열기 버튼으로 저장된 로그 파일에 쉽게 접근 가능
- 터미널 로그(`ChronoView_Debug_*.log`)와 UI 로그(`ChronoView_UI_*.log`) 모두 확인 가능
- Windows 탐색기에서 로그 파일 관리 가능
- 각 로그 파일의 세션 시작 시간 확인 가능

### Phase 6 완료 후 (UI 개선 - 선택)
- 빠른 저장 기능으로 필터된 로그를 추가로 저장 가능

## 참고사항

1. **터미널 로그 vs UI 로그 (중요)**
   - **터미널 로그**: 개발용, 상세한 디버그 정보
     - Microsoft.Extensions.Logging 사용
     - 저장 경로: `ChronoView_Debug_YYYYMMDD.log`
     - 목적: 개발자가 디버깅할 때 사용
   
   - **UI 로그**: 사용자용, 현재 상태 정보
     - MainWindowViewModel.LogMessages 사용
     - 저장 경로: `ChronoView_UI_YYYYMMDD.log`
     - 목적: 사용자가 확인한 상태 정보 보존
   
   - **두 로그는 목적이 다르므로 저장도 따로 되어야 함**

2. **터미널 로그 파일 저장 구현 방법**
   - Microsoft.Extensions.Logging.File NuGet 패키지 사용
   - 또는 Serilog 사용 (더 강력한 기능)
   - 또는 직접 FileLoggerProvider 구현

3. **기존 LogPanel.WriteToLogFile()**
   - LogPanel.AddLogMessage()는 실제로 사용되지 않음
   - MainWindowViewModel.AddLogMessage()가 실제 사용되는 메서드
   - 따라서 MainWindowViewModel에 파일 저장 기능을 추가해야 함

4. **리소스 사용 방식**
   - 실제 코드는 하드코딩된 문자열을 사용하는 경우가 많음
   - UI 요소(XAML)는 {x:Static res:Strings.XXX} 패턴 사용
   - 메시지 박스는 하드코딩된 문자열 사용 (기존 패턴 유지)

5. **코드 재사용**
   - 빠른 저장 기능은 기존 ExportToFile() 메서드 재사용
   - 파일 경로만 변경하여 동일한 로직 활용

6. **세션 시작 시간 기록**
   - 각 로그 파일 시작 부분에 세션 시작 시간 기록
   - 형식: "Session Started: YYYY-MM-DD HH:mm:ss"
   - 파일이 새로 생성될 때만 기록 (기존 파일에는 추가하지 않음)
   - 언제 실행했는지 추적 가능
   - **참고**: `_sessionStartTime`이 static이므로 같은 날짜에 여러 세션이 실행되면 첫 번째 세션 시간만 기록됨
     - 이는 의도된 동작: 같은 날짜의 첫 세션 시작 시간만 기록하여 파일을 깔끔하게 유지
     - **중요**: 새 날짜 파일일 때는 `_sessionStartTime`을 업데이트해야 함 (버그 수정)
     - 각 세션마다 기록하려면 날짜별로 세션 시작 시간을 관리하는 로직 추가 필요 (선택사항)

7. **로그 파일 자동 삭제**
   - 애플리케이션 시작 시 오래된 로그 파일 자동 삭제
   - 설정된 보관 기간(LogRetentionDays)보다 오래된 파일 삭제
   - 기본값: 30일 (한달)
   - 사용자가 설정에서 조정 가능

