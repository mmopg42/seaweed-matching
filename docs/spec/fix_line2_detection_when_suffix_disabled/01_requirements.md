---
Task: fix_line2_detection_when_suffix_disabled
Created: 2026-01-08
Status: Draft
Summary: Line 2 Normal folders not detected when UseFolderSuffix=false
Research Required: Yes
---

# Fix Line 2 Detection When UseFolderSuffix Disabled - Requirements

## 1. Goal

### 1.1 Primary Goal

When `UseFolderSuffix=false`, Normal folders in `Normal2Path` must appear in Line 2 tab regardless of suffix.

### 1.2 Success Criteria

- [ ] Normal folders in `Normal1Path` appear in Line 1 tab (regardless of suffix)
- [ ] Normal folders in `Normal2Path` appear in Line 2 tab (regardless of suffix)
- [ ] Works for both initial scan and real-time detection
- [ ] No duplicate `[Line X]` in log messages

## 2. Constraints

### 2.1 Technical Constraints

- Must not break existing `UseFolderSuffix=true` behavior
- Must not affect Camera/NIR detection logic
- Changes should be limited to Normal folder handling

### 2.2 Non-Goals (Out of Scope)

- Log filtering enhancement (separate issue)
- Changing how Camera/NIR determines line numbers
- UI changes

## 3. Questions to Investigate

- [ ] Q1: Why is Line 2 data not appearing? (InitialScanner? GroupManager? EventProcessor?)
- [ ] Q2: What happens to folders scanned from Normal2Path?
- [ ] Q3: Is `NormalFolderHelper.DetermineLineNumber` returning correct values?
- [ ] Q4: What was the original (pre-fix_normal_suffix_usage) behavior?

## 4. Assumptions

- `Normal1Path` and `Normal2Path` are correctly configured and different
- Folders exist in both paths
- User wants path-based line determination when suffix disabled

## 5. Rollback Status

### 5.1 Changes Made (Need Review)

| File | Change | Status |
|------|--------|--------|
| `NormalFolderHelper.cs` | NEW file | Review needed |
| `GroupManager.cs` | Uses helper for DetermineLineNumber | Review needed |
| `InitialScanner.cs` | Uses helper for filtering | Review needed |
| `StatisticsService.cs` | Uses helper for counting | Review needed |
| `FileWatcherService.cs` | Uses helper for IsNormalFolderName | Review needed |
| `FileGroup.cs` | Uses helper for GetLineNumberFromNormalFolder | Review needed |
| `ApplicationConfiguration.cs` | Comments updated | OK |

### 5.2 Rollback Actions

**Option A**: Full rollback to pre-fix state
**Option B**: Fix only the broken logic in `NormalFolderHelper`

---

## Approval

- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
- [ ] Scope boundaries are clear

**Next Step**: 02_research.md (investigate actual data flow)
