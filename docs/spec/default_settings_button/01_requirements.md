# Requirements: Default Settings Button

## 1. Overview
The user wants to add a "Default Settings" feature to the Settings Dialog. This will allow users to quickly reset the configuration to a factory default state. A critical requirement is that all folder and program paths must be reset to an empty state for safety.

## 2. Goals
1.  **Default Button**: Add a button in the Settings Dialog to trigger the reset.
2.  **Safety Reset**: When reset, all **Folder Paths** and **Program Paths** must be set to **Empty/Blank**.
3.  **Standard Defaults**: Other settings (window size, timings, etc.) should reset to their standard default values.

## 3. Scope
-   **Target File**: `ChronoView/UI/ViewModels/SettingsDialogViewModel.cs`
-   **Target File**: `ChronoView/UI/Views/SettingsDialog.xaml`
-   **New File**: `ChronoView/Core/Configuration/DefaultConfiguration.cs`

## 4. Detailed Requirements

### 4.1. Default Settings Behavior
-   **UI Change**: Add a "Default" (기본값) button to the Settings Dialog.
-   **Behavior**:
    -   When clicked, all settings in the dialog are populated with default values.
    -   **Path Rule**: All **Folder Paths** and **Program Paths** must be set to Empty/Blank by default. This overrides any internal default logic that might auto-fill paths (e.g., "D:/Data").
    -   **Other Values**: Use the standard default values defined in the code.
-   **Implementation Requirement**:
    -   The default values should be defined in a **separate script/file** (e.g., `DefaultConfiguration.cs`) to allow for easy modification and separation of concerns.

## 5. Constraints & Risks
-   **Path Safety**: If the system auto-fills paths (like `BasePath` = "D:/Data"), this must be explicitly overridden to be blank. The user specifically requested blank paths as a safety measure.
