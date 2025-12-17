# Research: Missing Normal/NIR Data Display

## 1. Problem Description
Real-time monitoring fails to detect new files in "Normal" and "NIR" folders on the network drive (`Z:\...`), resulting in incomplete file groups (Camera only).

## 2. Diagnosis Steps & Results
- **Log Analysis**:
  - `Startup`: "Matching Config Loaded" confirmed correct paths.
  - `Runtime`: "Processing Camera file" logs appear, but **NO** logs for Normal/NIR files.
  - `Injected Logs`: `DetermineFileType` logs (added for diagnosis) never appear for Normal/NIR files.
  - **Conclusion**: The `FileWatcher` event is never fired for these files.

- **Implementation Verification**:
  - `FileWatcherService.cs`: Uses `FileSystemWatcher` exclusively. **No polling logic implemented.**
  - `ApplicationConfiguration.cs`: Contains `EnableNetworkDrivePolling` (default: true) and `PollingIntervalMs` (default: 5000).
  - `App.xaml.cs`: Registers `FileWatcherService` as the singleton `IFileWatcher`.

- **Architecture Check**:
  - Verified `module_file_group_matcher.md` for file formats.
  - Confirmed `FileSystemWatcher` uses `*.*` filter (no file format excluded).
  - **Root Cause**: `FileSystemWatcher` is unreliable on network drives (SMB shares) and often fails to raise events. The system was designed to have a Polling fallback (evident in Config), but it was never implemented in `FileWatcherService`.

## 3. Root Cause
**Missing Feature**: The `FileWatcherService` does not implement the Polling mechanism required for reliable network drive monitoring, despite the configuration and architecture supporting/expecting it.

## 4. Proposed Fix
Implement Polling capability in `FileWatcherService`:
1.  Add `PollingInterval` property (or inject config).
2.  Implement a periodic timer to scan watched directories.
3.  Maintain a `HashSet` of known files to detect additions.
4.  Fire `FileChanged` (Created) events when new files are discovered during polling.
5.  Ensure thread safety and debouncing (MonitoringOrchestrator already handles debouncing).
