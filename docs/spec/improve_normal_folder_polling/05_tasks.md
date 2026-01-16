---
Task: improve_normal_folder_polling
Created: 2026-01-08
Status: In Progress
Depends On: 03_plan.md
---

# Improve Normal Folder Polling - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Implementation | 2 | 0 | 2 |
| Verification | 2 | 0 | 2 |
| **Total** | **4** | **0** | **4** |

---

## Phase 1: Implementation

### 1.1 Update Configuration

- [ ] Change `PollingIntervalMs` default value to 500 in `ChronoView/Models/ApplicationConfiguration.cs`

**Verify**:
- Check code: `PollingIntervalMs` is `500`.

---

### 1.2 Verify Existing FileWatcherService Logic

- [x] Confirm `FileWatcherService.cs` already contains logic to skip `_knownFiles` for incomplete Normal folders. (Verified by code review)
  - [x] `PerformSilentScan`: Logic exists.
  - [x] `HandleEvent`: Logic exists.

**Verify**:
- Code review completed.

## Phase 2: Verification

### 2.1 Verify Loop Logic

- [ ] Start application with Debug logs enabled.
- [ ] Create an empty folder `Normal_Test_0` in the monitored path.
- [ ] Observe logs: Expect "Normal folder without image, skipping _knownFiles addition" every 0.5s.

### 2.2 Verify Detection

- [ ] Copy `stitched_original.png` into `Normal_Test_0`.
- [ ] Observe logs: Expect "Processed group..." and loop stops.
- [ ] UI should show the new group/image.

---

## Completion Log

| Task | Completed | Duration | Notes |
|------|-----------|----------|-------|
| | | | |

## Success Criteria Check

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Empty Normal folders skipped in `_knownFiles` | ⬜ | |
| Delayed images detected | ⬜ | |
| Polling interval = 500ms | ⬜ | |
| No duplicate groups | ⬜ | |
