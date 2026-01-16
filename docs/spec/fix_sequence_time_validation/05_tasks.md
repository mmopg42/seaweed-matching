---
Task: fix_sequence_time_validation
Created: 2026-01-13
Status: Complete
Depends On: 04_design.md
---

# Fix Data Sequence Time Validation Bug - Task Checklist

## Implementation Tasks

### Phase 1: Core Implementation

- [x] **Task 1.1**: Add Successor Validation Logic
  - **File**: `ChronoView/Core/FileWatching/GroupManager.cs`
  - **Method**: `FindMatchingExistingGroup`
  - **Change**: After predecessor check passes (line 482), iterate over all successor types and validate time difference is within their min/max delay range
  - **Lines Modified**: 483-521

## Verification

### Build Verification
- [x] `dotnet build ChronoView/ChronoView.csproj` - Success (7 warnings, 0 errors)

### Manual Testing
- [ ] Test: Normal arrives after Cam1 in same group → Should reject match
- [ ] Test: Normal arrives before Cam1 with valid time diff → Should match
- [ ] Test: Normal arrives way before Cam1 (exceeds max delay) → Should reject

## Summary

Added successor validation in `GroupManager.FindMatchingExistingGroup`:
- Iterates all successor types (Order > newGroupOrder)
- For each successor already in the group, validates:
  - `succTimeDiff = successor.Timestamp - newFile.Timestamp`
  - Must be within `[succMinDelay, succMaxDelay]`
- If validation fails, candidate is rejected with detailed log message
