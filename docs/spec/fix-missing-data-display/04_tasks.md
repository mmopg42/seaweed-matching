# Task: Implement Polling in FileWatcherService

## Overview
Implement a robust polling mechanism to support reliable network drive monitoring, preventing "Missing Normal/NIR Data" issues.

## Dependencies
- [x] `docs/spec/fix-missing-data-display/01_requirements.md` (Approved)
- [x] `docs/spec/fix-missing-data-display/03_plan.md` (Approved)

## Checklist

### 1. Interface & Model Changes
- [x] Define `FileWatcherOptions` class in `IFileWatcher.cs`
  - `bool EnablePolling`
  - `int PollingIntervalMs`
- [x] Update `IFileWatcher.StartWatchingAsync` signature to accept `FileWatcherOptions`

### 2. FileWatcherService Core Logic
- [x] Add state variables to `FileWatcherService.cs`:
  - `HashSet<string> _knownFiles`
  - `object _lockObject` (for thread safety)
  - `Timer _pollingTimer`
- [x] Update `StartWatchingAsync`:
  - Implement **Silent Baseline Scan** (Populate `_knownFiles` without events)
  - Initialize and start `_pollingTimer` if enabled
- [x] Update `StopWatchingAsync` / `Dispose`:
  - Check for `Timer` disposal and `_knownFiles` cleanup
- [x] Implement `OnFileSystemEvent` synchronization:
  - Add created files to `_knownFiles` (Thread-safe)
  - Remove deleted files from `_knownFiles` (Thread-safe)

### 3. Polling Logic Implementation
- [x] Implement `PollDirectories` method:
  - [x] Iterate through all watched paths
  - [x] Use `Directory.EnumerateFiles` for checking
  - [x] **Detect New**: Compare snapshot vs `_knownFiles`, add & fire Created event
  - [x] **Detect Deleted**: Compare `_knownFiles` vs snapshot, remove & fire Deleted event
  - [x] Implement debouncing (skip if previous poll running)
  - [x] Add error handling (try/catch)

### 4. Integration
- [x] Update `MonitoringOrchestrator.StartAsync`:
  - Create `FileWatcherOptions`
  - Populate from `ApplicationConfiguration.WorkflowSettings` (`EnableNetworkDrivePolling`, `PollingIntervalMs`)
  - Pass options to `StartWatchingAsync`

### 5. Verification
- [x] Build solution
- [x] Run application with network drive paths configured
- [x] Verify logs: "Polling started..."
- [x] Manual Test: Add file to network folder -> Verify "Polling detected new file" in logs
