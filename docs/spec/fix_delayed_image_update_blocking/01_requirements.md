---
Task: fix_delayed_image_update_blocking
Created: 2026-01-13
Status: Draft
Summary: Fix bug where GroupManager._processedFiles prevents UI updates for delayed Normal images
Research Required: Yes
---

# Fix Delayed Image Update Blocking - Requirements

## 1. Goal

### 1.1 Primary Goal

The system must successfully update the UI with thumbnails when `stitched_original.png` is created in a Normal folder, even if the folder was already detected and processed as an empty placeholder.

### 1.2 Success Criteria

- [ ] Images in Normal folders are displayed in the UI automatically upon creation.
- [ ] No manual refresh is required to see the delayed images.
- [ ] The "already processed" logic in `GroupManager` does not block legitimate data updates (like images appearing in folders).
- [ ] System remains optimized and doesn't re-process the exact same file event indefinitely.

## 2. Constraints

### 2.1 Technical Constraints

- Must work with the existing `FileGroupMatcher` and `GroupManager` architecture.
- Must not introduce duplicate rows in the UI grid.
- Must handle both polling and native file system events.

### 2.2 Business Constraints

- Minimal impact on existing matching performance.

### 2.3 Non-Goals (Out of Scope)

- Refactoring the entire `FileGroup` matching logic.
- Changing how NIR camera data is handled.

## 3. Questions to Investigate

- [x] Q1: Why does `GroupManager` ignore the second event (image creation)?
- [x] Q2: How does `MonitoringOrchestrator` path normalization interact with `GroupManager`'s deduplication?
- [x] Q3: Should we use a more granular deduplication key?

## 4. Assumptions

- `FileWatcherService` correctly fires events for `stitched_original.png`.
- `MonitoringOrchestrator` correctly maps image events to their parent folders.

## 5. Dependencies

### 5.1 Blocked By

| Dependency | Status | Owner |
|------------|--------|-------|
| None       | -      | -     |

### 5.2 Blocks

| Dependent Task | Impact if Delayed |
|----------------|-------------------|
| General usage  | Users see empty rows permanently |

---

## Approval

- [ ] Requirements reviewed and approved

**Next Step**: 02_research.md
