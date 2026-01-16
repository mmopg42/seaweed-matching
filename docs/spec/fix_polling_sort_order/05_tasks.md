---
Task: fix_polling_sort_order
Created: 2026-01-09
Status: Draft
Depends On: 04_design.md
---

# Fix Polling Sort Order - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Core | 3 | 0 | 3 |
| Verification | 2 | 0 | 2 |
| **Total** | **5** | **0** | **5** |

---

## Phase 1: Core Implementation

### 1.1 Add Helper Methods <!-- id: 0 -->

- [ ] Add `ExtractTimestampForSorting` method to `FileWatcherService.cs`
  - [ ] Auto-detect NIR files (starts with `run_`)
  - [ ] Auto-detect Normal folders (starts with `C`, contains `T`)
  - [ ] Auto-detect Camera files (image extensions)
  - [ ] Return `null` for unknown patterns
- [ ] Add `GetSensorPriority` method to `FileWatcherService.cs`
  - [ ] Return 1 for NIR
  - [ ] Return 2 for Normal
  - [ ] Return 3 for Camera
  - [ ] Return 99 for unknown

**Verify**: Build succeeds: `dotnet build ChronoView/ChronoView.csproj`

---

### 1.2 Modify OnPollTick Sorting Logic <!-- id: 1 -->

- [ ] Replace line 178 in `FileWatcherService.cs`
  - [ ] Remove: `OrderBy(item => Path.GetFileName(item.Path))`
  - [ ] Add: Sort by extracted timestamp (ascending)
  - [ ] Add: Then by sensor priority (ascending)
  - [ ] Use `DateTime.MaxValue` as fallback for null timestamps

**Verify**: Build succeeds and no compilation errors

---

### 1.3 Add Logging for Sort Order <!-- id: 2 -->

- [ ] Add debug log showing sort order before firing events
  - [ ] Log first 5 items in sorted order with timestamps

**Verify**: Run application and check log output during polling cycle

---

## Phase 2: Verification

### 2.1 Manual Test <!-- id: 3 -->

- [ ] **Scenario**: Multiple file types with different timestamps
  1. Stop monitoring
  2. Place files in watch folders:
     - NIR: `run_120260109T174703A.txt` (17:47:03)
     - Camera: `20260109_174705_001.bmp` (17:47:05)
     - Normal: `C260109T174704_0` (17:47:04)
  3. Start monitoring
  4. Wait for polling cycle
  5. Check UI logs for group creation order

  **Expected**: 
  - NIR (17:47:03) creates group first (e.g., line1_001)
  - Normal (17:47:04) creates/matches second (e.g., line1_002 or matches 001)
  - Camera (17:47:05) creates/matches last

---

### 2.2 Regression Test <!-- id: 4 -->

- [ ] Run existing tests: `dotnet test ChronoView.Tests/ChronoView.Tests.csproj`
- [ ] Verify no test failures

**Verify**: All tests pass

---

## Success Criteria Check

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Files sorted by timestamp | ⬜ | Debug log shows timestamp order |
| NIR processed before Camera (same time) | ⬜ | Manual test with same-second files |
| Group IDs follow timestamp order | ⬜ | UI log shows sequential IDs |
| No regression | ⬜ | `dotnet test` passes |

---

## Completion Log

| Task | Completed | Duration | Notes |
|------|-----------|----------|-------|
| - | - | - | - |

---

## Approval

- [ ] All tasks completed
- [ ] All verifications pass
- [ ] Success criteria met
- [ ] Ready for 06_report.md

**Next Step**: Implementation, then 06_report.md
