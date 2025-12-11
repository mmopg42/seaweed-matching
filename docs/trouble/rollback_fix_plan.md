# Implementation Plan - Fix Rollback Data Loss

## Problem Description
The current rollback logic in `FileOperationService` uses a "Copy-then-Delete" pattern. However, the rollback mechanism blindly deletes the destination file (`dest`) upon failure. If the source file (`source`) has already been deleted (which happens immediately after a successful copy and verification), deleting the `dest` file results in **permanent data loss** of that file.

## User Review Required
> [!IMPORTANT]
> This fix changes the rollback behavior. Instead of deleting the destination file, it will attempt to **move it back** to the source location if the source file is missing. This ensures data preservation.

## Proposed Changes

### ChronoView/Core/FileOperations
#### [MODIFY] [FileOperationService.cs](file:///c:/workspace/seaweed/gui_kiro/ChronoView/Core/FileOperations/FileOperationService.cs)
- Update `RollbackAsync` method:
  - Check if `source` file exists.
  - If `source` exisis: Safe to delete `dest` (current behavior).
  - If `source` does **NOT** exist: Move `dest` back to `source` (restore).
  - Apply similar logic for directories.

### ChronoView.Tests/Core/FileOperations
#### [NEW] [FileOperationServiceTests.cs](file:///c:/workspace/seaweed/gui_kiro/ChronoView.Tests/Core/FileOperations/FileOperationServiceTests.cs)
- Create new test class `FileOperationServiceTests`.
- Add test case `Rollback_ShouldRestoreSource_WhenSourceDeleted()`:
  - Simulate a move operation.
  - Manually delete source to simulate "Copy-then-Delete" success.
  - Trigger rollback.
  - Verify that the file is restored to the source path.

## Verification Plan

### Automated Tests
Run the newly created unit test:
```powershell
dotnet test --filter "FullyQualifiedName~FileOperationServiceTests"
```

### Manual Verification
Since this is a race-condition-like failure recovery, manual testing is difficult without injecting failures. The unit test with mocked failure/state is the primary verification method.
