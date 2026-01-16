---
Task: fix_logging_redundancy
Created: 2026-01-13
Status: Approved
Depends On: 03_plan.md
---

# fix_logging_redundancy - Detailed Design

## 1. Component Designs

### 1.1 FileLogger

> From 03_plan.md: Append formatted log entries to `_logFilePath`.

#### Interface (from Plan)
```csharp
public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
```

#### Preconditions
- `_logFilePath` must be a valid path (provided during construction).
- `formatter` must not be null.

#### Detailed Logic

```pseudo
function Log(logLevel, eventId, state, exception, formatter):
    // 1. Check if logging is enabled for this level
    if !IsEnabled(logLevel): return

    // 2. Format the message
    message = formatter(state, exception)
    if isEmpty(message): return

    try:
        lock (_fileLock):
            // 3. Ensure the target directory exists
            // This handles cases where the directory might have been deleted mid-session
            logDir = Path.GetDirectoryName(_logFilePath)
            if !Directory.Exists(logDir):
                Directory.CreateDirectory(logDir)

            // 4. Prepare the log entry
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
            level = logLevel.ToString().ToUpper().PadRight(5)
            category = _categoryName
            
            logEntry = $"[{timestamp}] [{level}] [{category}] {message}"
            if exception is not null:
                logEntry += Environment.NewLine + exception.ToString()

            // 5. Append to the session-specific file
            // CRITICAL FIX: Use _logFilePath directly instead of recalculating it
            File.AppendAllText(_logFilePath, logEntry + Environment.NewLine)
    catch:
        // Fail silently to avoid crashing the app due to logging failures
        pass
```

#### Thread Safety
Uses a `static readonly object _fileLock` to ensure that multiple logger instances (from different categories) do not attempt to write to the same file simultaneously.

## 2. Integration Points

### 2.1 App.xaml.cs → FileLoggerProvider
The startup logic provides the exact filename (including date and time). `FileLogger` now respects this path.

## 3. Edge Cases

| Case | Behavior |
|------|----------|
| Directory deleted during session | Re-created by `Directory.CreateDirectory`. |
| Session spans across midnight | Continues writing to the original session file (consistent with start-time naming). |
| File locked by another process | `File.AppendAllText` might fail; handled by the silent `catch`. |

## 6. Testing Strategy

### 6.2 Manual Verification Steps

```
1. Run ChronoView.
2. Check %APPDATA%\ChronoView\Logs\{Today}\.
3. Verify that a file like ChronoView_Debug_{Date}_{Time}.log exists.
4. Verify that ALL subsequent log messages (visible in UI or caused by actions) appear in THIS file.
5. Verify that no ChronoView_Debug_{Date}_{Date}.log file is created.
```

---

## Approval

- [x] All components have detailed pseudo-code
- [x] Error handling specified for all failure modes
- [x] State management documented (if stateful)
- [x] Thread safety addressed
- [x] Edge cases covered
- [x] Test cases defined
- [x] No open questions

**Next Step**: 05_tasks.md
