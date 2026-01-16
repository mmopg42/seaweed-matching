# Bug Fix Specification: Delete Logic and Checkbox Binding

## 1. Overview
This document outlines the requirements and plan to fix three reported bugs related to the file deletion process and UI behaviors in the Monitor Dashboard.

### Reported Issues
1.  **Files Remain After Delete**: Consistently repro-able. Files are not deleted from the disk, and subsequently re-matched as new groups upon refresh or rescanning. Suspicion of "Line/Group confusion," but root cause analysis points to selection state issues.
2.  **Delete Message Clarity**: The current confirmation message is too verbose regarding "Partial Deletion" and lists internal component details (e.g., "Normal, Cam1, Cam2...") which confuses the user.
3.  **Checkbox Behavior**: Clicking the Header Checkbox selects individual items (visually) but does not check the "Row Checkbox" (leftmost column). Conversely, checking the Row Checkbox does not properly propagate to selecting all components for deletion.

## 2. Root Cause Analysis

### 2.1 Checkbox & Selection Propagation (Bug 3 & 1)
*   **Current Behavior**: The `FileGroupDataGrid`'s Row Checkbox is bound to `DataGridRow.IsSelected`. However, there is no explicit binding between `DataGridRow.IsSelected` and `FileGroupViewModel.IsSelected` in the `ItemContainerStyle`.
*   **Consequence**:
    *   When the user clicks "Select All" (Header), the code-behind sets `FileGroupViewModel.IsSelected = true`. This logically selects components, allowing `Delete` to work for "Select All".
    *   When the user clicks the **Row Checkbox** manually, it toggles `DataGridRow.IsSelected` but **does not update** `FileGroupViewModel.IsSelected`.
    *   The Delete Command relies on `FileGroupViewModel.IsSelected` (and subsequent `IsAnyPartialSelected`). If `ViewModel.IsSelected` is false, the Delete logic **skips** the group entirely.
    *   **Result**: User checks the row, clicks Delete, dialog might not even show, or shows empty? (Wait, confirms logic: `selectedGroups = Dashboard.FileGroups.Where(g => g.IsSelected)`). If VM.IsSelected is false, nothing is selected. The command shouldn't execute.
    *   **Alternative Scenario**: User uses "Select All". `VM.IsSelected` becomes true. Components selected. Delete proceeds.
    *   **Re-evaluating Bug 1**: If "Select All" works, why do files remain?
    *   Possible cause: `FileGroupViewModel.IsSelected` setter updates `IsCamXSelected`, but `FileGroupDataGrid.xaml.cs`'s `SelectAllCheckBox_Checked` sets `item.IsSelected = true`.
    *   It seems the code in `FileGroupViewModel` works:
        ```csharp
        public bool IsSelected { set { IsNormalSelected = ... = value; } }
        ```
    *   **The Fix**: We must ensure `DataGridRow.IsSelected` is TwoWay bound to `FileGroupViewModel.IsSelected` so that Row Click <-> ViewModel State is always synchronized.

### 2.2 Delete Message (Bug 2)
*   **Current**: Detailed listing of every selected component per group, which is too long for many items.
*   **Requirement**: Provide detailed context for a few items, then summarize the rest.
    *   **Format**:
        *   Show details for the **first 3 groups** (e.g., "group_001: Normal, Cam1, Cam2 (Move to Quarantine)").
        *   If more than 3, show "... and X more groups".
        *   Explicitly state the **Quarantine Path** where files are being moved.
    *   **Goal**: User should understand *exactly* what is happening (Move to Quarantine) and *what* content is affected, without being overwhelmed by a massive list.

### 2.3 Files Remaining (Bug 1)
*   If the selection logic is fixed, the "Skip" issue is resolved.
*   If physical deletion fails (e.g., file lock), the system currently indicates failure in the status bar/log but might be missed by the user.
*   **Action**: Ensure `DeleteService` behaves robustly and `MainWindowViewModel` reflects success/failure clearly.
*   **Verification**: Check if `DeleteService` logic (Move to Quarantine) handles `Directory.Move` collisions or failures gracefully.

## 3. Implementation Plan

### 3.1 Fix Checkbox Binding
*   **Target**: `ChronoView/UI/Controls/FileGroupDataGrid.xaml`
*   **Action**: Add `ItemContainerStyle` to `DataGrid` to bind `IsSelected` property.
    ```xml
    <DataGrid.ItemContainerStyle>
        <Style TargetType="DataGridRow" BasedOn="{StaticResource FileGroupRowStyle}">
            <Setter Property="IsSelected" Value="{Binding IsSelected, Mode=TwoWay}"/>
        </Style>
    </DataGrid.ItemContainerStyle>
    ```

### 3.2 Simplify Delete Message
*   **Target**: `ChronoView/UI/ViewModels/MainWindowViewModel.cs` -> `ExecuteDeleteWithConfirmation`.
*   **Action**: Refactor message construction.
    *   Header: "Deleting X Groups"
    *   List: "group_001", "group_002" ... (Compact list)
    *   Footer: "Move to Quarantine: [Path]"

### 3.3 Verify & Robustness
*   **Target**: `ChronoView/UI/ViewModels/FileGroupViewModel.cs`
*   **Action**: Verify `IsSelected` setter correctly triggers `NotifySelectionComputed`. (Confirmed in analysis, but will double-check).

## 4. Verification Steps
1.  **Checkbox Test**:
    *   Click Header Checkbox -> Verify all Row Checkboxes checked -> Verify all Component Checkboxes checked.
    *   Click Row Checkbox -> Verify `VM.IsSelected` updates -> Verify Components updated.
2.  **Delete Test**:
    *   Select multiple groups. delete.
    *   Verify files are actually moved to Quarantine.
    *   Verify they do NOT reappear on Refresh.
3.  **Message Test**:
    *   Check confirmation dialog readability.

