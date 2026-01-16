---
Task: Unified Config Paths
Created: 2026-01-14
Status: In Progress
Depends On: 04_design.md
---

# Unified Config Paths - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Setup | 1 | 0 | 1 |
| Core | 2 | 0 | 2 |
| Integration | 5 | 0 | 5 |
| Documentation | 1 | 0 | 1 |
| Verification | 3 | 0 | 3 |
| **Total** | **12** | **0** | **12** |

---

## Phase 1: Setup

### 1.1 Glossary Updates

- [ ] Add new terms to `docs/architecture/glossary.md`
  - [ ] `LogsDirectory`
  - [ ] `HistoryFilePath`
  - [ ] `PathHelper`
- [ ] Verify no naming conflicts with existing terms

**Verify**: `grep -rn "LogsDirectory" docs/` shows only glossary and spec

---

## Phase 2: Core Implementation

### 2.1 Extend IConfigurationManager & Update Implementation

- [ ] Modify `ChronoView/Core/Configuration/IConfigurationManager.cs`
  - [ ] Add `LogsDirectory` property
  - [ ] Add `HistoryFilePath` property
- [ ] Modify `ChronoView/Core/Configuration/ConfigurationManager.cs`
  - [ ] Implement `LogsDirectory` (LocalApplicationData/prische/ChronoView/Logs)
  - [ ] Implement `HistoryFilePath`
  - [ ] **Critical**: Ensure `appAuthor` defaults to "prische"

**Verify**:
```bash
# Check if properties exist
grep "LogsDirectory" ChronoView/Core/Configuration/IConfigurationManager.cs
```

---

### 2.2 Create PathHelper

- [ ] Create `ChronoView/Core/Configuration/PathHelper.cs`
- [ ] Implement static `LogsDirectory`
- [ ] Implement static `HistoryFilePath`
- [ ] Implement Fallback logic using `LocalApplicationData` + "prische"

**Verify**: File exists and builds

---

## Phase 3: Integration (Refactoring Consumers)

### 3.1 Refactor MainWindowViewModel

- [ ] Modify `ChronoView/UI/ViewModels/MainWindowViewModel.cs`
- [ ] Remove hardcoded `Environment.GetFolderPath` for logs
- [ ] Inject or use existing `IConfigurationManager.LogsDirectory`

**Verify**: `grep "SpecialFolder" ChronoView/UI/ViewModels/MainWindowViewModel.cs` returns 0

---

### 3.2 Refactor LogPanel

- [ ] Modify `ChronoView/UI/Controls/LogPanel.xaml.cs` (3 places)
- [ ] Replace `Environment.GetFolderPath` with `PathHelper.LogsDirectory`

**Verify**: `grep "SpecialFolder" ChronoView/UI/Controls/LogPanel.xaml.cs` returns 0

---

### 3.3 Refactor LogCleanupService

- [ ] Modify `ChronoView/Core/Logging/LogCleanupService.cs`
- [ ] Replace hardcoded path with `IConfigurationManager.LogsDirectory`

**Verify**: `grep "SpecialFolder" ChronoView/Core/Logging/LogCleanupService.cs` returns 0

---

### 3.4 Refactor AbnormalHistoryManager

- [ ] Modify `ChronoView/Core/Analytics/AbnormalHistoryManager.cs`
- [ ] Replace hardcoded path with `IConfigurationManager.HistoryFilePath`

**Verify**: `grep "SpecialFolder" ChronoView/Core/Analytics/AbnormalHistoryManager.cs` returns 0

---

### 3.5 Verify App.xaml.cs

- [ ] Check `App.xaml.cs`
- [ ] Ensure it uses `IConfigurationManager.LogsDirectory` (if not already)

**Verify**: Code inspection

---

## Phase 4: Documentation

### 4.1 Architecture Documentation

- [ ] Update `docs/architecture/glossary.md` (if not done in Phase 1)

---

## Phase 5: Final Verification

### 5.1 Build Verification

- [ ] Build solution

```bash
dotnet build ChronoView/ChronoView.csproj
```

### 5.2 Path Consistency Check

- [ ] Run grep to find any remaining hardcoded paths

```bash
grep -rn "SpecialFolder" ChronoView/ --include="*.cs" | grep -v "ConfigurationManager.cs" | grep -v "PathHelper.cs"
```
**Expected**: 0 results.

### 5.3 Manual Verification

- [ ] Delete `%LOCALAPPDATA%\prische\ChronoView` (Backup first!)
- [ ] Run App
- [ ] Verify `Logs` folder created
- [ ] Verify `config.json` created

---

## Completion Log

| Task | Completed | Duration | Notes |
|------|-----------|----------|-------|
| | | | |

---

## Blockers & Issues

| Issue | Impact | Resolution |
|-------|--------|------------|
| | | |
