---
phase: 31-cli-schema-standardization
plan: 03
subsystem: cli
tags: [json-response-standardization, cli-schema, workflow-commands, logs-commands, settings-commands, console-logs-commands, file-ops-commands]

# Dependency graph
requires:
  - phase: 31-01
    provides: JsonResponseHelper with PrintSuccess/PrintError, ExitCodes with IsRetryable()
provides:
  - WorkflowCommands.cs using JsonResponseHelper for WORKFLOW and LOGS commands
  - SettingsCommands.cs using JsonResponseHelper for SETTINGS_DIALOG and CONSOLE_LOGS commands
  - FileOpsCommands.cs using JsonResponseHelper for FILE_OPS commands
  - All error responses include retryable field and helpful suggestions
affects: [31-04] # TestCommands (already completed via auto-commit)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Standardized JSON response format for WORKFLOW, LOGS, SETTINGS_DIALOG, CONSOLE_LOGS, FILE_OPS commands"
    - "Error responses with retryable boolean and suggestion string for common failures"
    - "All JSON output uses JsonResponseHelper.PrintSuccess/PrintError methods"

key-files:
  created: []
  modified:
    - skills_scripts/ui_automation/Commands/WorkflowCommands.cs
    - skills_scripts/ui_automation/Commands/SettingsCommands.cs
    - skills_scripts/ui_automation/Commands/FileOpsCommands.cs
    - skills_scripts/ui_automation/Commands/DataPanelCommands.cs (blocking fix)

key-decisions:
  - "Keep all local PrintJsonOutput methods removed after migration to JsonResponseHelper"
  - "Error suggestions context-specific: LogPanel accessibility, file availability, dialog state"

patterns-established:
  - "Pattern 1: Success responses use PrintSuccess() with anonymous object data"
  - "Pattern 2: Error responses use PrintError() with message, exit code, and suggestion"
  - "Pattern 3: Retryable status automatically derived from ExitCodes.IsRetryable()"

issues-created: []

# Metrics
duration: 15min
completed: 2026-01-21
---

# Phase 31 Plan 3: Workflow, Settings, and FileOps Commands Migration Summary

**Migrated WORKFLOW, LOGS, SETTINGS_DIALOG, CONSOLE_LOGS, and FILE_OPS command handlers to standardized JSON response format with retryable and suggestion fields**

## Performance

- **Duration:** 15 min
- **Started:** 2026-01-21T23:15:00Z
- **Completed:** 2026-01-21T23:30:00Z
- **Tasks:** 3
- **Files modified:** 3 (plus 1 blocking fix)

## Accomplishments

- WorkflowCommands.cs migrated to JsonResponseHelper (WORKFLOW and LOGS commands)
- SettingsCommands.cs migrated to JsonResponseHelper (SETTINGS_DIALOG and CONSOLE_LOGS commands)
- FileOpsCommands.cs migrated to JsonResponseHelper (FILE_OPS commands)
- All error responses now include retryable field and helpful suggestions
- Removed all local PrintJsonOutput methods from migrated files

## Task Commits

Each task was committed atomically:

1. **Task 1: Migrate WorkflowCommands to JsonResponseHelper** - `3b9e148` (feat)
2. **Task 2: Migrate SettingsCommands to JsonResponseHelper** - `a4db1cf` (feat - part of 31-04 auto-commit)
3. **Task 3: Migrate FileOpsCommands to JsonResponseHelper** - `fe01d2a` (feat)

**Blocking fix:** `658c9d4` (fix) - Rule 3 blocking issue

**Plan metadata:** Pending

## Files Created/Modified

- `skills_scripts/ui_automation/Commands/WorkflowCommands.cs` - Migrated WORKFLOW and LOGS commands to JsonResponseHelper
- `skills_scripts/ui_automation/Commands/SettingsCommands.cs` - Migrated SETTINGS_DIALOG and CONSOLE_LOGS commands to JsonResponseHelper
- `skills_scripts/ui_automation/Commands/FileOpsCommands.cs` - Migrated FILE_OPS commands to JsonResponseHelper
- `skills_scripts/ui_automation/Commands/DataPanelCommands.cs` - Added missing System.Text.Json using statement (blocking fix)

## Decisions Made

- Keep all local PrintJsonOutput methods removed after migration to JsonResponseHelper
- Error suggestions are context-specific to command type (LogPanel accessibility, file availability, dialog state)
- All LOGS commands error handling now includes suggestions for common failures

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added missing System.Text.Json using to DataPanelCommands**

- **Found during:** Build verification after Task 3
- **Issue:** DataPanelCommands.cs had JsonSerializer usage without System.Text.Json using statement, causing build error
- **Fix:** Added `using System.Text.Json;` to DataPanelCommands.cs
- **Files modified:** skills_scripts/ui_automation/Commands/DataPanelCommands.cs
- **Verification:** dotnet build succeeds with no errors
- **Committed in:** 658c9d4 (blocking fix commit)

**Note:** SettingsCommands.cs migration was included in 31-04 auto-commit by linter

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Auto-fix was necessary for build to succeed. No scope creep. The change adds a missing using statement required for compilation.

## Issues Encountered

- SettingsCommands.cs changes were auto-committed by linter as part of 31-04 (TestCommands migration)
- Build errors in ToolbarCommands.cs and DataPanelCommands.cs were auto-fixed by removing unused PrintJsonOutput methods and adding missing using statements

## Next Phase Readiness

- All major command categories now use standardized JSON response format
- Remaining files with local PrintJsonOutput: SetupCommands.cs, UtilityCommands.cs
- Wave 2 plans (31-02, 31-04) will complete remaining migrations

---
*Phase: 31-cli-schema-standardization*
*Completed: 2026-01-21*
