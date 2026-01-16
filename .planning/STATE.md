# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-16)

**Core value:** UI 요소 식별 및 조작 — ChronoView의 모든 UI 요소를 안정적으로 식별하고 조작
**Current focus:** Phase 5 — Workflow panel control

## Current Position

Phase: 5 of 10 (workflow-control)
Plan: 2 of 4 in phase
Status: Plan 05-02 complete
Last activity: 2026-01-16 — Completed 05-02: ChronoWorkflowController class and CLI workflow camera commands

Progress: ████░░░░░░░░░░ 50% (2/4 plans)

## Performance Metrics

**Velocity:**
- Total plans completed: 16
- Average duration: 9.1 min
- Total execution time: 2.42 hours

**By Phase:**

| Phase | Plans | Complete | Avg/Plan |
|-------|-------|----------|----------|
| 01-infra | 2 | 2 | 12.5 min |
| 02-window-detection | 3 | 3 | 11 min |
| 03-toolbar-control | 4 | 4 | 6.5 min |
| 04-data-panel | 3 | 3 | 8.3 min |
| 05-workflow-control | 4 | 2 | 7 min |

**Recent Trend:**
- Last 5 plans: 05-02 (6 min), 05-01 (8 min), 04-03 (10 min), 04-02 (5 min), 04-01 (15 min)
- Trend: Stable, Phase 5 progressing

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

| Phase | Decision | Rationale |
|-------|----------|-----------|
| 1 | FlaUI.UIA3 5.0.0 with net10.0-windows | Same version already used in ChronoView project for consistency |
| 1 | System.CommandLine 2.0.0-beta4 | Modern Microsoft CLI library compatible with .NET 10 |
| 1 | FlaUI 5.x Properties access pattern | Properties.NativeWindowHandle.ValueOrDefault instead of direct property |
| 2 | Substring matching for all dialog finders | Borderless windows (WindowStyle="None") may have title detection quirks |
| 2 | Bilingual SettingsDialog search | Try English "Settings" first, fallback to Korean "설정" |
| 2 | Namespace alias for Program.cs | Using alias `UiAuto = SkillsScripts.UiAutomation.UiAutomation` to avoid conflict with `namespace UiAutomation` |
| 2 | Dedicated ChronoWindowFinder class | Cohesive window detection API, separate from UiAutomation core |
| 3 | Toolbar button finding by Korean text | Buttons have Korean text labels ("시작", "중지", "설정", etc.) |
| 3 | button.Patterns.Invoke.Pattern for clicking | Correct FlaUI 5.x pattern for button InvokePattern |
| 3 | Generic ClickToolbarButton helper | Consolidates button finding and clicking, reduces code duplication |
| 3 | ChronoToolbarController class | Dedicated controller class for toolbar automation, follows ChronoWindowFinder pattern |
| 3 | Wait helpers with 200ms poll interval | Responsive state detection without excessive CPU usage (WaitForButtonEnabled, WaitForButtonDisabled, ClickButtonAndWait) |
| 4 | Text-based statistics extraction | StatisticsPanel uses label TextBlock search and sibling value TextBlock |
| 4 | DataGrid via ControlType.DataItem | WPF DataGrid rows appear as DataItem, cells as Text children |
| 4 | ChronoDataPanelReader controller class | Dedicated data panel reader following ChronoWindowFinder/ChronoToolbarController pattern |
| 5 | FindWorkflowPanel by ControlType.Custom | WorkflowPanel has no AutomationId, searched by Name/ClassName containing "WorkflowPanel" |
| 5 | CLI inspect workflow command | Identifies camera buttons, path TextBoxes, expanders via element tree traversal |
| 5 | ChronoWorkflowController class | Dedicated workflow panel controller following ChronoToolbarController pattern |
| 5 | Camera button text substring matching | Buttons have dynamic text ("실행", "중지"), searched by Korean text substring |
| 5 | Ellipse detection via ClassName | WPF Ellipse appears as ControlType.Custom with ClassName="Ellipse" |

### Deferred Issues

None yet.

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-01-16
Stopped at: Plan 05-02 complete - ChronoWorkflowController class implemented with CLI workflow camera commands
Resume file: None
Note: Phase 5 (workflow-control) has 2 of 4 plans complete. Next: 05-03 path TextBox automation.
