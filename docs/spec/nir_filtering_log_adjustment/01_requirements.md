# Requirements: NIR Filtering Log Adjustment

## 1. Overview
The goal is to refine the logging behavior of the NIR Filtering Service in the UI Log Panel. Currently, it's unclear what is being logged to the UI. The user wants specific visibility for On/Off status while keeping detailed processing logs in the background.

## 2. Functional Requirements
1.  **UI Log Panel Visibility**:
    *   **Filtering ON event**: Must be displayed in the UI Log Panel as an `Information` level log.
    *   **Filtering OFF event**: Must be displayed in the UI Log Panel as an `Information` level log.
    *   **Detailed Processing**: File detection, movement, deletion, and criteria checks must be logged as `Debug` level (or generic `Information` that is NOT forwarded to the UI Panel, depending on implementation).
2.  **Log Level Separation**:
    *   The system must distinguish between "User-facing structural events" (On/Off) and "High-frequency operational logs" (Processing).
    *   Only "User-facing" events should appear in the main UI log panel to prevent clutter.

## 3. Constraints
*   Do not change the underlying logic of `NirFilteringService`.
*   Ensure that `Debug` logs are still written to the disk file (which `App.xaml.cs` configures).
*   Maintain existing architecture where `SystemControlViewModel` orchestrates the toggle.

## 4. User Scenario
*   User clicks "NIR Filtering ON".
*   UI Log Panel shows: `[Info] System: NIR Filtering Started`.
*   User copies files into the monitor folder.
*   Application processes files.
*   UI Log Panel **does not** scroll rapidly with "File detected", "Moved", etc.
*   User checks log file (`ChronoView_Debug_...log`): sees all details.
*   User clicks "NIR Filtering OFF".
*   UI Log Panel shows: `[Info] System: NIR Filtering Stopped`.
