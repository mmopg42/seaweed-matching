# ChronoView UI Automation Skills

## What This Is

ChronoView WPF 데스크톱 애플리케이션을 자동화하는 스킬 세트입니다. 최종적으로는 이 스킬을 사용하는 테스트 에이전트를 만들어 ChronoView의 UI 자동화 테스트를 수행할 수 있게 합니다.

## Core Value

**UI 요소 식별 및 조작** - ChronoView의 모든 UI 요소를 안정적으로 식별하고 조작하는 것이 최우선입니다.

## Requirements

### Validated

- ✓ ChronoView WPF 애플리케이션 구조 파악 완료 — 기존 코드베이스 매핑 확인
- ✓ MVVM 아키텍처 이해 — MainWindow, DashboardViewModel, 각종 Dialog 구조 확인

### Active

- [ ] UI 자동화 스킬 구현 — 시작/중지, 설정, 새로고침, 이동, 삭제 등 핵심 기능 제어
- [ ] 상태 확인 스킬 구현 — FileGroups, StatusMessage, 통계 정보 읽기
- [ ] 스킬 실행 인터페이스 — Claude Agent가 호출 가능한 명령줄 인터페이스
- [ ] 테스트 에이전트 구현 — 스킬을 사용하여 ChronoView를 자동 테스트하는 에이전트
- [ ] UI 요소 식별 최적화 — FlaUI를 사용한 안정적인 요소 찾기

### Out of Scope

- 화면 인식 기반 자동화 (픽셀 기반) — UI Automation API만 사용
- 복수 자동화 방식 혼용 — 단일 방식(FlaUI UIA3)만 채택
- ChronoView 코드 수정 — 외부에서 자동화만 수행

## Context

**기존 작업:**
- `skills_scripts/ui_automation/`에 FlaUI 기반 C# 프로젝트가 이미 존재
- FlaUI.UIA3 5.0.0 사용 중
- System.CommandLine으로 CLI 인터페이스 구현

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
- **목적**: 최종적으로 테스트 에이전트로 활용

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| FlaUI UIA3 사용 | Microsoft UIA3 기반으로 WPF 앱 자동화에 최적화 | — Pending |
| skills_scripts/에 별도 관리 | ChronoView 코드와 분리하여 독립적 유지/배포 | — Pending |
| CLI 인터페이스 | Agent가 호출하기 쉬운 표준 입출력 방식 | — Pending |

---
*Last updated: 2026-01-16 after initialization*
