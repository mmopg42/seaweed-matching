# Requirements: Setup Window UI Refinement

## 1. Overview
The user wants to refine the UI behavior in the Setup Window. The goal is to completely remove the status message feedback mechanism (the yellow/orange info bar) that appears on user interactions (launching cameras, toggling filters, etc.) and update the visual style of the NIR filtering status text.

## 2. Goals
1.  **Remove Status Message Feature**: The Setup Window should **never** display the visual status message bar (e.g., "Launching...", "Stopped", "Error...", etc.) at the bottom of the screen. This feature should be removed entirely from the View and ViewModel.
2.  **Update NIR Filtering Status Style**: The NIR filtering status text should be displayed in **White** (both Activated/Deactivated), because the Setup UI uses a red background/button where white has the best contrast.
3.  **3-State NIR Filtering Status**: The NIR filtering status should support a simple 3-state display: **Deactivated / Processing... / Activated** (text remains white for all states).

## 3. Scope
-   **Target File**: `ChronoView/UI/ViewModels/SetupWindowViewModel.cs`
-   **Target File**: `ChronoView/UI/Views/SetupWindow.xaml`
-   **Target Feature**: Setup Window Status Reporting and NIR Status Styling.

## 4. Detailed Requirements

### 4.1. Remove Status Message Mechanism
-   **Current Behavior**: The `ShowStatus` method uses `StatusMessage` and `StatusVisibility` properties to display a temporary message in a Border in the XAML.
-   **Desired Behavior**:
    -   Remove the status message Border/Controls from `SetupWindow.xaml`.
    -   Remove `StatusMessage` and `StatusVisibility` properties from `SetupWindowViewModel.cs`.
    -   Remove `ShowStatus` method and all calls to it.
    -   Ensure asynchronous operations (launching, toggling) still run but without visual text feedback in the Setup Window (logging should remain).

### 4.2. UI Styling (NIR Filter)
-   **Current Behavior**: The status text changes color (Activated = Green, Deactivated = White).
-   **Desired Behavior**:
    -   Status text is always **White** (Activated/Deactivated/Processing...).
    -   Introduce a **Processing...** intermediate status while the filtering start operation is running.



## 5. Constraints & Risks
-   **User Feedback**: Removing the status message means the user won't get visual confirmation of actions (like "Launching Camera...") directly in the window, except for side effects (windows opening) or the "Activated/Deactivated" text change. This is explicitly requested.
-   **Error Visibility**: Errors were previously shown via `ShowStatus`. Now errors will only be logged (unless we decide to use a MessageBox, but the request was specifically to remove *this* message feature. We will stick to logging per request to "delete this feature").

## 6. Success Metrics
-   Clicking any "Launch" or "Toggle" button in Setup Window does **not** show the status bar.
-   The "Activated" text for NIR filter appears in **White**.
