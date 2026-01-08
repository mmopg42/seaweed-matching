---
Task: log_synchronization
Created: 2026-01-07
Status: Draft
Depends On: 03_plan.md
---

# Log Synchronization - Detailed Design

## 1. Component Designs

### 1.1 `UiLogger` and `UiLoggerProvider`

> **Purpose**: A custom `ILogger` implementation that forwards all log events to the UI's `MainWindowViewModel`. This ensures 1:1 synchronization with standard loggers (Console/File).

#### Interface

```csharp
// Infrastructure/Logging/UiLoggerProvider.cs
public class UiLoggerProvider : ILoggerProvider { ... }

// Infrastructure/Logging/UiLogger.cs
public class UiLogger : ILogger { ... }
```

#### Preconditions

- `MainWindowViewModel` must be accessible (e.g., via a shared event, singleton, or weak reference) to receive log messages.
- The logging infrastructure (`App.xaml.cs`) must register this provider.

#### Postconditions

- All `Log<TState>` calls made through standard `ILogger` are forwarded to the UI.
- The UI list (`LogMessages`) reflects the exact same stream as the text log file.

#### Detailed Logic

```pseudo
// class UiLoggerProvider
function CreateLogger(categoryName):
    return new UiLogger(categoryName, _uiLogAction)

// class UiLogger
function Log(logLevel, eventId, state, exception, formatter):
    // ========== INPUT VALIDATION ==========
    if (!IsEnabled(logLevel)) return
    
    // ========== CORE LOGIC ==========
    // Step 1: Format message
    message = formatter(state, exception)
    
    // Step 2: Use event or action to dispatch to UI
    // Note: Must be thread-safe and non-blocking if possible, 
    // but UI updates must eventually hit the UI thread.
    
    GlobalEventAggregator.Publish(new LogEvent(logLevel, _categoryName, message))
```

#### State Variables

| Variable | Type | Initial | Purpose |
|----------|------|---------|---------|
| `_logAction` | `Action<LogSeverity, string, string>` | null | Delegate to update UI |

#### Thread Safety

- The `Log` method is called from multiple threads.
- The forwarding mechanism (Event or Action) must handle concurrency.
- `MainWindowViewModel.AddLogMessage` already handles `Dispatcher.Invoke`, so the Logger just needs to invoke the delegate safely.

#### Error Handling

| Error | Detection | Handling | Recovery |
|-------|-----------|----------|----------|
| UI Delegate is null | Check before invoke | Ignore (logging before UI start) | Continue |
| Formatting error | Exception in formatter | Catch and log fallback message | Skip malformed |

---

### 1.2 `MainWindowViewModel` Refactoring

> **Purpose**: Remove direct `AddLogMessage` calls from logic and rely on `ILogger`. Make `AddLogMessage` the *receiver* for `UiLogger`.

#### Detailed Logic

```pseudo
function AddLogMessage(severity, source, message):
    // ========== CORE LOGIC ==========
    // Step 1: Create LogMessage object
    entry = new LogMessage(severity, source, message)
    
    // Step 2: Add to ObservableCollection on UI Thread
    Dispatcher.Invoke(() => {
        LogMessages.Add(entry)
        if LogMessages.Count > 1000:
            LogMessages.RemoveAt(0)
    })
    
    // Note: We REMOVE WriteToUILogFile because the FileLogger is now the single source of truth for files.
    // User wants "Debug and UI idendical".
    // If the UI writes its own file, it duplicates the FileLogger's work.
    // STRATEGY CHANGE: The requirement "UI count abnormally high" suggests we should NOT have a separate UI log file if we are syncing with Debug log.
    // However, if the user wants a separate file for UI logs, it should be EXACTLY the same content as the UI list.
    // PROPOSAL: The Debug File Logger covers everything. The UI Log File is redundant if it's 1:1.
    // WE WILL REMOVE SEPARATE UI LOGGING TO FILE to prevent "abnormally high" file counts/confusion, 
    // OR ensure it writes exactly what it receives.
```

---

## 2. Integration Points

### 2.1 `App.xaml.cs` Configuration

#### Call Sequence

```
1. App Startup
2. ConfigureServices()
3. services.AddLogging()
   -> AddConsole()
   -> AddDebug()
   -> AddProvider(new FileLoggerProvider(...)) 
   -> AddProvider(new UiLoggerProvider(ActionRef)) // Inject the bridge
```

### 2.2 `MonitoringOrchestrator` & Service Logic

#### Logic Change

```pseudo
// BEFORE
_logger.LogInformation("Processing...")
_uiLogAction(Info, "Orchestrator", "Processing...") // DUPLICATE SOURCE

// AFTER
_logger.LogInformation("Processing...") 
// UiLoggerProvider catches this -> forwards to UI
// FileLoggerProvider catches this -> writes to file
// RESULT: 1:1 Match.
```

---

## 3. Edge Cases

| Case | Input | Expected Behavior | Implementation |
|------|-------|-------------------|----------------|
| High volume logs | 100 logs/sec | UI shouldn't freeze | `LogMessages` batching or capping (already exist: 1000 limit) |
| UI not ready | App start | Logs queued or ignored? | Ignored until UI comes up (acceptable for "UI Log") |

---

## 4. Testing Strategy

### 4.1 Unit Test Cases

| Test Name | Input | Expected | Verifies |
|-----------|-------|----------|----------|
| test_logger_forwarding | `logger.LogInfo("test")` | UI collection receives "test" | Integration |

---
