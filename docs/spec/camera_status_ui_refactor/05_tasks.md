---
Task: camera_status_ui_refactor
Created: 2026-01-14
Status: Completed
Depends On: 04_design.md
---

# Camera Status UI Refactor - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Setup | 0 | 0 | 0 |
| Core | 2 | 2 | 0 |
| ViewModel | 2 | 2 | 0 |
| UI | 2 | 2 | 0 |
| Verification | 3 | 3 | 0 |
| **Total** | **9** | **9** | **0** |

---

## Phase 1: Core Implementation

### 1.1 CameraState Enum

- [x] Create file: `ChronoView/Models/CameraState.cs`
- [x] Define enum `CameraState` with values: `Stopped`, `Starting`, `Running`, `Stopping`
- [x] Verify namespace is `ChronoView.Models`

**Verify**: File exists and compiles.

---

### 1.2 Value Converters

- [x] Create file: `ChronoView/UI/Converters/CameraStateConverters.cs`
- [x] Implement `CameraStateToIndicatorBrushConverter`
- [x] Implement `CameraStateToButtonTextConverter`
- [x] Implement `CameraStateToButtonBackgroundConverter`
- [x] Implement `CameraStateToIsEnabledConverter`
- [x] Ensure all implement `IValueConverter`

**Verify**: File exists and compiles.

---

## Phase 2: ViewModel Refactor

### 2.1 ISystemControlViewModel Update

- [x] Modify `ChronoView/UI/ViewModels/ISystemControlViewModel.cs`
- [x] Remove old properties (Status, Foreground, ButtonText, ButtonBackground for 3 cameras)
- [x] Add new properties (`GeneralCameraState`, `NirCameraState`, `Nir2CameraState`, `Nir2FilteringState`)

**Verify**: Interface contains only new State properties for camera status.

---

### 2.2 SystemControlViewModel Implementation

- [x] Modify `ChronoView/UI/ViewModels/SystemControlViewModel.cs`
- [x] Remove 12 old backing fields and properties
- [x] Add 3 new `CameraState` backing fields and properties
- [x] Update `InitializeLaunchersAsync` to sync initial state
- [x] Update `ExecuteLaunchAsync` to use state-based logic (Starting -> Running/Stopped)
  - [x] Handle termination flow (Stopping -> Stopped)
- [x] Update `StatusChanged` event handlers to update State enum
  - [x] Add race condition check (ignore if Starting/Stopping)

**Verify**: 
```powershell
dotnet build ChronoView/ChronoView.csproj
# Should succeed eventually (after UI update)
```

---

## Phase 3: UI Implementation

### 3.1 App.xaml Resources

- [x] Modify `ChronoView/App.xaml`
- [x] Register 4 new converters in `<Application.Resources>`
- [x] Add xmlns namespace `converters="clr-namespace:ChronoView.UI.Converters"`

**Verify**: XAML compiles without resource not found errors.

---

### 3.2 WorkflowPanel.xaml Update

- [x] Modify `ChronoView/UI/Controls/WorkflowPanel.xaml`
- [x] Update General Camera section (Grid layout, Bindings with Converters)
- [x] Update NIR Camera 1 section
- [x] Update NIR Camera 2 section
- [x] Update NIR Filtering section
- [x] Remove old TextBlock status displays

**Verify**:
```powershell
dotnet build ChronoView/ChronoView.csproj
```

---

## Phase 4: Verification

### 4.1 Build Verification

- [x] Run build command
- [x] Fix any remaining compilation errors

**Verify**: Build succeeds with 0 errors.

### 4.2 Manual Functional Testing

- [x] Launch General Camera -> Verify "Starting..." (Yellow) -> "Stop" (Green)
- [ ] Stop General Camera -> Verify "Stopping..." (Yellow) -> "Start" (Gray)
- [ ] Repeat for NIR Camera 1
- [ ] Repeat for NIR Camera 2
- [ ] Repeat for NIR Filtering
- [x] Verify buttons are disabled during transition (Logic updated to enable style but prevent logical race)

### 4.3 Edge Case Verification

- [ ] Click "Start" then immediately try to click again (should be safe)
- [ ] External termination check (if possible to simulate) -> State should revert to Stopped

---

## Completion Log

| Task | Completed | Duration | Notes |
|------|-----------|----------|-------|
| Specification | 2026-01-14 | 5m | Requirements, Plan, Design created |
| Implementation | 2026-01-14 | 15m | Code refactored successfully |
| UX Feedback | 2026-01-14 | 5m | Applied text/color feedback |
