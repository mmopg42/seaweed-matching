# Implementation Plan: Robust Network Drive Polling
> **Goal**: Implement a reliable polling fallback for `FileWatcherService` to detect files on network drives (Z:) where real-time events are dropped, while ensuring no duplicate events and thread safety.

## 1. Architecture Design

### Hybrid Monitoring Strategy
We will combine `FileSystemWatcher` (Push) and `Polling` (Pull):
- **FileSystemWatcher**: Low latency, primary detection for local files.
- **Polling**: High latency (e.g., 5s), fallback detection for network files.
- **Safety**: A `_knownFiles` registry will synchronize both methods to prevent duplicate events.

### The "Silent Baseline" Rule
When `StartWatchingAsync` is called, `FileWatcherService` will perform an initial scan to populate `_knownFiles` **without raising events**.
- **Reason**: The `MonitoringOrchestrator` performs its own `InitialScan` before enabling the watcher. Raising events for existing files would cause massive duplicate processing (re-processing all thousands of files).

## 2. Component Changes

### [MODIFY] [IFileWatcher.cs](file:///c:/workspace/seaweed/gui_kiro/ChronoView/Core/FileWatching/IFileWatcher.cs)
- Introduce `FileWatcherOptions` class.
- Update `StartWatchingAsync` signature to accept options.

```csharp
public class FileWatcherOptions
{
    public bool EnablePolling { get; set; } = false;
    public int PollingIntervalMs { get; set; } = 5000;
}

// Interface
Task StartWatchingAsync(IEnumerable<string> paths, FileWatcherOptions options);
```

### [MODIFY] [FileWatcherService.cs](file:///c:/workspace/seaweed/gui_kiro/ChronoView/Core/FileWatching/FileWatcherService.cs)

#### A. State Management
- Add `HashSet<string> _knownFiles`: Tracks all currently present files.
- Add `object _lockObject`: Ensures thread-safe access to `_knownFiles`.
- Add `Timer _pollingTimer`: Triggers periodic scans.

#### B. Updated `StartWatchingAsync`
1. **Silent Baseline Scan**: Iterate all `paths`. For each file, add to `_knownFiles`.
2. **Start Watchers**: Initialize `FileSystemWatcher` (existing logic).
3. **Start Polling**: If `options.EnablePolling` is true, start `_pollingTimer`.

#### C. Event Synchronization
- **OnFileSystemEvent (Watcher)**:
  - If `Created`: Add to `_knownFiles`. If already exists (race condition), ignore. Fire event.
  - If `Deleted`: Remove from `_knownFiles`. Fire event.
- **PollDirectories (Timer)**:
  - Get current file snapshot for all paths.
  - **Detect New**: `Current - Known`. For each new file:
    - Add to `_knownFiles`.
    - Fire `Created` event (manual invocation of `OnFileChanged`).
  - **Detect Deleted**: `Known - Current` (only for watched folders).
    - Remove from `_knownFiles`.
    - Fire `Deleted` event (optional, but good for consistency).

#### D. Polling Logic Details
- **Debouncing**: Ensure only one poll runs at a time (skip if previous poll valid).
- **Efficiency**: Use `Directory.EnumerateFiles` for low memory footprint.
- **Error Handling**: Try-catch around I/O operations (network glitches should not crash service).

### [MODIFY] [MonitoringOrchestrator.cs](file:///c:/workspace/seaweed/gui_kiro/ChronoView/Core/FileWatching/MonitoringOrchestrator.cs)
- Update `StartAsync` to create `FileWatcherOptions`.
- Map values from `ApplicationConfiguration`:
  - `EnablePolling` <- `WorkflowSettings.EnableNetworkDrivePolling`
  - `PollingIntervalMs` <- `WorkflowSettings.PollingIntervalMs`

## 3. Implementation Steps

### Step 1: Interface & Options
- Define `FileWatcherOptions` in `IFileWatcher.cs`.
- Update interface signature.

### Step 2: Implementation in FileWatcherService
- Implement `_knownFiles` management.
- Implement `PollDirectories` method.
- Update `StartWatchingAsync` / `StopWatchingAsync`.

### Step 3: Integration
- Update `MonitoringOrchestrator` to pass checking configuration.

## 4. Verification Plan

### Manual Verification
1. **Setup**:
   - Set `PollingIntervalMs` to 5000 (5s) in `settings.json` (or verify default).
   - Ensure `EnableNetworkDrivePolling` is true.
2. **Execution**:
   - Run `dotnet run`.
   - Check logs for "Polling started".
3. **Test Case: Network Detection**:
   - Open folder `Z:\...\normal` (or simulated monitored folder).
   - Copy a file `test_image.jpg`.
   - **Watcher Check**: If `FileSystemWatcher` misses it, wait 5 seconds.
   - **Log Check**: Expect `[FileWatcherService] Polling detected new file: ... \test_image.jpg`.
   - **UI Check**: Verify group appears in ChronoView.
