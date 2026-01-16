---
Task: fix_normal_suffix_usage
Created: 2026-01-08
Status: In Progress
Depends On: 04_design.md
---

# Fix Normal Folder Suffix Usage - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Setup | 0 | 0 | 0 |
| Core | 7 | 0 | 7 |
| Integration | 1 | 0 | 1 |
| Documentation | 0 | 0 | 0 |
| Verification | 5 | 0 | 5 |
| **Total** | **13** | **0** | **13** |

---

## Phase 1: Setup

*(No new packages or Glossary updates required)*

---

## Phase 2: Core Implementation

### 2.1 [NEW] Create NormalFolderHelper

- [ ] Create `ChronoView/Helpers/NormalFolderHelper.cs`
- [ ] Implement `DetermineLineNumber` (Suffix priority + Path fallback).
- [ ] Implement `IsValidNormalFolder` (Basic pattern + Suffix check).
- [ ] Implement `IsSubPath` (Path normalization).

**Verify**:
- Create unit test `NormalFolderHelperTests.cs` covering:
  - Suffix=True/False.
  - Path overlaps.
  - Invalid patterns.

### 2.2 Update ApplicationConfiguration Comments

- [ ] Modify `ChronoView/Models/ApplicationConfiguration.cs`
- [ ] Update comments for `Normal1Path` and `Normal2Path`.

### 2.3 Refactor FileGroup Helper

- [ ] Modify `ChronoView/Models/FileGroup.cs`
- [ ] Update `GetLineNumberFromNormalFolder` to delegate to `NormalFolderHelper.DetermineLineNumber`.

**Verify**:
- Existing or new unit tests for `FileGroup` should pass via the helper.

### 2.4 Refactor FileWatcherService Helper

- [ ] Modify `ChronoView/Core/FileWatching/FileWatcherService.cs`
- [ ] Update `IsNormalFolderName` to delegate to `NormalFolderHelper.IsValidNormalFolder`.

**Verify**:
- `FileWatcherServiceHelperTests.cs` (or extend existing).

### 2.5 Refactor GroupManager Logic

- [ ] Modify `ChronoView/Core/FileWatching/GroupManager.cs`
- [ ] Update `DetermineLineNumber` to delegate to `NormalFolderHelper.DetermineLineNumber`.

**Verify**:
- Manual run: App start -> `UseFolderSuffix=true` -> Create Normal folder -> Check logs.

### 2.6 Refactor InitialScanner Filtering

- [ ] Modify `ChronoView/Core/FileWatching/InitialScanner.cs`
- [ ] Update `ScanFilesForDataTypeAsync` to use `NormalFolderHelper.IsValidNormalFolder` predicate.

**Verify**:
- Integration test or manual run with dummy folders `ERR_1` in `Normal1Path`.

### 2.7 Refactor StatisticsService Counting

- [ ] Modify `ChronoView/Core/Analytics/StatisticsService.cs`
- [ ] Update `GetFileCountsAsync`:
  - [ ] Use `NormalFolderHelper.IsValidNormalFolder` as filter.
  - [ ] NOTE: May need to update `CountDirectoriesInDirectoryAsync` to accept a predicate.

**Verify**:
- Dashboard UI statistics check.

---

## Phase 3: Integration

### 3.1 Update FileMatchingEngine (Consistency)

- [ ] Modify `ChronoView/Core/FileMatching/FileMatchingEngine.cs`
- [ ] Update `MatchFiles` signature to accept `MatchingConfiguration`.
- [ ] Implement defensive check for suffix if `UseFolderSuffix` is true.
- [ ] Update callers to pass `Configuration`:
    - [ ] `ChronoView/Core/FileMatching/FileGroupMatcherService.cs`
    - [ ] Unit/Integration Tests (search `MatchFiles` references)

**Verify**: Run existing integration tests.

---

## Phase 4: Documentation

### 4.1 Architecture Documentation

- [ ] Update `docs/architecture/glossary.md` (if needed) to clarify `UseFolderSuffix` impact.
- [ ] Review `docs/architecture/*.md` for any strict suffix assumptions that need relaxing definition.

---

## Phase 5: Final Verification

### 5.1 Test Suite

- [ ] Run all unit tests for modified components.

### 5.2 Manual Verification (Suffix ENABLED)

- [ ] Set `UseFolderSuffix = true`.
- [ ] Create folder `Normal1Path/C123456T123456_0` -> Expected: Line 1.
- [ ] Create folder `Normal1Path/C123456T123456_1` -> Expected: Line 2 (cross-path, log warning).
- [ ] Create folder `Normal1Path/C123456T123456` (no suffix) -> Expected: Ignored or fallback to path (warn).

### 5.3 Manual Verification (Suffix DISABLED)

- [ ] Set `UseFolderSuffix = false`.
- [ ] Create folder `Normal1Path/C123456T123456_0` -> Expected: Line 1 (by path, suffix ignored).
- [ ] Create folder `Normal2Path/C123456T123456_0` -> Expected: Line 2 (by path covers it).

### 5.4 Success Criteria Check

| Criterion | Status | Evidence |
|-----------|--------|----------|
| `InitialScanner` respects flag | ⬜ | Manual scan test |
| `FileWatcher` polling respects flag | ⬜ | Manual drop test |
| `UseFolderSuffix` logic consistent | ⬜ | Code review |
