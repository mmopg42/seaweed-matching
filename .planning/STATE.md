# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-21)

**Core value:** UI 요소 식별 및 조작 — ChronoView의 모든 UI 요소를 안정적으로 식별하고 조작
**Current focus:** Milestone v1.4 - Test Agent Architecture & Reliability

## Current Position

**Milestone:** v1.4 Test Agent Architecture & Reliability
**Phase:** 25 (Simulator Status)
**Plan:** 02 (Status Documentation)
**Status:** Complete

Progress: ██████████ 100% (Phase 25: 2/2 plans complete)

## Plan 24-01 Summary

**Timeline:** 7 min (2026-01-21)
**Deliverables:**
- ExitCodes.cs with 5 centralized exit code constants
- Program.cs with try-catch wrapper around InvokeAsync
- Structured error messages to stderr with exception type and command context
- Verbose mode (--verbose) for stack trace output

**Commits:** 2 atomic commits

**Features:**
- SUCCESS=0, ERROR=1, NOT_FOUND=2, TIMEOUT=3, INVALID_ARGUMENT=4 exit codes
- FlaUI exception detection by namespace (AutomationException base class doesn't exist in 5.0.0)
- Error format: "[Error] ExceptionType: Message" + "Command: args"
- Stack traces shown when --verbose flag is used

## Plan 24-02 Summary

**Timeline:** 25 min (2026-01-21)
**Deliverables:**
- All 10 command handler files refactored to use ExitCodes via InvocationContext.ExitCode
- Try-catch wrappers in all ~150 command handlers for local error context
- Console.Error for error messages, Console.Out for normal output

**Commits:** 6 atomic commits

**Files Modified:**
- SetupCommands.cs - verify-config, complete-full, open-settings, camera-states handlers
- WindowsCommands.cs - main, setup, settings, preview, all handlers
- ToolbarCommands.cs - all 9 toolbar button handlers
- UtilityCommands.cs - inspect and config command handlers
- AppLifecycleCommands.cs - launch, stop, restart, status handlers (async)
- FileOpsCommands.cs - select, move, delete, wait, confirm, verify handlers
- TestCommands.cs - test, scenario, batch command handlers
- DataPanelCommands.cs - stats, datagrid command handlers
- WorkflowCommands.cs - workflow and logs command handlers
- SettingsCommands.cs - console-logs and settings-dialog handlers

## Plan 25-01 Summary

**Timeline:** 8 min (2026-01-21)
**Deliverables:**
- State file persistence (simulation_state.json) for cross-process status queries
- --status CLI argument for querying simulation status
- State updates integrated into run_line1_simulation and run_line2_simulation methods
- State file cleanup on simulation start to prevent stale status

**Commits:** 3 atomic commits

**Features:**
- _get_state_file_path() - Returns path to simulation_state.json
- _update_state() - Writes simulation state to JSON file
- _load_state() - Reads simulation state from JSON file
- get_status() - Returns current status (idle if no state file)
- --status CLI flag - Returns JSON with status, progress, items_created, simulation_id, last_activity

## Plan 25-02 Summary

**Timeline:** 2 min (2026-01-21)
**Deliverables:**
- test-executor.md updated with --status command documentation
- State file location documented (simulation_state.json)
- Status response JSON format documented with 5 fields
- Pattern 6: Status-Based Simulation Wait with bash polling
- Windows PowerShell polling example for cross-platform compatibility
- Pattern 2 updated to use status polling instead of blind wait

**Commits:** 3 atomic commits

**Features:**
- --status command example in Test Data Generator section
- State file persistence documentation (Windows script and EXE paths)
- Status JSON format: status, progress, items_created, simulation_id, last_activity
- Bash polling pattern with background execution (&)
- PowerShell polling pattern with Start-Process and Select-String

## Plan 23-01 Summary

**Timeline:** 1 day (2026-01-20)
**Deliverables:**
- ChronoFileOperationsController.cs delay optimization (200ms -> 100ms)
- test-executor.md updated with "Execute first, verify on failure" philosophy
- Parallel execution groups documented (PERF-01)

**Commits:** 3 atomic commits

**Features:**
- Dialog close delay reduced from 200ms to 100ms
- Execution philosophy emphasizing direct CLI commands without pre-checks
- Documented read-only queries that can parallelize
- All controller delays verified as appropriate

## Plan 23-03 Summary

**Timeline:** 1 day (2026-01-20)
**Deliverables:**
- setup open-settings command for opening SettingsDialog
- setup camera-states command for querying camera button states
- All CLI-01 requirements from REQUIREMENTS.md satisfied

**Commits:** 1 atomic commit

**Features:**
- `setup open-settings [--json]` - Click Settings button in SetupWindow
- `setup camera-states [--json]` - Get enabled states of general/nir1/nir2 camera buttons
- Exit codes: SUCCESS=0, ERROR=1, NOT_FOUND=2

## Plan 23-02 Summary

**Timeline:** 1 day (2026-01-20)
**Deliverables:**
- test-executor.md updated with Setup Commands section
- test-executor.md updated with Pattern 5: Setup Workflow
- test-orchestrator.md updated with Setup Commands section
- test-orchestrator.md updated with Config Verification guidance

**Commits:** 4 atomic commits

**Features:**
- CLI reference for setup verify-config command
- CLI reference for setup complete-full workflow
- Config verification step documented for orchestration
- Table format for concise command reference

## Plan 22-03 Summary

**Timeline:** 1 day (2026-01-20)
**Deliverables:**
- SetupConfigVerifier.cs (518 lines)
- SetupCommands.cs (301 lines) - verify-config and complete-full commands
- Path normalization for WSL/Windows compatibility

**Commits:** 2 atomic commits

**Features:**
- `setup verify-config` - Compare simulator and ChronoView settings
- `setup complete-full --verify-config` - Full workflow with config verification
- Path normalization: `/mnt/c/...` ↔ `C:\...`
- JSON output for programmatic consumption

## Plan 22-02 Summary

**Timeline:** 1 day (2026-01-20)
**Deliverables:**
- SetupCommands.cs initial version (762 lines)
- 7 CLI commands for SetupWindow automation
- Program.cs registration complete

**Commits:** 2 atomic commits

**Commands:**
- `setup open-settings` - Open SettingsDialog
- `setup camera-general` - Launch General Camera
- `setup camera-nir1` - Launch NIR1 Camera
- `setup camera-nir2` - Launch NIR2 Camera
- `setup toggle-nir-filtering` - Toggle/set NIR filtering
- `setup camera-states` - Query button states
- `setup start-monitoring` - Click Start, wait for MainWindow
- `setup complete-full` - Execute full workflow

## Plan 22-01 Summary

**Timeline:** 1 day (2026-01-20)
**Deliverables:**
- ChronoSetupWindowController.cs (511 lines)
- 10 public methods for SetupWindow automation
- Helper methods: FindButtonById, ClickButton, IsButtonEnabled, Dispose
- All SetupWindow button AutomationId references implemented

**Commits:** 10 atomic commits

**Methods:**
- FindSetupWindow() - Find SetupWindow via ChronoWindowFinder
- ClickSettingsButton() - Click Settings gear icon
- ClickGeneralCamera() - Launch General Camera program
- ClickNir1() - Launch NIR1 Camera program
- ClickNir2() - Launch NIR2 Camera program
- ToggleNirFiltering() - Toggle NIR filtering with optional target state
- ClickStartButton() - Start monitoring (transition to MainWindow)
- CloseWindow() - Close SetupWindow with timeout wait
- WaitForMainWindow() - Wait for MainWindow after Start click
- GetCameraStates() - Get enabled states of all camera buttons

## Milestone v1.2 Summary

**Timeline:** 1 day (2026-01-20)
**Deliverables:**
- AppLifecycleCommands handler with launch/stop/restart/status commands
- Test-executor and test-orchestrator documentation updated with app commands
- Fully autonomous test capability - agents can now launch, control, and terminate ChronoView programmatically

## Milestone v1.1 Summary

**Timeline:** 2 days (2026-01-19 → 2026-01-20)
**Deliverables:**
- CommandRegistry architecture with ICommandHandler interface
- 10 modular handler classes (each < 600 lines)
- Program.cs reduced from 3,611 to 60 lines (98.3% reduction)
- Zero behavioral regressions - all CLI commands verified working

## Milestone v1.0 Summary

**Timeline:** 73 days (2025-11-06 → 2026-01-18)
**Deliverables:**
- FlaUI.UIA3-based C# CLI tool (~10,000 LOC)
- 30+ CLI commands for ChronoView UI automation
- Python test agent with 75+ pytest tests
- Comprehensive documentation

## Accumulated Context

### Key Decisions

Decisions from all phases are logged in PROJECT.md.

| Phase | Decision | Rationale |
|-------|----------|-----------|
| 1-21 | UIA3 + Modular Commands | Stable automation foundation |
| 22-01 | SetupWindowController | Dedicated class for setup workflow |
| 22-02 | SetupCommands CLI handler | 7 commands for complete setup automation |
| 22-03 | SetupConfigVerifier | Data simulator vs ChronoView settings validation |
| 23-01 | Dialog delay optimization | 100ms sufficient for UI settle, faster execution |
| 23-01 | Execute first, verify on failure | Pre-checks add overhead; direct execution faster |
| 23-01 | Parallel execution for read queries | Independent operations can run concurrently |
| 23-03 | Complete CLI-01 commands | Expose ChronoSetupWindowController methods via CLI |
| 24-01 | Centralized error handling | Try-catch wrapper at Program.cs for better diagnostics |
| 24-02 | Return-based exit codes | Use InvocationContext.ExitCode for System.CommandLine beta4 compatibility |
| 25-01 | State file persistence | simulation_state.json in config_dir enables cross-process status queries |
| 25-01 | Cleanup-on-start pattern | Remove old state file before simulation to prevent stale status |
| 25-01 | Independent --status flag | Works without --cli, reads state file without blocking |
| 25-02 | Status polling patterns | Background execution with &/Start-Process for cross-process status queries |
| 25-02 | Bash polling via grep | Simple JSON parsing without jq dependency |
| 25-02 | PowerShell polling | Select-String regex-based JSON parsing for Windows |

### Deferred Issues

None.

### Pending Todos

- [x] Create ChronoSetupWindowController class
- [x] Implement setup complete-full workflow (Plan 22-02)
- [x] Add config verification logic (Plan 22-03)
- [x] Update agent documentation with setup commands
- [x] Optimize test execution speed (Plan 23-01)
- [x] Create ExitCodes.cs with centralized constants (Plan 24-01)
- [x] Add try-catch wrapper around InvokeAsync (Plan 24-01)
- [x] Refactor all command handlers to return exit codes via InvocationContext (Plan 24-02)

### Blockers/Concerns

**Identified Issues:**
1. ~~SetupWindow에서 설정을 열어서 모니터링 시작 버튼을 누르지 못함~~ (resolved with ChronoSetupWindowController)
2. 껐다가 다시 켰을 때 아무런 작동도 안 됨
3. ~~테스트 실행 속도가 느림~~ (resolved with delay optimization and parallel patterns)
4. ~~Exit Code 1 에러 메시지가 너무 일반적~~ (resolved in Phase 24)

**Root Causes:**
- ~~SetupWindow 전용 컨트롤러 부재~~ (resolved)
- ~~데이터 시뮬레이터 설정 검증 부재~~ (resolved with SetupConfigVerifier)
- ~~불필요한 사전 체크로 인한 지연~~ (resolved)
- ~~Environment.Exit() 호출로 인한 예외 처리 우회~~ (resolved with InvocationContext.ExitCode)

## Session Continuity

Last session: 2026-01-21
Stopped at: Completed Phase 25-02 (Status Documentation)
Resume file: None (Phase 25 complete, ready for Phase 26)

## Roadmap Evolution

- Milestone v1.4 created: Test Agent Architecture & Reliability, 4 phases (Phase 24-27)
- Phase 24 planned: Error Diagnosis (2 plans)
  - 24-01: Centralized Error Handler (ExitCodes.cs + Program.cs wrapper)
  - 24-02: Command Handler Refactoring (return-based exit codes)

**Current State:**
- Phase 22 complete: ChronoSetupWindowController + SetupCommands + SetupConfigVerifier (2,092 lines total)
- Phase 23 complete: Performance optimization + CLI-01 commands (open-settings, camera-states)
- Phase 24 complete: Error Diagnosis with centralized ExitCodes and InvocationContext pattern
- Phase 25 complete: Simulator status CLI endpoint + documentation with polling patterns
- 10 controller methods + 11 CLI commands + config verification + optimized execution patterns + centralized error handling + status query + polling patterns
- v1.3 Setup Automation & Test Reliability: Phase 22 complete, Phase 23 complete
- v1.4 Test Agent Architecture & Reliability: Phase 24 complete (100%), Phase 25 complete (100%)

**v1.2 Test Automation Enhancement - SHIPPED**

- AppLifecycleCommands with launch/stop/restart/status
- Agent documentation updated
- Full archive: .planning/milestones/v1.2-ROADMAP.md

**v1.1 Code Quality Refactoring - SHIPPED**

- Program.cs: 3,611 → 60 lines (98.3% reduction)
- 10 modular handler classes created
- Full archive: .planning/milestones/v1.1-ROADMAP.md

**v1.0 ChronoView UI Automation - SHIPPED**

- 73 days development (2025-11-06 → 2026-01-18)
- ~11,600 LOC C# + Python test agent
- Full archive: .planning/milestones/v1.0-ROADMAP.md

---

*Updated: 2026-01-21 after Phase 25-02 completion - Phase 25: 2/2 plans complete*
