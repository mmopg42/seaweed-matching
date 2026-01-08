# 이동/삭제 후 UI 즉시 갱신 구현 문서

## 개요

이동/삭제 작업 완료 직후(모니터링 OFF 상태 포함) 소스 폴더를 1회 재스캔하고 파일 카운트를 재로딩하여, 목록/카운트가 즉시 UI에 반영되도록 구현했습니다.

**구현 일자**: 2025-01-06  
**관련 계획서**: `c:\Users\redli\.cursor\plans\이동_삭제_후_ui_즉시_갱신_b3c22fb1.plan.md`

## 배경 (현재 문제 구조)

### 문제점

1. **Move 작업 후 UI 갱신 누락**
   - `FileOperationViewModel.ExecuteMoveAsync()`가 파일 이동만 수행
   - 작업 완료 후 대시보드 목록/카운트 갱신을 트리거하지 않음
   - UI에 잔상이 남음

2. **Delete 작업 후 불완전한 갱신**
   - `ExecuteDeleteAsync()`가 파일 제거 후 내부에서 `RefreshDataAsync()`를 호출
   - 하지만 이는 `Dashboard.FileGroups`만 Clear할 뿐 실제 파일 시스템 재스캔을 동반하지 않음
   - 통계가 맞지 않음

3. **모니터링 중지 시 자동 갱신 불가**
   - 이동/삭제 시 `MainWindowViewModel`에서 모니터링을 중지
   - 파일 감시 이벤트로 UI가 자동 갱신되지 않는 상황 발생

## 구현 내용

### 1. MainWindowViewModel 수정

**파일**: `ChronoView/UI/ViewModels/MainWindowViewModel.cs`

#### 1.1 ExecuteMoveWithConfirmation 수정

**변경 위치**: Line 388-420

**변경 전**:
```csharp
var result = System.Windows.MessageBox.Show(message, "이동 확인", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
if (result == System.Windows.MessageBoxResult.Yes)
{
    _ = Operations.ExecuteMoveAsync(Dashboard.FileGroups.ToList(), MoveNir, MoveAllData, SampleName);
}
```

**변경 후**:
```csharp
var result = System.Windows.MessageBox.Show(message, "이동 확인", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
if (result == System.Windows.MessageBoxResult.Yes)
{
    try
    {
        StatusMessage = "Processing...";
        await Operations.ExecuteMoveAsync(Dashboard.FileGroups.ToList(), MoveNir, MoveAllData, SampleName);

        StatusMessage = "Refreshing data...";
        await Operations.RefreshDataAsync(); // 1. UI 목록 Clear
        await Control.RefreshMonitoringAsync(); // 2. 통계 리로드 및 1회 재스캔(PerformInitialScanAsync)

        if (StatusMessage == "Refreshed")
        {
            StatusMessage = "Ready";
        }
        else
        {
            // PerformInitialScanAsync 실패 시 (예: DataSequenceSettings 미설정) 알림 보강
            _logger.LogWarning("Refresh may be incomplete due to configuration issues.");
            StatusMessage = "Refresh completed with warnings. Check settings.";
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during move operation or refresh");
        StatusMessage = $"Move failed: {ex.Message}";
        System.Windows.MessageBox.Show($"이동 작업 중 오류 발생: {ex.Message}", "오류",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
    }
}
```

**주요 변경사항**:
- `_ = Operations.ExecuteMoveAsync(...)` → `await Operations.ExecuteMoveAsync(...)`로 변경 (fire-and-forget 제거)
- 작업 완료 후 `RefreshDataAsync()` → `RefreshMonitoringAsync()` 순으로 호출
- StatusMessage를 통한 진행 상태 표시
- 재스캔 실패 시 경고 메시지 표시

#### 1.2 ExecuteDeleteWithConfirmation 수정

**변경 위치**: Line 446-478

**변경 전**:
```csharp
var result = System.Windows.MessageBox.Show(message, "삭제 확인", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
if (result == System.Windows.MessageBoxResult.Yes)
{
    _ = Operations.ExecuteDeleteAsync(selectedGroups, SampleName);
}
```

**변경 후**:
```csharp
var result = System.Windows.MessageBox.Show(message, "삭제 확인", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
if (result == System.Windows.MessageBoxResult.Yes)
{
    try
    {
        StatusMessage = "Processing...";
        await Operations.ExecuteDeleteAsync(selectedGroups, SampleName);

        StatusMessage = "Refreshing data...";
        await Operations.RefreshDataAsync(); // 1. UI 목록 Clear
        await Control.RefreshMonitoringAsync(); // 2. 통계 리로드 및 1회 재스캔(PerformInitialScanAsync)

        if (StatusMessage == "Refreshed")
        {
            StatusMessage = "Ready";
        }
        else
        {
            // PerformInitialScanAsync 실패 시 (예: DataSequenceSettings 미설정) 알림 보강
            _logger.LogWarning("Refresh may be incomplete due to configuration issues.");
            StatusMessage = "Refresh completed with warnings. Check settings.";
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during delete operation or refresh");
        StatusMessage = $"Delete failed: {ex.Message}";
        System.Windows.MessageBox.Show($"삭제 작업 중 오류 발생: {ex.Message}", "오류",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
    }
}
```

**주요 변경사항**:
- `_ = Operations.ExecuteDeleteAsync(...)` → `await Operations.ExecuteDeleteAsync(...)`로 변경 (fire-and-forget 제거)
- 작업 완료 후 `RefreshDataAsync()` → `RefreshMonitoringAsync()` 순으로 호출
- StatusMessage를 통한 진행 상태 표시
- 재스캔 실패 시 경고 메시지 표시

### 2. FileOperationViewModel 수정

**파일**: `ChronoView/UI/ViewModels/FileOperationViewModel.cs`

#### 2.1 ExecuteDeleteAsync 내부 Refresh 호출 제거

**변경 위치**: Line 115-122

**변경 전**:
```csharp
if (groupsToRemove.Any()) {
    LogRequested?.Invoke(LogSeverity.Info, "Delete", $"{groupsToRemove.Count} groups successfully deleted.");
}

StatusChanged?.Invoke($"Delete completed. Refreshing...");

// Re-match remaining files after deletion
await RefreshDataAsync();
```

**변경 후**:
```csharp
if (groupsToRemove.Any()) {
    LogRequested?.Invoke(LogSeverity.Info, "Delete", $"{groupsToRemove.Count} groups successfully deleted.");
}

StatusChanged?.Invoke("Delete completed.");

// Refresh는 MainWindowViewModel에서 일원화하여 처리
// 중복 갱신 방지를 위해 여기서는 제거
```

**주요 변경사항**:
- `await RefreshDataAsync()` 호출 제거
- 갱신 책임을 `MainWindowViewModel`로 일원화
- 중복 갱신 및 불필요한 Clear/깜빡임 방지

## 동작 흐름

### 1. 이동 작업 흐름

```
사용자가 이동 버튼 클릭
  → ExecuteMoveWithConfirmation() 호출
    → 모니터링 중지 확인 (필요 시)
    → 확인 메시지 표시
    → 사용자가 '예' 선택
      → StatusMessage = "Processing..."
      → await Operations.ExecuteMoveAsync(...) (파일 이동 실행)
      → StatusMessage = "Refreshing data..."
      → await Operations.RefreshDataAsync() (UI 목록 Clear)
      → await Control.RefreshMonitoringAsync() (통계 리로드 및 1회 재스캔)
      → StatusMessage 확인 및 업데이트
        → "Refreshed"이면 "Ready"
        → 아니면 경고 메시지 표시
```

### 2. 삭제 작업 흐름

```
사용자가 삭제 버튼 클릭
  → ExecuteDeleteWithConfirmation() 호출
    → 모니터링 중지 확인 (필요 시)
    → 확인 메시지 표시
    → 사용자가 '예' 선택
      → StatusMessage = "Processing..."
      → await Operations.ExecuteDeleteAsync(...) (파일 삭제 실행)
        → (내부에서 RefreshDataAsync 호출하지 않음)
      → StatusMessage = "Refreshing data..."
      → await Operations.RefreshDataAsync() (UI 목록 Clear)
      → await Control.RefreshMonitoringAsync() (통계 리로드 및 1회 재스캔)
      → StatusMessage 확인 및 업데이트
        → "Refreshed"이면 "Ready"
        → 아니면 경고 메시지 표시
```

### 3. RefreshMonitoringAsync 동작

**파일**: `ChronoView/UI/ViewModels/SystemControlViewModel.cs` (Line 90-110)

```csharp
public async Task RefreshMonitoringAsync()
{
    LogRequested?.Invoke(LogSeverity.Info, "System", "Refreshing data...");
    var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
    
    // Reload file statistics
    await _statsService.ReloadStatsAsync(config);

    // If monitoring is active, we might need to stop/start or use a dedicated refresh on orchestrator
    if (IsMonitoring)
    {
        // If Orchestrator supports hot-refresh:
        await _orchestrator.RefreshAsync(); 
    }
    else
    {
        // Just scan
        await _orchestrator.PerformInitialScanAsync();
    }
    StatusChanged?.Invoke("Refreshed");
}
```

**동작**:
- 모니터링이 활성화되어 있으면 `RefreshAsync()` 호출 (기존 그룹 Clear 후 재스캔)
- 모니터링이 비활성화되어 있으면 `PerformInitialScanAsync()` 호출 (1회 재스캔)
- 성공 시 `StatusChanged?.Invoke("Refreshed")` 호출하여 StatusMessage 업데이트

## 목표 동작 달성

### ✅ 모니터링 OFF 유지
- 작업 후에도 모니터링 상태는 OFF로 유지
- 사용자가 수동으로 다시 시작해야 함

### ✅ 작업 완료 직후 1회 전체 재스캔
- `RefreshMonitoringAsync()`를 통해 `PerformInitialScanAsync()` 또는 `RefreshAsync()` 호출
- 파일 시스템을 재스캔하여 목록과 통계를 "정답 상태"로 동기화

### ✅ 부분 삭제 후에도 전체 재스캔
- 부분 삭제(Components Delete) 후에도 최종적으로 전체 재스캔 수행
- 통계 정합성 보장

## 에러 처리

### 1. 재스캔 실패 감지

**방법**: `RefreshMonitoringAsync()` 완료 후 `StatusMessage` 확인

```csharp
if (StatusMessage == "Refreshed")
{
    StatusMessage = "Ready";
}
else
{
    // PerformInitialScanAsync 실패 시 (예: DataSequenceSettings 미설정) 알림 보강
    _logger.LogWarning("Refresh may be incomplete due to configuration issues.");
    StatusMessage = "Refresh completed with warnings. Check settings.";
}
```

**실패 원인 예시**:
- `DataSequenceSettings` 미설정
- 파일 시스템 접근 오류
- 설정 파일 오류

### 2. 작업 실패 처리

**방법**: try-catch 블록으로 예외 처리

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error during move operation or refresh");
    StatusMessage = $"Move failed: {ex.Message}";
    System.Windows.MessageBox.Show($"이동 작업 중 오류 발생: {ex.Message}", "오류",
        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
}
```

## 영향 범위

### 수정된 파일

1. **ChronoView/UI/ViewModels/MainWindowViewModel.cs**
   - `ExecuteMoveWithConfirmation()` 메서드 수정
   - `ExecuteDeleteWithConfirmation()` 메서드 수정

2. **ChronoView/UI/ViewModels/FileOperationViewModel.cs**
   - `ExecuteDeleteAsync()` 메서드 수정 (내부 Refresh 호출 제거)

### 영향 없는 부분

- 기존 이동/삭제 로직 (파일 이동/삭제 자체는 변경 없음)
- 모니터링 시작/중지 로직
- 다른 UI 갱신 로직

## 검증 시나리오

### 1. 모니터링 중 이동/삭제

**시나리오**:
1. 모니터링 시작
2. 파일 그룹 이동/삭제 실행
3. 모니터링 자동 중지 확인
4. UI 목록 비워짐 확인
5. 1회 재스캔으로 새 목록/통계 표시 확인
6. 모니터링 OFF 유지 확인

**예상 결과**:
- 작업 완료 후 목록이 즉시 갱신됨
- 통계가 정확하게 반영됨
- 모니터링은 OFF 상태 유지

### 2. 모니터링 OFF 상태에서 이동/삭제

**시나리오**:
1. 모니터링 OFF 상태
2. 파일 그룹 이동/삭제 실행
3. UI 목록 비워짐 확인
4. 1회 재스캔으로 새 목록/통계 표시 확인

**예상 결과**:
- 작업 완료 후 목록이 즉시 갱신됨
- 통계가 정확하게 반영됨
- 모니터링은 계속 OFF 상태 유지

### 3. 부분 삭제

**시나리오**:
1. 파일 그룹의 일부 컴포넌트만 선택하여 삭제
2. UI에서 해당 컴포넌트만 제거 확인
3. 최종적으로 전체 재스캔 수행 확인
4. 전체 통계 정합성 확인

**예상 결과**:
- 부분 삭제된 그룹이 UI에서 즉시 업데이트됨
- 전체 재스캔으로 통계가 정확하게 반영됨

### 4. 재스캔 실패 시나리오

**시나리오**:
1. `DataSequenceSettings` 미설정 상태
2. 파일 그룹 이동/삭제 실행
3. 재스캔 실패 확인
4. 경고 메시지 표시 확인

**예상 결과**:
- StatusMessage에 "Refresh completed with warnings. Check settings." 표시
- 로그에 경고 메시지 기록
- 사용자가 설정을 확인할 수 있도록 안내

## 주의사항

1. **비동기 작업 완료 대기**: `await`를 사용하여 작업 완료를 기다리므로, UI가 일시적으로 응답하지 않을 수 있습니다. 대량 파일 처리 시 시간이 걸릴 수 있습니다.

2. **재스캔 시간**: `PerformInitialScanAsync()`는 전체 폴더를 스캔하므로 시간이 걸릴 수 있습니다. 진행 상태는 StatusMessage를 통해 표시됩니다.

3. **모니터링 상태**: 작업 후 모니터링은 OFF 상태로 유지되므로, 사용자가 수동으로 다시 시작해야 합니다.

4. **에러 복구**: 재스캔 실패 시에도 작업 자체는 완료되었을 수 있으므로, 사용자가 설정을 확인하고 수동으로 새로고침할 수 있습니다.

## 향후 개선 사항

1. **진행률 표시**: 재스캔 진행률을 ProgressBar에 표시
2. **취소 기능**: 재스캔 중 취소 기능 추가
3. **부분 갱신**: 전체 재스캔 대신 변경된 부분만 갱신하는 최적화
4. **백그라운드 처리**: 재스캔을 백그라운드에서 처리하여 UI 응답성 향상

## 참고

- **계획서**: `c:\Users\redli\.cursor\plans\이동_삭제_후_ui_즉시_갱신_b3c22fb1.plan.md`
- **관련 파일**:
  - `ChronoView/UI/ViewModels/MainWindowViewModel.cs`
  - `ChronoView/UI/ViewModels/FileOperationViewModel.cs`
  - `ChronoView/UI/ViewModels/SystemControlViewModel.cs`
  - `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs`

