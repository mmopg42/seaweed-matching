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

### ✅ v1.2 Test Automation Enhancement (Shipped: 2026-01-20)

**Phases:** 20-21 (2 plans) | **Timeline:** 1 day | **LOC:** ~150

**Delivered:**
- AppLifecycleCommands handler with launch/stop/restart/status commands
- Test-executor and test-orchestrator documentation updated with app lifecycle commands
- Fully autonomous test capability - agents can now launch, control, and terminate ChronoView programmatically

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
<summary>📦 Completed Phases (11-21)</summary>

- [x] **Phase 11: Commands Architecture** - ICommandHandler interface and CommandRegistry
- [x] **Phase 12: Windows Commands** - 6 window detection commands extracted
- [x] **Phase 13: Toolbar Commands** - 9 toolbar operations extracted
- [x] **Phase 14: Data Panel Commands** - Stats and datagrid commands extracted
- [x] **Phase 15: Workflow & Settings Commands** - 11 workflow + 19 settings commands
- [x] **Phase 16: File Operations Commands** - 9 file operations extracted
- [x] **Phase 17: Test & Scenario Commands** - Test/scenario/batch orchestration
- [x] **Phase 18: Config & Utility Commands** - Inspect and config commands
- [x] **Phase 19: Main Cleanup** - Final cleanup and verification
- [x] **Phase 20: App Lifecycle Commands** - launch/stop/restart/status commands
- [x] **Phase 21: Test Executor Updates** - Agent documentation with app commands

</details>

## Progress

| Milestone | Phases | Plans | Status | Shipped |
|-----------|--------|-------|--------|---------|
| v1.0 UI Automation | 1-10 | 28 | ✅ Complete | 2026-01-18 |
| v1.1 Code Quality | 11-19 | 12 | ✅ Complete | 2026-01-20 |
| v1.2 Test Automation | 20-21 | 2 | ✅ Complete | 2026-01-20 |

| Phase | Milestone | Plans | Status | Completed |
|-------|-----------|-------|--------|-----------|
| 20. App Lifecycle Commands | v1.2 | 1 | ✅ Complete | 2026-01-20 |
| 21. Test Executor Agent Updates | v1.2 | 1 | ✅ Complete | 2026-01-20 |

## Current State

**Status:** ✅ v1.2 Test Automation Enhancement shipped. All planned features complete.

**Data Simulator:** Existing `data_simulator.py` retained - works with test-executor as-is.

---

*Last updated: 2026-01-20 - v1.2 Shipped*
