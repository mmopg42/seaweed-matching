# Design: Camera Comparison Option

## 1. System Architecture
This feature modifies the file matching logic within the Core layer and exposes a configuration option in the UI layer. It follows the existing MVVM and Service patterns.

## 2. Component Design

### 2.1 Data Model
#### `ChronoView.Models.DataSequenceSettings`
Add a new property to persist the configuration.

```csharp
public class DataSequenceSettings
{
    // ... existing code ...
    
    /// <summary>
    /// If true, Cam2/Cam3 use Cam1 (and Cam5/6 use Cam4) as the timestamp reference
    /// instead of the immediately preceding sequence item.
    /// </summary>
    public bool CompareToReferenceCamera { get; set; } = false;
}
```

### 2.2 UI Layer
#### `ChronoView.UI.ViewModels.SettingsDialogViewModel`
Expose the setting to the View.

```csharp
public bool CompareToReferenceCamera
{
    get => _configuration.DataSequenceSettings.CompareToReferenceCamera;
    set
    {
        if (_configuration.DataSequenceSettings.CompareToReferenceCamera != value)
        {
            _configuration.DataSequenceSettings.CompareToReferenceCamera = value;
            OnPropertyChanged();
        }
    }
}
```

#### `ChronoView.UI.Views.SettingsDialog.xaml`
Add a CheckBox to the `TabItem` for Sequence Settings.

```xml
<CheckBox Content="Compare Cam2/3 (or Cam5/6) timestamps directly to Cam1 (or Cam4)" 
          IsChecked="{Binding CompareToReferenceCamera}"
          ToolTip="If checked, secondary cameras are compared to the first camera instead of the previous sequence item."/>
```

### 2.3 Core Logic Detailed Design
#### `ChronoView.Core.FileMatching.FileMatchingEngine`
The modification targets the `BuildLineGroupsWithOrder` method.

**Context**: 
The method iterates through `orderedTypes` (the configured sequence). Historically it identified the predecessor by `Order == order - 1`, but this is fragile when there are gaps in order values. This design uses an index-based lookup: the fallback reference is the nearest enabled preceding item in the ordered list.

**Implementation Logic**:

1.  **Iterate Sequence**: Loop through `orderedTypes`.
2.  **Identify Target Reference Type**:
    -   If `CompareToReferenceCamera` is true AND current type is Cam2/3 (or Cam5/6):
        -   Target = `DataType.Cam1` (or `Cam4`).
    -   Else (or if Target not found/enabled):
        -   Fallback to **Nearest Preceding Item** in `orderedTypes` list (Index - 1).
3.  **Group Matching**:
    -   Iterate through existing `groups`.
    -   Check if group contains the `TargetReferenceType` using `HasDataTypeInGroup`.
    -   If yes, calculate `diff` = `currentFile.Timestamp - group.Timestamp`.
    -   Validate using `Min/Max Delay`.

**Code Structure**:
```csharp
foreach (var item in orderedTypes)
{
    // A. Reference Selection Logic
    DataType? targetRefType = null;

    if (dataSequenceSettings.CompareToReferenceCamera)
    {
        // Reference logic applies to Cam2 and Cam3 types.
        // Since Line 2 files are mapped to Cam1/2/3 DataTypes internally, 
        // this logic automatically covers generic "Cam4/5/6" logic as well.
        if (item.Type == DataType.Cam2 || item.Type == DataType.Cam3)
        {
            targetRefType = DataType.Cam1;
        }
        // Note: Cam5/6 are handled because they are mapped to Cam2/3 types in this context.
    }

    // B. Fallback / Standard Logic (Nearest Preceding Item)
    if (targetRefType == null)
    {
        // Find nearest preceding item in the ordered list that is enabled
        // Loop backwards from current item index
        int currentIndex = orderedTypes.IndexOf(item);
        if (currentIndex > 0)
        {
            targetRefType = orderedTypes[currentIndex - 1].Type;
        }
    }

    // C. Perform Lookup in Groups
    if (targetRefType.HasValue)
    {
        // ... (Existing Logic: Find closest group with targetRefType) ...
        foreach (var g in groups)
        {
             if (!HasDataTypeInGroup(g, targetRefType.Value)) continue;
             // ... Calculate diff and Match ...
        }
    }
}
```

**Note**: The engine iterates `groups`, not a dictionary of files. The lookup logic matches the file against candidate groups that contain the `targetRefType`.

## 3. Data Flow

### Scenario: Option Enabled, Cam2 arrives
1.  **Input**: Processing `Cam2`. Existing `groups` contains a group with `Cam1`.
2.  **Check**: `CompareToReferenceCamera` is true.
3.  **Target**: Cam2 maps to `Cam1`.
4.  **Action**: Iterate `groups`.
5.  **Match**: Found group where `HasDataTypeInGroup(Cam1)` is true.
6.  **Calc**: `Diff = Cam2.Time - Group.Timestamp`.
7.  **Validate**: Check matching settings.

### Scenario: Option Enabled, Cam1 Missing
1.  **Input**: Processing `Cam2`. Cam1 is disabled/missing in `orderedTypes`.
2.  **Check**: `CompareToReferenceCamera` is true.
3.  **Target**: Cam2 maps to `Cam1`.
4.  **Fallback**: Cam1 not found in `orderedTypes`. Code searches backwards from Cam2's index.
5.  **Resolution**: Finds `Normal` (or closest enabled item). New Target = `Normal`.
6.  **Action**: Iterate `groups`.
7.  **Match**: Found group where `HasDataTypeInGroup(Normal)` is true.
8.  **Calc**: `Diff = Cam2.Time - Group.Timestamp`.
9.  **Validate**: Check settings.

## 4. Security & Performance
-   **Performance**: Overhead is negligible (dictionary lookup).
-   **Safety**: Fallback mechanism ensures robustness against configuration mismatches (e.g. enabling option but removing Cam1 from sequence).

## 5. Implementation Steps
1.  **Models**: Update `DataSequenceSettings.cs`.
2.  **ViewModels**: Update `SettingsDialogViewModel.cs`.
3.  **Views**: Update `SettingsDialog.xaml`.
4.  **Core**: Update `FileMatchingEngine.cs` logic.
5.  **Tests**: Add unit tests covering the scenarios defined in Plan.
