# Design Document

## Overview

This design establishes the service integration layer that connects the ChronoView WPF UI (implemented in Task 10) with the core services (implemented in Tasks 1-9). The integration follows MVVM patterns with dependency injection, event-driven communication, and proper threading to create a responsive, maintainable application.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                         WPF UI Layer                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ MainWindow   │  │SettingsDialog│  │ Other Views  │      │
│  │  - TabControl│  │  - Tabs      │  └──────┬───────┘      │
│  └──────┬───────┘  └──────┬───────┘         │              │
│         │                  │                  │              │
└─────────┼──────────────────┼──────────────────┼──────────────┘
          │                  │                  │
┌─────────┼──────────────────┼──────────────────┼──────────────┐
│         │         ViewModel Layer             │              │
│  ┌──────▼──────────────┐  ┌─────▼────────────▼───┐          │
│  │MainWindowViewModel  │  │SettingsDialogViewModel│          │
│  │  - Commands         │  │  - Configuration      │          │
│  │  - Properties       │  │  - Validation         │          │
│  │  - Event Handlers   │  └──────────────────────┘          │
│  └──────┬──────────────┘                                     │
└─────────┼────────────────────────────────────────────────────┘
          │
┌─────────┼────────────────────────────────────────────────────┐
│         │         Service Layer                              │
│  ┌──────▼──────────────┐  ┌──────────────────┐              │
│  │MonitoringOrchestrator│  │ConfigurationMgr  │              │
│  │  - Coordinates       │  │  - Load/Save     │              │
│  │  - Events            │  └──────────────────┘              │
│  └──────┬───────────────┘                                    │
│         │                                                     │
│  ┌──────▼──────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐ │
│  │FileWatcher  │  │ImageProc │  │Statistics│  │FileOps   │ │
│  │Service      │  │Service   │  │Service   │  │Service   │ │
│  └─────────────┘  └──────────┘  └──────────┘  └──────────┘ │
└────────────────────────────────────────────────────────────┘
```

### Dependency Injection Container Setup

The application uses Microsoft.Extensions.DependencyInjection for IoC:

```csharp
// App.xaml.cs
public partial class App : Application
{
    private ServiceProvider _serviceProvider;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // Core Services (Singleton)
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        services.AddSingleton<IFileWatcher, FileWatcherService>();
        services.AddSingleton<IFileGroupMatcher, FileGroupMatcherService>();
        services.AddSingleton<IImageProcessor, ImageProcessingService>();
        services.AddSingleton<IStatisticsService, StatisticsService>();
        services.AddSingleton<MonitoringOrchestrator>();
        
        // File Operations (Transient - new instance per operation)
        services.AddTransient<IFileOperationService, FileOperationService>();
        services.AddTransient<IPathManagementService, PathManagementService>();
        
        // ViewModels (Transient)
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SettingsDialogViewModel>();
        
        // Views (Transient)
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsDialog>();
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }
}
```

## Components and Interfaces

### 1. MainWindowViewModel (Enhanced)

**Purpose**: Mediates between MainWindow and services, handling all user interactions.

**Dependencies**:
- `IConfigurationManager` - Load/save configuration
- `MonitoringOrchestrator` - Control file monitoring
- `IStatisticsService` - Receive statistics updates
- `IImageProcessor` - Load thumbnail images
- `IFileOperationService` - Move/delete operations
- `IPathManagementService` - Path configuration
- `ILogger<MainWindowViewModel>` - Logging

**Key Methods**:
```csharp
public class MainWindowViewModel : ViewModelBase
{
    private readonly MonitoringOrchestrator _orchestrator;
    private readonly IStatisticsService _statisticsService;
    private readonly IConfigurationManager _configManager;
    private readonly IFileOperationService _fileOps;
    private readonly IPathManagementService _pathMgmt;
    private readonly ILogger _logger;
    
    public MainWindowViewModel(
        MonitoringOrchestrator orchestrator,
        IStatisticsService statisticsService,
        IConfigurationManager configManager,
        IFileOperationService fileOps,
        IPathManagementService pathMgmt,
        ILogger<MainWindowViewModel> logger)
    {
        // Store dependencies
        // Subscribe to events
        // Initialize commands
    }
    
    // Event Handlers
    private void OnGroupCreated(object sender, FileGroup group);
    private void OnGroupRemoved(object sender, string groupId);
    private void OnFileCountsUpdated(object sender, FileCountStatistics stats);
    private void OnMatchingStatsUpdated(object sender, MatchingStatistics stats);
    
    // Command Implementations (Async)
    private async Task ExecuteStartAsync();
    private async Task ExecuteStopAsync();
    private async Task ExecuteMoveAsync();   // Supports progress & cancellation
    private async Task ExecuteDeleteAsync(); // Supports soft delete & confirmation
    private async Task ExecuteRefreshAsync(); // With monitor-off guard
    private void ExecutePathAutoConfig();
    private async Task ExecuteCreateSampleFolderAsync();
    
    // Tab Management
    public int ActiveTabIndex { get; set; }
    public FileGroupViewModel SelectedGroup { get; } // Computed based on ActiveTabIndex
}
```

### 1.1 FileGroupViewModel (Enhanced)

**Purpose**: Wrap FileGroup with image loading and status capabilities.

```csharp
public class FileGroupViewModel : ViewModelBase
{
    private readonly FileGroup _fileGroup;
    public bool IsAbnormal { get; }
    public string AbnormalReason { get; }
    public int LineNumber { get; } 
    
    // ... Existing image loading logic ...
}
```

### 2. MonitoringOrchestrator Events

**Purpose**: Notify ViewModels of file system changes and grouping results.

```csharp
public class MonitoringOrchestrator
{
    public event EventHandler<FileGroup> GroupCreated;
    public event EventHandler<string> GroupRemoved;
    public event EventHandler<FileGroup> GroupUpdated;
    public event EventHandler<string> MonitoringError;
    
    protected virtual void OnGroupCreated(FileGroup group)
    {
        GroupCreated?.Invoke(this, group);
    }
}
```

### 3. StatisticsService Events

**Purpose**: Notify ViewModels of statistics changes.

```csharp
public interface IStatisticsService
{
    event EventHandler<FileCountStatistics> FileCountsUpdated;
    event EventHandler<MatchingStatistics> MatchingStatisticsUpdated;
    
    Task StartMonitoringAsync(ApplicationConfiguration config);
    Task StopMonitoringAsync();
    FileCountStatistics GetCurrentFileCounts();
    MatchingStatistics GetCurrentMatchingStats();
}
```

### 4. FileOperationService

**Purpose**: Handle file move and delete operations with progress tracking.

```csharp
public interface IFileOperationService
{
    Task<OperationResult> MoveFileGroupAsync(
        FileGroup group, 
        string destinationPath,
        IProgress<OperationProgress> progress,
        CancellationToken cancellationToken);
    
    Task<OperationResult> MoveFileGroupAsync(
        FileGroup group, 
        string destinationPath,
        IProgress<OperationProgress> progress,
        CancellationToken cancellationToken,
        Func<string, Task<ConflictResolution>> onConflict); // Conflict callback
    
    Task<OperationResult> DeleteFileGroupAsync(
        FileGroup group,
        IProgress<OperationProgress> progress,
        CancellationToken cancellationToken); // Soft delete via Quarantine
}

public class OperationResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public int FilesProcessed { get; set; }
    public int FilesFailed { get; set; }
}

public class OperationProgress
{
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public string CurrentFile { get; set; }
    public double PercentComplete => (double)ProcessedFiles / TotalFiles * 100;
}
```

### 5. PathManagementService

**Purpose**: Handle path auto-configuration and sample folder creation.

```csharp
public interface IPathManagementService
{
    Dictionary<string, string> GeneratePathsFromDate(string dateString, ApplicationConfiguration config);
    Task<bool> CreateSampleFoldersAsync(string sampleName, ApplicationConfiguration config);
    Dictionary<string, string> GeneratePathsFromDate(string dateString, ApplicationConfiguration config); // Support Line 2
    Task<bool> CreateSampleFoldersAsync(string sampleName, ApplicationConfiguration config);
    bool ValidatePaths(Dictionary<string, string> paths);
}
```

### 5.1 File Operation Strategies
- **Move**: Uses "Copy-then-Delete" strategy for safety (especially for Normal folders).
- **Delete**: Uses "Soft Delete" strategy moving files to configured Quarantine path.
- **Conflict**: Supports Overwrite, Skip, Abort with "Apply to All" capability.
```

}
```

*(See Section 1.1 for FileGroupViewModel details)*

## Data Models

### FileCountStatistics

```csharp
public class FileCountStatistics
{
    public int Nir1Count { get; set; }
    public int Nir2Count { get; set; }
    public int Normal1Count { get; set; }
    public int Normal2Count { get; set; }
    public int Cam1Count { get; set; }
    public int Cam2Count { get; set; }
    public int Cam3Count { get; set; }
    public int Cam4Count { get; set; }
    public int Cam5Count { get; set; }
    public int Cam6Count { get; set; }
    public DateTime LastUpdated { get; set; }
}
```

### MatchingStatistics

```csharp
### MatchingStatistics

```csharp
public class MatchingStatistics
{
    // Unified Mode
    public int TotalGroups { get; set; }
    public int WithNir { get; set; }
    public int WithoutNir { get; set; }
    public int Failed { get; set; }
    public double MatchRate => TotalGroups > 0 ? (double)WithNir / TotalGroups * 100 : 0;
    
    // Separated Mode
    public LineStatistics Line1 { get; set; }
    public LineStatistics Line2 { get; set; }
}

public class LineStatistics
{
    public int TotalGroups { get; set; }
    public int WithNir { get; set; }
    public int WithoutNir { get; set; }
    public int Failed { get; set; }
    public double MatchRate => TotalGroups > 0 ? (double)WithNir / TotalGroups * 100 : 0;
}
```

### MatchingSettings

```csharp
public class MatchingSettings
{
    // Integrated / Separated
    public string LineMode { get; set; }

    // Line 1
    public string Nir1Path { get; set; }
    public string Normal1Path { get; set; }
    public string Camera1Path { get; set; }
    public string Camera2Path { get; set; }
    public string Camera3Path { get; set; }

    // Line 2
    public string Nir2Path { get; set; }
    public string Normal2Path { get; set; }
    public string Camera4Path { get; set; }
    public string Camera5Path { get; set; }
    public string Camera6Path { get; set; }
    
    // ... Time windows ...
}

public class WorkflowSettings 
{
    // ... Existing settings ...
    public string DeleteQuarantinePath { get; set; } // For soft delete
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Service Lifecycle Consistency
*For any* application startup sequence, all registered services should be successfully initialized before the main window is displayed, and all services should be properly disposed when the application closes.
**Validates: Requirements 1.1, 1.4**

### Property 2: UI Thread Marshalling Safety
*For any* background operation that updates UI-bound properties, the update should be marshalled to the UI thread using Dispatcher, preventing cross-thread access violations.
**Validates: Requirements 15.2**

### Property 3: Event Subscription Cleanup
*For any* ViewModel that subscribes to service events, all subscriptions should be unsubscribed when the ViewModel is disposed, preventing memory leaks.
**Validates: Requirements 15.5**

### Property 4: Command State Consistency
*For any* command with CanExecute logic, the command's enabled state should accurately reflect the current application state (e.g., Start disabled when monitoring, Stop disabled when not monitoring).
**Validates: Requirements 2.3, 3.3**

### Property 5: File Group Collection Synchronization
*For any* FileGroup added or removed by background threads, the ObservableCollection update should be marshalled to the UI thread, maintaining collection integrity.
**Validates: Requirements 4.4, 15.3**

### Property 6: Image Loading Cancellation
*For any* thumbnail loading operation, if the FileGroupViewModel is disposed before loading completes, the operation should be cancelled without throwing exceptions.
**Validates: Requirements 5.5, 15.4**

### Property 7: Statistics Update Atomicity
*For any* statistics update event, all related properties (counts, rates, percentages) should be updated atomically, preventing the UI from displaying inconsistent intermediate states.
**Validates: Requirements 6.4, 7.4**

### Property 8: File Operation Rollback
*For any* file move or delete operation that fails partway through, the operation should either complete fully or rollback changes, never leaving the file system in a partial state.
**Validates: Requirements 8.4, 9.5**

### Property 9: Configuration Persistence Reliability
*For any* configuration change saved through the Settings dialog, restarting the application should restore exactly those settings.
**Validates: Requirements 10.3**

### Property 10: Path Auto-Config Determinism
*For any* valid date string, the Path Auto Config function should generate the same folder paths when called multiple times with the same date and configuration.
**Validates: Requirements 11.2**

### Property 11: Error Propagation Completeness
*For any* service operation that fails, the error should be logged, and if user-facing, an appropriate error message should be displayed in the UI.
**Validates: Requirements 14.1, 14.2**

### Property 12: Window State Restoration
*For any* window position and size saved on close, reopening the application should restore the window to that exact position and size, unless the position is off-screen.
**Validates: Requirements 16.1, 16.4**

## Error Handling

### Error Categories

1. **Service Initialization Errors**
   - Log full exception details
   - Display user-friendly message
   - Exit application gracefully if critical service fails

2. **File Operation Errors**
   - Log error with file path and operation type
   - Display error message in UI
   - Add error entry to log panel
   - Rollback partial changes if possible

3. **Background Thread Errors**
   - Log error with thread context
   - Do not crash application
   - Notify user if operation was user-initiated

4. **Configuration Errors**
   - Log validation failures
   - Display specific validation messages
   - Prevent saving invalid configuration

### Error Handling Pattern

```csharp
private async Task ExecuteOperationAsync()
{
    try
    {
        // Perform operation
        await _service.DoSomethingAsync();
    }
    catch (OperationCanceledException)
    {
        // User cancelled - no error
        _logger.LogInformation("Operation cancelled by user");
    }
    catch (FileNotFoundException ex)
    {
        _logger.LogError(ex, "File not found: {Path}", ex.FileName);
        await ShowErrorMessageAsync("File not found", ex.Message);
        AddLogMessage(LogSeverity.Error, "FileOperation", $"File not found: {ex.FileName}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error in operation");
        await ShowErrorMessageAsync("Operation failed", "An unexpected error occurred. Please check the logs.");
        AddLogMessage(LogSeverity.Error, "System", $"Unexpected error: {ex.Message}");
    }
}
```

## Testing Strategy

### Unit Tests

Focus on individual component behavior:

1. **ViewModel Command Tests**
   - Test command execution logic
   - Test CanExecute conditions
   - Test property change notifications

2. **Service Integration Tests**
   - Test service method calls from ViewModels
   - Test event subscription and handling
   - Mock services to isolate ViewModel logic

3. **Error Handling Tests**
   - Test exception handling in commands
   - Test error message display
   - Test logging of errors

### Property-Based Tests

Verify universal properties using FsCheck.NET:

1. **Thread Safety Tests**
   - Generate random sequences of UI and background operations
   - Verify no cross-thread exceptions occur
   - Verify collections remain consistent

2. **Event Subscription Tests**
   - Generate random subscribe/unsubscribe sequences
   - Verify no memory leaks
   - Verify events fire correctly

3. **Configuration Round-Trip Tests**
   - Generate random valid configurations
   - Save and reload
   - Verify exact match

4. **Path Generation Tests**
   - Generate random valid dates
   - Verify deterministic path generation
   - Verify path validity

### Integration Tests

Test complete workflows:

1. **Start-Monitor-Stop Workflow**
   - Start monitoring
   - Verify file detection
   - Verify group creation
   - Stop monitoring
   - Verify clean shutdown

2. **File Operation Workflow**
   - Create test file groups
   - Execute move operation
   - Verify files moved
   - Verify UI updated

3. **Settings Workflow**
   - Open settings dialog
   - Modify configuration
   - Save settings
   - Verify services reloaded

## Threading Model

### Thread Types

1. **UI Thread**
   - All WPF UI operations
   - Property change notifications
   - Command execution initiation

2. **Background Threads (Task.Run)**
   - File I/O operations
   - Image processing
   - File system scanning

3. **FileSystemWatcher Thread**
   - File system event callbacks
   - Must marshal to UI thread for updates

### Synchronization Patterns

```csharp
// Pattern 1: Background work with UI update
private async Task LoadDataAsync()
{
    // Background work
    var data = await Task.Run(() => _service.LoadData());
    
    // UI update on UI thread
    await Application.Current.Dispatcher.InvokeAsync(() =>
    {
        DataProperty = data;
    });
}

// Pattern 2: Event handler from background thread
private void OnServiceEvent(object sender, EventArgs e)
{
    // Marshal to UI thread
    Application.Current.Dispatcher.InvokeAsync(() =>
    {
        UpdateUIProperty();
    });
}

// Pattern 3: Cancellable background operation
private CancellationTokenSource _cts;

private async Task StartOperationAsync()
{
    _cts = new CancellationTokenSource();
    
    try
    {
        await Task.Run(async () =>
        {
            await _service.LongRunningOperationAsync(_cts.Token);
        }, _cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Expected when cancelled
    }
}

private void StopOperation()
{
    _cts?.Cancel();
}
```

## Implementation Notes

### Service Lifetime Guidelines

- **Singleton**: Services that maintain state across the application lifetime
  - ConfigurationManager, FileWatcherService, StatisticsService
  
- **Transient**: Services that are stateless or operation-specific
  - FileOperationService, PathManagementService, ViewModels

### Memory Management

1. **Image Cache Management**
   - LRU cache with configurable size limit
   - Dispose BitmapSource objects when evicted
   - Clear cache on configuration change

2. **Event Subscription Cleanup**
   - Implement IDisposable on ViewModels
   - Unsubscribe from all events in Dispose()
   - Use weak event patterns for long-lived publishers

3. **Collection Management**
   - Limit log message collection to 1000 items
   - Clear file groups on refresh
   - Dispose ViewModels when removed from collections

### Performance Considerations

1. **Thumbnail Generation**
   - Generate on background thread
   - Cache results
   - Use lower quality for thumbnails (JPEG 85%)

2. **Statistics Updates**
   - Debounce rapid updates (max 1 update per 500ms)
   - Batch multiple changes into single update

3. **File System Events**
   - Buffer events for 100ms before processing
   - Aggregate multiple events for same file

## Deployment Considerations

### Configuration File Location

- Development: `bin/Debug/net8.0-windows/config.json`
- Production: `%APPDATA%/ChronoView/config.json`

### Logging Configuration

- Development: Console + Debug output
- Production: File logging to `%APPDATA%/ChronoView/Logs/`

### Error Reporting

- Critical errors: Windows Event Log
- User errors: UI message boxes
- Debug info: Log files
