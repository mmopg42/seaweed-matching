# Research: NIR Filtering Log Adjustment

## 1. Current Architecture
*   **Logging Service**: `ILogger<T>` is used throughout the application.
*   **UI Logging**: `MainWindowViewModel` listens to `LogRequested` events from `Dashboard`, `Operations`, and `Control` (SystemControlViewModel).
    *   It also allows `MonitoringOrchestrator` to inject logs via `SetUILog`.
*   **NIR Filtering**:
    *   `NirFilteringService.cs`: Uses `ILogger`. `LogInformation` is used for Start/Stop. `LogDebug` is used for file operations.
    *   **PROBLEM**: `NirFilteringService` has no direct connection to `MainWindowViewModel`'s `LogRequested`. Its `ILogger` writes to Console/Debug/File, but not the UI.

## 2. Analysis of Solutions

### Option A: Event Forwarding in `SystemControlViewModel`
*   **Mechanism**: `SystemControlViewModel` already subscribes to `NirFilteringService.StatusChanged`.
*   **Implementation**: In the event handler, trigger `LogRequested` with `LogSeverity.Info`.
*   **Pros**: 
    *   No changes to `NirFilteringService` structure.
    *   Keeps UI logic in ViewModel.
    *   Easy to distinguish "User Action" vs "Internal Service Log".
*   **Cons**: None significant.

### Option B: Inject `IMonitoringOrchestrator` into `NirFilteringService`
*   **Mechanism**: Pass orchestrator and use `SetUILog` equivalent.
*   **Pros**: Direct logging.
*   **Cons**: Circular dependency risk. `NirFilteringService` is a lower-level service; Orchestrator is a higher-level coordinator. Bad layering.

### Option C: Custom Logger Provider
*   **Mechanism**: Create a `UILoggerProvider` that routes all `LogInformation` to the UI.
*   **Pros**: Automatic.
*   **Cons**: Too noisy. We only want specific logs. Hard to filter generic `Info` logs from other services.

## 3. Selected Approach: Option A (ViewModel Forwarding)
We will modify `SystemControlViewModel` to explicitly log to the UI when the status changes.

*   **Status Change**: `SystemControlViewModel` captures `StatusChanged` event.
*   **Log Generation**: Calls `LogRequested?.Invoke(LogSeverity.Info, "System", "NIR Filtering [Activated/Deactivated]")`.
*   **Detail Logs**: `NirFilteringService` keeps using `_logger.LogDebug` for file details. These go to the file but not the UI (since UI only shows what is explicitly requested via events).

## 4. Verification Plan
1.  Run app.
2.  Toggle NIR Filtering.
3.  Check UI Log Panel -> Should see "NIR Filtering Activated".
4.  Drop file -> Check UI Log Panel -> Should NOT see file log.
5.  Check File Log -> Should see file log.
