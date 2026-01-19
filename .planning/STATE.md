# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-19)

**Core value:** UI 요소 식별 및 조작 — ChronoView의 모든 UI 요소를 안정적으로 식별하고 조작
**Current focus:** Phase 11 — Commands Architecture

## Current Position

**Milestone:** v1.1 Code Quality Refactoring (Phase 11 of 19)
**Plan:** Not started
**Status:** Ready to plan
**Last activity:** 2026-01-19 — Milestone v1.1 created

Progress: ██░░░░░░░░░ 10% (28/37 plans complete: v1.0 done, v1.1 TBD)

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

### Deferred Issues

None yet.

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-01-19
Stopped at: Milestone v1.1 initialization
Resume file: None

## Roadmap Evolution

- Milestone v1.1 created: Code quality refactoring, 9 phases (Phase 11-19)
- Goal: Refactor Program.cs from 3,604 lines to ~500 lines per module

---

*Updated: 2026-01-19 after v1.1 milestone creation*
