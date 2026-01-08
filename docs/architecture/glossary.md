---
Owner: ChronoView Team
Last Updated: 2025-01-05
Purpose: Single Source of Truth for naming conventions
---

# Glossary

> **CRITICAL**: All architecture documents MUST use terms exactly as defined here.  
> If you need a new term, ADD IT HERE FIRST before using in any document.

## How to Use This Glossary

1. **Before writing any architecture doc**: Search this file for existing terms
2. **Before introducing new naming**: Add to appropriate section below
3. **If conflict found**: Update ALL documents to match glossary

## Verification Command
```bash
# Check if a term exists in glossary
grep -n "term_name" docs/architecture/glossary.md

# Find all usages of a term across docs
grep -rn "term_name" docs/architecture/
```

---

## Classes

| Official Name | Description | Defined In |
|---------------|-------------|------------|
| `FileGroup` | Core data structure representing a matched set of files from different cameras (general, NIR, NIR2) | `Models/FileGroup.cs` |
| `UnmatchedFiles` | Collection of files that haven't been matched into groups yet, organized by folder type | `Models/UnmatchedFiles.cs` |
| `DataSequenceSettings` | Configuration for file naming patterns and sequence detection rules | `Models/DataSequenceSettings.cs` |
| `DataSequencePresets` | Predefined sequence configurations for common camera setups | `Models/DataSequencePresets.cs` |
| `ImageMetadata` | Metadata extracted from image files including dimensions, timestamp, and file path | `Models/ImageMetadata.cs` |
| `NirSpectrum` | NIR spectroscopy data structure containing wavelength and intensity value pairs | `Models/NirSpectrum.cs` |
| `ApplicationConfiguration` | Application-wide settings and user preferences | `Models/ApplicationConfiguration.cs` |
| `MonitoringOrchestrator` | Service that coordinates the file monitoring workflow between FileWatcher, FileMatcher, and GroupManager | `Core/FileWatching/MonitoringOrchestrator.cs` |
| `FileMatchingEngine` | Service responsible for matching files into groups based on timestamps and naming patterns | `Core/FileMatching/FileMatchingEngine.cs` |
| `FileGroupMatcherService` | Service that implements file group matching logic | `Core/FileMatching/FileGroupMatcherService.cs` |
| `ConfigurationManager` | Service for loading, saving, and managing application configuration | `Core/Configuration/ConfigurationManager.cs` |
| `FileWatcherService` | Service for monitoring file system changes in real-time using FileSystemWatcher | `Core/FileWatching/FileWatcherService.cs` |
| `AbnormalDetectorService` | Service for detecting anomalies in file groups using statistical analysis (Z-score) | `Core/Analytics/AbnormalDetectorService.cs` |
| `StatisticsService` | Service for collecting and calculating file matching statistics | `Core/Analytics/StatisticsService.cs` |
| `ImageProcessingService` | Service for loading, caching, and processing images asynchronously | `Core/ImageProcessing/ImageProcessingService.cs` |
| `FileOperationService` | Service for coordinating file operations (move, copy, delete) | `Core/FileOperations/FileOperationService.cs` |
| `DeleteService` | Service for safely deleting files with bucket organization | `Core/FileOperations/DeleteService.cs` |
| `MoveService` | Service for moving files with conflict resolution | `Core/FileOperations/MoveService.cs` |
| `PathManagementService` | Service for managing file paths and directory structures | `Core/FileOperations/PathManagementService.cs` |
| `FileGroupOperator` | Service for performing operations on entire file groups | `Core/FileOperations/FileGroupOperator.cs` |
| `EventProcessor` | Service for processing file system events with priority queuing | `Core/FileWatching/EventProcessor.cs` |
| `GroupManager` | Service for creating and managing file groups based on matching criteria | `Core/FileWatching/GroupManager.cs` |
| `ImageCaptureService` | Service for capturing images from file system events | `Core/FileWatching/ImageCaptureService.cs` |
| `InitialScanner` | Service for performing initial file system scan on startup | `Core/FileWatching/InitialScanner.cs` |
| `FolderTimestampCache` | Cache for storing folder modification timestamps to optimize scanning | `Core/FileWatching/FolderTimestampCache.cs` |
| `PriorityEventChannel` | Channel for managing file system events with priority ordering | `Core/FileWatching/PriorityEventChannel.cs` |
| `EventPriority` | Enum defining priority levels for file system events | `Core/FileWatching/EventPriority.cs` |
| `FileWatcherOptions` | Configuration options for FileWatcherService | `Core/FileWatching/FileWatcherOptions.cs` |
| `LruCache` | Least Recently Used cache implementation for image caching | `Core/ImageProcessing/LruCache.cs` |
| `NirSpectrumParser` | Parser for NIR spectroscopy data files | `Core/NIR/NirSpectrumParser.cs` |
| `NirGraphGenerator` | Generator for creating NIR spectrum graphs | `Core/NIR/NirGraphGenerator.cs` |
| `NirSpectrumFilter` | Filter for processing and cleaning NIR spectrum data | `Core/NIR/NirSpectrumFilter.cs` |
| `SpcTxtNirFileResolver` | Resolver for SPC and TXT format NIR files | `Core/NIR/SpcTxtNirFileResolver.cs` |
| `LocalizationManager` | Manager for multi-language support and resource strings | `Core/Localization/LocalizationManager.cs` |
| `GeneralCameraLauncher` | Launcher for general camera capture programs | `Core/ProgramLaunching/GeneralCameraLauncher.cs` |
| `NirCameraLauncher` | Launcher for NIR camera capture programs | `Core/ProgramLaunching/NirCameraLauncher.cs` |
| `Nir2CameraLauncher` | Launcher for secondary NIR camera capture programs | `Core/ProgramLaunching/Nir2CameraLauncher.cs` |
| `FileCountStatistics` | Statistics model for file counts by type | `Core/Analytics/FileCountStatistics.cs` |
| `MatchingStatistics` | Statistics model for file matching metrics | `Core/Analytics/MatchingStatistics.cs` |
| `DashboardViewModel` | ViewModel for the main dashboard view displaying file groups | `UI/ViewModels/DashboardViewModel.cs` |
| `FileGroupViewModel` | ViewModel representing a single file group in the UI | `UI/ViewModels/FileGroupViewModel.cs` |
| `FileOperationViewModel` | ViewModel for file operation controls | `UI/ViewModels/FileOperationViewModel.cs` |
| `SystemControlViewModel` | ViewModel for system monitoring and control | `UI/ViewModels/SystemControlViewModel.cs` |
| `MainWindowViewModel` | ViewModel for the main application window | `UI/ViewModels/MainWindowViewModel.cs` |
| `SettingsDialogViewModel` | ViewModel for application settings dialog | `UI/ViewModels/SettingsDialogViewModel.cs` |
| `SetupWindowViewModel` | ViewModel for initial setup window | `UI/ViewModels/SetupWindowViewModel.cs` |
| `DetailPreviewViewModel` | ViewModel for detailed file preview | `UI/ViewModels/DetailPreviewViewModel.cs` |
| `DataSequenceItemViewModel` | ViewModel for individual data sequence items | `UI/ViewModels/DataSequenceItemViewModel.cs` |
| `FileGroupMediaLoader` | Service for loading media files for file groups | `UI/ViewModels/FileGroupMediaLoader.cs` |
| `ViewModelBase` | Base class for all ViewModels implementing INotifyPropertyChanged | `UI/ViewModels/ViewModelBase.cs` |
| `RelayCommand` | ICommand implementation for ViewModel command binding | `UI/ViewModels/RelayCommand.cs` |
| `LogMessage` | Model representing a log message in the UI | `UI/ViewModels/LogMessage.cs` |
| `UILoggerProvider` | Custom logger provider that outputs to UI components | `Infrastructure/Logging/UILoggerProvider.cs` |
| `BoolToVisibilityConverter` | Converts boolean values to WPF Visibility enum | `Converters/BoolToVisibilityConverter.cs` |
| `NullToVisibilityConverter` | Converts null values to WPF Visibility enum | `Converters/NullToVisibilityConverter.cs` |
| `FileNamingHelper` | Helper class for file naming pattern operations | `Helpers/FileNamingHelper.cs` |
| `PlaceholderImageHelper` | Helper class for generating placeholder images | `Helpers/PlaceholderImageHelper.cs` |
| `ResourceHelper` | Helper class for loading application resources | `Helpers/ResourceHelper.cs` |
| `DragSelectBehavior` | Attached behavior for drag-to-select functionality in DataGrid | `UI/Behaviors/DragSelectBehavior.cs` |
| `DataSequenceSettingsValidation` | Validation class for DataSequenceSettings | `Tests/DataSequenceSettingsValidation.cs` |

> **Naming Convention**: Use `PascalCase` for all class names.

---

## Interfaces

| Official Name | Description | Defined In |
|---------------|-------------|------------|
| `IConfigurationManager` | Interface for configuration management services | `Core/Configuration/IConfigurationManager.cs` |
| `IFileGroupMatcher` | Interface for file group matching services | `Core/FileMatching/IFileGroupMatcher.cs` |
| `IMonitoringOrchestrator` | Interface for workflow orchestration services | `Core/FileWatching/IMonitoringOrchestrator.cs` |
| `IFileWatcher` | Interface for file system monitoring services | `Core/FileWatching/IFileWatcher.cs` |
| `IEventProcessor` | Interface for file system event processing | `Core/FileWatching/IEventProcessor.cs` |
| `IGroupManager` | Interface for file group management | `Core/FileWatching/IGroupManager.cs` |
| `IImageCaptureService` | Interface for image capture services | `Core/FileWatching/IImageCaptureService.cs` |
| `ITimestampCache` | Interface for timestamp caching services | `Core/FileWatching/ITimestampCache.cs` |
| `IFileOperationService` | Interface for file operation services | `Core/FileOperations/IFileOperationService.cs` |
| `IDeleteService` | Interface for file deletion services | `Core/FileOperations/IDeleteService.cs` |
| `IMoveService` | Interface for file moving services | `Core/FileOperations/IMoveService.cs` |
| `IPathManagementService` | Interface for path management services | `Core/FileOperations/IPathManagementService.cs` |
| `IFileGroupOperator` | Interface for file group operations | `Core/FileOperations/IFileGroupOperator.cs` |
| `IImageProcessor` | Interface for image processing services | `Core/ImageProcessing/IImageProcessor.cs` |
| `IAbnormalDetector` | Interface for anomaly detection services | `Core/Analytics/IAbnormalDetector.cs` |
| `IStatisticsService` | Interface for statistics collection services | `Core/Analytics/IStatisticsService.cs` |
| `INirFileResolver` | Interface for NIR file resolution services | `Core/NIR/INirFileResolver.cs` |
| `IDashboardViewModel` | Interface for dashboard ViewModel | `UI/ViewModels/IDashboardViewModel.cs` |
| `IFileOperationViewModel` | Interface for file operation ViewModel | `UI/ViewModels/IFileOperationViewModel.cs` |
| `ISystemControlViewModel` | Interface for system control ViewModel | `UI/ViewModels/ISystemControlViewModel.cs` |

> **Naming Convention**: Use `IPascalCase` for all interface names (I prefix).

---

## Methods (Common Patterns)

| Pattern | Example | Description |
|---------|---------|-------------|
| `Get*` | `GetConfiguration()` | Retrieve data or objects |
| `Set*` | `SetConfiguration()` | Update data or objects |
| `Load*` | `LoadConfiguration()` | Load from persistent storage |
| `Save*` | `SaveConfiguration()` | Save to persistent storage |
| `Create*` | `CreateFileGroup()` | Create new instances |
| `Delete*` | `DeleteFileGroup()` | Remove instances |
| `Update*` | `UpdateFileGroup()` | Modify existing instances |
| `Process*` | `ProcessEvent()` | Process or transform data |
| `Validate*` | `ValidateSettings()` | Validate data integrity |
| `Initialize*` | `InitializeService()` | Setup or initialize |
| `Start*` | `StartMonitoring()` | Begin an operation |
| `Stop*` | `StopMonitoring()` | End an operation |
| `On*` | `OnFileCreated()` | Event handlers |
| `Can*` | `CanExecute()` | Boolean checks for commands |
| `Is*` | `IsValid()` | Boolean property checks |
| `Has*` | `HasChanges()` | Boolean existence checks |

> **Naming Convention**: Use `PascalCase` for all method names (C# standard).

---

## Properties

| Pattern | Example | Description |
|---------|---------|-------------|
| `Is*` | `IsEnabled` | Boolean state properties |
| `Has*` | `HasErrors` | Boolean existence properties |
| `Can*` | `CanSave` | Boolean capability properties |
| Noun | `Configuration` | Object properties |
| Noun | `FileGroups` | Collection properties (plural) |

> **Naming Convention**: Use `PascalCase` for all property names.

---

## Configuration Keys

| Official Name | Type | Default | Description | Used In |
|---------------|------|---------|-------------|---------|
| `monitoring.enabled` | `bool` | `true` | Enable/disable file monitoring | `FileWatcherService` |
| `monitoring.interval_ms` | `int` | `1000` | File monitoring interval in milliseconds | `FileWatcherService` |
| `matching.time_threshold_sec` | `int` | `5` | Time threshold for file matching in seconds | `FileMatchingEngine` |
| `matching.require_all_cameras` | `bool` | `false` | Require files from all cameras for a match | `FileMatchingEngine` |
| `paths.general_camera` | `string` | `""` | Path to general camera folder | `ConfigurationManager` |
| `paths.nir_camera` | `string` | `""` | Path to NIR camera folder | `ConfigurationManager` |
| `paths.nir2_camera` | `string` | `""` | Path to secondary NIR camera folder | `ConfigurationManager` |
| `paths.output` | `string` | `""` | Path to output folder | `FileOperationService` |
| `paths.delete_bucket` | `string` | `""` | Path to delete bucket folder | `DeleteService` |
| `image.cache_size_mb` | `int` | `100` | Image cache size in megabytes | `ImageProcessingService` |
| `image.thumbnail_size` | `int` | `200` | Thumbnail size in pixels | `ImageProcessingService` |
| `ui.language` | `string` | `"en"` | UI language code | `LocalizationManager` |
| `ui.theme` | `string` | `"light"` | UI theme (light/dark) | `MainWindowViewModel` |
| `statistics.window_size` | `int` | `100` | Statistics rolling window size | `StatisticsService` |
| `statistics.z_score_threshold` | `double` | `3.0` | Z-score threshold for anomaly detection | `AbnormalDetectorService` |

> **Naming Convention**: Use `lowercase.dot.notation` for config keys.

---

## Environment Variables

| Official Name | Description | Required | Used In |
|---------------|-------------|----------|---------|
| `CHRONOVIEW_CONFIG_PATH` | Override default configuration file path | No | `ConfigurationManager` |
| `CHRONOVIEW_LOG_LEVEL` | Logging level (Debug, Info, Warning, Error) | No | `UILoggerProvider` |
| `CHRONOVIEW_DATA_DIR` | Override default data directory | No | `ConfigurationManager` |

> **Naming Convention**: Use `UPPER_SNAKE_CASE` with `CHRONOVIEW_` prefix.

---

## Domain Concepts

| Term | Definition | Related Docs |
|------|------------|--------------|
| File Group | A matched set of files from different cameras (general, NIR, NIR2) that belong to the same capture event, identified by timestamp | `Models/FileGroup.md` |
| Unmatched Files | Files that have been detected but not yet matched into a group, organized by folder type (general, NIR, NIR2) | `Models/UnmatchedFiles.md` |
| Monitoring Orchestration | The coordination of file watching, matching, and group management workflows | `Core/FileWatching/MonitoringOrchestrator.md` |
| File Matching | The process of grouping files from different cameras based on timestamps and naming patterns | `Core/FileMatching/` |
| Bucket Organization | A file organization strategy where files are grouped into "buckets" (folders) based on criteria like date or subject | `Core/FileOperations/` |
| NIR Spectrum | Near-Infrared spectroscopy data consisting of wavelength-intensity pairs used for material analysis | `Models/NirSpectrum.md` |
| Data Sequence | A pattern-based file naming convention that defines how files are numbered and organized | `Models/DataSequenceSettings.md` |
| Anomaly Detection | Statistical analysis using Z-scores to detect abnormal file groups (e.g., missing files, wrong dimensions) | `Core/Analytics/AbnormalDetectorService.md` |
| Event Priority | A system for prioritizing file system events (High, Normal, Low) to ensure critical events are processed first | `Core/FileWatching/EventPriority.md` |
| Timestamp Cache | A cache storing folder modification timestamps to optimize file system scanning by avoiding redundant scans | `Core/FileWatching/FolderTimestampCache.md` |

---

## Deprecated Terms (Do Not Use)

| Deprecated | Use Instead | Reason | Deprecated Date |
|------------|-------------|--------|-----------------|
| `FileManager` | `FileOperationService` | Renamed for clarity | 2024-12-01 |
| `ImageLoader` | `ImageProcessingService` | Renamed to reflect broader scope | 2024-12-01 |
| `ConfigService` | `ConfigurationManager` | Standardized naming | 2024-12-01 |

---

## Changelog

- 2025-01-05: Initial glossary creation with 14 core terms
- 2025-01-05: Added all ChronoView classes, interfaces, and domain concepts
- 2025-01-05: Added configuration keys and environment variables
- 2025-01-05: Added method and property naming patterns
