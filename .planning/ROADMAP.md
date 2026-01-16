# Roadmap: ChronoView UI Automation

## Overview

FlaUI.UIA3 기반으로 ChronoView WPF 데스크톱 애플리케이션을 자동화하는 스킬 세트와 테스트 에이전트를 개발합니다. UI 요소 식별/조작 인프라부터 시작하여 점진적으로 모든 주요 기능을 자동화하고, 최종적으로는 Agent가 호출 가능한 CLI 인터페이스와 테스트 에이전트를 구현합니다.

## Domain Expertise

None (Windows UI Automation with FlaUI)

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: FlaUI 인프라** - FlaUI 5.x 기반 C# CLI 도구 빌드 및 기반 구축
- [ ] **Phase 2: 윈도우 탐지** - ChronoView 메인 윈도우 및 대화상자 식별
- [ ] **Phase 3: 툴바 제어** - 시작/중지/설정/새로고침 버튼 자동화
- [ ] **Phase 4: 데이터 패널** - StatisticsPanel, FileGroupDataGrid 상태 읽기
- [ ] **Phase 5: 워크플로우 제어** - WorkflowPanel 카메라/경로 설정 자동화
- [ ] **Phase 6: 설정 대화상자** - SettingsDialog 자동화
- [ ] **Phase 7: 로그 모니터링** - LogPanel 실시간 로그 읽기
- [ ] **Phase 8: 파일 작업** - 이동/ 삭제 작업 자동화
- [ ] **Phase 9: CLI 인터페이스** - Agent 호출 가능한 명령줄 인터페이스
- [ ] **Phase 10: 테스트 에이전트** - 스킬을 사용하는 자동화 테스트 에이전트

## Phase Details

### Phase 1: FlaUI 인프라
**Goal**: FlaUI 5.x 기반 C# CLI 도구를 빌드 가능한 상태로 만들고 기반을 구축
**Depends on**: Nothing (first phase)
**Research**: Unlikely (FlaUI 패턴은 이미 문서화됨, 기존 코드 존재)
**Plans**: 2 plans

Plans:
- [x] 01-01: FlaUI 5.x 호환성 수정 및 빌드 성공
- [x] 01-02: 기본 UIAutomation 클래스 메서드 검증

### Phase 2: 윈도우 탐지
**Goal**: ChronoView 메인 윈도우와 모든 대화상자를 식별
**Depends on**: Phase 1
**Research**: Likely (WPF 윈도우 구조 및 AutomationId 파악 필요)
**Research topics**: WPF UI Automation 속성, FlaUI로 WPF 요소 식별 방법
**Plans**: 3 plans

Plans:
- [ ] 02-01: MainWindow 식별 및 속성 파악
- [ ] 02-02: 대화상자(Setup, Settings, ImagePreview) 식별
- [ ] 02-03: 윈도우 탐지 스킬 메서드 구현

### Phase 3: 툴바 제어
**Goal**: 툴바 버튼(시작/중지/설정/새로고침) 자동화
**Depends on**: Phase 2
**Research**: Unlikely (Command 바인딩 패턴은 이미 파악됨)
**Plans**: 4 plans

Plans:
- [ ] 03-01: 시작(StartCommand) 버튼 식별 및 클릭
- [ ] 03-02: 중지(StopCommand) 버튼 식별 및 클릭
- [ ] 03-03: 설정/새로고침 버튼 식별 및 클릭
- [ ] 03-04: 툴바 제어 스킬 메서드 구현

### Phase 4: 데이터 패널
**Goal**: StatisticsPanel, FileGroupDataGrid 상태 읽기
**Depends on**: Phase 2
**Research**: Likely (DataGrid 데이터 접근 방법 필요)
**Research topics**: WPF DataGrid UI Automation 패턴, 셀 값 읽기
**Plans**: 3 plans

Plans:
- [ ] 04-01: StatisticsPanel 통계 값 읽기
- [ ] 04-02: FileGroupDataGrid 구조 파악
- [ ] 04-03: FileGroupDataGrid 데이터 추출

### Phase 5: 워크플로우 제어
**Goal**: WorkflowPanel 카메라 설정/모니터링 경로 설정 자동화
**Depends on**: Phase 2
**Research**: Likely (WorkflowPanel 구조 파악 필요)
**Research topics**: WorkflowPanel UI 요소 구조, 경로 입력 방식
**Plans**: 3 plans

Plans:
- [ ] 05-01: WorkflowPanel 구조 파악 및 요소 식별
- [ ] 05-02: 카메라 설정 변경 자동화
- [ ] 05-03: 모니터링 경로 설정 자동화

### Phase 6: 설정 대화상자
**Goal**: SettingsDialog 자동화
**Depends on**: Phase 2
**Research**: Likely (설정 대화상자 구조 파악 필요)
**Research topics**: SettingsDialog UI 계층 구조, 설정 값 변경 패턴
**Plans**: 2 plans

Plans:
- [ ] 06-01: SettingsDialog 열기 및 구조 파악
- [ ] 06-02: 주요 설정 값 변경/확인 자동화

### Phase 7: 로그 모니터링
**Goal**: LogPanel 실시간 로그 읽기
**Depends on**: Phase 2
**Research**: Likely (TextBox/TextPattern 로그 접근 필요)
**Research topics**: LogPanel 구조, 실시간 텍스트 변경 감지
**Plans**: 2 plans

Plans:
- [ ] 07-01: LogPanel 구조 파악 및 로그 읽기
- [ ] 07-02: 로그 필터링 및 실시간 모니터링

### Phase 8: 파일 작업
**Goal**: 이동/ 삭제 작업 자동화
**Depends on**: Phase 4
**Research**: Unlikely (Command 패턴은 Phase 3에서 확인됨)
**Plans**: 2 plans

Plans:
- [ ] 08-01: FileGroup 선택 및 이동 작업 자동화
- [ ] 08-02: 삭제 작업 자동화 및 확인

### Phase 9: CLI 인터페이스
**Goal**: Agent가 호출 가능한 명령줄 인터페이스 구현
**Depends on**: Phase 3, Phase 4
**Research**: Unlikely (System.CommandLine 패턴은 이미 사용 중)
**Plans**: 3 plans

Plans:
- [ ] 09-01: 스킬 명령 설계 및 CLI 구조 정의
- [ ] 09-02: 각 스킬 명령 구현
- [ ] 09-03: CLI 테스트 및 문서화

### Phase 10: 테스트 에이전트
**Goal**: 스킬을 사용하는 자동화 테스트 에이전트 구현
**Depends on**: Phase 9
**Research**: Likely (에이전트 아키텍처 결정 필요)
**Research topics**: Claude Agent SDK 사용법, 또는 별도 Python 에이전트
**Plans**: 3 plans

Plans:
- [ ] 10-01: 에이전트 아키텍처 설계
- [ ] 10-02: 테스트 시나리오 구현
- [ ] 10-03: 에이전트 테스트 및 검증

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|------------|
| 1. FlaUI 인프라 | 2/2 | Complete | 2026-01-16 |
| 2. 윈도우 탐지 | 0/3 | Not started | - |
| 3. 툴바 제어 | 0/4 | Not started | - |
| 4. 데이터 패널 | 0/3 | Not started | - |
| 5. 워크플로우 제어 | 0/3 | Not started | - |
| 6. 설정 대화상자 | 0/2 | Not started | - |
| 7. 로그 모니터링 | 0/2 | Not started | - |
| 8. 파일 작업 | 0/2 | Not started | - |
| 9. CLI 인터페이스 | 0/3 | Not started | - |
| 10. 테스트 에이전트 | 0/3 | Not started | - |
