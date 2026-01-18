# CLI Test Matrix

Complete command inventory and test coverage matrix for the ChronoView UI Automation CLI.

## Command Inventory

### `windows` Command Group

| Subcommand | Arguments | Options | JSON Support | Exit Codes |
|------------|-----------|---------|--------------|------------|
| `windows main` | - | `--json`, `-j` | Yes | 0, 2 |
| `windows setup` | - | `--json`, `-j` | Yes | 0, 2 |
| `windows settings` | - | `--json`, `-j` | Yes | 0, 2 |
| `windows preview` | - | `--json`, `-j` | Yes | 0, 2 |
| `windows all` | - | `--json`, `-j` | Yes | 0 |

### `toolbar` Command Group

| Subcommand | Arguments | Options | JSON Support | Exit Codes |
|------------|-----------|---------|--------------|------------|
| `toolbar start` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `toolbar stop` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `toolbar settings` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `toolbar refresh` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `toolbar move` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `toolbar delete` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `toolbar list` | - | `--json`, `-j` | Yes | 0 |
| `toolbar click <text>` | text | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `toolbar enabled <text>` | text | `--quiet`, `-q`, `--verbose`, `-v` | No | 0 |

### `datagrid` Command Group

| Subcommand | Arguments | Options | JSON Support | Exit Codes |
|------------|-----------|---------|--------------|------------|
| `datagrid headers` | - | `--json`, `-j` | Yes | 0, 2 |
| `datagrid rows` | - | `--json`, `-j` | Yes | 0, 2 |
| `datagrid data` | - | `--json`, `-j` | Yes | 0, 2 |
| `datagrid info` | - | `--json`, `-j` | Yes | 0, 2 |
| `datagrid cell <row> <col>` | row, col | `--json`, `-j` | Yes | 0, 2, 4 |
| `datagrid export` | - | (Always JSON) | Always | 0, 2 |

### `workflow` Command Group

| Subcommand | Arguments | Options | JSON Support | Exit Codes |
|------------|-----------|---------|--------------|------------|
| `workflow launch-general` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `workflow launch-nir` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `workflow launch-nir2` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `workflow toggle-filtering` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `workflow camera-states` | - | `--json`, `-j` | Yes | 0 |
| `workflow path get-line1` | - | `--json`, `-j` | Yes | 0 |
| `workflow path get-line2` | - | `--json`, `-j` | Yes | 0 |
| `workflow path get-all` | - | `--json`, `-j` | Yes | 0 |
| `workflow path set-line1 <type> <value>` | type, value | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `workflow path set-line2 <type> <value>` | type, value | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |

### `logs` Command Group

| Subcommand | Arguments | Options | JSON Support | Exit Codes |
|------------|-----------|---------|--------------|------------|
| `logs get` | - | `--json`, `-j` | Yes | 0 |
| `logs tail [count]` | count (optional) | `--json`, `-j` | Yes | 0 |
| `logs filter` | - | `--level`, `-l`, `--json`, `-j` | Yes | 0 |
| `logs search <text>` | text | `--json`, `-j` | Yes | 0 |

### `settings-dialog` Command Group

| Subcommand | Arguments | Options | JSON Support | Exit Codes |
|------------|-----------|---------|--------------|------------|
| `settings-dialog open` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `settings-dialog close` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `settings-dialog inspect` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `settings-dialog status` | - | `--json`, `-j` | Yes | 0 |
| `settings-dialog path get-all` | - | `--json`, `-j` | Yes | 0 |
| `settings-dialog path get-line1` | - | `--json`, `-j` | Yes | 0 |
| `settings-dialog path get-line2` | - | `--json`, `-j` | Yes | 0 |
| `settings-dialog path get-output` | - | `--json`, `-j` | Yes | 0 |
| `settings-dialog path get-quarantine` | - | `--json`, `-j` | Yes | 0 |
| `settings-dialog path set <key> <value>` | key, value | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `settings-dialog checkbox get <name>` | name | `--json`, `-j` | Yes | 0 |
| `settings-dialog checkbox set <name> <value>` | name, value | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `settings-dialog checkbox list` | - | `--json`, `-j` | Yes | 0 |
| `settings-dialog action save` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `settings-dialog action apply` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `settings-dialog action cancel` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `settings-dialog action reset` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |

### `file-ops` Command Group

| Subcommand | Arguments | Options | JSON Support | Exit Codes |
|------------|-----------|---------|--------------|------------|
| `file-ops select row-index` | - | `--row-index`, `-r` | No | 0, 1 |
| `file-ops select group-id` | - | `--group-id`, `-g` | No | 0, 1 |
| `file-ops select-all` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `file-ops clear-selection` | - | `--quiet`, `-q`, `--verbose`, `-v` | No | 0, 1 |
| `file-ops selected` | - | `--json`, `-j` | Yes | 0 |
| `file-ops move rows` | - | `--rows`, `-r` | No | 0, 1 |
| `file-ops move group-ids` | - | `--group-ids`, `-g` | No | 0, 1 |
| `file-ops delete rows` | - | `--rows`, `-r` | No | 0, 1 |
| `file-ops delete group-ids` | - | `--group-ids`, `-g` | No | 0, 1 |
| `file-ops wait move` | - | `--timeout`, `-t` | No | 0, 1 |
| `file-ops wait delete` | - | `--timeout`, `-t` | No | 0, 1 |
| `file-ops confirm` | - | `--json`, `-j` | Yes | 0, 1 |
| `file-ops verify deleted <groupId>` | groupId | `--json`, `-j` | Yes | 0, 1 |
| `file-ops verify row-count <original>` | original | `--timeout`, `-t`, `--json`, `-j` | Yes | 0, 1 |

### `inspect` Command Group

| Subcommand | Arguments | Options | JSON Support | Exit Codes |
|------------|-----------|---------|--------------|------------|
| `inspect workflow` | - | (No) | No | 0 |
| `inspect log` | - | (No) | No | 0 |

### Legacy Commands (Deprecated)

| Subcommand | Arguments | Options | JSON Support | Exit Codes |
|------------|-----------|---------|--------------|------------|
| `detect` | - | (No) | No | 0 |
| `list` | - | (No) | No | 0 |
| `find` | - | `--title`, `-t`, `--process`, `-p` | No | 0 |
| `click start` | - | (No) | No | 0 |
| `click stop` | - | (No) | No | 0 |
| `click settings` | - | (No) | No | 0 |
| `click refresh` | - | (No) | No | 0 |
| `click move` | - | (No) | No | 0 |
| `click delete` | - | (No) | No | 0 |
| `stats` | - | `--json`, `-j` | Yes | 0, 2 |

---

## Test Coverage Matrix

### Prerequisites by Command

| Command | ChronoView Running | MainWindow Visible | SettingsDialog Open | Data Required |
|---------|-------------------|--------------------|---------------------|---------------|
| `windows main` | Required | No | No | No |
| `windows setup` | Optional | No | No | No |
| `windows settings` | Optional | No | No | No |
| `windows preview` | Optional | No | No | No |
| `windows all` | Optional | No | No | No |
| `toolbar *` | Required | Yes | No | No |
| `datagrid *` | Required | Yes | No | No |
| `workflow *` | Required | Yes | No | No |
| `logs *` | Required | Yes | No | Logs visible |
| `settings-dialog open` | Required | Yes | No | No |
| `settings-dialog close` | Required | Yes | Yes | No |
| `settings-dialog path *` | Required | Yes | Yes | No |
| `settings-dialog checkbox *` | Required | Yes | Yes | No |
| `settings-dialog action *` | Required | Yes | Yes | No |
| `file-ops select *` | Required | Yes | No | Data in grid |
| `file-ops move *` | Required | Yes | No | Data in grid |
| `file-ops delete *` | Required | Yes | No | Data in grid |
| `file-ops wait *` | Required | Yes | No | After operation |
| `file-ops verify *` | Required | Yes | No | After operation |

### Destructive Operations

Commands that modify data or state - **use with caution**:

| Command | Impact | Reversible |
|---------|--------|------------|
| `toolbar start` | Starts file monitoring | Yes (stop) |
| `toolbar stop` | Stops file monitoring | Yes (start) |
| `workflow launch-*` | Launches external camera app | Yes (close app) |
| `workflow toggle-filtering` | Changes filtering state | Yes (toggle again) |
| `settings-dialog path set` | Modifies configuration | No (backup first) |
| `settings-dialog checkbox set` | Modifies configuration | No (backup first) |
| `settings-dialog action save` | Saves configuration | No (backup first) |
| `settings-dialog action reset` | Resets to defaults | No (backup first) |
| `file-ops move *` | Moves file groups | No (manual revert) |
| `file-ops delete *` | Deletes file groups | No (backup required) |

---

## Test Scenarios

### Basic Connectivity Test Sequence

Verify the CLI can connect to ChronoView:

```bash
# 1. Build the CLI
dotnet build skills_scripts/ui_automation

# 2. Check if ChronoView is running
ui_automation.exe windows main --json
# Expected: exit code 0, success=true

# 3. List all windows
ui_automation.exe windows all --json
# Expected: at least MainWindow listed

# 4. Get toolbar button list
ui_automation.exe toolbar list --json
# Expected: array of button names

# 5. Get DataGrid row count
ui_automation.exe datagrid rows --json
# Expected: rowCount >= 0
```

### Full Workflow Test Sequence

Test a complete monitoring cycle:

```bash
# 1. Verify ChronoView is running
ui_automation.exe windows main
# Expected: exit code 0

# 2. Stop any active monitoring
ui_automation.exe toolbar stop

# 3. Open settings dialog
ui_automation.exe settings-dialog open
# Expected: exit code 0

# 4. Configure paths
ui_automation.exe settings-dialog path set nir1 "/test/nir1"
ui_automation.exe settings-dialog path set normal1 "/test/normal1"

# 5. Save settings
ui_automation.exe settings-dialog action save

# 6. Start monitoring
ui_automation.exe toolbar start
# Expected: exit code 0, Stop button becomes enabled

# 7. Verify monitoring is active
ui_automation.exe toolbar enabled "중지"
# Expected: "Button '중지' is enabled"

# 8. Get DataGrid info
ui_automation.exe datagrid info --json

# 9. Read logs
ui_automation.exe logs tail 10 --json

# 10. Stop monitoring
ui_automation.exe toolbar stop
```

### File Operations Test Sequence

Test file group operations (requires test data):

```bash
# 1. Get initial row count
ui_automation.exe datagrid rows --json
# Save rowCount as INITIAL_COUNT

# 2. Select a row
ui_automation.exe file-ops select row-index --row-index 0

# 3. Verify selection
ui_automation.exe file-ops selected --json
# Expected: selectedRows contains 0

# 4. Move the row
ui_automation.exe file-ops move rows --rows 0

# 5. Wait for completion
ui_automation.exe file-ops wait move --timeout 30000
# Expected: Success

# 6. Verify row count changed
ui_automation.exe datagrid rows --json
# Expected: rowCount = INITIAL_COUNT - 1
```

### Error Handling Test Cases

Test error conditions:

```bash
# Test 1: ChronoView not running (exit code 2)
# - Close ChronoView
# - Run: ui_automation.exe windows main
# - Expected: exit code 2, "not found" in output

# Test 2: Invalid row index (exit code 4)
# - Run: ui_automation.exe datagrid cell 9999 0
# - Expected: exit code 4, "out of range" in output

# Test 3: SettingsDialog not open (exit code 2)
# - Ensure SettingsDialog is closed
# - Run: ui_automation.exe settings-dialog path get-all
# - Expected: exit code 2 or handles gracefully

# Test 4: Invalid GroupId (exit code 1)
# - Run: ui_automation.exe file-ops move group-ids --group-ids INVALID-ID
# - Expected: exit code 1 or graceful handling
```

### Settings Dialog Test Sequence

Test SettingsDialog navigation and modification:

```bash
# 1. Open dialog
ui_automation.exe settings-dialog open

# 2. Check status
ui_automation.exe settings-dialog status --json
# Expected: isOpen = true

# 3. Inspect structure
ui_automation.exe settings-dialog inspect

# 4. Get all paths
ui_automation.exe settings-dialog path get-all --json

# 5. Get checkbox states
ui_automation.exe settings-dialog checkbox list --json

# 6. Modify a checkbox
ui_automation.exe settings-dialog checkbox set use_folder_suffix true

# 7. Verify change
ui_automation.exe settings-dialog checkbox get use_folder_suffix --json
# Expected: isChecked = true

# 8. Cancel (discard changes)
ui_automation.exe settings-dialog action cancel

# 9. Verify dialog closed
ui_automation.exe settings-dialog status --json
# Expected: isOpen = false
```

---

## Verification Checklist

Use this checklist for manual testing:

- [ ] Build succeeds without errors
- [ ] CLI help output lists all commands (`ui_automation.exe --help`)
- [ ] `windows main` returns correct result when ChronoView running
- [ ] `windows main` returns exit code 2 when ChronoView not running
- [ ] `toolbar list` returns all button names
- [ ] `datagrid rows` returns correct row count
- [ ] `datagrid data` returns all rows with correct columns
- [ ] `datagrid export` produces valid JSON file
- [ ] `workflow camera-states` returns all camera states
- [ ] `workflow path get-all` returns all configured paths
- [ ] `logs get` returns log messages
- [ ] `logs filter --level Error` filters correctly
- [ ] `logs search <text>` finds matching messages
- [ ] `settings-dialog open` opens the dialog
- [ ] `settings-dialog path get-all` returns paths when dialog open
- [ ] `settings-dialog checkbox list` returns all checkboxes
- [ ] `file-ops select row-index` selects the row
- [ ] `file-ops selected` returns selected rows
- [ ] `file-ops move rows` initiates move operation
- [ ] `file-ops wait move` waits for completion
- [ ] `file-ops verify deleted` correctly verifies deletion
- [ ] All JSON outputs are valid and parseable
- [ ] Exit codes match expected values
- [ ] `--quiet` suppresses non-error output
- [ ] `--verbose` adds debug information

---

## Test Notes

### Language Considerations

- Button text may be Korean (`"시작"`, `"중지"`, `"설정"`, etc.)
- Tab headers support bilingual search (English first, Korean fallback)
- Error messages may be mixed language

### Timing Considerations

- Window detection may take 1-2 seconds
- File operations may take 10-30 seconds depending on data size
- SettingsDialog save/close may take 1-3 seconds
- Use appropriate timeouts for wait commands

### State Dependencies

- Some commands require ChronoView to be running
- Some commands require specific windows to be open
- Some commands require data to be present in DataGrid
- Test sequences should respect these dependencies

### Environment

- Tests should be run on Windows OS
- ChronoView.exe should be in a known location or already running
- Test data paths should exist before running file operation tests
