---
Task: Global Refactoring (600-line Rule)
Created: 2024-05-16
Status: In Progress
Depends On: 04_design.md
---

# Global Refactoring - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Setup | 1 | 0 | 1 |
| Service Layer (FileOp) | 3 | 0 | 3 |
| ViewModel Layer (FileGroup) | 1 | 0 | 1 |
| ViewModel Layer (MainWindow) | 1 | 0 | 1 |
| Verification | 3 | 0 | 3 |
| **Total** | **9** | **0** | **9** |

---

## Phase 1: Setup

### 1.1 Infrastructure Preparation
- [x] Create new folders for holding specialized services/ViewModels if needed.
- [x] Create sub-ViewModel interfaces (IDashboardViewModel, etc.).

**Verify**: Project builds with empty interfaces.

---

## Phase 2: Service Layer (FileOperationService)

### 2.1 Extract FileGroupOperator
- [x] Create `Core/FileOperations/FileGroupOperator.cs`.
- [x] Move logic for single-group move/delete loop with rollback.
- [x] Implement generic path schema resolver.
- [/] Implement `ConflictResolution` caching logic (`_cachedConflictResolution`).

**Verify**: Unit tests for `FileGroupOperator` rollback pass.

---

### 2.2 Split into MoveService and DeleteService
- [x] Create `MoveService.cs` using `FileGroupOperator`.
  - [x] Implement Batch Move NIR Balancing logic (remove NIR if > limit).
  - [x] Implement Batch Move sorting (Oldest first).
- [x] Create `DeleteService.cs` using `FileGroupOperator`.
  - [/] Implement `DeleteComponentsAsync` for partial deletion.
- [x] Update `FileOperationService.cs` to delegate work.

**Verify**: `FileOperationService.cs` line count < 600.

---

## Phase 3: ViewModel Layer (FileGroupViewModel)

### 3.1 Extract MediaLoader
- [x] Create `UI/ViewModels/FileGroupMediaLoader.cs`.
- [x] Move thumbnail loading, NIR graph parsing, and retry timer logic.
- [/] Implement/Integrate `DetailPreviewViewModel` and `ImagePreviewWindow` support.
- [x] Inject `FileGroupMediaLoader` into `FileGroupViewModel`.

**Verify**: `FileGroupViewModel.cs` line count < 600. All images load in UI.

---

## Phase 4: ViewModel Layer (MainWindowViewModel)

### 4.1 Dismantle to Sub-ViewModels
- [x] Extract `DashboardViewModel` (Lists, Stats, RowHeight calc).
  - [ ] Implement Diagnostic logging (composition analysis).
- [x] Extract `FileOpViewModel` (Batch Commands, Partial Delete orchestration).
- [x] Extract `SystemControlViewModel` (Lifecycle, Launchers, Program Status).
- [/] Update `MainWindowViewModel.cs` to host properties.
  - [x] Implement Log message capping (1000 items).
  - [ ] Implement Auto-Save logic for Move counts.
  - [ ] Implement `RequestOpenSettings` event.

**Verify**: `MainWindowViewModel.cs` line count < 800 (Target 600). Application starts and commands work.

---

## Phase 5: Final Verification

### 5.1 Automated tests
- [ ] Run `dotnet test`.

---

### 5.2 Success Criteria Check

| Criterion | Status | Evidence |
|-----------|--------|----------|
| MainWindowViewModel < 800 lines | ⬜ | Line count check |
| FileGroupViewModel < 600 lines | ⬜ | Line count check |
| FileOperationService < 600 lines | ⬜ | Line count check |
| Move/Delete behavior parity | ⬜ | Manual verification |

---

## Completion Log

| Task | Completed | Duration | Notes |
|------|-----------|----------|-------|
| Planning | 2024-05-16 | 60m | 01-05 spec docs created |
