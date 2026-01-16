---
Task: fix_launch_activation_bugs
Created: 2026-01-12
Status: Todo
Depends On: 04_design.md
---

# External Program Launch & NIR Filtering Bug Fix - Tasks

## Phase 1: Launcher Updates (Core)

- [x] `GeneralCameraLauncher.cs`: Implement `CheckStatusAsync` method
    - Load config, validate path
    - Using `WindowActivationHelper.FindExistingProcess`
    - Update `_process` and fire `StatusChanged`
- [x] `NirCameraLauncher.cs`: Implement `CheckStatusAsync` method
- [x] `Nir2CameraLauncher.cs`: Implement `CheckStatusAsync` method

## Phase 2: ViewModel Integration

- [x] `SystemControlViewModel.cs`: Implement `InitializeLaunchersAsync`
    - Fire-and-forget logic for calling `CheckStatusAsync` on all launchers
- [x] `SystemControlViewModel.cs`: Call `InitializeLaunchersAsync` in Constructor and `RefreshMonitoringAsync`
- [x] `SystemControlViewModel.cs`: Implement Error Handling for NIR Filtering
    - Update `ExecuteToggleNir2Filtering` to await `StartFilteringAsync` result
    - Log error if `success` is false

## Phase 3: Verification

- [x] Manual Check: **Pre-Launch Synchronization**
    1. Close ChronoView.
    2. Open "General Camera" (or dummy app).
    3. Open ChronoView.
    4. Verify button is Green/OFF (Activated).
- [x] Manual Check: **NIR Filtering Error**
    1. Set invalid NIR Monitor path in config.
    2. Click "NIR Filtering ON".
    3. Verify Error Log appears in UI.
    4. Verify button remains Gray/ON (Deactivated).
