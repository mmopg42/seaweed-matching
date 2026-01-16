---
Task: fix_launch_activation_bugs
Created: 2026-01-12
Status: Draft
Depends On: 03_plan.md
---

# External Program Launch & NIR Filtering Bug Fix - Design

## 1. System Architecture

The fix involves enhancing the existing `Launcher` system and `SystemControlViewModel` to support two-way synchronization:
1.  **Pull (Startup)**: ViewModel asks Launchers to check current process state.
2.  **Push (Runtime)**: Launchers notify ViewModel of state changes (existing functionality).

Additionally, it enforces strict error checking for the NIR Filtering service.

## 2. Component Design

### 2.1 Launcher Classes (`GeneralCameraLauncher`, `NirCameraLauncher`, `Nir2CameraLauncher`)

All three launcher classes will implement a similar pattern (or potentially interface method if we refactored, but keeping current separate class structure):

```csharp
public async Task CheckStatusAsync()
{
    // 1. Get Path from Configuration
    var config = _configManager.LoadConfiguration<ApplicationConfiguration>();
    var path = config?.ExternalProgramSettings?.GeneralCameraProgramPath; // Specific to each launcher

    // 2. Validate Path
    if (string.IsNullOrWhiteSpace(path)) 
    {
        StatusChanged?.Invoke(this, false);
        return;
    }

    // 3. Find Process
    // Using existing helper which handles permissions
    var existingProcess = WindowActivationHelper.FindExistingProcess(path);

    if (existingProcess != null && !existingProcess.HasExited)
    {
        _process = existingProcess;
        StatusChanged?.Invoke(this, true); // Fire event to update UI
        StartMonitoring(); // Begin watching for exit
    }
    else
    {
        // Ensure UI is reset if not found
        StatusChanged?.Invoke(this, false);
    }
}
```

### 2.2 SystemControlViewModel

The ViewModel acts as the orchestrator for initialization and error reporting.

#### 2.2.1 Initialization

```csharp
private async Task InitializeLaunchersAsync()
{
    // Simply fire-and-forget checks for all
    await Task.WhenAll(
        _generalCameraLauncher.CheckStatusAsync(),
        _nirCameraLauncher.CheckStatusAsync(),
        _nir2CameraLauncher.CheckStatusAsync()
    );
}

// Call in Constructor or StartMonitoringAsync
InitializeLaunchersAsync();
```

#### 2.2.2 NIR Filtering Error Handling

```csharp
private async void ExecuteToggleNir2Filtering()
{
    if (_nirFilteringService.IsFilteringActive) 
    {
        _nirFilteringService.StopFiltering();
    }
    else 
    {
        // CRITICAL CHANGE: capture result
        (bool success, string message) = await _nirFilteringService.StartFilteringAsync();
        
        if (!success)
        {
            // Report error to user via log
            LogRequested?.Invoke(LogSeverity.Error, "System", $"NIR Filtering failed: {message}");
            // Do NOT rely on StatusChanged here, ensuring button stays OFF
        }
    }
}
```

## 3. Data Flow

### 3.1 Startup Flow
1.  `SystemControlViewModel` instantiated.
2.  Constructor calls `InitializeLaunchersAsync()`.
3.  Each Launcher loads config -> calls `WindowActivationHelper`.
4.  If process found:
    -   Launcher updates internal `_process`.
    -   Launcher fires `StatusChanged(true)`.
    -   ViewModel event handler receives `true` -> Updates buttons (Green/Deactivated-Text).

### 3.2 NIR Filtering Error Flow
1.  User clicks "NIR Filtering ON".
2.  `ExecuteToggleNir2Filtering` calls Service.
3.  Service detects invalid path -> returns `(false, "Path not found")`.
4.  ViewModel receives `false` -> Logs Error.
5.  Button state remains unchanged (User sees it didn't turn on).

## 4. UI/UX Impact

-   **Seamless Startup**: Users will see correct buttons immediately if they opened cameras first.
-   **Clear Feedback**: Users will know *why* NIR filtering didn't start (e.g., specific path error in log).
