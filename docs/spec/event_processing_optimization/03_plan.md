---
Task: Event Processing Optimization
Created: 2025-12-17
Status: Draft
Depends On: 01_requirements.md, 02_research.md
---

# Event Processing Optimization - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | How Addressed | Verification |
|-------------------|---------------|--------------|
| Average delay ≤ 2s | Parallel processing + folder events | Performance metrics logging |
| 95th percentile ≤ 3s | Priority queue for Normal folders | Percentile tracking |
| Normal folders detected within 500ms | Direct folder creation events | Event timestamp logging |
| UI row created immediately | Two-phase processing | UI response time measurement |
| Zero duplicate groups | Enhanced deduplication logic | Duplicate detection tests |
| **Polling removed entirely** | Delete PollDirectories + config | Code review, no polling timer |
| **Folder events handled directly** | OnFileSystemEvent folder handler | Folder creation event logs |
| Timestamp extraction (file deleted) | Folder name parsing on detection | Cache verification tests |
| Image loading deferred | Phase 2 lazy load | UI rendering inspection |
| CPU < 30% | Remove polling, limit parallelism | Resource monitoring |
| Memory increase ≤ 50MB | Efficient caching, concurrent collections | Memory profiler |
| Zero matching errors | Comprehensive testing | Integration test suite |

> **All criteria addressed in this plan.**

---

## 1. Architecture Overview

### 1.1 System Context Diagram

```
                    ┌──────────────────────────────────────┐
                    │   Filesystem (Normal Folders)       │
                    │   C251216T214727_0/                  │
                    └──────────────┬───────────────────────┘
                                   │ Folder Created Event
                                   ↓
┌─────────────────────────────────────────────────────────────────┐
│  FileSystemWatcher (Event-Based ONLY)                           │
│  - NotifyFilters.DirectoryName                                  │
│  - Created event for folders                                    │
│  - NO POLLING                                                   │
└──────────────┬──────────────────────────────────────────────────┘
               │ FileSystemEventArgs
               ↓
┌─────────────────────────────────────────────────────────────────┐
│  OnFileSystemEvent (Detection Stage)                            │
│  ✓ Detect: Directory.Exists(e.FullPath)                        │
│  ✓ Extract: ExtractTimestampFromFolderName()                   │
│  ✓ Cache: _folderTimestamps[path] = timestamp                  │
│  ✓ Priority: Determine file type                               │
└──────────────┬──────────────────────────────────────────────────┘
               │ EventWithPriority
               ↓
┌─────────────────────────────────────────────────────────────────┐
│  Priority Channel (UnboundedPriorityChannel)                    │
│  - Normal folders: Priority 0 (highest)                         │
│  - Camera files: Priority 2 (lower)                             │
│  - NIR files: Priority 1                                        │
└──────────────┬──────────────────────────────────────────────────┘
               │ Sequential read
               ↓
┌─────────────────────────────────────────────────────────────────┐
│  Parallel Workers (2-4 threads)                                 │
│  ├─ Worker 1: ProcessEventsAsync()                              │
│  ├─ Worker 2: ProcessEventsAsync()                              │
│  └─ Worker 3: ProcessEventsAsync()                              │
│  ✓ SemaphoreSlim(maxParallelism)                               │
└──────────────┬──────────────────────────────────────────────────┘
               │ FileGroup
               ↓
┌─────────────────────────────────────────────────────────────────┐
│  MonitoringOrchestrator                                         │
│  ✓ ConcurrentDictionary<string, FileGroup> _activeGroups       │
│  ✓ Phase 1: Create group + row (immediate)                     │
│  ✓ Phase 2: Load images (deferred)                             │
└──────────────┬──────────────────────────────────────────────────┘
               │ GroupCreated event
               ↓
┌─────────────────────────────────────────────────────────────────┐
│  UI (MainWindowViewModel)                                       │
│  - Row appears immediately                                      │
│  - Images load when scrolled into view                          │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Data Flow

```
1. Folder Created (C251216T214727_0/)
   ↓
2. FileSystemWatcher.Created event fires
   ↓
3. OnFileSystemEvent: 
   - Check if folder
   - Extract timestamp (21:47:27)
   - Cache timestamp
   - Determine priority (Normal = 0)
   - Write to priority channel
   ↓
4. Worker thread (one of 2-4):
   - Read from channel
   - Acquire semaphore slot
   - Call ShouldProcessEvent
   ↓
5. MonitoringOrchestrator.CreateOrUpdateGroupAsync:
   - **NO LOCK** on ConcurrentDictionary read
   - Create FileGroup with cached timestamp
   - ConcurrentDictionary.TryAdd (thread-safe)
   ↓
6. GroupCreated event → UI
   - Row created immediately
   - Images = null (will load later)
```

---

## 2. Components

### 2.1 Modified Components

| Component | File Path | Changes | Breaking? |
|-----------|-----------|---------|-----------|
| `FileWatcherService` | `Core/FileWatching/FileWatcherService.cs` | Remove polling, add folder event handler | No (internal) |
| `MonitoringOrchestrator` | `Core/FileWatching/MonitoringOrchestrator.cs` | Use ConcurrentDictionary, cache timestamps | No (internal) |
| `FileWatcherOptions` | `Core/FileWatching/IFileWatcher.cs` | Remove EnablePolling, PollingIntervalMs | **Yes** (config) |
| `WorkflowSettings` | `Models/ApplicationConfiguration.cs` | Remove polling config properties | **Yes** (config) |

### 2.2 New Components

| Component | Type | File Path | Purpose |
|-----------|------|-----------|---------|
| `PriorityEventChannel` | Class | `Core/FileWatching/PriorityEventChannel.cs` | Priority-based event queuing |
| `EventPriority` | Enum | `Core/FileWatching/EventPriority.cs` | Define priority levels (0-2) |
| `FolderTimestampCache` | Class | `Core/FileWatching/FolderTimestampCache.cs` | Thread-safe timestamp caching |

---

## 3. Interface Definitions

### 3.1 FileWatcherService (Modified)

```csharp
// REMOVE:
- void PollDirectories(object? state)
- Timer? _pollingTimer
- FileWatcherOptions.EnablePolling
- FileWatcherOptions.PollingIntervalMs

// ADD:
+ FolderTimestampCache _folderTimestamps
+ void HandleFolderCreatedEvent(FileSystemEventArgs e)
+ EventPriority DetermineEventPriority(FileSystemEventArgs e)
```

**Responsibilities**:
- Detect folder creation events from FileSystemWatcher
- Extract timestamps immediately on detection
- Cache timestamps before queuing
- Assign priority to events
- **NO LONGER**: Poll directories periodically

---

### 3.2 PriorityEventChannel

```csharp
public class PriorityEventChannel
{
    public async ValueTask WriteAsync(FileSystemEventArgs args, EventPriority priority);
    public async IAsyncEnumerable<FileSystemEventArgs> ReadAllAsync(CancellationToken ct);
}

public enum EventPriority
{
    High = 0,      // Normal folders (race condition critical)
    Medium = 1,    // NIR files
    Low = 2        // Camera files
}
```

**Responsibilities**:
- Queue events with priority
- Return highest priority events first
- Maintain FIFO order within same priority

---

### 3.3 FolderTimestampCache

```csharp
public class FolderTimestampCache
{
    public void Add(string folderPath, DateTime timestamp);
    public bool TryGet(string folderPath, out DateTime timestamp);
    public void Remove(string folderPath);
    public void Clear();
}
```

**Responsibilities**:
- Thread-safe timestamp storage
- Fast lookup during group creation
- Automatic cleanup of old entries (5 minute TTL)

---

### 3.4 MonitoringOrchestrator (Modified)

```csharp
// CHANGE:
- Dictionary<string, FileGroup> _activeGroups
+ ConcurrentDictionary<string, FileGroup> _activeGroups

// ADD:
+ FolderTimestampCache _folderTimestamps (injected)
+ Task ProcessEventsConcurrentlyAsync(int maxParallelism)
+ SemaphoreSlim _parallelismLimiter
```

**Responsibilities**:
- Process events in parallel (2-4 workers)
- Use cached timestamps for group creation
- Thread-safe group management
- **NO LONGER**: Single-threaded sequential processing

---

## 4. Key Design Decisions

### 4.1 Polling Removal

**Problem**: Polling wastes resources and doesn't solve core issue

**Options**:
- [ ] Option A: Keep polling as fallback for network drives
- [x] Option B: Remove polling entirely ← SELECTED
- [ ] Option C: Make polling opt-in (default OFF)

**Rationale**: 
- FileSystemWatcher with DirectoryName **already detects** folders
- Network drive concerns are outdated (modern Windows/SMB reliable)
- Polling doesn't fix 4-second delay (queue backlog issue)
- Simpler code, lower resource usage

**Impact**:
- **Breaking**: Remove `EnableNetworkDrivePolling`, `PollingIntervalMs` from config
- **Migration**: Users with polling enabled will auto-fall back to event-based
- **Risk**: If network drives fail to send events → manual refresh UI option

---

### 4.2 Folder Event Detection

**Problem**: Currently waits for `stitched_original.png` file creation

**Options**:
- [ ] Option A: Keep file-based detection
- [x] Option B: Detect folder creation directly ← SELECTED
- [ ] Option C: Hybrid (folder + file)

**Rationale**:
- FileSystemWatcher `NotifyFilters.DirectoryName` already configured
- `OnFileSystemEvent` fires for folder Created events
- Can check `Directory.Exists(e.FullPath)` to distinguish folder vs file
- Eliminates wait for file creation → faster detection

**Implementation**:
```csharp
private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
{
    if (e.ChangeType != WatcherChangeTypes.Created) return;
    
    // Check if this is a FOLDER creation
    if (Directory.Exists(e.FullPath))
    {
        var folderName = Path.GetFileName(e.FullPath);
        
        // Is this a Normal folder? (C + timestamp pattern)
        if (Regex.IsMatch(folderName, @"^C\d{6}T\d{6}"))
        {
            // Extract timestamp IMMEDIATELY
            var timestamp = ExtractTimestampFromFolderName(e.FullPath);
            if (timestamp.HasValue)
            {
                _folderTimestamps.Add(e.FullPath, timestamp.Value);
            }
            
            // Queue with HIGH priority
            _eventChannel.Write(e, EventPriority.High);
        }
    }
    else  // This is a file creation
    {
        // Handle file events (Camera, NIR)
        // ...
    }
}
```

---

### 4.3 Parallel Event Processing

**Problem**: Sequential processing creates queue backlog

**Options**:
- [ ] Option A: Fully parallel (no limit)
- [ ] Option B: Thread pool (8-16 threads)
- [x] Option C: Limited parallelism (2-4 workers) ← SELECTED

**Rationale**:
- Lock contention in `FindMatchingExistingGroup` limits scalability
- Too many threads → thrashing, no benefit
- 2-4 workers balances throughput vs overhead
- Configurable via `MaxEventProcessingWorkers`

**Implementation**:
```csharp
private async Task StartEventProcessingAsync(CancellationToken ct)
{
    var maxWorkers = _options.MaxEventProcessingWorkers; // Default: 3
    
    // Semaphore limits concurrent EVENT PROCESSING, not worker count
    var semaphore = new SemaphoreSlim(maxWorkers);
    
    // Start N workers (all run in parallel)
    var tasks = Enumerable.Range(0, maxWorkers)
        .Select(i => ProcessEventsWorkerAsync(i, semaphore, ct))
        .ToList();
    
    await Task.WhenAll(tasks);
}

private async Task ProcessEventsWorkerAsync(int workerId, SemaphoreSlim sem, CancellationToken ct)
{
    // Each worker reads from shared channel
    await foreach (var eventArgs in _internalEventChannel.ReadAllAsync(ct))
    {
        // ProcessSingleEventAsync acquires semaphore INSIDE
        // This limits how many events are being processed concurrently
        await ProcessSingleEventAsync(eventArgs, workerId, ct);
    }
}

private async Task ProcessSingleEventAsync(FileSystemEventArgs e, int workerId, CancellationToken ct)
{
    // Acquire semaphore to limit concurrent processing
    await sem.WaitAsync(ct); // Limit concurrency
    try
    {
        if (ShouldProcessEvent(e))
        {
            await CreateOrUpdateGroupAsync(e.FullPath, fileType);
        }
    }
    finally
    {
        sem.Release();
    }
}
```

**Clarification**:
- **3 workers** are always running (3 threads)
- **Semaphore(3)** limits that max 3 events can be processed AT THE SAME TIME
- If 100 events arrive, workers will process them in batches of 3
- This prevents resource exhaustion while maintaining parallelism

---

### 4.4 ConcurrentDictionary for _activeGroups

**Problem**: Global lock on entire matching operation

**Options**:
- [ ] Option A: Keep Dictionary + global lock
- [x] Option B: ConcurrentDictionary + minimal locking ← SELECTED
- [ ] Option C: Lock-free data structure

**Rationale**:
- `ConcurrentDictionary.TryAdd` is thread-safe atomic operation
- Eliminates need for lock around dictionary access
- Still need lock for `FindMatchingExistingGroup` (reads multiple keys)
- But lock duration is shorter (just matching, not add)

**Implementation**:
```csharp
// OLD:
lock (_lockObject)
{
    FileGroup? existing = FindMatchingExistingGroup(newGroup);
    if (existing != null) {
        MergeGroups(existing, newGroup);
    } else {
        _activeGroups[newGroup.GroupId] = newGroup;  // ← Lock held here
    }
}

// NEW:
FileGroup? existing = FindMatchingExistingGroup(newGroup); // ← NO lock

if (existing != null) {
    MergeGroups(existing, newGroup); // Merge is thread-safe
} else {
    if (_activeGroups.TryAdd(newGroup.GroupId, newGroup)) {  // ← Atomic add
        OnGroupCreated(newGroup);
    }
}
```

---

### 4.5 Two-Phase Processing

**Problem**: Image loading delays group creation

**Options**:
- [ ] Option A: Load all images before creating group
- [x] Option B: Create group immediately, load images later ← SELECTED
- [ ] Option C: Background thread loads images

**Rationale**:
- Race condition requires **fast** group creation (<500ms)
- Image loading can take 100-500ms (BMP decoding, thumbnails)
- UI can display row without images (show placeholders)
- ViewModel requests images when row is scrolled into view

**Implementation**:

**Phase 1 (MonitoringOrchestrator)**:
```csharp
var newGroup = new FileGroup
{
    GroupId = $"group_{_nextGroupId:D3}",
    Timestamp = cachedTimestamp,  // From cache
    NormalFolder = folderPath,
    MainImagePath = null,         // ← Don't load yet
    // ... other fields
};

_activeGroups.TryAdd(newGroup.GroupId, newGroup);
OnGroupCreated(newGroup);  // → UI updates immediately
```

**Phase 2 (FileGroupViewModel)**:
```csharp
// Lazy property
public BitmapImage? MainImage
{
    get
    {
        if (_mainImage == null && !string.IsNullOrEmpty(_model.MainImagePath))
        {
            // Load on first access (when row is visible)
            _mainImage = LoadImageAsync(_model.MainImagePath);
        }
        return _mainImage;
    }
}
```

---

## 5. Configuration

### Changes to ApplicationConfiguration

**REMOVE** (Breaking Changes):

| Key | Type | Reason |
|-----|------|--------|
| `WorkflowSettings.EnableNetworkDrivePolling` | bool | Polling removed |
| `WorkflowSettings.PollingIntervalMs` | int | Polling removed |

**ADD** (New Options):

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `WorkflowSettings.MaxEventProcessingWorkers` | int | 3 | Parallel worker count (1-8) |
| `WorkflowSettings.EnableParallelProcessing` | bool | true | Feature flag for parallel mode |
| `WorkflowSettings.FolderTimestampCacheTTL` | int | 300 | Cache TTL in seconds |

**Migration Path**:
```csharp
// ConfigurationManager.cs - Handle missing keys
if (!config.WorkflowSettings.ContainsKey("MaxEventProcessingWorkers"))
{
    config.WorkflowSettings.MaxEventProcessingWorkers = 3; // Default
}

// Warn if old polling config detected
if (config.WorkflowSettings.ContainsKey("EnableNetworkDrivePolling"))
{
    _logger.LogWarning("EnableNetworkDrivePolling is deprecated and will be ignored");
}
```

---

## 6. Glossary Updates

### New Terms

| Term | Type | Description |
|------|------|-------------|
| `PriorityEventChannel` | Class | Priority-based event channel for file events |
| `EventPriority` | Enum | Priority levels: High (0), Medium (1), Low (2) |
| `FolderTimestampCache` | Class | Thread-safe cache for folder timestamps |
| `MaxEventProcessingWorkers` | Config | Number of parallel event processing workers |

### Deprecated Terms

| Deprecated | Use Instead | Reason | Date |
|------------|-------------|--------|------|
| `EnablePolling` | - | Polling removed | 2025-12-17 |
| `PollingIntervalMs` | - | Polling removed | 2025-12-17 |
| `PollDirectories` | - | Polling removed | 2025-12-17 |
| `EnableNetworkDrivePolling` | - | Polling removed | 2025-12-17 |

---

## 7. Architecture Documentation Plan

### New Docs to Create

| Document | Purpose |
|----------|---------|
| `module_priority_event_channel.md` | Priority channel architecture |
| `module_folder_timestamp_cache.md` | Timestamp caching design |
| `impact_polling_removal.md` | Impact analysis of polling removal |

### Docs to Update

| Document | Changes |
|----------|---------|
| `module_file_watcher_service.md` | Remove polling section, add folder event handling |
| `module_monitoring_orchestrator.md` | Add parallel processing, ConcurrentDictionary |
| `glossary.md` | Add new terms, mark deprecated |
| `impact_configuration_changes.md` | Document breaking config changes |

---

## 8. Testing Strategy

### Unit Tests

**FileWatcherService**:
- [ ] Folder creation event triggers folder handler (not file handler)
- [ ] Timestamp extracted and cached immediately
- [ ] Priority assigned correctly (Normal=High, Camera=Low)
- [ ] PollDirectories method removed (compilation check)

**FolderTimestampCache**:
- [ ] Thread-safe concurrent adds
- [ ] TTL cleanup works (old entries removed)
- [ ] TryGet returns correct cached value

**PriorityEventChannel**:
- [ ] High priority events processed before low
- [ ] FIFO order within same priority
- [ ] Async enumeration completes on channel close

**MonitoringOrchestrator**:
- [ ] ConcurrentDictionary thread-safe operations
- [ ] Parallel workers process events concurrently
- [ ] Semaphore limits max concurrency
- [ ] Cached timestamps used for group creation

### Integration Tests

- [ ] End-to-end: Folder created → Group created <500ms
- [ ] No duplicates with parallel processing
- [ ] Race condition: External program deletes file, group still created
- [ ] Performance: 10 files arrive → all processed within 2 seconds

### Regression Tests

- [ ] All existing unit tests pass
- [ ] No duplicate groups for same Normal folder
- [ ] Timestamp extraction still works (all patterns)
- [ ] Camera/NIR files still matched correctly

---

## 9. Migration and Rollback

### Migration Steps

**For Existing Deployments**:
1. Backup current configuration (`appsettings.json`)
2. Deploy new version
3. Configuration auto-migrates (polling config ignored)
4. Monitor logs for folder detection events
5. Verify no performance degradation

**Configuration Migration**:
```json
// OLD (will be ignored):
{
  "WorkflowSettings": {
    "EnableNetworkDrivePolling": true,
    "PollingIntervalMs": 1000
  }
}

// NEW (auto-added defaults):
{
  "WorkflowSettings": {
    "MaxEventProcessingWorkers": 3,
    "EnableParallelProcessing": true,
    "FolderTimestampCacheTTL": 300
  }
}
```

### Rollback Plan

**If folder events don't fire** (unlikely):
1. Add feature flag: `UseLegacyPollingMode` (default: false)
2. Keep old code in separate branch
3. Restart with legacy config
4. Investigate FileSystemWatcher issues

**Rollback Trigger**:
- No folder events detected for >10 seconds
- Folders created but not processed
- User reports missing data

---

## 10. Performance Targets

### Baseline (Current)

- Detection-to-matching delay: **4-5 seconds**
- CPU usage: 10-15% (with polling overhead)
- Memory: ~150MB baseline
- Events processed: **1 per second** (sequential)

### Target (After Optimization)

- Detection-to-matching delay: **<2 seconds** (target: <1s for Normal folders)
- CPU usage: 5-10% (no polling)
- Memory: ~180MB (+ 30MB for concurrency)
- Events processed: **3-4 per second** (parallel, 3 workers)

### Monitoring

```csharp
// Add performance metrics
_logger.LogInformation("Folder detected in {Ms}ms", sw.ElapsedMilliseconds);
_logger.LogInformation("Group created in {Ms}ms from detection", totalMs);
_logger.LogInformation("Active workers: {Count}/{Max}", activeWorkers, maxWorkers);
```

---

## 11. Open Questions

- [x] All questions resolved

---

**Status**: [ ] Approved  
**Next Step**: 04_design.md (detailed implementation)
