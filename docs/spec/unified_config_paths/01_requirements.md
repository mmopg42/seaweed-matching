---
Task: unified_config_paths
Created: 2026-01-14
Status: Draft
Summary: Centralize all AppData paths into IConfigurationManager to prevent path inconsistency bugs
Research Required: No
---

# Unified Configuration Paths - Requirements

## 1. Goal

### 1.1 Primary Goal

All application data paths (config, logs, history) are managed through a single source (`IConfigurationManager`), eliminating hardcoded paths and ensuring consistency.

### 1.2 Success Criteria

- [ ] All `Environment.GetFolderPath(Environment.SpecialFolder.*)` calls are removed from files other than `ConfigurationManager.cs`
- [ ] `IConfigurationManager` exposes properties for all application paths (Config, Logs, Data)
- [ ] All 5 remaining affected files use `IConfigurationManager` to obtain paths (App.xaml.cs already fixed)
- [ ] Changing base path in one place (`ConfigurationManager`) propagates to all consumers
- [ ] Build succeeds with 0 errors after refactoring

## 2. Constraints

### 2.1 Technical Constraints

- Must maintain backward compatibility (existing config files should still load)
- Path structure convention: `%LOCALAPPDATA%\{Author}\{AppName}\{SubFolder}`
- Author: `prische`, AppName: `ChronoView`
- All paths should use `LocalApplicationData` (not Roaming) for device-specific data

### 2.2 Business Constraints

- No data loss during migration (existing logs/history should remain accessible)
- Minimal code changes to reduce regression risk

### 2.3 Non-Goals (Out of Scope)

- Migrating existing files from old paths to new paths (manual user action if needed)
- Changing config file format or schema
- Adding new configuration options

## 3. Affected Files

### 3.1 Files to Modify

| File | Current Issue | Target Change |
|------|---------------|---------------|
| `ConfigurationManager.cs` | Exposes only `AppDataDirectory` | Add `LogsDirectory`, `HistoryFilePath` properties |
| `IConfigurationManager.cs` | Missing path properties | Add interface members |
| `App.xaml.cs` | Hardcoded log path | Use `IConfigurationManager.LogsDirectory` |
| `MainWindowViewModel.cs:785` | Hardcoded log path | Use `IConfigurationManager.LogsDirectory` |
| `LogPanel.xaml.cs:225,259,351` | Hardcoded log path (3 places) | Use `IConfigurationManager` OR pass path via DI |
| `LogCleanupService.cs:30` | Hardcoded log path | Use injected `IConfigurationManager.LogsDirectory` |
| `AbnormalHistoryManager.cs:34` | Hardcoded history path | Use `IConfigurationManager` from DI |

### 3.2 Current Path Inconsistencies

```
AS-IS (Inconsistent):
├─ ConfigurationManager: %LOCALAPPDATA%\prische\ChronoView\config.json ✓
├─ App.xaml.cs (Logs):   %LOCALAPPDATA%\prische\ChronoView\Logs ✓ (just fixed)
├─ MainWindowViewModel:  %APPDATA%\ChronoView\Logs ✗ (Roaming, wrong author)
├─ LogPanel:             %APPDATA%\ChronoView\Logs ✗ (Roaming, wrong author)
├─ LogCleanupService:    %APPDATA%\ChronoView\Logs ✗ (Roaming, wrong author)
└─ AbnormalHistory:      %APPDATA%\ChronoView\abnormal_history.json ✗

TO-BE (Unified):
%LOCALAPPDATA%\prische\ChronoView\
├─ config.json
├─ abnormal_history.json
└─ Logs\
    └─ {date}\ChronoView_*.log
```

## 4. Assumptions

- `IConfigurationManager` is already registered as a singleton in DI container
- All affected classes can receive `IConfigurationManager` via constructor injection
- `LogPanel.xaml.cs` may need special handling as it's a UserControl (pass path from ViewModel or use static accessor)

## 5. Dependencies

### 5.1 Blocked By

| Dependency | Status | Owner |
|------------|--------|-------|
| None | - | - |

### 5.2 Blocks

| Dependent Task | Impact if Delayed |
|----------------|-------------------|
| None | - |

---

## Approval

- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
- [ ] Scope boundaries are clear
- [ ] All blocking dependencies identified

**Next Step**: 03_plan.md (Research not required - problem is well understood)
