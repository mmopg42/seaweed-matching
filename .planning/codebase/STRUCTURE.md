# Codebase Structure

**Analysis Date:** 2026-01-16

## Directory Layout

```
gui_kiro_v2/
├── ChronoView/                # Main WPF application
│   ├── App.xaml               # Application entry point and DI setup
│   ├── MainWindow.xaml        # Primary window XAML
│   ├── Converters/            # WPF value converters
│   ├── Core/                  # Business logic services
│   │   ├── Analytics/         # Statistics, abnormal detection
│   │   ├── Configuration/     # Settings, config management
│   │   ├── FileMatching/      # File grouping logic
│   │   ├── FileOperations/    # Move, delete, path management
│   │   ├── FileWatching/      # Monitoring, event processing
│   │   ├── GroupIdGeneration/ # Group ID strategies
│   │   ├── ImageProcessing/   # Image loading, caching
│   │   ├── Localization/      # Multi-language support
│   │   ├── Logging/           # Log cleanup service
│   │   ├── NIR/               # Spectroscopy data handling
│   │   └── ProgramLaunching/  # Camera program launchers
│   ├── Helpers/               # Shared utility functions
│   ├── Infrastructure/        # Logging providers
│   ├── Models/                # Domain data classes
│   ├── Resources/             # Images, localization resources
│   └── UI/                    # View layer
│       ├── Behaviors/         # Attached behaviors
│       ├── Controls/          # Custom controls
│       ├── Converters/        # UI value converters
│       ├── ViewModels/        # MVVM ViewModels
│       └── Views/             # Windows and dialogs
├── ChronoView.Tests/          # Test project
│   └── (Mirrors src structure)
├── docs/                      # Documentation
│   ├── architecture/          # Design documentation
│   ├── implementation/        # Implementation notes
│   ├── manual/                # User manual
│   └── spec/                  # Feature specifications
├── script/                    # Development utilities
└── Tools/                     # Helper tools
```

## Directory Purposes

**ChronoView/Core/Analytics/:**
- Purpose: Statistical analysis and anomaly detection
- Contains: AbnormalDetectorService, StatisticsService, AbnormalHistoryManager, MatchingStatistics, FileCountStatistics
- Key files: `AbnormalDetectorService.cs` (aspect ratio deviation detection), `StatisticsService.cs` (file metrics)

**ChronoView/Core/Configuration/:**
- Purpose: Application settings management
- Contains: ConfigurationManager, DefaultConfiguration, PathHelper, ConfigurationExtensions
- Key files: `ConfigurationManager.cs` (JSON persistence), `DefaultConfiguration.cs` (factory defaults)

**ChronoView/Core/FileWatching/:**
- Purpose: File system monitoring and event processing
- Contains: MonitoringOrchestrator, FileWatcherService, EventProcessor, GroupManager, InitialScanner, ImageCaptureService
- Key files: `MonitoringOrchestrator.cs` (workflow coordinator), `FileWatcherService.cs` (polling-based watcher)

**ChronoView/Core/FileMatching/:**
- Purpose: File grouping by timestamp
- Contains: FileMatchingEngine, FileGroupMatcherService, IFileGroupMatcher
- Key files: `FileMatchingEngine.cs` (timestamp matching logic)

**ChronoView/Core/FileOperations/:**
- Purpose: File move/delete/copy operations
- Contains: FileOperationService, MoveService, DeleteService, FileGroupOperator, PathManagementService
- Key files: `FileGroupOperator.cs` (group-level operations), `MoveService.cs` (conflict resolution)

**ChronoView/Core/ImageProcessing/:**
- Purpose: Image loading, caching, thumbnail generation
- Contains: ImageProcessingService, LruCache
- Key files: `ImageProcessingService.cs` (async image loading), `LruCache.cs` (memory-bound cache)

**ChronoView/Core/NIR/:**
- Purpose: Near-infrared spectroscopy data handling
- Contains: NirSpectrumParser, NirGraphGenerator, NirSpectrumFilter, SpcTxtNirFileResolver
- Key files: `NirSpectrumParser.cs` (SPC/TXT file parsing), `NirGraphGenerator.cs` (ScottPlot integration)

**ChronoView/Core/ProgramLaunching/:**
- Purpose: External camera program management
- Contains: GeneralCameraLauncher, NirCameraLauncher, Nir2CameraLauncher, NirFilteringService
- Key files: Camera launchers with activation/status tracking

**ChronoView/UI/ViewModels/:**
- Purpose: MVVM presentation layer
- Contains: MainWindowViewModel, DashboardViewModel, FileGroupViewModel, SettingsDialogViewModel, SetupWindowViewModel, SystemControlViewModel, FileOperationViewModel, ViewModelBase, RelayCommand
- Key files: `DashboardViewModel.cs` (main display logic), `ViewModelBase.cs` (INotifyPropertyChanged base)

**ChronoView/Models/:**
- Purpose: Domain data structures
- Contains: FileGroup, UnmatchedFiles, ImageMetadata, NirSpectrum, ApplicationConfiguration, DataSequenceSettings, DataSequencePresets, CameraState
- Key files: `FileGroup.cs` (core data structure), `ApplicationConfiguration.cs` (settings model)

## Key File Locations

**Entry Points:**
- `ChronoView/App.xaml.cs` - Application entry, DI container setup, global exception handling
- `ChronoView/MainWindow.xaml` - Primary window definition
- `ChronoView/MainWindow.xaml.cs` - Code-behind for main window

**Configuration:**
- `ChronoView/ChronoView.csproj` - Project file, NuGet packages
- `ChronoView/Core/Configuration/ConfigurationManager.cs` - Settings persistence
- `ChronoView/Core/Configuration/DefaultConfiguration.cs` - Default values

**Core Logic:**
- `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs` - Main workflow coordinator
- `ChronoView/Core/FileMatching/FileMatchingEngine.cs` - File grouping algorithm
- `ChronoView/Core/Analytics/AbnormalDetectorService.cs` - Anomaly detection

**Testing:**
- `ChronoView.Tests/ChronoView.Tests.csproj` - Test project
- `ChronoView.Tests/**/*Tests.cs` - Unit and integration tests

**Documentation:**
- `docs/architecture/glossary.md` - Official naming conventions (REQUIRED before modifying code)
- `docs/architecture/README.md` - Architecture overview
- `CLAUDE.md` - Project instructions for Claude Code

## Naming Conventions

**Files:**
- PascalCase.cs for all C# files: `FileWatcherService.cs`
- PascalCase.xaml for WPF views: `MainWindow.xaml`
- *Tests.cs for test files: `FileMatchingEngineTests.cs`
- I*.cs for interfaces: `IFileWatcher.cs`

**Directories:**
- PascalCase for all directories: `FileWatching/`, `Analytics/`
- Plural names for collections: `Models/`, `ViewModels/`, `Converters/`

**Special Patterns:**
- *Service.cs suffix for service classes: `FileWatcherService.cs`
- *ViewModel.cs suffix for ViewModels: `DashboardViewModel.cs`
- Manager suffix for coordination classes: `GroupManager.cs`

## Where to Add New Code

**New Feature (Service):**
- Implementation: `ChronoView/Core/{FeatureName}/{FeatureName}Service.cs`
- Interface: `ChronoView/Core/{FeatureName}/I{FeatureName}Service.cs`
- Tests: `ChronoView.Tests/Core/{FeatureName}/{FeatureName}ServiceTests.cs`
- DI registration: `App.xaml.cs` ConfigureServices() method

**New Feature (UI):**
- ViewModel: `ChronoView/UI/ViewModels/{FeatureName}ViewModel.cs`
- View: `ChronoView/UI/Views/{FeatureName}Window.xaml` (or .xaml/.cs)
- Tests: `ChronoView.Tests/UI/ViewModels/{FeatureName}ViewModelTests.cs`
- DI registration: Transient in ConfigureServices()

**New Model:**
- Implementation: `ChronoView/Models/{ModelName}.cs`
- Tests: `ChronoView.Tests/Models/{ModelName}Tests.cs`

**New Utility:**
- Helper functions: `ChronoView/Helpers/{UtilityName}Helper.cs`

## Special Directories

**ChronoView/Resources/:**
- Purpose: Embedded resources (images, localized strings)
- Source: Build action=Resource or EmbeddedResource
- Committed: Yes

**docs/ and .planning/:**
- Purpose: Documentation and planning
- Source: Markdown files
- Committed: Yes

---

*Structure analysis: 2026-01-16*
*Update when directory structure changes*
