# Task 3.2 Implementation Summary: ExecuteDelete Async

## Overview
**Task 3.2**에서는 파일 그룹의 물리적 삭제를 처리하는 `ExecuteDeleteAsync` 메서드를 구현했습니다. 파일 삭제는 되돌릴 수 없는 작업이므로 사용자 확인 절차를 강화하고, 상세한 진행 상황 및 결과 피드백을 제공하는 데 중점을 두었습니다.

---

## Key Implementation Details

### 1. ExecuteDeleteAsync 메서드 구현 (`MainWindowViewModel.cs`)
기존의 동기식 Stub 메서드를 비동기 메서드로 대체하고 다음 기능을 구현했습니다:

- **사용자 확인 (Confirmation)**: 
  - `MessageBox`를 사용하여 삭제 작업이 영구적임을 경고하고, "Yes/No" 확인을 받습니다.
  - 실수로 인한 삭제를 방지하기 위해 기본 포커스를 "No"에 둡니다.

- **복수 선택 지원**: 
  - 단일 그룹뿐만 아니라 다중 선택된 그룹(`GetSelectedGroups()`)에 대해서도 일괄 삭제를 지원합니다.

- **진행률 및 결과 보고**:
  - `IProgress<OperationProgress>`를 통해 현재 삭제 중인 파일/폴더명을 상태바에 표시합니다.
  - 작업 완료 후 성공 수, 실패 수, 그리고 **실패한 총 파일 수(Total Files Failed)**를 요약하여 로그 및 팝업으로 알립니다.

### 2. FileOperationService 연동
- `_fileOperationService.DeleteFileGroupAsync`를 호출하여 실제 삭제를 수행합니다.
- 서비스 계층에서 발생한 예외나 부분 실패(취소 시점까지의 실패 처리 등)를 ViewModel에서 받아 사용자에게 상세히 전달합니다.

### 3. 컬렉션 동기화
- 삭제가 성공한 그룹(`result.Success == true`)은 `RemoveFileGroup(groupId)`를 통해 메모리 상의 컬렉션(`Lines1`, `Lines2`, `Combined`)에서도 즉시 제거하여 UI와 데이터의 일관성을 유지합니다.

---

## Code Snippet (Core Logic)

```csharp
public async Task ExecuteDeleteAsync()
{
    // ... Selection Validation & Confirmation ...

    // Begin Operation
    var cancellationToken = BeginOperation();
    
    // ... Loop through groups ...
    foreach (var group in selectedGroups)
    {
        var result = await _fileOperationService.DeleteFileGroupAsync(
            group.Model, progress, cancellationToken);

        if (result.Success)
        {
            successCount++;
            deletedGroups.Add(group.GroupId);
        }
        else
        {
            failedCount++;
            totalFilesFailed += result.FilesFailed; // 상세 실패 카운트 집계
            // ... Error Logging ...
        }
    }

    // ... Cleanup Collections & Show Summary ...
}
```
