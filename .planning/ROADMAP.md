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

### ✅ Phase 22: Setup Window Controller (v1.3) — Shipped 2026-01-20

**Goal:** SetupWindow 전용 컨트롤러와 설정 다이얼로그 자동화

**Requirements:** SETUP-01, SETUP-02, SETUP-03, SETUP-04

**Plans:**
- [x] 22-01: ChronoSetupWindowController 클래스 생성 (512 lines)
- [x] 22-02: 완전한 셋업 완료 워크플로우 구현 (SetupCommands.cs)
- [x] 22-03: 설정값 검증 기능 (SetupConfigVerifier.cs)

**Delivered:**
- ChronoSetupWindowController: 10 public methods for SetupWindow automation
- SetupCommands: 2 CLI commands (complete-full, verify-config)
- SetupConfigVerifier: WSL/Windows path normalization, config comparison

### ✅ Phase 23: Performance & Documentation (v1.3) — Shipped 2026-01-20

**Goal:** 테스트 속도 최적화와 에이전트 문서 업데이트

**Requirements:** PERF-01, PERF-02, CLI-01

**Plans:**
- [x] 23-01: Test Speed Optimization
  - Reduced dialog close delay from 200ms to 100ms
  - Added "Parallel Execution Groups" section
  - Documented "Execute first, verify on failure" pattern

- [x] 23-02: Agent Documentation Update
  - Added Setup Commands to test-executor.md CLI reference
  - Added Pattern 5: Setup Workflow
  - Added Setup Commands to test-orchestrator.md
  - Added config verification workflow guidance

- [x] 23-03: CLI Command Registration
  - Implemented `setup open-settings` command
  - Implemented `setup camera-states` command
  - All CLI-01 requirements satisfied

---

## Progress

| Milestone | Phases | Plans | Status | Shipped |
|-----------|--------|-------|--------|---------|
| v1.0 UI Automation | 1-10 | 28 | ✅ Complete | 2026-01-18 |
| v1.1 Code Quality | 11-19 | 12 | ✅ Complete | 2026-01-20 |
| v1.2 Test Automation | 20-21 | 2 | ✅ Complete | 2026-01-20 |
| v1.3 Setup & Perf | 22-23 | 6 | ✅ Complete | 2026-01-20 |
| v1.4 Agent Architecture | 24-27 | 8 | 🔄 In Progress | — |

| Phase | Milestone | Plans | Status |
|-------|-----------|-------|--------|
| 22. Setup Window Controller | v1.3 | 3 | ✅ Complete |
| 23. Performance & Docs | v1.3 | 3 | ✅ Complete |
| 24. Error Diagnosis | v1.4 | 2 | ✅ Complete |
| 25. Simulator Status | v1.4 | 2 | 📋 Planned |
| 26. Log Path Finder | v1.4 | 2 | 📋 Planned |
| 27. Delegation Fix | v1.4 | 2 | ○ Pending |

## Current State

**Status:** 🔄 v1.4 Test Agent Architecture & Reliability in progress.

**Next Step:** Run `/gsd:execute-phase 25` to execute Phase 25 plans.

---

### ✅ Phase 24: Error Diagnosis (v1.4) — Shipped 2026-01-21

**Goal:** Exit Code 1 에러 원인 분석 및 해결

**Requirements:** ERROR-01

**Plans:**
- [x] 24-01: Centralized Error Handler (ExitCodes.cs + Program.cs wrapper)
- [x] 24-02: Command Handler Refactoring (return-based exit codes)

**Delivered:**
- ExitCodes.cs with 5 centralized exit code constants (SUCCESS, ERROR, NOT_FOUND, TIMEOUT, INVALID_ARGUMENT)
- Program.cs with try-catch wrapper around InvokeAsync for structured error output
- All 10 command handlers refactored to use context.ExitCode instead of Environment.Exit()
- 262 context.ExitCode assignments, 99 catch blocks with Console.Error.WriteLine

---

### ✅ Phase 22: Setup Window Controller (v1.3) — Shipped 2026-01-20

**Goal:** SetupWindow 전용 컨트롤러와 설정 다이얼로그 자동화

**Requirements:** SETUP-01, SETUP-02, SETUP-03, SETUP-04

**Plans:**
- [x] 22-01: ChronoSetupWindowController 클래스 생성 (512 lines)
- [x] 22-02: 완전한 셋업 완료 워크플로우 구현 (SetupCommands.cs)
- [x] 22-03: 설정값 검증 기능 (SetupConfigVerifier.cs)

### ✅ Phase 23: Performance & Documentation (v1.3) — Shipped 2026-01-20

**Goal:** 테스트 속도 최적화와 에이전트 문서 업데이트

**Requirements:** PERF-01, PERF-02, CLI-01

### ✅ Phase 24: Error Diagnosis (v1.4) — Shipped 2026-01-21

**Goal:** Exit Code 1 에러 원인 분석 및 해결

**Requirements:** ERROR-01

**Plans:**
- [x] 24-01: Centralized Error Handler (ExitCodes.cs + Program.cs wrapper)
  - Created ExitCodes.cs with SUCCESS=0, ERROR=1, NOT_FOUND=2, TIMEOUT=3, INVALID_ARGUMENT=4
  - Added try-catch wrapper around InvokeAsync in Program.cs
  - Structured error messages to stderr with exception type, message, and command
- [x] 24-02: Command Handler Refactoring (return-based exit codes)
  - Refactored all 10 command handler classes to use context.ExitCode
  - Replaced 262 Environment.Exit() calls with context.ExitCode assignments
  - Added 99 try-catch wrappers for local error context

**Delivered:**
- Unhandled exceptions produce detailed error messages on stderr
- Exit code errors include exception type and message
- FlaUI exceptions caught and formatted with context
- Verbose mode shows stack traces for debugging

### 📋 Phase 25: Simulator Status Endpoint (v1.4) — Ready to Execute

**Goal:** data_simulator.py에 --status endpoint 추가

**Requirements:** STATUS-01

**Plans:**
- [ ] 25-01: --status endpoint 구현 (data_simulator.py)
  - Add --status CLI argument
  - Add get_status() method to DataSimulator class
  - Return JSON with status, progress, items_created, simulation_id, last_activity
  - Add last_activity_time tracking in __init__ and simulation methods
- [ ] 25-02: test-executor에 --status 사용 추가
  - Document --status command in test-executor.md
  - Add Status Response Format subsection
  - Add Pattern 6: Status-Based Simulation Wait
  - Update Pattern 2 to use status polling

### 📋 Phase 26: Dynamic Log Path Discovery (v1.4) — Ready to Execute

**Goal:** 로그 경로 동적 해결 (Automatic log folder discovery)

**Requirements:** LOG-01

**Plans:**
- [ ] 26-01: 동적 로그 폴더 finder 구현
  - Add GetLatestLogDateFolder() method to ConsoleLogsReader.cs
  - Add GetLogFilesFromLatest() method to ConsoleLogsReader.cs
  - Add --latest flag to console-logs list/tail/search commands
- [ ] 26-02: log-analyst에 동적 경로 해결 추가
  - Update .claude/commands/test/logs.md with --latest examples
  - Update .claude/agents/log-analyst.md with automatic discovery guidance

**Expected Outcome:**
- Users can query latest logs without specifying date folder
- log-analyst agent can discover log paths automatically
- yyyyMMdd folder pattern validation via DateTime.TryParseExact

### ○ Phase 27: Orchestrator Delegation Fix (v1.4) — Pending

**Goal:** Orchestrator 역할 분할 수정

**Requirements:** DELEGATE-01

**Plans:**
- [ ] 27-01: test-orchestrator 위임 패턴 수정
- [ ] 27-02: 위임 패턴 검증 테스트

---

*Last updated: 2026-01-21 - Phase 24 complete, Phase 25 ready for execution*
