# Research: Delete Logic and Checkbox Bugs

## 1. Introduction
This document analyzes three reported issues:
1.  **Files Remaining**: Deleted tokens reappear or files persist on disk.
2.  **Delete Message**: Confirmation dialog is too verbose/confusing.
3.  **Checkbox Sync**: Header checkbox does not visually update row checkboxes, and selection state is inconsistent.

## 2. Code Analysis

### 2.1 Checkbox Synchronization Bug
**File**: `ChronoView/UI/Controls/FileGroupDataGrid.xaml` & `.xaml.cs`

*   **Current Implementation**:
    *   **Header Checkbox**: `SelectAllCheckBox_Checked` event iterates `ItemsSource` and sets `item.IsSelected = true`.
    *   **Row Checkbox**: CellTemplate contains a `CheckBox` bound to `RelativeSource AncestorType=DataGridRow, Path=IsSelected`.
    *   **Interaction Logic**: `OnRowCheckboxPreviewMouseDown` manually toggles `item.IsSelected` and `checkBox.IsChecked`.
*   **The Flaw**:
    *   There is NO binding between `DataGridRow.IsSelected` and `FileGroupViewModel.IsSelected`.
    *   When "Select All" runs, `VM.IsSelected` becomes `true`.
    *   However, `DataGridRow.IsSelected` remains `false` (because it's not bound to the VM).
    *   Since the Row Checkbox is bound to `DataGridRow.IsSelected`, it remains **Unchecked** visually, even though the data model is selected.
    *   **Result**: Visual desync. User sees empty checkboxes despite "Select All".

### 2.2 Delete Failure (Files Remaining)
**File**: `MainWindowViewModel.cs` (Command Execution) & `FileGroupViewModel.cs` (Selection State)

*   **Mechanism**:
    *   `DeleteCommand` executes on groups where `g.IsSelected` is true.
    *   It calls `ExecuteDeleteAsync`.
*   **Scenario A: Row Click vs Checkbox Click**:
    *   If a user clicks the **Row Background** (not checkbox):
        *   `DataGridRow.IsSelected` -> `true`.
        *   `VM.IsSelected` -> **False** (No binding).
        *   Result: `DeleteCommand` sees 0 selected items. Button might be disabled (if `CanExecute` checks VM). If enabled (e.g. by other selection), this group is ignored.
*   **Scenario B: Visual Confusion**:
    *   Due to the bug in 2.1, a user might think items are NOT selected (checkbox empty) and try to manually click them, potentially deselecting them in the VM while visually checking the box (due to `OnRowCheckboxPreviewMouseDown` logic potentially fighting with state).
*   **Conclusion**: The root cause of "Delete not working" is highly likely the **selection state desynchronization**. If the VM doesn't know it's selected, it doesn't delete it.

### 2.3 Delete Message Clarity
**File**: `MainWindowViewModel.cs` -> `ExecuteDeleteWithConfirmation`

*   **Current Output**: Iterates every single group and lists every component ("Normal, Cam1, Cam2...").
*   **Issue**: For 10+ groups, this fills the screen with repetitive text.
*   **Improvement**: 
    *   Display full details for the **first 3 groups** (e.g., "group_001 (4 items: Normal, Cam1, Cam2, Cam3)").
    *   Summarize the rest: "... and 22 more groups."
    *   Clearly display the destination: "Moving to Quarantine: D:\Data\Quarantine".

## 3. Proposed Solution Architecture

### 3.1 Fix Selection Binding (Critical)
In `FileGroupDataGrid.xaml`, add a `RowStyle` (or update existing) to bind the VM state to the View state.

```xml
<DataGrid.RowStyle>
    <Style TargetType="DataGridRow" BasedOn="{StaticResource FileGroupRowStyle}">
        <Setter Property="IsSelected" Value="{Binding IsSelected, Mode=TwoWay}"/>
    </Style>
</DataGrid.RowStyle>
```
*   **Effect**:
    *   ViewModel update (Select All) -> Row.IsSelected = true -> Checkbox (bound to Row) = Checked. **Fixed.**
    *   Row Click -> Row.IsSelected = true -> VM.IsSelected = true. **Fixed.**

### 3.2 Simplified Message
Refactor `ExecuteDeleteWithConfirmation` string builder logic.

### 3.3 Verify File System Ops
Ensure `DeleteService` calls are correct. The current logic seems sound (`Directory.Move` to Quarantine), assuming the input list is correct. Fixing the selection input shoud resolve the "Files Remain" issue.

## 4. Tasks
1.  **Refactor XAML**: Add TwoWay binding for `IsSelected`.
2.  **Refactor ViewModel**: Simplify `ExecuteDeleteWithConfirmation` message generation.
3.  **Verification**: Test "Select All", "Row Click", and "Delete" flows.
