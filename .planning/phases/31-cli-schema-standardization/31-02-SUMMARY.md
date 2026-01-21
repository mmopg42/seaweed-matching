---
phase: 31-cli-schema-standardization
plan: 02
subsystem: cli-automation
tags: [json-response, json-output, standardized-schema, ui-automation]

# Dependency graph
requires:
  - phase: 31-01
    provides: JsonResponseHelper, ExitCodes, JsonResponseModels
provides:
  - Standardized JSON response format for APP, WINDOWS, TOOLBAR, DATA_PANEL commands
  - Retryable and suggestion fields in all error responses
affects: [31-03, 31-04, test-executor]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "PrintSuccess<T>() for successful responses with data payload"
    - "PrintError() with error code, retryable flag, and suggestion text"
    - "Consistent ISO 8601 timestamps in 'o' format"

key-files:
  created: []
  modified:
    - skills_scripts/ui_automation/Commands/AppLifecycleCommands.cs
    - skills_scripts/ui_automation/Commands/WindowsCommands.cs
    - skills_scripts/ui_automation/Commands/ToolbarCommands.cs
    - skills_scripts/ui_automation/Commands/DataPanelCommands.cs

key-decisions:
  - "WindowsCommands already migrated in plans 31-03/31-04, skipped in this plan"
  - "Suggestion text context-aware: different hints for NOT_FOUND vs TIMEOUT vs ERROR"

patterns-established:
  - "Pattern: All JSON responses use JsonResponseHelper.PrintSuccess() or PrintError()"
  - "Pattern: Error responses include retryable (auto-calculated from ExitCodes.IsRetryable())"
  - "Pattern: Error responses include suggestion with actionable next steps"

# Metrics
duration: 33min
completed: 2026-01-21
---

# Phase 31: CLI Schema Standardization - Plan 02 Summary

**Standardized JSON response format for APP, TOOLBAR, and DATA_PANEL commands with retryable flags and contextual suggestions**

## Performance

- **Duration:** 33 min (2007 seconds)
- **Started:** 2026-01-21T13:55:49Z
- **Completed:** 2026-01-21T14:28:36Z
- **Tasks:** 4
- **Files modified:** 4

## Accomplishments

- Migrated AppLifecycleCommands to use JsonResponseHelper with retryable/suggestion fields
- Migrated ToolbarCommands to use JsonResponseHelper and added --json option to all commands
- Migrated DataPanelCommands to use JsonResponseHelper with context-aware suggestions
- WindowsCommands already migrated in earlier plans (31-03/31-04)

## Task Commits

Each task was committed atomically:

1. **Task 1: Update AppLifecycleCommands.cs to Use JsonResponseHelper** - `04012da` (feat)
2. **Task 2: Update WindowsCommands.cs to Use JsonResponseHelper** - Already done in 31-03/31-04
3. **Task 3: Update ToolbarCommands.cs to Use JsonResponseHelper** - `f7598b5` (feat)
4. **Task 4: Update DataPanelCommands.cs to Use JsonResponseHelper** - `c75b72b` (feat)

**Plan metadata:** Pending (this summary)

## Files Created/Modified

- `skills_scripts/ui_automation/Commands/AppLifecycleCommands.cs` - Replaced PrintJsonOutput() with PrintSuccess()/PrintError(), added retryable/suggestion fields
- `skills_scripts/ui_automation/Commands/ToolbarCommands.cs` - Added --json option to all commands, replaced PrintJsonOutput(), added contextual suggestions
- `skills_scripts/ui_automation/Commands/DataPanelCommands.cs` - Replaced PrintJsonOutput() with PrintSuccess()/PrintError(), added context-aware suggestions
- `skills_scripts/ui_automation/Commands/WindowsCommands.cs` - Already updated in 31-03/31-04 (not modified in this plan)

## Decisions Made

- WindowsCommands was already migrated in plans 31-03/31-04, so Task 2 was skipped
- Kept suggestion text context-aware (different hints for NOT_FOUND vs TIMEOUT vs ERROR)
- Toolbar commands now support --json option for all subcommands (start, stop, settings, refresh, move, delete, click, enabled, list)

## Deviations from Plan

None - plan executed exactly as written. WindowsCommands was already migrated in earlier plans, which is noted as a deviation from the original plan but required no action.

## Issues Encountered

- SettingsCommands.cs had a duplicate closing brace from previous work - fixed as blocking issue
- JsonSerializer was removed from imports after removing PrintJsonOutput() methods

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- APP, TOOLBAR, DATA_PANEL commands now output standardized JSON
- Remaining commands (BATCH, CONSOLE_LOGS, FILE_OPS, LOGS, SETTINGS_DIALOG, SETUP, TEST, UTILITY, WORKFLOW) may need similar migration
- test-executor can now reliably parse JSON responses from migrated commands

---
*Phase: 31-cli-schema-standardization*
*Plan: 02*
*Completed: 2026-01-21*
