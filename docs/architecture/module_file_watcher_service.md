---
Owner: Development Team
Last Updated: 2025-12-18
Related PRs: []
Code Ref: 2025-12-18 (Normal folder event filtering enhancement)
---

# FileWatcherService

## Overview
`FileWatcherService` provides reliable file system monitoring by combining real-time events (`FileSystemWatcher`) with periodic polling (`Timer`). This hybrid approach ensures file detection even on network drives (SMB shares) where event reliability is poor.

## Key Components
- **File**: `ChronoView/Core/FileWatching/FileWatcherService.cs`
- **Interface**: `IFileWatcher`
- **Class**: `FileWatcherOptions`

## Contracts
- **Inputs**: 
  - `paths`: List of directory paths to watch.
  - `options`: Configuration (`EnablePolling`, `PollingIntervalMs`).
- **Outputs**: 
  - `FileChanged` event (Unified stream of file system events).
  - `HealthStatusChanged` event.
- **Errors/Exceptions**: 
  - Logs errors for invalid paths or access denied.
  - Degrades health status on watcher errors.

## Logic Flow

### Hybrid Monitoring Strategy
1. **Silent Baseline Scan**: At startup, scans all files to populate `_knownFiles` *without firing events*.
2. **Real-time Watcher**: Listens for `Created`, `Deleted`, `Renamed`, `Changed`.
3. **Polling (Optional)**: Periodically scans directories to find files missed by the watcher.

### Duplicate Prevention
To prevent processing the same file twice (e.g., detected by both Watcher and Poller), the service uses a `_knownFiles` (HashSet) registry:
1. **Watcher Event**: If file in `_knownFiles`, ignore. Else, add to set and fire event.
2. **Polling Event**: New entries in snapshot are added to set and fired as `Created`.

### Normal Folder Event Filtering (2025-12-18)
**Location**: `FileWatcherService.cs:491-538` (`ShouldProcessEvent`)

**Special Case: stitched_original.png Conversion**
- `stitched_original.png` files inside Normal folders are converted to folder events
- Must be allowed through BEFORE filtering files inside Normal folders
- Logic: Check for `stitched_original.png` filename → allow through for conversion

**Normal Folder Detection**:
- Detects folder events (no extension) that match Normal folder pattern
- Pattern: Starts with "C" and contains "T" (e.g., "C251201T140609_0")
- Allows folder events to pass through for processing

**File Inside Normal Folder Filtering**:
- Files (with extension) inside Normal folders are filtered out
- Exception: `stitched_original.png` is allowed (converted to folder event)
- Prevents duplicate processing of individual image files
- Folder event handles the entire Normal folder as single unit

**Why This Matters**:
- Normal folders contain multiple image files
- Processing each image separately would create duplicate groups
- Folder-level processing ensures single group per Normal folder
- See `module_monitoring_orchestrator.md` Fix 3 for processKey unification

## Dependencies
- **Configuration**:
  - `FileWatcherOptions.EnablePolling`: Enables/disables the polling timer.
  - `FileWatcherOptions.PollingIntervalMs`: Frequency of polling scans.

## Failure Modes & Recovery
- **Network Disconnect**: Polling may throw exceptions (logged), but service continues to retry on next tick.
- **Watcher Error**: Sets `HealthStatus` to `Degraded` or `Unhealthy`.

## Impact / Touchpoints
<!-- VERIFY: grep -rn "FileWatcherService" ChronoView/ -->
- `MonitoringOrchestrator`: Primary consumer.
- `ApplicationConfiguration`: Source of polling settings.

## Changelog
- **2025-12-18**: Enhanced Normal Folder Event Filtering
  - Added special case handling for `stitched_original.png` conversion
  - Improved Normal folder detection pattern matching
  - Prevents duplicate processing of files inside Normal folders
  - Location: `FileWatcherService.cs:491-538` (`ShouldProcessEvent`)
  - Related: `docs/trouble/normal_nir_matching_failure.md` for detailed analysis
- **2025-12-15**: Added Polling support and Silent Baseline Scan.
