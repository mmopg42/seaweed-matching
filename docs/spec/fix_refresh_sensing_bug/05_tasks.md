---
Task: fix_refresh_sensing_bug
Created: 2026-01-13
Status: In Progress
Depends On: 04_design.md
---

# Fix Refresh Sensing Bug - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Setup | 1 | 0 | 1 |
| Core | 2 | 0 | 2 |
| Integration | 1 | 0 | 1 |
| Verification | 2 | 0 | 2 |
| **Total** | **6** | **0** | **6** |

---

## Phase 1: Setup

### 1.1 Pre-flight Check
- [ ] Verify `MonitoringOrchestrator.cs` and `SystemControlViewModel.cs` are accessible and ready for modification.

**Verify**: Files viewed and symbols confirmed.

---

## Phase 2: Core Implementation

### 2.1 Modify MonitoringOrchestrator.cs
- [ ] Update `RefreshAsync` to only start the `FileSystemWatcher` and `EventProcessor` if `_isMonitoring` is `true`.

**Verify**: Code inspection of `RefreshAsync` block starting at line 407.

### 2.2 Update SystemControlViewModel.cs
- [ ] Modify `RefreshMonitoringAsync` to remove the conditional `StartAsync` call.
- [ ] Ensure it calls `_orchestrator.RefreshAsync()` for both monitoring states.

**Verify**: Code inspection of `RefreshMonitoringAsync` methods.

---

## Phase 3: Integration

### 3.1 Synchronize ViewModel and Orchestrator
- [ ] Verify that `MainWindowViewModel.RefreshCommand` correctly triggers the updated flow.

**Verify**: `MainWindowViewModel.cs` line 261 remains valid.

---

## Phase 4: Final Verification

### 4.1 Manual Verification (Sensing OFF)
- [ ] Stop monitoring (Start button visible).
- [ ] Click Refresh.
- [ ] Verify logs: "Refreshing data...", then "새로고침 완료".
- [ ] Add a file to a watch folder.
- [ ] Verify: NO new row appears in the UI.

### 4.2 Manual Verification (Sensing ON)
- [ ] Start monitoring (Stop button visible).
- [ ] Click Refresh.
- [ ] Verify logs: "Refreshing data...", then "새로고침 완료".
- [ ] **Verify**: UI updates to "Stopped" (Start button visible).
- [ ] Add a file to a watch folder.
- [ ] Verify: NO new row appears (Watcher is OFF).

---

## Success Criteria Check

| Criterion (from requirements) | Status | Evidence |
|-------------------------------|--------|----------|
| Refresh while Monitoring OFF doesn't start watcher | ⬜ | Manual Test 4.1 |
| Refresh while Monitoring ON restarts watcher | ⬜ | Manual Test 4.2 |
| `IsMonitoring` UI status remains consistent | ⬜ | Manual check during refresh |

---

## Completion Log

| Task | Completed | Duration | Notes |
|------|-----------|----------|-------|
| | | | |

---

## Approval

- [ ] All tasks completed
- [ ] All verifications pass
- [ ] Success criteria met
- [ ] Ready for 06_report.md
