---
Task: Fix Camera File Merge Overwrite Issue
Created: 2025-12-15
Status: Draft
Summary: Prevent MergeGroups from overwriting existing camera files in real-time monitoring
---

# Fix Camera File Merge Overwrite Issue - Requirements

## 1. Goal

### Primary Goal
Prevent camera files from being overwritten when merging groups during real-time file monitoring. Each camera file should be added to a unique group, not overwrite existing camera data in the same group.

### Success Criteria
- [ ] Camera files (Cam1, Cam2, Cam3) are never overwritten during group merging
- [ ] Each camera file creates or updates a unique group
- [ ] MergeGroups only adds new data, never replaces existing camera files
- [ ] Real-time monitoring creates 8 separate groups for 8 Normal folders + their associated camera files

## 2. Constraints

### Technical Constraints
- Must maintain backward compatibility with NIR and Normal file merging behavior
- Must not modify the Match 3 logic (already has Non-Duplicate Filter)
- Must preserve thread-safety in MergeGroups method
- Must work with existing FileGroup data structure

### Non-Goals (Out of Scope)
- Modifying initial scan logic (legacy batch scan)
- Changing Match 1 or Match 2 logic
- Refactoring FileGroupMatcher algorithm

## 3. Questions to Investigate

- [x] Q1: Where does the overwrite happen?
  - **Answer**: In `MergeGroups()` method at line ~1103
  - **Evidence**: `existingGroup.CameraFiles[camera.Key] = camera.Value;` unconditionally overwrites

- [ ] Q2: Why does Match 3 Non-Duplicate Filter not prevent this?
  - **Method**: Analyze FindMatchingExistingGroup logic flow
  - **Need to check**: Are there other matching paths that bypass Match 3?

- [ ] Q3: Should MergeGroups skip merging if camera key already exists?
  - **Method**: Review intended behavior from architecture docs
  - **Need to check**: Is there a valid case where camera files should be updated?

- [ ] Q4: What is the correct behavior when a camera file arrives for a group that already has that camera type?
  - **Method**: Consult user requirements and expected workflow
  - **Options**: 
    - A) Skip merge (keep existing)
    - B) Create new group (current Match 3 intent)
    - C) Update with newer file (current buggy behavior)

## 4. Assumptions
- Assumption 1: Camera files should NEVER be overwritten once added to a group
- Assumption 2: Match 3 Non-Duplicate Filter is working correctly
- Assumption 3: The issue is in MergeGroups, not in FindMatchingExistingGroup
- Assumption 4: Each camera file instance should belong to exactly one group

---
**Status**: [ ] Approved
