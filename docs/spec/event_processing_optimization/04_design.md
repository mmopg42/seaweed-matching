---
Task: Event Processing Optimization
Created: 2025-12-17
Status: Draft
Depends On: 03_plan.md
---

# Event Processing Optimization - Detailed Design

## 1. Component Designs

### 1.1 FileWatcherService (Modified)

#### Interface (from Plan)

**Removed Methods**:
```csharp
void PollDirectories(object? state)  // DELETE
```

**Added Methods**:
```csharp
void HandleFolderCreatedEvent(FileSystemEventArgs e)
EventPriority DetermineEventPriority(FileSystemEventArgs e)
```

**Modified Fields**:
```csharp
// REMOVE:
Timer? _pollingTimer

// ADD:
FolderTimestampCache _folderTimestamps
PriorityEventChannel _eventChannel  // Replaces Channel<FileSystemEventArgs>
```

---

#### Preconditions

- `StartWatchingAsync` must be called before any events are processed
- `_folderTimestamps` must be initialized (injected via constructor)
- FileSystemWatcher must have `NotifyFilters.DirectoryName` enabled

#### Postconditions

- All folder creation events are detected within 50ms (FileSystemWatcher latency)
- Timestamps are cached immediately upon detection
- Events are queued with correct priority
- No polling timer is running

---

#### Detailed Logic (Pseudo-code)

**OnFileSystemEvent - Enhanced for Folder Detection**:

```csharp
private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
{
    try
    {
        // Step 1: Log event
        _logger.LogDebug("FileSystemEvent detected: {ChangeType} - {Path}", 
            e.ChangeType, e.FullPath);
        
        // Step 2: Only process Created events
        if (e.ChangeType != WatcherChangeTypes.Created)
        {
            return; // Ignore Changed, Deleted, Renamed for now
        }
        
        // Step 3: Determine if this is a folder or file
        bool isFolder = Directory.Exists(e.FullPath);
        
        if (isFolder)
        {
            // Step 4a: Handle folder creation
            HandleFolderCreatedEvent(e);
        }
        else
        {
            // Step 4b: Handle file creation (existing logic)
            HandleFileCreatedEvent(e);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error handling file system event: {Path}", e.FullPath);
        // Don't throw - event processing continues
    }
}
```

**HandleFolderCreatedEvent - NEW METHOD**:

```csharp
private void HandleFolderCreatedEvent(FileSystemEventArgs e)
{
    // Step 1: Extract folder name
    string folderPath = e.FullPath;
    string folderName = Path.GetFileName(folderPath);
    
    if (string.IsNullOrEmpty(folderName))
    {
        _logger.LogWarning("Empty folder name for path: {Path}", folderPath);
        return;
    }
    
    // Step 2: Check if this matches Normal folder pattern
    // Pattern: C + 6 digits (YYMMDD) + T + 6 digits (HHMMSS) [+ optional _N]
    // Examples: C251216T214727, C251216T214727_0
    Match match = Regex.Match(folderName, @"^C(\d{6}T\d{6})");
    
    if (!match.Success)
    {
        // Not a Normal folder, ignore
        _logger.LogDebug("Folder does not match Normal pattern: {Name}", folderName);
        return;
    }
    
    // Step 3: Extract timestamp IMMEDIATELY (race condition critical)
    DateTime? timestamp = ExtractTimestampFromFolderName(folderPath);
    
    if (!timestamp.HasValue)
    {
        _logger.LogWarning("Failed to extract timestamp from folder: {Name}", folderName);
        // Continue anyway - MonitoringOrchestrator will handle missing timestamp
    }
    else
    {
        // Step 4: Cache timestamp for later retrieval
        _folderTimestamps.Add(folderPath, timestamp.Value);
        
        _logger.LogInformation("Normal folder detected: {Name}, Timestamp: {Ts}", 
            folderName, timestamp.Value.ToString("yyyy-MM-dd HH:mm:ss"));
    }
    
    // Step 5: Determine priority
    EventPriority priority = DetermineEventPriority(e);
    
    // Step 6: Write to priority channel
    if (_eventChannel.Writer.TryWrite(e, priority))
    {
        Interlocked.Increment(ref _eventCount);
        _lastEventTime = DateTime.UtcNow;
        
        _logger.LogDebug("Folder event queued with priority {Priority}", priority);
    }
    else
    {
        _logger.LogWarning("Failed to queue folder event: {Path}", folderPath);
    }
}
```

**HandleFileCreatedEvent - Refactored from existing logic**:

```csharp
private void HandleFileCreatedEvent(FileSystemEventArgs e)
{
    // Step 1: Extract file name
    string fileName = Path.GetFileName(e.FullPath);
    
    // Step 2: Check if this is stitched_original.png (LEGACY support)
    // This should NOT happen if folder events are working correctly
    if (fileName.Equals("stitched_original.png", StringComparison.OrdinalIgnoreCase))
    {
        _logger.LogWarning("Detected stitched_original.png file event - " +
            "folder event should have fired first: {Path}", e.FullPath);
        
        // Convert to folder event (legacy fallback)
        string? parentDir = Path.GetDirectoryName(e.FullPath);
        if (!string.IsNullOrEmpty(parentDir))
        {
            string parentDirName = Path.GetFileName(parentDir);
            
            if (!string.IsNullOrEmpty(parentDirName) &&
                parentDirName.StartsWith("C", StringComparison.OrdinalIgnoreCase) &&
                parentDirName.Contains('T'))
            {
                string? grandParentDir = Path.GetDirectoryName(parentDir) ?? parentDir;
                FileSystemEventArgs folderEvent = new FileSystemEventArgs(
                    WatcherChangeTypes.Created,
                    grandParentDir,
                    parentDirName);
                
                HandleFolderCreatedEvent(folderEvent);
                return; // Don't process as file
            }
        }
    }
    
    // Step 3: Determine priority
    EventPriority priority = DetermineEventPriority(e);
    
    // Step 4: Write to channel
    if (_eventChannel.Writer.TryWrite(e, priority))
    {
        Interlocked.Increment(ref _eventCount);
        _lastEventTime = DateTime.UtcNow;
    }
    else
    {
        _logger.LogWarning("Failed to queue file event: {Path}", e.FullPath);
    }
}
```

**DetermineEventPriority - NEW METHOD**:

```csharp
private EventPriority DetermineEventPriority(FileSystemEventArgs e)
{
    // Step 1: Check if folder (highest priority - race condition critical)
    if (Directory.Exists(e.FullPath))
    {
        string folderName = Path.GetFileName(e.FullPath);
        if (Regex.IsMatch(folderName, @"^C\d{6}T\d{6}"))
        {
            return EventPriority.High; // Normal folders
        }
    }
    
    // Step 2: Check file type
    string fileName = Path.GetFileName(e.FullPath);
    string extension = Path.GetExtension(fileName).ToLowerInvariant();
    
    // NIR files (.csv)
    if (extension == ".csv")
    {
        return EventPriority.Medium;
    }
    
    // Camera files (.jpg, .png, .bmp)
    if (extension == ".jpg" || extension == ".jpeg" || 
        extension == ".png" || extension == ".bmp")
    {
        return EventPriority.Low;
    }
    
    // Default: Medium priority
    return EventPriority.Medium;
}
```

---

#### State Management

**Internal State Variables**:

```csharp
// REMOVED:
private Timer? _pollingTimer;  // DELETE

// EXISTING:
private readonly Channel<FileSystemEventArgs> _eventChannel;  // REPLACE with PriorityEventChannel
private readonly HashSet<string> _knownFiles;
private int _eventCount;
private DateTime _lastEventTime;

// NEW:
private readonly FolderTimestampCache _folderTimestamps;
private readonly PriorityEventChannel _eventChannel;
```

**State Transitions**:

```
INIT → WATCHING → STOPPED

INIT:
  - _folderTimestamps = new()
  - _eventChannel = new PriorityEventChannel()
  - _pollingTimer = null  (REMOVED)

WATCHING:
  - FileSystemWatcher.EnableRaisingEvents = true
  - NO polling timer
  - Events flowing through _eventChannel

STOPPED:
  - FileSystemWatcher.EnableRaisingEvents = false
  - _folderTimestamps.Clear()
  - _eventChannel.Complete()
```

---

#### Thread Safety / Concurrency

**Locking Strategy**:

```csharp
// _knownFiles access
lock (_lockObject)
{
    if (!_knownFiles.Contains(path))
    {
        _knownFiles.Add(path);
        // Process event
    }
}

// _folderTimestamps access (handled internally by FolderTimestampCache)
_folderTimestamps.Add(path, timestamp);  // Thread-safe

// _eventChannel writes (lock-free, channel handles concurrency)
_eventChannel.Writer.TryWrite(e, priority);  // Thread-safe
```

**Atomic Operations**:

```csharp
// Event count (atomic increment)
Interlocked.Increment(ref _eventCount);

// Last event time (lock-free, timestamp update)
_lastEventTime = DateTime.UtcNow;  // Race can occur, but not critical
```

---

#### Error Handling

| Error Scenario | Detection | Handling | Recovery |
|----------------|-----------|----------|----------|
| Invalid folder path | `Path.GetFileName` returns null/empty | Log warning, skip event | Continue processing next event |
| Timestamp extraction fails | `ExtractTimestampFromFolderName` returns null | Log warning, cache null, continue | MonitoringOrchestrator uses fallback |
| Channel full (shouldn't happen - unbounded) | `TryWrite` returns false | Log error, drop event | Health status degraded |
| FileSystemWatcher error | `OnWatcherError` event | Log error, set health status | Continue watching |
| Regex match failure | `Regex.Match` returns no match | Log debug, skip folder | Not an error (non-Normal folder) |

---

### 1.2 PriorityEventChannel (NEW)

#### Interface

```csharp
public class PriorityEventChannel
{
    public ChannelWriter<(FileSystemEventArgs, EventPriority)> Writer { get; }
    
    public async IAsyncEnumerable<FileSystemEventArgs> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken);
}

public enum EventPriority
{
    High = 0,      // Normal folders (race condition critical)
    Medium = 1,    // NIR files
    Low = 2        // Camera files
}
```

---

#### Preconditions

- Must be created before FileWatcherService starts
- All writers must complete before reader enumeration completes

#### Postconditions

- Events are returned in priority order (High → Medium → Low)
- Within same priority, FIFO order is maintained
- Reader completes when all writers complete and all events are read

---

#### Detailed Logic (Pseudo-code)

**Constructor**:

```csharp
public PriorityEventChannel()
{
    // Step 1: Create 3 separate channels (one per priority)
    _highPriorityChannel = Channel.CreateUnbounded<FileSystemEventArgs>(
        new UnboundedChannelOptions
        {
            SingleReader = false,  // Multiple workers can read
            SingleWriter = false
        });
    
    _mediumPriorityChannel = Channel.CreateUnbounded<FileSystemEventArgs>(...);
    _lowPriorityChannel = Channel.CreateUnbounded<FileSystemEventArgs>(...);
    
    // Step 2: Create aggregated writer
    _writer = new PriorityChannelWriter(this);
}
```

**TryWrite**:

```csharp
public bool TryWrite(FileSystemEventArgs args, EventPriority priority)
{
    // Step 1: Select channel based on priority
    Channel<FileSystemEventArgs> targetChannel = priority switch
    {
        EventPriority.High => _highPriorityChannel,
        EventPriority.Medium => _mediumPriorityChannel,
        EventPriority.Low => _lowPriorityChannel,
        _ => _mediumPriorityChannel // Default
    };
    
    // Step 2: Write to selected channel
    bool success = targetChannel.Writer.TryWrite(args);
    
    if (success)
    {
        _logger.LogDebug("Event written to {Priority} priority channel", priority);
    }
    
    return success;
}
```

**ReadAllAsync**:

```csharp
public async IAsyncEnumerable<FileSystemEventArgs> ReadAllAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    // Step 1: Read from channels in priority order
    //         Use select pattern to poll all channels
    
    while (!cancellationToken.IsCancellationRequested)
    {
        FileSystemEventArgs? nextEvent = null;
        
        // Step 2: Try HIGH priority first
        if (_highPriorityChannel.Reader.TryRead(out var highEvent))
        {
            nextEvent = highEvent;
        }
        // Step 3: Then MEDIUM priority
        else if (_mediumPriorityChannel.Reader.TryRead(out var medEvent))
        {
            nextEvent = medEvent;
        }
        // Step 4: Finally LOW priority
        else if (_lowPriorityChannel.Reader.TryRead(out var lowEvent))
        {
            nextEvent = lowEvent;
        }
        // Step 5: No events available, wait for any
        else
        {
            // Use WaitToReadAsync on all channels with Task.WhenAny
            Task<bool> highTask = _highPriorityChannel.Reader.WaitToReadAsync(cancellationToken).AsTask();
            Task<bool> medTask = _mediumPriorityChannel.Reader.WaitToReadAsync(cancellationToken).AsTask();
            Task<bool> lowTask = _lowPriorityChannel.Reader.WaitToReadAsync(cancellationToken).AsTask();
            
            Task completedTask = await Task.WhenAny(highTask, medTask, lowTask);
            
            if (completedTask == highTask && await highTask)
            {
                if (_highPriorityChannel.Reader.TryRead(out highEvent))
                    nextEvent = highEvent;
            }
            else if (completedTask == medTask && await medTask)
            {
                if (_mediumPriorityChannel.Reader.TryRead(out medEvent))
                    nextEvent = medEvent;
            }
            else if (completedTask == lowTask && await lowTask)
            {
                if (_lowPriorityChannel.Reader.TryRead(out lowEvent))
                    nextEvent = lowEvent;
            }
            else
            {
                // All channels completed
                break;
            }
        }
        
        // Step 6: Yield event if found
        if (nextEvent != null)
        {
            yield return nextEvent;
        }
    }
}
```

---

#### State Management

```csharp
// Internal state
private readonly Channel<FileSystemEventArgs> _highPriorityChannel;
private readonly Channel<FileSystemEventArgs> _mediumPriorityChannel;
private readonly Channel<FileSystemEventArgs> _lowPriorityChannel;
private readonly ILogger _logger;

// State: OPEN → CLOSING → CLOSED

// OPEN: Writers can write, readers can read
// CLOSING: Writers.Complete() called, readers draining
// CLOSED: All events read, enumeration complete
```

---

#### Thread Safety

**Lock-Free Design**:
- Each channel is thread-safe by design
- Multiple writers can write to same channel concurrently
- Single reader per channel (but 3 channels)
- No explicit locking needed

**Race Conditions**:
- If multiple events arrive at same time with same priority → FIFO order maintained by channel
- If High and Low events arrive simultaneously → High always processed first

---

#### Error Handling

| Error Scenario | Detection | Handling | Recovery |
|----------------|-----------|----------|----------|
| Channel write fails | `TryWrite` returns false | Return false to caller | Caller logs and continues |
| Cancellation during read | `OperationCanceledException` | Stop enumeration, yield break | Graceful shutdown |
| All channels empty | `WaitToReadAsync` returns false | Break loop, complete enumeration | Normal completion |

---

### 1.3 FolderTimestampCache (NEW)

#### Interface

```csharp
public class FolderTimestampCache
{
    public void Add(string folderPath, DateTime timestamp);
    public bool TryGet(string folderPath, out DateTime timestamp);
    public void Remove(string folderPath);
    public void Clear();
    public int Count { get; }
}
```

---

#### Preconditions

- `folderPath` must be an absolute path
- `timestamp` must be valid DateTime (not default)

#### Postconditions

- All operations are thread-safe
- Entries older than TTL are automatically removed
- Cache does not grow unbounded (TTL cleanup)

---

#### Detailed Logic (Pseudo-code)

**Add**:

```csharp
public void Add(string folderPath, DateTime timestamp)
{
    // Step 1: Validate inputs
    if (string.IsNullOrEmpty(folderPath))
    {
        throw new ArgumentNullException(nameof(folderPath));
    }
    
    if (timestamp == default)
    {
        throw new ArgumentException("Invalid timestamp", nameof(timestamp));
    }
    
    // Step 2: Create cache entry
    CacheEntry entry = new CacheEntry
    {
        Timestamp = timestamp,
        CachedAt = DateTime.UtcNow
    };
    
    // Step 3: Add to concurrent dictionary (thread-safe)
    _cache.AddOrUpdate(folderPath, entry, (key, existing) => entry);
    
    _logger.LogDebug("Cached timestamp for {Path}: {Timestamp}", 
        folderPath, timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
    
    // Step 4: Trigger cleanup if cache is getting large
    if (_cache.Count > 1000)
    {
        _ = Task.Run(() => CleanupExpiredEntries());
    }
}
```

**TryGet**:

```csharp
public bool TryGet(string folderPath, out DateTime timestamp)
{
    // Step 1: Validate input
    if (string.IsNullOrEmpty(folderPath))
    {
        timestamp = default;
        return false;
    }
    
    // Step 2: Try get from cache
    if (_cache.TryGetValue(folderPath, out CacheEntry? entry))
    {
        // Step 3: Check if expired
        TimeSpan age = DateTime.UtcNow - entry.CachedAt;
        
        if (age.TotalSeconds > _ttlSeconds)
        {
            // Expired - remove and return false
            _cache.TryRemove(folderPath, out _);
            timestamp = default;
            
            _logger.LogDebug("Cache entry expired for {Path}", folderPath);
            return false;
        }
        
        // Step 4: Valid entry found
        timestamp = entry.Timestamp;
        return true;
    }
    
    // Step 5: Not found
    timestamp = default;
    return false;
}
```

**CleanupExpiredEntries**:

```csharp
private void CleanupExpiredEntries()
{
    // Step 1: Calculate cutoff time
    DateTime cutoff = DateTime.UtcNow.AddSeconds(-_ttlSeconds);
    
    int removed = 0;
    
    // Step 2: Iterate all entries
    foreach (var kvp in _cache)
    {
        if (kvp.Value.CachedAt < cutoff)
        {
            // Step 3: Remove expired entry
            if (_cache.TryRemove(kvp.Key, out _))
            {
                removed++;
            }
        }
    }
    
    if (removed > 0)
    {
        _logger.LogInformation("Cleaned up {Count} expired cache entries", removed);
    }
}
```

---

#### State Management

```csharp
// Internal state
private readonly ConcurrentDictionary<string, CacheEntry> _cache;
private readonly int _ttlSeconds;  // Default: 300 (5 minutes)
private readonly ILogger _logger;

private class CacheEntry
{
    public DateTime Timestamp { get; set; }
    public DateTime CachedAt { get; set; }
}

// State: ACTIVE (always active, no state transitions)
```

---

#### Thread Safety

**ConcurrentDictionary Operations**:
```csharp
// AddOrUpdate - Atomic operation
_cache.AddOrUpdate(key, newValue, (k, existing) => newValue);

// TryGetValue - Lock-free read
_cache.TryGetValue(key, out value);

// TryRemove - Atomic removal
_cache.TryRemove(key, out removedValue);
```

**No Explicit Locking Needed** - ConcurrentDictionary handles all synchronization

---

#### Error Handling

| Error Scenario | Detection | Handling | Recovery |
|----------------|-----------|----------|----------|
| Null folderPath | `string.IsNullOrEmpty` check | Throw ArgumentNullException | Caller catches |
| Invalid timestamp | `timestamp == default` check | Throw ArgumentException | Caller catches |
| Cache full (>10000 entries) | Count check | Trigger aggressive cleanup | Remove >50% of entries |
| Cleanup fails | Exception in CleanupExpiredEntries | Log error, continue | Next cleanup attempt |

---

### 1.4 MonitoringOrchestrator (Modified)

#### Interface (from Plan)

**Changed Fields**:
```csharp
// CHANGE:
- private readonly Dictionary<string, FileGroup> _activeGroups;
+ private readonly ConcurrentDictionary<string, FileGroup> _activeGroups;

// ADD:
+ private readonly FolderTimestampCache _folderTimestamps;  // Injected
+ private readonly SemaphoreSlim _parallelismLimiter;  // Limits concurrent processing
+ private readonly int _maxParallelWorkers;  // From config, default: 3
+ private readonly Channel<FileSystemEventArgs> _internalEventChannel;  // Buffers FileChanged events
+ private List<Task> _workerTasks;  // Worker task references
```

**New Methods**:
```csharp
+ private async Task ProcessEventsWorkerAsync(int workerId, CancellationToken ct)
+ private void OnFileChanged(object? sender, FileSystemEventArgs e)  // Event handler
```

---

#### Preconditions

- `_folderTimestamps` must be initialized and shared with FileWatcherService
- `_maxParallelWorkers` must be between 1 and 8
- `_activeGroups` must be ConcurrentDictionary

#### Postconditions

- Events are processed in parallel (up to maxParallelWorkers concurrently)
- Group creation uses cached timestamps (no file system access needed)
- No duplicate groups created (thread-safe)

---

#### Detailed Logic (Pseudo-code)

**StartAsync - Modified**:

```csharp
public async Task StartAsync(ApplicationConfiguration config)
{
    // ... existing validation ...
    
    // Step NEW: Initialize parallelism limiter
    _maxParallelWorkers = config.WorkflowSettings.MaxEventProcessingWorkers;
    _parallelismLimiter = new SemaphoreSlim(_maxParallelWorkers);
    
    _logger.LogInformation("Starting with {Workers} parallel event processing workers", 
        _maxParallelWorkers);
    
    // ... existing FileWatcher setup ...
    
    // Step MODIFIED: Start parallel processing instead of single ProcessEventsAsync
    _fileWatcher.FileChanged += OnFileChanged;
    await _fileWatcher.StartWatchingAsync(watchPaths, watcherOptions);
    
    // Start worker tasks
    _isMonitoring = true;
    _cancellationTokenSource = new CancellationTokenSource();
    
    // Launch parallel workers
    _workerTasks = Enumerable.Range(0, _maxParallelWorkers)
        .Select(i => ProcessEventsWorkerAsync(i, _cancellationTokenSource.Token))
        .ToList();
    
    _logger.LogInformation("Started {Count} event processing workers", _workerTasks.Count);
}
```

**ProcessEventsWorkerAsync - NEW**:

```csharp
private async Task ProcessEventsWorkerAsync(int workerId, CancellationToken ct)
{
    _logger.LogInformation("Worker {Id} started", workerId);
    
    try
    {
        // Step 1: Read events from internal event queue
        // FileWatcherService.FileChanged event writes to _internalEventChannel
        // Each worker reads from this shared priority channel
        
        await foreach (var eventArgs in _internalEventChannel.ReadAllAsync(ct))
        {
            // Step 2: Process event with semaphore limiting concurrency
            await ProcessSingleEventAsync(eventArgs, workerId, ct);
        }
    }
    catch (OperationCanceledException)
    {
        _logger.LogInformation("Worker {Id} cancelled", workerId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Worker {Id} failed", workerId);
    }
    finally
    {
        _logger.LogInformation("Worker {Id} stopped", workerId);
    }
}

// Event handler bridges FileWatcherService → InternalEventChannel
private void OnFileChanged(object? sender, FileSystemEventArgs e)
{
    // Write to internal channel for workers to process
    if (!_internalEventChannel.Writer.TryWrite(e))
    {
        _logger.LogWarning("Failed to queue event for processing: {Path}", e.FullPath);
    }
}
```

**ProcessSingleEventAsync - Refactored**:

```csharp
private async Task ProcessSingleEventAsync(
    FileSystemEventArgs eventArgs, 
    int workerId,
    CancellationToken ct)
{
    // Step 1: Acquire semaphore slot (limit concurrency)
    await _parallelismLimiter.WaitAsync(ct);
    
    try
    {
        _logger.LogDebug("Worker {WorkerId} processing: {Path}", workerId, eventArgs.FullPath);
        
        // Step 2: Check if should skip (debouncing)
        if (ShouldSkipEvent(eventArgs))
        {
            _logger.LogDebug("Event skipped (debounced): {Path}", eventArgs.FullPath);
            return;
        }
        
        // Step 3: Determine file type
        FileType fileType = DetermineFileType(eventArgs.FullPath);
        
        if (fileType == FileType.Unknown)
        {
            _logger.LogDebug("Unknown file type: {Path}", eventArgs.FullPath);
            return;
        }
        
        // Step 4: Create or update group (thread-safe)
        FileGroup? group = await CreateOrUpdateGroupAsync(eventArgs.FullPath, fileType);
        
        if (group != null)
        {
            MarkFileAsProcessed(eventArgs.FullPath);
            _logger.LogInformation("Worker {WorkerId} processed group {GroupId}", 
                workerId, group.GroupId);
        }
    }
    finally
    {
        // Step 5: Release semaphore slot
        _parallelismLimiter.Release();
    }
}
```

**CreateOrUpdateGroupAsync - Modified for Thread Safety**:

```csharp
private async Task<FileGroup?> CreateOrUpdateGroupAsync(string filePath, FileType fileType)
{
    try
    {
        // Step 1: Normalize path (folder for Normal files)
        string processPath = filePath;
        if (fileType == FileType.Normal && !string.IsNullOrEmpty(Path.GetExtension(filePath)))
        {
            var parentFolder = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(parentFolder))
            {
                processPath = parentFolder;
            }
        }
        
        // Step 2: Create FileGroup from single file
        var newGroup = CreateGroupFromSingleFile(processPath, fileType);
        if (newGroup == null)
        {
            return null;
        }
        
        // Step 3: TRY to get cached timestamp (Phase 1 optimization)
        if (fileType == FileType.Normal)
        {
            if (_folderTimestamps.TryGet(processPath, out DateTime cachedTimestamp))
            {
                newGroup.Timestamp = cachedTimestamp;
                _logger.LogDebug("Used cached timestamp for {Path}: {Ts}", 
                    processPath, cachedTimestamp);
            }
            else
            {
                _logger.LogWarning("No cached timestamp for {Path}, extracting from disk", 
                    processPath);
                // Fallback: Extract from folder name (slower)
                var extractedTs = ExtractTimestampFromFolderName(processPath);
                if (extractedTs.HasValue)
                {
                    newGroup.Timestamp = extractedTs.Value;
                }
            }
        }
        
        // Step 4: Find matching existing group (uses snapshot for consistency)
        FileGroup? existingGroup = FindMatchingExistingGroup(newGroup);
        
        if (existingGroup != null)
        {
            // Step 5a: Merge into existing group (LOCK for thread-safety)
            _logger.LogInformation("Merging into existing group {GroupId}", existingGroup.GroupId);
            
            // CRITICAL: Lock the group during merge to prevent concurrent modifications
            lock (existingGroup)
            {
                MergeGroups(existingGroup, newGroup);
            }
            
            OnGroupUpdated(existingGroup);
            return existingGroup;
        }
        else
        {
            // Step 5b: Try add new group (atomic operation)
            var newGroupId = $"group_{Interlocked.Increment(ref _nextGroupId):D3}";
            newGroup.GroupId = newGroupId;
            
            // ConcurrentDictionary.TryAdd is atomic
            if (_activeGroups.TryAdd(newGroup.GroupId, newGroup))
            {
                _logger.LogInformation("Created new group {GroupId}", newGroup.GroupId);
                OnGroupCreated(newGroup);
                return newGroup;
            }
            else
            {
                // Extremely rare: GroupId collision
                _logger.LogWarning("Group ID collision: {GroupId}", newGroupId);
                // Retry with incremented ID
                newGroupId = $"group_{Interlocked.Increment(ref _nextGroupId):D3}";
                newGroup.GroupId = newGroupId;
                _activeGroups.TryAdd(newGroup.GroupId, newGroup);
                OnGroupCreated(newGroup);
                return newGroup;
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating/updating group for {Path}", filePath);
        return null;
    }
}
```

**FindMatchingExistingGroup - Modified (Snapshot for Consistency)**:

```csharp
private FileGroup? FindMatchingExistingGroup(FileGroup newGroup)
{
    // ⚠️ IMPORTANT: ConcurrentDictionary.Values enumeration is thread-safe,
    // but if other threads modify during iteration, we may see inconsistent state.
    // Use ToArray() to create a consistent snapshot.
    
    // Step 0: Create snapshot for consistency
    var snapshot = _activeGroups.ToArray();
    
    // Step 1: Match by NormalFolder
    if (!string.IsNullOrEmpty(newGroup.NormalFolder))
    {
        foreach (var kvp in snapshot)
        {
            var existingGroup = kvp.Value;
            if (existingGroup.NormalFolder == newGroup.NormalFolder)
            {
                _logger.LogDebug("Match by NormalFolder: {Folder}", newGroup.NormalFolder);
                return existingGroup;
            }
        }
    }
    
    // Step 2: Match by NirKey
    if (!string.IsNullOrEmpty(newGroup.NirKey))
    {
        foreach (var kvp in snapshot)
        {
            var existingGroup = kvp.Value;
            if (existingGroup.NirKey == newGroup.NirKey)
            {
                _logger.LogDebug("Match by NirKey: {Key}", newGroup.NirKey);
                return existingGroup;
            }
        }
    }
    
    // Step 3: Match by timestamp (within tolerance)
    // ... existing logic using snapshot ...
    
    return null;  // No match found
}
```

---

#### Thread Safety / Concurrency

**Concurrency Model**:

```
Parallel Workers (2-4 threads reading from channel)
    ↓
Each worker processes events sequentially
    ↓
SemaphoreSlim (limit max CONCURRENT event processing)
    ↓
FindMatchingExistingGroup (snapshot for consistency)
    ↓
lock(existingGroup) for MergeGroups
    OR
ConcurrentDictionary.TryAdd (atomic add for new groups)
    ↓
OnGroupCreated/Updated event (thread-safe event raising)
```

**Lock Strategy**:
```csharp
// 1. FindMatchingExistingGroup - Snapshot (no lock, but consistent)
var snapshot = _activeGroups.ToArray();

// 2. MergeGroups - Lock the specific group being modified
lock (existingGroup)
{
    MergeGroups(existingGroup, newGroup);
}

// 3. New group creation - Atomic operation (lock-free)
_activeGroups.TryAdd(key, value);

// 4. Group ID - Interlocked increment (lock-free)
Interlocked.Increment(ref _nextGroupId);

// 5. Semaphore - Controls max concurrent event processing
await _parallelismLimiter.WaitAsync();  // In ProcessSingleEventAsync
_parallelismLimiter.Release();
```

**Remaining Locks**:
```csharp
// _processedFiles dictionary (keep existing lock)
lock (_lockObject)
{
    _processedFiles[path] = DateTime.UtcNow;
}
```

**Why Semaphore in ProcessSingleEventAsync**:
- Workers are already running (N threads)
- Semaphore limits how many events can be processed CONCURRENTLY
- This prevents resource exhaustion if 100 events arrive at once
- Example: 3 workers, semaphore(3) → max 3 events processing at same time

---

#### Error Handling

| Error Scenario | Detection | Handling | Recovery |
|----------------|-----------|----------|----------|
| Worker task fails | Exception in ProcessEventsWorkerAsync | Log error, continue other workers | Other workers continue |
| Semaphore deadlock | Timeout (shouldn't happen) | Log critical error | Restart monitoring |
| ConcurrentDictionary corruption | Exception on TryAdd | Log error, skip event | Data loss for single event |
| Cached timestamp missing | TryGet returns false | Extract from folder name (fallback) | Slower but works |
| Group ID collision | TryAdd returns false | Increment ID and retry | Rare, but handled |

---

## 2. Integration Points

### 2.1 FileWatcherService ↔ PriorityEventChannel

**Sequence**:
```
1. FileWatcherService.OnFileSystemEvent fires
2. HandleFolderCreatedEvent called
3. ExtractTimestampFromFolderName(path)
4. _folderTimestamps.Add(path, timestamp)
5. DetermineEventPriority(e) → EventPriority.High
6. _eventChannel.Writer.TryWrite(e, EventPriority.High)
7. Event queued in _highPriorityChannel
```

**Error Propagation**:
- If `TryWrite` fails → Log warning, drop event
- If timestamp extraction fails → Cache null, log warning, continue

---

### 2.2 FileWatcherService ↔ MonitoringOrchestrator ↔ Workers

**Architecture**:
```
FileWatcherService
    ↓ FileChanged event
MonitoringOrchestrator.OnFileChanged()
    ↓ Write to _internalEventChannel (unbounded)
_internalEventChannel
    ↓ await foreach (multiple workers)
ProcessEventsWorkerAsync (Worker 1, 2, 3)
    ↓ Semaphore limits concurrency
ProcessSingleEventAsync
```

**Sequence**:
```
1. FileWatcherService raises FileChanged event
2. MonitoringOrchestrator.OnFileChanged writes to _internalEventChannel
3. _internalEventChannel is an UnboundedChannel<FileSystemEventArgs>
4. N workers read from channel via ReadAllAsync
5. Each worker acquires semaphore before processing
6. Semaphore limits MAX CONCURRENT PROCESSING to N
```

**Key Difference from Plan**:
- FileWatcherService does NOT expose its internal PriorityEventChannel
- MonitoringOrchestrator maintains its own internal event channel
- FileChanged event bridges the two components
- Semaphore limits concurrent processing, NOT worker count

**Error Propagation**:
- If worker fails → Other workers continue
- If all workers fail → Health status unhealthy, restart required
- If channel write fails → Log warning, event dropped

---

### 2.3 FolderTimestampCache ↔ MonitoringOrchestrator

**Sequence**:
```
1. FileWatcherService writes to cache: _folderTimestamps.Add(path, ts)
2. MonitoringOrchestrator reads from cache: _folderTimestamps.TryGet(path, out ts)
3. If cache hit → Use cached timestamp (fast)
4. If cache miss → Extract from folder name (slow, fallback)
```

**Shared Instance**:
```csharp
// Dependency Injection
var timestampCache = new FolderTimestampCache(config.FolderTimestampCacheTTL);

var fileWatcher = new FileWatcherService(logger, timestampCache);
var orchestrator = new MonitoringOrchestrator(logger, fileWatcher, timestampCache);
```

---

## 3. Edge Cases & Boundary Conditions

| Case | Input | Expected Behavior | Handling |
|------|-------|-------------------|----------|
| Empty folder name | `Path.GetFileName()` → "" | Skip event, log warning | Return early |
| Folder deleted immediately | `Directory.Exists()` → false on 2nd call | Use cached timestamp | Cache contains timestamp |
| Timestamp extraction fails | Regex no match | Continue with null timestamp | MonitoringOrchestrator uses fallback |
| 100 events arrive simultaneously | High channel load | All queued, processed in priority order | Workers drain queue in parallel |
| Worker crashes | Exception in worker | Other workers continue | Logged, health degraded |
| Cache expires during processing | TryGet returns false (TTL exceeded) | Extract from folder name | Fallback logic |
| Group ID overflow | _nextGroupId > 999 | Continue with 1000, 1001, ... | No wraparound needed |

---

## 4. Performance Considerations

### CPU Usage

**Before Optimization**:
```
Single thread processing: 10-15% CPU
Polling overhead: +5% CPU
Total: ~15-20% CPU
```

**After Optimization**:
```
3 parallel workers: 3 × (5-8%) = 15-24% CPU
No polling: -5% CPU
Total: ~10-19% CPU (LOWER than before)
```

### Memory Usage

**New Allocations**:
```
FolderTimestampCache: ~2KB per entry × 100 entries = 200KB
PriorityEventChannel: 3 channels × 64KB buffer = 192KB
Worker tasks: 3 tasks × ~5KB stack = 15KB
ConcurrentDictionary overhead: ~50KB

Total: ~457KB (~0.5MB) additional memory
```

**Well within 50MB target** ✅

### Latency

**Critical Path (Normal folder)**:
```
1. Folder created → FileSystemWatcher fires: ~50ms (OS latency)
2. HandleFolderCreatedEvent: ~1ms (regex + cache add)
3. Queue write (high priority): <1ms
4. Worker picks up event: <10ms (semaphore wait)
5. CreateOrUpdateGroupAsync: ~50ms (group matching)
6. OnGroupCreated event: ~5ms

Total: ~117ms → Target <500ms ✅
```

---

## 5. Testing Strategy

### Unit Test Cases

**FileWatcherService.HandleFolderCreatedEvent**:
```csharp
[Test]
public void HandleFolderCreatedEvent_ValidNormalFolder_ExtractsAndCachesTimestamp()
{
    // Arrange
    var mockCache = new Mock<FolderTimestampCache>();
    var service = new FileWatcherService(logger, mockCache.Object);
    var folderEvent = new FileSystemEventArgs(
        WatcherChangeTypes.Created, 
        "C:\\data\\normal", 
        "C251216T214727_0");
    
    // Act
    service.HandleFolderCreatedEvent(folderEvent);
    
    // Assert
    mockCache.Verify(c => c.Add(
        It.Is<string>(p => p.EndsWith("C251216T214727_0")),
        It.Is<DateTime>(dt => dt.Year == 2025 && dt.Month == 12 && dt.Day == 16)
    ), Times.Once);
}

[Test]
public void HandleFolderCreatedEvent_InvalidPattern_SkipsProcessing()
{
    // Arrange
    var service = new FileWatcherService(logger, cache);
    var folderEvent = new FileSystemEventArgs(
        WatcherChangeTypes.Created,
        "C:\\data",
        "SomeOtherFolder");
    
    // Act
    service.HandleFolderCreatedEvent(folderEvent);
    
    // Assert
    // Should not write to channel, should not cache
    Assert.AreEqual(0, mockChannel.WrittenEvents.Count);
}
```

**PriorityEventChannel.ReadAllAsync**:
```csharp
[Test]
public async Task ReadAllAsync_ReturnsHighPriorityFirst()
{
    // Arrange
    var channel = new PriorityEventChannel();
    var lowEvent = new FileSystemEventArgs(...);
    var highEvent = new FileSystemEventArgs(...);
    
    // Write LOW first, HIGH second
    channel.Writer.TryWrite(lowEvent, EventPriority.Low);
    channel.Writer.TryWrite(highEvent, EventPriority.High);
    channel.Writer.Complete();
    
    // Act
    var events = new List<FileSystemEventArgs>();
    await foreach (var e in channel.ReadAllAsync(CancellationToken.None))
    {
        events.Add(e);
    }
    
    // Assert
    Assert.AreEqual(2, events.Count);
    Assert.AreEqual(highEvent, events[0]);  // HIGH first
    Assert.AreEqual(lowEvent, events[1]);   // LOW second
}
```

**FolderTimestampCache TTL**:
```csharp
[Test]
public async Task TryGet_ExpiredEntry_ReturnsFalse()
{
    // Arrange
    var cache = new FolderTimestampCache(ttlSeconds: 1);  // 1 second TTL
    cache.Add("C:\\folder", DateTime.Now);
    
    // Act
    await Task.Delay(1500);  // Wait for expiration
    bool found = cache.TryGet("C:\\folder", out DateTime ts);
    
    // Assert
    Assert.IsFalse(found);
    Assert.AreEqual(default(DateTime), ts);
}
```

**MonitoringOrchestrator Thread Safety**:
```csharp
[Test]
public async Task CreateOrUpdateGroupAsync_ConcurrentCalls_NoRaceCondition()
{
    // Arrange
    var orchestrator = new MonitoringOrchestrator(...);
    
    // Act - 10 threads trying to create same group
    var tasks = Enumerable.Range(0, 10)
        .Select(_ => Task.Run(() => 
            orchestrator.CreateOrUpdateGroupAsync("C:\\same\\folder", FileType.Normal)))
        .ToArray();
    
    await Task.WhenAll(tasks);
    
    // Assert - Only ONE group created, no duplicates
    Assert.AreEqual(1, orchestrator.GetActiveGroups().Count);
}
```

---

## 6. Rollback Plan

### If Performance Degrades

**Rollback Trigger**:
- CPU usage >40% sustained
- Memory usage >250MB
- Detection delay >5 seconds

**Rollback Steps**:
1. Disable parallel processing: `EnableParallelProcessing = false`
2. Fall back to single worker
3. Keep folder event detection (don't revert to file-based)
4. Monitor for improvement

### If Folder Events Don't Fire

**Rollback Trigger**:
- No folder events detected for >60 seconds
- Folders created but not processed

**Rollback Steps**:
1. Add `UseLegacyFileBasedDetection` config flag
2. Revert `OnFileSystemEvent` to wait for `stitched_original.png`
3. Keep priority channel (works with file events too)
4. Investigate FileSystemWatcher configuration

---

**Status**: [ ] Approved  
**Next Step**: 05_tasks.md (implementation task breakdown)
