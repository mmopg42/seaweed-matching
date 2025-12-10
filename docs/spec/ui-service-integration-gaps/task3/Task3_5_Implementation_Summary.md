# Task 3.5 Implementation Summary: ExecuteCreateSampleFolder

## Overview
Task 3.5에서는 **샘플 폴더 생성(Create Sample Folders)** 기능을 구현했습니다. 테스트 및 검증 목적으로 모든 모니터링 경로에 샘플 폴더를 일괄 생성하는 편의 기능입니다.

---

## Key Implementation Details

### 1. Asynchronous Command Pattern
- **Wrapper Method**: `ExecuteCreateSampleFolder()` - UI 스레드 비차단
- **Async Implementation**: `ExecuteCreateSampleFolderAsync()` - 실제 비동기 로직

### 2. Auto-Generated Folder Naming
UI에 폴더명 입력 필드가 없으므로, **타임스탬프 기반 자동 생성**:
```csharp
var sampleName = $"Sample_{DateTime.Now:yyyyMMdd_HHmmss}";
// 예: "Sample_20251211_021712"
```
- **장점**: 중복 방지, 생성 시점 추적 용이
- **형식**: `Sample_YYYYMMDD_HHmmss`

### 3. User Confirmation
```csharp
var confirmMessage = $"Create sample folder '{sampleName}' in all configured paths?\n\n" +
                     "This will create folders in NIR, Normal, and Camera paths.";

var confirmResult = await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
    WpfMessageBox.Show(confirmMessage, "Confirm Sample Folder Creation",
        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No));
```
- 생성될 폴더명 미리 표시
- 영향 범위(NIR/Normal/Camera) 안내

### 4. PathManagementService Integration
```csharp
var result = await _pathManagementService.CreateSampleFoldersAsync(
    sampleName, 
    config, 
    cancellationToken);
```
- **대상 경로**: NIR1/2, Normal1/2, Camera1-6 (설정된 경로만)
- **생성 로직**: 각 경로 하위에 `sampleName` 폴더 생성
- **중복 처리**: 이미 존재하면 SKIP (로그 기록)
- **반환값**: `bool` (전체 성공 여부)

### 5. Cancellation Support
- `BeginOperation()` / `EndOperation()`으로 취소 토큰 관리
- `OperationCanceledException` 예외 처리
- 취소 시 진행 상태 및 로그 기록

### 6. User Feedback Strategy
#### 성공 시:
```
Sample folders created successfully.

Folder name: Sample_20251211_021712

Created in all configured monitoring paths.
```

#### 경고 시 (일부 실패):
```
Sample folder creation completed with warnings.
Check logs for details.
```
- 서비스가 `false` 반환 시 (일부 경로 실패)
- 로그 확인 유도

#### 오류 시:
- 예외 메시지 표시
- 로그에 상세 정보 기록

---

## Code Snippet (Core Logic)

```csharp
private async Task ExecuteCreateSampleFolderAsync()
{
    if (IsOperationInProgress) return;  // Guard

    try
    {
        var sampleName = $"Sample_{DateTime.Now:yyyyMMdd_HHmmss}";
        
        // Confirm → Begin → Load Config → Create → Feedback
        var cancellationToken = BeginOperation();
        var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
        
        var result = await _pathManagementService.CreateSampleFoldersAsync(
            sampleName, config, cancellationToken);

        // Handle success/warning/error...
    }
    finally
    {
        EndOperation();  // Release operation lock
    }
}
```

---

## PathManagementService Behavior Recap
(from `PathManagementService.cs` lines 110-148)
- **대상 경로 수집**: NIR1/2, Normal1/2, Cam1-6 (빈 경로 제외)
- **폴더 생성**: `Directory.CreateDirectory(Path.Combine(basePath, sampleName))`
- **중복 처리**: 존재하면 SKIP (에러 없이 로그만 기록)
- **카운팅**: `createdCount`, `skippedCount` 로그 출력
- **반환**: 모든 작업 성공 시 `true`, 예외/취소 시 `false`

---

## Verification
- **Build**: Successful (no errors)
- **Dependencies**: `IPathManagementService` 주입 완료
- **Operation Guards**: `BeginOperation/EndOperation` 적용

---

## Phase 3 Commands Summary
| Task | Command | Status |
|------|---------|--------|
| 3.1 | ExecuteMove | ✅ COMPLETED |
| 3.2 | ExecuteDelete | ✅ COMPLETED |
| 3.3 | ExecuteRefresh | ✅ COMPLETED (Refactored) |
| 3.4 | ExecutePathAutoConfig | ✅ COMPLETED |
| 3.5 | ExecuteCreateSampleFolder | ✅ COMPLETED |

**Phase 3 완료!**
