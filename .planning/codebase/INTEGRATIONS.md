# External Integrations

**Analysis Date:** 2026-01-16

## APIs & External Services

**None**
- This is a desktop application with no cloud API integrations
- All processing is local on the user's machine

## Data Storage

**Local File System:**
- Configuration files: %APPDATA%\ChronoView\
- Log files: %APPDATA%\ChronoView\Logs\{YYYYMMDD}\
- No database - all state is file-based

**Watch Directories (External Input):**
- General camera folder - Configurable via settings
- NIR camera folder - Configurable via settings
- NIR2 camera folder - Configurable via settings
- Output directories - For file operations (move, copy, delete)

## External Programs

**Camera Capture Programs (Launched via Process.Start):**
- General camera capture program - Configurable executable path
- NIR camera capture program - Configurable executable path
- NIR2 camera capture program - Configurable executable path
- Integration: `Core/ProgramLaunching/GeneralCameraLauncher.cs`, `NirCameraLauncher.cs`, `Nir2CameraLauncher.cs`
- Activation: Process.Start() with configured executable path and arguments
- Status tracking: Polling-based window detection via Windows API

## Authentication & Identity

**None**
- Single-user desktop application
- No authentication or user management

## Monitoring & Observability

**Logging:**
- Microsoft.Extensions.Logging with custom providers
- File logger: `Infrastructure/Logging/FileLoggerProvider.cs`
- UI logger: `Infrastructure/Logging/UILoggerProvider.cs`
- Log levels: Debug, Information, Warning, Error, Critical

**Error Tracking:**
- None (local application only)
- Exception handling in App.xaml.cs with global handlers

## CI/CD & Deployment

**Development:**
- Build: `dotnet build ChronoView/ChronoView.csproj`
- Test: `dotnet test ChronoView.Tests/ChronoView.Tests.csproj`
- Run: `dotnet run --project ChronoView/ChronoView.csproj`

**Distribution:**
- Single-file executable deployment
- Version: 1.0.0 (AssemblyVersion)

## Environment Configuration

**Development:**
- No environment variables required
- Configuration stored in user AppData folder
- Log cleanup service manages old log files

**Production:**
- User settings persisted via ConfigurationManager
- Settings location: %APPDATA%\ChronoView\prische.json
- Default settings: `Core/Configuration/DefaultConfiguration.cs`

## Webhooks & Callbacks

**None**
- Desktop application with no network callbacks

---

*Integration audit: 2026-01-16*
*Update when adding/removing external services*
