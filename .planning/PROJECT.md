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
- ✓ SetupWindow 전용 컨트롤러 구현 — v1.3
- ✓ 설정 다이얼로그 열기/닫기 자동화 — v1.3
- ✓ 완전한 셋업 완료 워크플로우 — v1.3
- ✓ 데이터 시뮬레이터 설정값 비교 검증 — v1.3
- ✓ 테스트 속도 최적화 — v1.3
- ✓ 중앙화된 에러 핸들러 — ExitCodes.cs + Program.cs wrapper — v1.4
- ✓ 명령어 핸들러 리팩토링 — InvocationContext.ExitCode 패턴 — v1.4
- ✓ 데이터 시뮬레이터 상태 endpoint — --status JSON 반환 — v1.4
- ✓ 동적 로그 경로 해결 — GetLatestLogDateFolder() + --latest 플래그 — v1.4
- ✓ Orchestrator 위임 패턴 수정 — Bash 사용 금지 문서화 — v1.4
- ✓ 스킬 레지스트리 정의 — 92개 스킬 정의, 13개 카테고리 — v1.5
- ✓ Orchestrator 스킬 전용 위임 — CLI 명령어 직접 사용 금지 — v1.5
- ✓ Executor 스킬→CLI 변환 — 검증 및 에러 처리 — v1.5
- ✓ 표준화된 JSON 응답 — SuccessResponse<T>, ErrorResponse — v1.5
- ✓ Dry-Run 모드 — --dry-run 플래그, 스킬 검증 — v1.5

### Active

None — Current milestone complete. Use `/gsd:new-milestone` to define next milestone goals.

### Out of Scope

- 화면 인식 기반 자동화 (픽셀 기반) — UI Automation API만 사용
- 복수 자동화 방식 혼용 — 단일 방식(FlaUI UIA3)만 채택
- ChronoView 코드 수정 — 외부에서 자동화만 수행

## Context

**Current State (v1.5 shipped):**
- `skills_scripts/ui_automation/`에 FlaUI 기반 C# 프로젝트 (~15,900 LOC)
- FlaUI.UIA3 5.0.0, System.CommandLine 2.0.0-beta4
- 10개 모듈형 커맨드 핸들러 클래스 (각각 < 600 lines)
- Program.cs: 60 lines (98.3% reduction from original 3,611 lines)
- 92개 시맨틱 스킬 정의 (13개 카테고리)
- 표준화된 JSON 응답 형식 (SuccessResponse<T>, ErrorResponse)
- --dry-run 플래그 지원 (모든 명령어)

**ChronoView UI 구조:**
- 메인 윈도우: MainWindow (제목: "ChronoView Pro")
- 셋업 윈도우: SetupWindow (제목: "Setup - ChronoView Pro")
  - 설정 버튼 (SetupSettingsButton) - 설정 다이얼로그 열기
  - 모니터링 시작 버튼 (SetupStartButton) - "모니터링 프로그램 시작"
  - 카메라 실행 버튼들 (SetupGeneralCameraButton, SetupNir1CameraButton, SetupNir2CameraButton)
- 툴바: 시작(StartCommand), 중지(StopCommand), 설정, 새로고침(RefreshCommand), 이동(MoveCommand), 삭제(DeleteCommand)
- 대화상자: SettingsDialog, ImagePreviewWindow
- 컨트롤: WorkflowPanel, StatisticsPanel, FileGroupDataGrid, LogPanel
- 상태 표시줄: StatusMessage, TotalGroups, FailedCount, ProgressValue

**데이터 시뮬레이터 설정 (task_helper/data_test/data_simulator.py):**
- `source_line1`: Line 1 소스 폴더 (예: Z:\윤태경\seaweed\program\data\2025_A046)
- `source_line2`: Line 2 소스 폴더
- `target_base`: 타겟 베이스 폴더
- `move_folder`: 이동 폴더
- `trash_folder`: 휴지통 폴더

**ChronoView 설정 구조 (ApplicationConfiguration.cs):**
- `FolderPaths`: Dictionary<string, string> - 모니터링 폴더 경로들
- `WorkflowSettings.Line1Settings.SampleName`: 샘플 이름
- `WorkflowSettings.Line1Settings.MoveNir`: NIR 이동 여부
- `WorkflowSettings.Line1Settings.MoveAllData`: 전체 데이터 이동 여부
- `WorkflowSettings.Line2Settings.*`: Line 2 설정 (동일 구조)

## Constraints

- **위치**: `skills_scripts/` 폴더에 별도로 관리 — ChronoView 프로젝트와 분리
- **실행**: Claude Agent에서 호출 가능해야 함
- **자동화 방식**: FlaUI UIA3 단일 방식 채택
- **목적**: 테스트 에이전트로 활용
- **파일 크기**: 각 파일 600줄 미만 유지

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| FlaUI UIA3 사용 | Microsoft UIA3 기반으로 WPF 앱 자동화에 최적화 | ✓ Good — v1.0 verified |
| skills_scripts/에 별도 관리 | ChronoView 코드와 분리하여 독립적 유지/배포 | ✓ Good — works well |
| CLI 인터페이스 | Agent가 호출하기 쉬운 표준 입출력 방식 | ✓ Good — v1.0 verified |
| ICommandHandler interface | Modular command registration pattern | ✓ Good — v1.1 verified |
| CommandRegistry class | Centralized handler aggregation | ✓ Good — enables 98% reduction |
| Pure migration approach | Original code copied verbatim for compatibility | ✓ Good — zero regressions |
| 13개 스킬 카테고리 | CLI 구조와 정렬된 카테고리로 발견성 향상 | ✓ Good — v1.5 verified |
| UPPER_SNAKE_CASE 스킬 네이밍 | CATEGORY_ACTION 형식으로 의도 전달 명확화 | ✓ Good — v1.5 verified |
| Record types for JSON | 불변성과 간결한 문법, JsonPropertyName 불필요 | ✓ Good — v1.5 verified |
| Levenshtein distance (threshold=3) | 유사한 스킬 이름 제안으로 오타 복구 | ✓ Good — v1.5 verified |
| --dry-run global option | 안전한 명령어 검증, 실행 없이 | ✓ Good — v1.5 verified |

---
*Last updated: 2026-01-22 after v1.5 milestone completion*
