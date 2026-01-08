# DataSequenceSettings NULL 오류 분석

## 증상

애플리케이션 시작 또는 새로고침 시 다음 오류가 반복적으로 발생:

```
warn: ChronoView.Core.FileWatching.MonitoringOrchestrator[0]
      DataSequenceSettings: CONFIG IS NULL
fail: ChronoView.Core.FileWatching.MonitoringOrchestrator[0]
      Legacy batch scan is deprecated and has been removed. Please configure DataSequenceSettings.
```

## 원인 분석

### 1. `_currentConfig`가 null인 경우

**위치**: `MonitoringOrchestrator.PerformInitialScanAsync()` (344-346번 라인)

```csharp
if (_currentConfig == null)
{
    _logger.LogWarning("DataSequenceSettings: CONFIG IS NULL");
}
```

**발생 시나리오**:
- `RefreshAsync()` 메서드에서 `PerformInitialScanAsync()`를 호출할 때 `_currentConfig`를 업데이트하지 않음
- `SystemControlViewModel.RefreshMonitoringAsync()`에서 모니터링이 비활성화된 상태에서 `PerformInitialScanAsync()`를 직접 호출할 때 config를 전달하지 않음

**코드 위치**:
- `MonitoringOrchestrator.RefreshAsync()` (303-334번 라인): `_currentConfig`를 업데이트하지 않고 `PerformInitialScanAsync()` 호출
- `SystemControlViewModel.RefreshMonitoringAsync()` (107번 라인): `IsMonitoring == false`일 때 `PerformInitialScanAsync()`를 직접 호출

### 2. `DataSequenceSettings`가 null인 경우

**위치**: `MonitoringOrchestrator.PerformInitialScanAsync()` (348-350번 라인)

```csharp
else if (_currentConfig.DataSequenceSettings == null)
{
    _logger.LogWarning("DataSequenceSettings: NULL (using legacy batch scan)");
}
```

**발생 시나리오**:
- 설정 파일(`config.json`)에 `DataSequenceSettings`가 없거나 null로 저장된 경우
- JSON 역직렬화 시 `DataSequenceSettings` 속성이 누락된 경우
- `ApplicationConfiguration`의 기본값(`DataSequencePresets.NormalFirst()`)이 적용되지 않은 경우

**참고**: `ApplicationConfiguration.cs` (47번 라인)에서 기본값이 설정되어 있지만, JSON 역직렬화 시 명시적으로 포함되지 않으면 null이 될 수 있음

### 3. Legacy Batch Scan 제거로 인한 실패

**위치**: `MonitoringOrchestrator.PerformInitialScanAsync()` (368-372번 라인)

```csharp
// Legacy: Batch matching (no DataSequenceSettings)
// REFACTOR: Removed legacy batch scan logic as it is dead code in production.
_logger.LogError("Legacy batch scan is deprecated and has been removed. Please configure DataSequenceSettings.");
result.Success = false;
result.Errors.Add("Legacy matching is not supported. Please configure DataSequenceSettings.");
```

**문제**: `DataSequenceSettings`가 null이면 Sequential scan을 사용할 수 없고, Legacy batch scan도 제거되어 초기 스캔이 실패함

## 영향

1. **모니터링 시작 실패**: `StartAsync()`에서 초기 스캔이 실패하면 모니터링이 시작되지 않음
2. **새로고침 실패**: `RefreshAsync()`에서 스캔이 실패하면 데이터가 갱신되지 않음
3. **사용자 경험 저하**: 반복적인 오류 로그로 인한 혼란

## 해결 방안

### 방안 1: `RefreshAsync`에서 Config 업데이트 (권장)

`RefreshAsync()` 메서드에서 최신 설정을 로드하여 `_currentConfig`를 업데이트:

```csharp
public async Task RefreshAsync(ApplicationConfiguration? config = null, CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Refreshing file groups");
        
        // Update config if provided
        if (config != null)
        {
            _currentConfig = config;
        }
        else if (_currentConfig == null)
        {
            // Load config if not set
            var configManager = /* get from DI */;
            _currentConfig = await configManager.LoadConfigurationAsync<ApplicationConfiguration>();
        }
        
        // ... rest of the method
    }
}
```

### 방안 2: `PerformInitialScanAsync`에서 Config 자동 로드

`PerformInitialScanAsync()`에서 `_currentConfig`가 null이면 자동으로 로드:

```csharp
public async Task<OrchestrationResult> PerformInitialScanAsync(CancellationToken cancellationToken = default)
{
    // Ensure config is loaded
    if (_currentConfig == null)
    {
        // Try to load from config manager (requires DI)
        // Or throw meaningful error
        _logger.LogError("Cannot perform initial scan: Configuration not set. Please call StartAsync() first.");
        return new OrchestrationResult 
        { 
            Success = false, 
            Errors = { "Configuration not initialized" } 
        };
    }
    
    // ... rest of the method
}
```

### 방안 3: `DataSequenceSettings` 기본값 보장

설정 로드 시 `DataSequenceSettings`가 null이면 기본값으로 초기화:

```csharp
// ConfigurationManager 또는 ApplicationConfiguration 생성자에서
if (config.DataSequenceSettings == null)
{
    config.DataSequenceSettings = DataSequencePresets.NormalFirst();
}
```

### 방안 4: `SystemControlViewModel.RefreshMonitoringAsync` 수정

모니터링이 비활성화된 상태에서도 config를 전달:

```csharp
public async Task RefreshMonitoringAsync()
{
    LogRequested?.Invoke(LogSeverity.Info, "System", "Refreshing data...");
    var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
    
    await _statsService.ReloadStatsAsync(config);

    if (IsMonitoring)
    {
        await _orchestrator.RefreshAsync(); 
    }
    else
    {
        // Config를 전달하여 초기 스캔 수행
        await _orchestrator.StartAsync(config);
        await _orchestrator.StopAsync(); // 스캔만 수행하고 모니터링은 중지
    }
    StatusChanged?.Invoke("Refreshed");
}
```

## 권장 해결책

**방안 1 + 방안 3 조합**:
1. `RefreshAsync()`에 config 매개변수 추가 및 `_currentConfig` 업데이트
2. 설정 로드 시 `DataSequenceSettings` null 체크 및 기본값 설정
3. `SystemControlViewModel.RefreshMonitoringAsync()`에서 config 전달

이렇게 하면:
- 모든 시나리오에서 `_currentConfig`가 유효하게 유지됨
- `DataSequenceSettings`가 항상 유효한 값으로 보장됨
- 초기 스캔이 정상적으로 수행됨

## 관련 파일

- `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs` (303-378번 라인)
- `ChronoView/UI/ViewModels/SystemControlViewModel.cs` (90-110번 라인)
- `ChronoView/Models/ApplicationConfiguration.cs` (47번 라인)
- `ChronoView/Core/Configuration/ConfigurationManager.cs` (82-103번 라인)


