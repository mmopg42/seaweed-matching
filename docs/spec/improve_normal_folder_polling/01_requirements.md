---
Task: improve_normal_folder_polling
Created: 2026-01-08
Status: Draft
Summary: Improve Normal monitoring to handle image generation delay and optimize polling interval
Research Required: No
---

# Improve Normal Folder Polling - Requirements

## 1. Goal

### 1.1 Primary Goal

Eliminate the 20-40 second delay in displaying Normal camera images by ensuring the system actively re-checks folders until images appear, while optimizing the polling interval for better responsiveness.

### 1.2 Success Criteria

- [ ] Normal folders detected without `stitched_original.png` are NOT added to `_knownFiles` (Already Implemented - Verify Only).
- [ ] Image files appearing 20-40 seconds after folder creation are correctly detected and displayed in the UI automatically.
- [ ] Polling interval is updated to 500ms (0.5 seconds) in the default configuration.
- [ ] No regression in duplicate handling; `GroupManager` correctly filters duplicate group creation requests.

## 1.3 Current Implementation Status

- **FileWatcherService**: The logic to skip `_knownFiles` for Normal folders without images is **already implemented**.
- **Pending Work**: Only the polling interval configuration needs to be updated.

## 2. Constraints

### 2.1 Technical Constraints

- Must rely on existing `processedFiles` deduplication in `GroupManager` to prevent logic errors.
- Must not introduce significant CPU/Disk I/O overhead despite the faster polling interval (500ms).

### 2.2 Business Constraints

- Must be implemented immediately to resolve user inconvenience.

### 2.3 Non-Goals (Out of Scope)

- Implementing a full "Pending Normal Folder" management system (using the simpler `_knownFiles` exclusion method instead).
- Changing the overall architecture of `FileWatcherService`.

## 3. Assumptions

- The `FileWatcherService` polling mechanism visits every folder in the monitoring path.
- `stitched_original.png` is the only trigger file required for Normal folders to be considered "complete" for visualization.
- The `GroupManager`'s lock and `_processedFiles` HashSet are thread-safe and sufficient to handle the increased event firing rate.

## 4. Dependencies

### 4.1 Blocked By

- None

### 4.2 Blocks

- None
