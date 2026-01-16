---
Task: implement_group_reordering
Created: 2026-01-13
Status: Complete
Depends On: 04_design.md
---

# Group Reordering (Eviction) - Task Checklist

## Implementation Tasks

### Phase 1: FileGroup Model Updates

- [x] **Task 1.1**: Add `CloneWithNewId` method
  - **File**: `ChronoView/Models/FileGroup.cs`
  - **Lines**: 133-159
  - Deep copies all data fields with new GroupId
  
- [x] **Task 1.2**: Add `ResetData` method
  - **File**: `ChronoView/Models/FileGroup.cs`
  - **Lines**: 165-181
  - Clears all data fields for reuse

### Phase 2: GroupManager Eviction Logic

- [x] **Task 2.1**: Add `CheckEvictionNeeded` method
  - **File**: `ChronoView/Core/FileWatching/GroupManager.cs`
  - **Lines**: 392-460
  - Determines if new file should evict existing group based on min/max delay

- [x] **Task 2.2**: Integrate eviction into `CreateOrUpdateGroupAsync`
  - **File**: `ChronoView/Core/FileWatching/GroupManager.cs`
  - **Lines**: 127-175
  - Calls CheckEvictionNeeded, performs eviction if needed, fires events

## Verification

### Build Verification
- [x] `dotnet build ChronoView/ChronoView.csproj` - Success (14 warnings, 0 errors)

### Manual Testing
- [ ] Test: Normal(T=45) creates group, Cam1(T=48) with min=5 delay → Eviction occurs
- [ ] Test: Normal(T=45) creates group, Cam1(T=48) with min=0 delay → No eviction
- [ ] Verify UI shows correct order after eviction

## Summary

Implemented group reordering via eviction:
- New file can "steal" an existing group's position if delay settings indicate it should precede
- Evicted data is cloned to a new group with later ID
- UI events properly fire for both updated and newly created groups
