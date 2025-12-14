# Startup Flow & Setup Screen Design

## Overview
This document outlines the revised startup flow and the requirements for the new **Setup Screen**. The goal is to introduce an intermediate step between the Splash Screen and the Main Application where external peripheral programs can be launched.

## Startup Flow & Lifecycle
1.  **Splash Screen**: Displayed immediately upon application launch (handled by `Splasher` or `App.xaml.cs`).
2.  **Setup Screen (`SetupWindow`)**:
    -   Instantiated and shown via `App.xaml.cs` after Splash Screen.
    -   **Critical**: `MainWindow` functionality is NOT initialized yet.
3.  **ChronoView (`MainWindow`)**:
    -   Instantiated only upon explicit completion of the Startup phase.
    -   Logic in `App.xaml.cs`:
        ```csharp
        // Pseudo-code
        var setup = new SetupWindow();
        if (setup.ShowDialog() == true) { // "Start" clicked
            var main = new MainWindow();
            main.Show();
        } else {
            Shutdown();
        }
        ```

## Setup Screen Requirements

### UI Components (SetupWindow.xaml)
-   **Window Properties**:
    -   `WindowStartupLocation="CenterScreen"`
    -   `ResizeMode="NoResize"` (or `CanResize="False"`)
    -   `Width="1024"`, `Height="768"` (Adjust based on design needs).
-   **Layout**:
    -   Header Area: Title "Setup".
    -   launchers Area:
        -   **General Camera**: 1 Button.
        -   **NIR Camera**: 2 Buttons (NIR 1, NIR 2).
    -   **Status Bar**:
        -   `TextBlock` or overlay to display temporary messages (Error/Status).
    -   Action Area (Bottom):
        -   **Start Button**: "Start Monitoring". Always Enabled. `IsDefault="True"` (Respond to Enter key).

### Logic & ViewModel (SetupWindowViewModel)
-   **Commands**:
    -   `LaunchProgramCommand(string programParam)`:
        -   **Async** execution.
        -   **Logic**:
            -   Resolve path from `AppConfig` (e.g., `Config.GeneralCamPath`, `Config.Nir1Path`).
            -   **Check**: If path empty -> `StatusMessage = "Path not set for [Program]"`; Log Warning; Return.
            -   **Try**: `Process.Start(path)`.
            -   **Catch**: `StatusMessage = "Failed to launch: " + ex.Message`; Log Error.
            -   **Success**: `StatusMessage = "Launched [Program]"`; Log Info.
    -   `StartCommand`:
        -   **Logic**:
            -   Log "User started monitoring".
            -   Set `DialogResult = true` (closes window).

### Logging
-   Use `System.Diagnostics.Trace` or existing Logger.
-   Events: Window Load, Launch Attempt (w/ Result), Start Click.

### Visual Design
-   Follow "Setup Screen UI Design".
-   Ensure consistent fonts and colors.
-   **Color Palette**:
    -   **Primary Red / Accent**: `#EB1E24` (Use for Highlights/Warnings/Important Buttons).
