---
Task: remove_legacy_ui_mode
Created: 2026-01-15
Status: Draft
Summary: Remove the unused "Legacy UI Mode" setting and associated UI elements.
Research Required: No
---

# Remove Legacy UI Mode - Requirements

## 1. Goal

### 1.1 Primary Goal
Remove the "Legacy UI Mode" setting, which has been identified as dead code/configuration, to clean up the codebase and UI.

### 1.2 Success Criteria
- [ ] "Legacy UI Mode" checkbox is removed from the Settings Dialog.
- [ ] `LegacyUiMode` property is removed from `ApplicationConfiguration` and `SettingsDialogViewModel`.
- [ ] Application builds successfully without errors.
- [ ] Application runs and saves settings correctly without this property.

## 2. Constraints

### 2.1 Technical Constraints
- Must not affect other UI settings.
- Must preserve existing configuration loading/saving mechanism for other properties.

### 2.2 Business Constraints
- Minimal risk; this is a cleanup task.

### 2.3 Non-Goals (Out of Scope)
- Refactoring the entire Settings Dialog (only removing this specific item).
- Removing other potentially unused settings (scope limited to Legacy UI Mode).

## 3. Questions to Investigate
- None. Analysis confirmed it is unused.

## 4. Assumptions
- The setting is truly unused (verified by analysis phase).

## 5. Dependencies
- None.

## Approval
- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
- [ ] Scope boundaries are clear
