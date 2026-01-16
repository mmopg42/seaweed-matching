---
Task: fix_delayed_image_update_blocking
Created: 2026-01-13
Status: In Progress
Depends On: 04_design.md
---

# Fix Delayed Image Update Blocking - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Setup | 1 | 0 | 1 |
| Core | 2 | 0 | 2 |
| Verification | 2 | 0 | 2 |
| **Total** | **5** | **0** | **5** |

---

## Phase 1: Setup

### 1.1 Pre-flight Checks
- [ ] Verify existing tests for `GroupManager`
- [ ] Create reproduction script `oneoff/reproduce_delayed_image.py`

**Verify**: Reproduction script fails (simulates image landing but no update)

---

## Phase 2: Core Implementation

### 2.1 GroupManager Logic Fix
- [ ] Remove `_processedFiles` check in `GroupManager.cs`
- [ ] Ensure `MergeGroups` handles data-only updates correctly

**Verify**: `dotnet test` (existing tests pass)

### 2.2 EventProcessor Debounce Adjustment
- [ ] Modify `EventProcessor.cs` to skip debouncing for `stitched_original.png`

**Verify**: Code analysis or small unit test if possible

---

## Phase 3: Final Verification

### 3.1 Manual Verification
- [ ] Run ChronoView
- [ ] Run `reproduce_delayed_image.py`
- [ ] Confirm image appears automatically after delay

---

## Success Criteria Check

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Images appear automatically | ⬜ | Manual test |
| No manual refresh needed | ⬜ | Manual test |
| Deduplication still works | ⬜ | Manual test (same event twice) |

---

## Completion Log

| Task | Completed | Duration | Notes |
|------|-----------|----------|-------|
| - | - | - | - |

---

## Approval

- [ ] All tasks completed
- [ ] All verifications pass

**Next Step**: 06_report.md
