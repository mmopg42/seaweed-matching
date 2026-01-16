# Troubleshooting: Phantom Images after Refresh

## 1. Problem Description
**Symptom**: When the user changes the watch path to an empty folder (or one with no images) and clicks "Refresh", the Application does NOT show an empty screen. Instead, the previous images/groups remain visible.
**Expected Behavior**: The UI should clear all File Groups and show an empty state.

## 2. Root Cause Analysis

### 2.1 Backend vs Frontend State
- **Backend (`MonitoringOrchestrator`)**: correctly calls `_groupManager.ResetState()`, which clears `_activeGroups` and `_processedFiles`. The backend state IS empty.
- **Frontend (`MainWindowViewModel`)**: maintains an `ObservableCollection<FileGroupViewModel> FileGroups`. This collection is updated via events (`GroupCreated`, `GroupRemoved`).

### 2.2 The Missing Link
- When `RefreshAsync` calls `_groupManager.ResetState()` (or `Clear()`), it **does NOT** fire individual `GroupRemoved` events for performance reasons (clearing 1000s of items one by one would freeze the UI).
- Consequently, `MainWindowViewModel` never receives a notifications that the groups have been removed.
- When `InitialScan` runs on the new empty folder, it finds 0 files, so it fires 0 `GroupCreated` events.
- Result: **UI retains the stale collection** from before the Refresh.

## 3. Proposed Fix

### 3.1 Architecture Change
We need a "Bulk Reset" signal.

1.  **Modify `IMonitoringOrchestrator`**:
    - Add `event EventHandler MonitoringStateReset;`

2.  **Update `MonitoringOrchestrator`**:
    - Fire `MonitoringStateReset` inside `CoreResetAndScanAsync` (before scanning).

3.  **Update `MainWindowViewModel`**:
    - Subscribe to `MonitoringStateReset`.
    - In the handler, call `FileGroups.Clear()` (on the UI thread).

## 4. Implementation Plan (Amendment to `fix_refresh_logic`)

This fix is part of the "Fix Refresh Logic" scope, as a "Deep Reset" must include the UI.

1.  **Interface**: Add event to `IMonitoringOrchestrator`.
2.  **Implementation**: Fire event in `MonitoringOrchestrator`.
3.  **Wiring**: Subscribe in `MainWindowViewModel`.
