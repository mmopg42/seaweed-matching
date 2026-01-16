---
Task: remove_legacy_ui_mode
Created: 2026-01-15
Status: Draft
Depends On: 02_analysis.md
---

# Remove Legacy UI Mode - Plan

## 1. Plan Summary
Remove the dead code associated with "Legacy UI Mode" from the configuration, ViewModel, and View.

## 2. Implementation Steps

### 2.1 Configuration Layer
- **Target**: `ApplicationConfiguration.cs`
- **Action**: Remove `LegacyUiMode` property from `UISettings` class.

### 2.2 ViewModel Layer
- **Target**: `SettingsDialogViewModel.cs`
- **Action**:
    - Remove private field `_legacyUiMode`.
    - Remove public property `LegacyUiMode`.
    - Remove loading logic: `LegacyUiMode = _configuration.UISettings.LegacyUiMode;`
    - Remove saving logic: `_configuration.UISettings.LegacyUiMode = LegacyUiMode;`

### 2.3 UI Layer
- **Target**: `SettingsDialog.xaml`
- **Action**: Remove the `CheckBox` element bound to `LegacyUiMode`.

## 3. Verification Plan

### 3.1 Automated Verification
- Run `dotnet build` to ensure no binding errors or missing properties.

### 3.2 Manual Verification
1. Open application.
2. Navigate to Settings -> UI Options tab.
3. Confirm "Legacy UI Mode" checkbox is absent.
4. Save settings and restart.
5. Confirm application starts normally.

## 4. Backout Plan
- If compilation fails, revert changes.
- If runtime error occurs (e.g. JSON deserialization), restore property but keep it hidden (though unlikely to fail as JSON parser ignores extra fields).

## Approval
- [ ] Plan reviewed and approved
