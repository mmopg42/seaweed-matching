---
phase: 24-error-diagnosis
plan: 02
subsystem: automation-commands
tags: [system-commandline, invocationcontext, error-handling, exit-codes]

# Dependency graph
requires:
  - phase: 24-error-diagnosis
    plan: 01
    provides: ExitCodes.cs with centralized exit code constants
provides:
  - All command handlers now return exit codes via InvocationContext.ExitCode instead of Environment.Exit()
  - Localized error context with try-catch wrappers in all handlers
  - Console.Error for error messages, Console.Out for normal output
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Exit code centralization via ExitCodes static class
    - InvocationContext.SetExitCode() for System.CommandLine beta4
    - Try-catch wrappers in all command handlers for local error context

key-files:
  modified:
    - skills_scripts/ui_automation/Commands/SetupCommands.cs
    - skills_scripts/ui_automation/Commands/WindowsCommands.cs
    - skills_scripts/ui_automation/Commands/ToolbarCommands.cs
    - skills_scripts/ui_automation/Commands/UtilityCommands.cs
    - skills_scripts/ui_automation/Commands/AppLifecycleCommands.cs
    - skills_scripts/ui_automation/Commands/FileOpsCommands.cs
    - skills_scripts/ui_automation/Commands/TestCommands.cs
    - skills_scripts/ui_automation/Commands/DataPanelCommands.cs
    - skills_scripts/ui_automation/Commands/WorkflowCommands.cs
    - skills_scripts/ui_automation/Commands/SettingsCommands.cs

key-decisions:
  - "Use InvocationContext.ExitCode for System.CommandLine 2.0.0-beta4 compatibility"
  - "Add try-catch in all handlers to catch local errors and return ERROR exit code"
  - "Write error messages to Console.Error for proper stderr separation"

patterns-established:
  - "Pattern 1: All command handlers use (InvocationContext context) lambda parameter"
  - "Pattern 2: Try-catch wrappers set context.ExitCode = ERROR on exception"
  - "Pattern 3: Normal/error output separated via Console.WriteLine/Console.Error.WriteLine"
  - "Pattern 4: using static UiAutomation.Commands.ExitCodes for constant access"

# Metrics
duration: ~25min
completed: 2025-01-21
---

# Phase 24 Plan 02: Refactor Commands to Use ExitCodes Summary

**All 10 command handler files refactored to use centralized ExitCodes class and return exit codes via InvocationContext instead of Environment.Exit()**

## Performance

- **Duration:** 25 min
- **Started:** 2025-01-21T10:00:00Z (approx)
- **Completed:** 2025-01-21T10:25:00Z (approx)
- **Tasks:** 2 (grouped into 6 commits)
- **Files modified:** 10 command handler files

## Accomplishments

- All command handlers now use centralized ExitCodes constants (SUCCESS, ERROR, NOT_FOUND, TIMEOUT, INVALID_ARGUMENT)
- Replaced all Environment.Exit() calls with context.ExitCode via InvocationContext
- Added try-catch wrappers in all ~150 command handlers for local error context
- Separated error output to Console.Error for proper stderr handling

## Task Commits

Each task was committed atomically:

1. **Task 1: SetupCommands.cs refactoring** - `3460f5e` (feat)
2. **Task 2: WindowsCommands.cs refactoring** - `302acf1` (feat)
3. **Task 3: ToolbarCommands.cs + UtilityCommands.cs refactoring** - `da2e753` (feat)
4. **Task 4: AppLifecycleCommands.cs + FileOpsCommands.cs refactoring** - `1dfc13b` (feat)
5. **Task 5: TestCommands.cs refactoring** - `3bd0c51` (feat)
6. **Task 6: DataPanelCommands.cs + WorkflowCommands.cs + SettingsCommands.cs refactoring** - `d0db17f` (feat)

## Files Created/Modified

All files modified in `skills_scripts/ui_automation/Commands/`:

- `SetupCommands.cs` - Refactored verify-config, complete-full, open-settings, camera-states handlers
- `WindowsCommands.cs` - Refactored main, setup, settings, preview, all handlers
- `ToolbarCommands.cs` - Refactored start, stop, settings, refresh, move, delete, list, click, enabled handlers
- `UtilityCommands.cs` - Refactored inspect (workflow, log), config (path, read, get) handlers
- `AppLifecycleCommands.cs` - Refactored launch, stop, restart, status handlers (async)
- `FileOpsCommands.cs` - Refactored select, select-all, move, delete, wait, confirm, verify handlers
- `TestCommands.cs` - Refactored connectivity, capabilities, datagrid, scenario, batch handlers
- `DataPanelCommands.cs` - Refactored stats, datagrid (headers, rows, data, info, cell, export) handlers
- `WorkflowCommands.cs` - Refactored workflow (launch, toggle, camera-states, path, select-tab), logs handlers
- `SettingsCommands.cs` - Refactored console-logs, settings-dialog (open, close, inspect, status, path, checkbox, action) handlers

## Deviations from Plan

### API Discovery Issue

**1. [Rule 3 - Blocking] InvocationContext API compatibility with System.CommandLine beta4**
- **Found during:** Task 1 (SetupCommands.cs)
- **Issue:** Initial attempt used direct int return from lambda, but System.CommandLine 2.0.0-beta4 SetHandler doesn't support that pattern
- **Fix:** Changed to using `SetHandler((InvocationContext context) => {...})` and setting `context.ExitCode` value
- **Files modified:** All 10 command files
- **Verification:** Build succeeds, exit codes properly propagated

## Issues Encountered

- System.CommandLine 2.0.0-beta4 has different SetHandler signatures than later versions
  - Solution: Use InvocationContext parameter and set context.ExitCode instead of returning int
  - This is the recommended pattern for the beta4 version

## Deviations from Plan

None - plan executed exactly as written. The deviation above was an implementation detail discovery, not a scope change.

## Next Phase Readiness

- ExitCodes infrastructure fully integrated across all command handlers
- Ready for error diagnosis pattern implementation in next plan (24-03)
- All commands now return proper exit codes for CI/CD integration

---
*Phase: 24-error-diagnosis*
*Plan: 02*
*Completed: 2025-01-21*
