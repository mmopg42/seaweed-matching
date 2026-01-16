---
Task: remove_legacy_ui_mode
Created: 2026-01-15
Status: Draft
Depends On: 01_requirements.md
---

# Remove Legacy UI Mode - Code Analysis

## 1. Analysis Summary

| Target | Status | Found In (File:Line) |
|--------|--------|----------------------|
| `LegacyUiMode` Property | Exists | `ApplicationConfiguration.cs` (UISettings) |
| `LegacyUiMode` Property | Exists | `SettingsDialogViewModel.cs` |
| `LegacyUiMode` UI Binding | Exists | `SettingsDialog.xaml` (CheckBox) |
| `LegacyUiMode` Logic | Missing | **None found** in `Orchestrator`, `ViewModels`, or `Views` |

## 2. Codebase Audit

### 2.1 Current Implementation (As-Is)
**Settings Logic**:
- The property is defined in `UISettings` within `ApplicationConfiguration.cs`.
- It is bound in `SettingsDialog.xaml` via `SettingsDialogViewModel.cs`.
- When saved, it writes to the JSON config.
- **CRITICAL**: No other component reads this value to alter behavior. It is a "ghost" setting.

**Evidence**:
- `grep "LegacyUiMode"` only returns definition and binding sites.
- `MonitoringOrchestrator.cs` and `MainWindowViewModel.cs` have no conditional logic based on this flag.

### 2.2 Spaghetti/Legacy Detection
| Type | Location | Description | Risk Level |
|------|----------|-------------|------------|
| Dead Code | `ApplicationConfiguration.cs` | `UISettings.LegacyUiMode` is never read for logic. | Low |
| Dead UI | `SettingsDialog.xaml` | Checkbox exists but does nothing. | Low |

### 2.3 Data Structure Analysis
- **Model**: `UISettings` class inside `ApplicationConfiguration.cs`.
- **Field**: `public bool LegacyUiMode { get; set; } = false;`
- **Action**: Delete this field.

## 3. Impact Analysis (Pre-Flight)

### 3.1 Affected Files
- `c:\workspace\seaweed\gui_kiro_v2\ChronoView\Models\ApplicationConfiguration.cs` (Remove property)
- `c:\workspace\seaweed\gui_kiro_v2\ChronoView\UI\ViewModels\SettingsDialogViewModel.cs` (Remove ViewModel property & mapping)
- `c:\workspace\seaweed\gui_kiro_v2\ChronoView\UI\Views\SettingsDialog.xaml` (Remove CheckBox)

### 3.2 Breaking Changes
- [ ] API Signature Change: None.
- [ ] Database Schema Change: None.
- [x] Config File Format Change: `LegacyUiMode` key will disappear from JSON. This is backward compatible (ignoring missing keys is standard).

## 4. Unresolved Mysteries
- None.

## Approval
- [x] All "Found In" links verified
- [x] No assumptions in the text
- [x] Spaghetti code identified
