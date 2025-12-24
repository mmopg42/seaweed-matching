# Real-time Event Queue Failure Analysis

**Date**: 2025-12-22  
**Issue**: All real-time file detection events fail to queue with "Failed to queue event to internal channel" warning  
**Impact**: Real-time monitoring completely non-functional after initial scan  
**Severity**: CRITICAL

---

## Observed Behavior

### Symptoms
When Start button is pressed after initial scan:
- `FileWatcherService` detects Normal folders and files correctly
- All `Info` logs show successful detection
- **All `TryWrite()` calls fail** with `warn` messages
- No events reach workers → No UI updates
- cam1 and NIR graphs don't appear

### Log Evidence
```
info: FileWatcherService: Normal folder detected: C251201T140546_0
warn: MonitoringOrchestrator: Failed to queue event to internal channel: Z:\...\C251201T140546_0

warn: Failed to queue event to internal channel: Z:\...\20251201_140548_897.bmp
warn: Failed to queue event to internal channel: Z:\...\run_120251201T140558.spc
```

**Pattern**: 100% failure rate for all file types (Normal, Cam1-6, NIR)

---

## Root Cause Analysis

### Core Problem: Channel Lifecycle Bug

**Location**: [MonitoringOrchestrator.cs](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/Core/FileWatching/MonitoringOrchestrator.cs)

#### Initialization (Line 45)
```csharp
private Channel<FileSystemEventArgs> _internalEventChannel = 
    Channel.CreateUnbounded<FileSystemEventArgs>();
```
✅ Channel created once at class initialization

#### StopAsync (Line 290)
```csharp
_internalEventChannel.Writer.Complete();  // ❌ CLOSES WRITER PERMANENTLY
```
Problem: Channel writer is **completed** (closed), but channel instance is **not recreated**

#### StartAsync (Lines 210-256)
```csharp
_parallelismLimiter = new SemaphoreSlim(_maxParallelWorkers);
_workerCts = new CancellationTokenSource();
// Workers started...
// ❌ NO CHANNEL RECREATION!
```

**Result**: Second `StartAsync()` call uses a **closed channel**

---

## Execution Flow

### First Start (Works)
```
1. App Launch → Constructor runs
   └─ _internalEventChannel = CreateUnbounded() ✅ Fresh channel

2. Initial Scan completes
   └─ Groups created successfully

3. Workers start
   └─ Reading from _internalEventChannel.Reader ✅ Working

4. Real-time events (hypothetically works if never stopped)
```

### Stop → Restart (Fails)
```
1. User clicks Stop
   └─ StopAsync() → Writer.Complete() ❌ Channel closed

2. User clicks Start
   └─ StartAsync() runs
   └─ Workers start with CLOSED channel reader
   └─ OnFileChanged tries to write to COMPLETED writer
   └─ TryWrite() → FALSE (writer is completed)
   └─ "Failed to queue event" warning logged
```

---

## Why cam1 and NIR Don't Appear

### Immediate Cause
Events never reach `ProcessSingleEventAsync()`:
```csharp
// OnFileChanged (Line 340)
if (!_internalEventChannel.Writer.TryWrite(e))  // ← Always FALSE
{
    // Event discarded!
}
```

### Downstream Effects
1. **No cam1 images**: Events discarded → No `CreateOrUpdateGroupAsync` → No group update
2. **No NIR graphs**: Same reason → NIR files never processed
3. **UI frozen**: `GroupUpdated` event never fires

---

## Solution

### Option 1: Recreate Channel in StartAsync (Recommended)

```csharp
// MonitoringOrchestrator.cs:StartAsync (after line 208)
public async Task StartAsync(ApplicationConfiguration config)
{
    // ... existing code ...
    
    // ✅ RECREATE CHANNEL if completed
    if (_internalEventChannel.Reader.Completion.IsCompleted)
    {
        _internalEventChannel = Channel.CreateUnbounded<FileSystemEventArgs>();
        _logger.LogInformation("Recreated internal event channel for new monitoring session");
    }
    
    // Initialize parallel processing infrastructure...
    _maxParallelWorkers = config.WorkflowSettings?.MaxEventProcessingWorkers ?? 3;
    // ...
}
```

**Pros**:
- Minimal code change
- Clear intent (fresh channel per session)
- Safe (checks completion status)

**Cons**:
- Slight code duplication

### Option 2: Don't Complete Channel in StopAsync

```csharp
// MonitoringOrchestrator.cs:StopAsync (remove line 290)
public async Task StopAsync()
{
    // ...
    _workerCts.Cancel();  // Signal workers to stop
    
    // ❌ REMOVE THIS LINE:
    // _internalEventChannel.Writer.Complete();
    
    await Task.WhenAll(_workerTasks);
    // ...
}
```

**Pros**:
- Even simpler

**Cons**:
- Channel accumulates events during stopped period (minor)
- Workers need proper cancellation handling

### Option 3: Dispose and Recreate Pattern

```csharp
private Channel<FileSystemEventArgs>? _internalEventChannel;

public async Task StartAsync(...)
{
    _internalEventChannel = Channel.CreateUnbounded<FileSystemEventArgs>();
    // ...
}

public async Task StopAsync()
{
    _internalEventChannel.Writer.Complete();
    await Task.WhenAll(_workerTasks);
    _internalEventChannel = null;  // Clear reference
}
```

**Pros**:
- Explicit lifecycle management

**Cons**:
- Nullable reference handling

---

## Recommendation

**Implement Option 1**: Recreate channel in `StartAsync`

**Rationale**:
1. Safest approach (checks before recreating)
2. Explicit about fresh start
3. No behavioral changes to `StopAsync`
4. Minimal risk

---

## Implementation

### Change Location
**File**: `MonitoringOrchestrator.cs`  
**Method**: `StartAsync`  
**Line**: After line 208 (before parallelism initialization)

### Code Change
```csharp
// After logging total paths
_logger.LogError("DEBUG: Total paths collected for watching: {Count}", watchPaths.Count);

// ✅ ADD THIS BLOCK:
// Recreate channel if it was completed in previous StopAsync
if (_internalEventChannel.Reader.Completion.IsCompleted)
{
    _internalEventChannel = Channel.CreateUnbounded<FileSystemEventArgs>();
    _logger.LogInformation("Recreated event channel for new monitoring session");
}

// Initialize parallel processing infrastructure
_maxParallelWorkers = config.WorkflowSettings?.MaxEventProcessingWorkers ?? 3;
```

---

## Testing

### Verification Steps
1. Start app → Initial scan completes
2. Click "Stop" button
3. Click "Start" button
4. Add a new file/folder to monitored directory
5. **Expected**: No "Failed to queue" warnings
6. **Expected**: New group appears in UI
7. **Expected**: cam1 and NIR images load

### Success Criteria
- ✅ Log shows "Recreated event channel for new monitoring session"
- ✅ No "Failed to queue event to internal channel" warnings
- ✅ Real-time events processed successfully
- ✅ UI updates with new groups
- ✅ cam1 and NIR thumbnails appear

---

## Related Issues
- Initial scan cache pre-population (resolved)
- Worker parallelism configuration (working)
- File watcher event detection (working - not the issue)

---

## Prevention

### Future Safeguards
1. Add unit test for Stop→Start cycle
2. Log channel status on `StartAsync`:
   ```csharp
   _logger.LogDebug("Channel status: Completed={IsCompleted}", 
       _internalEventChannel.Reader.Completion.IsCompleted);
   ```
3. Consider using `IDisposable` pattern for cleaner lifecycle
