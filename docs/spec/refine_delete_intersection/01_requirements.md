# Requirement Specification: Deletion Logic Refinement (Intersection Rule)

## 1. Overview
This document specifies the refinement of the file deletion logic in the ChronoView dashboard. The current implementation allows deletion based on partial selection but does not strictly enforce the "Row Selection" as a master gate, leading to potential confusion or unintended deletions (or non-deletions).

### 1.1 Goal
Ensure that deletion only occurs for components that satisfy **BOTH** conditions:
1.  **Row Logic**: The File Group (Row) must be explicitly selected (checked).
2.  **Component Logic**: The individual component (Normal, NIR, Cam1...) must be explicitly selected (checked).

Equation:  
`Delete(Component) = IsSelected(Group) AND IsSelected(Component)`

## 2. Detailed Rules

### 2.1 The Intersection Rule
*   **Case A (All Checked)**: Row Checked + Component Checked -> **DELETE**.
*   **Case B (Comp Unchecked)**: Row Checked + Component Unchecked -> **KEEP** (Do not delete).
*   **Case C (Row Unchecked)**: Row Unchecked + Component Checked (if state exists) -> **KEEP** (Do not delete).
    *   *Note*: Typically, unchecking the row unchecks components in the UI helper, but programmatically or via specific UI interactions, this state might exist. The rule strictly enforces "Row Unchecked = Safe".

### 2.2 UI Feedback & Confirmation
*   The confirmation dialog must accurately reflect *exactly* what will be deleted based on the Intersection Rule.
*   If a user unchecks a specific component (e.g., "Normal") but keeps the Row checked, the dialog must **NOT** list "Normal" as a deletion target.
*   If a user unchecks the Row, no components from that group should appear in the dialog, even if internal flags were previously true.

### 2.3 Definitions / Sources of Truth (Observed in Code)
*   **Row Selection ("Row Checked")**:
    *   Defined by the left-most row checkbox in the DataGrid.
    *   In the current UI, this checkbox is bound to `DataGridRow.IsSelected`, which is TwoWay-bound to `FileGroupViewModel.IsSelected` via `DataGrid.ItemContainerStyle`.
    *   **Source**: `ChronoView/UI/Controls/FileGroupDataGrid.xaml`
*   **Component Selection ("Component Checked")**:
    *   Defined by the per-item overlay checkboxes (Normal/NIR/Cam1..Cam6).
    *   Bound to `FileGroupViewModel.IsNormalSelected`, `IsNirSelected`, `IsCam1Selected` ... `IsCam6Selected`.
    *   **Source**: `ChronoView/Resources/SharedResources.xaml`

## 3. Success Criteria
1.  **Scenario 1**: Select Row 99. Uncheck 'Normal' checkbox in Row 99. Click Delete.
    *   **Result**: Dialog lists Row 99 but *excludes* Normal image from the list. Normal file is NOT deleted. Other checked components (Cam1, etc.) ARE deleted.
2.  **Scenario 2**: Select "Select All" (Header). Uncheck Row 100. Click Delete.
    *   **Result**: Row 100 is completely ignored. No files from Row 100 are deleted.

## 4. Non-Goals
*   Changing the visual style of checkboxes (focus is on logic).
*   Changing the "Move" logic (this spec focuses on "Delete", though logic might be shared).
