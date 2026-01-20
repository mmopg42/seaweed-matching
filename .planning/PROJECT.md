# ChronoView UI Automation Skills

## What This Is

FlaUI.UIA3 기반 ChronoView WPF 데스크톱 애플리케이션 자동화 도구입니다. 30+ CLI 명령어와 Python 테스트 에이전트를 제공하여 ChronoView의 모든 UI 요소를 자동으로 제어하고 테스트할 수 있습니다.

## Core Value

**UI 요소 식별 및 조작** — ChronoView의 모든 UI 요소를 안정적으로 식별하고 조작하는 것이 최우선입니다.

## Requirements

### Validated

- ✓ FlaUI.UIA3 기반 UI 자동화 스킬 구현 — v1.0
- ✓ ChronoView WPF 애플리케이션 구조 파악 완료 — v1.0
- ✓ MVVM 아키텍처 이해 — MainWindow, DashboardViewModel, 각종 Dialog 구조 확인 — v1.0
- ✓ 상태 확인 스킬 구현 — FileGroups, StatusMessage, 통계 정보 읽기 — v1.0
- ✓ 스킬 실행 인터페이스 — Claude Agent가 호출 가능한 명령줄 인터페이스 — v1.0
- ✓ 테스트 에이전트 구현 — 스킬을 사용하여 ChronoView를 자동 테스트하는 에이전트 — v1.0
- ✓ CommandRegistry 아키텍처 — 모듈형 커맨드 핸들러 구조 — v1.1
- ✓ 10개 핸들러 클래스 — 각각 600줄 미만의 단일 책임 클래스 — v1.1

### Active

None currently. All v1.0 and v1.1 requirements validated.

### Out of Scope

- 화면 인식 기반 자동화 (픽셀 기반) — UI Automation API만 사용
- 복수 자동화 방식 혼용 — 단일 방식(FlaUI UIA3)만 채택
- ChronoView 코드 수정 — 외부에서 자동화만 수행

## Context

**Current State (v1.1 shipped):**
- `skills_scripts/ui_automation/`에 FlaUI 기반 C# 프로젝트 (~11,600 LOC)
- FlaUI.UIA3 5.0.0, System.CommandLine 2.0.0-beta4
- 10개 모듈형 커맨드 핸들러 클래스 (각각 < 600 lines)
- Program.cs: 60 lines (98.3% reduction from original 3,611 lines)

**ChronoView UI 구조:**
- 메인 윈도우: MainWindow (제목: "ChronoView Pro")
- 툴바: 시작(StartCommand), 중지(StopCommand), 설정, 새로고침(RefreshCommand), 이동(MoveCommand), 삭제(DeleteCommand)
- 대화상자: SetupWindow, SettingsDialog, ImagePreviewWindow
- 컨트롤: WorkflowPanel, StatisticsPanel, FileGroupDataGrid, LogPanel
- 상태 표시줄: StatusMessage, TotalGroups, FailedCount, ProgressValue

## Constraints

- **위치**: `skills_scripts/` 폴더에 별도로 관리 — ChronoView 프로젝트와 분리
- **실행**: Claude Agent에서 호출 가능해야 함
- **자동화 방식**: FlaUI UIA3 단일 방식 채택
- **목적**: 테스트 에이전트로 활용

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| FlaUI UIA3 사용 | Microsoft UIA3 기반으로 WPF 앱 자동화에 최적화 | ✓ Good — v1.0 verified |
| skills_scripts/에 별도 관리 | ChronoView 코드와 분리하여 독립적 유지/배포 | ✓ Good — works well |
| CLI 인터페이스 | Agent가 호출하기 쉬운 표준 입출력 방식 | ✓ Good — v1.0 verified |
| ICommandHandler interface | Modular command registration pattern | ✓ Good — v1.1 verified |
| CommandRegistry class | Centralized handler aggregation | ✓ Good — enables 98% reduction |
| Pure migration approach | Original code copied verbatim for compatibility | ✓ Good — zero regressions |

---
*Last updated: 2026-01-20 after v1.1 milestone completion*
