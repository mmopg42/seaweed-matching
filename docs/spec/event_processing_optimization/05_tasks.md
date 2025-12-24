---
Task: Event Processing Optimization
Created: 2025-12-17
Status: Ready for Implementation
Depends On: 04_design.md
---

# Event Processing Optimization - Implementation Tasks

## Task Breakdown

This document provides a detailed, actionable checklist for implementing the event processing optimization. Tasks are organized by priority and component.

---

## Priority 1: Foundation Components (Critical Path) ✅

### 1.1 Create EventPriority Enum ✅

**File**: `ChronoView/Core/FileWatching/EventPriority.cs`

**Tasks**:
- [x] Create file with enum definition
- [x] Add XML documentation comments
- [x] Build and verify no compilation errors

---

### 1.2 Create FolderTimestampCache ✅

**File**: `ChronoView/Core/FileWatching/FolderTimestampCache.cs`

**Implementation Checklist**:
- [x] Create class with ConcurrentDictionary field
- [x] Implement `Add(string path, DateTime timestamp)`
- [x] Implement `TryGet(string path, out DateTime timestamp)`
- [x] Implement `Remove(string path)`
- [x] Implement `Clear()`
- [x] Implement `Count` property
- [x] Add private `CleanupExpiredEntries()` method
- [x] Add TTL logic (default 300 seconds from config)
- [x] Add logging for cache operations
- [ ] Create unit test file `FolderTimestampCacheTests.cs` (optional)
- [ ] Test: Add and TryGet basic functionality
- [ ] Test: TTL expiration removes entries
- [ ] Test: Thread safety (multiple threads concurrent add/get)
- [ ] Test: Cleanup reduces cache size

---

### 1.3 Create PriorityEventChannel ✅

**File**: `ChronoView/Core/FileWatching/PriorityEventChannel.cs`

**Implementation Checklist**:
- [x] Create class with 3 Channel<FileSystemEventArgs> fields (High, Medium, Low)
- [x] Implement `Writer` property (custom writer class)
- [x] Implement `TryWrite(FileSystemEventArgs args, EventPriority priority)`
- [x] Implement `ReadAllAsync(CancellationToken ct)` with priority ordering
- [x] Use `WaitToReadAsync` + `Task.WhenAny` for efficient waiting
- [x] Add logging for priority routing
- [ ] Create unit test file `PriorityEventChannelTests.cs` (optional)
- [ ] Test: High priority events returned before Low
- [ ] Test: FIFO order within same priority
- [ ] Test: Multiple readers (workers) can read concurrently
- [ ] Test: Cancellation token cancels enumeration

---

## Priority 2: Remove Polling (Breaking Change) ✅

### 2.1 Delete Polling Code from FileWatcherService ✅

**File**: `ChronoView/Core/FileWatching/FileWatcherService.cs`

**Tasks**:
- [x] Delete `PollDirectories` method (not found - already removed or never existed)
- [x] Delete `_pollingTimer` field (not found)
- [x] Remove polling timer initialization from constructor (not found)
- [x] Remove polling timer start logic from `StartWatchingAsync` (not found)
- [x] Remove polling timer stop logic from `StopWatchingAsync` (not found)
- [x] Remove polling timer disposal from `Dispose` (not found)
- [x] Delete unit tests for `PollDirectories` (if any)
- [x] Build and verify no compiler errors

---

### 2.2 Remove Polling Configuration ✅

**Files**:
- `ChronoView/Core/FileWatching/IFileWatcher.cs` (FileWatcherOptions)
- `ChronoView/Models/ApplicationConfiguration.cs` (WorkflowSettings)

**Tasks**:
- [x] Mark `EnablePolling` as Obsolete in `FileWatcherOptions`
- [x] Mark `PollingIntervalMs` as Obsolete in `FileWatcherOptions`
- [x] Mark `EnableNetworkDrivePolling` as Obsolete in `WorkflowSettings`
- [x] Mark `PollingIntervalMs` as Obsolete in `WorkflowSettings`
- [x] Remove usage from `MonitoringOrchestrator.cs`
- [x] Update `appsettings.json` schema (N/A - no appsettings.json found)
- [x] Build and verify no compiler errors

---

## Priority 3: Folder Event Detection ✅

### 3.1 Add Folder Detection to FileWatcherService ✅

**File**: `ChronoView/Core/FileWatching/FileWatcherService.cs`

**Tasks**:
- [x] Add `FolderTimestampCache _folderTimestamps` field
- [x] Update constructor to inject `FolderTimestampCache` dependency
- [x] Replace `Channel<FileSystemEventArgs>` with `PriorityEventChannel`
- [x] Create `HandleFolderCreatedEvent(FileSystemEventArgs e)` method
- [x] Create `HandleFileCreatedEvent(FileSystemEventArgs e)` method
- [x] Create `DetermineEventPriority(FileSystemEventArgs e)` method
- [x] Modify `OnFileSystemEvent`:
  - [x] Check if event is folder (`Directory.Exists(e.FullPath)`)
  - [x] If folder → call `HandleFolderCreatedEvent`
  - [x] If file → call `HandleFileCreatedEvent`
- [x] In `HandleFolderCreatedEvent`:
  - [x] Check folder name pattern (Regex: `^C\d{6}T\d{6}`)
  - [x] Extract timestamp with `ExtractTimestampFromFolderName`
  - [x] Cache timestamp: `_folderTimestamps.Add(path, timestamp)`
  - [x] Determine priority: `DetermineEventPriority(e)`
  - [x] Write to channel: `_eventChannel.Writer.TryWrite(e, priority)`
- [x] In `HandleFileCreatedEvent`:
  - [x] Keep legacy `stitched_original.png` → folder conversion (fallback)
  - [x] Determine priority
  - [x] Write to channel
- [x] Build and test

---

### 3.2 Unit Tests for Folder Detection

**File**: `ChronoView.Tests/Core/FileWatching/FileWatcherServiceTests.cs`

**Tasks**:
- [ ] Test: Folder creation event triggers `HandleFolderCreatedEvent` (optional)
- [ ] Test: Timestamp extracted from folder name `C251216T214727_0`
- [ ] Test: Timestamp cached in `_folderTimestamps`
- [ ] Test: Event queued with `EventPriority.High`
- [ ] Test: Invalid folder name (not matching pattern) is ignored
- [ ] Test: File creation event triggers `HandleFileCreatedEvent`
- [ ] Test: Legacy `stitched_original.png` → folder conversion still works
- [ ] Test: `DetermineEventPriority` returns correct priority for each file type

---

## Priority 4: Parallel Processing in MonitoringOrchestrator

### 4.1 Update MonitoringOrchestrator for Concurrency ✅

**File**: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs`

**Tasks**:
- [x] Change `_activeGroups` type:
  - [x] FROM: `Dictionary<string, FileGroup>`
  - [x] TO: `ConcurrentDictionary<string, FileGroup>`
- [x] Add `_internalEventChannel` field: `Channel<FileSystemEventArgs>`
- [x] Add `_parallelismLimiter` field: `SemaphoreSlim`
- [x] Add `_maxParallelWorkers` field: `int`
- [x] Add `_workerTasks` field: `List<Task>`
- [x] Update constructor:
  - [x] Inject `FolderTimestampCache` dependency
  - [x] Initialize `_internalEventChannel` (unbounded)
  - [x] Read `_maxParallelWorkers` from config (default: 3)
  - [x] Initialize `_parallelismLimiter = new SemaphoreSlim(_maxParallelWorkers)`
- [x] Build and verify

---

### 4.2 Implement Worker Pattern ✅

**File**: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs`

**Tasks**:
- [x] Create `OnFileChanged(object? sender, FileSystemEventArgs e)` method:
  - [x] Write event to `_internalEventChannel`
  - [x] Log warning if write fails
- [x] Create `ProcessEventsWorkerAsync(int workerId, CancellationToken ct)` method:
  - [x] `await foreach` on `_internalEventChannel.ReadAllAsync(ct)`
  - [x] Call `ProcessSingleEventAsync(eventArgs, workerId, ct)`
  - [x] Catch and log exceptions
- [x] Create `ProcessSingleEventAsync` method:
  - [x] Acquire semaphore: `await _parallelismLimiter.WaitAsync(ct)`
  - [x] Process event (ShouldSkipEvent, DetermineFileType, CreateOrUpdateGroupAsync)
  - [x] Release semaphore in `finally` block
- [x] Modify `StartAsync`:
  - [x] Subscribe to `_fileWatcher.FileChanged += OnFileChanged`
  - [x] Initialize `_parallelismLimiter` and `_workerCts`
  - [x] Launch N workers: `Enumerable.Range(0, _maxParallelWorkers).Select(i => ProcessEventsWorkerAsync(i, ct))`
  - [x] Store tasks in `_workerTasks`
- [x] Modify `StopAsync`:
  - [x] Complete `_internalEventChannel.Writer.Complete()`
  - [x] Cancel `_workerCts`
  - [x] `await Task.WhenAll(_workerTasks)`
  - [x] Dispose semaphore and cancellation token
- [x] Build and test (no linter errors)

---

### 4.3 Update Group Matching for Thread Safety ✅

**File**: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs`

**Tasks**:
- [x] Modify `FindMatchingExistingGroup`:
  - [x] Create snapshot: `var snapshot = _activeGroups.ToArray()`
  - [x] Iterate over snapshot instead of `_activeGroups.Values`
  - [x] Add comment explaining why snapshot is needed
- [x] Modify `CreateOrUpdateGroupAsync`:
  - [x] When merging: Add lock: `lock (existingGroup) { MergeGroups(...); }`
  - [x] When creating new: Use `_activeGroups.TryAdd(groupId, newGroup)`
  - [x] Handle collision case (retry with new ID)
- [x] Modify group ID generation:
  - [x] Change to: `Interlocked.Increment(ref _nextGroupId)`
- [x] Build and test

---

### 4.4 Use Cached Timestamps ✅

**File**: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs`

**Tasks**:
- [x] In `CreateOrUpdateGroupAsync`, after creating `newGroup`:
  - [x] Check if `fileType == FileType.Normal`
  - [x] Try get cached timestamp: `if (_folderTimestamps.TryGet(processPath, out DateTime cachedTs))`
  - [x] If found: `newGroup.Timestamp = cachedTs`
  - [x] If not found: Log warning, fall back to `ExtractTimestampFromFolderName`
  - [x] Log cache hit/miss metrics
- [x] Build and test

---

## Priority 5: Configuration

### 5.1 Add Parallel Processing Configuration ✅

**File**: `ChronoView/Models/ApplicationConfiguration.cs`

**Tasks**:
- [x] Add to `WorkflowSettings`:
  - [x] `public int MaxEventProcessingWorkers { get; set; } = 3;`
  - [x] `public bool EnableParallelProcessing { get; set; } = true;`
  - [x] `public int FolderTimestampCacheTTL { get; set; } = 300;  // seconds` (already exists)
- [ ] Update `appsettings.json` with new keys (no appsettings.json found - config loaded from UI)
- [x] Build and test loading configuration

---

### 5.2 Migration Logic for Old Config

**File**: `ChronoView/Services/ConfigurationManager.cs` (or equivalent)

**Tasks**:
- [ ] Add detection for deprecated keys:
  - [ ] Check if `EnableNetworkDrivePolling` exists
  - [ ] Check if `PollingIntervalMs` exists
- [ ] Log warning: "Config key 'X' is deprecated and will be ignored"
- [ ] Set default values for new keys if missing
- [ ] Test with old and new config files

---

## Priority 6: Testing

### 6.1 Unit Tests - MonitoringOrchestrator

**File**: `ChronoView.Tests/Core/FileWatching/MonitoringOrchestratorTests.cs`

**Tasks**:
- [ ] Test: Parallel workers process events concurrently
- [ ] Test: Semaphore limits max concurrent processing to N
- [ ] Test: Snapshot prevents inconsistent matching during concurrent adds
- [ ] Test: Lock protects MergeGroups from concurrent modification
- [ ] Test: No duplicate groups created (race condition scenario)
- [ ] Test: Cached timestamp used when available
- [ ] Test: Fallback to extraction when cache miss
- [ ] Test: Worker failure doesn't stop other workers

---

### 6.2 Integration Tests

**File**: `ChronoView.Tests/Integration/EventProcessingIntegrationTests.cs`

**Tasks**:
- [ ] Test: End-to-end folder creation → group creation <500ms
- [ ] Test: Race condition (external program deletes file, group still created)
- [ ] Test: 10 files arrive simultaneously → all processed <2 seconds
- [ ] Test: No duplicate groups for same Normal folder (concurrent creation)
- [ ] Test: Performance target: 95th percentile <3 seconds

---

### 6.3 Regression Tests

**Tasks**:
- [ ] Run all existing unit tests → verify 100% pass rate
- [ ] Test: Camera files (Cam1-6) still matched correctly
- [ ] Test: NIR files still matched correctly
- [ ] Test: Timestamp extraction for all patterns still works
- [ ] Test: UI displays groups correctly
- [ ] Test: No memory leaks (run for 5 minutes, check memory growth)

---

## Priority 7: Performance Verification

### 7.1 Add Performance Logging

**File**: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs`

**Tasks**:
- [ ] Add stopwatch to measure detection-to-group-creation time
- [ ] Log: "Folder detected in {Ms}ms"
- [ ] Log: "Group created in {Ms}ms from detection"
- [ ] Log: "Active workers: {Count}/{Max}"
- [ ] Log: "Semaphore wait time: {Ms}ms"
- [ ] Log: "Cache hit rate: {HitCount}/{TotalCount}"

---

### 7.2 Measure and Verify Success Criteria

**Tasks**:
- [ ] Baseline measurement: Current delay (before changes)
- [ ] Post-optimization measurement: New delay
- [ ] Verify: Average delay ≤ 2 seconds ✅
- [ ] Verify: 95th percentile ≤ 3 seconds ✅
- [ ] Verify: Normal folders detected within 500ms ✅
- [ ] Verify: CPU usage < 30% ✅
- [ ] Verify: Memory increase ≤ 50MB ✅
- [ ] Verify: Zero duplicate groups ✅
- [ ] Document results in `06_report.md`

---

## Priority 8: Documentation

### 8.1 Create Architecture Documentation

**Tasks**:
- [ ] Create `docs/architecture/module_priority_event_channel.md`
- [ ] Create `docs/architecture/module_folder_timestamp_cache.md`
- [ ] Create `docs/architecture/impact_polling_removal.md`
- [ ] Update `docs/architecture/module_file_watcher_service.md`
- [ ] Update `docs/architecture/module_monitoring_orchestrator.md`
- [ ] Update `docs/architecture/glossary.md`
  - [ ] Add: PriorityEventChannel, EventPriority, FolderTimestampCache
  - [ ] Deprecate: EnablePolling, PollingIntervalMs, PollDirectories
- [ ] Update `docs/architecture/README.md` index

---

## Priority 9: Completion Report

### 9.1 Create Completion Report

**File**: `docs/spec/event_processing_optimization/06_report.md`

**Tasks**:
- [ ] Document what was implemented
- [ ] Document performance results (before/after metrics)
- [ ] Document any deviations from design
- [ ] Document known limitations
- [ ] Document rollback procedure
- [ ] List all modified files
- [ ] List all new files
- [ ] List all deleted files
- [ ] List breaking changes (config)

---

## Implementation Order

**Recommended sequence**:

1. **Day 1-2**: Priority 1 (Create EventPriority, FolderTimestampCache, PriorityEventChannel)
2. **Day 3**: Priority 2 (Remove polling code and configuration)
3. **Day 4-5**: Priority 3 (Add folder event detection)
4. **Day 6-7**: Priority 4 (Parallel processing in MonitoringOrchestrator)
5. **Day 8**: Priority 5 (Configuration changes)
6. **Day 9-10**: Priority 6 (Testing)
7. **Day 11**: Priority 7 (Performance verification)
8. **Day 12**: Priority 8-9 (Documentation and completion)

**Total Estimated Time**: 10-12 days

---

## Definition of Done

A task is complete when:
- [ ] Code is written and builds without errors
- [ ] Unit tests are written and pass
- [ ] Integration tests pass (if applicable)
- [ ] Code is reviewed (self-review or peer review)
- [ ] Documentation is updated
- [ ] Performance is measured and meets targets
- [ ] No regressions introduced

---

**Status**: [ ] Ready to Start Implementation
**Next Step**: Begin Priority 1.1 - Create EventPriority Enum
