---
phase: 24-error-diagnosis
plan: 01
subsystem: error-handling
tags: [System.CommandLine, exception-handling, stderr, exit-codes, FlaUI]

# Dependency graph
requires: []
provides:
  - ExitCodes.cs with centralized exit code constants (SUCCESS, ERROR, NOT_FOUND, TIMEOUT, INVALID_ARGUMENT)
  - Program.cs with centralized exception handler wrapping InvokeAsync
  - Structured error messages to stderr with exception type, message, and command context
  - Stack trace output when --verbose flag is used
affects: [24-02]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Centralized exception handler at CLI entry point
    - Return-based exit codes from command handlers
    - Stderr for error output (vs stdout for normal output)
    - Namespace-based exception type detection

key-files:
  created:
    - skills_scripts/ui_automation/Commands/ExitCodes.cs
  modified:
    - skills_scripts/ui_automation/Program.cs

key-decisions:
  - Use FlaUI namespace detection instead of specific exception type (FlaUI.Core.Exceptions.AutomationException does not exist in 5.0.0)
  - Simplified catch block to single Exception handler with type discrimination
  - Removed CommandLineException catch (does not exist in System.CommandLine 2.0.0-beta4)

patterns-established:
  - "Pattern 1: Centralized Error Handler - Wrap InvokeAsync in try-catch at Program.cs"
  - "Pattern 2: ExitCodes Static Class - Single source of truth for exit code constants"
  - "Pattern 3: Structured Error Messages - [Type] ExceptionName: Message + Command context"

# Metrics
duration: 7min
completed: 2026-01-21
---

# Phase 24: Plan 01 - Centralized Error Handler Summary

**ExitCodes.cs with centralized constants and Program.cs exception handler providing detailed error diagnostics for CLI commands**

## Performance

- **Duration:** 7 min
- **Started:** 2026-01-21T01:19:21Z
- **Completed:** 2026-01-21T01:26:12Z
- **Tasks:** 2
- **Files modified:** 2 (1 created, 1 modified)

## Accomplishments

- Created `ExitCodes.cs` with 5 centralized exit code constants replacing scattered hardcoded values
- Added try-catch wrapper around `InvokeAsync()` in Program.cs for unhandled exception capture
- Implemented structured error messages to stderr with exception type, message, and command arguments
- Added verbose mode support (--verbose) for stack trace output during debugging
- FlaUI exceptions detected by namespace for UI-specific error messages

## Task Commits

Each task was committed atomically:

1. **Task 1: Create ExitCodes.cs with centralized exit code constants** - `616bbab` (feat)
2. **Task 2: Add centralized exception handler in Program.cs** - `d13e561` (feat)

## Files Created/Modified

- `skills_scripts/ui_automation/Commands/ExitCodes.cs` - Centralized exit code constants (SUCCESS=0, ERROR=1, NOT_FOUND=2, TIMEOUT=3, INVALID_ARGUMENT=4)
- `skills_scripts/ui_automation/Program.cs` - Try-catch wrapper around InvokeAsync, WriteError helper method

## Decisions Made

**FlaUI Exception Detection:** Original plan specified catching `FlaUI.Core.Exceptions.AutomationException` but this type does not exist in FlaUI.UIA3 5.0.0. Used namespace-based detection (`ex.GetType().Namespace?.Contains("FlaUI")`) instead to identify UI automation errors.

**CommandLineException Removal:** Plan specified catching `System.CommandLine.Parsing.CommandLineException` but this type does not exist in the beta4 version. System.CommandLine handles parse errors internally before our try-catch, so this catch was unnecessary.

**Single Catch Block:** Simplified to single Exception catch with type discrimination rather than multiple specific catches. This maintains code clarity while still providing appropriate error messages.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] FlaUI.Core.Exceptions.AutomationException does not exist**
- **Found during:** Task 2 (Program.cs exception handler implementation)
- **Issue:** Plan specified `catch (AutomationException ex)` but FlaUI.UIA3 5.0.0 doesn't have this base exception class
- **Fix:** Changed to namespace-based detection (`ex.GetType().Namespace?.Contains("FlaUI")`)
- **Files modified:** skills_scripts/ui_automation/Program.cs
- **Verification:** Build succeeds, error messages show "[UI Automation Error]" for FlaUI exceptions

**2. [Rule 1 - Bug] System.CommandLine.Parsing.CommandLineException does not exist**
- **Found during:** Task 2 (Program.cs exception handler implementation)
- **Issue:** Plan specified `catch (System.CommandLine.Parsing.CommandLineException ex)` but this type doesn't exist in beta4
- **Fix:** Removed CommandLineException catch - System.CommandLine handles parse errors before InvokeAsync
- **Files modified:** skills_scripts/ui_automation/Program.cs
- **Verification:** Invalid commands still show proper error messages via System.CommandLine's built-in handling

---

**Total deviations:** 2 auto-fixed (2 bugs - non-existent exception types in plan)
**Impact on plan:** Both fixes necessary for code to compile. Functionality equivalent to plan intent.

## Issues Encountered

**Build Error - AutomationException not found:** Initial implementation failed to compile because `FlaUI.Core.Exceptions.AutomationException` doesn't exist in FlaUI.UIA3 5.0.0. Fixed by using namespace-based detection.

**Build Error - CommandLineException not found:** `System.CommandLine.Parsing.CommandLineException` doesn't exist in the beta4 version. Fixed by removing this catch block since parse errors are handled internally by System.CommandLine.

## Error Message Format

The centralized handler produces structured error messages:

```
[Error] ExceptionType: Message
Command: arg1 arg2 arg3
```

For FlaUI exceptions:
```
[UI Automation Error] PropertyNotFoundException: AutomationId not supported
Command: windows main --json
```

With --verbose flag:
```
[Error] ExceptionType: Message
Command: arg1 arg2 arg3
Stack trace: at UiAutomation.Commands.WindowsCommands...
```

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- ExitCodes.cs provides centralized constants for use in Plan 24-02 (command handler refactoring)
- Program.cs exception handler ready to catch exceptions from command handlers after they switch to return-based exit codes
- Known limitation: Command handlers still use `Environment.Exit()` which bypasses the new exception handler - addressed in Plan 24-02

---
*Phase: 24-error-diagnosis*
*Completed: 2026-01-21*
