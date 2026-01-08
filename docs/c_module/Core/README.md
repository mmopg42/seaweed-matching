# Core Services

> **Purpose**: Business logic and service layer  
> **Files**: 48 C# files across 9 modules  
> **Pattern**: Interface-based dependency injection

## Overview

The Core layer contains all business logic services for ChronoView. Services are designed to be independent, testable, and follow the dependency inversion principle through interface-based contracts.

## Modules

| Module | Files | Purpose |
|--------|-------|---------|
| [Analytics/](Analytics/) | 6 | Statistical analysis and anomaly detection |
| [Configuration/](Configuration/) | 2 | Application configuration management |
| [FileMatching/](FileMatching/) | 3 | File grouping and matching algorithms |
| [FileOperations/](FileOperations/) | 10 | File system operations (move, delete, copy) |
| [FileWatching/](FileWatching/) | 16 | Real-time file monitoring and orchestration |
| [ImageProcessing/](ImageProcessing/) | 3 | Image loading, caching, and processing |
| [Localization/](Localization/) | 1 | Multi-language support |
| [NIR/](NIR/) | 5 | NIR spectroscopy data processing |
| [ProgramLaunching/](ProgramLaunching/) | 3 | External camera program integration |

## Key Interfaces

All major services implement interfaces for testability and flexibility:

- `IConfigurationManager` - Configuration service contract
- `IFileGroupMatcher` - File matching service contract
- `IMonitoringOrchestrator` - Workflow orchestration contract
- `IFileWatcher` - File system monitoring contract
- `IFileOperationService` - File operations contract
- `IImageProcessor` - Image processing contract
- `IAbnormalDetector` - Anomaly detection contract

## Architecture

```
Core Services Layer Architecture

┌──────────────────────────────────────────────┐
│         MonitoringOrchestrator               │
│         (Workflow Coordination)              │
└────┬─────────────────────────────────────┬───┘
     │                                     │
┌────▼──────────────┐          ┌──────────▼─────┐
│  FileWatcherService│          │ FileMatchingEngine│
│  (Event Detection) │          │ (Grouping Logic)  │
└────┬──────────────┘          └──────────┬─────┘
     │                                     │
┌────▼─────────────────────────────────────▼───┐
│           FileOperationService                │
│           (Move/Delete/Copy)                  │
└───────────────────────────────────────────────┘
```

## Dependencies

### Internal Dependencies
- Core services depend on Models layer
- All services use ConfigurationManager
- FileWatching orchestrates FileMatching and FileOperations

### External Dependencies
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- System.IO.FileSystem
- System.Text.Json

## Common Patterns

### Service Registration
```csharp
services.AddSingleton<IConfigurationManager, ConfigurationManager>();
services.AddSingleton<IFileWatcher, FileWatcherService>();
services.AddScoped<IFileOperationService, FileOperationService>();
```

### Service Usage
```csharp
public class MyService
{
    private readonly IConfigurationManager _config;
    private readonly ILogger<MyService> _logger;
    
    public MyService(IConfigurationManager config, ILogger<MyService> logger)
    {
        _config = config;
        _logger = logger;
    }
}
```

## Module Documentation

- [Analytics/](Analytics/) - Statistical analysis and anomaly detection
- [Configuration/](Configuration/) - Configuration management
- [FileMatching/](FileMatching/) - File matching algorithms
- [FileOperations/](FileOperations/) - File system operations
- [FileWatching/](FileWatching/) - File monitoring and orchestration
- [ImageProcessing/](ImageProcessing/) - Image processing services
- [Localization/](Localization/) - Localization support
- [NIR/](NIR/) - NIR data processing
- [ProgramLaunching/](ProgramLaunching/) - External program integration

## Related Documentation

- [Models Documentation](../Models/) - Data structures used by services
- [UI ViewModels](../UI/ViewModels/) - Consumers of Core services
- [Architecture Overview](../../architecture/module_core_services.md) - High-level design

---

**Last Updated**: 2025-01-05
