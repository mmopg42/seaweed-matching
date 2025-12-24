# External Program Launching Module

## Overview
This module handles the launching, monitoring, and lifecycle management of external applications required by ChronoView, specifically the General Camera (Normal Camera) software and the NIR (Near-Infrared) sensor program.

## Key Components

### 1. GeneralCameraLauncher (`ChronoView.Core.ProgramLaunching`)
**Responsibility**: Manages the "Normal Camera" executable.

- **Functionality**:
  - Validates the executable path defined in `ApplicationConfiguration`.
  - Checks if the process is already running to prevent duplicate instances.
  - Launches the process using `System.Diagnostics.Process`.
  - Monitors the process lifecycle (start/exit) using `EnableRaisingEvents`.
  - Exposes `StatusChanged` event to notify the application of state changes (Activated/Deactivated).
  - Implements `IDisposable` to clean up resources.

### 2. NirCameraLauncher (`ChronoView.Core.ProgramLaunching`)
**Responsibility**: Manages the NIR sensor executable(s).

- **Functionality**:
  - Supports launching multiple NIR program instances (NIR1, NIR2) although currently focused on NIR1 tracking.
  - Validates paths for NIR programs.
  - Manages process launching and exit monitoring for each instance independently.
  - Exposes `StatusChanged` event that provides the index of the NIR program and its new state.
  - Tracking Policy: Primarily monitors NIR1 for the global "NIR Camera" status displayed in the UI.

## Integration Points

### ViewModels
- **`SetupWindowViewModel`**: 
  - Uses launchers to execute the actual "Run" commands triggered by the UI buttons.
  - Delegates the complexity of process management to these launchers.
- **`MainWindowViewModel`**: 
  - Subscribes to `StatusChanged` events from both launchers.
  - Updates the `GeneralCameraStatus` and `NirCameraStatus` properties for UI display (green/red indicators).
  - Ensures launchers are properly disposed when the application exits.

### Dependency Injection
- Launchers are registered as **Singleton** services in `App.xaml.cs`. This ensures that the process state is maintained consistently across the entire application lifecycle (e.g., status monitored in MainWindow reflects actions taken in SetupWindow).

## Verification Commands
<!-- VERIFY: Check if Launchers are registered -->
`grep "services.AddSingleton<GeneralCameraLauncher>" ChronoView/App.xaml.cs`
`grep "services.AddSingleton<NirCameraLauncher>" ChronoView/App.xaml.cs`

<!-- VERIFY: Check usages in ViewModels -->
`grep "GeneralCameraLauncher" ChronoView/UI/ViewModels/SetupWindowViewModel.cs`
`grep "NirCameraLauncher" ChronoView/UI/ViewModels/MainWindowViewModel.cs`
