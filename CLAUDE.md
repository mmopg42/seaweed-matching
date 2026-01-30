# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ChronoView is a WPF-based file monitoring application (.NET 10.0, C# 13) for multi-camera image capture workflows with NIR spectroscopy integration. It monitors folders, matches files by timestamp across cameras, and facilitates batch operations.

## Build & Test Commands

```bash
# Build
dotnet build ChronoView/ChronoView.csproj

# Run tests
dotnet test ChronoView.Tests/ChronoView.Tests.csproj

# Build release
dotnet build -c Release ChronoView/ChronoView.csproj

# Run application
dotnet run --project ChronoView/ChronoView.csproj
```

## Architecture

**3-Layer MVVM Architecture:**

```
UI Layer (WPF/XAML)
├── ViewModels (MainWindowViewModel, DashboardViewModel, FileGroupViewModel, SettingsDialogViewModel, etc.)
├── Views (MainWindow, SplashWindow, ImagePreviewWindow, SettingsDialog, SetupWindow)
├── Controls (FileGroupDataGrid, LogPanel, StatisticsPanel, WorkflowPanel)
├── Behaviors (DragSelectBehavior)
└── Converters (CameraStateConverters)
         ↓
Core Services Layer
├── FileWatching (MonitoringOrchestrator, FileWatcherService, EventProcessor, GroupManager, InitialScanner, ImageCaptureService)
├── FileMatching (FileMatchingEngine, FileGroupMatcherService)
├── FileOperations (FileOperationService, MoveService, DeleteService, FileGroupOperator, PathManagementService)
├── Analytics (StatisticsService, AbnormalDetectorService, AbnormalHistoryManager)
├── ImageProcessing (ImageProcessingService, LruCache)
├── Configuration (ConfigurationManager, DefaultConfiguration, PathHelper, ConfigurationExtensions)
├── NIR (NirSpectrumParser, SpcTxtNirFileResolver, NirSpectrumFilter, NirGraphGenerator)
├── ProgramLaunching (GeneralCameraLauncher, NirCameraLauncher, Nir2CameraLauncher, NirFilteringService)
├── GroupIdGeneration (GlobalGroupIdGenerator, LineBasedGroupIdGenerator)
├── Localization (LocalizationManager)
└── Logging (LogCleanupService)
         ↓
Helpers & Infrastructure
├── Helpers (FileNamingHelper, NormalFolderHelper, ProcessExtensions, WindowActivationHelper)
└── Infrastructure/Logging (FileLoggerProvider, UILoggerProvider)
         ↓
Models Layer
├── FileGroup, UnmatchedFiles, ImageMetadata
├── ApplicationConfiguration, DataSequenceSettings, DataSequencePresets
├── CameraState, NirSpectrum
```

**Key Entry Points:**
- `App.xaml.cs` - DI setup (`ConfigureServices()` at line ~169)
- `MainWindow.xaml` / `MainWindowViewModel` - Main UI
- `MonitoringOrchestrator` - Coordinates file monitoring workflow

**Design Patterns:**
- Dependency Injection via `Microsoft.Extensions.DependencyInjection`
- MVVM with `ViewModelBase` and `RelayCommand`
- Interface-based design (all major services have `I*` interfaces)
- Event-driven architecture for file monitoring

## Critical Documentation

**Before modifying code:**
1. Read `docs/architecture/glossary.md` - Official naming conventions (REQUIRED)
2. Check `docs/c_module/` - File-by-file documentation (95 files documented)
3. Review module documentation to check for duplicate implementations
4. Use grep to verify dependents before changing interfaces

```bash
# Verify term exists in glossary
grep -n "ClassName" docs/architecture/glossary.md

# Find all usages across codebase
grep -rn "ClassName" ChronoView/
```

## High-Risk Areas

These components have many dependents - extra caution needed:
- `IConfigurationManager` - Used by all services
- `FileGroup` model - Used in 15+ classes
- `MonitoringOrchestrator` - Coordinates 8+ services
- `ViewModelBase` - Base for all ViewModels

## File Monitoring Workflow

```
File Created → FileWatcherService → EventProcessor → MonitoringOrchestrator
    → FileMatchingEngine → GroupManager → DashboardViewModel → UI Display
```

**Note:** Watch folders are located in WSL, so file monitoring uses polling instead of native file system events.

## Coding Guidelines

- **File Line Limit:** Keep each file under 600 lines. If a file exceeds this limit, plan to split it into smaller, focused modules.

## Code Quality Principles

**Avoid Code Smells:**
- **No Spaghetti Code:** Keep control flow simple and readable. Avoid deep nesting (>3 levels), excessive goto/early returns, and interwoven logic. Extract complex conditions into well-named methods.
- **No Duplicate Code:** Before writing new code, search `docs/c_module/` and use `grep` to find existing implementations. Reuse or extend existing code rather than copying. If you find duplicate code during implementation, refactor to eliminate it.
- **Single Source of Truth (SSOT):** Each concept/term/abstraction should have exactly one definition. Check `docs/architecture/glossary.md` before introducing new terminology. Never create the same concept with different names.

**Refactoring Triggers:**
- File exceeds 600 lines → Split into focused modules
- Same logic appears in 3+ places → Extract to shared service/helper
- Method exceeds 50 lines → Break down into smaller methods
- Class has >10 dependencies → Consider splitting (Single Responsibility Principle)

**Before Adding New Code:**
1. Search `docs/c_module/` for similar functionality
2. Grep for relevant class/method names
3. Check if existing code can be extended instead
4. Verify term exists in glossary.md or add it

## Important Implementation Notes

- **General Camera Move/Delete:** General camera files are moved/deleted as folder units (not individual files)
- **WSL Polling:** Since watch folders reside in WSL, the monitoring logic uses polling-based detection
- **Camera Line Mapping:** Cam4, Cam5, Cam6 are Line 2 versions of Cam1, Cam2, Cam3 respectively. They share the same settings (timing windows, etc.) - only the line number and save path differ.

## Configuration

- Config path: `%APPDATA%\ChronoView\`
- Log path: `%APPDATA%\ChronoView\Logs\{YYYYMMDD}\`
- Key class: `ApplicationConfiguration` (FolderPaths, ImageSettings, MatchingSettings, DataSequenceSettings)

## Key Dependencies

- `SixLabors.ImageSharp` - Image processing
- `ScottPlot` - Data visualization
- `Microsoft.Extensions.Logging` - Structured logging
- Testing: xUnit, FsCheck (property-based), Moq

## Git Info

- Main branch for PRs: `main`
- Current feature branch: `feature/gui_kiro`
