---
phase: 20-app-lifecycle-commands
plan: 01
subsystem: testing
tags: [process-management, cli, automation, dotnet]

# Dependency graph
requires:
  - phase: 19-main-cleanup
    provides: CommandRegistry architecture, ICommandHandler interface
provides:
  - AppLifecycleCommands handler with launch/stop/restart/status commands
  - Foundation for autonomous test-executor agents to manage ChronoView process
affects: [20-02, 20-03, 21-test-executor, 22-test-scenarios]

# Tech tracking
tech-stack:
  added: [] # No new external dependencies - using System.Diagnostics.Process
  patterns:
    - ICommandHandler implementation with "app" command group
    - Non-blocking process launch using Task.Run
    - JSON output via JsonSerializer.Serialize with WriteIndented=false
    - Exit codes: 0=success, 1=error, 2=not_found, 3=timeout

key-files:
  created:
    - skills_scripts/ui_automation/Commands/AppLifecycleCommands.cs
  modified:
    - skills_scripts/ui_automation/Program.cs

key-decisions:
  - "Use Process.GetProcessesByName without .exe extension (returns process name only)"
  - "Non-blocking launch: return immediately after Process.Start(), don't WaitForExit()"
  - "Restart sequence: stop all processes, 500ms delay for cleanup, then launch"

patterns-established:
  - "Process management pattern: GetProcessesByName -> Kill -> WaitForExit(5000)"
  - "Error handling: InvalidOperationException for already-exited processes"
  - "Status output: isRunning, processCount, processIds, mainWindowTitles"

issues-created: []

# Metrics
duration: 15min
completed: 2026-01-20
---

# Phase 20-01: App Lifecycle Commands Summary

**AppLifecycleCommands handler implementing ChronoView process management with 4 commands (launch/stop/restart/status) using System.Diagnostics.Process**

## Performance

- **Duration:** 15 min
- **Started:** 2026-01-20
- **Completed:** 2026-01-20
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- AppLifecycleCommands class created with ICommandHandler implementation
- Four app lifecycle commands: launch (non-blocking), stop (kill all), restart (stop+launch), status (check running)
- All commands support --json output for programmatic consumption
- Handler registered in Program.cs, increasing count from 10 to 11

## Task Commits

Each task was committed atomically:

1. **Task 1: Create AppLifecycleCommands handler class** - `9deb50d` (feat)
2. **Task 2: Register AppLifecycleCommands in Program.cs** - `68548be` (feat)

**Plan metadata:** N/A (plan executed in single session)

## Files Created/Modified

- `skills_scripts/ui_automation/Commands/AppLifecycleCommands.cs` - New command handler with 4 subcommands for ChronoView process management
- `skills_scripts/ui_automation/Program.cs` - Added AppLifecycleCommands registration (handler count: 10 -> 11)

## Decisions Made

- **Process name without .exe**: Process.GetProcessesByName("ChronoView") requires name without extension (documented in plan)
- **Non-blocking launch**: Use Task.Run wrapper with Process.Start() that returns immediately without WaitForExit() - enables test agents to continue execution
- **Restart delay**: 500ms Task.Delay after stop to allow process cleanup before launch

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None - build succeeded with 0 errors, 0 warnings on first attempt.

## Verification Results

- dotnet build ui_automation.csproj: 0 errors, 0 warnings
- All 4 commands registered: `ui_automation.exe app --help` shows launch/stop/restart/status
- Handler count in CommandRegistry: 11 (verified in Program.cs)
- Code follows existing patterns: ICommandHandler, exit codes, JSON output
- Commands tested:
  - `app status` when not running: Exit code 2, reports "not running"
  - `app status --json`: Returns structured JSON with isRunning=false
  - `app stop` when none running: Exit code 2, reports "no processes found"

## Next Phase Readiness

- AppLifecycleCommands complete and functional
- Ready for 20-02 (next plan in phase 20)
- Test-executor agents can now launch, stop, restart, and check status of ChronoView autonomously

---
*Phase: 20-app-lifecycle-commands*
*Plan: 01*
*Completed: 2026-01-20*
