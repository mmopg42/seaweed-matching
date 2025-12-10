# Design Document

## Overview

This document outlines the design for migrating a comprehensive Python GUI monitoring application to C#. The migration strategy focuses on leveraging C#'s native performance advantages while maintaining complete functional parity with the original system.

The Python system is a sophisticated file monitoring and image processing application built with PySide6, featuring real-time file system watching, NIR spectrum analysis, image thumbnail generation, and complex file matching algorithms. The C# implementation will modernize this architecture using WPF for the UI, async/await patterns for concurrency, and native .NET libraries for improved performance.

**UI Design Reference**: The user interface implementation follows the reference design located at `C#_project/gui_c/components/chrono-view-pro.tsx`. This Next.js/React prototype defines the layout structure, component organization, and user interaction patterns that should be replicated in WPF.

Key migration principles:
- **Performance First**: Utilize native C# libraries and patterns for superior performance
- **Functional Parity**: Maintain identical behavior and data compatibility
- **Modern Architecture**: Implement MVVM pattern with dependency injection
- **Maintainability**: Create modular, testable components with clear separation of concerns
- **UI Consistency**: Follow the reference design for layout, components, and user experience

## Architecture

### High-Level Architecture

The C# system follows a layered architecture with clear separation between UI, business logic, and data access:

```
┌─────────────────────────────────────────┐
│              UI Layer (WPF)             │
│  ┌─────────────┐  ┌─────────────────┐   │
│  │ MainWindow  │  │ SettingsDialog  │   │
│  │   (MVVM)    │  │    (MVVM)       │   │
│  └─────────────┘  └─────────────────┘   │
└─────────────────────────────────────────┘
                    │
┌─────────────────────────────────────────┐
│           Application Layer             │
│  ┌─────────────┐  ┌─────────────────┐   │
│  │   Services  │  │   ViewModels    │   │
│  │             │  │                 │   │
│  └─────────────┘  └─────────────────┘   │
└─────────────────────────────────────────┘
                    │
┌─────────────────────────────────────────┐
│            Core Layer                   │
│  ┌─────────────┐  ┌─────────────────┐   │
│  │File Watcher │  │  Image Manager  │   │
│  │Group Matcher│  │  NIR Processor  │   │
│  │Config Mgr   │  │  Statistics     │   │
│  └─────────────┘  └─────────────────┘   │
└─────────────────────────────────────────┘
                    │
┌─────────────────────────────────────────┐
│         Infrastructure Layer            │
│  ┌─────────────┐  ┌─────────────────┐   │
│  │File System  │  │  Data Storage   │   │
│  │   I/O       │  │     (JSON)      │   │
│  └─────────────┘  └─────────────────┘   │
└─────────────────────────────────────────┘
```

### Technology Stack

- **UI Framework**: WPF with XAML for declarative UI design
- **Architecture Pattern**: MVVM with dependency injection
- **Async Processing**: Task-based async/await patterns
- **File System Monitoring**: FileSystemWatcher with custom buffering
- **Image Processing**: ImageSharp for cross-platform image operations
- **Data Serialization**: System.Text.Json for configuration and state
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Testing**: xUnit with property-based testing using FsCheck.NET

### Complete Module Mapping

**CRITICAL UPDATE**: Analysis now covers ALL 41 Python modules (previously only 7 were documented).

Python modules are mapped to C# namespaces organized by functional categories:

#### Core Application Modules (3 modules - Critical Priority)
| Python Module | C# Namespace | Functions | Complexity | Purpose |
|---------------|--------------|-----------|------------|---------|
| `main.py` | `ChronoView.App` | 16 | Medium | Application controller and window management |
| `monitoring_app.py` | `ChronoView.UI.Views` | 50+ | High | Main monitoring application with complex UI |
| `ui_builder.py` | `ChronoView.UI.Builders` | 13 | High | Dynamic UI construction and layout management |

#### File Operations Modules (8 modules - Critical/High Priority)
| Python Module | C# Namespace | Functions | Complexity | Purpose |
|---------------|--------------|-----------|------------|---------|
| `file_matcher.py` | `ChronoView.Core.FileMatching` | 15 | High | File system monitoring and matching logic |
| `group_manager.py` | `ChronoView.Core.GroupManagement` | 10 | High | File matching and group creation algorithms |
| `file_operations.py` | `ChronoView.Core.FileOperations` | 20 | High | File operation worker with complex logic |
| `file_operation_manager.py` | `ChronoView.Core.FileOperations` | 9 | High | Business logic for file operations |
| `delete_manager.py` | `ChronoView.Core.FileOperations` | 9 | High | File deletion management |
| `operation_planner.py` | `ChronoView.Core.Planning` | 5 | Medium | Operation planning and validation |
| `operation_validator.py` | `ChronoView.Core.Validation` | 5 | Medium | Input validation for operations |
| `path_utils.py` | `ChronoView.Utils.Path` | 6 | Low | Path manipulation utilities |

#### Image Processing Modules (4 modules - High Priority)
| Python Module | C# Namespace | Functions | Complexity | Purpose |
|---------------|--------------|-----------|------------|---------|
| `image_loader.py` | `ChronoView.Core.ImageProcessing` | 15 | High | Asynchronous image loading with priority queue |
| `image_manager.py` | `ChronoView.Core.ImageProcessing` | 15 | High | Image processing service with caching |
| `image_registry.py` | `ChronoView.Core.ImageProcessing` | 10 | Medium | Image-widget mapping and optimization |
| `preview_dialog.py` | `ChronoView.UI.Dialogs` | 5 | Medium | Image preview dialog |

#### NIR Processing Modules (5 modules - High Priority)
| Python Module | C# Namespace | Functions | Complexity | Purpose |
|---------------|--------------|-----------|------------|---------|
| `nir_app.py` | `ChronoView.NIR.Processing` | 15 | High | NIR processing application |
| `nir_spectrum_monitor.py` | `ChronoView.NIR.Monitoring` | 8 | High | NIR spectrum analysis and monitoring |
| `nir_status_monitor.py` | `ChronoView.NIR.Status` | 5 | Medium | NIR status IPC communication |
| `nir_status_widget.py` | `ChronoView.UI.Components` | 5 | Medium | NIR status display widget |
| `nir_pruning_service.py` | `ChronoView.NIR.Services` | 1 | Low | NIR file management service |

#### UI Components Modules (8 modules - High/Medium Priority)
| Python Module | C# Namespace | Functions | Complexity | Purpose |
|---------------|--------------|-----------|------------|---------|
| `ui_components.py` | `ChronoView.UI.Controls` | 25 | High | Reusable UI components and dialogs |
| `drag_select_widget.py` | `ChronoView.UI.Controls` | 7 | Medium | Multi-row selection widget |
| `log_panel.py` | `ChronoView.UI.Panels` | 3 | Low | Log display panel |
| `window_state_manager.py` | `ChronoView.UI.Utils` | 2 | Low | Window state persistence |
| `tooltips.py` | `ChronoView.UI.Utils` | 2 | Low | UI tooltip management |
| `statistics_presenter.py` | `ChronoView.UI.Presentation` | 3 | Medium | Statistics display and formatting |
| `statistics_calculator.py` | `ChronoView.Core.Analytics` | 2 | Medium | Statistical calculations |

#### Monitoring Services Modules (6 modules - High/Medium Priority)
| Python Module | C# Namespace | Functions | Complexity | Purpose |
|---------------|--------------|-----------|------------|---------|
| `watchdog_manager.py` | `ChronoView.Core.FileWatching` | 8 | Medium | File system monitoring service |
| `file_count_worker.py` | `ChronoView.Core.Monitoring` | 12 | High | Background file counting service |
| `file_count_monitor.py` | `ChronoView.UI.Monitoring` | 5 | Medium | File counting UI component |
| `file_count_monitor_standalone.py` | `ChronoView.Apps.Monitoring` | 5 | Medium | Standalone monitoring application |
| `monitoring_orchestrator.py` | `ChronoView.Core.Orchestration` | 3 | High | Workflow coordination service |
| `abnormal_detector.py` | `ChronoView.Core.Analytics` | 5 | High | Statistical anomaly detection |

#### Infrastructure and Utilities (7 modules - Critical/Medium Priority)
| Python Module | C# Namespace | Functions | Complexity | Purpose |
|---------------|--------------|-----------|------------|---------|
| `config_manager.py` | `ChronoView.Core.Configuration` | 12 | Medium | Configuration management (Critical) |
| `crash_logger.py` | `ChronoView.Infrastructure.Logging` | 6 | High | Global exception handling (Critical) |
| `group_state_manager.py` | `ChronoView.Core.State` | 9 | High | Group state persistence |
| `heartbeat.py` | `ChronoView.Infrastructure.Health` | 5 | Medium | Application health monitoring |
| `memory_monitor.py` | `ChronoView.Infrastructure.Monitoring` | 4 | Medium | Memory usage monitoring |
| `thread_monitor.py` | `ChronoView.Infrastructure.Monitoring` | 6 | Medium | Thread monitoring and debugging |
| `signal_handler.py` | `ChronoView.Infrastructure.System` | 1 | Low | OS signal handling |
| `utils.py` | `ChronoView.Utils.Core` | 12 | Medium | Core utility functions |

### Implementation Statistics

**Complete Module Coverage**: 41/41 modules analyzed (100% complete)

#### By Priority Distribution
- **Critical Priority**: 12 modules (29%) - Core functionality, must implement first
- **High Priority**: 15 modules (37%) - Important features, implement early  
- **Medium Priority**: 12 modules (29%) - Supporting features, implement after core
- **Low Priority**: 2 modules (5%) - Nice-to-have, implement last

#### By Complexity Distribution
- **High Complexity**: 38 functions (27%) - Complex algorithms, significant effort required
- **Medium Complexity**: 58 functions (41%) - Standard implementation, moderate effort
- **Low Complexity**: 45 functions (32%) - Simple conversions, minimal effort

#### Effort Estimation
- **Total Functions**: 400+ across all modules
- **Total Estimated Hours**: 184 hours
- **Average per Function**: 1.3 hours
- **High Complexity Average**: 2.5 hours per function
- **Medium Complexity Average**: 1.2 hours per function  
- **Low Complexity Average**: 0.5 hours per function

## Components and Interfaces

### Core Interfaces

#### IFileWatcher
```csharp
public interface IFileWatcher
{
    event EventHandler<FileSystemEventArgs> FileChanged;
    Task StartWatchingAsync(IEnumerable<string> paths);
    Task StopWatchingAsync();
    bool IsWatching { get; }
}
```

#### IImageProcessor
```csharp
public interface IImageProcessor
{
    Task<byte[]> GenerateThumbnailAsync(string imagePath, int width, int height);
    Task<ImageMetadata> GetImageMetadataAsync(string imagePath);
    void ClearCache();
}
```

#### IFileGroupMatcher
```csharp
public interface IFileGroupMatcher
{
    Task<IEnumerable<FileGroup>> MatchFilesAsync(UnmatchedFiles unmatchedFiles);
    Task<FileGroup> UpdateGroupAsync(FileGroup group, string filePath, FileType fileType);
    MatchingConfiguration Configuration { get; set; }
}
```

#### INirProcessor
```csharp
public interface INirProcessor
{
    Task<NirSpectrum> ParseSpcFileAsync(string filePath);
    Task<bool> MoveNirFileAsync(string sourcePath, string destinationPath);
    Task<NirMetadata> ExtractMetadataAsync(string filePath);
}
```

#### IConfigurationManager
```csharp
public interface IConfigurationManager
{
    Task<T> LoadConfigurationAsync<T>() where T : class, new();
    Task SaveConfigurationAsync<T>(T configuration) where T : class;
    event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
}
```

### Key Components

#### FileWatcherService
Replaces Python's watchdog with native FileSystemWatcher:
- Implements buffering to handle high-frequency file events
- Provides recursive directory monitoring
- Handles network drive limitations with fallback polling
- Uses async/await for non-blocking event processing

#### ImageProcessingService
Replaces PIL/Pillow with ImageSharp:
- Generates thumbnails with configurable quality settings
- Implements LRU cache for processed images
- Supports multiple image formats (JPEG, PNG, TIFF, BMP)
- Provides async image loading with cancellation support

#### FileGroupMatchingService
Implements the complex file matching logic:
- Time-based correlation algorithms for grouping related files
- Configurable time windows for different file types
- Support for both integrated and separated line modes
- Abnormal condition detection using z-score analysis

#### NirProcessingService
Handles NIR spectrum file processing with pure C# implementation:

**Pandas/NumPy Alternative Strategy**:
- **No external numerical libraries required** (MathNet.Numerics or Accord.NET are unnecessary)
- **Pure LINQ + Custom Classes** for spectrum data processing
- **Data Structure**: `List<(double X, double Y)>` for spectrum points
- **Operations**: LINQ methods (Where, OrderBy, Select, Max, Min) for filtering and aggregation

**Core Implementation**:
```csharp
public class SpectrumData
{
    public List<(double X, double Y)> Points { get; set; }
    
    // CSV parsing with StreamReader (replaces pd.read_csv)
    public static SpectrumData LoadFromFile(string path);
    
    // Sliding window analysis (replaces pandas vectorized operations)
    public List<YVariationRegion> FindYVariationInXWindow(
        double xWindow = 800, 
        double stride = 50);
}
```

**Algorithm Details**:
- Text file parsing with comment filtering (# lines)
- Data filtering: X range 4500-6500
- Sliding window analysis: 800-unit windows with 50-unit stride
- Y variation detection: 0.05 ≤ range ≤ 0.1
- File operations: Move or delete based on detection results

**Performance Considerations**:
- LINQ provides sufficient performance for typical spectrum data sizes
- No need for vectorization - sequential processing is adequate
- Memory efficient with streaming file reading

#### PathManagementService
Handles path configuration and folder management:

**Path Auto-Configuration**:
- Regex-based date pattern detection (8-digit YYYYMMDD)
- Batch path updates with confirmation dialog
- Automatic folder creation with error handling
- Monitoring restart coordination

**Sample Folder Management**:
- Creates sample folders with "with NIR" and "without NIR" subfolders
- Validates output directory before creation
- Auto-creation on monitoring start

**Implementation**:
```csharp
public interface IPathManagementService
{
    Task<PathAutoConfigResult> AutoConfigurePathsAsync(string newDate);
    Task<bool> CreateSampleFolderAsync(string sampleName, string outputRoot);
    Task<bool> AutoCreateSampleFoldersAsync();
    List<(string key, string oldPath, string newPath)> PreviewPathChanges(string newDate);
}
```

#### OperationValidationService
Validates file operations before execution:

**Validation Checks**:
- Source path existence
- Destination path writability
- Disk space availability
- File lock detection
- Permission verification

**Implementation**:
```csharp
public interface IOperationValidationService
{
    Task<ValidationResult> ValidateOperationAsync(FileOperation operation);
    Task<bool> CanWriteToPathAsync(string path);
    Task<long> GetAvailableDiskSpaceAsync(string path);
}
```

#### OperationPlanningService
Plans and estimates file operations:

**Planning Features**:
- Disk space calculation
- Operation time estimation
- Conflict detection
- Batch operation optimization

**Implementation**:
```csharp
public interface IOperationPlanningService
{
    Task<OperationPlan> PlanOperationAsync(List<FileGroup> groups, OperationSettings settings);
    Task<TimeSpan> EstimateOperationTimeAsync(OperationPlan plan);
}
```

#### NirPruningService
Manages NIR file cleanup and organization:

**Pruning Features**:
- Automatic NIR file detection
- Duplicate removal
- Orphaned file cleanup
- Archive management

**Implementation**:
```csharp
public interface INirPruningService
{
    Task<PruningResult> PruneNirFilesAsync(string nirPath);
    Task<List<string>> FindOrphanedNirFilesAsync();
    Task<bool> ArchiveOldNirFilesAsync(DateTime cutoffDate);
}
```

#### MonitoringOrchestrator
Coordinates monitoring workflows:

**Orchestration Features**:
- Initial scan coordination
- File matching workflow
- Group creation management
- State synchronization

**Implementation**:
```csharp
public interface IMonitoringOrchestrator
{
    Task<OrchestrationResult> PerformInitialScanAsync();
    void ResetState();
    Task<List<FileGroup>> ProcessFileEventsAsync(List<FileSystemEvent> events);
}
```

#### GroupStateManager
Manages persistent group state:

**State Management**:
- JSON serialization with debouncing (300ms default)
- Automatic save on group changes
- Load on application start
- Hash-based change detection to prevent unnecessary writes

**C# Optimization**:
- Use `System.Text.Json` for high-performance serialization
- Implement `INotifyCollectionChanged` for automatic save triggers
- Use `SemaphoreSlim` for thread-safe debouncing

**Implementation**:
```csharp
public interface IGroupStateManager
{
    Task SaveGroupStateAsync(List<FileGroup> groups, int debounceMs = 300);
    Task<List<FileGroup>> LoadGroupStateAsync();
    void EnableAutoSave(ObservableCollection<FileGroup> groups);
}
```

#### WindowStateManager
Manages window position and size persistence:

**C# Optimization**:
- Use WPF's built-in `Window.RestoreBounds` property
- Leverage `Settings.Default` for automatic persistence
- Handle multi-monitor scenarios with `SystemParameters.VirtualScreen`

**Implementation**:
```csharp
public interface IWindowStateManager
{
    void SaveWindowState(Window window);
    void RestoreWindowState(Window window);
    bool IsWindowOnScreen(Rect bounds);
}
```

#### CrashLogger
Global exception handling and crash reporting:

**C# Optimization**:
- Use `AppDomain.CurrentDomain.UnhandledException` for global handler
- Use `TaskScheduler.UnobservedTaskException` for async exceptions
- Use `Dispatcher.UnhandledException` for UI thread exceptions
- Integrate with Windows Event Log for production monitoring

**Implementation**:
```csharp
public interface ICrashLogger
{
    void InstallGlobalHandlers();
    void WriteCrashReport(Exception exception, string context);
    string GetLogPath();
}
```

#### Performance Monitoring (C# Optimized Approach)

**Python Approach** (ImageRegistry, Heartbeat, MemoryMonitor, BackgroundWorker):
- Manual registry for image-widget mapping
- Periodic heartbeat logging
- Manual memory monitoring
- Custom background worker threads

**C# Optimized Approach**:
Instead of directly porting Python's performance monitoring components, leverage C#/.NET's superior built-in capabilities:

1. **Image Loading Optimization**:
   - Use `BitmapImage` with `CacheOption.OnLoad` for immediate file handle release
   - Leverage WPF's built-in image caching via `BitmapCache`
   - Use `Dispatcher.InvokeAsync` with priority for smooth UI updates
   - Implement `IValueConverter` for automatic image path-to-bitmap conversion

2. **File System Monitoring**:
   - Use `FileSystemWatcher` with `NotifyFilters` optimization
   - Implement `BufferedEventHandler` pattern for high-frequency events
   - Use `ConcurrentQueue<T>` for thread-safe event buffering
   - Leverage `Task.Run` with `CancellationToken` for background processing

3. **Performance Monitoring**:
   - Use `System.Diagnostics.PerformanceCounter` for CPU/Memory metrics
   - Use `EventSource` for high-performance structured logging
   - Leverage `PerfView` or `dotTrace` for production profiling
   - Use `GC.GetTotalMemory` and `GC.Collect` for memory management

4. **Background Processing**:
   - Use `BackgroundWorker` or `Task.Run` with proper `SynchronizationContext`
   - Implement `IProgress<T>` for progress reporting
   - Use `async/await` throughout for non-blocking operations
   - Leverage `Channel<T>` for producer-consumer patterns

**Implementation Strategy**:
```csharp
// Instead of manual ImageRegistry, use WPF data binding with converters
public class ImagePathToSourceConverter : IValueConverter
{
    private static readonly BitmapImageCache _cache = new();
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string path)
            return _cache.GetOrLoad(path);
        return null;
    }
}

// Instead of manual heartbeat, use EventSource for structured logging
[EventSource(Name = "ChronoView-Performance")]
public sealed class PerformanceEventSource : EventSource
{
    [Event(1, Level = EventLevel.Informational)]
    public void Heartbeat(string status, int groupCount, long memoryMB) 
    {
        WriteEvent(1, status, groupCount, memoryMB);
    }
}

// Instead of manual background worker, use modern async patterns
public class FileCountService
{
    private readonly Channel<CountRequest> _channel = Channel.CreateUnbounded<CountRequest>();
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await foreach (var request in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            var counts = await CountFilesAsync(request.Paths);
            await _dispatcher.InvokeAsync(() => UpdateUI(counts));
        }
    }
}
```

#### StatisticsService
Provides real-time analytics and file counting:

**File Count Monitoring**:
- Background worker thread for non-blocking file counting
- Monitors all configured folders: NIR1/2, Normal1/2, Cam1-6
- Update frequency: Configurable (default 2 seconds)
- Thread-safe updates to UI via Dispatcher

**Matching Statistics Calculation**:
- Unified mode: Total, With NIR, Without NIR, Failed counts
- Separated mode: Independent statistics for Line 1 and Line 2
- Real-time updates when file groups change
- Match rate percentage calculation

**Implementation**:
```csharp
public interface IStatisticsService
{
    // File count monitoring
    Task<FileCountStatistics> GetFileCountsAsync();
    void StartFileCountMonitoring();
    void StopFileCountMonitoring();
    event EventHandler<FileCountStatistics> FileCountsUpdated;
    
    // Matching statistics
    MatchingStatistics CalculateUnifiedStats(List<FileGroup> groups);
    (MatchingStatistics line1, MatchingStatistics line2) CalculateSeparatedStats(
        List<FileGroup> line1Groups, 
        List<FileGroup> line2Groups);
}

public class FileCountStatistics
{
    public int NirCount { get; set; }
    public int Nir2Count { get; set; }
    public int NormalCount { get; set; }
    public int Normal2Count { get; set; }
    public int Cam1Count { get; set; }
    public int Cam2Count { get; set; }
    public int Cam3Count { get; set; }
    public int Cam4Count { get; set; }
    public int Cam5Count { get; set; }
    public int Cam6Count { get; set; }
}

public class MatchingStatistics
{
    public int TotalGroups { get; set; }
    public int WithNir { get; set; }
    public int WithoutNir { get; set; }
    public int Failed { get; set; }
    public double MatchRate => TotalGroups > 0 ? (double)WithNir / TotalGroups * 100 : 0;
}
```

**Performance Considerations**:
- File counting runs on background thread to avoid UI blocking
- Uses Directory.EnumerateFiles for efficient counting
- Caches results and only updates on changes
- Abnormal condition detection and reporting
- Performance metrics collection

### UI Components (WPF Implementation)

The WPF user interface follows the reference design at `C#_project/gui_c/components/chrono-view-pro.tsx` and consists of the following major components:

#### MainWindow Layout Structure
```
┌─────────────────────────────────────────────────────────┐
│ Title Bar & Menu Bar                                    │
├─────────────────────────────────────────────────────────┤
│ Toolbar (Start, Stop, Setup, Refresh, Move, Delete)    │
├─────────────────────────────────────────────────────────┤
│ 📊 File Count Statistics Bar                            │
│ NIR1:50 | Normal1:50 | Cam1:50 | Cam2:50 | Cam3:50 ... │
├─────────────────────────────────────────────────────────┤
│ 🔗 Matching Statistics Bar (Unified/Separated Mode)     │
│ Total:45 | With NIR:30 | Without NIR:15 | Failed:0     │
├──────────────┬──────────────────────────────────────────┤
│              │ Tab Control (Line1 | Line2 | Combined)  │
│  Left        │ ┌──────────────────────────────────────┐ │
│  Sidebar     │ │ File Group Table View                │ │
│              │ │ Index | Status | Main | NIR | Cams  │ │
│  - Control   │ │ #145  | ✓      | [img]| [img]| [...] │ │
│  - Status    │ │ #144  | ⚠      | [img]| ✗    | [...] │ │
│              │ └──────────────────────────────────────┘ │
│              ├──────────────────────────────────────────┤
│              │ Selected Group Detail Preview            │
│              │ [Main Img] [NIR Img] [Composite Cams]   │
│              ├──────────────────────────────────────────┤
│              │ Message Log                              │
│              │ Time | Event | Description               │
├──────────────┴──────────────────────────────────────────┤
│ Status Bar (Statistics, Connection Status, Time)        │
└─────────────────────────────────────────────────────────┘
```

#### FileGroupTableView (DataGrid)
**Purpose**: Display file groups in tabular format with real-time updates
**Key Features**:
- Columns: Checkbox, Index, Status, Main Image, NIR Image, Camera 1-3
- Row selection with visual feedback (highlight selected row)
- Individual image selection via checkboxes (enabled when row is selected)
- Status indicators with icons (Complete ✓, Missing ⚠, Abnormal ✗, Error)
- Thumbnail display for available images
- Support for Line 1/Line 2 tab switching

**WPF Implementation**:
```xaml
<DataGrid ItemsSource="{Binding FileGroups}" 
          SelectedItem="{Binding SelectedGroup}"
          AutoGenerateColumns="False">
    <DataGrid.Columns>
        <DataGridCheckBoxColumn Header="" Binding="{Binding IsSelected}"/>
        <DataGridTextColumn Header="Index" Binding="{Binding GroupId}"/>
        <DataGridTemplateColumn Header="Status">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <StatusIndicator Status="{Binding Status}"/>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
        <DataGridTemplateColumn Header="Main Img">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <ImageThumbnailControl ImagePath="{Binding MainImagePath}"/>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
        <!-- Additional columns for NIR and Camera images -->
    </DataGrid.Columns>
</DataGrid>
```

#### LeftSidebarPanel (StackPanel)
**Purpose**: Workflow control and system status display
**Sections**:
1. **Workflow Control (Control)**
   - System Status: Main Cam status (Ready/Error with colored indicator)
   - System Status: NIR Cam status (Ready/Error with colored indicator)
   - Sample Information: Sample name input field
   - Move NIR: NIR file movement configuration
   - Move All Data: Bulk data movement configuration

2. **Data Status**
   - Total Groups count
   - Match Rate percentage
   - Failures count (highlighted in red if > 0)

**WPF Implementation**: Use Expander controls for collapsible sections

#### DetailPreviewPanel (Grid)
**Purpose**: Display detailed view of selected file group
**Components**:
- Main Image preview (larger thumbnail)
- NIR Image preview with error state display
- Composite Camera Images grid (3x2 or configurable layout)
- Group metadata (Group ID, Status, timestamps)

#### MessageLogPanel (DataGrid)
**Purpose**: Real-time system event logging
**Columns**:
- Event Type (Info/Warning/Error with icons and colors)
- Timestamp (HH:mm:ss.fff format)
- Event Source (System, GroupManager, AbnormalDetect, etc.)
- Description (detailed message text)

**Color Coding**:
- Info: Blue icon
- Warning: Amber/Yellow icon
- Error: Red icon

#### ToolbarPanel (ToolBar)
**Buttons** (with icons and labels):
- Start: Begin monitoring/processing
- Stop: Stop current operations
- Setup: Open configuration dialog
- Refresh: Reload data
- Move: Execute file move operations
- Delete: Delete selected items

#### FileCountStatisticsBar (StackPanel/WrapPanel)
**Purpose**: Display real-time file counts for all monitored folders
**Components**:
- NIR1 Count: Number of NIR files in line 1 folder
- Normal1 Count: Number of normal camera images in line 1 folder
- Cam1-3 Counts: Individual camera file counts for line 1
- NIR2 Count: Number of NIR files in line 2 folder (separated mode only)
- Normal2 Count: Number of normal camera images in line 2 folder (separated mode only)
- Cam4-6 Counts: Individual camera file counts for line 2 (separated mode only)

**Visual Design**:
- Chip-style display with label and value
- Background: Light gray (#f5f5f5)
- Border: 1px solid #d0d0d0
- Font: Bold for values, regular for labels
- Update frequency: Real-time via background worker thread

**WPF Implementation**:
```xaml
<StackPanel Orientation="Horizontal" Background="#f5f5f5" Padding="12,8">
    <TextBlock Text="📊 파일 개수 현황:" FontWeight="Bold" Margin="0,0,10,0"/>
    <Border Style="{StaticResource ChipStyle}">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="NIR1:" Foreground="#6c757d"/>
            <TextBlock Text="{Binding NirCount}" FontWeight="Bold" Margin="5,0,0,0"/>
        </StackPanel>
    </Border>
    <!-- Repeat for Normal1, Cam1-6, NIR2, Normal2, Cam4-6 -->
</StackPanel>
```

#### MatchingStatisticsBar (StackPanel/WrapPanel)
**Purpose**: Display matching statistics in unified or separated mode
**Unified Mode Components**:
- Total Matched: Total number of file groups created
- With NIR: Groups that have NIR files matched
- Without NIR: Groups without NIR files
- Failed: Groups with matching errors

**Separated Mode Components** (Line 1 and Line 2 displayed separately):
- Line 1: Total, With NIR, Without NIR, Failed (blue color scheme)
- Line 2: Total, With NIR, Without NIR, Failed (red color scheme)

**Visual Design**:
- Chip-style display similar to file count bar
- Color coding: Blue for Line 1, Red for Line 2 in separated mode
- Failed count highlighted in red if > 0
- Update frequency: Real-time when groups are updated

**WPF Implementation**:
```xaml
<!-- Unified Mode -->
<StackPanel x:Name="UnifiedStatsBar" Orientation="Horizontal" Background="#f5f5f5" Padding="12,8">
    <TextBlock Text="🔗 매칭 현황:" FontWeight="Bold" Margin="0,0,10,0"/>
    <Border Style="{StaticResource ChipStyle}">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="총 매칭:" Foreground="#6c757d"/>
            <TextBlock Text="{Binding TotalGroups}" FontWeight="Bold" Margin="5,0,0,0"/>
        </StackPanel>
    </Border>
    <!-- Repeat for With NIR, Without NIR, Failed -->
</StackPanel>

<!-- Separated Mode -->
<StackPanel x:Name="SeparatedStatsBar" Orientation="Horizontal" Background="#f5f5f5" Padding="12,8">
    <TextBlock Text="🔗 라인1:" FontWeight="Bold" Foreground="#2563eb" Margin="0,0,10,0"/>
    <!-- Line 1 stats with blue theme -->
    <TextBlock Text="🔗 라인2:" FontWeight="Bold" Foreground="#dc2626" Margin="20,0,10,0"/>
    <!-- Line 2 stats with red theme -->
</StackPanel>
```

#### TabControl (Line1/Line2/Combined)
**Purpose**: Switch between different view modes for file groups
**Tabs**:
1. **Line1 Tab**: Display only line 1 file groups
2. **Line2 Tab**: Display only line 2 file groups (separated mode only)
3. **Combined Tab**: Display both lines side-by-side with 1:1 split ratio

**Visual Design**:
- Tab headers with clear labels
- Active tab highlighted
- Combined tab shows two DataGrids side-by-side with vertical splitter

**WPF Implementation**:
```xaml
<TabControl>
    <TabItem Header="라인1">
        <DataGrid ItemsSource="{Binding Line1Groups}"/>
    </TabItem>
    <TabItem Header="라인2" Visibility="{Binding IsSeparatedMode, Converter={StaticResource BoolToVisibilityConverter}}">
        <DataGrid ItemsSource="{Binding Line2Groups}"/>
    </TabItem>
    <TabItem Header="통합">
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="5"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <DataGrid Grid.Column="0" ItemsSource="{Binding Line1Groups}"/>
            <GridSplitter Grid.Column="1"/>
            <DataGrid Grid.Column="2" ItemsSource="{Binding Line2Groups}"/>
        </Grid>
    </TabItem>
</TabControl>
```

#### ImagePreviewDialog (Window)
**Purpose**: Display full-size image preview when thumbnail is clicked
**Features**:
- Full-size image display with scroll support
- EXIF rotation handling (auto-rotate based on EXIF orientation)
- Image title showing filename
- Close button or click-to-close functionality

**C# Implementation**:
```csharp
public class ImagePreviewDialog : Window
{
    public ImagePreviewDialog(BitmapSource image, string title)
    {
        Title = title;
        Content = new Image 
        { 
            Source = image,
            Stretch = Stretch.Uniform
        };
        // Handle EXIF rotation using BitmapMetadata
    }
}
```

#### DragSelectBehavior (Attached Behavior)
**Purpose**: Enable multi-row selection by dragging
**Features**:
- Mouse down starts selection
- Mouse move extends selection range
- Mouse up finalizes selection
- Visual feedback during drag

**C# Implementation** (using WPF Attached Behaviors):
```csharp
public static class DragSelectBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = 
        DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), 
            typeof(DragSelectBehavior), new PropertyMetadata(false, OnIsEnabledChanged));
    
    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGrid dataGrid && (bool)e.NewValue)
        {
            dataGrid.PreviewMouseDown += OnPreviewMouseDown;
            dataGrid.PreviewMouseMove += OnPreviewMouseMove;
            dataGrid.PreviewMouseUp += OnPreviewMouseUp;
        }
    }
    
    // Implement drag selection logic
}
```

#### LogPanel (Custom Control)
**Purpose**: Display system event logs with file persistence
**Features**:
- Real-time log message display
- Color-coded severity levels (Info: Blue, Warning: Amber, Error: Red)
- Timestamp for each message
- Auto-scroll to latest message
- File logging to disk (append mode)
- Log rotation support

**WPF Implementation**:
```xaml
<UserControl x:Class="ChronoView.UI.Controls.LogPanel">
    <DockPanel>
        <TextBox DockPanel.Dock="Top" Text="{Binding SearchText}" />
        <ListBox ItemsSource="{Binding LogMessages}" 
                 ScrollViewer.VerticalScrollBarVisibility="Auto">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <TextBlock>
                        <Run Text="{Binding Timestamp, StringFormat='[{0:HH:mm:ss}]'}" 
                             Foreground="Gray"/>
                        <Run Text="{Binding Message}" 
                             Foreground="{Binding Severity, Converter={StaticResource SeverityToColorConverter}}"/>
                    </TextBlock>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
    </DockPanel>
</UserControl>
```

#### SettingsDialog Advanced Options
**Additional Configuration Options**:

1. **Camera Subfolder Options**:
   - Use camera subfolder for Normal1 path (checkbox)
   - Use camera subfolder for Normal2 path (checkbox)
   - When enabled, monitors `{normal_path}/camera` instead of `{normal_path}`

2. **Image Processing Options**:
   - Enable disk cache for thumbnails (checkbox, default: true)
   - Cache directory location (text field with browse button)

3. **Matching Options**:
   - Use time-based matching for camera files (checkbox, default: true)
   - Camera match minimum time difference (numeric input, default: 4.0 seconds)
   - Camera match maximum time difference (numeric input, default: 6.0 seconds)
   - NIR match time difference (numeric input, default: 1.0 seconds)

4. **UI Options**:
   - Legacy UI mode (checkbox, default: false)
   - Use folder suffix for organization (checkbox, default: false)
   - Show tooltips (checkbox, default: true)

**WPF Implementation**:
```xaml
<TabControl>
    <TabItem Header="Paths">
        <!-- Existing path configuration -->
    </TabItem>
    <TabItem Header="Advanced">
        <StackPanel>
            <GroupBox Header="Camera Subfolder Options">
                <StackPanel>
                    <CheckBox Content="Use camera subfolder for Normal1" 
                              IsChecked="{Binding UseCameraSubfolderNormal}"/>
                    <CheckBox Content="Use camera subfolder for Normal2" 
                              IsChecked="{Binding UseCameraSubfolderNormal2}"/>
                </StackPanel>
            </GroupBox>
            <GroupBox Header="Image Processing">
                <StackPanel>
                    <CheckBox Content="Enable disk cache" 
                              IsChecked="{Binding UseDiskCache}"/>
                </StackPanel>
            </GroupBox>
            <GroupBox Header="Matching Options">
                <StackPanel>
                    <CheckBox Content="Use time-based camera matching" 
                              IsChecked="{Binding UseCamTimeMatching}"/>
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="Camera match min diff (sec):"/>
                        <TextBox Text="{Binding CamMatchMinDiff}" Width="60"/>
                    </StackPanel>
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="Camera match max diff (sec):"/>
                        <TextBox Text="{Binding CamMatchMaxDiff}" Width="60"/>
                    </StackPanel>
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="NIR match time diff (sec):"/>
                        <TextBox Text="{Binding NirMatchTimeDiff}" Width="60"/>
                    </StackPanel>
                </StackPanel>
            </GroupBox>
            <GroupBox Header="UI Options">
                <StackPanel>
                    <CheckBox Content="Legacy UI mode" 
                              IsChecked="{Binding LegacyUiMode}"/>
                    <CheckBox Content="Use folder suffix" 
                              IsChecked="{Binding UseFolderSuffix}"/>
                    <CheckBox Content="Show tooltips" 
                              IsChecked="{Binding ShowTooltips}"/>
                </StackPanel>
            </GroupBox>
        </StackPanel>
    </TabItem>
</TabControl>
```

#### StatusBar (StatusBar)
**Left Section**: Status message ("Ready", "Processing...", etc.)
**Right Section**: 
- Total Groups: [count]
- Match Rate: [percentage]
- Failures: [count] (red if > 0)
- NIR Connection: OK/Error (green/red)
- Current Time: HH:mm AM/PM

### UI Design Specifications

**Color Scheme** (from reference design):
- Background: #f0f0f0 (light gray)
- Panel Background: #f5f5f5
- Border: #d0d0d0
- Selected Row: #0078d4 (blue) with white text
- Hover: #e0e0e0
- Title Bar: #2d2d2d (dark gray)
- Error Text: Red (#ff0000 or similar)
- Warning Text: Amber (#ffa500 or similar)
- Success/OK: Green (#00ff00 or similar)

**Typography**:
- Default Font: Segoe UI or system default
- Font Size: 12px (standard), 10px (small labels), 14px (buttons)
- Monospace Font: Consolas (for logs and technical data)

**Spacing**:
- Panel Padding: 10-20px
- Control Margin: 2-5px
- Section Spacing: 10px

**Image Thumbnails**:
- Size: 40x40px (table view), 160x120px (detail view)
- Border: 1px solid #d0d0d0
- Background: #3a3a3a (for image placeholders)
- Missing Image: Red X icon or "No Image" text

## Data Models

### Core Data Structures

#### FileGroup
```csharp
public class FileGroup
{
    public string GroupId { get; set; }
    public string NirKey { get; set; }
    public string NormalFolder { get; set; }
    public Dictionary<string, string> CameraFiles { get; set; }
    public int LineNumber { get; set; }
    public bool HasNir { get; set; }
    public DateTime CreatedAt { get; set; }
    public GroupStatus Status { get; set; }
}
```

#### UnmatchedFiles
```csharp
public class UnmatchedFiles
{
    public Dictionary<string, string> NirFiles { get; set; }
    public Dictionary<string, string> NormalFolders { get; set; }
    public Dictionary<string, List<TimestampedFile>> CameraFiles { get; set; }
}
```

#### ApplicationConfiguration
```csharp
public class ApplicationConfiguration
{
    public Dictionary<string, string> FolderPaths { get; set; }
    public ImageSettings ImageSettings { get; set; }
    public MatchingSettings MatchingSettings { get; set; }
    public WorkflowSettings WorkflowSettings { get; set; }
}
```

#### ImageMetadata
```csharp
public class ImageMetadata
{
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime CreatedAt { get; set; }
    public long FileSize { get; set; }
    public string Format { get; set; }
    public bool IsAbnormal { get; set; }
    public double ZScoreWidth { get; set; }
    public double ZScoreHeight { get; set; }
}
```

#### NirSpectrum
```csharp
public class NirSpectrum
{
    public string FileName { get; set; }
    public DateTime Timestamp { get; set; }
    public double[] Wavelengths { get; set; }
    public double[] Intensities { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

### Data Persistence

**Independent Configuration Management**:

The C# system maintains its own configuration format, independent from the Python system:

- **config.json**: C#-specific application configuration settings
- **groups_state.json**: Current file group states for external process communication
- **move_plan.json**: File operation plans and metadata
- **moved_subjects.json**: Historical record of completed operations

**Configuration Strategy**:
- **No Python Compatibility Required**: The C# GUI manages its own settings independently
- **System.Text.Json**: Native C# serialization with optimal performance
- **Type-Safe Configuration**: Strongly-typed configuration classes with validation
- **User Settings**: Stored in user-specific application data folder

**Configuration Structure**:
```csharp
public class ApplicationConfiguration
{
    public Dictionary<string, string> FolderPaths { get; set; }
    public ImageSettings ImageSettings { get; set; }
    public MatchingSettings MatchingSettings { get; set; }
    public WorkflowSettings WorkflowSettings { get; set; }
    public WindowSettings WindowSettings { get; set; }
}
```

**Rationale**: 
- Python and C# systems operate independently
- No need for configuration file interoperability
- Allows C#-optimized configuration structure
- Simplifies implementation and maintenance

### State Management

The application uses a centralized state management approach:
- ViewModels maintain UI state using INotifyPropertyChanged
- Services maintain business logic state with event notifications
- Configuration changes trigger automatic UI updates through data binding
- File system events flow through the matching pipeline to update UI state

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property Reflection

After analyzing all acceptance criteria, several properties can be consolidated to eliminate redundancy:

- **Configuration Round-trip Properties**: Requirements 1.5 and 7.4 both test JSON configuration compatibility and can be combined into a single comprehensive property
- **Performance Comparison Properties**: Requirements 1.2, 8.1, 8.2, 8.3, and 8.5 all test performance improvements and can be grouped under a comprehensive performance property
- **Functional Equivalence Properties**: Requirements 7.1, 7.2, 7.3, and 7.5 all test that both systems produce identical results and can be combined
- **Implementation Choice Properties**: Requirements 3.1-3.5 all verify specific technology choices and can be grouped as examples rather than separate properties

### Core Properties

**Property 1: Functional Equivalence**
*For any* input dataset, configuration, or user interaction sequence, the C# system should produce identical results to the Python system
**Validates: Requirements 1.1, 1.3, 7.1, 7.2, 7.3, 7.5**

**Property 2: Configuration Persistence Reliability**
*For any* valid configuration object, serializing and then deserializing should produce an equivalent configuration with all settings preserved
**Validates: Requirements 1.5, 7.4**

**Property 3: Performance Improvement**
*For any* comparable workload (file processing, memory usage, startup time, CPU utilization), the C# system should demonstrate measurable performance improvements over the Python system
**Validates: Requirements 1.2, 8.1, 8.2, 8.3, 8.5**

**Property 4: Real-time File System Monitoring**
*For any* file system operation (create, modify, delete) in monitored directories, the system should detect and process the event within acceptable time limits
**Validates: Requirements 4.1**

**Property 5: File Grouping Consistency**
*For any* set of files with timestamps, the grouping algorithm should consistently group related files according to configured time windows
**Validates: Requirements 4.3**

**Property 6: NIR Processing Accuracy**
*For any* valid .spc file, the NIR processor should extract spectral data and metadata correctly and handle file movement operations reliably
**Validates: Requirements 4.2**

**Property 7: Asynchronous UI Responsiveness**
*For any* long-running file operation, the user interface should remain responsive and provide progress feedback without blocking user interactions
**Validates: Requirements 4.5, 8.4**

**Property 8: File Operation Reliability**
*For any* file move or copy operation, the system should provide progress tracking and complete rollback capability on failure
**Validates: Requirements 4.4**

**Property 9: Migration Tracker Completeness**
*For any* Python module analyzed, the migration tracker should generate comprehensive checklists with implementation feasibility, C# equivalents, and progress tracking
**Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5**

**Property 10: UI Data Display Consistency**
*For any* file group data, the UI should display all required elements (thumbnails, NIR info, status) in the correct tabular format with proper real-time updates
**Validates: Requirements 5.1, 5.4**

**Property 11: Configuration Management Completeness**
*For any* configuration option available in the Python system, the C# system should provide equivalent functionality with the same behavior
**Validates: Requirements 5.2**

**Property 12: Abnormal Condition Detection Consistency**
*For any* input data, the z-score algorithms and abnormal condition detection should produce identical results in both systems
**Validates: Requirements 5.3**

**Property 13: Multi-line Mode Support**
*For any* monitoring configuration, the system should correctly handle both integrated and separated line modes with proper data isolation and display
**Validates: Requirements 5.5**

**Property 14: Architecture Documentation Accuracy**
*For any* Python module set, the module analyzer should generate accurate mapping documents, dependency analysis, and implementation prioritization
**Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5**

## Error Handling

### Exception Handling Strategy

The C# system implements a comprehensive error handling strategy that improves upon the Python implementation:

#### Structured Exception Hierarchy
```csharp
public abstract class ChronoViewException : Exception
{
    public string ErrorCode { get; }
    public DateTime Timestamp { get; }
}

public class FileWatchingException : ChronoViewException { }
public class ImageProcessingException : ChronoViewException { }
public class NirProcessingException : ChronoViewException { }
public class ConfigurationException : ChronoViewException { }
```

#### Error Recovery Mechanisms
- **File System Errors**: Automatic retry with exponential backoff
- **Image Processing Errors**: Graceful degradation with placeholder images
- **NIR Processing Errors**: Quarantine invalid files with detailed error logging
- **Configuration Errors**: Fallback to default configuration with user notification

#### Logging and Monitoring
- Structured logging using Microsoft.Extensions.Logging
- Error aggregation and reporting for system health monitoring
- Integration with Windows Event Log for system-level error tracking
- Real-time error display in the application UI with severity classification

### Fault Tolerance

#### File System Resilience
- Handle network drive disconnections with automatic reconnection
- Manage file locking conflicts with retry mechanisms
- Detect and recover from file system permission changes

#### Memory Management
- Implement proper disposal patterns for image resources
- Use weak references for cached data to prevent memory leaks
- Monitor memory usage and trigger garbage collection when necessary

#### Concurrency Safety

**WPF Threading Model Strategy**:

The C# system must handle threading carefully due to WPF's strict UI thread requirements:

**FileSystemWatcher Event Handling**:
```csharp
private async void OnFileCreated(object sender, FileSystemEventArgs e)
{
    // FileSystemWatcher events fire on background thread
    // Process file on background thread
    await Task.Run(() => ProcessFile(e.FullPath));
    
    // UI updates MUST use Dispatcher
    await Dispatcher.InvokeAsync(() => 
    {
        LogTextBox.AppendText($"Processed: {e.Name}\n");
        StatusLabel.Content = "Ready";
    }, DispatcherPriority.Normal);
}
```

**Key Threading Patterns**:
1. **Background Processing**: Use `Task.Run()` for CPU-intensive operations (image processing, file operations, NIR analysis)
2. **UI Updates**: Always use `Dispatcher.InvokeAsync()` or `Dispatcher.Invoke()` when updating UI from background threads
3. **Cancellation**: Implement `CancellationToken` for all long-running operations
4. **Thread-Safe Collections**: Use `ConcurrentQueue<T>` or `ConcurrentDictionary<TKey, TValue>` for shared data

**Specific Threading Requirements**:
- **FileSystemWatcher**: Events occur on ThreadPool threads, not UI thread
- **Image Loading**: Background threads with Dispatcher marshalling for UI updates
- **File Operations**: Background tasks with progress reporting via Dispatcher
- **NIR Processing**: Async file processing with UI notification via Dispatcher

**Error Handling in Threading**:
- Catch exceptions in background tasks
- Marshal error messages to UI thread for display
- Ensure proper cleanup even on exceptions

## Testing Strategy

### Dual Testing Approach

The testing strategy combines unit testing and property-based testing to ensure comprehensive coverage:

#### Unit Testing
- **Framework**: xUnit with FluentAssertions for readable test assertions
- **Mocking**: Moq for dependency isolation
- **Coverage**: Minimum 80% code coverage for core business logic
- **Focus Areas**:
  - Individual component functionality
  - Error condition handling
  - Edge cases and boundary conditions
  - Integration points between components

#### Property-Based Testing
- **Framework**: FsCheck.NET for property-based testing in C#
- **Configuration**: Minimum 100 iterations per property test
- **Test Tagging**: Each property-based test tagged with format: `**Feature: python-gui-to-csharp-migration, Property {number}: {property_text}**`
- **Property Implementation**: Each correctness property implemented as a single property-based test

#### Testing Requirements
- Unit tests verify specific examples, edge cases, and error conditions
- Property tests verify universal properties that should hold across all inputs
- Both types of tests are complementary and provide comprehensive coverage
- Unit tests catch concrete bugs, property tests verify general correctness

#### Test Data Management
- **Test Datasets**: Maintain identical test datasets for both Python and C# systems
- **Golden Master Testing**: Compare outputs between systems for regression detection
- **Performance Benchmarking**: Automated performance comparison tests
- **Configuration Testing**: Verify configuration file interchangeability

#### Continuous Integration
- Automated test execution on every commit
- Performance regression detection
- Cross-platform testing (Windows, Linux via .NET Core)
- Integration testing with real file system operations

### Migration Validation Testing

#### Functional Parity Validation
- Side-by-side execution of both systems with identical inputs
- Automated comparison of outputs, configurations, and state files
- User acceptance testing with existing workflows
- Performance benchmarking under realistic workloads

#### Data Migration Testing
- Verify existing configuration files work with C# system
- Test state file compatibility and migration paths
- Validate historical data preservation and accessibility
- Ensure backup and recovery procedures work correctly

### Implementation Strategy

Based on the complete 41-module analysis, the implementation follows a 4-phase approach:

#### Phase 1: Foundation (Weeks 1-3) - 12 Critical Modules
**Focus**: Core infrastructure and configuration
- **config_manager.py** - Required by all other modules
- **crash_logger.py** - Essential error handling
- **main.py** - Application controller
- **utils.py** - Core utility functions
- **Data models** - Core data structures
- **Basic UI framework** - WPF project setup

#### Phase 2: Core Services (Weeks 4-6) - 15 High Priority Modules  
**Focus**: File processing and business logic
- **watchdog_manager.py** - File system monitoring
- **file_matcher.py** - Core matching logic
- **group_manager.py** - File grouping algorithms
- **image_manager.py** - Image processing pipeline
- **nir_app.py** - NIR processing capabilities
- **file_operations.py** - File operation workers

#### Phase 3: UI & Features (Weeks 7-9) - 12 Medium Priority Modules
**Focus**: User interface and monitoring
- **monitoring_app.py** - Main application UI
- **ui_components.py** - UI component library
- **Statistics services** - Analytics and reporting
- **Monitoring services** - Real-time monitoring
- **User experience enhancements**

#### Phase 4: Polish & Integration (Weeks 10-12) - 2 Low Priority Modules
**Focus**: Final utilities and system integration
- **tooltips.py** - UI enhancements
- **signal_handler.py** - OS integration
- **System integration testing**
- **Performance optimization**
- **Documentation completion**

### Risk Assessment and Mitigation

#### High Risk Items Identified
1. **NIR Spectrum Analysis** (nir_spectrum_monitor.py) - Complex pandas-based algorithms requiring careful migration
2. **Real-time File Processing** - High-frequency file system events under heavy load
3. **Complex UI Data Binding** - Real-time updates with large datasets
4. **Binary File Parsing** - .spc file format implementation complexity

#### Medium Risk Items
1. **Image Processing Pipeline** - Memory management for large image datasets
2. **File Operation Rollback** - Complex transaction-like file operations
3. **Multi-threading Coordination** - Converting QThread patterns to async/await
4. **Configuration Compatibility** - Exact JSON format matching

#### Mitigation Strategies
1. **Early Prototyping**: Implement high-risk components first for validation
2. **Incremental Testing**: Build and test in small iterations with real data
3. **Performance Monitoring**: Continuous benchmarking throughout development
4. **Compatibility Testing**: Side-by-side validation with Python system
5. **Fallback Implementations**: Alternative approaches for complex features

### Quality Assurance Framework

#### Completion Criteria
- [ ] All 41 modules implemented and tested
- [ ] All 400+ functions migrated with equivalent behavior
- [ ] Performance benchmarks met (25% improvement minimum)
- [ ] 100% configuration file compatibility
- [ ] Zero critical bugs in production scenarios

#### Quality Gates
- [ ] Code review completed for each module
- [ ] Unit test coverage ≥ 80% for all modules
- [ ] Property-based tests passing for all correctness properties
- [ ] Integration tests passing for all module interactions
- [ ] Performance tests meeting improvement targets
- [ ] User acceptance testing completed successfully

#### Success Metrics Validation
- **Functional**: 100% feature parity demonstrated across all 41 modules
- **Performance**: 25% improvement validated through automated benchmarking
- **Quality**: Zero critical bugs, 80%+ test coverage, all property tests passing
- **Timeline**: 184-hour estimate validated through actual implementation tracking
- **User Satisfaction**: Successful migration with minimal workflow disruption