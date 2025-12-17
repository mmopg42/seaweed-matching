---
Task: Configurable Data Sequence Settings
Created: 2025-12-15
Status: Draft
Depends On: 01_requirements.md, 02_research.md
---

# Configurable Data Sequence Settings - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | How Addressed | Verification |
|-------------------|---------------|--------------|
| Users can specify data arrival order | `DataSequenceSettings` model + Settings UI | Manual test: reorder in UI |
| Users can configure time tolerance | Per-type `TimeToleranceSeconds` property | Test with different tolerance values |
| UI column order matches sequence | Dynamic column generation in `MainWindow` | Visual inspection |
| Settings persisted | JSON serialization in `appsettings.json` | App restart test |
| Real-time matching uses config | Updated `FindMatchingExistingGroup` | Test file arrival at different times |

**All criteria will be addressed.**

---

## 1. Architecture Overview

### 1.1 System Context Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    User Configuration                        │
│  (Settings Dialog → Data Sequence Tab)                      │
└────────────────────┬────────────────────────────────────────┘
                     │ Save
                     ▼
┌─────────────────────────────────────────────────────────────┐
│          ApplicationConfiguration.DataSequenceSettings      │
│  - Sequence: List<DataSequenceItem>                        │
│  - Default: Normal → NIR → Cam1 → Cam2 → Cam3              │
└────────┬────────────────────────────┬────────────────────────┘
         │                            │
         ▼                            ▼
┌──────────────────┐        ┌───────────────────────┐
│  MainWindow      │        │ MonitoringOrchestrator│
│  - Dynamic       │        │ - Timestamp-based     │
│    Columns       │        │   matching            │
└──────────────────┘        └───────────────────────┘
```

### 1.2 Data Flow

```
1. User configures sequence in Settings Dialog
   ↓
2. Saved to appsettings.json
   ↓
3. Loaded on app startup
   ↓
4. MainWindow generates DataGrid columns dynamically
   ↓
5. MonitoringOrchestrator uses sequence for matching priority
   ↓
6. Files matched and merged into groups
   ↓
7. UI displays in configured column order
```

---

## 2. Components

### 2.1 New Components

#### [NEW] DataSequenceSettings.cs
**Location**: `ChronoView/Models/DataSequenceSettings.cs`

**Purpose**: Configuration model for data sequence

**Responsibilities**:
- Store ordered list of data types with delay/tolerance settings
- Provide helper methods for querying sequence configuration
- Validate sequence integrity

**Key Properties**:
- `Sequence`: List of DataSequenceItem
- `GetByType()`, `GetExpectedDelay()`, `GetTolerance()`, `GetOrderedTypes()`

#### [NEW] DataSequencePresets.cs
**Location**: `ChronoView/Models/DataSequencePresets.cs`

**Purpose**: Predefined sequence configurations

**Presets**:
- `NormalFirst()` - Default (Normal → NIR → Cam1-3)
- `NirFirst()` - NIR-first workflow
- `CamerasFirst()` - Camera-first workflow

---

### 2.2 Modified Components

#### [MODIFY] ApplicationConfiguration.cs
**Location**: `ChronoView/Models/ApplicationConfiguration.cs`

**Changes**:
- Add property: `public DataSequenceSettings DataSequenceSettings { get; set; } = new();`
- Initialize with default preset in constructor

#### [MODIFY] SettingsDialog.xaml
**Location**: `ChronoView/UI/Views/SettingsDialog.xaml`

**Changes**:
- Add 4th TabItem: "Data Sequence" (데이터 순서)
- Reorderable list for sequence items
- Per-item edit controls (delay, tolerance)
- Preset dropdown

#### [MODIFY] SettingsDialogViewModel.cs
**Location**: `ChronoView/UI/ViewModels/SettingsDialogViewModel.cs`

**Changes**:
- Add `ObservableCollection<DataSequenceItemViewModel> SequenceItems`
- Add `SelectedPreset` property
- Add `MoveUpCommand`, `MoveDownCommand`, `EditItemCommand`
- Add `ApplyPresetCommand`

#### [MODIFY] MainWindow.xaml
**Location**: `ChronoView/MainWindow.xaml`

**Changes**:
- **Remove** static `<DataGrid.Columns>` definitions
- Use code-behind to generate columns dynamically
- Keep 3 DataGrids (Line 1, Line 2, Combined view)

#### [MODIFY] MainWindow.xaml.cs
**Location**: `ChronoView/MainWindow.xaml.cs`

**Changes**:
- Add `GenerateDataGridColumns(DataGrid dataGrid, int lineNumber)` method
- Call on Window_Loaded and when settings change
- Subscribe to settings changed event

#### [MODIFY] MonitoringOrchestrator.cs
**Location**: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs`

**Changes**:
- Inject `DataSequenceSettings` via constructor
- Update `FindMatchingExistingGroup`:
  - Add Match 3: Timestamp + LineNumber fallback
  - Use configured tolerances
  - Priority-based matching (earlier types first)

---

## 3. Interface Definitions

### 3.1 DataSequenceSettings Contracts

**Responsibilities**:
- Provide ordered access to sequence configuration
- Calculate delays and tolerances for file types
- Validate sequence consistency

**Key Methods**:
| Method | Input | Output | Purpose |
|--------|-------|--------|---------|
| `GetByType` | DataType | DataSequenceItem? | Find config for type |
| `GetExpectedDelay` | DataType | int (seconds) | Get delay setting |
| `GetTolerance` | DataType | int (seconds) | Get tolerance setting |
| `GetOrderedTypes` | - | List\<DataType\> | Get types in order |

### 3.2 UI Settings Dialog Contracts

**Data Sequence Tab Requirements**:
1. Display reorderable list of sequence items
2. Show delay/tolerance for each item
3. Provide preset selection
4. Allow per-item editing

**ViewModel Contracts**:
- `SequenceItems`: ObservableCollection for two-way binding
- `MoveUp/Down`: Commands for reordering
- `ApplyPreset`: Command to load preset configuration

### 3.3 Dynamic Column Generation Contracts

**MainWindow Requirements**:
1. Generate DataGrid columns from sequence configuration
2. Map DataType to column headers and bindings
3. Filter columns by line number (Cam1-3 vs Cam4-6)
4. Regenerate columns when settings change

**Column Mapping**:
| DataType | Column Header | Binding Property |
|----------|---------------|------------------|
| Normal | "Main Img" | MainImageThumbnail |
| NIR | "NIR Graph" | NirGraphThumbnail |
| Cam1 | "Cam 1" | Camera1Thumbnail |
| Cam2 | "Cam 2" | Camera2Thumbnail |
| (etc.) | (etc.) | (etc.) |

### 3.4 Real-time Matching Contracts

**Updated Matching Logic**:
1. **Match 1**: By NormalFolder (exact string match)
2. **Match 2**: By NirKey (exact string match)
3. **Match 3** (NEW): By Timestamp + LineNumber with priority
   - Use configured tolerances from DataSequenceSettings
   - Prioritize earlier types in sequence order
   - Match within timestamp ± tolerance window

---

## 4. Key Design Decisions

### 4.1 Dynamic Column Generation

**Problem**: XAML columns are static, need to change based on configuration

**Options**:
- [x] **Option A**: Generate in code-behind ← SELECTED
  - Pros: Full control, easy to reorder
  - Cons: More complex than XAML
  
- [ ] Option B: Use XAML with visibility binding
  - Pros: XAML declarative
  - Cons: Can't reorder, awkward

**Rationale**: Option A provides necessary flexibility for column reordering

---

### 4.2 Sequence Storage Format

**Problem**: How to store in JSON

**Options**:
- [x] **Option A**: Array of objects ← SELECTED
  ```json
  "Sequence": [
    {"Type": "Normal", "Order": 1, "ExpectedDelaySeconds": 0, "TimeToleranceSeconds": 5},
    {"Type": "NIR", "Order": 2, "ExpectedDelaySeconds": 1, "TimeToleranceSeconds": 10}
  ]
  ```
  - Pros: Explicit, clear
  - Cons: Verbose
  
- [ ] Option B: Compact array
  ```json
  "Sequence": ["Normal", "NIR", "Cam1", "Cam2", "Cam3"]
  ```
  - Pros: Concise
  - Cons: No delay/tolerance configuration

**Rationale**: Option A allows per-type delay/tolerance configuration

---

### 4.3 Timestamp Matching Priority

**Problem**: Which type should match first when multiple exist?

**Decision**: **Earlier types in configured sequence have priority**

**Example**:
```
Sequence: NIR (Order=1) → Normal (Order=2) → Cam1 (Order=3)

New Normal file arrives at T=10:30:15
1. Search for NIR groups (T±10s) ← Check Order=1 first
2. If NIR found, merge into NIR group
3. If not, search for Normal groups
4. If not, create new group
```

**Rationale**: Prevents duplicate groups when files arrive out of order

---

### 4.4 Per-Line vs Global Sequence

**Problem**: Should Line 1 and Line 2 have different sequences?

**Decision**: **Global sequence for now, easy to extend later**

**Rationale**:
- Simpler implementation
- Most production lines use same sequence
- Can add per-line config in future if needed

---

## 5. UI Design & Presets

### 5.1 Design Consistency Requirements

> [!IMPORTANT]
> **The Data Sequence tab MUST maintain visual consistency with the existing application design.**

**Theme & Color Scheme**:
- Follow existing color palette from `MainWindow.xaml`:
  - Background: `#f0f0f0` (BackgroundBrush)
  - Panel Background: `#f5f5f5` (PanelBackgroundBrush)
  - Borders: `#d0d0d0` (BorderBrush)
  - Selection: `#0078d4` (SelectionBrush)
  - Hover: `#e0e0e0` (HoverBrush)

**UI Style Consistency**:
- **Borders**: 1px solid `#d0d0d0`, no rounded corners (BorderThickness="1", CornerRadius="0")
- **Spacing**: Consistent margin/padding with existing tabs (Margin="10", Padding="5")
- **Fonts**: Use default system font, same sizes as existing dialogs
- **Controls**: Use standard WPF controls styled to match Paths/Advanced/UI Options tabs
- **Layout**: ScrollViewer + StackPanel pattern (same as other tabs)

**Component Styling**:
- TextBox, ComboBox, ListBox → Follow existing `SettingsDialog.xaml` styles
- Buttons → Use same dimensions (Width="80", Height="30")
- Labels → Same width alignment (Width="150")

**Reference Files**:
- `ChronoView/UI/Views/SettingsDialog.xaml` (Lines 1-222) - Existing tab styles
- `ChronoView/MainWindow.xaml` (Lines 24-31) - Color resources

---

### 5.2 Settings Dialog - Data Sequence Tab Mockup

```
┌───────────────────────────────────────────────────────────┐
│ Settings                                        [×]        │
├─────────────┬─────────────────────────────────────────────┤
│ Paths       │ Data Sequence Configuration                 │
│ Advanced    │                                             │
│ UI Options  │ Preset: [Normal First ▼] [Apply]           │
│► Data       │                                             │
│  Sequence   │ ┌─────────────────────────────────────┐   │
│             │ │ [≡] Normal | Delay: 0s  | Tol: ±5s │   │
│             │ │ [≡] NIR    | Delay: 1s  | Tol: ±10s│   │
│             │ │ [≡] Cam1   | Delay: 5s  | Tol: ±15s│   │
│             │ │ [≡] Cam2   | Delay: 6s  | Tol: ±15s│   │
│             │ │ [≡] Cam3   | Delay: 7s  | Tol: ±15s│   │
│             │ └─────────────────────────────────────┘   │
│             │                                             │
│             │ [≡] = Drag handle for reordering            │
│             │ Click row to edit delay/tolerance           │
│             │                                             │
│             │ Timeline Preview:                           │
│             │ ┌─────────────────────────────────────┐   │
│             │ │ Normal  NIR    Cam1  Cam2  Cam3     │   │
│             │ │   │      │      │     │     │        │   │
│             │ │   0s     1s     5s    6s    7s   →time│   │
│             │ │  ◄5s►  ◄10s►  ◄──15s──►              │   │
│             │ └─────────────────────────────────────┘   │
│             │ (Bars show tolerance windows)               │
│             │                                             │
│             │              [Reset] [Save] [Cancel]        │
└─────────────┴─────────────────────────────────────────────┘
```

### 5.3 Preset Examples

#### Preset 1: "Normal First" (Default)
```
Sequence: Normal (0s±5s) → NIR (1s±10s) → Cam1 (5s±15s) → Cam2 (6s±15s) → Cam3 (7s±15s)

UI Column Order:
┌────────┬──────────┬───────┬───────┬───────┐
│Main Img│NIR Graph │ Cam 1 │ Cam 2 │ Cam 3 │
└────────┴──────────┴───────┴───────┴───────┘
```

#### Preset 2: "NIR First"
```
Sequence: NIR (0s±10s) → Normal (1s±5s) → Cam1 (5s±15s) → Cam2 (6s±15s) → Cam3 (7s±15s)

UI Column Order:
┌──────────┬────────┬───────┬───────┬───────┐
│NIR Graph │Main Img│ Cam 1 │ Cam 2 │ Cam 3 │
└──────────┴────────┴───────┴───────┴───────┘
```

#### Preset 3: "Cameras First"
```
Sequence: Cam1 (0s±15s) → Cam2 (1s±15s) → Cam3 (2s±15s) → Normal (5s±5s) → NIR (6s±10s)

UI Column Order:
┌───────┬───────┬───────┬────────┬──────────┐
│ Cam 1 │ Cam 2 │ Cam 3 │Main Img│NIR Graph │
└───────┴───────┴───────┴────────┴──────────┘
```

### 5.4 Per-Item Edit Dialog Mockup

```
┌─────────────────────────────────┐
│ Edit Sequence Item             │
├─────────────────────────────────┤
│                                 │
│ Data Type: [Normal        ▼]   │
│                                 │
│ Expected Delay: [0     ] seconds│
│ (Time after previous item)      │
│                                 │
│ Time Tolerance: [5     ] seconds│
│ (Matching window: ±5s)          │
│                                 │
│ ☑ Enabled                       │
│                                 │
│          [OK]     [Cancel]      │
└─────────────────────────────────┘
```

---

## 6. Configuration

### 6.1 Default Configuration

```json
{
  "DataSequenceSettings": {
    "Sequence": [
      {
        "Type": "Normal",
        "Order": 1,
        "ExpectedDelaySeconds": 0,
        "TimeToleranceSeconds": 5,
        "Enabled": true
      },
      {
        "Type": "NIR",
        "Order": 2,
        "ExpectedDelaySeconds": 1,
        "TimeToleranceSeconds": 10,
        "Enabled": true
      },
      {
        "Type": "Cam1",
        "Order": 3,
        "ExpectedDelaySeconds": 5,
        "TimeToleranceSeconds": 15,
        "Enabled": true
      },
      {
        "Type": "Cam2",
        "Order": 4,
        "ExpectedDelaySeconds": 6,
        "TimeToleranceSeconds": 15,
        "Enabled": true
      },
      {
        "Type": "Cam3",
        "Order": 5,
        "ExpectedDelaySeconds": 7,
        "TimeToleranceSeconds": 15,
        "Enabled": true
      }
    ]
  }
}
```

### 6.2 Validation Rules

| Rule | Check |
|------|-------|
| Unique Order values | No duplicates in Order |
| Unique Types | Each Type appears once |
| Valid delays | ExpectedDelaySeconds >= 0 |
| Valid tolerances | TimeToleranceSeconds >= 1 |
| At least one enabled | Count(Enabled=true) > 0 |

---

## 7. Glossary Updates

### New Terms

| Term | Type | Description |
|------|------|-------------|
| `DataSequenceSettings` | Class | Configuration for data arrival sequence |
| `DataSequenceItem` | Class | Single entry in sequence |
| `DataType` | Enum | Type of data (NIR, Normal, Cam1-6) |
| `ExpectedDelaySeconds` | Property | Expected time delay from previous type |
| `TimeToleranceSeconds` | Property | Allowed deviation from expected delay |

---

## 8. Architecture Documentation Plan

### New Docs to Create

| Document | Purpose |
|----------|---------|
| `module_data_sequence_settings.md` | DataSequenceSettings architecture |

### Docs to Update

| Document | Changes |
|----------|---------|
| `glossary.md` | Add DataSequenceSettings terms |
| `module_settings_dialog.md` | Add Data Sequence tab |
| `module_main_window.md` | Add dynamic column generation |
| `module_monitoring_orchestrator.md` | Add timestamp matching |

---

## 9. Verification Plan

### Automated Tests

**Unit Tests** (DataSequenceSettings):
- Preset configuration correctness
- Sequence ordering and validation
- Helper method contracts (GetByType, GetTolerance, etc.)
- Invalid configuration rejection

**Integration Tests**:
- Settings persistence (save/load from appsettings.json)
- Dynamic column generation matches sequence
- Timestamp-based matching with configured tolerances
- Preset application updates UI

### Manual Verification

**Scenario 1: Sequence Configuration**:
   - Open Settings → Data Sequence tab
   - Drag items to reorder
   - Change delay/tolerance values
   - Click Save
   - Verify saved to appsettings.json

**Scenario 2: UI Column Order**:
   - Configure sequence: NIR → Normal → Cam1
   - Restart app
   - Verify Main Window columns match order

**Scenario 3: Real-time Matching**:
   - Configure: NIR (T=0±10s) → Normal (T=1±5s)
   - Create NIR file
   - Wait 1s, create Normal file
   - Verify: Single group created

**Scenario 4: Preset Application**:
   - Select "NIR First" preset
   - Verify sequence updates
   - Verify columns reorder

---

## 10. Implementation Phases

### Phase 1: Configuration Model
**Files**: `DataSequenceSettings.cs`, `DataSequencePresets.cs`, `ApplicationConfiguration.cs`

**Tasks**:
- [ ] Create DataSequenceSettings class
- [ ] Create DataSequenceItem class
- [ ] Create DataType enum
- [ ] Create DataSequencePresets static class
- [ ] Add to ApplicationConfiguration
- [ ] Add validation logic

---

### Phase 2: Settings Dialog UI
**Files**: `SettingsDialog.xaml`, `SettingsDialogViewModel.cs`

**Tasks**:
- [ ] Add Data Sequence tab to XAML
- [ ] Create reorderable list UI
- [ ] Add preset dropdown
- [ ] Add per-item edit dialog
- [ ] Implement ViewModel properties
- [ ] Implement MoveUp/Down commands
- [ ] Implement ApplyPreset command

---

### Phase 3: Dynamic Column Generation
**Files**: `MainWindow.xaml`, `MainWindow.xaml.cs`, `MainWindowViewModel.cs`

**Tasks**:
- [ ] Remove static DataGrid.Columns from XAML
- [ ] Implement GenerateDataGridColumns method
- [ ] Call from Window_Loaded
- [ ] Subscribe to settings changed event
- [ ] Handle Line 1 / Line 2 differences
- [ ] Update Combined view

---

### Phase 4: Real-time Matching
**Files**: `MonitoringOrchestrator.cs`

**Tasks**:
- [ ] Inject DataSequenceSettings
- [ ] Update FindMatchingExistingGroup
- [ ] Add timestamp-based Match 3
- [ ] Use configured tolerances
- [ ] Implement priority-based matching
- [ ] Add diagnostic logging

---

### Phase 5: Testing & Documentation
**Files**: Test files, architecture docs

**Tasks**:
- [ ] Unit tests for DataSequenceSettings
- [ ] Integration tests for matching
- [ ] Manual UI tests
- [ ] Create module_data_sequence_settings.md
- [ ] Update glossary.md
- [ ] Update related architecture docs

---

## 11. Risk Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Dynamic column generation fails | High | Fallback to static columns if config missing |
| Settings not saved | Medium | Validate JSON before save, backup original |
| Timestamp matching too broad | Medium | Start with conservative tolerances (±5-10s) |
| UI becomes slow with reordering | Low | Use virtualization, limit list to 10 items max |
| Breaking existing configs | High | Provide migration logic for old appsettings.json |

---

## 12. Open Questions

- [ ] Should we support per-line sequence? → **TBD**: Start global, add later if needed
- [x] What default sequence to use? → **Normal → NIR → Cameras** (current behavior)
- [ ] Should column width also be configurable? → **TBD**: Future enhancement
- [x] How to handle missing data types? → **Skip columns** for disabled types

---

**Status**: [ ] Ready for approval
**Next Step**: User approval → Create 04_design.md
