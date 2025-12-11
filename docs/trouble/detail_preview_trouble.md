# Troubleshooting Detail Preview View

## Problem Description
User reported that the "Detail Preview View" was not appearing upon double-clicking rows in the DataGrid, or that "preview is not applied".

## Investigation Findings
1.  **Reference Project Impact**: The user provided `C#_project\gui_c` as a reference. Investigation revealed this is a Next.js (React) project. No relevant C# code was found for the "Preview" logic, so we focused on the WPF implementation.
2.  **Implementation Verification**:
    - `ExecuteOpenDetailView` in `MainWindowViewModel.cs` correctly calls `DetailPreviewVM.UpdateGroup`, which sets `IsVisible = true`.
    - `DetailPreviewControl` is correctly placed in Row 1 of the Main Grid in `MainWindow.xaml`.
3.  **Root Cause Identified**: The DataGrids in `MainWindow.xaml` had **redundant and potentially conflicting event handlers**.
    - They included `EventSetter` (via `FileGroupRowStyle`), `InputBindings` (inline), AND `i:Interaction.Triggers` (inline).
    - This triple-handling (especially mixed InputBindings/Triggers) can cause the event to be handled/consumed unpredictably, or fire multiple times, potentially toggling visibility or failing silently.

## Resolution
1.  **Cleaned up XAML**: Removed all redundant `DataGrid.InputBindings` and `i:Interaction.Triggers` from the Line 1, Line 2, and Combined DataGrids.
    - We now rely **exclusively** on the `EventSetter Event="MouseDoubleClick"` defined in `FileGroupRowStyle`. This is the standard and most robust way to handle row double-clicks in WPF MVVM.
    - Added `LogInformation` to `MainWindowViewModel.ExecuteOpenDetailView` to verify the command execution.
    - Added `LogInformation` to `MainWindow.xaml.cs` (`FileGroupRow_MouseDoubleClick`) to confirm the event hits the code-behind.
    - Added warnings for null groups.
3.  **Fixed Visibility Binding**:
    - Identified a potential XAML resource resolution issue where `BooleanToVisibilityConverter` (defined in `UserControl.Resources`) was being accessed by the `UserControl` root element's `Visibility` attribute. 
    - Moved the `Visibility` binding to the child `Border` element inside `DetailPreviewControl.xaml`. This ensures the resource is correctly in scope.

## Verification
- **Build**: Successful.
- **Action Required**: Run the application. Double-click any row. Check the "Message Log" panel or console output.
    - Expected Log: "Row DoubleClick detected..." followed by "Detail view updated/opened...".
    - The Detail View (between DataGrid and Log Panel) should appear.
