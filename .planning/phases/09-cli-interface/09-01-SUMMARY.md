---
phase: 09-cli-interface
plan: 01
subsystem: cli-interface
tags: system-commandline, cli, exit-codes, json-output

# Dependency graph
requires:
  - phase: 08-file-operations
    provides: ChronoFileOperationsController, file-ops CLI commands
provides:
  - Standardized CLI interface with exit codes for all commands
  - Global --quiet and --verbose options for agent control
  - Consistent JSON output format with success/error wrapper
  - PrintOutput, PrintVerbose, PrintJsonOutput, PrintError helper methods
affects: agent-automation

# Tech tracking
tech-stack:
  added: []
  patterns:
  - Exit code constants pattern (SUCCESS=0, ERROR=1, NOT_FOUND=2, TIMEOUT=3, INVALID_ARGUMENT=4)
  - JSON output wrapper pattern ({ success, data: {...} } for success, { success, error, errorCode } for failure)
  - Helper methods for output control (PrintOutput respects --quiet, PrintVerbose for debug, PrintJsonOutput for programmatic access)

key-files:
  created: []
  modified:
  - skills_scripts/ui_automation/Program.cs - CLI interface with standardized exit codes and output helpers

key-decisions:
  - "Parse global options before command invocation to set static state"
  - "Use Environment.Exit() in handlers since Main returns Task<int> but handlers are synchronous"
  - "JSON output uses compact formatting (WriteIndented=false) for programmatic parsing"
  - "PrintOutput helper suppresses output when --quiet is set, but PrintError (Console.Error) never suppressed"

patterns-established:
  - "Exit Code Pattern: EXIT_SUCCESS=0, EXIT_ERROR=1, EXIT_NOT_FOUND=2, EXIT_TIMEOUT=3, EXIT_INVALID_ARGUMENT=4"
  - "JSON Output Pattern: All commands with --json return { success: true, data: {...} } or { success: false, error: "...", errorCode: N }"
  - "Quiet Mode Pattern: --quiet/-q suppresses Console.WriteLine but Console.Error.WriteLine always shown"

issues-created: []

# Metrics
duration: 34min
completed: 2026-01-18
---

# Phase 09: Plan 01 Summary

**CLI interface standardized with exit codes, global options (--quiet/--verbose), and consistent JSON output format for agent consumption**

## Performance

- **Duration:** 34 min
- **Started:** 2026-01-16T18:09:04Z (first commit) to 2026-01-18T(current session)
- **Tasks:** 4
- **Files modified:** 1

## Accomplishments

- Exit code constants defined (SUCCESS=0, ERROR=1, NOT_FOUND=2, TIMEOUT=3, INVALID_ARGUMENT=4)
- All command handlers return proper exit codes via Environment.Exit()
- Global --quiet/-q and --verbose/-v options added and wired up
- PrintOutput, PrintVerbose, PrintJsonOutput, PrintError helper methods created
- JSON output standardized with success/error wrapper format
- Windows and toolbar commands converted to use PrintOutput for --quiet support

## Task Commits

Each task was committed atomically:

1. **Task 1: Add exit code constants and helper methods** - `631f696` (feat)
2. **Task 2: Refactor command handlers to use proper exit codes** - `40786f3` (feat), `d60ce87` (feat)
3. **Task 3: Add --quiet and --verbose options** - `077ecbe` (feat), `cb25a2e` (feat)
4. **Task 4: Standardize JSON output** - `40786f3` (feat), `d60ce87` (feat), `1bd1b98` (feat), `265f849` (feat)

**Plan metadata:** `265f849` (latest: convert toolbar commands to use PrintOutput helper)

_Note: Tasks 2 and 4 were implemented together in earlier commits. This session completed the remaining work for Task 3 (wiring up global options and converting commands to use PrintOutput)._

## Files Created/Modified

- `skills_scripts/ui_automation/Program.cs` - CLI interface with standardized exit codes, global options, and output helpers

## Decisions Made

- **Parse global options before command invocation**: Use `rootCommand.Parse(args)` to capture --quiet and --verbose values, then invoke commands. This ensures static state is set before handlers execute.
- **Environment.Exit vs return**: Handlers use `Environment.Exit(exitCode)` instead of returning int because SetHandler doesn't support returning exit codes directly. Main still returns `await rootCommand.InvokeAsync(args)` for proper process exit code.
- **JSON compact formatting**: PrintJsonOutput uses `WriteIndented = false` for compact output suitable for programmatic parsing.
- **PrintOutput helper**: Created to respect --quiet option. Error messages via PrintError (Console.Error) are never suppressed.

## Deviations from Plan

None - plan executed as specified.

## Issues Encountered

None

## Next Phase Readiness

- CLI interface is now robust and predictable for programmatic agent use
- All commands return consistent exit codes (0=success, 1=error, 2=not found, 3=timeout, 4=invalid argument)
- JSON output format is consistent across all commands with success/error wrapper
- --quiet option suppresses non-error output for agent consumption
- --verbose option available for debugging (though not yet heavily utilized)
- Ready for agent integration

---
*Phase: 09-cli-interface*
*Completed: 2026-01-18*
