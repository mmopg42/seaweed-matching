# Implementation Plan: Auto-Update Monitor Path Date

## Goal Description
Automatically update the date portion (`YYYY/MM/DD` or `YYYY\MM\DD`) of the `Nir2FilterMonitorPath` to the current date when the Settings Dialog is opened.

## User Review Required
> [!NOTE]
> The update happens silently when opening the dialog. If the user *wants* to keep an old date for some reason, they will have to manually change it back *after* the auto-update happens, or we might overwrite their intentional setting if they open settings just to check.
> ** assumption**: The primary use case is "Real-time monitoring", so "Today" is almost always the desired state.

## Proposed Changes

### ChronoView
#### [MODIFY] [SettingsDialogViewModel.cs](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/UI/ViewModels/SettingsDialogViewModel.cs)
*   Modify `Initialize()` method (or add logic to constructor/loading phase).
*   Add private method `UpdatePathWithCurrentDate(string path)`.
    *   Regex: `\d{4}[\\/]\d{2}[\\/]\d{2}`
    *   Replacement: `DateTime.Now.ToString("yyyy\\MM\\dd")` (matching system separator or backslash preferred for Windows).
*   Apply this logic to `Nir2FilterMonitorPath`.

## Verification Plan

### Automated Tests
*   Unit test for `UpdatePathWithCurrentDate` logic (if extracted to a helper).

### Manual Verification
1. Set `Nir2FilterMonitorPath` to `C:\Data\2024\01\01\Monitor` in `config.json` (or via UI and restart).
2. Open Settings Dialog.
3. Verify that `Nir2FilterMonitorPath` field now shows `C:\Data\2025\01\09\Monitor` (assuming today is 2025-01-09).
4. Verify that other parts of the path remain unchanged.
