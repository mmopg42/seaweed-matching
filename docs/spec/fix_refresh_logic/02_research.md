---
Task: fix_refresh_logic
Created: 2026-01-12
Status: Approved
Summary: Analysis confirms "Double Scan" inefficiency and potential race conditions. Proposed "InjectKnownFiles" pattern for optimization.
Research Required: No
---

# Fix Refresh Logic - Research (Updated)

## 1. Critical Issues Analysis

### 1.1 Performance: The "Double Scan" Problem
- **Observation**: 
  1. `MonitoringOrchestrator.PerformSequentialInitialScanAsync` calls `InitialScanner.ScanAndSortFilesAsync` (Full Disk I/O).
  2. `FileWatcherService.StartWatchingAsync` calls `PerformSilentScan` (Full Disk I/O again).
- **Impact**: For N files, we perform 2N I/O operations. With 10,000+ files, this doubles the freeze time during Refresh.
- **Estimated Cost**: ~50ms per 1,000 files x 2. (Significant on net-drives or WSL).

### 1.2 State Safety: Race Conditions
- **Stopping**: `FileWatcherService.StopWatchingAsync` disposes the Timer but does **not** wait for currently executing `OnPollTick` to finish.
- **Risk**: If `OnPollTick` is blocked on a lock while `Stop` clears critical collections, it might crash or resurrect "Zombie" state.
- **Locking**: Both services use `lock(_lockObject)`.
- **Solution**: `StopWatchingAsync` should acquire the lock once before clearing, forcing any pending Poll Tick to finish or block until disposal is marked.

## 2. Updated Solution Strategy

### 2.1 Optimization: "Scan Once, Use Everywhere"
- **Change**: `FileWatcherService` should allow starting **without** a self-scan if provided with a list of pre-scanned files.
- **Flow**:
  1. Orchestrator scans files (`InitialScanner`).
  2. Orchestrator creates groups.
  3. Orchestrator passes the *already scanned* file list to `FileWatcherService`.
  4. `FileWatcherService` skips `PerformSilentScan` and populates `_knownFiles` directly.

### 2.2 Thread Safety Enhancement
- **Action**: Modify `FileWatcherService.StopWatchingAsync` to:
  ```csharp
  public async Task StopWatchingAsync() {
      _pollingTimer?.Dispose(); // Stop future ticks
      // ... stop watchers ...
      
      // Wait for any active logic to drain
      lock (_lockObject) { 
          _isWatching = false; // Guard flag
          _knownFiles.Clear(); 
      }
  }
  ```

## 3. Impact Re-assessment

- **Performance**: Optimized approach cuts I/O by 50%.
- **Safety**: Robust against "Refresh while Polling".
- **Complexity**: Low. Requires new overload in `IFileWatcherService`.

## 4. Verification

- **Log Check**: Verify "Silent baseline scan detected X items" appears only **ONCE** (or "Skipped silent scan" log appears).
- **Stress Test**: Hammer "Refresh" button while simulating file IO (copying files) to trigger race conditions.
