---
Task: fix_general_cam_thumbnail_delay
Created: 2026-01-13
Status: In Progress
Depends On: 04_design.md
---

# Fix General Camera Thumbnail Delay - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Setup | 0 | 0 | 0 |
| Core | 2 | 2 | 0 |
| Integration | 0 | 0 | 0 |
| Documentation | 0 | 0 | 0 |
| Verification | 3 | 0 | 3 |
| **Total** | **5** | **2** | **3** |

---

## Phase 2: Core Implementation

### 2.1 FileWatcherService: Remove Event Rewrite

- [x] Modify `ChronoView/Core/FileWatching/FileWatcherService.cs`:
  - [x] Locate `HandleEvent` method.
  - [x] Remove the logic that attempts to rewrite `stitched_original.png` events into Folder events.
  - [x] Ensure `stitched_original.png` events fall through to standard processing (deduplication -> Invoke).

**Verify**:
- Inspection: Check that `FileWatcherService.cs` no longer contains the conversion logic block.

---

### 2.2 MonitoringOrchestrator: Add Renamed Processing & Logging

- [x] Modify `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs`:
  - [x] Locate `ProcessSingleEventAsync` method.
  - [x] Add `case WatcherChangeTypes.Renamed:` to the switch statement handling `Created`/`Changed`.
  - [x] Add Logging:
    - [x] Log "Fast Capture triggered for {Path}" when `IsStitchedImage` is true.

**Verify**:
- Inspection: Check `Renamed` case exists in switch statement.

---

## Phase 5: Final Verification

### 5.1 Manual Verification (Delayed Creation)

- [ ] **Setup**: Use user-provided simulation script (or similar logic) to create a folder, wait 10s, then create `stitched_original.png` (via Rename/Move).
- [ ] **Action**: Run ChronoView and observe.
- [ ] **Expected**:
  - [ ] Folder appears immediately as empty/placeholder.
  - [ ] After 10s, image thumbnail appears automatically.
  - [ ] "Fast Capture triggered" log appears in logs.
  - [ ] No duplicate lines in Dashboard.

### 5.2 Manual Verification (Standard Creation)

- [ ] **Setup**: Copy a normal folder with image into the watch directory.
- [ ] **Action**: Observe Dashboard.
- [ ] **Expected**: Group appears with image validation.

### 5.3 Success Criteria Check

| Criterion (from requirements) | Status | Evidence |
|-------------------------------|--------|----------|
| Thumbnails appear for delayed images | ⬜ | Manual Verification 5.1 |
| No placeholders remain | ⬜ | Manual Verification 5.1 |
| No manual refresh needed | ⬜ | Manual Verification 5.1 |

---

## Completion Log

| Task | Completed | Duration | Notes |
|------|-----------|----------|-------|
| ... | ... | ... | ... |

---

## Blockers & Issues

| Issue | Impact | Resolution |
|-------|--------|------------|
| ... | ... | ... |

**Next Step**: Implementation
