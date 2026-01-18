# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-16)

**Core value:** UI 요소 식별 및 조작 — ChronoView의 모든 UI 요소를 안정적으로 식별하고 조작
**Current focus:** Phase 9 — CLI interface standardization

## Current Position

Phase: 9 of 10 (cli-interface)
Plan: 1 of 1 in phase
Status: Complete
Last activity: 2026-01-18 — Completed 09-01: CLI interface standardization

Progress: █████████░ 90% (26/30 plans complete, Phase 9: 1/1 complete)

## Performance Metrics

**Velocity:**
- Total plans completed: 26
- Average duration: 9.2 min
- Total execution time: 4.00 hours

**By Phase:**

| Phase | Plans | Complete | Avg/Plan |
|-------|-------|----------|----------|
| 01-infra | 2 | 2 | 12.5 min |
| 02-window-detection | 3 | 3 | 11 min |
| 03-toolbar-control | 4 | 4 | 6.5 min |
| 04-data-panel | 3 | 3 | 8.3 min |
| 05-workflow-control | 4 | 3 | 7.3 min |
| 06-settings-dialog | 3 | 3 | 8 min |
| 07-log-monitoring | 2 | 2 | 7 min |
| 08-file-operations | 2 | 2 | 9.5 min |
| 09-cli-interface | 1 | 1 | 34 min |

**Recent Trend:**
- Last 5 plans: 09-01 (34 min), 08-02 (7 min), 08-01 (12 min), 06-03 (8 min), 06-02 (9 min)
- Trend: Phase 9 (cli-interface) complete

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
| 5 | Path TextBox ControlType.Edit | WPF TextBox appears as ControlType.Edit in UI Automation |
| 5 | ValuePattern for TextBox I/O | ValuePattern.Value for read, ValuePattern.SetValue() for write with Name property fallback |
| 5 | Label-TextBox association method | Search for Text label by content, then find sibling Edit control via parent traversal |
| 5 | Panel-scoped TextBox search for Line 2 | Line 2 section found via "Line 2" header, TextBoxes searched within that panel |
| 7 | FindLogPanel follows FindStatisticsPanel pattern | Name first search, ClassName fallback for LogPanel discovery |
| 7 | DataItem pattern for log row extraction | LogPanel rows appear as DataItem with Text children (Severity, Time, Source, Message) |
| 7 | inspect-log CLI command | Lists LogPanel structure at depth=2, shows log row count and headers |
| 7 | Method overloading for log filtering | GetLogsByLevel(logPanel, level) and GetLogsByLevel(level) for flexibility |
| 7 | Case-insensitive log filtering | StringComparison.OrdinalIgnoreCase for level and text search robustness |
| 7 | Private helper for log extraction | GetAllLogMessagesFromPanel reduces duplication across filtering methods |
| 7 | logs CLI command group | logs get, logs tail, logs filter, logs search with --json option |
| 6 | ChronoSettingsController class | Dedicated SettingsDialog controller following ChronoToolbarController pattern (injection + parameterless constructor + IDisposable) |
| 6 | CLI settings-dialog command | Named "settings-dialog" to avoid collision with existing "windows settings" subcommand |
| 6 | OpenSettingsDialog wait strategy | Uses ChronoWindowFinder.WaitForWindow() to detect dialog appearance after button click |
| 6 | Cancel button for dialog close | CloseSettingsDialog() uses Cancel button (취소/Cancel) instead of window close for clean dismissal |
| 6 | SelectionItemPattern for tab selection | Uses SelectionItemPattern.Select() to activate tabs with substring matching on tab Name property |
| 6 | Bilingual tab header support | English first with Korean fallback (Paths/경로, Data Sequence/데이터 순서, etc.) |
| 6 | Section-scoped TextBox search | Line 2 paths use scopeSection parameter to find TextBoxes within "Line 2" section header |
| 6 | CLI settings path variable naming | Prefixed with "settings" to avoid conflicts with existing workflow path commands |
| 6 | TogglePattern for CheckBox state | Uses TogglePattern.ToggleState (On=checked, Off=unchecked) for CheckBox state manipulation |
| 6 | CheckBox detection supports Button+TogglePattern | WPF CheckBoxes may appear as ControlType.Button with TogglePattern |
| 6 | Batch settings dictionary retrieval | GetAdvancedSettings() returns Dictionary<string, bool> for all Advanced tab CheckBoxes |
| 6 | Dialog action buttons with close wait | Save/Cancel wait for dialog close (3000ms); Apply/Reset keep dialog open |
| 8 | ChronoFileOperationsController class | Dedicated file operations controller following ChronoToolbarController pattern |
| 8 | DataGrid CheckBox selection pattern | CheckBox in first column (column 0) of each DataItem row, clicked via TogglePattern or InvokePattern |
| 8 | SelectAll checkbox in DataGrid header | Header ControlType.Header contains SelectAll CheckBox for selecting all rows |
| 8 | GroupId column-based row finding | GroupId in column 1 (after checkbox column 0) used for row identification |
| 8 | Reuse ChronoToolbarController for Move/Delete | File operations reuses existing toolbar controller instead of duplicating button click code |
| 8 | file-ops CLI command group | file-ops select, move, delete, wait, confirm, verify subcommands with --json option |
| 8 | Confirmation dialog search via GetDesktop().FindAllChildren(Window) | MessageBox dialogs appear as Window elements, enumerate all windows to find confirmation dialogs |
| 8 | Bilingual confirmation button support | Support Korean "예"/"확인" and English "Yes"/"OK" for robust confirmation dialog handling |
| 8 | Verification via DataGrid re-read | Post-delete verification re-reads DataGrid to check row count change or search for deleted GroupId |
| 9 | Exit code constants pattern | EXIT_SUCCESS=0, EXIT_ERROR=1, EXIT_NOT_FOUND=2, EXIT_TIMEOUT=3, EXIT_INVALID_ARGUMENT=4 |
| 9 | Parse global options before command invocation | Use rootCommand.Parse(args) to capture --quiet and --verbose, then invoke commands |
| 9 | JSON output wrapper format | All JSON responses: { success, data: {...} } or { success, error, errorCode } |
| 9 | PrintOutput helper respects --quiet | Suppresses Console.WriteLine but Console.Error (via PrintError) never suppressed |

### Deferred Issues

None yet.

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-01-18
Stopped at: Completed 09-01-PLAN.md - CLI interface standardization
Resume file: None
Note: Phase 9 (cli-interface) complete (1/1 plans). Ready for next phase.
