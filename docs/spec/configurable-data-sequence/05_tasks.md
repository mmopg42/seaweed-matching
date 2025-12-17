---
Task: Configurable Data Sequence Settings
Created: 2025-12-15
Status: Draft
Depends On: 04_design.md
---

# Configurable Data Sequence Settings - Implementation Tasks

> **Implementation Order**: Complete phases sequentially. Each phase builds on the previous.

---

## Phase 1: Configuration Model ⚙️

**Goal**: Create data models and presets for sequence configuration

### 1.1 Core Models
- [x] Create `ChronoView/Models/DataSequenceSettings.cs`
  - [x] Define `DataSequenceSettings` class
  - [x] Define `DataSequenceItem` class
  - [x] Define `DataType` enum (NIR, Normal, Cam1-6)
  - [x] Implement `GetByType(DataType)` method
  - [x] Implement `GetExpectedDelay(DataType)` method
  - [x] Implement `GetTolerance(DataType)` method
  - [x] Implement `GetOrderedTypes()` method
  - [x] Implement `Validate(out List<string> errors)` method

### 1.2 Presets
- [x] Create `ChronoView/Models/DataSequencePresets.cs`
  - [x] Implement `NormalFirst()` preset (Default)
  - [x] Implement `NirFirst()` preset
  - [x] Implement `CamerasFirst()` preset

### 1.3 Configuration Integration
- [x] Update `ChronoView/Models/ApplicationConfiguration.cs`
  - [x] Add property: `public DataSequenceSettings DataSequenceSettings { get; set; }`
  - [x] Initialize with default preset in constructor

### 1.4 Validation Testing
- [x] Test validation rules:
  - [x] Empty sequence → error
  - [x] All disabled → error
  - [x] Duplicate Order values → error
  - [x] Duplicate Type values → error
  - [x] Negative delay → error
  - [x] Zero tolerance → error
  - [x] Valid configuration → pass

---

## Phase 2: Settings Dialog UI (Drag-Drop) 🎨

**Goal**: Create UI for configuring data sequence with drag-drop reordering

### 2.1 ViewModel
- [x] Update `ChronoView/UI/ViewModels/SettingsDialogViewModel.cs`
  - [x] Add property: `ObservableCollection<DataSequenceItemViewModel> SequenceItems`
  - [x] Add property: `string SelectedPreset`
  - [x] Add property: `DataSequenceItemViewModel? DraggedItem`
  - [x] Add property: `int DropTargetIndex`
  - [x] Add command: `ICommand ApplyPresetCommand`
  - [x] Implement `LoadSequenceSettings()` method
  - [x] Implement `ApplyPreset()` method
  - [x] Update `SaveCommand` to save DataSequenceSettings
  - [x] Implement `HasUnsavedChanges()` for dirty tracking

### 2.2 Item ViewModel
- [x] Create `ChronoView/UI/ViewModels/DataSequenceItemViewModel.cs`
  - [x] Add property: `DataType Type`
  - [x] Add property: `int Order`
  - [x] Add property: `int ExpectedDelay`
  - [x] Add property: `int Tolerance`
  - [x] Add property: `bool Enabled`
  - [x] Add property: `bool IsDropTarget` (for visual feedback)
  - [x] Implement `INotifyPropertyChanged`

### 2.3 XAML - Data Sequence Tab
- [x] Update `ChronoView/UI/Views/SettingsDialog.xaml`
  - [x] Add 4th TabItem: "Data Sequence" (데이터 순서)
  - [x] Add preset ComboBox with:
    - [x] "Normal First"
    - [x] "NIR First"
    - [x] "Cameras First"
  - [x] Add [Apply Preset] button
  - [x] Add ListBox for sequence items with `AllowDrop="True"`
  - [x] Define ItemTemplate:
    - [x] Display drag handle icon ([≡])
    - [x] Display Type name
    - [x] Display Delay value
    - [x] Display Tolerance value
    - [x] Display Enabled checkbox
  - [ ] Add Timeline Preview panel (optional, nice-to-have)

### 2.4 Drag-Drop Event Handlers
- [x] Add to `ChronoView/UI/Views/SettingsDialog.xaml.cs`
  - [x] Implement `Item_PreviewMouseDown(sender, e)`
  - [x] Implement `Item_MouseMove(sender, e)` → Start drag if dragging
  - [x] Implement `Item_DragOver(sender, e)` → Highlight drop target
  - [x] Implement `Item_Drop(sender, e)` → Reorder items

### 2.5 Styling
- [x] Apply consistent WPF style:
  - [x] Match existing SettingsDialog colors (`#f0f0f0`, `#d0d0d0`)
  - [x] Match existing border style (1px solid, no rounded corners)
  - [x] Match existing button dimensions (Width="80", Height="30")
  - [x] Match existing label width (Width="150")
  - [x] Add drop target highlight style:
    - [x] Background: `#e0e0e0`
    - [x] BorderBrush: `#0078d4`
    - [x] BorderThickness: `2`

### 2.6 Per-Item Edit Dialog (Optional Enhancement)
- [ ] Create `EditSequenceItemDialog.xaml` (future enhancement)
  - [ ] Type dropdown
  - [ ] Delay input
  - [ ] Tolerance input
  - [ ] Enabled checkbox

---

## Phase 3: Dynamic Column Generation ✅

**Goal**: Modify MainWindow to dynamically generate DataGrid columns based on DataSequenceSettings order

### 3.1 MainWindow.xaml Modifications
- [x] Add `x:Name="Line1DataGrid"` to Line 1 DataGrid
- [x] Add `x:Name="Line2DataGrid"` to Line 2 DataGrid
- [x] Keep static columns (Checkbox, Index, Status) in XAML
- [x] Remove data columns (will be generated dynamically)
- [ ] Add similar changes for Combined Line DataGrids (if needed)

### 3.2 MainWindow.xaml.cs Implementation
- [x] Add `GenerateDataGridColumns()` method
  - [x] Load DataSequenceSettings from configuration
  - [x] Call `GetOrderedTypes()`
  - [x] Generate columns for Line1DataGrid
  - [x] Generate columns for Line2DataGrid

- [x] Implement `GenerateColumnsForDataGrid(DataGrid, List<DataType>, int lineNumber)`
  - [x] Use `BeginInit()` / `EndInit()` to prevent UI flicker
  - [x] Clear existing dynamic columns (keep first 3 static)
  - [x] Loop through orderedTypes
  - [x] Filter by line number with `IsDataTypeForLine()`
  - [x] Call `CreateColumnForDataType()` for each type
  - [x] Add columns to DataGrid.Columns

- [x] Implement `CreateColumnForDataType(DataType)` helper
  - [x] Create DataGridTemplateColumn
  - [x] Set Header from `GetColumnHeader()`
  - [x] Create Border with image binding
  - [x] Bind width/height (NirDisplayWidth/Height for NIR, DisplayImageWidth/Height for others)
  - [x] Create Grid → Image → ProgressBar hierarchy
  - [x] Bind Image.Source to appropriate thumbnail property
  - [x] Bind ProgressBar visibility (shown when loading)
  - [x] Return configured column

- [x] Implement `IsDataTypeForLine(DataType, int)` helper
  - [x] Return true for Normal, NIR (both lines)
  - [x] Return lineNumber == 1 for Cam1, Cam2, Cam3
  - [x] Return lineNumber == 2 for Cam4, Cam5, Cam6

- [x] Implement `GetColumnHeader(DataType)` helper
  - [x] Return localized header strings
  - [x] Add `// TODO: Localization` comments

- [x] Implement `GetBindingPath(DataType)` helper
  - [x] Return ViewModel property names (MainImageThumbnail, NirGraphThumbnail, Camera1Thumbnail, etc.)

- [x] Call from Window_Loaded event
  - [x] Call `GenerateDataGridColumns()` after InitializeComponent()

### 3.3 Settings Changed Regeneration
- [x] Add OnSettingsApplied handler
  - [x] Regenerate columns when DataSequenceSettings changes
  - [x] Call `GenerateDataGridColumns()` to refreshration:
  - [ ] Default sequence → columns match order
  - [ ] Change sequence → columns reorder
  - [ ] Disable type → column hidden
  - [ ] Line 1 shows Cam1-3, Line 2 shows Cam4-6
  - [ ] No UI flicker during regeneration

---

## Phase 4: Real-time Matching (Timestamp Fallback) 🔗

**Goal**: Update matching logic to use configured sequence and tolerances

### 4.1 Dependency Injection
- [ ] Update `ChronoView/App.xaml.cs`
  - [ ] Ensure `DataSequenceSettings` is available via DI
  - [ ] Inject into `MonitoringOrchestrator` constructor

### 4.2 Update Matching Logic
- [x] Update `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs`
  - [x] ~~Inject `ApplicationConfiguration` in constructor~~ (Already available via _currentConfig)
  - [x] Update `FindMatchingExistingGroup(FileGroup)`:
    - [x] **Match 1**: By NormalFolder (existing) ✓
    - [x] **Match 2**: By NirKey (existing) ✓
    - [x] **Match 3** (NEW): By Timestamp + LineNumber + Priority ✓
      - [x] Get `orderedTypes` from `DataSequenceSettings`
      - [x] Determine `newGroupType` using `DetermineDataTypeForGroup()`
      - [x] Loop through orderedTypes with higher priority
      - [x] Get tolerance from `DataSequenceSettings.GetTolerance()`
      - [x] Find candidates within tolerance window
      - [x] **Apply Temporal Ordering Constraint** ✓
      - [x] Pick closest timestamp match
  - [x] Implement `DetermineDataTypeForGroup(FileGroup)` helper
  - [x] Implement `HasDataType(FileGroup, DataType)` helper
  - [x] Implement `GetPriority(DataType)` helper

### 4.3 Logging
- [x] Add diagnostic logging:
  - [x] Log when Match 1 succeeds (NormalFolder)
  - [x] Log when Match 2 succeeds (NirKey)
  - [x] Log when Match 3 succeeds (Timestamp + Type)
  - [x] Log tolerance used for Match 3
  - [x] Log time delta (Δ) when matching by timestamp
  - [x] Log when no match found (new group created)
  - [x] Log temporal ordering constraint application

### 4.4 Testing
- [ ] Test matching scenarios:
  - [ ] NIR arrives first, Normal arrives 1s later → merged
  - [ ] Normal arrives first, NIR arrives 10s later → merged
  - [ ] Files arrive >tolerance apart → separate groups
  - [ ] Multiple candidates → closest timestamp selected
  - [ ] Priority matching: higher priority type matched first

---

## Phase 5: Testing & Documentation 📝

**Goal**: Verify implementation and update architecture documentation

### 5.1 Unit Tests
- [ ] Create `ChronoView.Tests/Models/DataSequenceSettingsTests.cs`
  - [ ] Test `GetByType()` returns correct item
  - [ ] Test `GetByType()` returns null for missing type
  - [ ] Test `GetExpectedDelay()` returns correct value
  - [ ] Test `GetExpectedDelay()` returns 0 for missing type
  - [ ] Test `GetTolerance()` returns correct value
  - [ ] Test `GetTolerance()` returns 10 for missing type
  - [ ] Test `GetOrderedTypes()` returns sorted enabled types
  - [ ] Test `Validate()` catches all error conditions
  - [ ] Test presets have valid configurations

### 5.2 Integration Tests
- [ ] Test Settings Dialog:
  - [ ] Load sequence from config
  - [ ] Drag-drop reorders items
  - [ ] Apply preset updates sequence
  - [ ] Save persists to appsettings.json
  - [ ] Cancel discards changes
  - [ ] Validation prevents invalid save

- [ ] Test Dynamic Columns:
  - [ ] Columns match sequence order
  - [ ] Column visibility based on line number
  - [ ] Columns regenerate on settings change
  - [ ] No UI flicker during regeneration

- [ ] Test Real-time Matching:
  - [ ] Timestamp-based matching works
  - [ ] Tolerances are respected
  - [ ] Priority-based matching works
  - [ ] Multiple matches → closest selected

### 5.3 Manual Testing
- [ ] Open Settings → Data Sequence tab
- [ ] Drag items to reorder → verify Order updates
- [ ] Change delay/tolerance values → verify saved
- [ ] Apply "NIR First" preset → verify sequence changes
- [ ] Save and restart app → verify settings persisted
- [ ] Open MainWindow → verify column order matches sequence
- [ ] Change sequence in settings → verify columns reorder
- [ ] Create test files:
  - [ ] NIR file → Normal file (1s later) → verify single group
  - [ ] Normal file → NIR file (10s later) → verify single group
  - [ ] Files >tolerance apart → verify separate groups

### 5.4 Architecture Documentation
- [ ] Create `docs/architecture/module_data_sequence_settings.md`
  - [ ] Overview
  - [ ] Key Components
  - [ ] Contracts (GetByType, GetTolerance, etc.)
  - [ ] Logic Flow
  - [ ] Dependencies
  - [ ] Dependents
  - [ ] Impact/Touchpoints
  - [ ] Verification commands
  - [ ] Related Docs

- [ ] Update `docs/architecture/glossary.md`
  - [ ] Add `DataSequenceSettings` entry
  - [ ] Add `DataSequenceItem` entry
  - [ ] Add `DataType` enum entry
  - [ ] Add `ExpectedDelaySeconds` entry
  - [ ] Add `TimeToleranceSeconds` entry

- [ ] Update `docs/architecture/module_settings_dialog.md`
  - [ ] Add Data Sequence tab section
  - [ ] Document drag-drop reordering
  - [ ] Document preset functionality

- [ ] Update `docs/architecture/module_main_window.md`
  - [ ] Document dynamic column generation
  - [ ] Document BeginInit/EndInit usage
  - [ ] Document column mapping logic

- [ ] Update `docs/architecture/module_monitoring_orchestrator.md`
  - [ ] Document Match 3 (Timestamp + Priority)
  - [ ] Document tolerance configuration
  - [ ] Document priority-based matching

- [ ] Update `docs/architecture/README.md`
  - [ ] Add link to `module_data_sequence_settings.md`

### 5.5 Localization Preparation (Future)
- [ ] Identify all user-facing strings:
  - [ ] Column headers ("Main Img", "NIR Graph", etc.)
  - [ ] Preset names ("Normal First", "NIR First", etc.)
  - [ ] Validation error messages
  - [ ] Dialog titles and buttons
- [ ] Document localization plan in architecture docs
- [ ] Add TODO comments in code where strings should be localized

---

## Phase 6: Final Report 📋

**Goal**: Document what was implemented and create completion report

### 6.1 Create Report
- [ ] Create `docs/spec/configurable-data-sequence/06_report.md`
  - [ ] Summary of implementation
  - [ ] Features delivered
  - [ ] Testing results
  - [ ] Known limitations
  - [ ] Future enhancements
  - [ ] Breaking changes (if any)

### 6.2 Freeze Spec
- [ ] Mark all spec documents as **FROZEN**
- [ ] Update status in frontmatter: `Status: Complete`
- [ ] Add completion date

---

## Definition of Done (DoD) ✅

**Before marking this task as complete, verify:**

- [ ] All 5 implementation phases completed
- [ ] All unit tests passing
- [ ] All integration tests passing
- [ ] Manual testing scenarios verified
- [ ] No regressions in existing functionality
- [ ] Architecture documentation updated
- [ ] Glossary updated with new terms
- [ ] Code builds without errors
- [ ] Settings save and load correctly
- [ ] UI matches design consistency requirements
- [ ] Drag-drop reordering works smoothly
- [ ] Dynamic columns generate without flicker
- [ ] Real-time matching uses configured tolerances
- [ ] 06_report.md created

---

## Notes & Considerations 📌

### High-Risk Items
1. **Dynamic Column Generation**: WPF DataGrid column manipulation can be tricky
   - Mitigation: Test thoroughly, use BeginInit/EndInit
2. **Drag-Drop UX**: Complex to implement correctly
   - Mitigation: Follow WPF best practices, test edge cases
3. **Timestamp Matching**: Risk of false positives
   - Mitigation: Start with conservative tolerances, add logging

### Nice-to-Have Enhancements (Post-MVP)
- [ ] Timeline Preview visualization in Settings Dialog
- [ ] Per-item edit dialog (currently inline editing)
- [ ] Undo/Redo for sequence changes
- [ ] Import/Export sequence configurations
- [ ] Per-line sequence configuration (currently global)
- [ ] Column width configuration
- [ ] Actual localization implementation (Resources.resx)

### Breaking Changes
- `appsettings.json` schema changes:
  - New section `DataSequenceSettings` added
  - Existing configs will get default preset on first load

---

**Status**: [ ] Ready to implement
**Estimated Effort**: 3-4 days (assuming 1 developer)
