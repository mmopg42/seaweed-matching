# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-19)

**Core value:** UI 요소 식별 및 조작 — ChronoView의 모든 UI 요소를 안정적으로 식별하고 조작
**Current focus:** Phase 15 — Workflow and Settings Commands Extraction

## Current Position

**Milestone:** v1.1 Code Quality Refactoring (Phase 15 of 19)
**Plan:** 15-02 (Settings and Console Log Commands Migration) - COMPLETE
**Status:** Phase 15 complete
**Last activity:** 2026-01-19 — Phase 15-02 completed

Progress: ███░░░░░░░░ 11% (35/37 plans complete: v1.0 done, v1.1 in progress)

## Milestone v1.0 Summary

**Timeline:** 73 days (2025-11-06 → 2026-01-18)
**Deliverables:**
- FlaUI.UIA3-based C# CLI tool (~10,000 LOC)
- 30+ CLI commands for ChronoView UI automation
- Python test agent with 75+ pytest tests
- Comprehensive documentation

**Tech Stack:**
- C# (.NET 10): FlaUI.UIA3 5.0.0, System.CommandLine 2.0.0-beta4
- Python 3: pytest>=7.0.0, pydantic>=2.0.0

**CLI Commands:**
- test: connectivity, inspect
- windows: find, list
- toolbar: start, stop, settings, refresh
- stats: get, datagrid operations
- workflow: camera control, path configuration
- settings: dialog control, path/checkbox management
- logs: get, tail, filter, search
- file-ops: select, move, delete, verify
- scenario: high-level workflows
- batch: bulk operations

## Accumulated Context

### Key Decisions

Decisions from all phases are logged in PROJECT.md.

| Phase | Decision | Rationale |
|-------|----------|-----------|
| 1 | FlaUI.UIA3 5.0.0 with net10.0-windows | Same version as ChronoView for consistency |
| 1 | System.CommandLine 2.0.0-beta4 | Modern Microsoft CLI library for .NET 10 |
| 2 | Substring matching for all dialog finders | Borderless windows may have title detection quirks |
| 2 | Dedicated ChronoWindowFinder class | Cohesive window detection API |
| 3 | Toolbar button finding by Korean text | Buttons have Korean labels ("시작", "중지", etc.) |
| 3 | ChronoToolbarController class | Dedicated controller following ChronoWindowFinder pattern |
| 4 | DataGrid via ControlType.DataItem | WPF DataGrid rows appear as DataItem |
| 5 | ValuePattern for TextBox I/O | ValuePattern.SetValue() for write with Name fallback |
| 6 | Bilingual support (English/Korean) | English first with Korean fallback for robustness |
| 7 | DataItem pattern for log row extraction | LogPanel rows appear as DataItem with Text children |
| 8 | Confirmation dialog via window enumeration | MessageBox dialogs appear as Window elements |
| 9 | Exit code constants pattern | SUCCESS=0, ERROR=1, NOT_FOUND=2, TIMEOUT=3, INVALID_ARGUMENT=4 |
| 9 | JSON output wrapper format | { success, data: {...} } or { success, error, errorCode } |
| 10 | Python subprocess test agent | Language-agnostic, leverages --json output |
| 10 | Pydantic for JSON validation | Type-safe SuccessResponse/ErrorResponse models |
| 11-01 | ICommandHandler interface with RegisterCommands(RootCommand) | Enables modular command registration pattern |
| 11-01 | CommandRegistry class using List<ICommandHandler> | Centralized handler aggregation for Program.cs |
| 11-01 | Infrastructure-first approach | Add registry before migrating commands (minimizes risk) |
| 11-02 | LegacyCommands class with detect/list/find/click commands | First command group migrated from Program.cs |
| 11-02 | Pure migration approach (no refactoring) | Original handler code copied verbatim for compatibility |
| 11-02 | Registry.RegisterAllCommands() after inline commands | Maintains command order in help output |
| 12-01 | WindowsCommands class with 6 window detection commands | Second command group migrated from Program.cs |
| 12-01 | Private helper methods in handler classes | PrintJsonOutput, TryGetAutomationId kept local to handlers |
| 13-01 | ToolbarCommands class with 9 toolbar commands | Third command group migrated from Program.cs |
| 14-01 | DataPanelCommands class with 7 data panel commands | Fourth command group migrated from Program.cs |
| 14-01 | Use ChronoDataPanelReader directly in DataPanelCommands | DataPanelCommands uses DataReader alias for direct API access |
| 15-01 | WorkflowCommands class with 11 workflow and log commands | Fifth command group migrated from Program.cs |
| 15-01 | Use ChronoWorkflowController and ChronoDataPanelReader in WorkflowCommands | WorkflowCommands uses Workflow and DataReader aliases |
| 15-02 | SettingsCommands class with 19 settings and console log commands | Sixth command group migrated from Program.cs |
| 15-02 | Use ChronoSettingsController and ConsoleLogsReader in SettingsCommands | SettingsCommands uses Settings and ConsoleLogs aliases |

### Deferred Issues

None yet.

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-01-19
Stopped at: Phase 15-02 completed, Phase 15 fully complete
Resume file: .planning/phases/15-workflow-settings-commands/15-02-SUMMARY.md

## Roadmap Evolution

- Milestone v1.1 created: Code quality refactoring, 9 phases (Phase 11-19)
- Goal: Refactor Program.cs from 3,604 lines to ~500 lines per module
- Phase 11-02: Program.cs reduced from 3611 to 3429 lines (182 line reduction)
- Phase 12-01: Program.cs reduced from 3429 to 3054 lines (375 line reduction)
- Phase 13-01: Program.cs reduced from 3054 to 2871 lines (183 line reduction)
- Phase 14-01: Program.cs reduced from 2871 to 2477 lines (394 line reduction)
- Phase 15-01: Program.cs reduced from 2477 to 2045 lines (432 line reduction)
- Phase 15-02: Program.cs reduced from 2045 to 1406 lines (639 line reduction)
- Total reduction so far: 2,205 lines (~61% reduction from original)
- Pattern confirmed for future command extraction phases

---

*Updated: 2026-01-19 after Phase 15-02 completion*
