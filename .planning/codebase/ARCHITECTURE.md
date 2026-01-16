# Architecture

**Analysis Date:** 2026-01-16

## Pattern Overview

**Overall:** 3-Layer MVVM Desktop Application

**Key Characteristics:**
- Single-executable Windows desktop application
- MVVM pattern for UI separation
- Service-oriented architecture with dependency injection
- Event-driven file monitoring workflow
- Polling-based file watching (WSL compatibility)

## Layers

**UI Layer (WPF/XAML):**
- Purpose: User interface and interaction
- Contains: ViewModels (MainWindowViewModel, DashboardViewModel, FileGroupViewModel, SettingsDialogViewModel, SetupWindowViewModel), Views (MainWindow, SplashWindow, ImagePreviewWindow, SettingsDialog, SetupWindow), Controls (FileGroupDataGrid, LogPanel, WorkflowPanel), Behaviors (DragSelectBehavior), Converters (CameraStateConverters)
- Location: `ChronoView/UI/`
- Depends on: Core services via dependency injection
- Used by: Application entry point (App.xaml.cs)

**Core Services Layer:**
- Purpose: Business logic and domain services
- Contains: FileWatching (MonitoringOrchestrator, FileWatcherService, EventProcessor, GroupManager, InitialScanner, ImageCaptureService), FileMatching (FileMatchingEngine, FileGroupMatcherService), FileOperations (FileOperationService, MoveService, DeleteService, FileGroupOperator, PathManagementService), Analytics (StatisticsService, AbnormalDetectorService, AbnormalHistoryManager), ImageProcessing (ImageProcessingService, LruCache), Configuration (ConfigurationManager, DefaultConfiguration, PathHelper, ConfigurationExtensions), NIR (NirSpectrumParser, SpcTxtNirFileResolver, NirSpectrumFilter, NirGraphGenerator), ProgramLaunching (GeneralCameraLauncher, NirCameraLauncher, Nir2CameraLauncher, NirFilteringService), GroupIdGeneration (GlobalGroupIdGenerator, LineBasedGroupIdGenerator), Localization (LocalizationManager), Logging (LogCleanupService)
- Location: `ChronoView/Core/`
- Depends on: Models, Infrastructure
- Used by: UI Layer via DI

**Helpers & Infrastructure:**
- Purpose: Shared utilities and cross-cutting concerns
- Contains: Helpers (FileNamingHelper, NormalFolderHelper, ProcessExtensions, WindowActivationHelper), Infrastructure/Logging (FileLoggerProvider, UILoggerProvider)
- Location: `ChronoView/Helpers/`, `ChronoView/Infrastructure/`
- Depends on: .NET framework only
- Used by: Core Services Layer

**Models Layer:**
- Purpose: Domain data structures
- Contains: FileGroup, UnmatchedFiles, ImageMetadata, NirSpectrum, ApplicationConfiguration, DataSequenceSettings, DataSequencePresets, CameraState
- Location: `ChronoView/Models/`
- Depends on: Nothing (pure data classes)
- Used by: All layers

## Data Flow

**File Monitoring Workflow:**

1. Application starts → SplashWindow → SetupWindow → MainWindow
2. User clicks Start → MonitoringOrchestrator.StartAsync()
3. FileWatcherService begins polling watch folders (WSL compatibility)
4. File created → EventProcessor queues with priority
5. MonitoringOrchestrator.ProcessNewFilesAsync() → FileMatchingEngine.MatchFiles()
6. Match results → GroupManager.CreateOrUpdateGroups()
7. DashboardViewModel.UpdateFileGroups() → UI refresh via INotifyPropertyChanged
8. AbnormalDetectorService analyzes each group → UI indicator

**State Management:**
- FileGroups collection maintained in DashboardViewModel
- Configuration persisted via ConfigurationManager to %APPDATA%
- Abnormal history persisted to JSON file

## Key Abstractions

**Service:**
- Purpose: Encapsulate business logic for a domain
- Examples: `ChronoView/Core/FileWatching/FileWatcherService.cs`, `ChronoView/Core/FileMatching/FileMatchingEngine.cs`, `ChronoView/Core/Analytics/StatisticsService.cs`
- Pattern: Singleton lifestyle in DI (services maintain state)

**ViewModel:**
- Purpose: MVVM presentation logic with INotifyPropertyChanged
- Examples: `ChronoView/UI/ViewModels/DashboardViewModel.cs`, `ChronoView/UI/ViewModels/FileGroupViewModel.cs`
- Pattern: Transient lifestyle in DI (created per view)

**Interface:**
- Purpose: Abstraction for DI and testing
- Examples: `IFileWatcher`, `IMonitoringOrchestrator`, `IConfigurationManager`
- Pattern: IPrefixed naming, all major services have interfaces

## Entry Points

**Application Entry:**
- Location: `ChronoView/App.xaml.cs`
- Triggers: Process launch
- Responsibilities: DI setup (ConfigureServices), global exception handling, splash screen, window navigation

**Windows:**
- SetupWindow: Initial configuration flow
- MainWindow: Primary application interface
- SettingsDialog: Configuration management
- ImagePreviewWindow: Image detail view

## Error Handling

**Strategy:** Global exception handlers at App.xaml.cs level, try/catch in async methods

**Patterns:**
- DispatcherUnhandledException for UI thread exceptions
- AppDomain.CurrentDomain.UnhandledException for background threads
- TaskScheduler.UnobservedTaskException for unobserved task exceptions
- Async methods use try/catch with logging
- Errors logged to file and shown via MessageBox

## Cross-Cutting Concerns

**Logging:**
- Microsoft.Extensions.Logging with custom FileLoggerProvider
- Structured logging with scopes, levels (Debug to Critical)
- UI logging via UILoggerProvider

**Validation:**
- DataAnnotation attributes on models
- Manual validation in ViewModels
- Property-based tests with FsCheck

**Localization:**
- LocalizationManager with .resx resources
- Resource strings in `Resources/Strings.Designer.cs`

**Dependency Injection:**
- Microsoft.Extensions.DependencyInjection container
- Singleton for stateful services, Transient for ViewModels
- Service registration in App.xaml.cs ConfigureServices() (line ~176)

---

*Architecture analysis: 2026-01-16*
*Update when major patterns change*
