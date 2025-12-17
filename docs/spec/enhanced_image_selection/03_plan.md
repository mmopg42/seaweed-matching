---
Task: Enhanced Image Selection (C#)
Created: 2025-12-15
Status: Draft
Depends On: 01_requirements.md
---

# Enhanced Image Selection - Implementation Plan (C#)

## 0. Requirements Traceability

| Success Criterion | How Addressed | Verification |
|-------------------|---------------|--------------|
| Checkboxes under thumbnails | Modify `MainWindow.xaml` DataGrid DataTemplates. | Visual inspection |
| Granular selection | Add `Is...DeleteSelected` properties to `FileGroupViewModel`. | Unit test / Manual test |
| Tri-State Row Checkbox | Implement sync logic in `FileGroupViewModel`. | Unit Test (SelectionLogic) |
| "Delete Selected" action | Update `MainWindowViewModel` & `FileOperationService`. | Manual deletion test |
| "Pretty" design | Use Styled Checkbox in XAML. | Visual inspection |

## 1. Architecture Overview

### 1.1 Data Flow
```
User clicks Checkbox (UI) 
  <-> TwoWay Binding 
  <-> `FileGroupViewModel.Is<Type>DeleteSelected`
  
User clicks "Delete Selected" (UI)
  -> `MainWindowViewModel.ExecuteDeleteAsync`
  -> Collect specific file paths from selected groups based on flags
  -> `FileOperationService.DeleteFilesAsync` (New Method or Overload)
  -> Physical File Move to Quarantine
```

### 1.2 State Management
- **Row Selection**: `IsRowDeleteSelected` (New, bool?, Tri-State).
    - Separation from standard `IsSelected` (which creates visual row highlight) to avoid conflict.
- **Image Selection**: New properties in `FileGroupViewModel`:
    - `IsMainDeleteSelected`
    - `IsNirDeleteSelected`
    - `IsCam1DeleteSelected` ... `IsCam6DeleteSelected`
    - `IsNirGraphDeleteSelected` (Linked to NIR file deletion)
- **Synchronization Logic**: 
    - **Row -> Item**: Sets all *available* (non-null) items to Read value.
    - **Item -> Row**: Recalculates state based on *available* items only.
        - All Available Selected -> True
        - All Available Deselected -> False
        - Mixed -> Null (Indeterminate)

---

## 2. Components

### 2.1 ViewModels (`FileGroupViewModel.cs`)

#### Properties
- `bool? IsRowDeleteSelected`
- `bool IsMainDeleteSelected`
- `bool IsNirDeleteSelected`
- `bool IsCam1DeleteSelected` ...

#### Logic
- `UpdateRowDeleteSelectionState()`:
    - Lists all active properties where `Path != null`.
    - Computes Tri-State logic.
- `OnIsRowDeleteSelectedChanged()`:
    - If user sets to True/False, propagate to all *available* item properties.

### 2.2 UI (`MainWindow.xaml`)

#### DataGrid Columns
- **Row Header**: Bind Checkbox to `IsRowDeleteSelected`.
- **Image Columns**: 
    - StackPanel with Image + Checkbox.
    - Checkbox visibility bound to Image availability (if Image null, Checkbox collapsed or disabled).
    - Bind to `Is...DeleteSelected`.

### 2.3 Services (`FileOperationService.cs`)

#### `DeleteFilesAsync` (Refined Approach)
- **Concept**: Explicitly pass list of files to delete.
- **Signature**: `Task<OperationResult> DeleteFilesAsync(IEnumerable<string> filePaths, string quarantinePath, ...)`
- **Behavior**:
    - Iterate unique paths.
    - Move each to Quarantine.
    - **No Directory Optimization**: Since we are deleting specific files, we cannot simply move the "Normal Folder". We must move files individually.
    - **Safety**: Verify file existence before move.
- **Legacy Logic Support**: `DeleteFileGroupAsync` can remain for backward compatibility or be refactored to call `DeleteFilesAsync` with all group files.

---

## 3. Implementation Details

### 3.1 `FileGroupViewModel` Logic
```csharp
private void UpdateRowState()
{
    var states = new List<bool>();
    if (MainImagePath != null) states.Add(IsMainDeleteSelected);
    if (NirImagePath != null) states.Add(IsNirDeleteSelected);
    // ... add others if Path != null

    if (states.All(x => x)) IsRowDeleteSelected = true;
    else if (states.All(x => !x)) IsRowDeleteSelected = false;
    else IsRowDeleteSelected = null;
}
```

### 3.2 `MainWindowViewModel` Deletion
```csharp
var filesToDelete = new List<string>();
foreach (var group in selectedGroups)
{
    if (group.IsMainDeleteSelected && group.MainImagePath != null) filesToDelete.Add(group.MainImagePath);
    // ... collect all
}

var result = await _service.DeleteFilesAsync(filesToDelete, quarantinePath, progress);
```

### 3.3 Safety & Error Handling (New)
- **Confirmation**: Show count of *individual files* to be deleted, not just groups.
    - "Delete 5 files from 2 groups?"
- **Partial Failure**:
    - If some files fail, report "Deleted X files, Failed Y files".
    - Do NOT rollback successful deletions (standard file manager behavior).
- **Update UI**:
    - After deletion, refresh `FileGroupViewModel` to reflect missing files (e.g. set ImagePath to null or reload).
    - If all files in a group are gone, remove group from list.

---

## 4. Verification Plan

### 4.1 Automated Tests
- `FileGroupViewModelTests.cs`:
    - Test `IsRowDeleteSelected` logic with missing images (e.g. HasNir=false).
    - Test Tri-State transitions.

### 4.2 Manual Verification
- **Scenario 1**: Row with partial images (No NIR).
    - Verify Row Checkbox controls only existing images.
    - Verify empty NIR slot has no checkbox or disabled.
- **Scenario 2**: Granular Delete.
    - Select Main + Cam1. Leave Cam2.
    - Delete.
    - Verify Main/Cam1 moved to Trash. Cam2 remains.
    - Verify UI updates (Main/Cam1 gone, Cam2 visible).

---

## 5. Documentation Plan

### Docs to Update
- `docs/architecture/feature_enhanced_image_selection.md` (New): Document the feature architecture.
- `docs/architecture/module_file_operation_service.md`: Update to reflect `DeleteFilesAsync`.
- `docs/architecture/glossary.md`: Add `IsRowDeleteSelected` and related terms.
