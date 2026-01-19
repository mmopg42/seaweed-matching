# Roadmap: ChronoView UI Automation

## Overview

FlaUI.UIA3 기반으로 ChronoView WPF 데스크톱 애플리케이션을 자동화하는 스킬 세트와 테스트 에이전트를 개발합니다. UI 요소 식별/조작 인프라부터 시작하여 점진적으로 모든 주요 기능을 자동화하고, 최종적으로는 Agent가 호출 가능한 CLI 인터페이스와 테스트 에이전트를 구현합니다.

## Domain Expertise

None (Windows UI Automation with FlaUI)

## Milestones

### ✅ v1.0 ChronoView UI Automation (Shipped: 2026-01-18)

**Phases:** 1-10 (28 plans) | **Timeline:** 73 days | **LOC:** ~12,300

**Delivered:**
- FlaUI.UIA3-based C# CLI tool for ChronoView UI automation (~10,000 LOC)
- Comprehensive UI control coverage (windows, toolbar, data panels, workflow, settings, logs, file operations)
- Agent-callable CLI with standardized JSON output and exit codes
- Python test agent with 75+ pytest tests and HTML reports

**[→ Full details: milestones/v1.0-ROADMAP.md](milestones/v1.0-ROADMAP.md)**

### 🚧 v1.1 Code Quality Refactoring (In Progress)

**Phases:** 11-19 | **Focus:** Maintainability & Agent Efficiency

**Goal:** Refactor Program.cs (3,604 lines) into focused modules under 500 lines each for better maintainability and AI agent comprehension.

---

<details>
<summary>📦 Completed Phases (1-10)</summary>

- [x] **Phase 1: FlaUI 인프라** - FlaUI 5.x 기반 C# CLI 도구 빌드 및 기반 구축
- [x] **Phase 2: 윈도우 탐지** - ChronoView 메인 윈도우 및 대화상자 식별
- [x] **Phase 3: 툴바 제어** - 시작/중지/설정/새로고침 버튼 자동화
- [x] **Phase 4: 데이터 패널** - StatisticsPanel, FileGroupDataGrid 상태 읽기
- [x] **Phase 5: 워크플로우 제어** - WorkflowPanel 카메라/경로 설정 자동화
- [x] **Phase 6: 설정 대화상자** - SettingsDialog 자동화
- [x] **Phase 7: 로그 모니터링** - LogPanel 실시간 로그 읽기
- [x] **Phase 8: 파일 작업** - 이동/삭제 작업 자동화
- [x] **Phase 9: CLI 인터페이스** - Agent 호출 가능한 명령줄 인터페이스
- [x] **Phase 10: 테스트 에이전트** - 스킬을 사용하는 자동화 테스트 에이전트

</details>

## Progress

| Milestone | Phases | Plans | Status | Shipped |
|-----------|--------|-------|--------|---------|
| v1.0 UI Automation | 1-10 | 28 | ✅ Complete | 2026-01-18 |
| v1.1 Code Quality | 11-19 | TBD | 🚧 In Progress | - |

## Current State

**Status:** Milestone v1.0 complete. v1.1 refactoring planned.

**Next:** Plan Phase 11 with `/gsd:plan-phase 11`

---

### 🚧 v1.1 Code Quality Refactoring (In Progress)

**Milestone Goal:** Refactor Program.cs (3,604 lines → ~500 lines per file) for maintainability and AI agent comprehension.

#### Phase 11: Commands Architecture

**Goal**: Design and implement the command registration architecture
**Depends on**: Phase 10 (previous milestone complete)
**Research**: Unlikely (System.CommandLine patterns established)
**Plans**: 2 plans

Plans:
- [ ] 11-01: Create Commands/ infrastructure with ICommandHandler interface
- [ ] 11-02: Extract legacy commands (detect, list, find, click) to LegacyCommands.cs

#### Phase 12: Windows Commands

**Goal**: Extract all window-related commands to dedicated module
**Depends on**: Phase 11
**Research**: Unlikely (existing ChronoWindowFinder patterns)
**Plans**: 1 plan

Plans:
- [ ] 12-01: Extract windows/* commands to WindowsCommands.cs (~400 lines)

#### Phase 13: Toolbar Commands

**Goal**: Extract toolbar control commands
**Depends on**: Phase 11
**Research**: Unlikely (existing ChronoToolbarController patterns)
**Plans**: 1 plan

Plans:
- [ ] 13-01: Extract toolbar/* and click/* commands to ToolbarCommands.cs (~300 lines)

#### Phase 14: Data Panel Commands

**Goal**: Extract stats and datagrid commands
**Depends on**: Phase 11
**Research**: Unlikely (existing ChronoDataPanelReader patterns)
**Plans**: 1 plan

Plans:
- [ ] 14-01: Extract stats/* and datagrid/* commands to DataPanelCommands.cs (~500 lines)

#### Phase 15: Workflow & Settings Commands

**Goal**: Extract workflow, logs, and settings-dialog commands
**Depends on**: Phase 11
**Research**: Unlikely (existing controller patterns)
**Plans**: 2 plans

Plans:
- [ ] 15-01: Extract workflow/* and logs/* commands to WorkflowCommands.cs (~400 lines)
- [ ] 15-02: Extract settings-dialog/* and console-logs/* to SettingsCommands.cs (~500 lines)

#### Phase 16: File Operations Commands

**Goal**: Extract file-ops commands
**Depends on**: Phase 11
**Research**: Unlikely (existing ChronoFileOperationsController patterns)
**Plans**: 1 plan

Plans:
- [ ] 16-01: Extract file-ops/* commands to FileOpsCommands.cs (~400 lines)

#### Phase 17: Test & Scenario Commands

**Goal**: Extract test, scenario, and batch commands
**Depends on**: Phase 11
**Research**: Unlikely (high-level orchestration, existing patterns)
**Plans**: 1 plan

Plans:
- [ ] 17-01: Extract test/*, scenario/*, and batch/* commands to TestCommands.cs (~600 lines)

#### Phase 18: Config & Utility Commands

**Goal**: Extract remaining config and inspect commands
**Depends on**: Phase 11
**Research**: Unlikely (utility commands)
**Plans**: 1 plan

Plans:
- [ ] 18-01: Extract config/* and inspect/* commands to UtilityCommands.cs (~200 lines)

#### Phase 19: Main Cleanup

**Goal**: Finalize Program.cs as thin coordinator and verify all tests pass
**Depends on**: Phases 11-18
**Research**: Unlikely (coordination, verification)
**Plans**: 2 plans

Plans:
- [ ] 19-01: Refactor Program.cs to command registration only (<100 lines)
- [ ] 19-02: Run full test suite, verify CLI behavior unchanged

---

*Last updated: 2026-01-19 after v1.1 milestone creation*
