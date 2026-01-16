# Requirements: Auto-Update Monitor Path Date

## 1. Overview
Automatically detect and update date patterns (YYYY/MM/DD or YYYY\MM\DD) in the `Nir2FilterMonitorPath` setting to the current date. This ensures the monitor path always points to the correct daily folder without manual intervention.

## 2. Functional Requirements
*   **Pattern Detection**:
    *   Target Field: `Nir2FilterMonitorPath` (in Settings Dialog)
    *   Pattern: `\d{4}[\\/]\d{2}[\\/]\d{2}` (e.g., `2025/01/09`, `2025\01\09`)
*   **Auto-Update Logic**:
    *   If the path contains the date pattern, replace it with today's date (formatted as `yyyy\MM\dd` to match Windows path style, or preserving original separator if possible).
    *   The replacement should happen when the Settings Dialog is **initialized/opened**. This way, the user sees the updated path immediately.
    *   Also consider applying it on **Save**? -> User feedback implies "entering" or "having", but functionally, updating on Open is most useful so it's ready-to-use. If it's stale, it updates.

## 3. User Interaction
*   User opens Settings > External Programs.
*   The "Monitor Path" field automatically shows the path with **Today's Date**, replacing any old date found in the configuration.
*   User can manually change it if needed, but the auto-update happens on load.

## 4. Technical Details
*   Location: `SettingsDialogViewModel.Initialize()` or property getter/setter?
*   `Initialize()` is safer to avoid side effects during typing.
*   Regex: `new Regex(@"\d{4}[\\/]\d{2}[\\/]\d{2}")`

## 5. Constraints
*   Only affects `Nir2FilterMonitorPath`.
*   Should not affect other parts of the path.
