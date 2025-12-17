---
Task: Configurable Data Sequence Settings
Created: 2025-12-15
Status: Complete
Depends On: 01_requirements.md
---

# Configurable Data Sequence Settings - Research Findings

## 1. Investigation Summary

Investigated existing codebase to identify modification points for implementing configurable data arrival sequence. Analyzed configuration models, UI components, and matching logic.

## 2. Question Answers

### Q1: What is the current configuration structure?
**Method**: Reviewed `ApplicationConfiguration.cs`
**Findings**: 
- Configuration organized into nested classes: `MatchingSettings`, `UISettings`, `WorkflowSettings`, `WindowSettings`, `ImageSettings`
- `MatchingSettings` contains time windows but NO data sequence configuration
- Fixed time windows: `NirTimeWindowSeconds=300`, `CameraTimeWindowSeconds=2`
- No column order configuration

**Conclusion**: Need to add new `DataSequenceSettings` class

**Evidence**: `ChronoView/Models/ApplicationConfiguration.cs` Lines 27-42

---

### Q2: How is SettingsDialog structured?
**Method**: Reviewed `SettingsDialog.xaml`
**Findings**:
- TabControl with 3 tabs: "Paths", "Advanced", "UI Options"
- Tab content uses ScrollViewer + StackPanel layout
- Settings bound to `SettingsDialogViewModel` properties
- No drag-drop UI components currently

**Conclusion**: Need to add 4th tab "Data Sequence" and implement reorderable list control

**Evidence**: `ChronoView/UI/Views/SettingsDialog.xaml` Lines 33-219

---

### Q3: How are DataGrid columns defined?
**Method**: Analyzed `MainWindow.xaml`
**Findings**:
- **Hardcoded column order** in XAML:
  - Line 1: Main Img → NIR Graph → Cam1 → Cam2 → Cam3
  - Line 2: Main Img → NIR Graph → Cam4 → Cam5 → Cam6
- Columns defined as `DataGridTemplateColumn` with static headers
- No dynamic column generation
- Column visibility not configurable

**Conclusion**: Need to **generate columns dynamically** from `DataSequenceSettings`

**Evidence**: `ChronoView/MainWindow.xaml` Lines 381-472 (Line 1), Lines 486-577 (Line 2)

---

### Q4: Where is real-time matching implemented?
**Method**: Previously analyzed `MonitoringOrchestrator.cs`
**Findings**:
- `FindMatchingExistingGroup` uses NormalFolder and NirKey for matching
- **NO timestamp-based fallback** currently ← This is the root cause of the issue!
- Matching logic: exact string match only

**Conclusion**: Need to add timestamp-based matching with configurable tolerances

**Evidence**: `MonitoringOrchestrator.cs` Lines 657-700

---

### Q5: What ViewModel properties exist for UI settings?
**Method**: Searched for `SettingsDialogViewModel`
**Findings**: Need to view this file to understand property binding

**Next Step**: View `SettingsDialogViewModel.cs`

---

## 3. Code Analysis Results

### Relevant Existing Code

| File | Function/Class | Relevance |
|------|----------------|-----------|
| `ApplicationConfiguration.cs` | `MatchingSettings` class | Contains time windows, need to add sequence settings |
| `SettingsDialog.xaml` | TabControl structure | Need to add new tab for sequence |
| `MainWindow.xaml` | DataGrid column definitions | Need to make dynamic instead of static |
| `MonitoringOrchestrator.cs` | `FindMatchingExistingGroup` | Need to add timestamp fallback |

### Glossary Check

**Existing terms**:
- `NirKey`, `NormalFolder`, `FileType`, `LineNumber`
- `MatchingSettings`, `UISettings`

**New terms needed**:
- `DataSequenceSettings`
- `DataSequenceItem` / `SequenceEntry`
- `DataType` enum (NIR, Normal, Cam1-6)

---

## 4. Recommendations

### 4.1 Configuration Model

**Create**:
```csharp
// ChronoView/Models/DataSequenceSettings.cs
public class DataSequenceSettings
{
    public List<DataSequenceItem> Sequence { get; set; } = new();
}

public class DataSequenceItem
{
    public DataType Type { get; set; }
    public int Order { get; set; }
    public int ExpectedDelaySeconds { get; set; }
    public int TimeToleranceSeconds { get; set; }
    public bool Enabled { get; set; } = true;
}

public enum DataType
{
    NIR,
    Normal,
    Cam1,
    Cam2,
    Cam3,
    Cam4,
    Cam5,
    Cam6
}
```

**Update**: `ApplicationConfiguration.cs` add property:
```csharp
public DataSequenceSettings DataSequenceSettings { get; set; } = new();
```

---

### 4.2 Settings Dialog

**Create**:
- `ChronoView/UI/ViewModels/SettingsDialogViewModel.cs` - Add properties for sequence settings
- `ChronoView/UI/Controls/ReorderableListControl.xaml` - Drag-drop list for sequence

**Update**: `SettingsDialog.xaml` - Add 4th tab "Data Sequence"

---

### 4.3 Main Window

**Update**: `MainWindow.xaml`
- Remove static column definitions
- Add `ItemsControl` with dynamic `DataGridColumn` generation
- Bind to `DataSequenceSettings.Sequence`

**Create**: 
- `MainWindowViewModel.GenerateDataGridColumns()` method
- Column visibility logic

---

### 4.4 Real-time Matching

**Update**: `MonitoringOrchestrator.FindMatchingExistingGroup`
- Add Match 3: Timestamp + LineNumber fallback
- Use `DataSequenceSettings` tolerances
- Priority-based matching (earlier types first)

---

## 5. Unanswered Questions

- [x] How are columns currently defined? → Static XAML, need dynamic generation
- [x] Where is matching logic? → `FindMatchingExistingGroup`, needs timestamp fallback
- [ ] How to implement drag-drop in WPF? → Need to research WPF drag-drop controls
- [ ] How to persist sequence order? → JSON serialization (already supported)

---

## 6. Implementation Complexity Assessment

| Component | Complexity | Reason |
|-----------|------------|--------|
| Configuration Model | Low | Simple POCO classes |
| Settings Dialog Tab | Medium | Need drag-drop UI control |
| Dynamic Column Generation | **High** | DataGrid columns must be generated in code-behind |
| Real-time Matching | Medium | Add timestamp fallback logic |
| Persistence | Low | Already have JSON config system |

**Highest Risk**: Dynamic DataGrid column generation in WPF

---

## 7. Next Steps

1. ✅ Configuration model design
2. ✅ Settings dialog layout design  
3. → Detailed implementation plan (03_plan.md)
4. → Code design with drag-drop control (04_design.md)

---

**Status**: ✅ Research Complete
**Next Document**: `03_plan.md` - Implementation Plan
