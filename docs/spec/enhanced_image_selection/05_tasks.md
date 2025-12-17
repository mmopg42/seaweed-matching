---
Task: Enhanced Image Selection (C#)
Created: 2025-12-15
Status: Draft
Depends On: 04_design.md
---

# Enhanced Image Selection - Implementation Tasks

## 1. ViewModel Implementation (`FileGroupViewModel.cs`)
- [ ] **Add Properties**:
    - `bool? IsRowDeleteSelected` (Tri-State)
    - `bool IsMainDeleteSelected`, `IsNirDeleteSelected`, `IsCam1DeleteSelected`... `IsCam6DeleteSelected`
- [ ] **Implement Propagation Logic** (`OnIsRowDeleteSelectedChanged`):
    - Set all child properties to match Row state (if Row state is not null).
    - Ensure only non-null file paths are affected.
- [ ] **Implement Synchronization Logic** (`UpdateRowDeleteSelectionState`):
    - Check all child properties.
    - Set Row state to True (All), False (None), or Null (Mixed).

## 2. Service Implementation (`FileOperationService.cs`)
- [ ] **Add `DeleteFilesAsync` Method**:
    - Signature: `Task<OperationResult> DeleteFilesAsync(IEnumerable<string> filePaths, string quarantinePath, ...)`
- [ ] **Implement Batch Logic**:
    - Create timestamped subfolder in Quarantine (e.g., `Trash/Deleted_20251215_103000`).
    - Iterate paths -> `Task.Run` -> `File.Move`.
    - Handle errors (try-catch per file).
    - Return `OperationResult` with success/failure counts.

## 3. UI Implementation (`App.xaml`, `MainWindow.xaml`)
- [ ] **Add Checkbox Style**:
    - Define `ModernCheckbox` style in `App.xaml` (or `MainWindow.Resources`).
- [ ] **Update DataGrid Columns**:
    - **Row Header**: Replace current template with Tri-State Checkbox bound to `IsRowDeleteSelected`.
    - **Image Columns**: Update `DataTemplate` for Main, NIR, Cam1-6 to include Checkbox bound to respective `Is...DeleteSelected` property.
    - **Visibility**: Bind Checkbox Visibility to Image Path existence (Converter).

## 4. Integration (`MainWindowViewModel.cs`)
- [ ] **Update `ExecuteDeleteAsync`**:
    - Iterate **all** groups (Line1 + Line2).
    - Collect file paths where `Is...DeleteSelected` is true.
    - Show Confirmation Dialog with **File Count**.
    - Call `_fileOperationService.DeleteFilesAsync`.
- [ ] **Post-Delete Cleanup**:
    - Refresh groups (reload thumbnails/paths).
    - If a group becomes completely empty (no files left), remove it from the collection? (Decision: Keep empty shell for now or implement Remove logic if requested).

## 5. Verification
- [ ] **Manual Test 1**: Tri-State Logic (UI).
    - Click Row -> All Child Check.
    - Uncheck One -> Row Indeterminate.
- [ ] **Manual Test 2**: Granular Deletion.
    - Select specific items.
    - Delete.
    - Verify files moved to `Trash/Deleted_.../`.
    - Verify files removed from UI (thumbnails gone).
