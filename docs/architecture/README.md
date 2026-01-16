# Architecture Documentation Index

> **Purpose**: Entry point for any developer modifying the ChronoView codebase.  
> **Read relevant docs BEFORE making changes.**  
> **Run verification commands BEFORE trusting documented dependents.**  
> **Check glossary.md for official naming BEFORE writing any document.**

> Last Updated: 2025-01-05

## Quick Start
> New to this codebase? Start here.

1. **[Glossary](glossary.md)** - **READ FIRST** - Official naming definitions
2. **[C# Module Documentation](../c_module/)** - Detailed documentation for all C# files
3. **[System Overview](#system-overview)** - How everything fits together

## System Overview

ChronoView is a WPF-based file monitoring and management application designed for multi-camera image capture workflows with NIR spectroscopy integration.

### Key Features
- Real-time file system monitoring
- Automatic file matching and grouping by timestamp
- Multi-camera support (General, NIR, NIR2)
- Anomaly detection (aspect ratio deviation vs per-context median baseline)
- Batch file operations with conflict resolution
- NIR spectrum visualization

### Architecture Layers

```
┌─────────────────────────────────────────┐
│        UI Layer (WPF/XAML)              │
│   ViewModels, Views, Controls           │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│        Core Services Layer              │
│   Business Logic & Domain Services      │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│         Models Layer                    │
│   Domain Models & DTOs                  │
└─────────────────────────────────────────┘
```

## ChronoView C# Documentation

### Module Documentation
The [C# Module Documentation](../c_module/) provides comprehensive coverage of all 95 C# files in ChronoView:

- **[Core Services](../c_module/Core/)** - 48 files across 9 modules
  - Analytics, Configuration, FileMatching, FileOperations
  - FileWatching, ImageProcessing, Localization, NIR, ProgramLaunching
  
- **[UI Components](../c_module/UI/)** - 28 files across 4 modules
  - ViewModels, Views, Controls, Behaviors
  
- **[Data Models](../c_module/Models/)** - 7 model classes
  - FileGroup, UnmatchedFiles, Configuration, Metadata
  
- **[Utilities](../c_module/Helpers/)** - Helper classes and converters
  - Helpers, Converters, Infrastructure, Tests

### Quick Links
- [Master Index](../c_module/README.md) - Start here for C# documentation
- [Core Services Overview](../c_module/Core/README.md) - Business logic layer
- [UI Components Overview](../c_module/UI/README.md) - MVVM UI layer
- [Models Overview](../c_module/Models/README.md) - Data structures

## Subsystems

### Core Services Layer

The Core layer contains all business logic organized into specialized services:

| Module | Purpose | Key Classes |
|--------|---------|-------------|
| **FileWatching** | Real-time monitoring | MonitoringOrchestrator, FileWatcherService, EventProcessor |
| **FileMatching** | File grouping | FileMatchingEngine, FileGroupMatcherService |
| **FileOperations** | File management | FileOperationService, DeleteService, MoveService |
| **Analytics** | Statistics & anomalies | AbnormalDetectorService, StatisticsService |
| **ImageProcessing** | Image handling | ImageProcessingService, LruCache |
| **Configuration** | Settings management | ConfigurationManager |
| **NIR** | Spectroscopy data | NirSpectrumParser, NirGraphGenerator |
| **Localization** | Multi-language | LocalizationManager |
| **ProgramLaunching** | External programs | CameraLaunchers |

### UI Layer

The UI follows MVVM pattern with strict separation:

| Component | Purpose | Key Classes |
|-----------|---------|-------------|
| **ViewModels** | Presentation logic | MainWindowViewModel, DashboardViewModel, FileGroupViewModel |
| **Views** | XAML UI | MainWindow, SettingsDialog, DetailPreviewView |
| **Controls** | Custom controls | FileGroupDataGrid, LogPanel, StatisticsPanel |
| **Behaviors** | UI behaviors | DragSelectBehavior |

### Data Models

Core data structures:

| Model | Purpose |
|-------|---------|
| **FileGroup** | Matched file set from multiple cameras |
| **UnmatchedFiles** | Files awaiting matching |
| **ApplicationConfiguration** | App settings |
| **DataSequenceSettings** | File naming patterns |
| **ImageMetadata** | Image file metadata |
| **NirSpectrum** | NIR spectroscopy data |

## Key Workflows

### File Monitoring Workflow

```
File Created
    ↓
FileWatcherService (detects change)
    ↓
EventProcessor (prioritizes event)
    ↓
MonitoringOrchestrator (coordinates)
    ↓
FileMatchingEngine (matches files)
    ↓
GroupManager (creates FileGroup)
    ↓
DashboardViewModel (displays in UI)
```

### File Operation Workflow

```
User Action (UI)
    ↓
FileOperationViewModel (command)
    ↓
FileOperationService (validates)
    ↓
MoveService / DeleteService (executes)
    ↓
PathManagementService (organizes)
    ↓
Result displayed in UI
```

## Design Patterns

### Dependency Injection
All services use constructor injection:
```csharp
public class MyService
{
    private readonly IConfigurationManager _config;
    
    public MyService(IConfigurationManager config)
    {
        _config = config;
    }
}
```

### Interface-Based Design
Core services implement interfaces:
```csharp
public interface IFileWatcher { ... }
public class FileWatcherService : IFileWatcher { ... }
```

### MVVM Pattern
UI follows strict MVVM:
```csharp
public class MyViewModel : ViewModelBase
{
    private string _property;
    public string Property
    {
        get => _property;
        set => SetProperty(ref _property, value);
    }
}
```

### Event-Driven Architecture
File monitoring uses events:
```csharp
public event EventHandler<FileEventArgs> FileDetected;
```

## High-Risk Areas
> These areas have many dependents. Extra caution required.

| Area | Documentation | Dependents | Verification |
|------|---------------|------------|--------------|
| ConfigurationManager | [Core/Configuration/](../c_module/Core/Configuration/) | All services | `grep -rn "IConfigurationManager" ChronoView/` |
| FileGroup Model | [Models/FileGroup.md](../c_module/Models/FileGroup.md) | 15+ classes | `grep -rn "FileGroup" ChronoView/` |
| MonitoringOrchestrator | [Core/FileWatching/](../c_module/Core/FileWatching/) | 8+ services | `grep -rn "IMonitoringOrchestrator" ChronoView/` |
| ViewModelBase | [UI/ViewModels/](../c_module/UI/ViewModels/) | All ViewModels | `grep -rn "ViewModelBase" ChronoView/` |

## Development Guidelines

### Before Modifying Code

1. **Read the glossary** - Understand official terminology
2. **Find relevant docs** - Check c_module/ for the file you're modifying
3. **Run VERIFY commands** - Ensure documentation is current
4. **Check dependents** - Understand what depends on your changes
5. **Review interfaces** - If modifying a service, check its interface

### After Modifying Code

1. **Update documentation** - Keep c_module/ docs in sync
2. **Update glossary** - Add new terms if introduced
3. **Update VERIFY commands** - If dependencies changed
4. **Test thoroughly** - Run all affected tests
5. **Update cross-references** - Fix any broken links

## Related Documentation

- **[C# Module Documentation](../c_module/)** - Detailed file-by-file documentation
- **[Glossary](glossary.md)** - Official naming conventions
- **[Python Modules](../modules/)** - Original Python implementation (reference)
- **[GUI C# Migration](../gui_c/)** - Migration checklists and progress

## Documentation Maintenance

### Keeping Docs Updated

- Update c_module/ docs when modifying C# files
- Update glossary when introducing new terms
- Run VERIFY commands to validate dependents
- Check cross-references after refactoring
- Update architecture diagrams if structure changes

### Documentation Standards

- Follow [Architecture Document Template](glossary.md#documentation-template)
- Use glossary terms exactly as defined
- Include VERIFY commands for dependents
- Provide usage examples for complex APIs
- Document all public interfaces

## Tools & Resources

### Verification Commands

```bash
# Find all usages of a class
grep -rn "ClassName" ChronoView/**/*.cs

# Find all interface implementations
grep -rn "IInterfaceName" ChronoView/**/*.cs

# Find all configuration keys
grep -rn "config_key" ChronoView/**/*.cs

# Check documentation consistency
grep -rn "term" docs/c_module/
```

### Documentation Structure

```
docs/
├── architecture/           # This folder
│   ├── README.md          # This file
│   └── glossary.md        # Official terminology
│
├── c_module/              # C# module documentation
│   ├── README.md          # Master index
│   ├── Core/              # Core services docs
│   ├── UI/                # UI component docs
│   ├── Models/            # Model docs
│   └── ...                # Other modules
│
└── gui_c/                 # Migration documentation
    └── ...
```

## Document Registry

| Document | Type | Last Updated | Purpose |
|----------|------|--------------|---------|
| glossary.md | Reference | 2025-01-05 | Official naming conventions |
| ../c_module/README.md | Index | 2025-01-05 | C# module documentation master index |
| ../c_module/Core/README.md | Index | 2025-01-05 | Core services overview |
| ../c_module/UI/README.md | Index | 2025-01-05 | UI components overview |
| ../c_module/Models/README.md | Index | 2025-01-05 | Data models overview |

---

**Last Updated**: 2025-01-05  
**ChronoView Version**: 1.0  
**Documentation Version**: 1.0
