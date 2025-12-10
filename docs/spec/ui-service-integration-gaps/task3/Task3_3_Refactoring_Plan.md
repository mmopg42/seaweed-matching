# Task 3.3 Refactoring Plan: ExecuteRefresh Async & Command Guards

## 1. Current Defects Analysis
코드 리뷰 및 동작 분석 결과, `ExecuteRefreshAsync` 구현에 심각한 논리적 결함이 발견되었습니다.

1.  **데이터 소실 위험 (Data Loss Risk)**:
    - `MainWindowViewModel.ExecuteRefreshAsync`가 모니터링 상태를 확인하지 않고 무조건 `ClearFileGroups()`를 호출하여 UI 목록을 비웁니다.
    - 그 후 호출된 `_orchestrator.RefreshAsync()`는 `_isMonitoring`이 false이면 조용히 리턴합니다.
    - 결과적으로 사용자는 "새로고침을 눌렀는데 모든 목록이 사라지고 아무것도 로드되지 않는" 경험을 하게 됩니다.

2.  **취소 불가 (No Cancellation Support)**:
    - ViewModel에서 `cancellationToken`을 생성하지만 `_orchestrator.RefreshAsync()`에 전달하지 않습니다.
    - Orchestrator 인터페이스에도 `CancellationToken` 파라미터가 없어, 긴 스캔 작업 중 취소가 불가능합니다.

3.  **잘못된 사용자 피드백 (False Positive Feedback)**:
    - 모니터링이 비활성 상태여서 아무 작업도 안 하고 리턴됨에도 불구하고, ViewModel은 "Refresh complete"라는 성공 메시지를 표시합니다.

4.  **중복 실행 방지 미흡**:
    - Move/Delete와 같은 장기 실행 작업(Long-running Operations) 중에 Refresh나 Start/Stop을 누를 경우 상태 꼬임이 발생할 수 있습니다.

---

## 2. Refactoring Plan

### 2.1 MainWindowViewModel.cs 수정
1.  **`ExecuteRefreshAsync` 로직 변경**:
    - **Guard Clause**: `!IsMonitoring`일 경우, `BeginOperation` 호출 **이전에** `MessageBox` 경고 후 리턴합니다. (불필요한 `IsOperationInProgress` 토글 방지)
    - **Remove Explicit Clear**: `ClearFileGroups()` 호출을 제거합니다. `RefreshAsync` 내부에서 `OnGroupRemoved`가 발생하므로 이를 통해 UI가 정리되도록 합니다.
    - **Pass Token**: `_orchestrator.RefreshAsync(cancellationToken)`으로 토큰을 전달합니다.

2.  **Command Guards (CanExecute) 강화**:
    - `IsOperationInProgress` 프로퍼티 setter에서 `RaiseCanExecuteChanged`를 호출하도록 수정합니다.
      - 대상: `StartCommand`, `StopCommand`, `MoveCommand`, `DeleteCommand`, `RefreshCommand`
    - `CanExecuteStart`, `CanExecuteStop`, `CanExecuteRefresh`에 `!IsOperationInProgress` 조건을 추가합니다.

### 2.2 MonitoringOrchestrator.cs 및 Interface 수정
1.  **Token Propagation (Deep)**:
    - `ExecuteRefreshAsync` -> `RefreshAsync(token)` -> `PerformInitialScanAsync(token)` -> 내부 루프까지 `CancellationToken`을 전파하여 즉시 취소가 가능하도록 구현합니다.

2.  **Implementation Update**:
    - `RefreshAsync` 내부에서 `PerformInitialScanAsync` 호출 시 토큰을 전달합니다.
    - 예외 처리 시 Rollback이나 상태 복구 로직을 점검합니다.

---

## 3. Implementation Steps
1.  **Interface Update**: `IMonitoringOrchestrator` 및 `PerformInitialScanAsync` 시그니처 수정.
2.  **Orchestrator Update**: `RefreshAsync` 및 내부 스캔 로직에 토큰 체크(`ThrowIfCancellationRequested`) 추가.
3.  **ViewModel Properties Update**: `IsOperationInProgress` setter 수정 (Command 갱신).
4.  **ViewModel ExecuteRefresh Update**: Guard, Token, UX 개선.
5.  **Verification**: 
    - 취소 동작 테스트 (로그 확인).
    - 버튼 비활성화 확인.
