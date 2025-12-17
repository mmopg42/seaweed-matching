---
Owner: Development Team
Last Updated: 2025-12-15
Related PRs: []
Code Ref: (FileWatcherService.cs)
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
- 2025-12-15: Added Polling support and Silent Baseline Scan.
