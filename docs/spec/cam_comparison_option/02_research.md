---
Task: cam_comparison_option
Created: 2026-01-14
Status: Draft
Depends On: 01_requirements.md
---

# Camera Comparison Option - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: Where to store the option? | `DataSequenceSettings` class in `ChronoView/Models/DataSequenceSettings.cs`. | High |
| Q2: How to expose in UI? | Add checkbox in `SettingsDialog.xaml` (Sequence tab) and bind to `SettingsDialogViewModel`. | High |
| Q3: How to implement logic? | Modify `FileMatchingEngine.BuildLineGroupsWithOrder` to use Cam1/Cam4 as reference if enabled. | High |

## 2. Detailed Findings

### 2.1 Code Implementation Location

**Method**: Code review of `FileMatchingEngine.cs` and `DataSequenceSettings.cs`.

**Findings**:
- `FileMatchingEngine.BuildLineGroupsWithOrder` iterates through `orderedTypes`.
- Currently, it uses `orderedTypes.FirstOrDefault(x => x.Order == order - 1)` (immediate predecessor) as the reference.
- We need to introduce a conditional check: if `CompareToReferenceCamera` is true, force the reference to be `DataType.Cam1` when processing `DataType.Cam2` or `DataType.Cam3`. (Since Line 2 uses these same types internally, logic applies to both).

**Evidence**:
- `FileMatchingEngine.cs`: `BuildLineGroupsWithOrder` method iteration logic.
- `DataSequenceSettings.cs`: Currently holds `List<DataSequenceItem> Sequence`. Can easily add `public bool CompareToReferenceCamera { get; set; }`.

**Conclusion**: Store the setting in `DataSequenceSettings` and use it in `FileMatchingEngine`.

### 2.2 UI Integration

**Method**: Reviewed `SettingsDialogViewModel.cs`.

**Findings**:
- `SequenceItems` (ObservableCollection) populates the sequence UI.
- The `DataType` enum values for Cam1/2/3/4/5/6 are already handled in the codebase, with Cam4/5/6 often treated functionally as Line 2 versions of 1/2/3.
- We need to add a property `CompareToReferenceCamera` to `SettingsDialogViewModel` and map it to `_configuration.DataSequenceSettings.CompareToReferenceCamera`.

**Evidence**:
- `SettingsDialogViewModel.cs:779`: `var settings = _configuration.DataSequenceSettings;`

**Conclusion**: Add property to ViewModel and a CheckBox in the XAML.

### 2.3 Edge Cases to Handle

1. **Non-sequential Orders**: 
   - If Cam1.Order=3, Cam2.Order=5 (gap in sequence)
   - Current logic (`order - 1`) will fail to find Cam1
   - **Solution**: Use `orderedTypes.FirstOrDefault(x => x.Type == DataType.Cam1)` directly

2. **Cam1 Not in Sequence**:
   - If Cam1 is disabled or missing
   - **Solution**: Log warning and fall back to immediate predecessor

### 2.4 Line Number Detection

- **Line 2 Handling**:
  - The engine maps Line 2 files (Cam4/5/6) to `DataType.Cam1/2/3`.
  - The logic only needs to handle `CompareToReferenceCamera` for `DataType.Cam2` and `DataType.Cam3`, comparing them to `DataType.Cam1`.
  - This single logic covers both lines.

### 2.5 Edge Case Handling Matrix

| Case | Issue | Solution |
|------|-------|----------|
| Cam1 disabled | Cam2/3 can't find reference | Fall back to **nearest enabled preceding item** in ordered list + log warning |
| Non-sequential Order | `order - 1` fails | Use `orderedTypes` index-based lookup to find preceding item |
| Cam1 has no files | Cam2/3 create separate groups | Expected behavior, document as "partial match" |

