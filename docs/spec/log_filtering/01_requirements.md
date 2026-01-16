# Log Filtering Requirements

## 1. Problem Statement
The current log system displays a "Source" column (e.g., "System", "GroupManager") which is not optimal for users managing multiple production lines. Users want to see logs filtered by the active production line ("Line 1", "Line 2") to reduce noise and focus on relevant events.

## 2. Goals
- **Line-Aware Logging**: Logs must carry information about which production line they belong to (Lines 1, 2, or System/Common).
- **UI Update**: Keep the "Source" column. Prepend `[Line X]` to log messages for line-aware display.
- **Dynamic Filtering**: The Log Panel should automatically filter logs to show only those relevant to the currently active tab in the main window.
    - Tab "Line 1" -> Show "Line 1" + "System" logs.
    - Tab "Line 2" -> Show "Line 2" + "System" logs.
    - Tab "Combined" -> Show All logs.

## 3. Scope
- **Models**: `LogMessage.cs`
- **ViewModels**: `MainWindowViewModel.cs`, `LogPanel code-behind` (or VM if extracted).
- **Views**: `LogPanel.xaml`

## 4. Constraints
- Must backward compatible (existing calls to `AddLogMessage` with only `source` string should still work, defaulting to "System" or inferred).
- "System" logs should be visible in all Line tabs.
