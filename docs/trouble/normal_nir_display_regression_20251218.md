# Normal and NIR Display Failure - Regression Analysis

**Date**: 2025-12-18
**Status**: Investigated
**Severity**: High
**Symptom**: Normal camera and NIR data not displaying in UI after event processing optimization changes

---

## 1. Problem Summary

### 1.1 Observed Behavior

After implementing event processing optimization (event_processing_optimization spec), the application successfully builds and runs, but **Normal camera folders and NIR text files are not being detected and displayed** in the UI.

### 1.2 Expected Behavior

Based on DataSequenceSettings order:
1. **NIR files** should be detected first (Order: 1)
2. **Normal folders** should be detected (Order: 2)
3. **Camera 1-6 files** should be detected (Order: 3-8)

All detected data should be matched into groups and displayed in the DataGrid.

---

## 2. Investigation Findings

### 2.1 Build Status

✅ **Build successful** - no compilation errors
```
ChronoView net10.0-windows 3 경고 와 함께 성공
```

**Warnings observed** (non-blocking):
- `CS0618`: Deprecated properties in `MatchingSettings` (legacy time window settings)
- These warnings do not affect runtime behavior

### 2.2 Runtime Analysis

From application output:
```
MainWindowViewModel disposed and all event subscriptions cleaned up
```

This indicates the application starts and shuts down cleanly, but the issue is that Normal/NIR data is not being processed during runtime.

### 2.3 Potential Root Causes

#### 2.3.1 Hypothesis 1: FileWatcher Path Registration Issue (High Probability)

**Evidence**:
- Code location: `MonitoringOrchestrator.cs:140-194`
- Debug logging shows paths being collected for FileWatcher

**Potential Problem**:
```csharp
// Line 140-194: Path collection logic
var watchPaths = new List<string>();
var matchingConfig = _fileGroupMatcher.Configuration;

if (!string.IsNullOrEmpty(matchingConfig.Nir1Path))
{
    watchPaths.Add(matchingConfig.Nir1Path);
    _logger.LogError("DEBUG: Added Nir1Path: {Path}", matchingConfig.Nir1Path);
}

if (!string.IsNullOrEmpty(matchingConfig.Normal1Path))
{
    watchPaths.Add(matchingConfig.Normal1Path);
    _logger.LogError("DEBUG: Added Normal1Path: {Path}", matchingConfig.Normal1Path);
}
```

**Issue**: If `FileGroupMatcher.Configuration` is null or paths are not correctly set, Normal/NIR paths won't be registered with FileWatcher.

**Verification**:
- Check if `DEBUG` log messages appear showing `Nir1Path` and `Normal1Path` being added
- If missing → Configuration not loaded correctly
- If present but paths empty → Configuration file issue

#### 2.3.2 Hypothesis 2: FileWatcherOptions Removal Side Effect (Medium Probability)

**Change Made**:
```csharp
// Before (polling enabled)
var watcherOptions = new FileWatcherOptions
{
    EnablePolling = config.WorkflowSettings.EnableNetworkDrivePolling,
    PollingIntervalMs = config.WorkflowSettings.PollingIntervalMs
};

// After (pure event-based)
var watcherOptions = new FileWatcherOptions(); // Line 219
```

**Potential Issue**:
- Network drives may not reliably generate file system events
- Polling was previously compensating for missed events
- Pure event-based approach may miss Normal/NIR files on network paths

**Verification**:
- Check if data paths are on network drives (Z:\, \\server\, etc.)
- Test with local paths to isolate network drive issue

#### 2.3.3 Hypothesis 3: Event Channel Flow Broken (Low Probability)

**Recent Changes**:
- Introduced `PriorityEventChannel` for event processing
- Events flow: `FileWatcher` → `OnFileChanged` → `_internalEventChannel` → `ProcessEventsWorkerAsync` → `ProcessSingleEventAsync`

**Potential Issue**:
```csharp
// Line 318-336: OnFileChanged
private void OnFileChanged(object? sender, FileSystemEventArgs e)
{
    if (!_internalEventChannel.Writer.TryWrite(e))
    {
        _logger.LogWarning("Failed to queue event to internal channel: {Path}", e.FullPath);
    }
}
```

If event channel is full or not initialized, events could be dropped.

**Verification**:
- Check logs for "Failed to queue event" warnings
- Check if workers are started (log: "Started {Count} event processing workers")

#### 2.3.4 Hypothesis 4: Parallel Processing Race Condition (Low Probability)

**New Infrastructure**:
```csharp
// Line 198-212: Parallel processing setup
_maxParallelWorkers = config.WorkflowSettings?.MaxEventProcessingWorkers ?? 3;
_parallelismLimiter = new SemaphoreSlim(_maxParallelWorkers);
```

**Potential Issue**:
- Multiple workers processing events concurrently
- Race condition in `_activeGroups` (ConcurrentDictionary) access
- Group creation/update conflicts

**Verification**:
- Check if groups are created but immediately removed
- Check logs for duplicate group IDs or merge conflicts

---

## 3. Diagnosis Checklist

Complete these checks in order to identify the root cause:

### 3.1 Configuration Verification

- [ ] Open application configuration file (JSON/settings)
- [ ] Verify `Nir1Path` is set correctly (e.g., `Z:\...\nir`)
- [ ] Verify `Normal1Path` is set correctly (e.g., `Z:\...\normal`)
- [ ] Verify paths point to correct network/local drives
- [ ] Check if `MaxEventProcessingWorkers` is set (default: 3)

### 3.2 Log Analysis

**Critical logs to find**:

#### Startup Logs:
- [ ] `"DEBUG: Collecting paths to watch..."` (line 140)
- [ ] `"DEBUG: Added Nir1Path: {Path}"` (line 153)
- [ ] `"DEBUG: Added Normal1Path: {Path}"` (line 159)
- [ ] `"Starting file watcher with pure event-based detection"` (line 221)
- [ ] `"Initialized parallel processing with {X} workers"` (line 211)
- [ ] `"Started {X} event processing workers"` (line 242)

#### Event Detection Logs:
- [ ] `"FileSystemEvent detected: Created - {Path}"` (FileWatcherService)
- [ ] `"Normal folder detected: {Name}"` (FileWatcherService)
- [ ] `"Event queued for processing"` (line 329)

#### Processing Logs:
- [ ] `"Worker {Id} processing: {Path}"` (line 388)
- [ ] `"Checking Normal: Path={Path}, N1={N1}"` (line 2302)
- [ ] `"Folder Identified as Normal: {Path}"` (line 2307)
- [ ] `"File Identified as NIR: {Path}"` (line 2343)

**If missing**:
- Startup logs missing → Configuration not loading paths
- Event logs missing → FileWatcher not detecting file system changes
- Processing logs missing → Events not flowing through channel

### 3.3 Quick Test

**Test 1: Manual File Creation**
1. Start application
2. Manually create a Normal folder (e.g., `C251218T120000_0`)
3. Check logs for "Normal folder detected"
4. Create a NIR file (e.g., `run_120251218T120000A.txt`)
5. Check logs for "File Identified as NIR"

**Test 2: Initial Scan**
1. Place existing Normal folders and NIR files in configured paths
2. Start application
3. Check if initial scan detects them (log: "Initial scan completed: {X} files scanned")
4. Check UI to see if groups appear

---

## 4. Proposed Solutions

### 4.1 Immediate Actions

#### Action 1: Enable Verbose Logging
Temporarily set log level to `Debug` or `Trace` to capture all diagnostic messages.

Location: `appsettings.json` or equivalent
```json
{
  "Logging": {
    "LogLevel": {
      "ChronoView.Core.FileWatching": "Debug"
    }
  }
}
```

#### Action 2: Verify Configuration Loading
Add temporary debug output at application startup:

```csharp
// After line 106 in MonitoringOrchestrator.cs
_logger.LogError("CONFIG CHECK: Nir1Path={Nir1}, Normal1Path={Normal1}", 
    config.MatchingSettings.Nir1Path, 
    config.MatchingSettings.Normal1Path);
```

### 4.2 Temporary Workaround

If network drive events are unreliable, temporarily re-enable polling:

**NOTE**: This contradicts the optimization spec (Priority 2.1: Remove Polling) but can serve as a diagnostic tool.

```csharp
// Temporarily add to FileWatcherOptions (for testing only)
var watcherOptions = new FileWatcherOptions
{
    EnablePolling = true,  // TEMPORARY
    PollingIntervalMs = 2000
};
```

**Important**: Remove this workaround after fixing the root cause.

### 4.3 Long-term Fix (Depending on Root Cause)

#### If Cause is Configuration Issue:
- Fix configuration loading logic
- Add validation to ensure required paths are set
- Add startup warning if paths are missing

#### If Cause is Network Drive Events:
- Implement hybrid approach: events for local drives, polling fallback for network drives
- Detect drive type at startup
- Document limitation in architecture docs

#### If Cause is Event Channel Issue:
- Increase channel capacity
- Add monitoring for channel fullness
- Add circuit breaker for dropped events

#### If Cause is Race Condition:
- Re-review locking strategy in `CreateOrUpdateGroupAsync`
- Add transaction-like guarantees for group creation
- Add race condition detection and retry logic

---

## 5. Comparison with Previous Issue

### 5.1 Previous Issue (`normal_nir_matching_failure.md`)

**Root Cause**:
- `DetermineFileType` returned `FileType.Unknown` for Normal/NIR files
- `Directory.Exists()` check failed due to network delay (race condition)
- Path comparison failed due to trailing backslash mismatch

**Fixes Applied**:
- Use `Path.HasExtension()` check before `Directory.Exists()`
- Normalize paths with `TrimEnd('\\', '/')` before comparison

### 5.2 Current Issue (Suspected Differences)

**New Factor**: Event Processing Optimization
- New parallel worker architecture
- New event channel infrastructure
- Polling removal

**Hypothesis**: The issue is now **earlier in the pipeline** (event detection/registration) rather than file type determination.

**Evidence**:
- Previous fix improved `DetermineFileType` logic
- Current issue: likely no events reaching `DetermineFileType` at all

---

## 6. Related Documents

- `docs/spec/event_processing_optimization/01_requirements.md`: Optimization requirements
- `docs/spec/event_processing_optimization/04_design.md`: Design decisions
- `docs/spec/event_processing_optimization/feedback.md`: Implementation feedback
- `docs/trouble/normal_nir_matching_failure.md`: Previous related issue

---

## 7. Next Steps

1. **Immediate**: Run application with debug logging enabled
2. **Collect**: All logs from startup to first few file detections
3. **Analyze**: Use diagnosis checklist (Section 3) to identify missing log entries
4. **Report**: Share logs showing:
   - Path registration status
   - Event detection status
   - Worker status
5. **Fix**: Based on identified root cause, apply corresponding solution (Section 4)

---

## 8. Code References

### Key Files:
- `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs`
  - Lines 140-194: Path collection for FileWatcher
  - Lines 218-230: FileWatcher startup
  - Lines 237-242: Worker task initialization
  - Lines 318-336: `OnFileChanged` event handler
  - Lines 2293-2382: `DetermineFileType` logic

- `ChronoView/Core/FileWatching/FileWatcherService.cs`
  - Event detection and priority assignment
  - Event filtering (`ShouldProcessEvent`)

- `ChronoView/Core/FileWatching/PriorityEventChannel.cs`
  - Event buffering and priority ordering

### Configuration:
- `ChronoView/Models/ApplicationConfiguration.cs`
  - `MatchingSettings.Nir1Path`, `Normal1Path`
  - `WorkflowSettings.MaxEventProcessingWorkers`

---

## Appendix A: Log Template for Diagnosis

When reporting this issue, collect logs matching this template:

```
=== STARTUP ===
[TIME] ChronoView starting...
[TIME] DEBUG: Collecting paths to watch from FileGroupMatcher.Configuration...
[TIME] DEBUG: Added Nir1Path: Z:\path\to\nir
[TIME] DEBUG: Added Normal1Path: Z:\path\to\normal
[TIME] Starting file watcher with pure event-based detection
[TIME] File watcher started. Watching 8 paths
[TIME] Initialized parallel processing with 3 workers
[TIME] Started 3 event processing workers

=== RUNTIME (File Creation) ===
[TIME] FileSystemEvent detected: Created - Z:\path\to\normal\C251218T120000_0
[TIME] Normal folder detected: C251218T120000_0, Timestamp: 2025-12-18 12:00:00
[TIME] Event queued for processing: Created - Z:\path\to\normal\C251218T120000_0
[TIME] Worker 0 processing: Z:\path\to\normal\C251218T120000_0
[TIME] Checking Normal: Path=Z:\path\to\normal\C251218T120000_0, N1=Z:\path\to\normal
[TIME] Folder Identified as Normal: Z:\path\to\normal\C251218T120000_0

=== EXPECTED UI UPDATE ===
[TIME] GroupCreated event raised: group_001
[TIME] UI: Added group group_001 to DataGrid
```

**If any of these logs are missing, that's where the failure occurs.**
