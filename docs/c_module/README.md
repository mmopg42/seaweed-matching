# ChronoView C# Module Documentation

> **Status**: Under Construction  
> **Last Updated**: 2025-01-05

## Overview

This documentation provides comprehensive coverage of all C# modules in the ChronoView application. ChronoView is a WPF-based file monitoring and management system designed for handling multi-camera image sequences with NIR spectroscopy data integration.

## Quick Navigation

- [Core Services](#core-services) - Business logic and service layer
- [UI Components](#ui-components) - User interface layer (MVVM)
- [Data Models](#data-models) - Domain models and DTOs
- [Utilities](#utilities) - Helper classes and infrastructure

## Architecture Overview

```
ChronoView Architecture (Layered)

┌─────────────────────────────────────────┐
│           UI Layer (WPF/XAML)           │
│  ViewModels, Views, Controls, Behaviors │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│          Core Services Layer            │
│  FileWatching, FileMatching, Analytics  │
│  FileOperations, ImageProcessing, NIR   │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│            Models Layer                 │
│  FileGroup, Configuration, Metadata     │
└─────────────────────────────────────────┘
```

## Subsystems

| Subsystem | Files | Description |
|-----------|-------|-------------|
| [Core/](Core/) | 48 | Business logic services and core functionality |
| [UI/](UI/) | 28 | User interface components (MVVM pattern) |
| [Models/](Models/) | 7 | Data models and configuration structures |
| [Helpers/](Helpers/) | 3 | Utility helper classes |
| [Converters/](Converters/) | 2 | XAML value converters |
| [Infrastructure/](Infrastructure/) | 1 | Cross-cutting concerns (logging) |
| [Tests/](Tests/) | 1 | Validation and test utilities |
| [Root/](Root/) | 5 | Application entry point and main window |

**Total**: 95 documented C# files

## Core Services

The Core layer contains the business logic organized into specialized modules:

- **[Analytics/](Core/Analytics/)** - Statistical analysis and anomaly detection
- **[Configuration/](Core/Configuration/)** - Application configuration management
- **[FileMatching/](Core/FileMatching/)** - File grouping and matching algorithms
- **[FileOperations/](Core/FileOperations/)** - File system operations (move, delete, copy)
- **[FileWatching/](Core/FileWatching/)** - Real-time file monitoring and event processing
- **[ImageProcessing/](Core/ImageProcessing/)** - Image loading, caching, and processing
- **[Localization/](Core/Localization/)** - Multi-language support
- **[NIR/](Core/NIR/)** - NIR spectroscopy data processing
- **[ProgramLaunching/](Core/ProgramLaunching/)** - External camera program integration

## UI Components

The UI layer follows the MVVM (Model-View-ViewModel) pattern:

- **[ViewModels/](UI/ViewModels/)** - View models implementing presentation logic
- **[Views/](UI/Views/)** - XAML views and code-behind
- **[Controls/](UI/Controls/)** - Custom WPF controls
- **[Behaviors/](UI/Behaviors/)** - Attached behaviors for UI interactions

## Data Models

Core data structures used throughout the application:

- **FileGroup** - Represents a matched set of files from different cameras
- **UnmatchedFiles** - Collection of files awaiting matching
- **DataSequenceSettings** - Configuration for file naming patterns
- **ApplicationConfiguration** - Application-wide settings
- **ImageMetadata** - Image file metadata
- **NirSpectrum** - NIR spectroscopy data

## Getting Started

### For New Developers

1. Start with [Core/Configuration/](Core/Configuration/) to understand configuration management
2. Review [Models/](Models/) to understand core data structures
3. Explore [Core/FileWatching/](Core/FileWatching/) for the main workflow
4. Study [UI/ViewModels/](UI/ViewModels/) for UI architecture

### For Documentation Maintenance

- Each `.cs` file has a corresponding `.md` file in the same relative path
- Follow the [Architecture Document Template](../architecture/README.md) for consistency
- Update cross-references when modifying dependencies
- Run VERIFY commands to ensure documentation accuracy

## Key Patterns

- **Dependency Injection**: Services use constructor injection
- **Interface-based Design**: Core services implement `I*` interfaces
- **MVVM**: UI follows strict Model-View-ViewModel separation
- **Async/Await**: Asynchronous operations throughout
- **Event-driven**: File monitoring uses event-based architecture

## Related Documentation

- [Architecture Documentation](../architecture/) - High-level architecture and design decisions
- [Glossary](../architecture/glossary.md) - Official naming conventions and terminology
- [Python Module Docs](../modules/) - Original Python implementation reference

## Documentation Status

✅ Phase 1: Foundation - Complete  
🚧 Phase 2: Core Services - In Progress  
⏳ Phase 3: Data Models - Pending  
⏳ Phase 4: UI Layer - Pending  
⏳ Phase 5: Utilities - Pending

---

**Last Generated**: 2025-01-05  
**ChronoView Version**: 1.0  
**Documentation Version**: 1.0
