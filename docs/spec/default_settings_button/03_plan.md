# Implementation Plan - No Default Paths Rule

## Goal Description
The user wants to enforce a strict "No Default Paths" rule.
1.  **Missing Config**: If configuration is lost/init, paths must be `""` (Blank).
2.  **Default Button**: Resetting to defaults must **PRESERVE** existing paths (not clear them, not reset them to D:/Data).
3.  **Code Cleanup**: Remove any logic that auto-populates paths (e.g., `BasePath = "D:/Data"` must go).

## Proposed Changes

### 1. Model Cleanup [ApplicationConfiguration.cs]
-   **Remove Default BasePath**: Change `BasePath` default from `"D:/Data"` to `""`.
-   **Remove Auto-Fill Logic**: Ensure no property initializers set default paths (already checked, mostly good, but BasePath is the culprit).

### 2. ViewModel Logic Update [SettingsDialogViewModel.cs]
-   **Remove Auto-Quarantine Logic**: `LoadFromConfiguration` has a block that sets `DeleteQuarantinePath` to `Trash` if empty. This must be REMOVED.
    -   *Why*: User wants "No Default". If it's empty, it stays empty until user sets it.
    -   *Risk*: Application might need a check before deleting files if path is empty (it should already be safe, but we'll verify).
-   **Verify Reset Logic**: The current implementation of `ExecuteResetToDefaults` (preserving paths) is correct based on the latest request.

## Tasks
1.  [MODIFY] `ApplicationConfiguration.cs`: Set `BasePath = ""`.
2.  [MODIFY] `SettingsDialogViewModel.cs`: Remove auto-fill for `DeleteQuarantinePath`.
3.  [VERIFY] Manual test:
    -   Delete config file -> Start App -> Check paths are empty.
    -   Set paths -> Click Default -> Paths preserved, other settings reset.
