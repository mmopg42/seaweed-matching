---
Task: Fix Refresh Group Index Reset
Created: 2025-01-17
Status: Draft
Summary: Ensure group index counter is properly reset to 1 when refresh operation is executed
---

# Fix Refresh Group Index Reset - Requirements

## 1. Goal

### Primary Goal
When the user executes a refresh operation, the group index counter (`_nextGroupId` in `MonitoringOrchestrator`) should be reset to 1, ensuring that newly created groups start from index 1 instead of continuing from the previous counter value.

### Success Criteria
- [ ] After refresh, the first new group created has GroupId = 1
- [ ] Subsequent groups increment sequentially from 1 (2, 3, 4, ...)
- [ ] Group index reset is consistent across all refresh scenarios (manual refresh, path change, etc.)

## 2. Constraints

### Technical Constraints
- Must maintain thread safety when resetting the counter
- Must not break existing refresh functionality
- Must work with both sequential scan and legacy batch scan modes
- Must coordinate with `FileGroupMatcherService.ResetState()` which already resets `_groupCounter`

### Non-Goals (Out of Scope)
- Changing the group ID assignment logic beyond the refresh scenario
- Modifying how groups are displayed in the UI
- Changing the FileGroupMatcher's internal counter logic

## 3. Questions to Investigate

- [x] Q1: Where is `_nextGroupId` currently initialized and used?
  - **Answer**: Initialized to 1 in `MonitoringOrchestrator` constructor, incremented when creating new groups
  
- [x] Q2: Does `RefreshAsync` currently reset any counters?
  - **Answer**: Yes, it calls `_fileGroupMatcher.ResetState()` which resets `_groupCounter` to 0, but does NOT reset `_nextGroupId`
  
- [x] Q3: Are there any other places where `_nextGroupId` should be reset?
  - **Answer**: Need to investigate `StartMonitoringAsync` and configuration change scenarios

- [x] Q4: Is there any code that depends on `_nextGroupId` being monotonically increasing across refreshes?
  - **Answer**: No, GroupId is only used for equality comparisons (`==`), not for ordering or sorting
  
- [x] Q5: What happens to the UI when groups are recreated with the same GroupId after refresh?
  - **Answer**: UI clears all groups before refresh (`FileGroups.Clear()`), so no duplicate detection issues. Duplicate detection in `OnGroupCreated` is a safety guard for concurrent events.

- [x] Q6: Does `StartAsync` reset the counter when monitoring starts?
  - **Answer**: No, `StartAsync` does not reset `_nextGroupId`, which could cause issues if monitoring is stopped and restarted

## 4. Assumptions
- Assumption 1: Group IDs do not need to be globally unique across application lifetime, only unique within the current session
- Assumption 2: Resetting group IDs on refresh will not cause UI issues since all groups are cleared before refresh
- Assumption 3: The `_nextGroupId` counter in `MonitoringOrchestrator` and `_groupCounter` in `FileGroupMatcherService` should be synchronized

---
**Status**: [ ] Approved
