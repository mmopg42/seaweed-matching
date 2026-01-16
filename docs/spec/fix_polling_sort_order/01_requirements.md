---
Task: fix_polling_sort_order
Created: 2026-01-09
Status: Draft
Summary: Fix file polling sort order to use timestamp instead of lexicographic filename sorting
Research Required: No
---

# Fix Polling Sort Order - Requirements

## 1. Goal

### 1.1 Primary Goal

Files discovered during polling are processed in **chronological order by their embedded timestamp**, ensuring NIR (Leader) files create groups with proper sequential IDs.

### 1.2 Success Criteria

- [ ] Files are sorted by extracted timestamp, not by filename lexicographically
- [ ] NIR files with earlier timestamps are processed before Camera/Normal files with later timestamps
- [ ] Group IDs follow timestamp order (e.g., NIR 17:47:03 → 061, not 069)
- [ ] At equal timestamps, sensors are processed in **DataSequenceSettings configured order**
- [ ] No regression in file detection or event processing

## 2. Constraints

### 2.1 Technical Constraints

- Must use existing `FileNamingHelper.ExtractTimestamp()` method
- Must maintain compatibility with all three file types (NIR, Normal, Camera)
- Must not change the polling interval or detection mechanism
- Change is isolated to `FileWatcherService.OnPollTick()`

### 2.2 Business Constraints

- Must preserve existing file naming conventions
- Must not require changes to other services

### 2.3 Non-Goals (Out of Scope)

- Changing ID generation logic (addressed separately if needed)
- Modifying real-time FileSystemWatcher event order (only polling is addressed)
- Changing `GroupManager` matching logic

## 3. Assumptions

- `FileNamingHelper.ExtractTimestamp()` correctly parses timestamps from all file types
- Polling discovers files in batches (not one-by-one)
- Timestamp extraction never fails for valid monitored files

## 4. Dependencies

### 4.1 Blocked By

| Dependency | Status | Owner |
|------------|--------|-------|
| None | - | - |

### 4.2 Blocks

| Dependent Task | Impact if Delayed |
|----------------|-------------------|
| Group ID timestamp alignment | Cannot proceed until sort order is fixed |

---

## Approval

- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
- [ ] Scope boundaries are clear
- [ ] All blocking dependencies identified

**Next Step**: 03_plan.md (Research not required)
