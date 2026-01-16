# Implementation Plan: Deletion Logic Refinement

## 1. Goal
Refine the deletion logic to strictly enforce the intersection of "Row Selection" and "Component Selection", and ensure the UI confirmation message accurately reflects this state.

## 2. Granular Implementation Steps
1.  **Unlock Implementation**: Create `05_tasks.md` to clear Spec-Lock.
2.  **ViewModel Gate Logic**:
    *   Modify `FileGroupViewModel.cs`.
    *   Update `GetSelectedComponents` and `GetSelectedComponentDetails`.
    *   Add check: `if (!IsSelected) return empty;`.
    *   **Note (SSoT)**: In the current UI, "Row Checked" is the left-most DataGrid row checkbox. It is TwoWay-bound to `DataGridRow.IsSelected`, which is TwoWay-bound to `FileGroupViewModel.IsSelected` in `ChronoView/UI/Controls/FileGroupDataGrid.xaml`.
3.  **XAML Binding Hardening**:
    *   Modify `SharedResources.xaml`.
    *   Locate logic for `NormalFileTemplate`, `NirFileTemplate`, `CameraTemplates`.
    *   Update `CheckBox` bindings to `Mode=TwoWay, UpdateSourceTrigger=PropertyChanged`.
4.  **Impact Verification**:
    *   Verify `MainWindowViewModel` delete confirmation message logic (Step 2 should automatically fix this).
    *   Verify `FileOperationViewModel` delete execution logic (Step 2 should automatically fix this).
5.  **Performance / Complexity Note**:
    *   Component checks are a fixed, small set (Normal/NIR/Cam1..Cam6), so selection building is effectively **O(1)**.
    *   The implementation should early-return on `IsSelected == false` to avoid any further work when the row is not selected.

## 3. Verification Steps
1.  **Unit/Logic Test**: Verify `GetSelectedComponents` returns empty when `IsSelected=false`.
2.  **Integration Test**:
    *   Open App. Select 1 Row. Uncheck row.
    *   Click Delete. Confirm nothing happens or button disabled.
    *   Select Row. Uncheck Normal. Click Delete. Confirm Normal excluded.
3.  **Message Verification**:
    *   Check that the count in the Delete Dialog strictly matches the components returned by the updated logic.
