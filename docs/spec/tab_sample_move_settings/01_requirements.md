---
Task: tab_sample_move_settings
Created: 2026-01-08
Status: Draft
Summary: Implement dynamic sample move settings that change based on the active tab (Line1/Line2/Combined).
Research Required: Yes
---

# Tab-Specific Sample Move Settings - Requirements

## 1. Goal

### 1.1 Primary Goal

The "Sample Move Settings" panel in the left sidebar must dynamically display and bind to line-specific settings (Sample Name, Move NIR Count, Move All Data Count) based on the currently active tab (Line 1, Line 2, or Combined).

### 1.2 Success Criteria

- [ ] **Dynamic UI**: When "Line 1" tab is active, the settings panel displays and edits Line 1's "Sample Name", "Move NIR", "Move All Data".
- [ ] **Dynamic UI**: When "Line 2" tab is active, the settings panel displays and edits Line 2's settings.
- [ ] **Combined View**: When "Combined" tab is active, the settings panel displays both Line 1 and Line 2 settings side-by-side (or clearly separated).
- [ ] **Independent Persistence**: Settings for Line 1 and Line 2 are maintained independently in the model, but saved efficiently (e.g., shared debounce).
- [ ] **Moving Correctness**: "Move" operation uses the correct settings and target groups for each line (Line 1 settings + Line 1 groups for Line 1 tab).
- [ ] **Delete Correctness**: "Delete" operation uses the correct "Sample Name" (subject) for quarantine paths and targets only the active line's groups.
  - Line 1 Tab: Uses Line 1 Sample Name, targets Line 1 groups.
  - Line 2 Tab: Uses Line 2 Sample Name, targets Line 2 groups.
  - Combined Tab: Target both lines sequentially with their respective Sample Names.
- [ ] **Sequential Execution**: In Combined tab, operations (Move/Delete) execute sequentially (Line 1 then Line 2) to ensure stability.
- [ ] **Legacy Clean-up**: Old global `SampleName`, `MoveNir`, `MoveAllData` fields are completely removed from configuration and logic (including `FileOperationViewModel`).

## 2. Constraints

### 2.1 Technical Constraints

- **WPF/MVVM**: Implementation must follow the existing MVVM pattern using `MainWindowViewModel`.
- **Configuration Compatibility**: Breaking change for `MatchingSettings` in `ApplicationConfiguration`. Existing global settings will be lost (no migration required as per investigation).
- **Debounced Save**: Settings must be auto-saved with a debounce mechanism to prevent excessive disk I/O.

### 2.2 Business Constraints

- **User Experience**: Transition between tabs should be instant.

### 2.3 Non-Goals (Out of Scope)

- **Migration**: Migrating old global settings to new line-specific settings is NOT required. Users will re-enter settings.
- **Other Tabs**: Tabs other than Line 1, Line 2, and Combined are not affected (if any).

## 3. Questions to Investigate

> Research has been completed and documented in `documents/implementation/탭별_샘플_이동_설정_조사_리포트.md`.

- [x] Q1: How to structure the configuration model? (Solved: `LineMoveSettings` class)
- [x] Q2: How to handle UI switching? (Solved: `DataTemplateSelector`)
- [x] Q3: How to execute moves in Combined mode? (Solved: Sequential execution)

## 4. Assumptions

- Users understand that they need to re-enter their move settings after this update.
- The "Move" logic in `MoveService.BatchMoveAsync` does not need core logic changes, only correct parameters passed from the ViewModel.

## 5. Dependencies

### 5.1 Blocked By

- None

### 5.2 Blocks

- None

---

## Approval

- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
- [ ] Scope boundaries are clear

**Next Step**: 02_research.md (Migration of existing report)
