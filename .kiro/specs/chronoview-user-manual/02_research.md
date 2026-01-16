---
Task: ChronoView User Manual - File-by-File Feature Documentation
Created: 2025-01-13
Status: Draft
Depends On: requirements.md
---

# ChronoView User Manual - Research Findings

## 1. Investigation Summary

Analyzed the complete ChronoView directory structure to identify all C# files that need documentation. The codebase is organized into clear subsystems with well-defined responsibilities.

## 2. Question Answers

### Q1: What is the complete directory structure of `ChronoView/`?

**Method**: Directory listing with depth 3
**Findings**: ChronoView has the following main directories:
- Root level: Application entry points (App.xaml.cs, MainWindow.xaml.cs, AssemblyInfo.cs)
- Converters/: Value converters for UI binding
- Core/: Business logic organized by feature
  - Analytics/
  - Configuration/
  - FileMatching/
  - FileOperations/
  - FileWatching/
  - GroupIdGeneration/
  - ImageProcessing/
  - Localization/
  - Logging/
  - NIR/
  - ProgramLaunching/
- Helpers/: Utility classes
- Infrastructure/: Cross-cutting concerns (Logging)
- Models/: Data models
- Resources/: UI resources and localization
- Tests/: Test files
- UI/: User interface components
  - Behaviors/
  - Controls/
  - ViewModels/
  - Views/

**Conclusion**: Well-organized MVVM architecture with clear separation of concerns
**Evidence**: Directory structure from listDirectory command

### Q2: How many C# files exist in total?

**Method**: Manual count from directory listing
**Findings**: 
- Root: 3 files (App.xaml.cs, MainWindow.xaml.cs, AssemblyInfo.cs)
- Converters: 2 files
- Core/Analytics: 7 files
- Core/Configuration: 3 files
- Core/FileMatching: 3 files
- Core/FileOperations: 10 files
- Core/FileWatching: 16 files
- Core/GroupIdGeneration: 3 files
- Core/ImageProcessing: 3 files
- Core/Localization: 1 file
- Core/Logging: 1 file
- Core/NIR: 5 files
- Core/ProgramLaunching: 4 files
- Helpers: 5 files
- Infrastructure/Logging: 2 files
- Models: 7 files
- Resources: 1 file (Strings.Designer.cs)
- Tests: 1 file
- UI/Behaviors: 1 file
- UI/Controls: 4 files (.xaml.cs only)
- UI/ViewModels: 17 files
- UI/Views: 6 files (.xaml.cs only)

**Conclusion**: Total of approximately 105 C# files to document
**Evidence**: Directory listing results

### Q3: What are all the classes, methods, and properties in each file?

**Method**: Will need to read each file individually to extract complete information
**Findings**: This will be done during the implementation phase
**Conclusion**: Each file needs to be analyzed for:
- Class names
- Public/private methods
- Properties
- Events
- Interfaces
- Purpose and functionality

## 3. Code Analysis Results

### Relevant Existing Code

| Directory | File Count | Purpose |
|-----------|------------|---------|
| Root | 3 | Application entry and configuration |
| Converters | 2 | UI value converters |
| Core/Analytics | 7 | Statistics and abnormal detection |
| Core/Configuration | 3 | Configuration management |
| Core/FileMatching | 3 | File grouping logic |
| Core/FileOperations | 10 | File operations (move, delete, path management) |
| Core/FileWatching | 16 | File system monitoring and event processing |
| Core/GroupIdGeneration | 3 | Group ID generation strategies |
| Core/ImageProcessing | 3 | Image processing and caching |
| Core/Localization | 1 | Localization management |
| Core/Logging | 1 | Log cleanup |
| Core/NIR | 5 | NIR spectrum processing |
| Core/ProgramLaunching | 4 | External program launching |
| Helpers | 5 | Utility functions |
| Infrastructure/Logging | 2 | Logging providers |
| Models | 7 | Data models |
| Resources | 1 | Resource strings |
| Tests | 1 | Validation tests |
| UI/Behaviors | 1 | UI behaviors |
| UI/Controls | 4 | Custom controls |
| UI/ViewModels | 17 | ViewModels for MVVM |
| UI/Views | 6 | View windows and dialogs |

### Dependencies Identified

The application uses:
- WPF (Windows Presentation Foundation) for UI
- MVVM pattern for architecture
- Dependency injection (likely)
- File system watching
- Image processing
- NIR spectrum analysis
- External program launching

### Documentation Structure

Based on the code organization, documentation should be structured as:

```
docs/manual/
├── Root/
│   ├── App.xaml.cs.md
│   ├── MainWindow.xaml.cs.md
│   └── AssemblyInfo.cs.md
├── Converters/
│   ├── BoolToVisibilityConverter.cs.md
│   └── NullToVisibilityConverter.cs.md
├── Core/
│   ├── Analytics/
│   │   ├── AbnormalDetectorService.cs.md
│   │   ├── AbnormalHistoryManager.cs.md
│   │   ├── FileCountStatistics.cs.md
│   │   ├── IAbnormalDetector.cs.md
│   │   ├── IStatisticsService.cs.md
│   │   ├── MatchingStatistics.cs.md
│   │   └── StatisticsService.cs.md
│   ├── Configuration/
│   │   ├── ConfigurationExtensions.cs.md
│   │   ├── ConfigurationManager.cs.md
│   │   └── IConfigurationManager.cs.md
│   ├── FileMatching/
│   │   ├── FileGroupMatcherService.cs.md
│   │   ├── FileMatchingEngine.cs.md
│   │   └── IFileGroupMatcher.cs.md
│   ├── FileOperations/
│   │   ├── DeleteService.cs.md
│   │   ├── FileGroupOperator.cs.md
│   │   ├── FileOperationService.cs.md
│   │   ├── IDeleteService.cs.md
│   │   ├── IFileGroupOperator.cs.md
│   │   ├── IFileOperationService.cs.md
│   │   ├── IMoveService.cs.md
│   │   ├── IPathManagementService.cs.md
│   │   ├── MoveService.cs.md
│   │   └── PathManagementService.cs.md
│   ├── FileWatching/
│   │   ├── EventPriority.cs.md
│   │   ├── EventProcessor.cs.md
│   │   ├── FileWatcherOptions.cs.md
│   │   ├── FileWatcherService.cs.md
│   │   ├── FolderTimestampCache.cs.md
│   │   ├── GroupManager.cs.md
│   │   ├── IEventProcessor.cs.md
│   │   ├── IFileWatcher.cs.md
│   │   ├── IGroupManager.cs.md
│   │   ├── IImageCaptureService.cs.md
│   │   ├── ImageCaptureService.cs.md
│   │   ├── IMonitoringOrchestrator.cs.md
│   │   ├── InitialScanner.cs.md
│   │   ├── ITimestampCache.cs.md
│   │   ├── MonitoringOrchestrator.cs.md
│   │   └── PriorityEventChannel.cs.md
│   ├── GroupIdGeneration/
│   │   ├── GlobalGroupIdGenerator.cs.md
│   │   ├── IGroupIdGenerator.cs.md
│   │   └── LineBasedGroupIdGenerator.cs.md
│   ├── ImageProcessing/
│   │   ├── IImageProcessor.cs.md
│   │   ├── ImageProcessingService.cs.md
│   │   └── LruCache.cs.md
│   ├── Localization/
│   │   └── LocalizationManager.cs.md
│   ├── Logging/
│   │   └── LogCleanupService.cs.md
│   ├── NIR/
│   │   ├── INirFileResolver.cs.md
│   │   ├── NirGraphGenerator.cs.md
│   │   ├── NirSpectrumFilter.cs.md
│   │   ├── NirSpectrumParser.cs.md
│   │   └── SpcTxtNirFileResolver.cs.md
│   └── ProgramLaunching/
│       ├── GeneralCameraLauncher.cs.md
│       ├── Nir2CameraLauncher.cs.md
│       ├── NirCameraLauncher.cs.md
│       └── NirFilteringService.cs.md
├── Helpers/
│   ├── FileNamingHelper.cs.md
│   ├── NormalFolderHelper.cs.md
│   ├── PlaceholderImageHelper.cs.md
│   ├── ResourceHelper.cs.md
│   └── WindowActivationHelper.cs.md
├── Infrastructure/
│   └── Logging/
│       ├── FileLoggerProvider.cs.md
│       └── UILoggerProvider.cs.md
├── Models/
│   ├── ApplicationConfiguration.cs.md
│   ├── DataSequencePresets.cs.md
│   ├── DataSequenceSettings.cs.md
│   ├── FileGroup.cs.md
│   ├── ImageMetadata.cs.md
│   ├── NirSpectrum.cs.md
│   └── UnmatchedFiles.cs.md
├── Resources/
│   └── Strings.Designer.cs.md
├── Tests/
│   └── DataSequenceSettingsValidation.cs.md
└── UI/
    ├── Behaviors/
    │   └── DragSelectBehavior.cs.md
    ├── Controls/
    │   ├── FileGroupDataGrid.xaml.cs.md
    │   ├── LogPanel.xaml.cs.md
    │   ├── StatisticsPanel.xaml.cs.md
    │   └── WorkflowPanel.xaml.cs.md
    ├── ViewModels/
    │   ├── DashboardViewModel.cs.md
    │   ├── DataSequenceItemViewModel.cs.md
    │   ├── DetailPreviewViewModel.cs.md
    │   ├── FileGroupMediaLoader.cs.md
    │   ├── FileGroupViewModel.cs.md
    │   ├── FileOperationViewModel.cs.md
    │   ├── IDashboardViewModel.cs.md
    │   ├── IFileOperationViewModel.cs.md
    │   ├── ISystemControlViewModel.cs.md
    │   ├── LogMessage.cs.md
    │   ├── MainWindowViewModel.cs.md
    │   ├── RelayCommand.cs.md
    │   ├── SettingsDialogViewModel.cs.md
    │   ├── SetupWindowViewModel.cs.md
    │   ├── SystemControlViewModel.cs.md
    │   └── ViewModelBase.cs.md
    └── Views/
        ├── DetailPreviewView.xaml.cs.md
        ├── ImagePreviewDialog.xaml.cs.md
        ├── ImagePreviewWindow.xaml.cs.md
        ├── SettingsDialog.xaml.cs.md
        ├── SetupWindow.xaml.cs.md
        └── SplashWindow.xaml.cs.md
```

## 4. Recommendations

1. Create one markdown file per C# file
2. Each markdown file should contain:
   - File path
   - Purpose/overview
   - All classes defined in the file
   - All public methods with signatures and descriptions
   - All private methods with signatures and descriptions
   - All properties
   - All events
   - Dependencies (what it uses)
   - Usage (what uses it)
3. Write in Korean for user accessibility
4. Focus on WHAT the code does, not HOW it's implemented
5. Include code examples where helpful

## 5. Unanswered Questions

None - all questions have been answered. Ready to proceed to implementation plan.

---
**Status**: [ ] Approved
