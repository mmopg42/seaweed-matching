# ChronoView CLI Reference

Complete reference documentation for the ChronoView UI Automation CLI. This CLI enables programmatic control of ChronoView through Windows UI Automation (FlaUI 5.x).

## Overview

The ChronoView CLI provides agent developers with a command-line interface to automate ChronoView operations. It uses FlaUI UIA3 for Windows UI Automation, allowing interaction with all major ChronoView UI elements.

**Purpose:** Enable AI agents and automation scripts to control ChronoView without manual interaction.

**Executable:** `ui_automation.exe` (built from `skills_scripts/ui_automation/Program.cs`)

**Target Application:** ChronoView.exe (must be running for most commands)

### Build and Run

```bash
# Build
cd skills_scripts/ui_automation
dotnet build

# Run (from skills_scripts/ui_automation directory)
dotnet run

# Run built executable
bin/Debug/net10.0/ui_automation.exe [command] [options]
```

### Requirements

- .NET 10.0
- Windows OS
- FlaUI.UIA3 5.0.0
- ChronoView.exe running (for most commands)

## Global Options

| Option | Short | Description |
|--------|-------|-------------|
| `--quiet` | `-q` | Suppress all non-error output (for agent consumption) |
| `--verbose` | `-v` | Enable verbose output for debugging |
| `--json` | `-j` | Output in JSON format for programmatic access |

### Usage Examples

```bash
# Quiet mode - only errors are output
ui_automation.exe --quiet windows main

# Verbose mode - detailed debug information
ui_automation.exe --verbose workflow camera-states

# JSON output - parse with jq or other tools
ui_automation.exe --json datagrid data | jq '.data.data'
```

## Exit Codes

The CLI uses standard exit codes for programmatic consumption:

| Code | Constant | Meaning |
|------|----------|---------|
| 0 | EXIT_SUCCESS | Command completed successfully |
| 1 | EXIT_ERROR | General error or operation failed |
| 2 | EXIT_NOT_FOUND | Window/dialog/element not found |
| 3 | EXIT_TIMEOUT | Operation timed out |
| 4 | EXIT_INVALID_ARGUMENT | Invalid argument provided |

## Command Groups

### `windows` - Window Detection Commands

Find and inspect ChronoView windows.

| Subcommand | Description | JSON Support |
|------------|-------------|--------------|
| `windows main` | Find ChronoView MainWindow | Yes |
| `windows setup` | Find SetupWindow | Yes |
| `windows settings` | Find SettingsDialog | Yes |
| `windows preview` | Find ImagePreviewWindow | Yes |
| `windows all` | List all ChronoView windows | Yes |

#### Examples

```bash
# Check if ChronoView is running
ui_automation.exe windows main --json
# Output: {"success":true,"data":{"found":true,"windowType":"MainWindow",...}}

# List all windows
ui_automation.exe windows all
# Output:
# [All ChronoView Windows] Found: 2
#   - 'ChronoView - MainWindow'
#   - 'Settings'
```

#### JSON Output Schema

```json
{
  "success": true,
  "data": {
    "found": true,
    "windowType": "MainWindow",
    "title": "ChronoView - MainWindow",
    "className": "Window",
    "automationId": null
  }
}
```

---

### `toolbar` - Toolbar Button Control

Control ChronoView toolbar buttons.

| Subcommand | Description | Requires |
|------------|-------------|----------|
| `toolbar start` | Click Start button | MainWindow |
| `toolbar stop` | Click Stop button | MainWindow |
| `toolbar settings` | Click Settings button | MainWindow |
| `toolbar refresh` | Click Refresh button | MainWindow |
| `toolbar move` | Click Move button | MainWindow |
| `toolbar delete` | Click Delete button | MainWindow |
| `toolbar list` | List all toolbar buttons | MainWindow |
| `toolbar click <text>` | Click button by text | MainWindow |
| `toolbar enabled <text>` | Check if button enabled | MainWindow |

#### Examples

```bash
# Start monitoring
ui_automation.exe toolbar start

# Check if Stop button is enabled (monitoring active)
ui_automation.exe toolbar enabled "중지"

# Click button by text
ui_automation.exe toolbar click "새로고침"

# List all buttons
ui_automation.exe toolbar list --json
```

---

### `datagrid` - DataGrid Data Operations

Read data from the ChronoView DataGrid panel.

| Subcommand | Description | JSON Support |
|------------|-------------|--------------|
| `datagrid headers` | Get column headers | Yes |
| `datagrid rows` | Get row count | Yes |
| `datagrid data` | Get all data | Yes |
| `datagrid info` | Get headers and row count | Yes |
| `datagrid cell <row> <col>` | Get specific cell value | Yes |
| `datagrid export` | Export all data as JSON | Always JSON |

#### Examples

```bash
# Get row count
ui_automation.exe datagrid rows --json
# {"success":true,"data":{"rowCount":42}}

# Get all data
ui_automation.exe datagrid data --json | jq '.data.data[]'

# Get specific cell (row 0, column 1)
ui_automation.exe datagrid cell 0 1 --json
# {"success":true,"data":{"row":0,"column":1,"value":"GRP-001"}}

# Export as formatted JSON
ui_automation.exe datagrid export > data.json
```

#### JSON Output Schema (data command)

```json
{
  "success": true,
  "data": {
    "rowCount": 42,
    "data": [
      {
        "Selected": "true",
        "GroupId": "GRP-001",
        "Timestamp": "2026-01-18 10:30:00",
        "Normal1": "/path/to/file.jpg",
        "NIR1": "/path/to/nir.spc",
        ...
      }
    ]
  }
}
```

---

### `workflow` - WorkflowPanel Control

Control the WorkflowPanel for camera operations and path settings.

| Subcommand | Description |
|------------|-------------|
| `workflow launch-general` | Launch General Camera |
| `workflow launch-nir` | Launch NIR 1 Camera |
| `workflow launch-nir2` | Launch NIR 2 Camera |
| `workflow toggle-filtering` | Toggle NIR Filtering |
| `workflow camera-states` | Get all camera states |
| `workflow path get-line1` | Get Line 1 paths |
| `workflow path get-line2` | Get Line 2 paths |
| `workflow path get-all` | Get all paths |
| `workflow path set-line1 <type> <value>` | Set Line 1 path |
| `workflow path set-line2 <type> <value>` | Set Line 2 path |

#### Examples

```bash
# Get camera states
ui_automation.exe workflow camera-states --json
# {"success":true,"data":{"source":"WorkflowPanel","count":4,...}}

# Get all paths
ui_automation.exe workflow path get-all --json

# Set Line 1 SampleName path
ui_automation.exe workflow path set-line1 samplename "/mnt/c/samples"

# Get Line 2 paths
ui_automation.exe workflow path get-line2
```

#### Path Types

- `samplename` - Sample name path
- `movenir` - Move NIR path
- `movealldata` - Move all data path

---

### `logs` - LogPanel Reading and Filtering

Read and filter logs from the LogPanel.

| Subcommand | Description | Arguments |
|------------|-------------|-----------|
| `logs get` | Get all log messages | - |
| `logs tail [count]` | Get latest N messages | count (default: 10) |
| `logs filter` | Filter by log level | `--level <Debug|Info|Warning|Error>` |
| `logs search <text>` | Search messages by text | search text |

#### Examples

```bash
# Get all logs
ui_automation.exe logs get --json

# Get latest 20 logs
ui_automation.exe logs tail 20 --json

# Filter errors only
ui_automation.exe logs filter --level Error --json

# Search for specific text
ui_automation.exe logs search "File moved" --json
```

#### JSON Output Schema

```json
{
  "success": true,
  "data": {
    "source": "LogPanel",
    "count": 150,
    "logs": [
      {
        "Severity": "Info",
        "Time": "10:30:15",
        "Source": "FileWatcher",
        "Message": "File detected: sample.jpg"
      }
    ]
  }
}
```

---

### `settings-dialog` - SettingsDialog Control

Open, navigate, and control the SettingsDialog.

| Subcommand | Description | Requires |
|------------|-------------|----------|
| `settings-dialog open` | Open SettingsDialog | MainWindow |
| `settings-dialog close` | Close SettingsDialog | Dialog open |
| `settings-dialog inspect` | Inspect dialog structure | Dialog open |
| `settings-dialog status` | Check if dialog is open | - |

#### Path Commands

| Subcommand | Description |
|------------|-------------|
| `settings-dialog path get-all` | Get all paths |
| `settings-dialog path get-line1` | Get Line 1 paths |
| `settings-dialog path get-line2` | Get Line 2 paths |
| `settings-dialog path get-output` | Get output path |
| `settings-dialog path get-quarantine` | Get quarantine path |
| `settings-dialog path set <key> <value>` | Set a path |

#### CheckBox Commands

| Subcommand | Description |
|------------|-------------|
| `settings-dialog checkbox get <name>` | Get checkbox state |
| `settings-dialog checkbox set <name> <value>` | Set checkbox state |
| `settings-dialog checkbox list` | List all advanced settings |

#### Action Commands

| Subcommand | Description |
|------------|-------------|
| `settings-dialog action save` | Click Save/OK (closes dialog) |
| `settings-dialog action apply` | Click Apply (keeps dialog open) |
| `settings-dialog action cancel` | Click Cancel (closes dialog) |
| `settings-dialog action reset` | Click Reset/Defaults |

#### Examples

```bash
# Open settings
ui_automation.exe settings-dialog open

# Get all paths
ui_automation.exe settings-dialog path get-all --json

# Set NIR1 path
ui_automation.exe settings-dialog path set nir1 "/mnt/c/nir1"

# Enable a checkbox
ui_automation.exe settings-dialog checkbox set use_folder_suffix true

# Save and close
ui_automation.exe settings-dialog action save
```

#### Path Keys

- `nir1`, `normal1` - Line 1 paths
- `nir2`, `normal2` - Line 2 paths
- `cam1`, `cam2`, `cam3` - Line 1 camera paths
- `cam4`, `cam5`, `cam6` - Line 2 camera paths

#### CheckBox Names

- `use_folder_suffix`
- `use_disk_cache`
- `auto_group_by_timestamp`
- `enable_nir_filtering`
- (See `settings-dialog checkbox list` for all options)

---

### `file-ops` - File Operation Automation

Select, move, and delete file groups in the DataGrid.

| Subcommand | Description |
|------------|-------------|
| `file-ops select row-index --row-index <n>` | Select row by index |
| `file-ops select group-id --group-id <id>` | Select row by GroupId |
| `file-ops select-all` | Select all rows |
| `file-ops clear-selection` | Clear all selections |
| `file-ops selected` | List selected rows |
| `file-ops move rows --rows <1,2,3>` | Move rows by index |
| `file-ops move group-ids --group-ids <id1,id2>` | Move rows by GroupId |
| `file-ops delete rows --rows <1,2,3>` | Delete rows by index |
| `file-ops delete group-ids --group-ids <id1,id2>` | Delete rows by GroupId |
| `file-ops wait move --timeout <ms>` | Wait for move completion |
| `file-ops wait delete --timeout <ms>` | Wait for delete completion |
| `file-ops confirm` | Handle confirmation dialog |
| `file-ops verify deleted <groupId>` | Verify group was deleted |
| `file-ops verify row-count <original> --timeout <ms>` | Verify row count changed |

#### Examples

```bash
# Select row by index
ui_automation.exe file-ops select row-index --row-index 0

# Select and move by GroupId
ui_automation.exe file-ops move group-ids --group-ids GRP-001,GRP-002

# Select and delete by index
ui_automation.exe file-ops delete rows --rows 0,1,2

# Wait for move completion
ui_automation.exe file-ops wait move --timeout 60000

# Verify deletion
ui_automation.exe file-ops verify deleted GRP-001 --json
```

---

### `test` - Connectivity and Capability Tests

Verify ChronoView connection and basic operations.

| Subcommand | Description |
|------------|-------------|
| `test connectivity` | Verify ChronoView is running |
| `test main-window` | Verify MainWindow is accessible |
| `test datagrid` | Verify DataGrid is readable |
| `test workflow` | Verify WorkflowPanel is accessible |

#### Examples

```bash
# Basic connectivity check
ui_automation.exe test connectivity
# Output: [test-connectivity] ChronoView is running

# With JSON
ui_automation.exe test connectivity --json
# {"success":true,"data":{"running":true}}
```

---

### `scenario` - End-to-End Workflow Automation

Complete workflows combining multiple operations.

| Subcommand | Description |
|------------|-------------|
| `scenario configure-paths <json>` | Configure all monitoring paths |
| `scenario start-monitoring` | Start file monitoring |
| `scenario stop-monitoring` | Stop file monitoring |
| `scenario export-data` | Export all DataGrid data |

#### Examples

```bash
# Configure paths from JSON
ui_automation.exe scenario configure-paths paths.json

# Start monitoring
ui_automation.exe scenario start-monitoring

# Export all data
ui_automation.exe scenario export-data > export.json
```

---

### `inspect` - UI Structure Inspection

Inspect UI element structures for development/debugging.

| Subcommand | Description |
|------------|-------------|
| `inspect workflow` | Inspect WorkflowPanel structure |
| `inspect log` | Inspect LogPanel structure |

#### Examples

```bash
# Inspect workflow panel
ui_automation.exe inspect workflow
# Output: Element tree with depth=3 showing all buttons, textboxes, expanders

# Inspect log panel
ui_automation.exe inspect log
# Output: Element tree with depth=2 showing LogDataGrid, search controls
```

---

## Common Workflows

### Verify ChronoView is Running

```bash
ui_automation.exe windows main --json
# Check exit code: 0 = running, 2 = not found
```

### Start/Stop Monitoring

```bash
# Start
ui_automation.exe toolbar start

# Stop
ui_automation.exe toolbar stop

# Check status (Stop button enabled = monitoring active)
ui_automation.exe toolbar enabled "중지" --json
```

### Read All Data from DataGrid

```bash
# JSON output
ui_automation.exe datagrid data --json > all_data.json

# Pretty print
ui_automation.exe datagrid data --json | jq '.data.data'

# Row count only
ui_automation.exe datagrid rows --json
```

### Configure Monitoring Paths

```bash
# Open settings
ui_automation.exe settings-dialog open

# Set paths
ui_automation.exe settings-dialog path set nir1 "/mnt/c/nir1"
ui_automation.exe settings-dialog path set normal1 "/mnt/c/normal1"
ui_automation.exe settings-dialog path set cam1 "/mnt/c/cam1"

# Save and close
ui_automation.exe settings-dialog action save
```

### Move File Groups by GroupId

```bash
# Single group
ui_automation.exe file-ops move group-ids --group-ids GRP-001

# Multiple groups
ui_automation.exe file-ops move group-ids --group-ids GRP-001,GRP-002,GRP-003

# Wait for completion
ui_automation.exe file-ops wait move --timeout 60000
```

### Export All Data as JSON

```bash
# Method 1: datagrid export (always JSON)
ui_automation.exe datagrid export > export.json

# Method 2: datagrid data with --json flag
ui_automation.exe datagrid data --json > export.json

# Method 3: Format with jq
ui_automation.exe datagrid data --json | jq '{data: .data.data, count: .data.rowCount}' > export.json
```

---

## JSON Output Format Reference

### Success Response

All commands with `--json` return a consistent success format:

```json
{
  "success": true,
  "data": {
    // Command-specific data
  }
}
```

### Error Response

Errors follow a consistent format:

```json
{
  "success": false,
  "error": "Error message description",
  "errorCode": 2
}
```

### Per-Command Data Schemas

| Command | Data Schema |
|---------|-------------|
| `windows main` | `{found, windowType, title, className, automationId}` |
| `datagrid data` | `{rowCount, data: [{...}]}` |
| `datagrid rows` | `{rowCount}` |
| `workflow camera-states` | `{count, states: {camera: state, ...}}` |
| `logs get` | `{count, logs: [{Severity, Time, Source, Message}]}` |
| `settings-dialog status` | `{isOpen}` |
| `file-ops selected` | `{count, selectedRows: [...]}` |

---

## Best Practices for Agent Integration

1. **Always use `--json`** for programmatic consumption
2. **Check exit codes** to determine success/failure
3. **Use `--quiet`** to suppress unnecessary output
4. **Handle `EXIT_NOT_FOUND (2)`** gracefully - ChronoView may not be running
5. **Use specific subcommands** instead of generic commands when available
6. **Parse JSON with a proper library** (jq, System.Text.Json, etc.)
7. **Verify window state** before operations that require specific windows

### Example: Agent Pseudo-code

```python
import subprocess
import json

def run_cli(cmd):
    result = subprocess.run(
        ["ui_automation.exe", "--json"] + cmd,
        capture_output=True,
        text=True
    )
    output = json.loads(result.stdout) if result.stdout else {}
    return result.returncode, output

# Check if ChronoView is running
code, data = run_cli(["windows", "main"])
if code == 0 and data["success"]:
    # Get row count
    code, data = run_cli(["datagrid", "rows"])
    if code == 0:
        print(f"Rows: {data['data']['rowCount']}")
```

---

## Error Handling

### Common Errors and Solutions

| Error | Exit Code | Solution |
|-------|-----------|----------|
| "MainWindow not found" | 2 | Start ChronoView.exe |
| "DataGrid not found" | 2 | Ensure MainWindow is visible |
| "SettingsDialog not found" | 2 | Open settings first with `settings-dialog open` |
| "Timeout" | 3 | Increase timeout or check if ChronoView is responsive |
| "Invalid argument" | 4 | Check command syntax and argument values |

### Debug Mode

Use `--verbose` to get detailed information about UI element searches:

```bash
ui_automation.exe --verbose workflow camera-states
```

---

## Additional Documentation

- **Quick Start Guide:** `skills_scripts/ui_automation/README.md`
- **Test Matrix:** `docs/cli-test-matrix.md`
- **FlaUI Documentation:** https://flaui.com/
- **ChronoView User Manual:** `docs/manual/`
