---
name: test:action
description: Perform UI actions on ChronoView (toolbar, workflow, settings)
argument-hint: [action] [additional args...]
allowed-tools:
  - Bash
  - AskUserQuestion
---

<objective>
Execute UI automation actions on the running ChronoView application.

This skill provides a unified interface for all UI operations:
- **Toolbar actions**: start, stop, settings, refresh, move, delete
- **Workflow actions**: launch cameras, toggle filtering, get camera states
- **Settings dialog**: open, close, get/set paths, checkboxes
</objective>

<context>
Working directory: C:\workspace\seaweed\gui_kiro_v2

UI Automation CLI: skills_scripts/ui_automation/bin/Debug/net10.0/ui_automation.exe

Arguments: $ARGUMENTS
</context>

<process>
**1. Parse action from arguments**

First argument determines the action category:
- `start`, `stop`, `settings`, `refresh`, `move`, `delete` → Toolbar
- `launch-general`, `launch-nir`, `launch-nir2`, `toggle-filtering`, `camera-states` → Workflow
- `open-settings`, `close-settings`, `get-paths`, `set-path` → Settings

**2. Execute toolbar actions**

```bash
# Start monitoring
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- toolbar start

# Stop monitoring
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- toolbar stop

# Settings button
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- toolbar settings

# Refresh
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- toolbar refresh

# Move selected items
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- toolbar move

# Delete selected items
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- toolbar delete
```

**3. Execute workflow actions**

```bash
# Launch General Camera
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- workflow launch-general

# Launch NIR 1 Camera
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- workflow launch-nir

# Launch NIR 2 Camera
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- workflow launch-nir2

# Toggle NIR Filtering
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- workflow toggle-filtering

# Get all camera states
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- workflow camera-states --json
```

**4. Execute settings dialog actions**

```bash
# Open settings dialog
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- settings-dialog open

# Close settings dialog
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- settings-dialog close

# Get all paths
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- settings-dialog path get-all --json

# Set a specific path (example: Line 1 General)
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- settings-dialog path set-line1 --camera general --path "/path/to/folder"
```

**5. Parse and report results**

All CLI commands support `--json` output for programmatic parsing.

Success: Exit code 0 with `success: true`
Failure: Exit code non-zero or `success: false`
</process>

<success_criteria>
- [ ] Action correctly identified from arguments
- [ ] CLI command executed successfully
- [ ] Result parsed and reported to user
- [ ] Error handling for common failures (window not found, button not found)

**Examples:**

```
[action] Toolbar: Clicked START button
[action] Result: SUCCESS
[action] Monitoring should now be active

[action] Workflow: Launching NIR 1 Camera
[action] Result: SUCCESS
[action] Camera launch initiated

[action] Error: MainWindow not found
[action] Suggestion: Use /test:launch to start ChronoView first
```
</success_criteria>

<troubleshooting>
**"MainWindow not found" error:**
- ChronoView may not be running → Use /test:launch
- ChronoView may be at SetupWindow → Navigate through setup first

**"Button not found" error:**
- Button may be disabled (check UI state)
- Button text may have changed (verify UI)
- May need to wait for UI to settle

**Settings dialog won't open:**
- Settings button may be disabled in current state
- Another dialog may already be open
</troubleshooting>

<action_reference>
## Quick Action Reference

| Action | Command | Description |
|--------|---------|-------------|
| start | toolbar start | Start monitoring |
| stop | toolbar stop | Stop monitoring |
| settings-btn | toolbar settings | Open settings |
| refresh | toolbar refresh | Refresh data |
| move | toolbar move | Move selected items |
| delete | toolbar delete | Delete selected items |
| launch-gen | workflow launch-general | Launch General Camera |
| launch-nir1 | workflow launch-nir | Launch NIR 1 Camera |
| launch-nir2 | workflow launch-nir2 | Launch NIR 2 Camera |
| toggle-nir | workflow toggle-filtering | Toggle NIR filtering |
| cameras | workflow camera-states | Get camera states |
| open-settings | settings-dialog open | Open settings dialog |
| close-settings | settings-dialog close | Close settings dialog |
</action_reference>
