---
Task: Event Processing Optimization
Created: 2025-12-17
Status: Draft
Summary: Optimize file event processing to reduce detection-to-matching delay from 4 seconds to under 2 seconds
Research Required: Yes
---

# Event Processing Optimization - Requirements

## 1. Goal

### Primary Goal
Reduce file detection-to-matching delay from current 4 seconds to under 2 seconds while maintaining system stability and correct file matching.

### Success Criteria
- [ ] Average detection-to-matching delay reduced to ≤ 2 seconds
- [ ] 95th percentile delay ≤ 3 seconds
- [ ] **Normal folders detected and timestamped within 500ms** (race condition requirement)
- [ ] **UI row created immediately** without waiting for image load (two-phase strategy)
- [ ] **Zero duplicate groups for same Normal folder** (critical regression prevention)
- [ ] **Polling removed entirely** - pure event-based detection only
- [ ] **Folder creation events handled directly** (not via file detection)
- [ ] Timestamp extraction works even if stitched_original.png is deleted
- [ ] Image loading deferred and handles missing files gracefully
- [ ] CPU usage remains under 30% during normal operation
- [ ] Memory usage increase ≤ 50MB
- [ ] Zero file matching errors or duplicates introduced
- [ ] All existing unit tests pass

## 2. Constraints

### Technical Constraints
- Must maintain compatibility with existing `FileWatcherService` and `MonitoringOrchestrator` architecture
- Must preserve thread-safe group matching logic (no race conditions)
- Must support both FileSystemWatcher and Polling modes
- Must work on Windows systems with varied hardware (2-16 core CPUs)
- Cannot break existing configuration system (`ApplicationConfiguration`)

### Architecture Change Requirement: Remove Polling ⚠️

**Context**: Based on `docs/trouble/event_vs_polling_analysis.md`

**Current State**:
- FileWatcherService uses **hybrid approach**: FileSystemWatcher (event) + Timer-based polling
- Polling enabled via `FileWatcherOptions.EnablePolling` (default: true for network drives)
- Polling interval: 1000ms (configurable via `PollingIntervalMs`)

**Problems with Polling**:
1. **Resource Waste**: CPU/disk I/O every second even when no files arrive
2. **Redundancy**: FileSystemWatcher already detects events (polling is backup)
3. **Complexity**: Two parallel detection mechanisms create maintenance burden
4. **Delay Still Exists**: 1-second polling doesn't solve 4-second delay issue

**Decision**: **REMOVE POLLING ENTIRELY** ✅

**Rationale**:
- FileSystemWatcher with `NotifyFilters.DirectoryName` **CAN detect** folder creation
- Current issue is **not polling**, but **waiting for file creation**
- Solution: Use folder creation events directly, not file-based triggers
- Polling was added for network drives, but proper event-based detection is more reliable

**Required Changes**:
1. Remove `PollDirectories` method from FileWatcherService
2. Remove `_pollingTimer` and `EnablePolling` option
3. Configure FileSystemWatcher to handle folder creation events **directly**
4. Remove polling-related configuration (PollingIntervalMs, EnableNetworkDrivePolling)

**Benefits**:
- Cleaner code (single detection mechanism)
- Lower resource usage (no periodic scans)
- Faster detection (immediate events vs 1-second polling)
- Simpler testing (no timing-dependent behavior)

### Critical Race Condition Constraint ⚠️
**URGENT**: Normal camera folders are monitored by an **external program** that moves/deletes files immediately after creation.

**Problem**:
```
Timeline:
1. Normal folder created (C251216T214727_0/)
2. stitched_original.png created inside folder
3. External program detects → MOVES/DELETES file (within 1-2 seconds)
4. ChronoView polling (1s delay) → detects → file already GONE!
```

**Impact**: 
- Current 4-second delay means ChronoView **always loses the race**
- Even if we reduce to 2 seconds, still might lose
- **CRITICAL**: Must extract timestamp from folder name immediately, regardless of file presence

**Required Solution**:
- Extract timestamp from **folder name** (C251216T214727_0 → 21:47:27), not file
- Process Normal folders **immediately** when detected (highest priority)
- Do NOT depend on `stitched_original.png` being present during processing

**Two-Phase Processing Strategy** ⭐:
```
Phase 1 (IMMEDIATE - within 500ms):
  Folder detected → Extract timestamp → Create group → Display row in UI
  ↓ External program can delete file now, we already have row!

Phase 2 (DEFERRED - lazy loading):
  User scrolls to row → Load image if still available
  If file deleted → Show placeholder or empty cell
```

**Benefits**:
- **Row is secured** immediately (timestamp + metadata)
- **Matching works** even if images are deleted
- **Image loading** is nice-to-have, not required for core functionality
- **Graceful degradation**: Missing images don't break workflow

**⚠️ CRITICAL CONSTRAINT: Prevent Duplicate Events**

**Historical Issue**:
Previously, the same Normal folder triggered TWO separate events:
1. Folder creation event → Group created (row 1)
2. File (`stitched_original.png`) creation event → Group created AGAIN (row 2)
Result: **Duplicate groups for same folder** ❌

**Required Prevention**:
```
Event Deduplication Strategy:
1. Watch folder creation → Create group ONCE
2. Ignore file creation events inside that folder
   (or detect but don't create new group, just update existing)

Key Mechanisms:
- Track processed folders in memory (_processedFolders cache)
- When folder event → Add to cache → Create group
- When file event → Check if parent folder in cache
  - If YES: Update existing group (Phase 2 image load)
  - If NO: Log warning (should not happen)
```

**Phase 1 vs Phase 2 Clarification**:
- **Phase 1**: Create group + row (happens ONCE per folder)
- **Phase 2**: Update SAME row with image (not create new row)
- **Never**: Create multiple groups/rows for same Normal folder

### Performance Constraints
- Maximum acceptable CPU overhead: +10% during peak file arrival
- Maximum acceptable memory overhead: +50MB
- Must handle burst scenarios: 10+ files arriving within 1 second
- Must not degrade UI responsiveness (WPF main thread)

### Non-Goals (Out of Scope)
- Rewriting entire file watching architecture
- Changing file matching logic/algorithms
- GPU acceleration or hardware-specific optimizations
- Distributed processing across multiple machines
- Real-time file streaming (current polling approach is acceptable)

## 3. Questions to Investigate

- [ ] Q1: What is the current event channel capacity and does it ever fill up?
- [ ] Q2: How much time does `FindMatchingExistingGroup()` actually take per file?
- [ ] Q3: What is the worst-case scenario for concurrent file arrivals?
- [ ] Q4: Are there any I/O bottlenecks (disk read, thumbnail generation)?
- [ ] Q5: What is the current CPU core utilization during file processing?
- [ ] Q6: How does lock contention affect throughput in `_lockObject`?
- [ ] Q7: Can we use FileSystemWatcher for Normal folders instead of polling? (immediate detection)
- [ ] Q8: Does current code already extract timestamp from folder name? If not, where does it read from?
- [ ] Q9: What is the average time between folder creation and file deletion by external program?
- [ ] Q10: Can we detect folder creation event instead of waiting for stitched_original.png?
- [ ] Q11: Does current UI already support lazy image loading or will this require ViewModel changes?
- [ ] Q12: When is thumbnail generation triggered - at group creation or UI display?
- [ ] Q13: How is the current duplicate prevention implemented in FileWatcherService.ShouldProcessEvent?
- [ ] Q14: Does _processedFiles dictionary track folders or files? Need folder-level tracking?
- [ ] Q15: What happens when both folder and file events fire - which is processed first?

## 4. Assumptions

- Files arrive in bursts during production runs (5-10 files in 1-2 seconds)
- Most files can be processed independently (different groups)
- Lock contention on `_lockObject` is a major bottleneck
- Sequential `await foreach` in `ProcessEventsAsync` creates artificial delay
- Hardware: Modern multi-core CPU (4+ cores) available
- Network drives may have higher I/O latency than local drives

## 5. Current System Analysis

### Current Architecture
```
File Created → Polling (1s) → Event Channel → Sequential Processing → Lock → Match → Update UI
                                                    ↓
                                            ONE FILE AT A TIME
                                            (Bottleneck here)
```

### Identified Bottlenecks
1. **Sequential Event Processing**: `ProcessEventsAsync` handles one event at a time
2. **Lock Scope**: Entire `FindMatchingExistingGroup + Add/Merge` locked
3. **Event Queue Backlog**: No visibility into queue depth or priority

### Example Timeline (Current)
```
21:47:42.000 - Normal file created
21:47:43.000 - Detected by polling (1s delay) → added to channel
21:47:43.100 - Cam1 event processing starts
21:47:43.500 - Cam2 event processing starts  
21:47:43.900 - Cam3 event processing starts
21:47:46.535 - Normal event processing starts (2.6s queue wait)
```

## 6. Proposed Solution Direction

### Option A: Parallel Event Processing (Preferred)
Process independent events concurrently with controlled parallelism.

**Pros**:
- Dramatic reduction in queue wait time
- Scales with CPU cores
- Maintains order for dependent events (same group)

**Cons**:
- Increased complexity in lock management
- Need concurrent collection for `_activeGroups`
- Potential race conditions if not careful

**Resource Impact**:
- CPU: +5-15% (proportional to parallelism level)
- Memory: +20-40MB (thread overhead, concurrent collections)
- Threads: +2-4 background threads

### Option B: Priority-Based Queue
Keep sequential processing but prioritize Normal/NIR files.

**Pros**:
- Simpler implementation
- Low resource overhead
- No concurrency issues

**Cons**:
- Only partial improvement (still sequential)
- Doesn't scale with hardware
- Camera files still delayed

**Resource Impact**:
- CPU: +1-2% (priority queue overhead)
- Memory: +5-10MB
- Threads: No change

### Option C: Hybrid Approach (RECOMMENDED for Race Condition)
Priority queue + limited parallelism (2-3 workers) + **immediate timestamp extraction**.

**Key Addition for Normal Folder Race Condition**:
1. **Watch folder creation** (not file creation): Detect `C251216T214727_0/` immediately
2. **Extract timestamp on detection**: Parse folder name → timestamp cache
3. **Priority processing**: Normal folders processed first (before external program deletes files)
4. **File-independent processing**: Timestamp already cached, file deletion doesn't matter

**Implementation**:
```
Detection Stage (FileWatcherService):
  Folder created → Extract timestamp → Cache → Queue with HIGH priority

Processing Stage (MonitoringOrchestrator):
  Phase 1 - IMMEDIATE:
    Read from cache → Create FileGroup → Raise GroupCreated event → UI row appears
    
  Phase 2 - DEFERRED (lazy):
    User scrolls → ViewModel requests image → Load if available → Update UI
    If file deleted → Display placeholder or empty
```

**Pros**:
- Balanced risk/reward
- Controlled resource usage
- **Solves race condition** by extracting timestamp before external program acts
- Gradual improvement path

**Cons**:
- Moderate complexity
- Requires folder-level watching (currently watches files)

**Resource Impact**:
- CPU: +3-8%
- Memory: +15-25MB (includes timestamp cache)
- Threads: +2-3

## 7. Parallel Processing Feasibility

### Computer Resource Impact Assessment

**Low Impact (Acceptable)**:
- Modern CPUs have 4-16 cores; using 2-4 threads is minimal
- File processing is I/O-bound, not CPU-bound (disk read, matching logic)
- .NET ThreadPool efficiently manages thread reuse
- Most time spent waiting (I/O), not computing

**Risk Areas**:
- Lock contention: If all threads compete for `_lockObject`, no benefit
- Memory: Concurrent collections use more memory than simple Dictionary
- Thread thrashing: Too many threads (>8) would hurt performance

**Mitigation**:
- Limit max parallelism to 4 workers (configurable)
- Use `SemaphoreSlim` or `ConcurrentDictionary` to reduce lock scope
- Monitor CPU/memory and add configuration knob

### Recommended Approach
**Option C (Hybrid)**: Priority queue + 2-4 parallel workers + **immediate timestamp extraction**

**Reasoning**:
1. **PRIMARY**: Solves Normal folder race condition with external program
2. **SECONDARY**: Reduces general processing delay from 4s to ~1s
3. Controlled risk (configurable parallelism)
4. Measurable improvement without over-engineering
5. Can disable parallelism if issues arise (fallback to sequential)
6. Incremental path to full parallelism later

**Critical Race Condition Solution**:
- Move timestamp extraction to detection stage (before queuing)
- Watch folder creation events (not file creation)
- Cache timestamps immediately (file deletion won't affect us)
- Process Normal folders with highest priority

## 8. Key Risks

### Risk 1: Race Conditions
**Scenario**: Two threads try to create same group simultaneously  
**Mitigation**: Use `ConcurrentDictionary.GetOrAdd` or double-check lock pattern

### Risk 2: Duplicate Events (CRITICAL - Historical Issue)
**Scenario**: Folder creation + file creation both trigger group creation → duplicate rows  
**Mitigation**: 
- Maintain `_processedFolders` cache with folder paths
- Check cache before creating group
- Folder event creates group, file event only updates
**Testing**: Automated test with folder + file events in quick succession

### Risk 3: Performance Regression
**Scenario**: Overhead of parallelism > benefit of concurrency  
**Mitigation**: Add feature flag to disable, benchmark before/after

### Risk 4: Resource Exhaustion
**Scenario**: 100+ files arrive, spawn 100 threads  
**Mitigation**: Hard cap on max parallel workers (4-8)

### Risk 5: Debugging Complexity
**Scenario**: Race conditions hard to reproduce  
**Mitigation**: Extensive logging, deterministic testing with delays

## 9. Next Steps

After approval of this requirements document:

1. **Research Phase** (02_research.md):
   - Benchmark current performance with detailed metrics
   - Measure lock contention and queue depth
   - Profile CPU/memory during burst scenarios

2. **Planning Phase** (03_plan.md):
   - Design parallel processing architecture
   - Define concurrency primitives (locks, semaphores)
   - Plan configuration options

3. **Design Phase** (04_design.md):
   - Detailed pseudo-code for parallel workers
   - Error handling and rollback strategy
   - Testing strategy (unit, integration, stress)

---
**Status**: [ ] Approved
**Next Step**: 02_research.md (if Research Required = Yes)
