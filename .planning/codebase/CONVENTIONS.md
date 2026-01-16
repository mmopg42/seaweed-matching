# Coding Conventions

**Analysis Date:** 2026-01-16

## Naming Patterns

**Files:**
- PascalCase.cs for all C# files
- PascalCase.xaml for WPF views
- *Tests.cs for test files (e.g., `FileMatchingEngineTests.cs`)
- I*.cs for interfaces (e.g., `IFileWatcher.cs`)

**Functions:**
- PascalCase for all public methods (C# standard): `ProcessFilesAsync()`, `GetConfiguration()`
- Async methods have Async suffix: `StartMonitoringAsync()`, `ProcessEventsAsync()`
- Event handlers: On* prefix for overrides, *EventHandler suffix for delegates
- Command handlers: Execute* prefix in RelayCommand implementations

**Variables:**
- camelCase for local variables and parameters: `fileGroup`, `maxSize`
- _camelCase for private fields (optional, some code uses this): `_logger`
- PascalCase for public properties: `FileGroups`, `IsEnabled`
- THIS for private members is also common (no underscore prefix)

**Types:**
- PascalCase for classes, no prefix: `FileWatcherService`, `ViewModelBase`
- PascalCase for interfaces with I prefix: `IFileWatcher`, `IConfigurationManager`
- PascalCase for enums: `EventPriority`, `CameraState`
- PascalCase for delegates: `EventHandler`, `Func<>`

## Code Style

**Formatting:**
- No explicit .editorconfig file (uses Visual Studio defaults)
- 4 space indentation (C# standard)
- Opening braces on new line (Allman style - WPF convention)
- 120 character line length (Visual Studio default)

**Linting:**
- .NET 10.0 compiler with nullable reference types enabled
- No additional linting tools configured
- Implicit usings enabled

## Import Organization

**Order:**
1. System namespaces (System.*, Microsoft.*)
2. Third-party namespaces (SixLabors.*, ScottPlot.*)
3. Internal namespaces (ChronoView.*)
4. Aliased using statements (if any)

**Grouping:**
- Blank line between groups
- Sorted alphabetically within groups (Visual Studio default)

**Path Aliases:**
- None (uses full namespace paths)

## Error Handling

**Patterns:**
- Throw exceptions, catch at boundaries (App.xaml.cs, service boundaries)
- Custom exception types: Not commonly used (standard Exception types)
- Async methods: try/catch with proper exception logging
- Global handlers in App.xaml.cs for unhandled exceptions

**Error Types:**
- Throw on invalid input, null values, invariant violations
- Log error with context before throwing: `logger.LogError(ex, "Failed to process file")`
- Include inner exceptions: `new Exception("message", innerException)`

## Logging

**Framework:**
- Microsoft.Extensions.Logging with ILogger<T>
- Levels: Debug, Information, Warning, Error, Critical

**Patterns:**
- Structured logging with message templates: `logger.LogInformation("Processing {FileCount} files", fileCount)`
- Log at service boundaries, not in utility functions
- Log state transitions, external operations, errors
- Custom FileLoggerProvider for file output

## Comments

**When to Comment:**
- XML doc comments for public APIs (`/// <summary>`)
- Explain business rules and non-obvious algorithms
- Document thread safety and async behavior
- Avoid obvious comments

**JSDoc/TSDoc:**
- XML documentation comments required for public APIs
- Use `<summary>`, `<param>`, `<returns>`, `<exception>` tags

**TODO Comments:**
- Format: // TODO: description
- No formal tracking (rely on git blame/code review)

## Function Design

**Size:**
- Keep functions focused and readable
- Extract helpers for complex logic
- One level of abstraction per function

**Parameters:**
- Prefer specific parameters over many (generally 3-5 max)
- Use options objects for many parameters: `ProcessFiles(options: ProcessOptions)`
- Destructure in parameter list: `Process(string path, int count)`

**Return Values:**
- Explicit return statements
- Return early for guard clauses
- Async methods return Task or Task<T>

## Module Design

**Exports:**
- Public classes marked as public
- Internal classes marked as internal (default)
- No explicit export patterns (C# access modifiers)

**Barrel Files:**
- Not applicable (C# uses namespace-based organization)

## Special WPF Conventions

**Dependency Injection:**
- Singleton services for stateful objects
- Transient for ViewModels
- Register interfaces in App.xaml.cs ConfigureServices()

**MVVM:**
- ViewModels inherit from ViewModelBase
- Commands use RelayCommand
- Properties raise PropertyChanged event

**File Organization:**
- Keep each file under 600 lines (project guideline)
- Split large files into focused modules

---

*Convention analysis: 2026-01-16*
*Update when patterns change*
