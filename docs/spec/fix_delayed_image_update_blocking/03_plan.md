---
Task: fix_delayed_image_update_blocking
Created: 2026-01-13
Status: Draft
Depends On: 02_research.md
---

# Fix Delayed Image Update Blocking - Plan

## 1. Proposed Changes

### 1.1 Core Strategy

The primary fix is to remove the aggressive "already processed" check in `GroupManager` for Normal folders (and potentially all types) because `GroupManager` is designed to handle updates/merges. Deduplication is already handled at the infrastructure level (`FileWatcherService` and `EventProcessor`). `GroupManager` should focus on whether a group's state *actually* changes.

### 1.2 GroupManager (`GroupManager.cs`)

- [ ] **Modify** `CreateOrUpdateGroupAsync`:
    - Remove the early return for `_processedFiles.Contains(filePath)`.
    - Or, move the `_processedFiles.Add` to the end, only after confirming that the file has been successfully integrated or if it's a non-updateable type.
    - **Recommended**: Completely remove the `_processedFiles` check within `GroupManager` as it contradicts the "Update" part of `CreateOrUpdateGroupAsync`.

### 1.3 EventProcessor (`EventProcessor.cs`)

- [ ] **Review** `ShouldSkipEvent`:
    - Ensure that the 2-second debounce doesn't accidentally swallow the image event if it follows the folder event too closely. 2 seconds might be too long for local system events.
    - Consider reducing to 500ms or excluding `stitched_original.png` from debouncing.

## 2. Verification Plan

### 2.1 Manual Test Scenario

1.  **Preparation**:
    - Use a Python script to simulate:
        a. Create directory `Normal1/20260113_150000_1`.
        b. Wait 5 seconds.
        c. Create file `Normal1/20260113_150000_1/stitched_original.png`.
2.  **Expected result**:
    - UI shows a row for the folder immediately.
    - After 5 seconds, the row updates with the thumbnail automatically.

### 2.2 Automated Unit Tests

- [ ] Add unit test to `GroupManagerTests` (if exists) or create new one:
    - Call `CreateOrUpdateGroupAsync` with folder path first.
    - Call `CreateOrUpdateGroupAsync` with the same folder path again (simulating image arrival).
    - Assert that `GroupUpdated` is fired and the group state is updated.

---

## Approval

- [ ] Target components identified
- [ ] Verification plan defined

**Next Step**: 04_design.md
