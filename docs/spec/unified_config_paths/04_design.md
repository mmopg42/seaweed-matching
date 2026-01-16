---
Task: unified_config_paths
Created: 2026-01-14
Status: Draft
Depends On: 03_plan.md
---

# Unified Configuration Paths - Detailed Design

## 1. Component Designs

### 1.1 IConfigurationManager (Interface Extension)

> Application data path interface expansion.

#### Interface (from Plan)

```csharp
public interface IConfigurationManager
{
    // ... Existing members ...
    
    /// <summary>
    /// Gets the path to the application data directory.
    /// Example: %LOCALAPPDATA%\prische\ChronoView
    /// </summary>
    string AppDataDirectory { get; }

    /// <summary>
    /// Gets the directory path where log files are stored.
    /// Example: %LOCALAPPDATA%\prische\ChronoView\Logs
    /// </summary>
    string LogsDirectory { get; }

    /// <summary>
    /// Gets the full path to the abnormal history file.
    /// Example: %LOCALAPPDATA%\prische\ChronoView\abnormal_history.json
    /// </summary>
    string HistoryFilePath { get; }
}
```

#### Detailed Logic (Implementation in ConfigurationManager.cs)

```csharp
public class ConfigurationManager : IConfigurationManager
{
    public string AppDataDirectory { get; }
    public string ConfigurationFilePath { get; }
    
    // Calculated Properties
    public string LogsDirectory => Path.Combine(AppDataDirectory, "Logs");
    public string HistoryFilePath => Path.Combine(AppDataDirectory, "abnormal_history.json");

    public ConfigurationManager(string appName = "ChronoView", string appAuthor = "prische")
    {
        // Existing logic...
        AppDataDirectory = GetUserDataDirectory(appName, appAuthor);
        // ...
    }
}
```

#### Error Handling

| Error | Detection | Handling | Recovery |
|-------|-----------|----------|----------|
| Directory Missing | `Directory.Exists` check in consumer | Consumer responsibility | Create directory |

---

### 1.2 PathHelper (New Static Class)

> Static helper for accessing paths in UserControls where DI is limited.

#### Detailed Logic

```csharp
public static class PathHelper
{
    public static string LogsDirectory
    {
        get
        {
            try
            {
                var configManager = (Application.Current as App)?.Services.GetService(typeof(IConfigurationManager)) as IConfigurationManager;
                if (configManager != null)
                {
                    return configManager.LogsDirectory;
                }
            }
            catch
            {
                // Fallback handled below
            }
            return FallbackLogsDirectory;
        }
    }

    // Fallback logic matches legacy behavior but correctly targeted to LocalAppData/prische if possible,
    // or safey defaults.
    private static string FallbackLogsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "prische",
        "ChronoView",
        "Logs");
}
```

#### Thread Safety
- `LogsDirectory` is read-only property accessing thread-safe ServiceProvider pattern.
- Access is safe for concurrent reads.

---

### 1.3 Consumers (MainWindowViewModel, LogCleanupService, etc.)

> Identify how they switch to new interface.

#### Logic Update

```csharp
// BEFORE
var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChronoView", "Logs");

// AFTER
var logDir = _configurationManager.LogsDirectory;
Directory.CreateDirectory(logDir); // Ensure existence before use
```

---

## 2. Integration Points

### 2.1 App.xaml.cs → ConfigurationManager

#### Call Sequence
1. App startup (`ConfigureServices`)
2. `new ConfigurationManager("ChronoView", "prische")` registered as Singleton
3. `LogsDirectory` property used for checking/creating initial log folder

### 2.2 Consumers → IConfigurationManager

#### Data Contract
Consumers expect a valid, absolute path string from `LogsDirectory` and `HistoryFilePath`. They typically ensure the directory exists before writing using `Directory.CreateDirectory`.

---

## 3. Edge Cases & Boundary Conditions

| Case | Input | Expected Behavior | Implementation |
|------|-------|-------------------|----------------|
| AppData not writable | File system permission error | Exception propagates | Standard I/O exception handling |
| DI Service not ready | `PathHelper` called too early | Use Fallback path | `try-catch` with Fallback property |

---

## 4. Testing Strategy

### 4.1 Unit Test Cases

| Test Name | Verification |
|-----------|--------------|
| `ConfigurationManager_ReturnsCorrectPaths` | Verify `LogsDirectory` ends with `Logs` and `HistoryFilePath` ends with `abnormal_history.json` |
| `PathHelper_ReturnsPath_WhenServiceAvailable` | Mock `ServiceProvider` and verify helper returns mock path |

### 4.2 Manual Verification

1. **Delete Config/Logs**: Close app, delete `%LOCALAPPDATA%\prische`.
2. **Launch App**: Verify folder structure recreated.
3. **Check Logs**: Verify `Logs` folder contains current session log.
4. **Check Config**: Verify `config.json` created.
5. **Check History**: Run abnormal detection, verify `abnormal_history.json`.

---

## 5. Security Considerations

- **Path Traversal**: `AppDataDirectory` is constructed from system API + trusted strings. No user input used for path construction. Safe.

---

## Approval

- [x] All components have detailed pseudo-code
- [x] Error handling specified
- [x] Thread safety addressed
- [x] Test cases defined
- [x] No open questions

**Next Step**: 05_tasks.md
