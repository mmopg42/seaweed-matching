# Plan: Fix Delete and Checkbox Bugs

## 1. Goal
Fix three critical issues:
1.  **Selection Sync**: Files selected via UI are disregarded during delete because ViewModel doesn't sync with DataGrid selection.
2.  **Delete Message**: Provide a clear, summarized confirmation dialog showing the top 3 items and a total count, explicitly mentioning the quarantine path.
3.  **Files Remaining**: Ensure delete operations are robust by fixing the selection sync issue, which is the root cause of skipped files.

## 2. Proposed Changes

### 2.1 Fix Checkbox Binding (Selection Sync)
**File**: `ChronoView/UI/Controls/FileGroupDataGrid.xaml`

*   **Current**: No binding for `DataGridRow.IsSelected`.
*   **Change**: Remove `RowStyle` attribute from `DataGrid`. Add `ItemContainerStyle` to bind `IsSelected` TwoWay.
    ```xml
    <DataGrid ... > <!-- Remove RowStyle attribute -->
        <DataGrid.ItemContainerStyle>
            <Style TargetType="DataGridRow" BasedOn="{StaticResource FileGroupRowStyle}">
                <Setter Property="IsSelected" Value="{Binding IsSelected, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
            </Style>
        </DataGrid.ItemContainerStyle>
    </DataGrid>
    ```
*   **Impact**: Resolves the discrepancy between visual selection (Header/Row) and logical selection (ViewModel).

### 2.2 Refactor Delete Message
**File**: `ChronoView/UI/ViewModels/MainWindowViewModel.cs`

*   **Method**: `ExecuteDeleteWithConfirmation()`
*   **Logic Update**:
    1.  Split `selectedGroups` into `Full` and `Partial` delete lists.
    2.  For each category:
        *   Display header: "• Category: N items"
        *   List Top 3 items with specific component details (e.g., "group_xxx (Normal, Cam1)")
        *   Summarize remainder: "... and X more"
    3.  Display Total Summary:
        *   `"Total: {TotalGroups} groups, {TotalItems} items"`
    4.  Display Destination:
        *   `"📁 Moving to Quarantine: {QuarantinePath}"` (Explicitly loaded from config)

## 3. Verification Plan

### 3.1 Automated Tests
*   N/A (UI Binding is hard to unit test).

### 3.2 Manual Verification
1.  **Checkbox Sync**:
    *   Click "Select All" -> Verify `IsSelected` property in VM is true for all items.
    *   Click Row -> Verify `IsSelected` in VM matches.
2.  **Delete Confirmation**:
    *   Select 5 groups.
    *   Click Delete.
    *   **Verify Dialog**: Shows 3 items detailed + "and 2 more groups". Shows correct Path.
3.  **Execution**:
    *   Proceed with Delete.
    *   **Verify**: Files are actually moved to Quarantine folder.
    *   **Verify**: Refreshing does NOT bring them back (because they are physically moved).
