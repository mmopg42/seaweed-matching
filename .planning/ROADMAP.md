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

### ✅ v1.1 Code Quality Refactoring (Shipped: 2026-01-20)

**Phases:** 11-19 (12 plans) | **Timeline:** 2 days | **LOC:** ~11,600

**Delivered:**
- CommandRegistry architecture with ICommandHandler interface
- 10 modular handler classes (each < 600 lines)
- Program.cs reduced from 3,611 to 60 lines (98.3% reduction)
- Zero behavioral regressions - all CLI commands verified working

**[→ Full details: milestones/v1.1-ROADMAP.md](milestones/v1.1-ROADMAP.md)**

### 🚧 v1.2 Test Automation Enhancement (In Progress)

**Phases:** 20-22 (estimated 3-6 plans) | **Goal:** 완전 자율 테스트를 위한 앱 실행 자동화

**Planned:**
- App lifecycle commands (launch/terminate/restart/status)
- Test executor agent improvements to use new app commands
- Test data generator enhancements

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

<details>
<summary>📦 Completed Phases (11-19)</summary>

- [x] **Phase 11: Commands Architecture** - ICommandHandler interface and CommandRegistry
- [x] **Phase 12: Windows Commands** - 6 window detection commands extracted
- [x] **Phase 13: Toolbar Commands** - 9 toolbar operations extracted
- [x] **Phase 14: Data Panel Commands** - Stats and datagrid commands extracted
- [x] **Phase 15: Workflow & Settings Commands** - 11 workflow + 19 settings commands
- [x] **Phase 16: File Operations Commands** - 9 file operations extracted
- [x] **Phase 17: Test & Scenario Commands** - Test/scenario/batch orchestration
- [x] **Phase 18: Config & Utility Commands** - Inspect and config commands
- [x] **Phase 19: Main Cleanup** - Final cleanup and verification

</details>

### 🚧 v1.2 Test Automation Enhancement (In Progress)

**Milestone Goal:** 완전 자율 테스트를 위한 앱 실행 자동화 — CLI에서 ChronoView 실행/종료/재시작/상태 확인 기능을 추가하여 test-executor 에이전트가 독립적으로 ChronoView를 제어하고 테스트할 수 있게 함

#### Phase 20: App Lifecycle Commands

**Goal**: UI Automation CLI에 ChronoView 실행/종료/재시작/상태 확인 명령어 추가
**Depends on**: Phase 19 (v1.1 complete)
**Research**: Unlikely (내부 프로세스 관리, 기존 CommandRegistry 패턴 활용)
**Plans**: TBD

Plans:
- [ ] 20-01: TBD (run /gsd:plan-phase 20 to break down)

#### Phase 21: Test Executor Agent Updates

**Goal**: test-executor 에이전트가 새 app 명령어를 사용하도록 업데이트
**Depends on**: Phase 20
**Research**: Unlikely (에이전트 스크립트 패턴 수정)
**Plans**: TBD

Plans:
- [ ] 21-01: TBD (run /gsd:plan-phase 21 to break down)

#### Phase 22: Test Data Generator Improvements

**Goal**: data_simulator.py 개선 (구체적 내용은 진행 중 정의)
**Depends on**: Phase 21
**Research**: Unlikely (내부 Python 스크립트 개선)
**Plans**: TBD

Plans:
- [ ] 22-01: TBD (run /gsd:plan-phase 22 to break down)

## Progress

| Milestone | Phases | Plans | Status | Shipped |
|-----------|--------|-------|--------|---------|
| v1.0 UI Automation | 1-10 | 28 | ✅ Complete | 2026-01-18 |
| v1.1 Code Quality | 11-19 | 12 | ✅ Complete | 2026-01-20 |
| v1.2 Test Automation | 20-22 | 0/? | 🚧 In Progress | - |

| Phase | Milestone | Plans | Status | Completed |
|-------|-----------|-------|--------|-----------|
| 20. App Lifecycle Commands | v1.2 | 0/? | Not started | - |
| 21. Test Executor Agent Updates | v1.2 | 0/? | Not started | - |
| 22. Test Data Generator Improvements | v1.2 | 0/? | Not started | - |

## Current State

**Status:** 🚧 v1.2 Test Automation Enhancement in progress. Phase 20 ready to plan.

**Next:** `/gsd:plan-phase 20` to create detailed plan for App Lifecycle Commands.

---

*Last updated: 2026-01-20 after v1.2 milestone creation*
