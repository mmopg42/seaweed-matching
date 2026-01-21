# Roadmap: ChronoView UI Automation

## Overview

FlaUI.UIA3 기반으로 ChronoView WPF 데스크톱 애플리케이션을 자동화하는 스킬 세트와 테스트 에이전트를 개발합니다. UI 요소 식별/조작 인프라부터 시작하여 점진적으로 모든 주요 기능을 자동화하고, 최종적으로는 Agent가 호출 가능한 CLI 인터페이스와 테스트 에이전트를 구현합니다.

## Domain Expertise

Windows UI Automation with FlaUI

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
- Test-executor and test-orchestrator documentation updated with app commands
- Fully autonomous test capability - agents can now launch, control, and terminate ChronoView programmatically

### ✅ v1.3 Setup Automation & Test Reliability (Shipped: 2026-01-20)

**Phases:** 22-23 (2 phases) | **Timeline:** 1 day | **LOC:** ~2,500

**Delivered:**
- ChronoSetupWindowController with 10 public methods for SetupWindow automation
- SetupCommands with 4 CLI commands (verify-config, complete-full, open-settings, camera-states)
- SetupConfigVerifier for simulator vs ChronoView settings validation
- Test execution speed optimization (20-30% faster via delay reduction and parallel patterns)
- Agent documentation updated with setup workflow patterns

### ✅ v1.4 Test Agent Architecture & Reliability (Shipped: 2026-01-21)

**Phases:** 24-27 (4 phases) | **Timeline:** 1 day | **LOC:** ~150

**Delivered:**
- ExitCodes.cs with 5 centralized exit code constants
- Program.cs error handling wrapper for structured error output
- data_simulator.py --status endpoint for state persistence
- Dynamic log path discovery with --latest flag
- test-orchestrator delegation pattern fix with explicit Bash prohibitions

---

## Phases

### ✅ Phase 1-10: v1.0 UI Automation (Shipped)

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

### ✅ Phase 11-19: v1.1 Code Quality (Shipped)

- [x] **Phase 11: Commands Architecture** - ICommandHandler interface and CommandRegistry
- [x] **Phase 12: Windows Commands** - 6 window detection commands extracted
- [x] **Phase 13: Toolbar Commands** - 9 toolbar operations extracted
- [x] **Phase 14: Data Panel Commands** - Stats and datagrid commands extracted
- [x] **Phase 15: Workflow & Settings Commands** - 11 workflow + 19 settings commands
- [x] **Phase 16: File Operations Commands** - 9 file operations extracted
- [x] **Phase 17: Test & Scenario Commands** - Test/scenario/batch orchestration
- [x] **Phase 18: Config & Utility Commands** - Inspect and config commands
- [x] **Phase 19: Main Cleanup** - Final cleanup and verification

### ✅ Phase 20-21: v1.2 Test Automation (Shipped)

- [x] **Phase 20: App Lifecycle Commands** - launch/stop/restart/status commands
- [x] **Phase 21: Test Executor Agent Updates** - Agent documentation with app commands

### ✅ Phase 22-23: v1.3 Setup & Performance (Shipped)

- [x] **Phase 22: Setup Window Controller** - SetupWindow automation and settings dialog
- [x] **Phase 23: Performance & Documentation** - Test speed optimization and agent docs

### ✅ Phase 24-27: v1.4 Agent Architecture (Shipped)

- [x] **Phase 24: Error Diagnosis** - Exit Code 1 analysis and centralized error handling
- [x] **Phase 25: Simulator Status Endpoint** - data_simulator.py --status endpoint
- [x] **Phase 26: Dynamic Log Path Discovery** - Automatic log folder discovery
- [x] **Phase 27: Orchestrator Delegation Fix** - Bash tool prohibitions in test-orchestrator

---

### 🚧 v1.5 CLI Skill Encapsulation (In Progress)

**Milestone Goal:** AI agents use semantic skill names for test automation instead of constructing CLI commands directly. This creates a clean separation: orchestrators define WHAT to test (intent), executors handle HOW to execute it (implementation).

**Success Criteria:**
- Orchestrator agents never construct CLI commands (prohibition enforced in docs)
- All 90+ CLI commands mapped to semantic skill names
- Executor validates skills before execution and reports unknown skills
- JSON responses standardized with retryable and suggestion fields
- Dry-run mode enables safe command validation

#### Phase 28: Skill Registry Definition

**Goal:** Create semantic skill definitions for all 90+ CLI commands

**Depends on:** Phase 27 (delegation pattern established)
**Research:** Unlikely (skills map 1:1 to existing CLI commands)
**Plans:** 1 plan

**Requirements:** SKILL-01 through SKILL-06

**Success Criteria:**
1. All 90+ CLI commands have corresponding skill definitions
2. Each skill has semantic name (intent-based, not CLI-based)
3. Skill definition includes exact CLI command pattern and parameter schema
4. Skills organized by category (APP, BATCH, CONSOLE_LOGS, DATA_PANEL, FILE_OPS, LOGS, SETTINGS_DIALOG, SETUP, TEST, TOOLBAR, UTILITY, WINDOWS, WORKFLOW)
5. test-executor-skills.md documents complete skill registry

Plans:
- [x] 28-01-PLAN.md — Create skill registry document with all 90+ skill definitions

#### Phase 29: Orchestrator Skill Integration

**Goal:** Update test-orchestrator to use skill names only (no CLI commands)

**Depends on:** Phase 28 (skills defined)
**Research:** Unlikely (delegation pattern established in Phase 27)
**Plans:** 1 plan

**Requirements:** ORCH-01 through ORCH-05

**Success Criteria:**
1. Orchestrator uses skill names only (no CLI commands in delegation)
2. test-orchestrator.md includes skill reference section (names and descriptions)
3. "No Command Construction" prohibition added with examples
4. Delegation template uses skill names
5. Orchestrator documentation explicitly forbids CLI command construction

Plans:
- [x] 29-01-PLAN.md — Update test-orchestrator with skill-based delegation format, prohibition section, and skill reference

#### Phase 30: Executor Skill Translation

**Goal:** Enable executor to translate skill names to CLI commands

**Depends on:** Phase 28 (skills defined), Phase 29 (orchestrator using skills)
**Research:** Complete (30-RESEARCH.md provides patterns)
**Plans:** 1 plan

**Requirements:** EXEC-01 through EXEC-05

**Success Criteria:**
1. test-executor.md includes skill-to-CLI translation patterns
2. Executor validates skill names against registry before execution
3. Executor reports error for unknown skills with helpful message
4. Retry logic respects skill `retryable` flag
5. Error handling includes skill context in messages

Plans:
- [ ] 30-01-PLAN.md — Add skill translation documentation to test-executor.md with parsing, validation, error handling, and retryable flag support

#### Phase 31: CLI Schema Standardization

**Goal:** Standardize JSON response format across all commands

**Depends on:** Phase 28 (skill definitions include schema expectations)
**Research:** Unlikely (schema standardization is well-defined)
**Plans:** TBD

**Requirements:** SCHEMA-01 through SCHEMA-05

**Success Criteria:**
1. All CLI commands return standardized JSON with success, data/error, errorCode fields
2. Error responses include retryable boolean for recoverable failures
3. Error responses include suggestion string for common failures
4. JSON schemas documented for each command category
5. Executor can reliably parse all command responses

#### Phase 32: Dry-Run Mode

**Goal:** Add --dry-run flag for safe command validation

**Depends on:** Phase 31 (standardized schemas)
**Research:** Unlikely (dry-run is standard CLI pattern)
**Plans:** TBD

**Requirements:** DRYRUN-01 through DRYRUN-05

**Success Criteria:**
1. All CLI commands support --dry-run flag
2. Dry-run returns command that would execute without execution
3. Dry-run validates parameter syntax before returning
4. Dry-run validates skill exists (in executor)
5. test-executor.md documents dry-run usage patterns

---

## Progress

| Milestone | Phases | Plans | Status | Shipped |
|-----------|--------|-------|--------|---------|
| v1.0 UI Automation | 1-10 | 28 | ✅ Complete | 2026-01-18 |
| v1.1 Code Quality | 11-19 | 12 | ✅ Complete | 2026-01-20 |
| v1.2 Test Automation | 20-21 | 2 | ✅ Complete | 2026-01-20 |
| v1.3 Setup & Perf | 22-23 | 6 | ✅ Complete | 2026-01-20 |
| v1.4 Agent Architecture | 24-27 | 8 | ✅ Complete | 2026-01-21 |
| v1.5 CLI Skill Encapsulation | 28-32 | 3 | 🚧 In progress | - |

| Phase | Milestone | Plans | Status |
|-------|-----------|-------|--------|
| 22. Setup Window Controller | v1.3 | 3 | ✅ Complete |
| 23. Performance & Docs | v1.3 | 3 | ✅ Complete |
| 24. Error Diagnosis | v1.4 | 2 | ✅ Complete |
| 25. Simulator Status | v1.4 | 2 | ✅ Complete |
| 26. Log Path Finder | v1.4 | 2 | ✅ Complete |
| 27. Delegation Fix | v1.4 | 2 | ✅ Complete |
| 28. Skill Registry | v1.5 | 1 | ✅ Complete |
| 29. Orchestrator Integration | v1.5 | 1 | ✅ Complete |
| 30. Executor Translation | v1.5 | 1 | Ready |
| 31. CLI Schema | v1.5 | TBD | Not started |
| 32. Dry-Run Mode | v1.5 | TBD | Not started |

## Current State

**Status:** 🚧 v1.5 CLI Skill Encapsulation - Phase 30 planned, ready for execution.

**Next Step:** Run `/gsd:execute-phase 30` to add skill translation documentation to test-executor.md.

---

*Last updated: 2026-01-21 - Phase 30 planned*
