---
Task: fix_normal_suffix_usage
Created: 2026-01-08
Status: Draft
Summary: Consistent application of UseFolderSuffix setting for Normal folders
Research Required: No
---

# Fix Normal Folder Suffix Usage - Requirements

## 1. Goal

### 1.1 Primary Goal

The application consistently applies the `UseFolderSuffix` configuration across all file scanning, monitoring, and processing components, ensuring correct line separation (Line 1 vs Line 2).

### 1.2 Success Criteria

- [ ] `InitialScanner` ignores suffixes when `UseFolderSuffix` is false.
- [ ] `FileWatcherService` (polling/silent scan) ignores suffixes when `UseFolderSuffix` is false.
- [ ] `GroupManager` determines line numbers based solely on `Normal1Path`/`Normal2Path` when `UseFolderSuffix` is false.
- [ ] `FileGroup` helper methods respect the `UseFolderSuffix` setting.
- [ ] `StatisticsService` counts files correctly without suffix filtering when `UseFolderSuffix` is false.
- [ ] `FileMatchingEngine` respects `UseFolderSuffix` for line-based grouping logic.
- [ ] `UseFolderSuffix` property comments in `ApplicationConfiguration` are updated to reflect reality (optional/required).

## 2. Constraints

### 2.1 Technical Constraints

- Must rely on `Normal1Path` and `Normal2Path` for line determination when suffixes are disabled.
- Must maintain backward compatibility for existing deployments using suffixes.
- Modifications should be localized to the specified file watching/matching components.

### 2.2 Non-Goals (Out of Scope)

- Changes to Python scripts (`file_matcher.py` etc.) are explicitly out of scope.
- Changes to UI logic beyond what is affected by the underlying data models.

## 3. Questions to Investigate

> Research Required = No

## 4. Assumptions

- `Normal1Path` and `Normal2Path` are distinct when `UseFolderSuffix` is false, or overlap issues are handled by existing path checking logic.
- The user has already verified the Python logic is correct.

## 5. Dependencies

### 5.1 Blocked By

None.

### 5.2 Blocks

None.

---

## Approval

- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
- [ ] Scope boundaries are clear
- [ ] All blocking dependencies identified

**Next Step**: 03_plan.md
