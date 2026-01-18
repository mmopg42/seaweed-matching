# ChronoView UI Automation CLI

A command-line interface for programmatic control of ChronoView using Windows UI Automation (FlaUI 5.x).

## Quick Start

### Build

```bash
cd skills_scripts/ui_automation
dotnet build
```

### Run

```bash
# From the ui_automation directory
dotnet run -- [command] [options]

# Or run the built executable
bin/Debug/net10.0/ui_automation.exe [command] [options]
```

### Verify Installation

```bash
# Check if ChronoView is running
ui_automation.exe windows main

# Run connectivity test
ui_automation.exe test connectivity --json
```

## Common Usage Examples

### 1. Check if ChronoView is Running

```bash
ui_automation.exe windows main --json
# Output: {"success":true,"data":{"found":true,...}}
```

**Exit codes:** 0 = running, 2 = not found

### 2. Start and Stop Monitoring

```bash
# Start monitoring
ui_automation.exe toolbar start

# Stop monitoring
ui_automation.exe toolbar stop

# Check status (Stop button enabled = monitoring active)
ui_automation.exe toolbar enabled "중지" --json
```

### 3. Read File Groups from DataGrid

```bash
# Get row count
ui_automation.exe datagrid rows --json
# {"success":true,"data":{"rowCount":42}}

# Get all data
ui_automation.exe datagrid data --json

# Export to file
ui_automation.exe datagrid export > data.json
```

### 4. Configure Monitoring Paths

```bash
# Open settings dialog
ui_automation.exe settings-dialog open

# Set Line 1 paths
ui_automation.exe settings-dialog path set nir1 "/mnt/c/nir1"
ui_automation.exe settings-dialog path set normal1 "/mnt/c/normal1"
ui_automation.exe settings-dialog path set cam1 "/mnt/c/cam1"

# Save and close
ui_automation.exe settings-dialog action save
```

### 5. Move File Groups by GroupId

```bash
# Move single group
ui_automation.exe file-ops move group-ids --group-ids GRP-001

# Move multiple groups
ui_automation.exe file-ops move group-ids --group-ids GRP-001,GRP-002,GRP-003

# Wait for completion
ui_automation.exe file-ops wait move --timeout 60000
```

### 6. Read Logs

```bash
# Get all logs
ui_automation.exe logs get --json

# Get latest 20
ui_automation.exe logs tail 20 --json

# Filter errors only
ui_automation.exe logs filter --level Error --json

# Search for text
ui_automation.exe logs search "File moved" --json
```

## Agent Integration Guide

### Exit Code Handling

Always check exit codes to determine success/failure:

```python
import subprocess

def run_cli(args):
    result = subprocess.run(
        ["ui_automation.exe", "--json"] + args,
        capture_output=True
    )
    return result.returncode, json.loads(result.stdout)

# Exit codes: 0=success, 1=error, 2=not_found, 3=timeout, 4=invalid_arg
```

### JSON Output Parsing

Use `--json` flag for programmatic consumption:

```python
code, data = run_cli(["datagrid", "rows"])
if code == 0 and data["success"]:
    row_count = data["data"]["rowCount"]
```

### Error Handling Patterns

```python
def safe_run_cli(args):
    code, data = run_cli(args)

    if code == 2:
        print("Error: Window not found - is ChronoView running?")
        return None
    elif code == 3:
        print("Error: Operation timed out")
        return None
    elif code != 0:
        print(f"Error: {data.get('error', 'Unknown error')}")
        return None

    return data["data"]
```

### Best Practices

1. **Always use `--json`** - Consistent output format for parsing
2. **Check exit codes first** - Parse JSON only if exit code is 0
3. **Use `--quiet`** - Suppress unnecessary output when logging elsewhere
4. **Handle `EXIT_NOT_FOUND (2)`** - ChronoView may not be running
5. **Use specific subcommands** - More reliable than generic commands

## Command Reference

| Group | Description | Key Commands |
|-------|-------------|--------------|
| `windows` | Window detection | `windows main`, `windows all` |
| `toolbar` | Toolbar buttons | `toolbar start`, `toolbar stop` |
| `datagrid` | DataGrid operations | `datagrid data`, `datagrid export` |
| `workflow` | WorkflowPanel control | `workflow camera-states`, `workflow path` |
| `logs` | LogPanel operations | `logs get`, `logs filter` |
| `settings-dialog` | Settings control | `settings-dialog open`, `settings-dialog path` |
| `file-ops` | File operations | `file-ops move`, `file-ops delete` |

## Global Options

| Option | Short | Description |
|--------|-------|-------------|
| `--json` | `-j` | Output JSON for programmatic access |
| `--quiet` | `-q` | Suppress all output except errors |
| `--verbose` | `-v` | Enable debug output |

## Build and Deployment

### Dependencies

- .NET 10.0
- FlaUI.UIA3 5.0.0
- System.CommandLine 2.0.0-beta4

### Publish as Standalone

```bash
# Windows self-contained
dotnet publish -c Release -r win-x64 --self-contained true

# Output in bin/Release/net10.0/win-x64/publish/
# Copy ui_automation.exe to your desired location
```

### Project Structure

```
skills_scripts/ui_automation/
|-- Program.cs                    # Main CLI entry point
|-- ChronoWindowFinder.cs         # Window detection
|-- ChronoToolbarController.cs    # Toolbar control
|-- ChronoDataPanelReader.cs      # DataGrid/LogPanel reading
|-- ChronoWorkflowController.cs   # WorkflowPanel control
|-- ChronoSettingsController.cs   # SettingsDialog control
|-- ChronoFileOperationsController.cs # File operations
|-- UiAutomation.cs               # Core FlaUI automation
```

## Additional Documentation

- **Full CLI Reference:** `docs/cli-reference.md`
- **Test Matrix:** `docs/cli-test-matrix.md`
- **FlaUI Documentation:** https://flaui.com/

## Troubleshooting

### "MainWindow not found"

**Cause:** ChronoView is not running

**Solution:** Start ChronoView.exe first

### "DataGrid not found"

**Cause:** MainWindow is not visible or DataGrid not loaded

**Solution:** Ensure ChronoView window is visible and fully loaded

### Timeout Errors

**Cause:** Operation took longer than expected

**Solution:**
- Increase timeout with `--timeout` option
- Check if ChronoView is responsive
- Use `--verbose` to debug

### Button Click Fails

**Cause:** Button may be disabled or not found

**Solution:**
- Check button state with `toolbar enabled <text>`
- Verify button text matches (may be Korean)
- Use `--verbose` for search details
