---
Task: fix_refresh_logic
Created: 2026-01-12
Status: Approved
Summary: Implement "Deep Reset" in MonitoringOrchestrator.RefreshAsync with performance optimization (InjectKnownFiles) and strict thread safety.
Research Required: No
---

# Fix Refresh Logic - Plan (Optimized)

## 1. Goal

Implement a robust **Deep Reset** mechanism that ensures state synchronization while eliminating redundant disk I/O ("Double Scan") and preventing race conditions during reset.

## 2. Component Changes

### 2.1 `IFileWatcherService` & `FileWatcherService`

- **New Method**: `StartWatchingAsync(..., IEnumerable<string> knownFiles)`
    - **Purpose**: Allows starting the watcher with a pre-populated list of files, bypassing `PerformSilentScan`.
- **Modify `StopWatchingAsync`**:
    - **Safety**: Acquire `lock (_lockObject)` before clearing `_knownFiles` to ensure no active `OnPollTick` is reading/writing to it during disposal.

### 2.2 `MonitoringOrchestrator`

- **Refactor `RefreshAsync` w/ Deep Reset & Optimization**:
    1.  **Stop**: `_eventProcessor.StopAsync()`, `_fileWatcher.StopWatchingAsync()`.
    2.  **Config**: Reload `ApplicationConfiguration`.
    3.  **Reset Services**:
        - `_groupManager.ResetState()`
        - `_fileGroupMatcher.ResetState()`
        - `_folderTimestamps.Clear()`
        - `_imageCache.Clear()`
        - `_abnormalDetector.Reset()`
    4.  **Restart w/ Optimization**:
        - `_fileGroupMatcher.Configuration = ...`
        - **Scan**: `var result = await PerformInitialScanAsync()` (Returns list of scanned files).
        - **Inject**: Extract file paths from `result` (or `GroupManager`) to create `knownFiles` list.
        - **Start**: `_fileWatcher.StartWatchingAsync(..., knownFiles)` (Skips 2nd scan).
        - `_eventProcessor.Start()`.

### 2.3 `AbnormalDetectorService`

- **Reset**: Ensure `Reset()` clears internal history.

## 3. Implementation Steps

1.  **FileWatcher Update**: Implement method overload and locking safety.
2.  **Orchestrator Update**: Implement the new Refresh flow.
3.  **Configuration**: Ensure `InitialScanner` returns the full list of files it scanned, so we can pass them to FileWatcher. (Currently `InitialScanner` returns `List<(string Path, DataType Type, DateTime Timestamp)>` - we can just project `Path`).

## 4. Verification Plan

- [ ] **Performance Test**:
    - Measure `RefreshAsync` duration with 1000+ files.
    - Expect: Duration ~ Time of `InitialScan`. (StartWatching should be near-instant).
- [ ] **Safety Test**:
    - Rapidly toggle Refresh while files are being copied into the watch folder.
    - Verify no `NullReferenceException` or `CollectionModifiedException`.
- [ ] **Functional Test**:
    - Change Settings (Suffix, Thresholds) -> Refresh -> Verify applied.
    - Delete File -> Refresh -> Verify removed.
