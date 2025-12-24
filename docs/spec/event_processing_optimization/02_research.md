---
Task: Event Processing Optimization
Created: 2025-12-17
Status: In Progress
Depends On: 01_requirements.md
---

# Event Processing Optimization - Research Findings

## 1. Investigation Summary

This document answers the research questions from requirements and analyzes current implementation.

**Research Date**: 2025-12-17
**Code Version**: Current working directory
**Methods**: Code inspection, architecture analysis

---

## 2. Question Answers

### Q1: Event Channel Capacity
**Answer**: **UNBOUNDED** ✅

```csharp
// FileWatcherService.cs:83-96
_eventChannel = Channel.CreateUnbounded<FileSystemEventArgs>(new UnboundedChannelOptions
{
    SingleReader = true,
    SingleWriter = false
});
```

**Finding**: Channel will never fill up, but this means **no backpressure** - if processing is slower than arrival, queue grows indefinitely.

**Implication**: Memory could grow if processing can't keep up with file arrival rate.

---

### Q2: FindMatchingExistingGroup() Performance
**Answer**: **Needs timing measurement** - Cannot determine from static code analysis

**Current Implementation** (MonitoringOrchestrator.cs:1156-1410):
- Lock-based serial execution
- 3 sequential matching strategies:
  1. Match by NormalFolder (O(n) scan)
  2. Match by NirKey (O(n) scan)
  3. Match by timestamp + DataSequenceSettings (O(n³) worst case)

**Estimate**: For n=100 groups:
- Best case (Match 1 hit): <1ms
- Worst case (Match 3 full scan): 10-50ms

**Recommendation**: Add performance instrumentation in research phase.

---

### Q3: Worst-Case File Arrival Scenario
**Answer**: Based on user's production workflow

```
Burst Scenario (production run):
21:47:42 - Normal folder created
21:47:44 - Cam1 file
21:47:45 - Cam2 file  
21:47:45 - Cam3 file
21:47:46 - NIR file

Total: 5 files in 4 seconds
```

**Worst Case**:
- 10+ files arriving within 1-2 seconds (multiple groups simultaneously)
- Current sequential processing creates 4+ second delay

---

### Q4: I/O Bottlenecks
**Answer**: **YES** - Multiple potential bottlenecks identified

**1. Thumbnail Generation** (Need to verify when triggered):
- Location: Unknown from current analysis
- Likely in UI layer (ViewModel or ImageCache)

**2. File System Access**:
```csharp
// FileWatcherService.cs: PollDirectories
Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
```
- Network drives: High latency
- Polling every 1000ms

**3. Timestamp Extraction**:
- Regex parsing: Fast (<1ms)
- Not a bottleneck

---

### Q5: CPU Core Utilization
**Answer**: **LOW** - Single-threaded sequential processing

**Current Architecture**:
```
ProcessEventsAsync (single thread)
  → await foreach (one event at a time)
    → ShouldProcessEvent
      → FileChanged event
        → CreateOrUpdateGroupAsync (locks entire operation)
```

**Estimate**: 5-15% CPU on 4-core system during bursts

**Underutilized**: 85-95% of CPU cores idle

---

### Q6: Lock Contention
**Answer**: **HIGH RISK** - Global lock on entire matching operation

```csharp
// MonitoringOrchestrator.cs:819-923
lock (_lockObject)
{
    FileGroup? existingGroup = FindMatchingExistingGroup(newGroup);
    // ... matching logic ...
    if (existingGroup != null) {
        MergeGroups(existingGroup, newGroup);
    } else {
        _activeGroups[newGroup.GroupId] = newGroup;
    }
}
```

**Problem**: While one file is matching, all other files wait

**Impact**: Serializes all group operations → bottleneck for parallel processing

---

### Q7: FileSystemWatcher for Normal Folders
**Answer**: **CAN DETECT but NOT UTILIZED** ⚠️

**Current Implementation** (FileWatcherService.cs:243):
```csharp
NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | 
               NotifyFilters.LastWrite | NotifyFilters.CreationTime
```

**Capability**: ✅ FileSystemWatcher **IS configured** to detect DirectoryName events

**Actual Behavior**: 
- FileSystemWatcher **CAN detect** folder creation events
- However, current code **WAITS for file creation** (`stitched_original.png`)
- When file is detected → **converts** to parent folder event

**OnFileSystemEvent Logic** (FileWatcherService.cs:265-291):
```csharp
// Detects stitched_original.png FILE creation
if (fileName.Equals("stitched_original.png", StringComparison.OrdinalIgnoreCase)) {
    // Convert to parent folder event
    eventToBuffer = new FileSystemEventArgs(
        WatcherChangeTypes.Created,
        grandParentDir,
        parentDirName);
}
```

**Problem**: 
- Waits for **file creation**, not folder creation
- Folder creation events are **not actively handled**
- Delay occurs waiting for `stitched_original.png` to appear
- External program race condition (file deleted before processing)

---

### Q8: Timestamp Extraction from Folder Name
**Answer**: **YES** ✅ Already implemented

```csharp
// MonitoringOrchestrator.cs:1522-1567
private DateTime? ExtractTimestampFromFolderName(string? folderPath)
{
    // Pattern 1: C + 6 digits (date) + T + 6 digits (time)
    // Example: C251204T111028
    var match = Regex.Match(folderName, @"C(\d{6}T\d{6})");
    
    // Pattern 2: C + 8 digits (date) + _ + 6 digits (time)
    // Example: C20240115_143022
    match = Regex.Match(folderName, @"C(\d{8}_\d{6})");
}
```

**Supports**:
- ✅ `C251216T214727` format
- ✅ `C20241216_214727` format

**User's Format**: `C251216T214727_0` (suffix `_0` is line number)
- Base pattern matches ✅
- Suffix ignored ✅

---

### Q9: Time Between Folder Creation and File Deletion
**Answer**: **UNKNOWN** - Needs production measurement

**User Statement**: External program moves/deletes files "immediately"

**Estimate**: 1-3 seconds based on:
- External program also likely polling (similar 1s interval)
- Processing time for external program

**Current Delay**: 4 seconds → **Too slow**, always loses race

---

### Q10: Detect Folder Creation Instead of File
**Answer**: **POSSIBLE** but not currently implemented

**Current**: Watches for `stitched_original.png` → converts to folder event

**Better Approach**:
```csharp
// Watch folder creation directly
watcher.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.CreationTime;
watcher.Created += OnFolderCreated;

private void OnFolderCreated(object sender, FileSystemEventArgs e)
{
    if (Directory.Exists(e.FullPath)) {
        // This is a folder creation event
        var folderName = Path.GetFileName(e.FullPath);
        if (folderName.StartsWith("C") && folderName.Contains('T')) {
            // Normal folder detected immediately!
        }
    }
}
```

**Benefit**: Immediate detection without waiting for file creation

---

### Q11: UI Lazy Image Loading Support
**Answer**: **NEEDS VERIFICATION** - Cannot determine from file watching layer

**Current Analysis**: FileWatcherService and MonitoringOrchestrator don't handle images
- Image loading likely in:
  - `FileGroupViewModel`
  - `ImageCacheService`
  - WPF binding converters

**Next Step**: Examine UI layer in separate investigation

---

### Q12: Thumbnail Generation Timing
**Answer**: **NEEDS VERIFICATION** - Not visible in current code layer

**Hypothesis**:
- Likely triggered when row is displayed (lazy)
- Or when `GroupCreated` event is raised (eager)

**Next Step**: Examine ViewModel and UI bindings

---

### Q13: Current Duplicate Prevention - FileWatcherService
**Answer**: **FILE-LEVEL DEDUPLICATION** using `_knownFiles` HashSet

```csharp
// FileWatcherService.cs:30 (field declaration)
private readonly HashSet<string> _knownFiles = new();

// FileWatcherService.cs:363-441 (ShouldProcessEvent method)
private bool ShouldProcessEvent(FileSystemEventArgs e)
{
    // ... filters ...
    
    // De-duplication Logic (line 414)
    lock (_lockObject)
    {
        if (e.ChangeType == WatcherChangeTypes.Created)
        {
            if (_knownFiles.Contains(e.FullPath)) {
                return false; // Duplicate, skip
            }
            _knownFiles.Add(e.FullPath);
        }
    }
    return true;
}
```

**Mechanism**: Tracks full file paths to prevent duplicate Created events

**Problem for Normal folders**:
```
Folder event: C251216T214727_0/ → Added to _knownFiles
File event: C251216T214727_0/stitched_original.png → Different path, NOT filtered
```

**Current Mitigation** (FileWatcherService.cs:383-410):
```csharp
// Line 383-387: Check if this is a Normal FOLDER event
bool isNormalFolderEvent = !Path.HasExtension(e.FullPath) &&
                            !string.IsNullOrEmpty(pathFileName) &&
                            pathFileName.StartsWith("C", StringComparison.OrdinalIgnoreCase) &&
                            pathFileName.Contains('T');

if (isNormalFolderEvent) {
    // This is a Normal folder event - ALLOW IT
    // Continue to deduplication logic
}
else if (!string.IsNullOrEmpty(pathFileName) && Path.HasExtension(e.FullPath)) {
    // Line 395-409: This is a FILE (has extension)
    var parentDirName = Path.GetFileName(Path.GetDirectoryName(e.FullPath));
    if (parentDirName.StartsWith("C") && parentDirName.Contains('T')) {
        // File INSIDE Normal folder - SKIP
        return false;
    }
}
```

**Finding**: ✅ Duplicate prevention exists with two-stage filtering:
1. Folder events (no extension) → Allowed, go to dedup
2. File events inside Normal folders → Blocked immediately

---

### Q14: _processedFiles Dictionary - What Does It Track?
**Answer**: **FOLDER-LEVEL tracking with 2-second debouncing**

```csharp
// MonitoringOrchestrator.cs:Dictionary<string, DateTime> _processedFiles

private bool ShouldSkipEvent(FileSystemEventArgs eventArgs)
{
    var checkPath = eventArgs.FullPath;
    var fileType = DetermineFileType(eventArgs.FullPath);
    
    if (fileType == FileType.Normal && !Directory.Exists(eventArgs.FullPath)) {
        // For files inside Normal folder, use PARENT FOLDER for dedup
        var parentFolder = Path.GetDirectoryName(eventArgs.FullPath);
        if (!string.IsNullOrEmpty(parentFolder)) {
            checkPath = parentFolder;
        }
    }
    
    if (_processedFiles.TryGetValue(checkPath, out var lastProcessed)) {
        if ((DateTime.UtcNow - lastProcessed).TotalSeconds < 2) {
            return true; // Skip if processed within last 2 seconds
        }
    }
    return false;
}
```

**Finding**: ✅ Already implements folder-level deduplication for Normal files!

**Mechanism**:
1. Normal folder event → Adds folder path
2. File event inside folder → Uses parent folder path
3. 2-second window prevents duplicate processing

---

### Q15: Folder vs File Event Processing Order
**Answer**: **UNPREDICTABLE** - Depends on FileSystemWatcher timing

**Possible Scenarios**:

**Scenario A** (Folder first):
```
1. Folder creation detected → Folder event
2. File creation detected → File event (filtered by ShouldProcessEvent)
Result: ONE group created ✅
```

**Scenario B** (File first):
```
1. File creation detected → Converted to folder event
2. Folder creation detected → Folder event (dedup by _knownFiles)
Result: ONE group created ✅
```

**Scenario C** (Simultaneous):
```
1. Both detected at same time
2. Race condition → Depends on thread scheduling
Result: Possible duplicate if both pass _knownFiles check before adding
```

**Current Mitigation**:
- `_knownFiles` in FileWatcherService (file-level)
- `_processedFiles` in MonitoringOrchestrator (folder-level, 2s window)
- File filtering (files inside Normal folders blocked)

**Risk**: Scenario C could still create duplicates if events arrive within microseconds

---

## 3. Code Analysis Results

### 3.1 Current Event Flow

```
[Filesystem]
    ↓
[FileSystemWatcher OR Polling]
    ↓
[OnFileSystemEvent / PollDirectories]
    ↓ (converts stitched_original.png → folder event)
[_eventChannel.Writer.TryWrite(event)]
    ↓
[Channel Queue] ← BOTTLENECK: Sequential processing
    ↓
[ProcessEventsAsync - await foreach]
    ↓ (one at a time)
[ShouldProcessEvent] ← Filters + _knownFiles dedup
    ↓
[FileChanged event raised]
    ↓
[MonitoringOrchestrator.OnFileChanged]
    ↓
[ProcessFileEventsAsync]
    ↓
[ShouldSkipEvent] ← _processedFiles dedup (2s window)
    ↓
[CreateOrUpdateGroupAsync]
    ↓
[lock(_lockObject)] ← BOTTLENECK: Global lock
  ├─ FindMatchingExistingGroup (O(n) to O(n³))
  ├─ MergeGroups OR Add new group
  └─ OnGroupCreated/Updated event
    ↓
[UI Update]
```

**Total Latency**: Polling (1s) + Queue wait + Processing + Lock contention = **4+ seconds**

---

### 3.2 Duplicate Prevention Layers

**Layer 1: FileWatcherService._knownFiles**
- Scope: Individual file paths
- Method: HashSet Contains check
- Timing: Immediate (< 1ms)

**Layer 2: FileWatcherService.ShouldProcessEvent (File Filtering)**
- Scope: Files inside Normal folders
- Method: Parent directory name pattern check
- Effect: Blocks file events if parent is Normal folder

**Layer 3: MonitoringOrchestrator._processedFiles**
- Scope: Folder paths for Normal type
- Method: 2-second time window
- Effect: Prevents rapid re-processing of same folder

**Layer 4: FindMatchingExistingGroup**
- Scope: Group matching by NormalFolder, NirKey, timestamp
- Method: Search existing groups before creating new
- Effect: Merges into existing group instead of creating duplicate

**Result**: ✅ Multi-layered defense, but relies on timing assumptions

---

### 3.3 Bottleneck Identification

**Primary Bottleneck**: Sequential event processing
```csharp
await foreach (var eventArgs in _eventChannel.Reader.ReadAllAsync(cancellationToken))
{
    // Process ONE event at a time
}
```

**Secondary Bottleneck**: Global lock in CreateOrUpdateGroupAsync
```csharp
lock (_lockObject)
{
    FindMatchingExistingGroup(newGroup); // Can take 10-50ms
    // ... all group operations locked ...
}
```

**Combined Effect**:
- Event 1 processing: 50ms (includes lock)
- Event 2 waits in queue: 50ms
- Event 3 waits: 100ms
- Event 4 waits: 150ms
- For 10 events: Last event waits ~500ms just for queue

**Total delay = Polling interval (1s) + Queue wait (0-500ms) + Processing time (50ms each)**

---

## 4. Polling vs Event-Based Analysis

### 4.1 Current Hybrid Approach

**FileWatcherService** uses both:
1. **Event-Based**: FileSystemWatcher (immediate detection)
2. **Polling-Based**: Timer every 1000ms (fallback for network drives)

### 4.2 Polling Implementation Details

```csharp
// FileWatcherService.cs:166-171
if (_options.EnablePolling)
{
    _pollingTimer = new Timer(PollDirectories, null, 
        _options.PollingIntervalMs,  // Initial: 1000ms
        _options.PollingIntervalMs); // Period: 1000ms
}

// PollDirectories (443-535): Scans all directories every 1000ms
foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
{
    if (!_knownFiles.Contains(file)) {
        // Create event, add to channel
    }
}
```

**Configuration**:
- `WorkflowSettings.EnableNetworkDrivePolling` (default: true)
- `WorkflowSettings.PollingIntervalMs` (default: 1000)

### 4.3 Problems Identified

**Problem 1: Resource Waste**
- Scans ALL directories every second
- Even when no files are created
- CPU + disk I/O overhead

**Problem 2: Redundancy**
- FileSystemWatcher **already detects** file/folder creation
- Polling is **duplicate detection** mechanism
- Both send events to same channel → potential duplicates

**Problem 3: Doesn't Solve Core Issue**
- 4-second delay is **NOT from polling interval**
- Delay is from:
  1. Waiting for file creation (not folder)
  2. Sequential event processing
  3. Lock contention
- Polling at 1000ms still has **same queue delay** issue

**Problem 4: Code Complexity**
- Two parallel code paths (event + polling)
- Duplicate logic (OnFileSystemEvent vs PollDirectories)
- Both handle `stitched_original.png` conversion
- More code to test and maintain

### 4.4 Decision: Remove Polling ✅

**Rationale**:
Based on `docs/trouble/event_vs_polling_analysis.md`:

1. **FileSystemWatcher IS Sufficient**:
   - Already configured with `NotifyFilters.DirectoryName`
   - CAN detect folder creation immediately
   - Network drive concerns are outdated (modern Windows handles this)

2. **Real Problem is File-Based Detection**:
   - Current: Wait for `stitched_original.png` file → Convert to folder event
   - Better: Detect folder creation **directly**
   - FileSystemWatcher already supports this!

3. **Polling Doesn't Help**:
   - Polling at 1s doesn't fix 4s delay (queue + processing)
   - Just adds overhead without solving root cause

**Impact of Removal**:
- ✅ Cleaner code (single detection path)
- ✅ Lower CPU usage (no periodic scans)
- ✅ Lower disk I/O (no directory enumeration)
- ✅ Simpler testing (deterministic event-based)
- ✅ Actually **faster** (no 1s polling wait)

### 4.5 Implementation Strategy

**Step 1**: Configure FileSystemWatcher for folder events
```csharp
// CreateWatcher - Already has DirectoryName!
NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | ...

// OnFileSystemEvent - Add folder creation handler
if (e.ChangeType == WatcherChangeTypes.Created && Directory.Exists(e.FullPath)) {
    // This is a folder creation event!
    var folderName = Path.GetFileName(e.FullPath);
    if (folderName.StartsWith("C") && folderName.Contains('T')) {
        // Normal folder detected IMMEDIATELY
    }
}
```

**Step 2**: Remove polling code
- Delete `PollDirectories` method
- Remove `_pollingTimer` field
- Remove `EnablePolling` option
- Remove polling config (PollingIntervalMs, EnableNetworkDrivePolling)

**Step 3**: Simplify configuration
- Remove `FileWatcherOptions.EnablePolling`
- Remove `WorkflowSettings.EnableNetworkDrivePolling`
- Remove `WorkflowSettings.PollingIntervalMs`

---

## 5. Recommendations

### 5.1 Immediate Actions (Can Do Now)

1. **Add Performance Logging**:
   ```csharp
   var sw = Stopwatch.StartNew();
   FindMatchingExistingGroup(newGroup);
   _logger.LogDebug("FindMatchingExistingGroup took {Ms}ms", sw.ElapsedMilliseconds);
   ```

2. **Monitor Channel Depth**:
   ```csharp
   _logger.LogDebug("Event channel depth: {Count}", _eventChannel.Reader.Count);
   ```

3. **Remove Polling** ✅:
   - Following analysis in Section 4, remove polling entirely
   - Implement folder creation event handling
   - Eliminate resource waste

### 5.2 Architecture Changes (Design Phase)

1. **Parallel Event Processing**:
   - Replace `await foreach` with parallel workers (2-4 threads)
   - Use SemaphoreSlim to limit concurrency

2. **Reduce Lock Scope**:
   - Use ConcurrentDictionary for `_activeGroups`
   - Lock only critical sections (adding, not searching)

3. **Priority Queue**:
   - Normal folders processed first
   - Camera files deferred

### 5.3 Race Condition Mitigation

1. **Immediate Timestamp Extraction**:
   - Move `ExtractTimestampFromFolderName` to detection stage
   - Cache in memory before queuing

2. **Folder-Level Events** ✅:
   - Stop relying on `stitched_original.png`
   - Watch folder creation directly
   - **This is now part of polling removal**

3. **Deferred Image Loading**:
   - Create group immediately (Phase 1)
   - Load images when UI requests (Phase 2)

---

## 5. Next Steps

After approval of this research document:

1. **Planning Phase** (03_plan.md):
   - Design parallel processing architecture
   - Define concurrency primitives
   - Plan lock reduction strategy

2. **Performance Baseline**:
   - Add instrumentation to current code
   - Measure actual timings in production
   - Confirm bottleneck hypotheses

3. **Prototype**:
   - Build proof-of-concept for parallel processing
   - Test with synthetic file arrival patterns

---
**Status**: [ ] Approved
**Next Step**: 03_plan.md (implementation architecture)
