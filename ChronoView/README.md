# ChronoView - C# Migration Project

## Overview

ChronoView is a WPF-based file monitoring and image processing application, migrated from Python to C# for improved performance and native Windows integration.

## Project Structure

```
ChronoView/
├── Core/                          # Core business logic and services
│   ├── Configuration/             # Configuration management
│   ├── FileWatching/              # File system monitoring
│   ├── ImageProcessing/           # Image processing and caching
│   ├── FileMatching/              # File grouping and matching logic
│   ├── NIR/                       # NIR spectrum processing
│   ├── FileOperations/            # File move, copy, delete operations
│   └── Analytics/                 # Statistics and analytics
├── UI/                            # User interface components
│   ├── ViewModels/                # MVVM ViewModels
│   ├── Views/                     # XAML views and windows
│   └── Controls/                  # Custom WPF controls
├── Infrastructure/                # Infrastructure services
├── Models/                        # Data models and entities
├── App.xaml                       # Application entry point
└── MainWindow.xaml                # Main application window

ChronoView.Tests/
└── ProjectStructureTests.cs       # Property-based tests for architecture validation
```

## Technology Stack

- **Framework**: .NET 10.0 with WPF
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Logging**: Microsoft.Extensions.Logging (Console + Debug)
- **Image Processing**: SixLabors.ImageSharp
- **Testing**: xUnit + FsCheck.NET (Property-Based Testing)

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
- Windows OS (for WPF support)

### Building the Project

```bash
dotnet build ChronoView/ChronoView.csproj
```

### Running Tests

```bash
dotnet test ChronoView.Tests/ChronoView.Tests.csproj
```

## Architecture

The application follows a layered architecture with clear separation of concerns:

- **Core Layer**: Business logic, file operations, and data processing
- **UI Layer**: WPF views, ViewModels (MVVM pattern), and custom controls
- **Infrastructure Layer**: Cross-cutting concerns like logging and configuration
- **Models Layer**: Data transfer objects and domain entities

## Dependency Injection

The application uses Microsoft.Extensions.DependencyInjection for IoC. Services are configured in `App.xaml.cs`:

```csharp
private void ConfigureServices(IServiceCollection services)
{
    // Configure logging
    services.AddLogging(configure =>
    {
        configure.AddConsole();
        configure.AddDebug();
        configure.SetMinimumLevel(LogLevel.Information);
    });

    // Register main window
    services.AddSingleton<MainWindow>();

    // TODO: Register services as they are implemented
}
```

## Testing Strategy

The project uses a dual testing approach:

1. **Unit Tests**: Verify specific examples, edge cases, and error conditions
2. **Property-Based Tests**: Verify universal properties across all inputs using FsCheck.NET

Property-based tests are configured to run a minimum of 100 iterations per property.

## Migration Status

This project is part of a comprehensive migration from Python to C#. See the following documents for detailed progress:

- `docs/gui_c/migration_analysis_checklist.md` - Complete feasibility analysis
- `docs/gui_c/implementation_progress_checklist.md` - Real-time implementation tracking
- `docs/gui_c/module_function_checklist.md` - Function-level implementation status

## Next Steps

1. Implement Configuration Management System (Task 2)
2. Create Data Models and Core Entities (Task 3)
3. Implement File System Monitoring Service (Task 4)
4. Continue with remaining tasks as outlined in `.kiro/specs/python-gui-to-csharp-migration/tasks.md`

## License

[Add license information here]
