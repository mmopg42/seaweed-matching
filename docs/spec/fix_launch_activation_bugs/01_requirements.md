---
Task: fix_launch_activation_bugs
Created: 2026-01-12
Status: Draft
Summary: Fix bugs related to external program launch synchronization and NIR filtering activation feedback
---

# External Program Launch & NIR Filtering Bug Fix - Requirements

## 1. Goal

### 1.1 Primary Goal

Address two critical bugs identified in the previous implementation (`launch_and_activate_external_programs`) to improve system stability and user experience:

1.  **Startup Synchronization**: When ChronoView starts (or enters setup mode), if external programs (General Camera, NIR 1, NIR 2) are already running, the system must automatically detect them and synchronize the UI button state to 'OFF' (indicating "Activated/Running").
2.  **NIR Filtering Feedback**: When the user clicks the "NIR Filtering ON" button, if the service fails to start (e.g., due to invalid paths), the system must provide clear error feedback and NOT toggle the UI to the "Activated" state.

### 1.2 Success Criteria

- [ ] **Startup Detection**: On application startup or refresh, any already running external camera programs are immediately detected, and their corresponding buttons/status indicators show "Activated".
- [ ] **Filtering Error Handling**: If NIR filtering fails to start (e.g., `StartFilteringAsync` returns failure), the button remains in the "Deactivated" state, and an error message is logged to the system log.
- [ ] **UI-State Consistency**: The `StatusChanged` events in Launcher classes are correctly propagated to the ViewModel, ensuring the UI always reflects the actual process state.

## 2. Bug Analysis

### 2.1 Bug 1: No Connection on Setup (Startup Sync)

-   **Symptom**: In the Setup environment, even if camera programs are already running, ChronoView shows them as 'Deactivated'.
-   **Cause**: Currently, `SystemControlViewModel` and Launcher classes only check for process existence when the user *clicks* the launch button (`LaunchAsync`). There is no initial check (`Constructor` or `StartMonitoring`) to bind to existing processes.
-   **Resolution**: Implement a `CheckStatus()` method in Launchers and call it during `SystemControlViewModel` initialization to bind to any pre-existing processes.

### 2.2 Bug 2: NIR Filtering Toggle Fail

-   **Symptom**: Clicking the NIR Filtering 'ON' button does nothing (UI does not change, filtering does not start).
-   **Cause**:
    1.  `NirFilteringService.StartFilteringAsync` returns `(false, ErrorMessage)` due to configuration errors (e.g., invalid path).
    2.  `SystemControlViewModel.ExecuteToggleNir2Filtering` ignores this return value and takes no action.
    3.  Since the service failed to start, the `StatusChanged` event is not fired, so the UI status update logic is never triggered.
-   **Resolution**: The ViewModel must check the return value of `StartFilteringAsync` and explicitly handle failure cases by logging errors and alerting the user.

## 3. Requirements Details

### 3.1 Launcher Class Updates

-   **Method Addition**: Add `InitializeStatus()` or `CheckStatusAsync()` to `GeneralCameraLauncher`, `NirCameraLauncher`, and `Nir2CameraLauncher`.
    -   This method will search for the process based on the configured file path.
    -   If found, it initializes the `_process` field and fires `StatusChanged`.

### 3.2 SystemControlViewModel Updates

-   **Initialization Logic**: Call the status check methods of all Launchers in the ViewModel constructor or `StartMonitoringAsync`.
-   **Error Handling**: In `ExecuteToggleNir2Filtering` and `ExecuteLaunchAsync`, strictly check return values (Success/Failure) and log errors with `LogSeverity.Error` if the operation fails.

### 3.3 Configuration Verification

-   Ensure that `ExternalProgramSettings` validation logs warning messages if paths are missing or invalid during the initial check.

## 4. Dependencies

-   Utilizes existing `WindowActivationHelper` for process detection.
-   Depends on `ApplicationConfiguration` for file paths.

## 5. Risks

-   **Permission Issues**: Identifying processes started by other users (or as Admin) might still fail even with `WindowActivationHelper`. The existing `try-catch` block in `FindExistingProcess` should be verified for robustness.
