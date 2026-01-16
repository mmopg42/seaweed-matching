# Technology Stack

**Analysis Date:** 2026-01-16

## Languages

**Primary:**
- C# 13 - All application code (.NET 10.0)

**Secondary:**
- XAML - UI definitions and views
- Python - Development scripts and tools (oneoff/, script/)

## Runtime

**Environment:**
- .NET 10.0 Windows (net10.0-windows)
- Windows-only (WPF dependency)

**Package Manager:**
- NuGet via dotnet CLI
- No lockfile committed (uses standard NuGet package restore)

## Frameworks

**Core:**
- WPF (Windows Presentation Foundation) - Desktop UI framework
- Windows Forms - Mixed mode support (UseWindowsForms enabled)

**Testing:**
- xUnit 2.9.3 - Unit and integration test framework
- FsCheck 3.3.2 - Property-based testing for F#-style verification
- Moq 4.20.72 - Mocking framework
- FlaUI 5.0.0 - UI automation testing

**Build/Dev:**
- .NET SDK 10.0 - Compilation and build
- Microsoft.Extensions DI 10.0.0 - Dependency injection container
- Microsoft.Extensions.Logging 10.0.0 - Structured logging

## Key Dependencies

**Critical:**
- Microsoft.Extensions.DependencyInjection 10.0.0 - DI container for service composition
- Microsoft.Extensions.Logging 10.0.0 - Logging abstraction throughout app
- SixLabors.ImageSharp 3.1.12 - Image loading and processing
- ScottPlot 5.0.42 - NIR spectrum graph visualization

**Infrastructure:**
- Microsoft.Xaml.Behaviors.Wpf 1.1.135 - WPF attached behaviors
- SixLabors.ImageSharp.Drawing 2.1.7 - Image drawing/annotation

## Configuration

**Environment:**
- Configuration via ApplicationConfiguration class (JSON-serializable)
- Settings stored in %APPDATA%\ChronoView\
- Key settings: FolderPaths, ImageSettings, MatchingSettings, DataSequenceSettings

**Build:**
- ChronoView.csproj - Main project SDK-style format
- Implicit usings enabled
- Nullable reference types enabled

## Platform Requirements

**Development:**
- Windows 10/11 with .NET 10.0 SDK
- Visual Studio 2022 or JetBrains Rider recommended

**Production:**
- Windows 10/11 desktop application
- Single-file executable deployment supported
- AppConfig: %APPDATA%\ChronoView\
- Log path: %APPDATA%\ChronoView\Logs\{YYYYMMDD}\

---

*Stack analysis: 2026-01-16*
*Update after major dependency changes*
