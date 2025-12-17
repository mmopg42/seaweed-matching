---
Task: Enhanced Image Selection (C#)
Created: 2025-12-15
Status: Draft
Depends On: 03_plan.md
---

# Enhanced Image Selection - Detailed Design

## 1. Component Designs

### 1.1 `FileGroupViewModel` (State Machine)

#### Properties
```csharp
// Tri-State Master Checkbox for Deletion
public bool? IsRowDeleteSelected 
{ 
    get => _isRowDeleteSelected; 
    set => SetProperty(ref _isRowDeleteSelected, value, onChanged: OnIsRowDeleteSelectedChanged); 
}

// Individual Deletion Flags (Separated from Row Selection)
public bool IsMainDeleteSelected 
{
    get => _isMainDeleteSelected;
    set => SetProperty(ref _isMainDeleteSelected, value, onChanged: UpdateRowDeleteSelectionState);
}
// Mirrors for NIR, Cam1-6...
public bool IsNirDeleteSelected { ... }
public bool IsCam1DeleteSelected { ... }
```

#### Logic: Parent -> Child (Propagation)
```csharp
private void OnIsRowDeleteSelectedChanged()
{
    if (_isUpdatingSelection) return;

    // Only propagate explicit True/False from user. Ignore Null (Indeterminate) set by logic.
    if (IsRowDeleteSelected.HasValue)
    {
        _isUpdatingSelection = true;
        try
        {
            bool intendedState = IsRowDeleteSelected.Value;
            
            // Only select available files (ignore null paths)
            if (MainImagePath != null) IsMainDeleteSelected = intendedState;
            if (NirImagePath != null) IsNirDeleteSelected = intendedState;
            // ... repeat for all existing cameras
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }
}
```

#### Logic: Child -> Parent (Synchronization)
```csharp
private void UpdateRowDeleteSelectionState()
{
    if (_isUpdatingSelection) return;
    _isUpdatingSelection = true;
    
    try
    {
        var states = new List<bool>();
        
        // Collect states ONLY for existing files
        if (MainImagePath != null) states.Add(IsMainDeleteSelected);
        if (NirImagePath != null) states.Add(IsNirDeleteSelected);
        // ... Check all Cams
        
        if (!states.Any()) 
        {
            IsRowDeleteSelected = false; 
            return;
        }

        bool allSelected = states.All(s => s);
        bool noneSelected = states.All(s => !s);

        if (allSelected) IsRowDeleteSelected = true;
        else if (noneSelected) IsRowDeleteSelected = false;
        else IsRowDeleteSelected = null; // Indeterminate
    }
    finally
    {
        _isUpdatingSelection = false;
    }
}
```

---

### 1.2 `MainWindow.xaml` (UI Templates)

#### Scope of Deletion
> **Design Decision**: "Delete Selected" acts on **ALL checked boxes** in the current view (Line 1 / Line 2 / Combined), **regardless of Row Highlighting**.
> - Why: The Checkbox *is* the explicit selection for this action.
> - Verification: Loop through `Line1Groups` + `Line2Groups` (or `FileGroups` if unified) checking `IsRowDeleteSelected != false`.

#### Templates
*Use standard `CheckBox` with minimal styling for native feel, or `ModernCheckbox` style defined in App.xaml.*

```xml
<!-- Row Header: Tri-State Master -->
<DataGridTemplateColumn Width="40">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <CheckBox IsChecked="{Binding IsRowDeleteSelected, UpdateSourceTrigger=PropertyChanged}"
                      IsThreeState="True"
                      HorizontalAlignment="Center" VerticalAlignment="Center"/>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>

<!-- Image Column: Image + Checkbox -->
<DataGridTemplateColumn Header="Main Img" Width="*">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <StackPanel>
                <Border ...><Image Source="{Binding MainImageThumbnail}"/></Border>
                <CheckBox IsChecked="{Binding IsMainDeleteSelected}" 
                          Visibility="{Binding MainImagePath, Converter={StaticResource NullToVisibilityConverter}}"
                          HorizontalAlignment="Center"/>
            </StackPanel>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

---

### 1.3 `FileOperationService` (Deletion Logic)

#### Target Specifications
| Property | Target File | Notes |
|---|---|---|
| `IsMainDeleteSelected` | `MainImagePath` | Usually `stitched_original.png` |
| `IsNirDeleteSelected` | `NirFilePath` | The NIR source file (not just graph) |
| `IsCamXDeleteSelected` | `GetCameraImagePath(X)` | `cam_X.png` |

#### Quarantine Structure
To prevent collisions and loss of context:
`QuarantinePath / {GroupId}_{Timestamp} / {FileName}`

#### Async Safety
Service methods MUST use `await Task.Run(...)` for all I/O to prevent UI freeze.

```csharp
public async Task<OperationResult> DeleteFilesAsync(
    IEnumerable<string> filePaths, 
    string quarantinePath, 
    ...)
{
    return await Task.Run(async () => 
    {
        // 1. Group files by 'Source Folder' or just create unique quarantine subfolder per batch?
        // Decision: Create ONE batch folder for this operation, or per Group?
        // Per Group is safer for restore.
        
        foreach (var path in filePaths)
        {
            // Find GroupId logic or just use flat timestamped folder
            var batchName = $"Deleted_{DateTime.Now:yyyyMMdd_HHmmss}"; 
            var destFolder = Path.Combine(quarantinePath, batchName);
            Directory.CreateDirectory(destFolder);
            
            var destPath = Path.Combine(destFolder, Path.GetFileName(path));
            
            // ... Copy-Verify-Delete ...
        }
    });
}
```
*Refinement*: To properly group by `GroupId` in Quarantine, we might need to pass `FileGroup` objects + `SelectedFiles` list, OR just accept flattening for now (as per user/legacy constraint). 
*Current Decision*: Use `Flattening + Unique Rename` (existing logic) or simple `TimeStamp` folder to avoid collision.

---

## 2. Integration & Verification

### 2.1 `MainWindowViewModel.ExecuteDeleteAsync`

```csharp
// Logic:
// 1. Identify Target Scope (Line1, Line2, or Both based on Tab?)
//    - Safest: Check ALL loaded groups.
// 2. Collect `filesToDelete` list.
// 3. Confirm count.
// 4. Call Service.
// 5. Update UI (Refresh/Remove groups).
```

### 2.2 Verification Steps
1.  **Partial Deletion**: Check Main, Uncheck NIR. Click Delete. Verify Main matches moved, NIR matches stay.
2.  **Row Logic**: Click Row Box -> All Check. Uncheck one -> Row Indeterminate.
3.  **Cross-Row**: Check Item in Row A, Item in Row B. Click "Delete Selected". Verify both deleted. (Ensures we don't rely on `DataGrid.SelectedItem`).
