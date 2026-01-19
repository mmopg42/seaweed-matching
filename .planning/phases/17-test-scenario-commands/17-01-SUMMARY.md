---
phase: 17-test-scenario-commands
plan: 01
subsystem: ui-automation-commands
tags: [command-extraction, test-scenario-batch-commands, code-quality, refactoring]
tech-stack: ["C# .NET 10", "System.CommandLine 2.0.0-beta4", "FlaUI.UIA3 5.0.0"]

# Dependency graph
requires:
  - phase: 16-file-ops-commands
    provides: FileOpsCommands pattern and FileOps API for file operations
provides:
  - TestCommands.cs with 9 command groups (test, scenario, batch)
  - Reduced Program.cs from 1126 to 368 lines (758 line reduction)
affects: [future command extraction phases]

# Tech tracking
tech-stack:
  added: []
  patterns: [High-level orchestration command extraction, Multi-controller coordination]

key-files:
  created:
    - skills_scripts/ui_automation/Commands/TestCommands.cs
  modified:
    - skills_scripts/ui_automation/Program.cs

key-decisions:
  - decision: "TestCommands orchestrates multiple controllers for end-to-end workflows"
    rationale: "Test, scenario, and batch commands coordinate multiple controllers (Finder, Toolbar, Workflow, DataReader, Settings, FileOps) to implement high-level automation workflows"
  - decision: "Local helper methods in TestCommands instead of using Program.cs methods"
    rationale: "PrintJsonOutput and PrintOutput are implemented as private static methods in TestCommands to avoid dependencies on Program.cs static methods"

patterns-established:
  - "Pattern 8: High-level orchestration command extraction using multiple controllers"

issues-created: []

# Metrics
duration: 20min
completed: 2026-01-19
---

# Phase 17-01 Summary: Test, Scenario, and Batch Commands Extraction

**Extracted all `test/*`, `scenario/*`, and `batch/*` commands from Program.cs to TestCommands handler module, achieving 758-line reduction**

## Performance

- **Duration:** 20 min
- **Started:** 2026-01-19
- **Completed:** 2026-01-19
- **Tasks:** 3
- **Files modified:** 2

## Accomplishments

- TestCommands class created with all 9 high-level orchestration commands
- Commands coordinate multiple controllers for end-to-end workflows
- JSON output support maintained for all commands
- Program.cs reduced from 1126 to 368 lines (758 line reduction)
- **Total reduction across Phase 11-17: 3,243 lines (90% reduction from original 3,604 lines)**

## Task Commits

Each task was committed atomically:

1. **Task 1: Create TestCommands class shell** - `50a8717` (feat)
2. **Task 2: Extract all test, scenario, and batch commands to TestCommands** - `de9ba04` (feat)
3. **Task 3: Register TestCommands in Program.cs and remove extracted commands** - `366fa94` (feat)

## Files Created/Modified

- `skills_scripts/ui_automation/Commands/TestCommands.cs` (844 lines) - High-level orchestration command handler
  - test connectivity: Check if ChronoView is running
  - test capabilities: List available automation features
  - test datagrid: Verify DataGrid accessibility
  - scenario start-monitoring: Complete monitoring startup workflow
  - scenario configure-paths: Complete path configuration workflow
  - scenario move-groups: Complete file group move workflow
  - batch select-and-move: Multi-row selection and move
  - batch select-and-delete: Multi-row selection and delete
  - batch export-all: Export all ChronoView data
- `skills_scripts/ui_automation/Program.cs` (-760 lines, +2 lines for registration)
  - Removed test command definition (~180 lines)
  - Removed scenario command with 3 subcommands (~290 lines)
  - Removed batch command with 3 subcommands (~290 lines)
  - Added TestCommands registration via CommandRegistry

## Commands Migrated

| Command Group | Subcommands | Controllers Used | Lines of Code |
|---------------|-------------|------------------|---------------|
| test | connectivity, capabilities, datagrid | UiAuto, Finder, DataReader | ~180 |
| scenario | start-monitoring, configure-paths, move-groups | UiAuto, Finder, Toolbar, DataReader, Settings, FileOps | ~290 |
| batch | select-and-move, select-and-delete, export-all | UiAuto, DataReader, Workflow, FileOps | ~290 |

## Verification Commands Used

```bash
# Build verification
dotnet build skills_scripts/ui_automation/ui_automation.csproj

# Help output verification
./bin/Debug/net10.0-windows/ui_automation.exe test --help
./bin/Debug/net10.0-windows/ui_automation.exe scenario --help
./bin/Debug/net10.0-windows/ui_automation.exe batch --help
./bin/Debug/net10.0-windows/ui_automation.exe --help

# Line count verification
powershell -Command "(Get-Content 'skills_scripts\ui_automation\Program.cs').Count"
```

## Code Quality Metrics

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Program.cs lines | 1,126 | 368 | -758 (-67%) |
| Number of handlers | 7 | 8 (+TestCommands) | +14% |
| Commands in handlers | 48 | 57 | +19% |
| Files in Commands/ | 7 | 8 | +14% |
| **Total reduction** | **3,604** | **368** | **-3,236 (-90%)** |

## Decisions Made

1. **Multi-controller coordination**: TestCommands uses multiple controller APIs (UiAuto, Finder, Toolbar, Workflow, DataReader, Settings, FileOps) to implement high-level orchestration workflows. This is appropriate for test, scenario, and batch commands which coordinate multiple aspects of ChronoView automation.

2. **Local helper methods**: PrintJsonOutput and PrintOutput are implemented as private static methods in TestCommands to avoid dependencies on Program.cs static methods, keeping the handler self-contained.

3. **Exit code constants**: TestCommands defines its own exit code constants (SUCCESS, ERROR, NOT_FOUND, TIMEOUT, INVALID_ARGUMENT) matching the values in Program.cs for consistency.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None - all tasks completed without issues.

## Overall Progress: Phase 11-17 Command Extraction

| Phase | Commands Extracted | Line Reduction | Handler Created |
|-------|-------------------|-----------------|-----------------|
| 11-02 | LegacyCommands (detect, list, find, click) | -182 | LegacyCommands |
| 12-01 | WindowsCommands (windows subcommands) | -375 | WindowsCommands |
| 13-01 | ToolbarCommands (toolbar subcommands) | -183 | ToolbarCommands |
| 14-01 | DataPanelCommands (stats, datagrid) | -394 | DataPanelCommands |
| 15-01 | WorkflowCommands (workflow, logs) | -432 | WorkflowCommands |
| 15-02 | SettingsCommands (settings-dialog, console-logs) | -639 | SettingsCommands |
| 16-01 | FileOpsCommands (file-ops subcommands) | -280 | FileOpsCommands |
| 17-01 | TestCommands (test, scenario, batch) | -758 | TestCommands |
| **Total** | **57 commands in 8 handlers** | **-3,243** | **8 handlers** |

**Original Program.cs:** 3,604 lines
**Current Program.cs:** 368 lines
**Reduction:** 90%

## Next Phase Readiness

All major command groups have been extracted. The Program.cs is now at 368 lines, well below the target of 500 lines. The remaining code in Program.cs consists of:

1. Global options setup (--quiet, --verbose, --json)
2. inspect command (UI element structure inspection)
3. config command (Config file direct reading)
4. CommandRegistry setup and handler registrations
5. Main method orchestration

The command extraction pattern has been successfully applied across all major command groups. The CLI tool now has a clean modular structure with each command group in its own handler class.

## Lessons Learned

1. **High-level orchestration**: TestCommands demonstrates how to coordinate multiple controllers for end-to-end workflows. Each command may use 2-3 different controllers to complete its operation.

2. **Exit code consistency**: Using matching exit code constants across all handlers ensures consistent behavior for agent consumption of CLI output.

3. **Self-contained handlers**: Each handler should have its own helper methods (PrintJsonOutput, PrintOutput) to avoid dependencies on Program.cs, making the handlers more portable and testable.

---
*Phase: 17-test-scenario-commands*
*Completed: 2026-01-19*
