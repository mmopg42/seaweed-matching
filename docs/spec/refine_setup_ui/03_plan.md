# Implementation Plan - Refine Setup UI

## Goal Description
Refine the Setup Window UI by removing the status message feedback mechanism (as requested) and adding a "Default Settings" feature to allow users to easily reset configuration to a factory state with empty paths.

## User Review Required


#### [MODIFY] [SetupWindowViewModel.cs](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/UI/ViewModels/SetupWindowViewModel.cs)
- Remove `StatusMessage` and `StatusVisibility` properties.
- Remove `ShowStatus` method.
- Update `LaunchCameraCommand`, `ToggleNirFilterCommand`, etc., to remove calls to `ShowStatus`.
- Update `NirFilterStatus` logic:
    - Ensure text color is White for all states.
    - Add "Processing..." state during async operations.

#### [MODIFY] [SetupWindow.xaml](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/UI/Views/SetupWindow.xaml)
- Remove the Status Bar border/text control.
- Update NIR Filter Status text styling (remove triggers that change color to green, force White).

## Verification Plan

### Automated Tests
- **Unit Test for DefaultConfiguration**:
    - Verify `GetDefault()` returns a config object.
    - Verify all path properties in the returned object are Empty/Null.
    - Verify other properties have expected default values.

### Manual Verification
1. **Status Removal**:
    - Open Setup Window.
    - Click "Launch Camera" or Toggle Filter.
    - Verify **NO** yellow/orange status bar appears at the bottom.
    - Verify NIR Filter text changes to "Processing..." then "Activated" (in White).
2. **Default Settings**:
    - Open Settings Dialog.
    - Change some values (paths, checkboxes).
    - Click "Default" button.
    - **Verify**:
        - All path fields become empty.
        - Checkboxes/Numbers reset to default values.
    - Click "Cancel" -> Re-open -> Verify old values persist (changes not saved).
    - Click "Default" -> "Save" -> Verify defaults are saved.
