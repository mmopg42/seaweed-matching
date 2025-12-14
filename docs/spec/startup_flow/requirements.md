# Startup Flow Requirements

## 1. Introduction
This document defines the requirements for the application's startup sequence, specifically the introduction of a Setup Screen to manage external program execution before the main application starts.

## 2. Startup Sequence & Lifecycle
-   **Flow**: Splash Screen -> **Setup Screen** -> ChronoView Main Window.
-   **Lifecycle Management**:
    -   The `MainWindow` must **not** be instantiated until the "Start" button on the Setup Screen is clicked.
    -   The application must start by showing the Splash Screen, then transition to the Setup Screen.
    -   Upon clicking "Start" in Setup Screen: Close Setup Screen -> Initialize & Show MainWindow.

## 3. Setup Screen Requirements

### 3.1. External Program Launchers
The Setup Screen must provide controls to launch the following external applications:
1.  **General Camera Program**: 1 Button ("General Camera").
2.  **NIR Camera Programs**: 2 Buttons ("NIR Camera 1", "NIR Camera 2").

**Behavior & Error Handling**:
-   **Path Not Configured**: If a button is clicked but no path is configured, display a user-friendly message (e.g., "Path not set") in a Status Area or Message Box. Do not crash.
-   **Execution Failure**: If the program fails to start (e.g., file not found, permission denied), capture the exception and display an error message.
-   **Asynchronous Execution**: Program launching must be asynchronous to prevent UI freezing.
-   **Double-Click Prevention**: Buttons should be temporarily disabled or debounced to prevent launching multiple instances accidentally.

### 3.2. Navigation Control
1.  **Start Button**:
    -   **Label**: "Start Monitoring" (or "Start").
    -   **State**: **Always Enabled**.
    -   **Behavior**:
        1.  Log the "Start" action.
        2.  Close Setup Screen.
        3.  Open ChronoView Main Window.

### 3.3. UI/UX Policies
-   **Reference Design**: `C:\workspace\seaweed\gui_kiro\Setup Screen UI Design`.
-   **Window Settings**:
    -   **Position**: Center Screen.
    -   **Resize**: Fixed size (CanResize=False) or MinSize defined to prevent layout breaking.
    -   **Status Feedback**: A distinct area (e.g., Status Bar or Toast) to show success/error messages (e.g., "General Camera Started", "Error: File not found").
-   **Accessibility**:
    -   Support logical Tab sequencing (Buttons -> Start Button).
    -   Support 'Enter' key to trigger "Start" if focused.

## 4. Pending Functionality (Future Scope)
-   **Process Monitoring**: Active monitoring of external processes is NOT required for this phase.
-   **Localization**: English labels for now.

## 5. Logging Requirements
-   Log entry when Setup Screen opens.
-   Log entry when any Launcher button is clicked (include success/failure status).
-   Log entry when "Start" button is clicked.
