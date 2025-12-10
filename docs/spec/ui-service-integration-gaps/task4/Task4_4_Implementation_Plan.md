# Task 4.4 Implementation Plan: Line 2 Tab Implementation

## 1. Goal
Implement/Verify the "Line 2" tab in the DataGrid to display monitoring results for the second production line (Cam 4-6, NIR 2, Normal 2).

## 2. Status Analysis
- **XAML (`MainWindow.xaml`)**: Already contains `TabItem Header="Line 2"` with a `DataGrid` bound to `Line2Groups` and `SelectedLine2Group`.
- **ViewModel (`FileGroupViewModel.cs`)**: Already contains properties for `Camera4Thumbnail`, `Camera5Thumbnail`, `Camera6Thumbnail`.
- **ViewModel (`MainWindowViewModel.cs`)**: `Line2Groups` and `SelectedLine2Group` are presumed to exist (based on XAML binding and naming convention).

## 3. Implementation Steps

### 3.1 Verification & Code Review
- **XAML Columns (Line 2)**: 
  - Ensure columns are "Cam 4", "Cam 5", "Cam 6" (already confirmed in XAML inspection).
  - Verify "Main Img" column serves as the "Normal 2" display.
  - Check if `FileGroupRowStyle` applies to Line 2 DataGrid (it shares the style `StaticResource FileGroupDataGridStyle` but RowStyle needs to be checked).
- **Backend Logic (Crucial)**: 
  - Verify `FileWatcherService` or matching logic correctly assigns NIR 2 files to `FileGroup.NirKey` for Line 2 groups.
  - `FileGroupViewModel` uses `NirKey` to load `NirImageThumbnail`. If the backend maps correctly, the UI works.

### 3.2 Consistency Updates
- **Row Style**: Ensure `FileGroupRowStyle` (modified in Task 4.3) is used by Line 2 DataGrid.
- **DragSelectBehavior**: Confirmed present in XAML.

### 3.3 Execution Steps
1.  **Visual Check**: Re-confirm `MainWindow.xaml` Line 2 columns.
2.  **Style Check**: Ensure Task 4.3 `IsSelected` binding propagates to Line 2.
3.  **Runtime Verification**: 
    - Use "Auto Config" or manual file placement to trigger Line 2 group creation.
    - Verify Cam 4-6 thumbnails appear.
    - Verify NIR 2 image appears in "NIR Img" column.
    - Verify Normal 2 image appears in "Main Img" column.

## 4. Execution Plan
since the code is largely in place:
1.  **Code Review**: Double-check `MainWindow.xaml` Line 2 DataGrid columns.
2.  **Refinement**: Ensure `FileGroupRowStyle` updates from Task 4.3 are applied (DataGrid uses the style resource).
3.  **Testing**: Build and run. Simulate Line 2 data generation (or use sample folder) and verify display.

## 5. Dependencies
- Completion of Task 4.3 (DataGridRow styling).
