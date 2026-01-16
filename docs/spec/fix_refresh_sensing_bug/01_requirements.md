---
Task: fix_refresh_sensing_bug
Created: 2026-01-13
Status: Draft
Summary: Fix the bug where "Refresh" inadvertently starts background monitoring/sensing even when it's supposed to be OFF.
Research Required: Yes
---

# Fix Refresh Sensing Bug - Requirements

## 1. Goal

### 1.1 Primary Goal

The "Refresh" action should update the UI with current disk data without starting or restarting background real-time monitoring (FileWatcher) if monitoring was already OFF.

### 1.2 Success Criteria

- [ ] Clicking "Refresh" ALWAYS stops the `FileSystemWatcher`, regardless of previous state.
- [ ] Clicking "Refresh" updates the grid with current disk data.
- [ ] The `IsMonitoring` status in the UI (System Control panel) updates to "Stopped" (Start button visible) after a refresh.
- [ ] No regression in the "Deep Reset" logic (all caches and services must still be cleared).

## 2. Constraints

### 2.1 Technical Constraints

- Must maintain the thread-safety of `MonitoringOrchestrator`.
- Cannot break the `MonitoringStateReset` event which signals UI to clear data.
- Must ensure `CoreResetAndScanAsync` remains the single source of truth for the reset/scan sequence.

### 2.2 Business Constraints

- User experience: Refresh should feel like a "sync" operation, not a "start sensing" operation.

### 2.3 Non-Goals (Out of Scope)

- Adding a separate "One-time Scan" button (Refresh will be overloaded to handle both states correctly).
- Modifying the `FileWatcherService` polling logic itself.

## 3. Assumptions

- The user wants the current behavior of `Refresh` (clearing all data and re-scanning) to persist, but only wants to opt-out of the *continuous* sensing if it wasn't already active.
- `MonitoringOrchestrator._isMonitoring` correctly tracks the intended state of the background watcher.

## 4. Dependencies

### 4.1 Blocked By

| Dependency | Status | Owner |
|------------|--------|-------|
| (None) | Done | N/A |

### 4.2 Blocks

| Dependent Task | Impact if Delayed |
|----------------|-------------------|
| User verification of sensing status | User remains confused about background activity |

---

## Approval

- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
- [ ] Scope boundaries are clear
- [ ] All blocking dependencies identified

**Next Step**: [03_plan.md]
