# Test Executor Skills Registry

**Purpose:** Semantic skill registry for ChronoView CLI automation

This registry provides intent-based names (skills) for all ChronoView CLI automation commands. Skills are organized by category and follow UPPER_SNAKE_CASE naming convention (CATEGORY_ACTION).

**Usage:**
- `test-orchestrator` delegates by skill name (what to do)
- `test-executor` translates skill names to CLI commands (how to do it)

**Skill Format:** `CATEGORY_ACTION` (e.g., `APP_LAUNCH`, `TOOLBAR_START`)

**Related Documents:**
- [test-executor.md](test-executor.md) - Execution details and command patterns
- [test-orchestrator.md](test-orchestrator.md) - Orchestration and delegation

---

## Skill Overview Table

| Skill | Description | Category | CLI Command |
|-------|-------------|----------|-------------|
| APP_LAUNCH | Launch ChronoView application | APP | app launch |
| APP_STOP | Stop all ChronoView processes | APP | app stop |
| APP_RESTART | Restart ChronoView (stop + launch) | APP | app restart |
| APP_STATUS | Check ChronoView running status | APP | app status |
| BATCH_SELECT_AND_MOVE | Select multiple rows and move them | BATCH | batch select-and-move |
| BATCH_SELECT_AND_DELETE | Select multiple rows and delete them | BATCH | batch select-and-delete |
| BATCH_EXPORT_ALL | Export all available data | BATCH | batch export-all |
| CONSOLE_LOGS_LIST | List available console log files | CONSOLE_LOGS | console-logs list |
| CONSOLE_LOGS_TAIL | Read recent N lines from console log | CONSOLE_LOGS | console-logs tail |
| CONSOLE_LOGS_SEARCH | Search console logs for text | CONSOLE_LOGS | console-logs search |
| DATA_PANEL_STATS | Read StatisticsPanel data | DATA_PANEL | stats |
| DATA_PANEL_HEADERS | Get DataGrid column headers | DATA_PANEL | datagrid headers |
| DATA_PANEL_ROWS | Get DataGrid row count | DATA_PANEL | datagrid rows |
| DATA_PANEL_DATA | Extract all DataGrid data | DATA_PANEL | datagrid data |
| DATA_PANEL_INFO | Get DataGrid summary (headers + row count) | DATA_PANEL | datagrid info |
| DATA_PANEL_CELL | Get specific cell value | DATA_PANEL | datagrid cell |
| DATA_PANEL_EXPORT | Export DataGrid as JSON | DATA_PANEL | datagrid export |
| FILE_OPS_SELECT_ROW_INDEX | Select row by index | FILE_OPS | file-ops select row-index |
| FILE_OPS_SELECT_GROUP_ID | Select row by GroupId | FILE_OPS | file-ops select group-id |
| FILE_OPS_SELECT_PREFIX | Select rows by GroupId prefix | FILE_OPS | file-ops select prefix |
| FILE_OPS_SELECT_ALL | Select all rows | FILE_OPS | file-ops select-all |
| FILE_OPS_CLEAR_SELECTION | Clear row selection | FILE_OPS | file-ops clear-selection |
| FILE_OPS_SELECTED | Get selected row indices | FILE_OPS | file-ops selected |
| FILE_OPS_MOVE_ROWS | Select and move by row indices | FILE_OPS | file-ops move rows |
| FILE_OPS_MOVE_GROUP_IDS | Select and move by GroupIds | FILE_OPS | file-ops move group-ids |
| FILE_OPS_MOVE_PREFIX | Select and move by prefix | FILE_OPS | file-ops move prefix |
| FILE_OPS_DELETE_ROWS | Select and delete by row indices | FILE_OPS | file-ops delete rows |
| FILE_OPS_DELETE_GROUP_IDS | Select and delete by GroupIds | FILE_OPS | file-ops delete group-ids |
| FILE_OPS_WAIT_MOVE | Wait for move operation completion | FILE_OPS | file-ops wait move |
| FILE_OPS_WAIT_DELETE | Wait for delete operation completion | FILE_OPS | file-ops wait delete |
| FILE_OPS_CONFIRM | Handle delete confirmation dialog | FILE_OPS | file-ops confirm |
| FILE_OPS_VERIFY_DELETED | Verify group was deleted | FILE_OPS | file-ops verify deleted |
| FILE_OPS_VERIFY_ROW_COUNT | Verify row count changed | FILE_OPS | file-ops verify row-count |
| LOGS_GET | Get all LogPanel messages | LOGS | logs get |
| LOGS_TAIL | Get recent N log messages | LOGS | logs tail |
| LOGS_FILTER | Filter logs by severity level | LOGS | logs filter |
| LOGS_SEARCH | Search log messages for text | LOGS | logs search |
| SETTINGS_DIALOG_OPEN | Open SettingsDialog | SETTINGS_DIALOG | settings-dialog open |
| SETTINGS_DIALOG_CLOSE | Close SettingsDialog | SETTINGS_DIALOG | settings-dialog close |
| SETTINGS_DIALOG_INSPECT | Inspect dialog structure | SETTINGS_DIALOG | settings-dialog inspect |
| SETTINGS_DIALOG_STATUS | Check if dialog is open | SETTINGS_DIALOG | settings-dialog status |
| SETTINGS_DIALOG_PATH_GET_ALL | Get all configured paths | SETTINGS_DIALOG | settings-dialog path get-all |
| SETTINGS_DIALOG_PATH_GET_LINE1 | Get Line 1 paths | SETTINGS_DIALOG | settings-dialog path get-line1 |
| SETTINGS_DIALOG_PATH_GET_LINE2 | Get Line 2 paths | SETTINGS_DIALOG | settings-dialog path get-line2 |
| SETTINGS_DIALOG_PATH_GET_OUTPUT | Get output path | SETTINGS_DIALOG | settings-dialog path get-output |
| SETTINGS_DIALOG_PATH_GET_QUARANTINE | Get quarantine path | SETTINGS_DIALOG | settings-dialog path get-quarantine |
| SETTINGS_DIALOG_PATH_SET | Set a path value | SETTINGS_DIALOG | settings-dialog path set |
| SETTINGS_DIALOG_CHECKBOX_GET | Get checkbox state | SETTINGS_DIALOG | settings-dialog checkbox get |
| SETTINGS_DIALOG_CHECKBOX_SET | Set checkbox state | SETTINGS_DIALOG | settings-dialog checkbox set |
| SETTINGS_DIALOG_CHECKBOX_LIST | List all checkboxes | SETTINGS_DIALOG | settings-dialog checkbox list |
| SETTINGS_DIALOG_ACTION_SAVE | Click Save/OK button | SETTINGS_DIALOG | settings-dialog action save |
| SETTINGS_DIALOG_ACTION_APPLY | Click Apply button | SETTINGS_DIALOG | settings-dialog action apply |
| SETTINGS_DIALOG_ACTION_CANCEL | Click Cancel button | SETTINGS_DIALOG | settings-dialog action cancel |
| SETTINGS_DIALOG_ACTION_RESET | Click Reset button | SETTINGS_DIALOG | settings-dialog action reset |
| SETUP_VERIFY_CONFIG | Verify simulator vs ChronoView config | SETUP | setup verify-config |
| SETUP_COMPLETE_FULL | Complete full setup workflow | SETUP | setup complete-full |
| SETUP_OPEN_SETTINGS | Open SettingsDialog from SetupWindow | SETUP | setup open-settings |
| SETUP_CAMERA_STATES | Get camera button states | SETUP | setup camera-states |
| TEST_CONNECTIVITY | Check ChronoView connection | TEST | test connectivity |
| TEST_CAPABILITIES | List available automation capabilities | TEST | test capabilities |
| TEST_DATAGRID | Check DataGrid accessibility | TEST | test datagrid |
| TOOLBAR_START | Click Start button | TOOLBAR | toolbar start |
| TOOLBAR_STOP | Click Stop button | TOOLBAR | toolbar stop |
| TOOLBAR_SETTINGS | Click Settings button | TOOLBAR | toolbar settings |
| TOOLBAR_REFRESH | Click Refresh button | TOOLBAR | toolbar refresh |
| TOOLBAR_MOVE | Click Move button | TOOLBAR | toolbar move |
| TOOLBAR_DELETE | Click Delete button | TOOLBAR | toolbar delete |
| TOOLBAR_LIST | List all toolbar buttons | TOOLBAR | toolbar list |
| TOOLBAR_CLICK | Click button by text | TOOLBAR | toolbar click |
| TOOLBAR_ENABLED | Check if button is enabled | TOOLBAR | toolbar enabled |
| UTILITY_INSPECT_WORKFLOW | Inspect WorkflowPanel structure | UTILITY | inspect workflow |
| UTILITY_INSPECT_LOG | Inspect LogPanel structure | UTILITY | inspect log |
| UTILITY_CONFIG_PATH | Get config file location | UTILITY | config path |
| UTILITY_CONFIG_READ | Read config file contents | UTILITY | config read |
| UTILITY_CONFIG_GET | Get specific config value | UTILITY | config get |
| WINDOWS_MAIN | Find MainWindow | WINDOWS | windows main |
| WINDOWS_SETUP | Find SetupWindow | WINDOWS | windows setup |
| WINDOWS_SETUP_COMPLETE | Complete SetupWindow and go to MainWindow | WINDOWS | windows setup-complete |
| WINDOWS_SETTINGS | Find SettingsDialog | WINDOWS | windows settings |
| WINDOWS_PREVIEW | Find ImagePreviewWindow | WINDOWS | windows preview |
| WINDOWS_ALL | List all ChronoView windows | WINDOWS | windows all |
| WORKFLOW_LAUNCH_GENERAL | Launch General Camera | WORKFLOW | workflow launch-general |
| WORKFLOW_LAUNCH_NIR | Launch NIR 1 Camera | WORKFLOW | workflow launch-nir |
| WORKFLOW_LAUNCH_NIR2 | Launch NIR 2 Camera | WORKFLOW | workflow launch-nir2 |
| WORKFLOW_TOGGLE_FILTERING | Toggle NIR filtering | WORKFLOW | workflow toggle-filtering |
| WORKFLOW_CAMERA_STATES | Get all camera states | WORKFLOW | workflow camera-states |
| WORKFLOW_PATH_GET_LINE1 | Get Line 1 paths | WORKFLOW | workflow path get-line1 |
| WORKFLOW_PATH_GET_LINE2 | Get Line 2 paths | WORKFLOW | workflow path get-line2 |
| WORKFLOW_PATH_GET_ALL | Get all workflow paths | WORKFLOW | workflow path get-all |
| WORKFLOW_PATH_SET_LINE1 | Set Line 1 path | WORKFLOW | workflow path set-line1 |
| WORKFLOW_PATH_SET_LINE2 | Set Line 2 path | WORKFLOW | workflow path set-line2 |
| WORKFLOW_SELECT_TAB | Select a tab in MainWindow | WORKFLOW | workflow select-tab |

---

## APP Category

Application lifecycle commands for launching, stopping, and checking ChronoView process status.

### APP_LAUNCH

Launch ChronoView application using dotnet run. Returns immediately after process start.

```typescript
{
  skill: "APP_LAUNCH",
  desc: "Launch ChronoView application using dotnet run",
  cli: "app launch [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      launched: boolean;
      processId: number;
    };
  }
}
```

### APP_STOP

Stop all ChronoView processes. Returns count of processes terminated.

```typescript
{
  skill: "APP_STOP",
  desc: "Stop all ChronoView processes",
  cli: "app stop [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      stopped: boolean;
      processesStopped: number;
    };
  }
}
```

### APP_RESTART

Restart ChronoView (stop existing processes, then launch new instance).

```typescript
{
  skill: "APP_RESTART",
  desc: "Restart ChronoView (stop + launch)",
  cli: "app restart [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      restarted: boolean;
      processesStopped: number;
      newProcessId: number;
    };
  }
}
```

### APP_STATUS

Check if ChronoView is running and get process details.

```typescript
{
  skill: "APP_STATUS",
  desc: "Check ChronoView running status",
  cli: "app status [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      isRunning: boolean;
      processCount: number;
      processIds: number[];
      mainWindowTitles: string[];
    };
  }
}
```

---

## BATCH Category

Bulk operations for multi-row selection and data export.

### BATCH_SELECT_AND_MOVE

Select multiple rows by indices, GroupIds, or range, then move them.

```typescript
{
  skill: "BATCH_SELECT_AND_MOVE",
  desc: "Select multiple rows and move them",
  cli: "batch select-and-move [--rows <indices>] [--group-ids <ids>] [--start-index <n>] [--count <n>] [--json]",
  params: {
    rows?: number[];      // Specific row indices (comma-separated)
    groupIds?: string[];  // Specific GroupIds (comma-separated)
    startIndex?: number;  // Start index for range selection
    count?: number;       // Count for range selection
    json?: boolean;       // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      selected: number;
      moved: number;
      duration: string;
    };
  }
}
```

### BATCH_SELECT_AND_DELETE

Select multiple rows by indices, GroupIds, or range, then delete them with confirmation handling.

```typescript
{
  skill: "BATCH_SELECT_AND_DELETE",
  desc: "Select multiple rows and delete them",
  cli: "batch select-and-delete [--rows <indices>] [--group-ids <ids>] [--start-index <n>] [--count <n>] [--json]",
  params: {
    rows?: number[];      // Specific row indices (comma-separated)
    groupIds?: string[];  // Specific GroupIds (comma-separated)
    startIndex?: number;  // Start index for range selection
    count?: number;       // Count for range selection
    json?: boolean;       // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      selected: number;
      deleted: number;
      confirmed: boolean;
    };
  }
}
```

### BATCH_EXPORT_ALL

Export all available data from ChronoView (statistics, DataGrid rows, camera states).

```typescript
{
  skill: "BATCH_EXPORT_ALL",
  desc: "Export all available data from ChronoView",
  cli: "batch export-all [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      timestamp: string;
      statistics: Record<string, string>;
      dataGrid: {
        rowCount: number;
        rows: Array<Record<string, string>>;
      };
      cameraStates: Record<string, boolean>;
    };
  }
}
```

---

## CONSOLE_LOGS Category

Console log file reading commands for developer debug logs (not LogPanel UI logs).

### CONSOLE_LOGS_LIST

List available console log files with optional date filtering.

```typescript
{
  skill: "CONSOLE_LOGS_LIST",
  desc: "List available console log files",
  cli: "console-logs list [--date <YYYYMMDD>] [--latest] [--json]",
  params: {
    date?: string;   // Date filter (YYYYMMDD format)
    latest?: boolean; // Use latest date folder
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      source: string;
      logDirectory: string;
      dateFilter?: string;
      count: number;
      files: string[];
    };
  }
}
```

### CONSOLE_LOGS_TAIL

Read recent N lines from console log file.

```typescript
{
  skill: "CONSOLE_LOGS_TAIL",
  desc: "Read recent N lines from console log",
  cli: "console-logs tail [count] [--file <path>] [--latest] [--json]",
  params: {
    count?: number;   // Number of lines to read (default: 20)
    file?: string;    // Specific log file path
    latest?: boolean; // Use latest log file
    json?: boolean;   // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      source: string;
      file: string;
      requested: number;
      returned: number;
      logs: string[];
    };
  }
}
```

### CONSOLE_LOGS_SEARCH

Search console logs for specific text.

```typescript
{
  skill: "CONSOLE_LOGS_SEARCH",
  desc: "Search console logs for text",
  cli: "console-logs search <text> [--file <path>] [--latest] [--max <n>] [--json]",
  params: {
    text: string;     // Search text (required)
    file?: string;    // Specific log file path
    latest?: boolean; // Use latest log file
    max?: number;     // Maximum results (default: 50)
    json?: boolean;   // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      source: string;
      file: string;
      search: string;
      maxResults: number;
      count: number;
      logs: string[];
    };
  }
}
```

---

## DATA_PANEL Category

Data panel commands for reading StatisticsPanel and FileGroupDataGrid data.

### DATA_PANEL_STATS

Read statistics from StatisticsPanel (file counts, matching status).

```typescript
{
  skill: "DATA_PANEL_STATS",
  desc: "Read StatisticsPanel data",
  cli: "stats [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      source: string;
      statistics: Record<string, string>;
    };
  }
}
```

### DATA_PANEL_HEADERS

Get DataGrid column headers.

```typescript
{
  skill: "DATA_PANEL_HEADERS",
  desc: "Get DataGrid column headers",
  cli: "datagrid headers [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      columnCount: number;
      columns: string[];
    };
  }
}
```

### DATA_PANEL_ROWS

Get DataGrid row count (data rows only, excluding header).

```typescript
{
  skill: "DATA_PANEL_ROWS",
  desc: "Get DataGrid row count",
  cli: "datagrid rows [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      rowCount: number;
    };
  }
}
```

### DATA_PANEL_DATA

Extract all DataGrid data (all rows and columns).

```typescript
{
  skill: "DATA_PANEL_DATA",
  desc: "Extract all DataGrid data",
  cli: "datagrid data [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      rowCount: number;
      data: Array<Record<string, string>>;
    };
  }
}
```

### DATA_PANEL_INFO

Get DataGrid summary (column headers and row count).

```typescript
{
  skill: "DATA_PANEL_INFO",
  desc: "Get DataGrid summary (headers + row count)",
  cli: "datagrid info [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      columnCount: number;
      rowCount: number;
      columns: string[];
    };
  }
}
```

### DATA_PANEL_CELL

Get specific cell value by row and column index.

```typescript
{
  skill: "DATA_PANEL_CELL",
  desc: "Get specific cell value",
  cli: "datagrid cell <row> <col> [--json]",
  params: {
    row: number;     // Row index (0-based)
    col: number;     // Column index (0-based)
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      row: number;
      column: number;
      value: string | null;
    };
  }
}
```

### DATA_PANEL_EXPORT

Export all DataGrid data as JSON (always outputs JSON format).

```typescript
{
  skill: "DATA_PANEL_EXPORT",
  desc: "Export DataGrid as JSON",
  cli: "datagrid export",
  params: {},
  returns: {
    success: boolean;
    data: {
      rowCount: number;
      exportedAt: string;
      data: Array<Record<string, string>>;
    };
  }
}
```

---

## FILE_OPS Category

File operations commands for selecting, moving, deleting, and verifying file groups.

### FILE_OPS_SELECT_ROW_INDEX

Select a row by its index.

```typescript
{
  skill: "FILE_OPS_SELECT_ROW_INDEX",
  desc: "Select row by index",
  cli: "file-ops select row-index --row-index <n>",
  params: {
    rowIndex: number;  // Row index to select (0-based)
  },
  returns: {
    success: boolean;
  }
}
```

### FILE_OPS_SELECT_GROUP_ID

Select a row by its GroupId value.

```typescript
{
  skill: "FILE_OPS_SELECT_GROUP_ID",
  desc: "Select row by GroupId",
  cli: "file-ops select group-id --group-id <id>",
  params: {
    groupId: string;  // GroupId value to find and select
  },
  returns: {
    success: boolean;
  }
}
```

### FILE_OPS_SELECT_PREFIX

Select all rows with GroupId starting with the specified prefix.

```typescript
{
  skill: "FILE_OPS_SELECT_PREFIX",
  desc: "Select rows by GroupId prefix",
  cli: "file-ops select prefix --prefix <text>",
  params: {
    prefix: string;  // GroupId prefix to filter (e.g., 'line2_')
  },
  returns: {
    success: boolean;
  }
}
```

### FILE_OPS_SELECT_ALL

Select all rows by clicking the SelectAll checkbox.

```typescript
{
  skill: "FILE_OPS_SELECT_ALL",
  desc: "Select all rows",
  cli: "file-ops select-all",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### FILE_OPS_CLEAR_SELECTION

Clear all row selections.

```typescript
{
  skill: "FILE_OPS_CLEAR_SELECTION",
  desc: "Clear row selection",
  cli: "file-ops clear-selection",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### FILE_OPS_SELECTED

Get indices of currently selected rows.

```typescript
{
  skill: "FILE_OPS_SELECTED",
  desc: "Get selected row indices",
  cli: "file-ops selected [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      count: number;
      selectedRows: number[];
    };
  }
}
```

### FILE_OPS_MOVE_ROWS

Select rows by indices and click Move button.

```typescript
{
  skill: "FILE_OPS_MOVE_ROWS",
  desc: "Select and move by row indices",
  cli: "file-ops move rows --rows <indices>",
  params: {
    rows: number[];  // Row indices to move (comma-separated)
  },
  returns: {
    success: boolean;
  }
}
```

### FILE_OPS_MOVE_GROUP_IDS

Select rows by GroupIds and click Move button.

```typescript
{
  skill: "FILE_OPS_MOVE_GROUP_IDS",
  desc: "Select and move by GroupIds",
  cli: "file-ops move group-ids --group-ids <ids>",
  params: {
    groupIds: string[];  // GroupIds to move (comma-separated)
  },
  returns: {
    success: boolean;
  }
}
```

### FILE_OPS_MOVE_PREFIX

Select rows by GroupId prefix and click Move button.

```typescript
{
  skill: "FILE_OPS_MOVE_PREFIX",
  desc: "Select and move by GroupId prefix",
  cli: "file-ops move prefix --prefix <text>",
  params: {
    prefix: string;  // GroupId prefix to filter (e.g., 'line2_')
  },
  returns: {
    success: boolean;
  }
}
```

### FILE_OPS_DELETE_ROWS

Select rows by indices and click Delete button.

```typescript
{
  skill: "FILE_OPS_DELETE_ROWS",
  desc: "Select and delete by row indices",
  cli: "file-ops delete rows --rows <indices>",
  params: {
    rows: number[];  // Row indices to delete (comma-separated)
  },
  returns: {
    success: boolean;
  }
}
```

### FILE_OPS_DELETE_GROUP_IDS

Select rows by GroupIds and click Delete button.

```typescript
{
  skill: "FILE_OPS_DELETE_GROUP_IDS",
  desc: "Select and delete by GroupIds",
  cli: "file-ops delete group-ids --group-ids <ids>",
  params: {
    groupIds: string[];  // GroupIds to delete (comma-separated)
  },
  returns: {
    success: boolean;
  }
}
```

### FILE_OPS_WAIT_MOVE

Wait for move operation to complete (monitors row count changes).

```typescript
{
  skill: "FILE_OPS_WAIT_MOVE",
  desc: "Wait for move operation completion",
  cli: "file-ops wait move [--timeout <ms>]",
  params: {
    timeout?: number;  // Timeout in milliseconds (default: 30000)
  },
  returns: {
    success: boolean;
  }
}
```

### FILE_OPS_WAIT_DELETE

Wait for delete operation to complete (monitors row count changes).

```typescript
{
  skill: "FILE_OPS_WAIT_DELETE",
  desc: "Wait for delete operation completion",
  cli: "file-ops wait delete [--timeout <ms>]",
  params: {
    timeout?: number;  // Timeout in milliseconds (default: 30000)
  },
  returns: {
    success: boolean;
  }
}
```

### FILE_OPS_CONFIRM

Find and click confirmation dialog button (for delete operations).

```typescript
{
  skill: "FILE_OPS_CONFIRM",
  desc: "Handle delete confirmation dialog",
  cli: "file-ops confirm [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      action: string;
      confirmed: boolean;
    };
  }
}
```

### FILE_OPS_VERIFY_DELETED

Verify that a GroupId no longer exists in the DataGrid.

```typescript
{
  skill: "FILE_OPS_VERIFY_DELETED",
  desc: "Verify group was deleted",
  cli: "file-ops verify deleted <groupId> [--json]",
  params: {
    groupId: string;  // GroupId to verify
    json?: boolean;   // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      groupId: string;
      verified: boolean;
    };
  }
}
```

### FILE_OPS_VERIFY_ROW_COUNT

Wait and verify that row count has changed from original value.

```typescript
{
  skill: "FILE_OPS_VERIFY_ROW_COUNT",
  desc: "Verify row count changed",
  cli: "file-ops verify row-count <originalCount> [--timeout <ms>] [--json]",
  params: {
    originalCount: number;  // Original row count before operation
    timeout?: number;       // Timeout in milliseconds (default: 30000)
    json?: boolean;         // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      originalCount: number;
      currentCount: number;
      changed: boolean;
    };
  }
}
```

---

## LOGS Category

LogPanel commands for reading and filtering log messages from the UI.

### LOGS_GET

Get all log messages from LogPanel.

```typescript
{
  skill: "LOGS_GET",
  desc: "Get all LogPanel messages",
  cli: "logs get [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      source: string;
      count: number;
      logs: Array<{
        Severity: string;
        Time: string;
        Source: string;
        Message: string;
      }>;
    };
  }
}
```

### LOGS_TAIL

Get the most recent N log messages from LogPanel.

```typescript
{
  skill: "LOGS_TAIL",
  desc: "Get recent N log messages",
  cli: "logs tail [count] [--json]",
  params: {
    count?: number;  // Number of messages (default: 10)
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      source: string;
      requested: number;
      returned: number;
      logs: Array<{
        Severity: string;
        Time: string;
        Source: string;
        Message: string;
      }>;
    };
  }
}
```

### LOGS_FILTER

Filter log messages by severity level.

```typescript
{
  skill: "LOGS_FILTER",
  desc: "Filter logs by severity level",
  cli: "logs filter [--level <level>] [--json]",
  params: {
    level?: string;  // Severity level: Debug, Info, Warning, Error (null = All)
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      source: string;
      filter: { level: string | null };
      count: number;
      logs: Array<{
        Severity: string;
        Time: string;
        Source: string;
        Message: string;
      }>;
    };
  }
}
```

### LOGS_SEARCH

Search log messages for specific text.

```typescript
{
  skill: "LOGS_SEARCH",
  desc: "Search log messages for text",
  cli: "logs search <text> [--json]",
  params: {
    text: string;    // Search text (required)
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      source: string;
      search: string;
      count: number;
      logs: Array<{
        Severity: string;
        Time: string;
        Source: string;
        Message: string;
      }>;
    };
  }
}
```

---

## SETTINGS_DIALOG Category

Settings dialog commands for configuration management.

### SETTINGS_DIALOG_OPEN

Open the SettingsDialog by clicking the Settings button.

```typescript
{
  skill: "SETTINGS_DIALOG_OPEN",
  desc: "Open SettingsDialog",
  cli: "settings-dialog open",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### SETTINGS_DIALOG_CLOSE

Close the SettingsDialog by clicking the Cancel button.

```typescript
{
  skill: "SETTINGS_DIALOG_CLOSE",
  desc: "Close SettingsDialog",
  cli: "settings-dialog close",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### SETTINGS_DIALOG_INSPECT

Inspect the SettingsDialog structure (print element tree).

```typescript
{
  skill: "SETTINGS_DIALOG_INSPECT",
  desc: "Inspect dialog structure",
  cli: "settings-dialog inspect",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### SETTINGS_DIALOG_STATUS

Check if SettingsDialog is currently open.

```typescript
{
  skill: "SETTINGS_DIALOG_STATUS",
  desc: "Check if dialog is open",
  cli: "settings-dialog status [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      dialogType: string;
      isOpen: boolean;
    };
  }
}
```

### SETTINGS_DIALOG_PATH_GET_ALL

Get all configured paths (Line 1, Line 2, Output, Quarantine).

```typescript
{
  skill: "SETTINGS_DIALOG_PATH_GET_ALL",
  desc: "Get all configured paths",
  cli: "settings-dialog path get-all [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      line1: Record<string, string>;
      line2: Record<string, string>;
      output: string;
      quarantine: string;
    };
  }
}
```

### SETTINGS_DIALOG_PATH_GET_LINE1

Get Line 1 paths only.

```typescript
{
  skill: "SETTINGS_DIALOG_PATH_GET_LINE1",
  desc: "Get Line 1 paths",
  cli: "settings-dialog path get-line1 [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      line: string;
      count: number;
      paths: Record<string, string>;
    };
  }
}
```

### SETTINGS_DIALOG_PATH_GET_LINE2

Get Line 2 paths only.

```typescript
{
  skill: "SETTINGS_DIALOG_PATH_GET_LINE2",
  desc: "Get Line 2 paths",
  cli: "settings-dialog path get-line2 [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      line: string;
      count: number;
      paths: Record<string, string>;
    };
  }
}
```

### SETTINGS_DIALOG_PATH_GET_OUTPUT

Get output path.

```typescript
{
  skill: "SETTINGS_DIALOG_PATH_GET_OUTPUT",
  desc: "Get output path",
  cli: "settings-dialog path get-output [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      pathType: string;
      path: string;
    };
  }
}
```

### SETTINGS_DIALOG_PATH_GET_QUARANTINE

Get quarantine path.

```typescript
{
  skill: "SETTINGS_DIALOG_PATH_GET_QUARANTINE",
  desc: "Get quarantine path",
  cli: "settings-dialog path get-quarantine [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      pathType: string;
      path: string;
    };
  }
}
```

### SETTINGS_DIALOG_PATH_SET

Set a path value by key.

```typescript
{
  skill: "SETTINGS_DIALOG_PATH_SET",
  desc: "Set a path value",
  cli: "settings-dialog path set <key> <value>",
  params: {
    key: string;   // Path key (e.g., nir1, normal1, nir2, normal2, cam1-6)
    value: string; // Path value to set
  },
  returns: {
    success: boolean;
  }
}
```

### SETTINGS_DIALOG_CHECKBOX_GET

Get checkbox state.

```typescript
{
  skill: "SETTINGS_DIALOG_CHECKBOX_GET",
  desc: "Get checkbox state",
  cli: "settings-dialog checkbox get <name> [--json]",
  params: {
    name: string;   // Checkbox name (e.g., use_folder_suffix, use_disk_cache)
    json?: boolean; // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      checkbox: string;
      isChecked: boolean;
    };
  }
}
```

### SETTINGS_DIALOG_CHECKBOX_SET

Set checkbox state.

```typescript
{
  skill: "SETTINGS_DIALOG_CHECKBOX_SET",
  desc: "Set checkbox state",
  cli: "settings-dialog checkbox set <name> <value>",
  params: {
    name: string;  // Checkbox name
    value: boolean; // Checkbox value (true/false)
  },
  returns: {
    success: boolean;
  }
}
```

### SETTINGS_DIALOG_CHECKBOX_LIST

List all checkboxes from the Advanced tab.

```typescript
{
  skill: "SETTINGS_DIALOG_CHECKBOX_LIST",
  desc: "List all checkboxes",
  cli: "settings-dialog checkbox list [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      source: string;
      count: number;
      settings: Record<string, boolean>;
    };
  }
}
```

### SETTINGS_DIALOG_ACTION_SAVE

Click Save/OK button (saves and closes dialog).

```typescript
{
  skill: "SETTINGS_DIALOG_ACTION_SAVE",
  desc: "Click Save/OK button",
  cli: "settings-dialog action save",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### SETTINGS_DIALOG_ACTION_APPLY

Click Apply button (applies changes, keeps dialog open).

```typescript
{
  skill: "SETTINGS_DIALOG_ACTION_APPLY",
  desc: "Click Apply button",
  cli: "settings-dialog action apply",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### SETTINGS_DIALOG_ACTION_CANCEL

Click Cancel button (discards changes and closes dialog).

```typescript
{
  skill: "SETTINGS_DIALOG_ACTION_CANCEL",
  desc: "Click Cancel button",
  cli: "settings-dialog action cancel",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### SETTINGS_DIALOG_ACTION_RESET

Click Reset/Defaults button (resets to defaults, keeps dialog open).

```typescript
{
  skill: "SETTINGS_DIALOG_ACTION_RESET",
  desc: "Click Reset button",
  cli: "settings-dialog action reset",
  params: {},
  returns: {
    success: boolean;
  }
}
```

---

## SETUP Category

Setup window commands for SetupWindow control and configuration verification.

### SETUP_VERIFY_CONFIG

Verify simulator configuration matches ChronoView configuration.

```typescript
{
  skill: "SETUP_VERIFY_CONFIG",
  desc: "Verify simulator vs ChronoView config",
  cli: "setup verify-config [--config-path <path>] [--open-settings] [--strict] [--json]",
  params: {
    configPath?: string; // Simulator config path (default: task_helper/data_test/dist/simulator_config.json)
    openSettings?: boolean; // Open SettingsDialog if not open
    strict?: boolean;     // Fail on mismatches
    json?: boolean;       // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      success: boolean;
      error?: string;
      matched: string[];
      mismatches: Array<{
        simulatorKey: string;
        chronoViewKey: string;
        simulatorPath: string;
        chronoViewPath: string;
        reason: string;
      }>;
      missing: string[];
    };
  }
}
```

### SETUP_COMPLETE_FULL

Complete full setup workflow (launch cameras, click Start button, wait for MainWindow).

```typescript
{
  skill: "SETUP_COMPLETE_FULL",
  desc: "Complete full setup workflow",
  cli: "setup complete-full [--verify-config] [--config-path <path>] [--strict] [--json]",
  params: {
    verifyConfig?: boolean; // Run config verification first
    configPath?: string;    // Simulator config path for verification
    strict?: boolean;       // Fail if verification fails
    json?: boolean;         // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      completed: boolean;
      configVerified: boolean;
      mainWindowAppeared: boolean;
    };
  }
}
```

### SETUP_OPEN_SETTINGS

Open SettingsDialog from SetupWindow by clicking the Settings button.

```typescript
{
  skill: "SETUP_OPEN_SETTINGS",
  desc: "Open SettingsDialog from SetupWindow",
  cli: "setup open-settings [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      settingsOpened: boolean;
    };
  }
}
```

### SETUP_CAMERA_STATES

Get camera button states from SetupWindow.

```typescript
{
  skill: "SETUP_CAMERA_STATES",
  desc: "Get camera button states",
  cli: "setup camera-states [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      general: boolean;
      nir1: boolean;
      nir2: boolean;
    };
  }
}
```

---

## TEST Category

Test commands for connectivity and capability checks.

### TEST_CONNECTIVITY

Check if ChronoView is running and accessible.

```typescript
{
  skill: "TEST_CONNECTIVITY",
  desc: "Check ChronoView connection",
  cli: "test connectivity [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      connected: boolean;
      windowFound: boolean;
      appName?: string;
      timestamp: string;
    };
  }
}
```

### TEST_CAPABILITIES

List available automation capabilities (windows, controllers, commands).

```typescript
{
  skill: "TEST_CAPABILITIES",
  desc: "List available automation capabilities",
  cli: "test capabilities [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      windows: Array<{
        type: string;
        title?: string;
        accessible: boolean;
      }>;
      controllers: string[];
      commands: string[];
    };
  }
}
```

### TEST_DATAGRID

Check if DataGrid is accessible and get row/column information.

```typescript
{
  skill: "TEST_DATAGRID",
  desc: "Check DataGrid accessibility",
  cli: "test datagrid [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      accessible: boolean;
      rowCount: number;
      headers: string[];
    };
  }
}
```

---

## TOOLBAR Category

Toolbar commands for clicking main toolbar buttons.

### TOOLBAR_START

Click the Start button to begin monitoring.

```typescript
{
  skill: "TOOLBAR_START",
  desc: "Click Start button",
  cli: "toolbar start",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### TOOLBAR_STOP

Click the Stop button to pause monitoring.

```typescript
{
  skill: "TOOLBAR_STOP",
  desc: "Click Stop button",
  cli: "toolbar stop",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### TOOLBAR_SETTINGS

Click the Settings button to open SettingsDialog.

```typescript
{
  skill: "TOOLBAR_SETTINGS",
  desc: "Click Settings button",
  cli: "toolbar settings",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### TOOLBAR_REFRESH

Click the Refresh button to reload data.

```typescript
{
  skill: "TOOLBAR_REFRESH",
  desc: "Click Refresh button",
  cli: "toolbar refresh",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### TOOLBAR_MOVE

Click the Move button to move selected file groups.

```typescript
{
  skill: "TOOLBAR_MOVE",
  desc: "Click Move button",
  cli: "toolbar move",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### TOOLBAR_DELETE

Click the Delete button to delete selected file groups.

```typescript
{
  skill: "TOOLBAR_DELETE",
  desc: "Click Delete button",
  cli: "toolbar delete",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### TOOLBAR_LIST

List all available toolbar buttons.

```typescript
{
  skill: "TOOLBAR_LIST",
  desc: "List all toolbar buttons",
  cli: "toolbar list [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      count: number;
      buttons: string[];
    };
  }
}
```

### TOOLBAR_CLICK

Click a button by its text content.

```typescript
{
  skill: "TOOLBAR_CLICK",
  desc: "Click button by text",
  cli: "toolbar click <text>",
  params: {
    text: string;  // Button text (e.g., 'Start', 'Stop', 'Settings')
  },
  returns: {
    success: boolean;
  }
}
```

### TOOLBAR_ENABLED

Check if a button is enabled.

```typescript
{
  skill: "TOOLBAR_ENABLED",
  desc: "Check if button is enabled",
  cli: "toolbar enabled <text>",
  params: {
    text: string;  // Button text
  },
  returns: {
    success: boolean;
  }
}
```

---

## UTILITY Category

Utility commands for UI inspection and config file reading.

### UTILITY_INSPECT_WORKFLOW

Inspect WorkflowPanel structure (print element tree for debugging).

```typescript
{
  skill: "UTILITY_INSPECT_WORKFLOW",
  desc: "Inspect WorkflowPanel structure",
  cli: "inspect workflow",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### UTILITY_INSPECT_LOG

Inspect LogPanel structure (print element tree for debugging).

```typescript
{
  skill: "UTILITY_INSPECT_LOG",
  desc: "Inspect LogPanel structure",
  cli: "inspect log",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### UTILITY_CONFIG_PATH

Get the config file location.

```typescript
{
  skill: "UTILITY_CONFIG_PATH",
  desc: "Get config file location",
  cli: "config path",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### UTILITY_CONFIG_READ

Read the entire config file contents.

```typescript
{
  skill: "UTILITY_CONFIG_READ",
  desc: "Read config file contents",
  cli: "config read [--json]",
  params: {
    json?: boolean;  // Pretty-print JSON output
  },
  returns: {
    success: boolean;
  }
}
```

### UTILITY_CONFIG_GET

Get a specific config value by JSON path.

```typescript
{
  skill: "UTILITY_CONFIG_GET",
  desc: "Get specific config value",
  cli: "config get --key <path>",
  params: {
    key: string;  // JSON path (e.g., folderPaths.line1SampleName)
  },
  returns: {
    success: boolean;
  }
}
```

---

## WINDOWS Category

Window detection commands for finding ChronoView windows.

### WINDOWS_MAIN

Find the ChronoView MainWindow.

```typescript
{
  skill: "WINDOWS_MAIN",
  desc: "Find MainWindow",
  cli: "windows main [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      found: boolean;
      windowType: string;
      title?: string;
      className?: string;
      automationId?: string;
    };
  }
}
```

### WINDOWS_SETUP

Find the SetupWindow.

```typescript
{
  skill: "WINDOWS_SETUP",
  desc: "Find SetupWindow",
  cli: "windows setup [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      found: boolean;
      windowType: string;
      title?: string;
      className?: string;
      automationId?: string;
    };
  }
}
```

### WINDOWS_SETUP_COMPLETE

Complete SetupWindow by clicking Start button and waiting for MainWindow.

```typescript
{
  skill: "WINDOWS_SETUP_COMPLETE",
  desc: "Complete SetupWindow and go to MainWindow",
  cli: "windows setup-complete [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      completed: boolean;
      mainWindowFound: boolean;
      mainWindowTitle?: string;
    };
  }
}
```

### WINDOWS_SETTINGS

Find the SettingsDialog.

```typescript
{
  skill: "WINDOWS_SETTINGS",
  desc: "Find SettingsDialog",
  cli: "windows settings [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      found: boolean;
      windowType: string;
      title?: string;
      className?: string;
      automationId?: string;
    };
  }
}
```

### WINDOWS_PREVIEW

Find the ImagePreviewWindow.

```typescript
{
  skill: "WINDOWS_PREVIEW",
  desc: "Find ImagePreviewWindow",
  cli: "windows preview [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data?: {
      found: boolean;
      windowType: string;
      title?: string;
      className?: string;
      automationId?: string;
    };
  }
}
```

### WINDOWS_ALL

List all ChronoView windows.

```typescript
{
  skill: "WINDOWS_ALL",
  desc: "List all ChronoView windows",
  cli: "windows all [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      count: number;
      windows: Array<{
        title: string;
        className?: string;
        automationId?: string;
      }>;
    };
  }
}
```

---

## WORKFLOW Category

Workflow panel commands for camera operations and path management.

### WORKFLOW_LAUNCH_GENERAL

Launch the General Camera program.

```typescript
{
  skill: "WORKFLOW_LAUNCH_GENERAL",
  desc: "Launch General Camera",
  cli: "workflow launch-general",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### WORKFLOW_LAUNCH_NIR

Launch the NIR 1 Camera program.

```typescript
{
  skill: "WORKFLOW_LAUNCH_NIR",
  desc: "Launch NIR 1 Camera",
  cli: "workflow launch-nir",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### WORKFLOW_LAUNCH_NIR2

Launch the NIR 2 Camera program.

```typescript
{
  skill: "WORKFLOW_LAUNCH_NIR2",
  desc: "Launch NIR 2 Camera",
  cli: "workflow launch-nir2",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### WORKFLOW_TOGGLE_FILTERING

Toggle NIR filtering state.

```typescript
{
  skill: "WORKFLOW_TOGGLE_FILTERING",
  desc: "Toggle NIR filtering",
  cli: "workflow toggle-filtering",
  params: {},
  returns: {
    success: boolean;
  }
}
```

### WORKFLOW_CAMERA_STATES

Get all camera button states from WorkflowPanel.

```typescript
{
  skill: "WORKFLOW_CAMERA_STATES",
  desc: "Get all camera states",
  cli: "workflow camera-states [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      source: string;
      count: number;
      states: Record<string, boolean>;
    };
  }
}
```

### WORKFLOW_PATH_GET_LINE1

Get Line 1 paths from WorkflowPanel.

```typescript
{
  skill: "WORKFLOW_PATH_GET_LINE1",
  desc: "Get Line 1 paths",
  cli: "workflow path get-line1 [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      line: string;
      count: number;
      paths: Record<string, string>;
    };
  }
}
```

### WORKFLOW_PATH_GET_LINE2

Get Line 2 paths from WorkflowPanel.

```typescript
{
  skill: "WORKFLOW_PATH_GET_LINE2",
  desc: "Get Line 2 paths",
  cli: "workflow path get-line2 [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      line: string;
      count: number;
      paths: Record<string, string>;
    };
  }
}
```

### WORKFLOW_PATH_GET_ALL

Get all workflow paths (Line 1 and Line 2).

```typescript
{
  skill: "WORKFLOW_PATH_GET_ALL",
  desc: "Get all workflow paths",
  cli: "workflow path get-all [--json]",
  params: {
    json?: boolean;  // Output in JSON format
  },
  returns: {
    success: boolean;
    data: {
      count: number;
      paths: Record<string, Record<string, string>>;
    };
  }
}
```

### WORKFLOW_PATH_SET_LINE1

Set a Line 1 path value by type.

```typescript
{
  skill: "WORKFLOW_PATH_SET_LINE1",
  desc: "Set Line 1 path",
  cli: "workflow path set-line1 <type> <value>",
  params: {
    type: string;  // Path type: samplename, movenir, movealldata
    value: string; // Path value to set
  },
  returns: {
    success: boolean;
  }
}
```

### WORKFLOW_PATH_SET_LINE2

Set a Line 2 path value by type.

```typescript
{
  skill: "WORKFLOW_PATH_SET_LINE2",
  desc: "Set Line 2 path",
  cli: "workflow path set-line2 <type> <value>",
  params: {
    type: string;  // Path type: samplename, movenir, movealldata
    value: string; // Path value to set
  },
  returns: {
    success: boolean;
  }
}
```

### WORKFLOW_SELECT_TAB

Select a tab in the MainWindow TabControl.

```typescript
{
  skill: "WORKFLOW_SELECT_TAB",
  desc: "Select a tab in MainWindow",
  cli: "workflow select-tab <tab>",
  params: {
    tab: string;  // Tab name: 'Line 1', 'Line 2', or 'Combined'
  },
  returns: {
    success: boolean;
  }
}
```

---

## Summary

**Total Skills:** 92

**Category Breakdown:**

| Category | Skills |
|----------|--------|
| APP | 4 |
| BATCH | 3 |
| CONSOLE_LOGS | 3 |
| DATA_PANEL | 7 |
| FILE_OPS | 15 |
| LOGS | 4 |
| SETTINGS_DIALOG | 17 |
| SETUP | 4 |
| TEST | 3 |
| TOOLBAR | 9 |
| UTILITY | 5 |
| WINDOWS | 6 |
| WORKFLOW | 11 |

**Version:** 1.0.0

**Related Documents:**
- [test-executor.md](test-executor.md) - Execution patterns and CLI details
- [test-orchestrator.md](test-orchestrator.md) - Orchestration and delegation
