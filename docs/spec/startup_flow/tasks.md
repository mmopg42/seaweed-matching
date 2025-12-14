# Startup Flow Implementation Tasks

Based on `design.md` and `requirements.md`.

## 1. Setup Infrastructure
- [ ] **Create SetupWindow**
    - [ ] Create `SetupWindow.xaml` and code-behind.
    - [ ] Create `SetupWindowViewModel.cs` implementing `INotifyPropertyChanged`.
    - [ ] Register `SetupWindow` and ViewModel in `App.xaml.cs` or DI container.
- [ ] **Logging & Config**
    - [ ] Ensure logging mechanism is available (e.g., `Trace` or `Logger`).
    - [ ] Ensure `AppConfig` has properties for program paths.

## 2. UI Implementation (SetupWindow.xaml)
-   **Window Properties**: CenterScreen, NoResize, Size ~1024x768.
- [ ] **Layout Structure**
    - [ ] Header/Title area ("Setup").
    - [ ] **Launchers Area**:
        - [ ] Button: "General Camera".
        - [ ] Button: "NIR Camera 1".
        - [ ] Button: "NIR Camera 2".
    -   **Status Area**:
        - [ ] Add `TextBlock` (or Status Bar) for showing messages (e.g., "Launching...", "Error: ...").
    -   **Action Section**:
        - [ ] Button: "Start Monitoring" (`IsDefault=True`).
        - [ ] Ensure "Start" is **Always Enabled**.

## 3. Application Logic & ViewModel
- [ ] **ViewModel Logic (`SetupWindowViewModel`)**
    - [ ] Implement `LaunchProgramCommand` (Async).
        - [ ] Check configuration path.
        - [ ] Update Status: "Path not set" (if empty) or "Launching...".
        - [ ] Try `Process.Start`.
        - [ ] Catch Exception -> Status: "Error: [Msg]".
        - [ ] Debounce/Disable button during launch? (Optional but good).
    - [ ] Implement `StartCommand`.
        - [ ] Log "Start Monitoring".
        - [ ] Close Window (`DialogResult = true`).
- [ ] **Startup Sequence (`App.xaml.cs`)**
    - [ ] Remove `StartupUri` from `App.xaml` (if present).
    - [ ] Override `OnStartup`:
        - [ ] Show Splash (existing logic).
        - [ ] Create & Show `SetupWindow` (Modal/Dialog).
        - [ ] If `SetupWindow` returns `true` (Start clicked) -> Create & Show `MainWindow`.
        - [ ] Else -> Launch Shutdown.

## 4. Verification
- [ ] **Manual Test Scenarios**
    -   **Scenario 1: Happy Path**
        -   Launch App -> Splash -> Setup.
        -   Click Gen Cam -> Success Status/Log.
        -   Click NIR Buttons -> Success Status/Log.
        -   Click Start -> MainWindow opens.
    -   **Scenario 2: Path Not Set**
        -   Launch App -> Setup.
        -   Click Launch Button (with empty config) -> Message "Path not set" appears.
    -   **Scenario 3: Launch Failure**
        -   Configure invalid path.
        -   Click Launch Button -> Message "Error: ..." appears. Application does NOT crash.
    -   **Scenario 4: Direct Start**
        -   Launch App -> Setup.
        -   Click Start immediately -> MainWindow opens.
