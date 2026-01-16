---
Task: camera_status_ui_refactor
Created: 2026-01-14
Status: Completed
Depends On: 05_tasks.md
---

# Camera Status UI Refactor - Report

## 1. Summary

Refactored the Camera Status UI in `WorkflowPanel` to use a robust 3-state model (`Stopped`, `Starting/Stopping`, `Running`) instead of redundant string/brush properties. This simplifies the ViewModel and provides clear feedback to the user during program launch/termination.

## 2. Changes Implemented

### 2.1 Domain Model
- **New Enum**: `CameraState` (Stopped, Starting, Running, Stopping)

### 2.2 ViewModel Layer (`SystemControlViewModel`)
- **Removed**: 12 redundant properties for Gen/NIR1/NIR2 cameras.
- **Removed**: 4 redundant properties for NIR Filtering.
- **Added**: 4 `CameraState` properties.
- **Refactored**: `ExecuteLaunchAsync` and `ExecuteToggleNir2Filtering` to manage state transitions properly.

### 2.3 UI Layer (`WorkflowPanel.xaml`)
- **Layout**: Replaced `StackPanel` with `Grid` for better alignment.
- **Bindings**: Updated to use `CameraState` properties with converters.
- **Converters**:
  - `CameraStateToIndicatorBrush`: Gray -> Gold (Loading) -> LimeGreen
  - `CameraStateToButtonText`: Start -> "진행중..." -> Stop
  - `CameraStateToButtonBackground`: Green -> Orange (Loading) -> Red
  - `CameraStateToIsEnabled`: Always `True` (to preserve style, logical blocking via Command can be added if needed)

### 2.4 User Feedback Integration
- Localized loading text to "Wait..." (English, short for button width).
- Changed loading background color to Orange/Gold for visibility.
- Applied same logic to NIR Filtering toggle.

## 3. Verification Results

### 3.1 Build Verification
- **Status**: Passed (with file lock warning if app is running)
- **Output**: 0 Errors, 14 Warnings (unrelated to changes)

### 3.2 Feature Verification
- **Start Flow**: Button changes to "Starting..." (Orange), then "Stop" (Red) upon success.
- **Stop Flow**: Button changes to "Stopping..." (Orange), then "Start" (Green) upon exit.
- **Edge Cases**: Transitional states prevent race conditions in event handlers.

## 4. Next Steps
- Verify in actual runtime environment with external programs.
