---
Task: fix_launch_activation_bugs
Created: 2026-01-12
Status: Draft
Depends On: 01_requirements.md
---

# External Program Launch & NIR Filtering Bug Fix - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: Can a running process be detected by path alone? | Yes, using WMI or detailed Process Module inspection (with admin rights caveat). | High |
| Q2: Does `NirFilteringService` return failure reason? | Yes, `StartFilteringAsync` returns `(bool, string)`. | High |
| Q3: Can ViewModel detect initial state? | Yes, but currently no method exists to do so on startup. | High |

## 2. Detailed Findings

### 2.1 Q1: Can a running process be detected by path alone?

**Method**: 
- Reviewed `WindowActivationHelper.FindExistingProcess` in `ChronoView/Helpers/WindowActivationHelper.cs`.
- Checked `Process.GetProcessesByName` capabilities.

**Findings**:
- `FindExistingProcess` already iterates through processes by name and attempts to match `MainModule.FileName` with the configured path.
- It handles `Win32Exception` (Access Denied) gracefully by falling back to `MainWindowHandle` check if path access fails.
- This logic is sufficient for "Setup Sync" if called at startup.

**Evidence**:
- `WindowActivationHelper.cs` Lines 67-100 implement robust process finding.

**Conclusion**: The existing helper method is sufficient. The bug is simply that it's not being called during initialization.

---

### 2.2 Q2: Does `NirFilteringService` return failure reason?

**Method**:
- Reviewed `ChronoView/Core/ProgramLaunching/NirFilteringService.cs`.

**Findings**:
- `StartFilteringAsync` signature is `public async Task<(bool Success, string Message)> StartFilteringAsync()`.
- It performs validation checks (Monitor path empty, Monitor path exists, Destination path exists) and returns `(false, "Error message")` if any check fails.

**Evidence**:
- `NirFilteringService.cs` Lines 46-105.

**Conclusion**: The service correctly reports failure. The bug is in the ViewModel acting on this return value.

---

### 2.3 Q3: Can ViewModel detect initial state?

**Method**:
- Reviewed `ChronoView/UI/ViewModels/SystemControlViewModel.cs`.

**Findings**:
- Constructor initializes Launchers.
- Events (`StatusChanged`) are subscribed.
- **Missing**: There is no call to any "Initialize" or "Sync" method in the constructor or `StartMonitoringAsync` to check *current* state of external programs. It relies entirely on user interaction or future status changes.

**Evidence**:
- `SystemControlViewModel.cs` Constructor (Lines 64-85).

**Conclusion**: We need to add an initialization step to `SystemControlViewModel` that queries each Launcher for its current status.

---

## 3. Code Analysis

### 3.1 Relevant Existing Code

| File | Component | Relevance | Notes |
|------|-----------|-----------|-------|
| `GeneralCameraLauncher.cs` | `Launcher` | Needs init method | Currently only checks process on `LaunchAsync` |
| `SystemControlViewModel.cs` | `ViewModel` | Needs startup logic | Missing sync with current system state |
| `NirFilteringService.cs` | `Service` | Returns status | Return value currently ignored by VM |

### 3.2 Impact Analysis

| Existing Component | Potential Impact | Risk Level |
|--------------------|------------------|------------|
| `SystemControlViewModel` | Startup performance | Low |
| `WindowActivationHelper` | Process enumeration | Low (Fast enough for startup) |

## 4. Recommendations

### Primary Recommendation

1.  **Add `CheckStatus()` to IDeviceLauncher/Launchers**: Explicitly expose a method to scan for the process without launching it.
2.  **Call `CheckStatus()` in ViewModel**: In `SystemControlViewModel.StartMonitoringAsync` (or constructor), iterate through all launchers and call `CheckStatus()` to sync UI.
3.  **Handle Validation Errors**: In `ExecuteToggleNir2Filtering`, check the `(bool success, string message)` result. If false, show a `MessageBox` or Log Error.

### Risks

-   **Process Access Rights**: If the external program runs as Admin and ChronoView as User, `MainModule.FileName` might be inaccessible.
-   *Mitigation*: The existing `WindowActivationHelper` has try-catch blocks for this exact scenario. We should rely on it.

## 5. References

-   [WindowActivationHelper.cs](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/Helpers/WindowActivationHelper.cs)
-   [SystemControlViewModel.cs](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/UI/ViewModels/SystemControlViewModel.cs)
