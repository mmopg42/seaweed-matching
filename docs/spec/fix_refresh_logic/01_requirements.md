---
Task: fix_refresh_logic
Created: 2026-01-12
Status: Draft
Summary: "Refresh" button must perform a deep reset to reflect all settings, file system, and logic changes.
Research Required: Yes
---

# Fix Refresh Logic - Requirements

## 1. Goal

### 1.1 Primary Goal

The "Refresh" function must trigger a **complete state reset** and **re-evaluation** of the entire workspace, ensuring that any changes to settings, file system, or matching logic are immediately and correctly reflected in the UI.

### 1.2 Success Criteria

- [ ] **Config Update**: Changing settings (e.g., Abnormal Threshold, Folder Suffix, Line Groups) and clicking Refresh applies the new settings to *all* existing and new groups.
- [ ] **File Sync**: use external tools to delete or add files, then click Refresh -> UI reflects exact current state of disk (no stale items).
- [ ] **Matching Logic Reset**: If a sequence was incorrect during real-time monitoring (e.g., wrong grouping), Refresh must re-evaluate all files based on the *current* sequence rules, fixing the errors.
- [ ] **State Clearing**: Internal dictionaries (mappings of Line/Nir/Camera files) must be completely cleared before re-scanning.

> **Rule**: If I cannot trust "Refresh" to fix a weird state, the task is failed.

## 2. Constraints

### 2.1 Technical Constraints

- **Performance**: Deep reset is expensive. Must ensure UI remains responsive (async/await) during the process.
- **State Safety**: Must stop active monitoring/polling threads *before* clearing state to prevent race conditions.
- **Dependency**: `MonitoringOrchestrator`, `GroupManager`, `FileWatcherService` must all have a exposed `Reset()` or `Restart()` mechanism.

### 2.2 Non-Goals (Out of Scope)

- **Preserving Manual Edits**: If the user manually edited a group (if such feature exists), Refresh *should* likely overwrite it if it contradicts the file system. (Assuming "Source of Truth" is the File System + Config).

## 3. Questions to Investigate

- [ ] Q1: Does `MonitoringOrchestrator.Stop()` properly dispose/clear all internal state?
- [ ] Q2: How does `GroupManager` handle re-processing of files it has already "seen"? Does it have a cache that needs clearing?
- [ ] Q3: Is `FileWatcherService` correctly effectively restarting its polling loop with new settings?
- [ ] Q4: Are there any static caches (like `AbnormalHistoryManager`) that need explicit clearing?

## 4. Assumptions

- "Refresh" is intended to be a "Soft Restart" of the monitoring engine.
- The user operates in a way where they expect the previous "wrong" history to be rewritten if the rules change (e.g., changing sequence definition re-groups existing files).

## 5. Dependencies

### 5.1 Blocked By
None.

### 5.2 Blocks
- Reliable testing of other features depends on a working Refresh.
