# Plan: NIR Filtering Log Adjustment

## 1. Goal
Ensure UI Log Panel displays NIR Filtering On/Off status as "Information" while hiding detailed file operations (keeping them as "Debug" in file logs).

## 2. Approach
*   **SystemControlViewModel**: Subscribe to `_nirFilteringService.StatusChanged` and explicitly log "NIR Filtering Activated/Deactivated" to the UI Log Panel via `LogRequested` event.
*   **NirFilteringService**: Ensure internal logs are `LogDebug` or `LogInformation` (standard logger behavior), but they won't reach the UI unless forwarded.
    *   *Self-Correction*: The current `NirFilteringService` logging is fine as is. It logs to the underlying logger. The UI Panel only sees what `MainWindowViewModel` (subscriber) receives. Since `NirFilteringService` is not connected to `MainWindowViewModel`'s UI log list, its logs remain in the file/console only, which is exactly what we want for "rest of the logs". We only need to bridge the "Status" logs.

## 3. Changes

### 3.1. `SystemControlViewModel.cs`
*   In `UpdateNir2FilteringStatus` (or where the event is handled):
    *   Add logic to trigger `LogRequested?.Invoke(LogSeverity.Info, "System", ...)` when status changes.
    *   Need to use a local field to detect *change* vs *initial load* to avoid spamming log on startup?
        *   The event handler fires when `StatusChanged` event is raised.
        *   `NirFilteringService` raises `StatusChanged` only on `StartFilteringAsync` (success) and `StopFiltering`.
        *   Initial state update in constructor doesn't come from event.
        *   So, just logging inside the event handler lambda is sufficient.

## 4. Verification
*   Build and Run.
*   Toggle filtering on/off -> check UI Log.
*   Check that no file processing logs appear in UI Log.
