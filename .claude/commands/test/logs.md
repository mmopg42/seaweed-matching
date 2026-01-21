---
name: test:logs
description: View and analyze logs from ChronoView (LogPanel and Console)
argument-hint: [source] [action] [query]
allowed-tools:
  - Bash
  - AskUserQuestion
---

<objective>
Retrieve and analyze logs from ChronoView application.

This skill provides access to two log sources:
- **LogPanel**: User-facing logs displayed in the UI (errors, warnings, info)
- **Console**: Developer debug logs written to file (detailed traces, exceptions)

Use LogPanel for user issues, Console logs for development debugging.
</objective>

<context>
Working directory: C:\workspace\seaweed\gui_kiro_v2

UI Automation CLI: skills_scripts/ui_automation/bin/Debug/net10.0/ui_automation.exe

LogPanel: In-memory, accessible via UI Automation
Console logs: %LOCALAPPDATA%\prische\ChronoView\Logs\{YYYYMMDD}\*.log (auto-discovered with --latest flag)

Arguments: $ARGUMENTS
</context>

<process>
**1. Parse arguments**

Format: [source] [action] [query]
- source: `panel` (default) or `console` or `both`
- action: `tail`, `search`, `list`
- query: search text or count

**2. LogPanel actions (user-facing logs)**

Get recent logs:
```bash
# Get last 20 log entries
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- logs tail 20 --json

# Get all logs
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- logs get --json
```

Filter by level:
```bash
# Get only ERROR level logs
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- logs filter --level ERROR --json

# Get only WARNING level logs
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- logs filter --level Warning --json
```

Search LogPanel:
```bash
# Search for specific text
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- logs search "camera" --json

# Search for errors
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- logs search "Error" --json
```

**3. Console log actions (developer debug logs)**

### Using automatic latest log discovery

The `--latest` flag automatically finds the most recent yyyyMMdd log folder. This is the recommended approach for most cases:

```bash
# List files from latest log folder (recommended)
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- console-logs list --latest --json

# Tail from latest log folder
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- console-logs tail 50 --latest --json

# Search in latest log folder
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- console-logs search "Exception" --latest --json
```

### Using specific date folders

For historical analysis, use `--date` to specify a particular session:

```bash
# List log files for specific date
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- console-logs list --date 20260118 --json

# Tail from specific date
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- console-logs tail 50 --date 20260118 --json

# Search in specific date
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- console-logs search "Exception" --date 20260118 --json
```

### When to use --latest vs --date

- **Use `--latest`** for automation, debugging current sessions, or when you don't know the exact log date
- **Use `--date`** only when analyzing a specific past session or comparing historical logs

### Other console log commands

Read recent console logs (default behavior uses all folders):
```bash
# Get last 50 lines (default: latest folder)
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- console-logs tail 50 --json

# Get last 100 lines
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- console-logs tail 100 --json
```

Search console logs (default behavior uses all folders):
```bash
# Search for exceptions
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- console-logs search "Exception" --json

# Search for specific service
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- console-logs search "FileWatcherService" --json

# Search for errors
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- console-logs search "fail:" --json
```

**4. Analyze results**

Parse JSON output and format for user:
- Show log count
- Highlight errors/warnings
- Show relevant excerpts
- Identify patterns

**5. Default behavior**

If no arguments provided:
1. Get last 20 LogPanel entries
2. Get last 50 console log entries
3. Display summary with any errors highlighted
</process>

<success_criteria>
- [ ] Correct log source accessed
- [ ] Logs retrieved successfully
- [ ] Errors/warnings highlighted in output
- [ ] Search results show relevant excerpts

**Output examples:**

```
[logs] LogPanel: 20 entries retrieved
[logs] Console: 50 lines retrieved
[logs]
[logs] === ERRORS FOUND ===
[logs] [ERROR] [14:32:00] [CameraLauncher] NIR 카메라 시작 실패
[logs] fail: ChronoView.Services.NirCameraLauncher[50]
[logs]       Failed to launch NIR camera: Port already in use
```
</success_criteria>

<troubleshooting>
**"No log files found" for console logs:**
- ChronoView may not have been run yet
- Check log directory exists: %LOCALAPPDATA%\prische\ChronoView\Logs\
- Verify date filter is correct

**"MainWindow not found" for LogPanel:**
- ChronoView must be running to access LogPanel
- Use /test:launch to start the application

**Empty log results:**
- Application may have just started (no logs yet)
- Try increasing tail count
- Check if logging is configured correctly
</troubleshooting>

<log_guide>
## Log Source Selection Guide

| Situation | Use Source | Reason |
|-----------|------------|--------|
| User reports error | LogPanel | Shows user-facing errors |
| UI not responding | Console | Has detailed traces |
| Camera not working | Console | Shows port conflicts |
| File not detected | Console | Shows file watcher events |
| Verification after fix | LogPanel | Confirms fix worked |
| Debugging new feature | Console | Shows execution flow |

## Common Search Patterns

| Pattern | Console | LogPanel |
|---------|---------|----------|
| Errors | "fail:", "Exception", "Error" | "[ERROR]" |
| Warnings | "warn:" | "[WARN]" |
| File events | "File created", "File detected" | "파일 감지" |
| Camera issues | "Camera", "Port", "NIR" | "카메라" |
| Service state | "Starting", "Stopping", "Initialized" | N/A |
</log_guide>
