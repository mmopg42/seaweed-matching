---
Task: Fix Duplicate File Events in FileWatcherService
Created: 2024-12-16
Status: Draft
Summary: Prevent duplicate file system events from being raised when monitoring directories, especially for Normal camera files
---

# Fix Duplicate File Events - Requirements

## 1. Goal

### Primary Goal
Eliminate duplicate file system events being raised by FileWatcherService, ensuring each file creation is processed exactly once by the matching engine.

### Success Criteria
- [ ] Each file creation event is raised exactly once to subscribers
- [ ] No duplicate matching attempts for the same file in logs
- [ ] Normal camera files (and all other file types) are processed only once
- [ ] Existing deduplication logic in `_knownFiles` works correctly
- [ ] No performance degradation in file monitoring

## 2. Problem Analysis

### Current Behavior (from logs)
```
Debug 2025-12-16 오후 8:52:25 Normal[C251216T205208_0] 타임스탬프 매칭 실패 → 새 그룹 생성 → group_013
Debug 2025-12-16 오후 8:52:30 Normal[C251216T205208_0] 타임스탬프 매칭 실패 → 새 그룹 생성 → group_018
```

The same Normal camera file `C251216T205208_0` is being processed multiple times, creating duplicate groups.

### Root Cause Hypothesis
The FileSystemWatcher is configured with:
- `IncludeSubdirectories = true`
- Multiple event types subscribed (Created, Changed, Deleted, Renamed)
- Watching at folder level

This can cause:
1. Multiple events for the same file (Created + Changed)
2. Events from both parent and subdirectory watchers
3. Polling mechanism detecting files already seen by FileSystemWatcher

## 3. Constraints

### Technical Constraints
- Must maintain existing `_knownFiles` deduplication mechanism
- Must not break existing file watching functionality for NIR, Normal, and Camera files
- Must preserve health monitoring and error handling
- Must work with both FileSystemWatcher and polling mechanisms

### Non-Goals (Out of Scope)
- Changing the overall architecture of file monitoring
- Modifying the matching engine logic
- Changing how files are grouped

## 4. Questions to Investigate

- [ ] Q1: Are duplicate events coming from FileSystemWatcher itself (Created + Changed for same file)?
- [ ] Q2: Is the polling mechanism detecting files already seen by FileSystemWatcher?
- [ ] Q3: Is the `ShouldProcessEvent` filter working correctly for all file types?
- [ ] Q4: Are there race conditions between polling and FileSystemWatcher events?
- [ ] Q5: Does the baseline scan properly populate `_knownFiles` before monitoring starts?

## 5. Assumptions
- The `_knownFiles` HashSet is the correct mechanism for deduplication
- The issue is in event filtering, not in the matching engine
- FileSystemWatcher can raise multiple events for a single file operation
- The polling mechanism is intended as a backup, not primary detection method

---
**Status**: [ ] Approved
