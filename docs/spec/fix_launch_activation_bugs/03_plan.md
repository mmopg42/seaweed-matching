---
Task: fix_launch_activation_bugs
Created: 2026-01-12
Status: Approved
Depends On: 01_requirements.md, 02_research.md
---

# External Program Launch & NIR Filtering Bug Fix - Implementation Plan

## 1. Goal Description

Ensure that ChronoView accurately reflects the state of external camera programs immediately upon startup and provides immediate feedback if NIR filtering cannot be enabled, preventing UI state desynchronization.

## 2. Proposed Changes

### 2.1 Launcher Classes (Core)

#### [MODIFY] [GeneralCameraLauncher.cs](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/Core/ProgramLaunching/GeneralCameraLauncher.cs)
#### [MODIFY] [NirCameraLauncher.cs](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/Core/ProgramLaunching/NirCameraLauncher.cs)
#### [MODIFY] [Nir2CameraLauncher.cs](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/Core/ProgramLaunching/Nir2CameraLauncher.cs)

-   **Implement `CheckStatusAsync()`**:
    1.  Load `ApplicationConfiguration` via `_configManager` to retrieve the target program path.
    2.  Validate the path (check for null/whitespace).
    3.  Call `WindowActivationHelper.FindExistingProcess(path)` to attempt to locate the process.
        -   *Note*: This helper already handles `Win32Exception` for permission issues, addressing the risk identified in research.
    4.  If a process is found:
        -   Update `_process` field.
        -   Invoke `StatusChanged(true)`.
        -   Call `StartMonitoring()` to track when it exits.
    5.  If not found:
        -   Invoke `StatusChanged(false)`.

### 2.2 ViewModel Layer

#### [MODIFY] [SystemControlViewModel.cs](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/UI/ViewModels/SystemControlViewModel.cs)

-   **Startup Synchronization**:
    -   Add `InitializeLaunchers()` method.
    -   Call `CheckStatusAsync()` for all 3 launchers (`General`, `Nir1`, `Nir2`).
    -   **Trigger Point**: Call this method in the **Constructor** (fire-and-forget safe execution) and also in `RefreshMonitoringAsync` to allow manual re-sync.
    
-   **NIR Filtering Feedback**:
    -   In `ExecuteToggleNir2Filtering()`:
        -   Await `_nirFilteringService.StartFilteringAsync()`.
        -   **Check the Result**: Capture the `(bool success, string message)` return value.
        -   **On Failure**: 
            -   Log the error via `LogRequested(LogSeverity.Error, ...)` to show it in the UI Log Panel.
            -   Do *not* manually toggle the button state (let the Service's state drive the UI).

## 3. Verification Plan

### 3.1 Code-Level Verification
-   **Launcher Logic**: Verify `CheckStatusAsync` handles `null` config paths gracefully (returns no status change or false).
-   **ViewModel Logic**: Verify `ExecuteToggleNir2Filtering` logs an error when `StartFilteringAsync` returns false.

### 3.2 Manual Verification Scenarios
1.  **"Pre-Launch" Scenario**:
    -   Close ChronoView.
    -   Launch "General Camera" (or a dummy Notepad renamed to match config).
    -   Open ChronoView.
    -   **Expectation**: The "General Camera" button shows "OFF" (Green/Activated) immediately.

2.  **"Invalid Path" Scenario**:
    -   Set NIR Monitoring Path to `Z:\InvalidPath`.
    -   Click "NIR Filtering ON".
    -   **Expectation**: 
        -   Log Panel shows "Error: Monitor path does not exist...".
        -   Button stays "ON" (Gray/Deactivated).

3.  **"Permission" Scenario**:
    -   Run the external program as Administrator.
    -   Run ChronoView as User.
    -   **Expectation**: `WindowActivationHelper` fallback logic works, and the button still shows "Activated".
