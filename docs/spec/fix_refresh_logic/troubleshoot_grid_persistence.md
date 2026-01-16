# Troubleshooting: Grid Persists Old Data after Refresh

## 1. Problem Description
**Symptom**: After changing the file path to an empty directory and clicking "Refresh", the Grid still displays the previous images.
**Expectation**: The Grid should clear because the new directory is empty.

## 2. Analysis of "Phantom UI" Fix
The previous fix implemented:
1.  **Backend Signal**: `MonitoringOrchestrator` fires `MonitoringStateReset` when resetting.
2.  **UI Response**: `DashboardViewModel` subscribes to this and calls `ClearFileGroups()`.
3.  **Result**: `FileGroups` (and `Line1Groups`/`Line2Groups`) are explicitly cleared.

If the Grid still shows data, there are two possibilities:
1.  The Clear didn't happen (Event failure).
2.  The Clear happened, but **Old Data was immediately re-added**.

## 3. Root Cause Identification: "Stale Configuration"
The user report states: *"It is loading the previous images."*
This strongly implies **Possibility #2**.

If `RefreshAsync` were scanning the *new* (empty) directory, it would find 0 files, and the UI would remain empty (after the Clear).
The fact that it finds images means it is **scanning the Old Directory**.

### Chain of Failure
1.  User updates settings in `SettingsDialog`.
2.  User clicks "Refresh".
3.  `MonitoringOrchestrator.RefreshAsync` calls `_configManager.LoadConfiguration()`.
4.  **FAILURE POINT**: This Load operation returns the **Old Configuration** (Old Path).
5.  `PerformInitialScanAsync` scans the Old Path.
6.  Old files are found -> `GroupCreated` events fired -> UI repopulates.

### Why Stale Config?
Possible reasons:
- **Save Delay**: Settings file (`config.json`) wasn't finished writing when Refresh started.
- **Save Failure**: `SettingsDialog` failed to save to disk (silent error?).
- **Instance Mismatch**: Unlikely (Singleton DI), but if multiple `config.json` locations exist.

## 4. Proposed Resolution
1.  **Verify Configuration Path**: Add logging to `RefreshAsync` to print the exact path being scanned.
2.  **Force Save/Reload**: Ensure `SettingsDialog` awaits file write completion before closing.
3.  **Verify Scan Logic**: Ensure `InitialScanner` doesn't have internal caching (Verified: it does not).
