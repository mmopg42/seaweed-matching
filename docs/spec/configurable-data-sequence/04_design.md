---
Task: Configurable Data Sequence Settings
Created: 2025-12-15
Status: Draft
Depends On: 03_plan.md
---

# Configurable Data Sequence Settings - Detailed Design

## 1. Component Designs

### 1.1 DataSequenceSettings Class

#### Interface (from Plan)
```csharp
public class DataSequenceSettings
{
    public List<DataSequenceItem> Sequence { get; set; }
    public DataSequenceItem? GetByType(DataType type);
    public int GetExpectedDelay(DataType type);
    public int GetTolerance(DataType type);
    public List<DataType> GetOrderedTypes();
    public bool Validate(out List<string> errors);
}
```

#### Preconditions
- Sequence list must not be null
- Each DataSequenceItem must have unique Type and Order values

#### Postconditions
- GetOrderedTypes() returns list sorted by Order (ascending)
- Validate() returns true only if all validation rules pass

#### Detailed Logic (Pseudo-code)

**GetByType Method**:
```
function GetByType(type: DataType) -> DataSequenceItem?:
    if Sequence is null or empty:
        return null
    
    for each item in Sequence:
        if item.Type == type:
            return item
    
    return null
```

**GetExpectedDelay Method**:
```
function GetExpectedDelay(type: DataType) -> int:
    item = GetByType(type)
    
    if item is null:
        return 0  // Default: no delay
    
    return item.ExpectedDelaySeconds
```

**GetTolerance Method**:
```
function GetTolerance(type: DataType) -> int:
    item = GetByType(type)
    
    if item is null:
        return 10  // Default tolerance: 10 seconds
    
    return item.TimeToleranceSeconds
```

**GetOrderedTypes Method**:
```
function GetOrderedTypes() -> List<DataType>:
    if Sequence is null or empty:
        return empty list
    
    sorted = Sequence
        .Where(item => item.Enabled)
        .OrderBy(item => item.Order)
        .Select(item => item.Type)
        .ToList()
    
    return sorted
```

**Validate Method**:
```
function Validate(out errors: List<string>) -> bool:
    errors = new List<string>()
    
    // Rule 1: At least one enabled item
    if Sequence.Count(item => item.Enabled) == 0:
        errors.Add("At least one data type must be enabled")
    
    // Rule 2: Unique Order values
    orderCounts = Sequence.GroupBy(item => item.Order)
    for each group in orderCounts:
        if group.Count > 1:
            errors.Add($"Duplicate Order value: {group.Key}")
    
    // Rule 3: Unique Type values
    typeCounts = Sequence.GroupBy(item => item.Type)
    for each group in typeCounts:
        if group.Count > 1:
            errors.Add($"Duplicate DataType: {group.Key}")
    
    // Rule 4: Valid delays
    for each item in Sequence:
        if item.ExpectedDelaySeconds < 0:
            errors.Add($"{item.Type}: Delay must be >= 0")
    
    // Rule 5: Valid tolerances
    for each item in Sequence:
        if item.TimeToleranceSeconds < 1:
            errors.Add($"{item.Type}: Tolerance must be >= 1")
    
    return errors.Count == 0
```

#### State Management
- **Immutable after load**: Configuration loaded once at startup
- **Modified only through SettingsDialog**: User changes via UI
- **Saved to JSON**: Persisted in appsettings.json

#### Thread Safety
- **Read-only after initialization**: Safe for concurrent reads
- **Write protection**: Settings changed only on UI thread
- **No locking needed**: Singleton pattern, UI thread-only modifications

#### Error Handling

| Error Scenario | Detection | Handling |
|----------------|-----------|----------|
| Null Sequence | Check in each method | Return defaults (0, 10, empty list) |
| Invalid JSON | ConfigurationManager.Load | Load default preset |
| Validation failure | Validate() call | Show error dialog, reject save |

---

### 1.2 DataSequencePresets Class

#### Interface (from Plan)
```csharp
public static class DataSequencePresets
{
    public static DataSequenceSettings NormalFirst();
    public static DataSequenceSettings NirFirst();
    public static DataSequenceSettings CamerasFirst();
}
```

#### Detailed Logic (Pseudo-code)

**NormalFirst Preset**:
```
function NormalFirst() -> DataSequenceSettings:
    return new DataSequenceSettings {
        Sequence = [
            { Type: Normal, Order: 1, Delay: 0,  Tolerance: 5,  Enabled: true },
            { Type: NIR,    Order: 2, Delay: 1,  Tolerance: 10, Enabled: true },
            { Type: Cam1,   Order: 3, Delay: 5,  Tolerance: 15, Enabled: true },
            { Type: Cam2,   Order: 4, Delay: 6,  Tolerance: 15, Enabled: true },
            { Type: Cam3,   Order: 5, Delay: 7,  Tolerance: 15, Enabled: true },
            { Type: Cam4,   Order: 6, Delay: 8,  Tolerance: 15, Enabled: false },
            { Type: Cam5,   Order: 7, Delay: 9,  Tolerance: 15, Enabled: false },
            { Type: Cam6,   Order: 8, Delay: 10, Tolerance: 15, Enabled: false }
        ]
    }
```

**NirFirst Preset**:
```
function NirFirst() -> DataSequenceSettings:
    return new DataSequenceSettings {
        Sequence = [
            { Type: NIR,    Order: 1, Delay: 0,  Tolerance: 10, Enabled: true },
            { Type: Normal, Order: 2, Delay: 1,  Tolerance: 5,  Enabled: true },
            { Type: Cam1,   Order: 3, Delay: 5,  Tolerance: 15, Enabled: true },
            // ... (same pattern, NIR first)
        ]
    }
```

**CamerasFirst Preset**:
```
function CamerasFirst() -> DataSequenceSettings:
    return new DataSequenceSettings {
        Sequence = [
            { Type: Cam1,   Order: 1, Delay: 0,  Tolerance: 15, Enabled: true },
            { Type: Cam2,   Order: 2, Delay: 1,  Tolerance: 15, Enabled: true },
            { Type: Cam3,   Order: 3, Delay: 2,  Tolerance: 15, Enabled: true },
            { Type: Normal, Order: 4, Delay: 5,  Tolerance: 5,  Enabled: true },
            { Type: NIR,    Order: 5, Delay: 6,  Tolerance: 10, Enabled: true },
            // ... (cameras first)
        ]
    }
```

---

### 1.3 SettingsDialogViewModel Updates

#### New Properties
```csharp
public ObservableCollection<DataSequenceItemViewModel> SequenceItems { get; }
public string SelectedPreset { get; set; }
public ICommand ApplyPresetCommand { get; }
public DataSequenceItemViewModel? DraggedItem { get; set; }  // For drag-drop
public int DropTargetIndex { get; set; }  // For drag-drop
```

#### Detailed Logic (Pseudo-code)

**LoadSequenceSettings Method**:
```
function LoadSequenceSettings():
    SequenceItems.Clear()
    
    settings = _config.DataSequenceSettings
    for each item in settings.Sequence.OrderBy(x => x.Order):
        viewModel = new DataSequenceItemViewModel {
            Type = item.Type,
            Order = item.Order,
            ExpectedDelay = item.ExpectedDelaySeconds,
            Tolerance = item.TimeToleranceSeconds,
            Enabled = item.Enabled
        }
        SequenceItems.Add(viewModel)
```

**Drag-Drop Reordering Logic**:

> [!NOTE]
> Using drag-drop provides better UX than MoveUp/Down buttons for reordering.

**OnItemDragStart** (Mouse down + move):
```
function OnItemDragStart(item: DataSequenceItemViewModel, mouseEventArgs):
    DraggedItem = item
    
    // Create drag data
    dragData = new DataObject()
    dragData.SetData("SequenceItem", item)
    
    // Start drag operation
    DragDrop.DoDragDrop(
        source: mouseEventArgs.Source,
        data: dragData,
        allowedEffects: DragDropEffects.Move)
    
    DraggedItem = null  // Clear after drop
```

**OnItemDragOver** (While dragging over other items):
```
function OnItemDragOver(targetItem: DataSequenceItemViewModel, dragEventArgs):
    // Validate drag source
    if !dragEventArgs.Data.GetDataPresent("SequenceItem"):
        dragEventArgs.Effects = DragDropEffects.None
        return
    
    draggedItem = dragEventArgs.Data.GetData("SequenceItem") as DataSequenceItemViewModel
    
    if draggedItem == null or draggedItem == targetItem:
        dragEventArgs.Effects = DragDropEffects.None
        return
    
    // Allow move
    dragEventArgs.Effects = DragDropEffects.Move
    
    // Visual feedback: highlight drop target
    DropTargetIndex = SequenceItems.IndexOf(targetItem)
    OnPropertyChanged(nameof(DropTargetIndex))
```

**OnItemDrop** (Item dropped):
```
function OnItemDrop(targetItem: DataSequenceItemViewModel, dragEventArgs):
    draggedItem = dragEventArgs.Data.GetData("SequenceItem") as DataSequenceItemViewModel
    
    if draggedItem == null or draggedItem == targetItem:
        return
    
    // Get indices
    oldIndex = SequenceItems.IndexOf(draggedItem)
    newIndex = SequenceItems.IndexOf(targetItem)
    
    // Move item in collection
    SequenceItems.Move(oldIndex, newIndex)
    
    // Reassign Order values to match new positions
    for i = 0 to SequenceItems.Count - 1:
        SequenceItems[i].Order = i + 1  // 1-based ordering
    
    // Clear drop target highlight
    DropTargetIndex = -1
    OnPropertyChanged(nameof(DropTargetIndex))
    
    _logger.LogDebug("Reordered: {0} from index {1} to {2}", 
        draggedItem.Type, oldIndex, newIndex)
```

**XAML Binding** (in SettingsDialog.xaml):
```xaml
<!-- ListBox with drag-drop enabled -->
<ListBox ItemsSource="{Binding SequenceItems}"
         AllowDrop="True">
    <ListBox.ItemContainerStyle>
        <Style TargetType="ListBoxItem">
            <!-- Attach drag-drop event handlers -->
            <EventSetter Event="PreviewMouseLeftButtonDown" Handler="Item_PreviewMouseDown"/>
            <EventSetter Event="MouseMove" Handler="Item_MouseMove"/>
            <EventSetter Event="DragOver" Handler="Item_DragOver"/>
            <EventSetter Event="Drop" Handler="Item_Drop"/>
            
            <!-- Visual feedback for drop target -->
            <Style.Triggers>
                <DataTrigger Binding="{Binding IsDropTarget}" Value="True">
                    <Setter Property="Background" Value="#e0e0e0"/>
                    <Setter Property="BorderBrush" Value="#0078d4"/>
                    <Setter Property="BorderThickness" Value="2"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </ListBox.ItemContainerStyle>
</ListBox>
```

**ApplyPresetCommand Logic**:
```
function ApplyPreset():
    preset = GetPresetByName(SelectedPreset)
    if preset is null:
        return
    
    // Confirm with user if changes exist
    if HasUnsavedChanges():
        result = MessageBox.Show("Apply preset? Current changes will be lost.", 
                                  "Confirm", YesNo)
        if result != Yes:
            return
    
    // Load preset
    SequenceItems.Clear()
    for each item in preset.Sequence:
        viewModel = CreateViewModel(item)
        SequenceItems.Add(viewModel)
    
    OnPropertyChanged(nameof(SequenceItems))
```

**SaveCommand Updates**:
```
function Save():
    // Existing save logic...
    
    // NEW: Save DataSequenceSettings
    sequenceSettings = new DataSequenceSettings {
        Sequence = SequenceItems.Select(vm => new DataSequenceItem {
            Type = vm.Type,
            Order = vm.Order,
            ExpectedDelaySeconds = vm.ExpectedDelay,
            TimeToleranceSeconds = vm.Tolerance,
            Enabled = vm.Enabled
        }).ToList()
    }
    
    // Validate
    if !sequenceSettings.Validate(out errors):
        MessageBox.Show("Validation failed:\n" + string.Join("\n", errors))
        return
    
    // Save to config
    _config.DataSequenceSettings = sequenceSettings
    _configManager.SaveConfiguration(_config)
    
    // Trigger app-wide settings changed event
    RaiseSettingsChangedEvent()
```

#### State Management
```
// Internal state
_originalSequence: List<DataSequenceItemViewModel>  // For cancel/reset

// State transitions
Initial → Loaded (from config) → Modified (user edits) → Saved (to config)

// Dirty tracking
function HasUnsavedChanges() -> bool:
    return !SequenceItems.SequenceEqual(_originalSequence)
```

---

### 1.4 MainWindow Dynamic Column Generation

#### Interface (from Plan)
```csharp
private void GenerateDataGridColumns(DataGrid dataGrid, int lineNumber)
```

#### Preconditions
- DataGrid must not be null
- LineNumber must be 1 or 2
- DataSequenceSettings must be loaded

#### Postconditions
- DataGrid.Columns contains only generated columns
- Column order matches DataSequenceSettings.Sequence
- Columns are filtered by lineNumber (Cam1-3 for Line 1, Cam4-6 for Line 2)

#### Detailed Logic (Pseudo-code)

```
function GenerateDataGridColumns(dataGrid: DataGrid, lineNumber: int):
    // Prevent UI flicker during column regeneration
    dataGrid.BeginInit()
    
    try:
        // 1. Clear existing columns
        dataGrid.Columns.Clear()
        
        // 2. Add static columns (Checkbox, Index, Status)
        AddCheckboxColumn(dataGrid)
        AddIndexColumn(dataGrid)
        AddStatusColumn(dataGrid)
        
        // 3. Get sequence configuration
        settings = _viewModel.Config.DataSequenceSettings
        orderedTypes = settings.GetOrderedTypes()
        
        // 4. Generate data columns
        for each dataType in orderedTypes:
            // Filter by line number
            if !ShouldShowForLine(dataType, lineNumber):
                continue
            
            column = CreateColumnForDataType(dataType)
            dataGrid.Columns.Add(column)
    finally:
        // Resume UI rendering (prevents flicker)
        dataGrid.EndInit()
```

**Helper: ShouldShowForLine**:
```
function ShouldShowForLine(dataType: DataType, lineNumber: int) -> bool:
    switch dataType:
        case Normal, NIR:
            return true  // Show on both lines
        case Cam1, Cam2, Cam3:
            return lineNumber == 1
        case Cam4, Cam5, Cam6:
            return lineNumber == 2
        default:
            return false
```

**Helper: CreateColumnForDataType**:
```
function CreateColumnForDataType(dataType: DataType) -> DataGridTemplateColumn:
    column = new DataGridTemplateColumn()
    column.Header = GetColumnHeader(dataType)
    column.Width = new DataGridLength(1, DataGridLengthUnitType.Star)
    
    // Create cell template
    template = new DataTemplate()
    
    // Border with image
    borderFactory = new FrameworkElementFactory(typeof(Border))
    borderFactory.SetValue(Border.BackgroundProperty, "#3a3a3a")
    borderFactory.SetValue(Border.BorderBrushProperty, "#505050")
    borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1))
    
    // Bind width/height from ViewModel
    if dataType == NIR:
        borderFactory.SetBinding(Border.WidthProperty, "NirDisplayWidth")
        borderFactory.SetBinding(Border.HeightProperty, "NirDisplayHeight")
    else:
        borderFactory.SetBinding(Border.WidthProperty, "DisplayImageWidth")
        borderFactory.SetBinding(Border.HeightProperty, "DisplayImageHeight")
    
    // Grid inside border
    gridFactory = new FrameworkElementFactory(typeof(Grid))
    
    // Image
    imageFactory = new FrameworkElementFactory(typeof(Image))
    imageFactory.SetBinding(Image.SourceProperty, GetBindingPath(dataType))
    imageFactory.SetValue(Image.StretchProperty, Stretch.Uniform)
    
    // ProgressBar (loading indicator)
    progressFactory = new FrameworkElementFactory(typeof(ProgressBar))
    progressFactory.SetValue(ProgressBar.IsIndeterminateProperty, true)
    progressFactory.SetValue(ProgressBar.HeightProperty, 4.0)
    progressFactory.SetBinding(ProgressBar.VisibilityProperty, 
        new Binding(GetBindingPath(dataType)) { 
            Converter = NullToVisibilityConverter 
        })
    
    // Assemble hierarchy
    gridFactory.AppendChild(imageFactory)
    gridFactory.AppendChild(progressFactory)
    borderFactory.AppendChild(gridFactory)
    template.VisualTree = borderFactory
    
    column.CellTemplate = template
    return column
```

**Helper: GetColumnHeader**:
```
function GetColumnHeader(dataType: DataType) -> string:
    // TODO: Move to Resources.resx for localization support
    // Future: Resources.GetString($"Column_{dataType}")
    
    switch dataType:
        case Normal: return "Main Img"  // TODO: Resources.Column_Normal
        case NIR: return "NIR Graph"    // TODO: Resources.Column_NIR
        case Cam1: return "Cam 1"       // TODO: Resources.Column_Cam1
        case Cam2: return "Cam 2"       // TODO: Resources.Column_Cam2
        case Cam3: return "Cam 3"       // TODO: Resources.Column_Cam3
        case Cam4: return "Cam 4"       // TODO: Resources.Column_Cam4
        case Cam5: return "Cam 5"       // TODO: Resources.Column_Cam5
        case Cam6: return "Cam 6"       // TODO: Resources.Column_Cam6
        default: return dataType.ToString()
```

> [!NOTE]
> **Localization Plan**:
> - Create `Resources.resx` (default, English)
> - Create `Resources.ko.resx` (Korean)
> - Replace hardcoded strings with `Resources.GetString(key)`
> - Apply to: Column headers, preset names, validation messages, dialog text

**Helper: GetBindingPath**:
```
function GetBindingPath(dataType: DataType) -> string:
    switch dataType:
        case Normal: return "MainImageThumbnail"
        case NIR: return "NirGraphThumbnail"
        case Cam1: return "Camera1Thumbnail"
        case Cam2: return "Camera2Thumbnail"
        case Cam3: return "Camera3Thumbnail"
        case Cam4: return "Camera4Thumbnail"
        case Cam5: return "Camera5Thumbnail"
        case Cam6: return "Camera6Thumbnail"
        default: return ""
```

#### Calling Points
```
// 1. Window_Loaded event
function Window_Loaded(sender, e):
    GenerateDataGridColumns(_line1DataGrid, lineNumber: 1)
    GenerateDataGridColumns(_line2DataGrid, lineNumber: 2)
    GenerateDataGridColumns(_combinedLine1DataGrid, lineNumber: 1)
    GenerateDataGridColumns(_combinedLine2DataGrid, lineNumber: 2)

// 2. Settings changed event
function OnSettingsChanged(sender, e):
    // Regenerate all columns
    GenerateDataGridColumns(_line1DataGrid, lineNumber: 1)
    GenerateDataGridColumns(_line2DataGrid, lineNumber: 2)
    // ... (same for combined view)
```

#### Thread Safety
- **UI Thread Only**: Must be called from Dispatcher thread
- **No locking needed**: DataGrid operations are thread-affine

#### Error Handling

| Error Scenario | Detection | Handling | Recovery |
|----------------|-----------|----------|----------|
| Null DataGrid | Argument check | Guard clause, early return | Skip column generation |
| Empty sequence | Check Count | Use default preset | Fall back to static columns |
| Invalid binding path | Runtime WPF error | Log warning | Column shows blank |

---

### 1.5 MonitoringOrchestrator Timestamp Matching

#### Updated Method
```csharp
private FileGroup? FindMatchingExistingGroup(FileGroup newGroup)
```

#### Preconditions
- newGroup must not be null
- _activeGroups dictionary initialized
- DataSequenceSettings loaded

#### Postconditions
- Returns existing group if match found
- Returns null if no match
- Does not modify any groups

#### Detailed Logic (Pseudo-code)

```
function FindMatchingExistingGroup(newGroup: FileGroup) -> FileGroup?:
    lock (_lockObject):
        _logger.LogDebug("FindMatchingExistingGroup: NormalFolder={0}, NirKey={1}, Timestamp={2}",
            newGroup.NormalFolder ?? "null",
            newGroup.NirKey ?? "null",
            newGroup.Timestamp)
        
        // ============================================================
        // Match 1: By NormalFolder (exact)
        // ============================================================
        if newGroup.NormalFolder is not null and not empty:
            match = _activeGroups.Values
                .FirstOrDefault(g => g.NormalFolder == newGroup.NormalFolder)
            
            if match is not null:
                _logger.LogInformation("Match found by NormalFolder: {0}", match.GroupId)
                return match
        
        // ============================================================
        // Match 2: By NirKey (exact)
        // ============================================================
        if newGroup.NirKey is not null and not empty:
            match = _activeGroups.Values
                .FirstOrDefault(g => g.NirKey == newGroup.NirKey)
            
            if match is not null:
                _logger.LogInformation("Match found by NirKey: {0}", match.GroupId)
                return match
        
        // ============================================================
        // Match 3: By Timestamp + LineNumber + DataType Priority (NEW)
        // ============================================================
        if newGroup.Timestamp != DateTime.MinValue:
            // Get sequence configuration
            sequenceSettings = _config.DataSequenceSettings
            orderedTypes = sequenceSettings.GetOrderedTypes()
            
            // Determine newGroup's DataType
            newGroupType = DetermineDataTypeForGroup(newGroup)
            
            // Search in priority order (earlier types first)
            for each dataType in orderedTypes:
                // Skip types with lower or equal priority
                if GetPriority(dataType) >= GetPriority(newGroupType):
                    continue
                
                // Get tolerance for this type
                tolerance = sequenceSettings.GetTolerance(dataType)
                toleranceWindow = TimeSpan.FromSeconds(tolerance)
                
                // Find candidates
                candidates = _activeGroups.Values
                    .Where(g => g.LineNumber == newGroup.LineNumber)
                    .Where(g => HasDataType(g, dataType))
                    .Where(g => Math.Abs((g.Timestamp - newGroup.Timestamp).TotalSeconds) <= tolerance)
                    .ToList()
                
                if candidates.Count > 0:
                    // Pick closest timestamp
                    closest = candidates
                        .OrderBy(g => Math.Abs((g.Timestamp - newGroup.Timestamp).TotalSeconds))
                        .First()
                    
                    _logger.LogInformation("Match found by Timestamp: {0} (Type={1}, Δ={2:F1}s)",
                        closest.GroupId,
                        dataType,
                        (newGroup.Timestamp - closest.Timestamp).TotalSeconds)
                    
                    return closest
        
        // No match found
        _logger.LogInformation("No match found - will create new group")
        return null
```

**Helper: DetermineDataTypeForGroup**:
```
function DetermineDataTypeForGroup(group: FileGroup) -> DataType:
    if group.HasNir:
        return DataType.NIR
    else if group.NormalFolder is not null:
        return DataType.Normal
    else if group.CameraFiles.ContainsKey("1"):
        return DataType.Cam1
    else if group.CameraFiles.ContainsKey("2"):
        return DataType.Cam2
    // ... etc
    else:
        return DataType.Normal  // Default
```

**Helper: HasDataType**:
```
function HasDataType(group: FileGroup, dataType: DataType) -> bool:
    switch dataType:
        case Normal:
            return group.NormalFolder is not null
        case NIR:
            return group.HasNir
        case Cam1:
            return group.CameraFiles.ContainsKey("1")
        case Cam2:
            return group.CameraFiles.ContainsKey("2")
        // ... etc
        default:
            return false
```

**Helper: GetPriority**:
```
function GetPriority(dataType: DataType) -> int:
    sequenceSettings = _config.DataSequenceSettings
    item = sequenceSettings.GetByType(dataType)
    
    if item is null:
        return int.MaxValue  // Lowest priority
    
    return item.Order  // Lower Order = Higher Priority
```

#### Thread Safety
- **Locked**: Entire method protected by `_lockObject`
- **Read-only access**: Only reads _activeGroups, does not modify
- **Safe for concurrent calls**: Lock ensures serialization

#### Error Handling

| Error Scenario | Detection | Handling | Recovery |
|----------------|-----------|----------|----------|
| Null timestamp | Check MinValue | Skip Match 3 | Fall through to "No match" |
| Empty sequence | Check Count | Use defaults | Fallback tolerance: 10s |
| Multiple matches | Count > 1 | Pick closest timestamp | Log warning |
| Negative time delta | Comparison | Allow (abs value) | Match regardless of direction |

---

## 2. Edge Cases & Boundary Conditions

### 2.1 Configuration Edge Cases

| Case | Input | Expected Behavior | Handling |
|------|-------|-------------------|----------|
| Empty Sequence | `Sequence = []` | Validation fails | Show error, prevent save |
| All Disabled | All `Enabled=false` | Validation fails | Show error "At least one required" |
| Duplicate Orders | Order 1, 1, 2 | Validation fails | Show error "Duplicate Order: 1" |
| Negative Delay | `Delay=-1` | Validation fails | Show error "{Type}: Delay >= 0" |
| Zero Tolerance | `Tolerance=0` | Validation fails | Show error "{Type}: Tolerance >= 1" |
| Missing Types | Only Normal + NIR | Valid | Cameras disabled/hidden |

### 2.2 Real-time Matching Edge Cases

| Case | Scenario | Expected Behavior | Handling |
|------|----------|-------------------|----------|
| Exact same timestamp | Two files at T=10:30:00 | Match to existing | Tolerance window includes T±0 |
| Time goes backwards | File2 arrives before File1 | Match using abs(Δ) | Use absolute time difference |
| Multiple matches in window | 3 groups at T±5s | Pick closest | OrderBy(abs time diff).First() |
| No enabled types | All types disabled | No Match 3 | Skip timestamp matching |
| NIR arrives before Normal | NIR T=0, Normal T=1 | Match NIR to Normal | Priority: NIR (Order=1) < Normal (Order=2) |

### 2.3 UI Edge Cases

| Case | User Action | Expected Behavior | Handling |
|------|-------------|-------------------|----------|
| Drag item onto itself | Drag Normal onto Normal | Nothing | Check draggedItem == targetItem |
| Drag to invalid target | Drag outside ListBox | Cancel drag | DragEffects.None |
| Drag multiple items | Select 2+ items, drag | Not supported | Single-item drag only |
| Apply preset with unsaved changes | Select preset, changes exist | Confirm dialog | MessageBox.Show with Yes/No |
| Cancel after changes | Click Cancel | Discard changes | Reload from _originalSequence |
| Invalid delay input | Type "abc" | Prevent input | TextBox input validation |
| Extremely large tolerance | Tolerance=9999 | Allow but warn | Show warning "Large tolerance" |
| Rapid reordering | Drag multiple times quickly | All changes tracked | Order recalculated each drop |

### 2.4 Column Generation Edge Cases

| Case | Configuration | Expected Behavior | Handling |
|------|---------------|-------------------|----------|
| No enabled types | All disabled | Show static columns only | Check Count > 0 before loop |
| Line mismatch | Try to show Cam4 on Line 1 | Skip column | ShouldShowForLine filter |
| Missing binding path | Invalid DataType enum | Blank column | GetBindingPath returns "" |
| DataGrid already has columns | Regenerate after settings change | Clear first, then add | dataGrid.Columns.Clear() |

---

## 3. Resource Management

### 3.1 Initialization
```
// Application startup
ConfigurationManager.LoadConfiguration()
  → Deserialize appsettings.json
  → Populate ApplicationConfiguration
  → DataSequenceSettings loaded

// Settings Dialog opened
SettingsDialogViewModel.Constructor()
  → LoadSequenceSettings()
  → Create ObservableCollection
  → Clone to _originalSequence (for cancel)

// Main Window opened
Window_Loaded()
  → GenerateDataGridColumns (x4 DataGrids)
  → Subscribe to SettingsChanged event
```

### 3.2 Cleanup
```
// Settings Dialog closed
SettingsDialog.Closed()
  → Dispose ViewModels
  → Clear ObservableCollection
  → Release _originalSequence

// Application shutdown
App.OnExit()
  → Dispose ServiceProvider
  → No explicit cleanup needed (GC handles)
```

### 3.3 Memory Considerations
- **ObservableCollection**: Small (max 8 items), negligible impact
- **DataGrid Columns**: Regenerated on demand, old columns GC'd
- **Preset objects**: Static methods, no persistent instances

---

## 4. Concurrency Considerations

### 4.1 Thread-Safe Components

| Component | Access Pattern | Thread Safety Strategy |
|-----------|----------------|------------------------|
| DataSequenceSettings | Read-mostly | Singleton, UI thread writes only |
| SettingsDialogViewModel | UI-bound | WPF data binding (UI thread) |
| MonitoringOrchestrator | Multi-threaded | Lock _lockObject for reads/writes |
| MainWindow columns | UI-bound | Dispatcher.Invoke if needed |

### 4.2 Race Conditions

**Scenario 1: Settings changed while matching in progress**
- **Problem**: Tolerance value changes mid-match
- **Solution**: MonitoringOrchestrator  locks `_lockObject` for entire method
- **Result**: Settings change waits until matching completes

**Scenario 2: Multiple DataGrids regenerating simultaneously**
- **Problem**: UI thread busy
- **Solution**: All on UI thread (sequential)
- **Result**: No concurrency issue

### 4.3 Atomic Operations
```
// Settings save is atomic
lock (_configLock):
    ValidateSettings()
    SaveToFile()
    NotifyChanged()

// No partial state visible to other threads
```

---

## 5. Error Recovery Strategies

### 5.1 Configuration Load Failures

**Scenario**: appsettings.json corrupted or missing

```
Recovery Strategy:
1. Detect: ConfigurationManager catches JsonException
2. Log: Write to error log with exception details
3. Fallback: Load DataSequencePresets.NormalFirst()
4. Notify: Show warning toast to user
5. Continue: Application runs with default config
```

### 5.2 Validation Failures

**Scenario**: User tries to save invalid sequence

```
Recovery Strategy:
1. Detect: DataSequenceSettings.Validate() returns false
2. Display: MessageBox with all validation errors
3. Reject: Do not save to config
4. Retain: Keep dialog open with current values
5. Allow Fix: User can correct and retry
```

### 5.3 Column Generation Failures

**Scenario**: Exception during dynamic column creation

```
Recovery Strategy:
1. Detect: Try-catch around GenerateDataGridColumns
2. Log: Exception details to logger
3. Fallback: Keep existing columns (don't clear)
4. Notify: Status bar message "Column generation failed"
5. Degrade: Static columns remain visible, app continues
```

---

## 6. Performance Considerations

### 6.1 Bottlenecks

| Operation | Complexity | Performance Impact |
|-----------|------------|-------------------|
| GetOrderedTypes | O(n log n) | Negligible (n ≤ 8) |
| FindMatchingExistingGroup | O(m) | Low (m = active groups, typically < 100) |
| GenerateDataGridColumns | O(n) | Low (n ≤ 8), UI thread only |
| Settings save | O(1) | Low (JSON serialization) |

### 6.2 Optimization Strategies

**Cache ordered types**:
```
// Instead of calling GetOrderedTypes() repeatedly
_orderedTypesCache = settings.GetOrderedTypes()
// Invalidate on settings change
```

**Lazy column generation**:
```
// Generate columns only when tab is activated
TabControl.SelectionChanged += (s, e) =>
{
    if (e.NewValue == DataSequenceTab)
        GenerateColumnsIfNeeded()
}
```

### 6.3 Acceptable Limits
- Max sequence items: 8 (all data types)
- Max active groups for matching: 1000 (linear search acceptable)
- Column generation time: < 100ms (not noticeable)

---

## 7. Testing Hooks

### 7.1 Testable Interfaces

```csharp
// Inject configuration for testing
public SettingsDialogViewModel(IConfigurationManager configManager)

// Expose validation for testing
public bool Validate(out List<string> errors)

// Testable matching logic
internal FileGroup? FindMatchingExistingGroup(FileGroup newGroup)
```

### 7.2 Mock Points

- `IConfigurationManager`: Mock for testing save/load
- `DataSequenceSettings`: Create test instances with known sequences
- `FileGroup`: Create test groups with specific timestamps

### 7.3 Integration Test Scenarios

1. **End-to-End Settings Flow**:
   - Load config → Modify sequence → Save → Reload app → Verify persisted

2. **Dynamic Column Generation**:
   - Configure sequence → Open MainWindow → Verify column order

3. **Timestamp Matching**:
   - Create NIR group (T=0) → Create Normal file (T=1) → Verify single group

---

**Status**: [ ] Ready for approval
**Next Step**: User approval → Create 05_tasks.md
