---
Task: fix_nir_filtering_button_ui
Created: 2026-01-14
Status: Draft
Depends On: 01_requirements.md
---

# Fix NIR Filtering Button UI - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: Why does the button text not change color? | XAML binding for `Foreground` is missing in `SetupWindow.xaml`. | High |
| Q2: Does the ViewModel update state correctly? | Yes, logic exists but UI does not consume the color property. | High |
| Q3: Is the initial state correct? | No, ViewModel defaults to "Deactivated" without checking Service state. | High |

## 2. Detailed Findings

### 2.1 Q1: Why does the button text not change color?

**Method**: 
- Code review of `ChronoView/UI/Views/SetupWindow.xaml`.
- Analysis of `SetupWindowViewModel.cs`.

**Findings**:
- In `SetupWindow.xaml` (lines 374-407), the button template uses a `Run` element for the status text:
  ```xml
  <Run Text="{Binding Nir2FilteringStatus, Mode=OneWay}" FontWeight="Bold"/>
  ```
- It binds `Text` but **does not bind `Foreground`**.
- The parent `Button` explicitly sets `Foreground="White"` (line 379), which overrides/inherits to the content.
- `SetupWindowViewModel.cs` defines `Nir2FilteringForeground` property (lines 104-108) and updates it (lines 252-270), but it is **unused in XAML**.

**Evidence**:
- `SetupWindow.xaml`: Line 393 `<Run Text="{Binding Nir2FilteringStatus, Mode=OneWay}" FontWeight="Bold"/>` lacks `Foreground` binding.

**Conclusion**: The missing `Foreground` binding on the `Run` element is the primary cause of the visual bug.

---

### 2.2 Q2: Does the ViewModel update state correctly?

**Method**:
- Code review of `ExecuteToggleNirFilteringAsync` and `UpdateNir2FilteringStatus` in `SetupWindowViewModel.cs`.

**Findings**:
- `ExecuteToggleNirFilteringAsync` calls `UpdateNir2FilteringStatus()` finally.
- `UpdateNir2FilteringStatus()` correctly sets `Nir2FilteringStatus` ("Activated"/"Deactivated") and `Nir2FilteringForeground` (Green/Red).
- The logic is sound for *updating* state after a user action.

**Evidence**:
- `SetupWindowViewModel.cs`: Lines 252-270 show correct logic for setting both text and color properties based on `_nirFilteringService.IsFilteringActive`.

**Conclusion**: The update logic is correct; the issue is purely the UI connection.

---

### 2.3 Q3: Is the initial state correct when the window opens?

**Method**:
- Analyzed `SetupWindowViewModel` constructor and `NirFilteringService` lifecycle in `App.xaml.cs`.

**Findings**:
- `NirFilteringService` is registered as a **Singleton** in `App.xaml.cs` (line 293). This means filtering state persists across `SetupWindow` open/close cycles (if the app logic allows reopening Setup, or if it persists in background).
- `SetupWindowViewModel` initializes `_nir2FilteringStatus = "Deactivated"` (line 32) and does **not check the actual service state in the constructor**.
- If `SetupWindow` is reopened (though current flow suggests it's mostly startup-only, but `App.xaml.cs` logic is complex), or if we add logic to reopen it, it would show "Deactivated" even if filtering is running.

**Evidence**:
- `SetupWindowViewModel.cs` Constructor (lines 46-73) does not call `UpdateNir2FilteringStatus()`.

**Conclusion**: Initialization logic is missing. We should call `UpdateNir2FilteringStatus()` in the constructor to ensure the UI reflects the true state of the singleton service.

---

## 3. Code Analysis

### 3.1 Relevant Existing Code

| File | Component | Relevance | Notes |
|------|-----------|-----------|-------|
| `UI/Views/SetupWindow.xaml` | `ToggleNirFilteringCommand` Button | **Target Fix** | Missing `Foreground` binding. |
| `UI/ViewModels/SetupWindowViewModel.cs` | `SetupWindowViewModel` | **Target Fix** | Needs init logic in ctor. |
| `Core/ProgramLaunching/NirFilteringService.cs` | `NirFilteringService` | Dependency | Singleton, holds truth state `IsFilteringActive`. |

### 3.2 Glossary Check
- `Nir2FilteringStatus`: String property for UI text.
- `Nir2FilteringForeground`: Brush property for UI color.

### 3.3 Impact Analysis

| Existing Component | Potential Impact | Risk Level |
|--------------------|------------------|------------|
| `SetupWindow` | Visual change only. | Low |
| `SetupWindowViewModel` | Added initialization logic. | Low |

## 4. Recommendations

### Primary Recommendation

1.  **Modify `SetupWindow.xaml`**:
    - Add `Foreground="{Binding Nir2FilteringForeground}"` to the `<Run>` element inside the NIR filtering button to enable color changes.
    
2.  **Modify `SetupWindowViewModel.cs`**:
    - Call `UpdateNir2FilteringStatus()` in the constructor (after service injection) to sync initial UI state with the singleton service.

## 5. Unanswered Questions
None.

---
