---
Task: fix_refresh_logic
Created: 2026-01-12
Status: Approved
Summary: Detailed design for Deep Reset Refresh with complete dependency injection and option object accuracy.
---

# Fix Refresh Logic - Design (Final v2)

## 1. System Architecture

### 1.1 State Transition Diagram (Refresh Flow)

```mermaid
graph TD
    A[Monitor-Active] -->|Refresh Clicked| B(Stopping)
    B -->|Stop FileWatcher| C{Strict Lock}
    C -->|Clear KnownFiles| D[Stopped]
    D -->|Reload Config| E[Configuration Reloaded]
    E -->|CoreReset| F[Services Reset]
    F -->|Initial Scan| G[Scanning...]
    G -->|Inject Files| H[Start FileWatcher]
    H --> I[Monitor-Active (New State)]
```

## 2. API Changes

### 2.1 `IFileWatcherService`

```csharp
public interface IFileWatcherService : IDisposable
{
    // EXISTING
    Task StartWatchingAsync(IEnumerable<string> paths, FileWatcherOptions options);

    // NEW Overload
    Task StartWatchingAsync(IEnumerable<string> paths, FileWatcherOptions options, IEnumerable<string> knownFiles);
    
    // MODIFIED Behavior
    Task StopWatchingAsync(); // Must acquire lock before clearing state
}
```

### 2.2 `IGroupManager`

```csharp
public interface IGroupManager
{
    // ... existing ...
    
    // NEW: Needed for FileWatcher injection optimization
    IEnumerable<string> GetAllProcessedFilePaths();
}
```

### 2.3 `IAbnormalDetectorService`

```csharp
public interface IAbnormalDetectorService
{
    // ... existing ...
    void Reset(); // New method
}
```

### 2.4 `IMonitoringOrchestrator`

```csharp
public interface IMonitoringOrchestrator
{
    // ... existing ...
    
    // NEW: Signals UI to clear all data
    event EventHandler MonitoringStateReset; 
}
```

## 3. Detailed Logic

### 3.1 `MonitoringOrchestrator` Refactoring

#### Dependencies
- **Issue**: `MonitoringOrchestrator` lacks `IConfigurationManager` and `IAbnormalDetectorService`.
- **Fix**: Inject both into constructor.

```csharp
private readonly IConfigurationManager _configManager;
private readonly IAbnormalDetectorService _abnormalDetector; // NEW

public MonitoringOrchestrator(
    IFileGroupMatcher fileGroupMatcher,
    IFileWatcher fileWatcher,
    ILogger<MonitoringOrchestrator> logger,
    INirFileResolver nirFileResolver,
    ITimestampCache folderTimestamps,
    IInitialScanner initialScanner,
    IGroupManager groupManager,
    IImageCaptureService imageCache,
    IEventProcessor eventProcessor,
    IConfigurationManager configManager, // NEW
    IAbnormalDetectorService abnormalDetector, // NEW
    Action<LogSeverity, string, string>? uiLog = null)
{
    // ... Assignments ...
    _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
    _abnormalDetector = abnormalDetector ?? throw new ArgumentNullException(nameof(abnormalDetector));
    // ...
}
```

#### `CoreResetAndScanAsync` (Refactored)

```csharp
private async Task<IEnumerable<string>> CoreResetAndScanAsync(CancellationToken ct)
{
    // 1. Reset all stateful services
    _groupManager.ResetState();
    _fileGroupMatcher.ResetState();
    _folderTimestamps.Clear();
    _imageCache.Clear();
    _abnormalDetector.Reset(); // Now accessible
    
    // 2. Perform fresh scan
    var result = await PerformInitialScanAsync(ct);
    
    if (!result.Success)
    {
        throw new InvalidOperationException($"Scan failed: {string.Join(", ", result.Errors)}");
    }
    
    // 3. Return all known files for injection
    return _groupManager.GetAllProcessedFilePaths();
}
```

#### `RefreshAsync` (Refactored)

```csharp
public async Task RefreshAsync(CancellationToken ct)
{
    try 
    {
        _logger.LogInformation("Refreshing...");

        // 1. Stop everything
        await _eventProcessor.StopAsync();
        await _fileWatcher.StopWatchingAsync();

        // 2. Reload Config explicitly
        _currentConfig = _configManager.LoadConfiguration();
        
        // 3. Re-configure Matcher
        _fileGroupMatcher.Configuration = _currentConfig.MatchingSettings;
        
        // 4. Core Reset & Scan
        var knownFiles = await CoreResetAndScanAsync(ct); 
        
        // 5. Restart FileWatcher with INJECTION
        await _fileWatcher.StartWatchingAsync(
            GetWatchPaths(_currentConfig), 
            new FileWatcherOptions 
            { 
                EnablePolling = _currentConfig.WorkflowSettings.EnablePolling,
                PollingIntervalMs = _currentConfig.WorkflowSettings.PollingIntervalMs,
                EnableNetworkOptimization = _currentConfig.WorkflowSettings.EnableNetworkOptimization,
                DataSequenceSettings = _currentConfig.DataSequenceSettings // Correctly passed
            }, 
            knownFiles
        );
        
        // 6. Restart Event Processor
        _eventProcessor.Start(_currentConfig.WorkflowSettings.MaxEventProcessingWorkers, ProcessSingleEventAsync);
        
        _logger.LogInformation("Refresh completed successfully.");
    }
    catch (Exception ex)
    {
        OnMonitoringError($"Refresh failed: {ex.Message}");
    }
}
```

## 4. Safety Considerations

- **DI Registration**: `App.xaml.cs` must register `IConfigurationManager` and `IAbnormalDetectorService` as Singletons or Scoped services available to `IMonitoringOrchestrator`.

## 5. Verification

See `05_tasks.md` for verifiable implementation steps.
