# Milestone v1.3 Requirements

## Setup Automation & Test Reliability

---

## Active Requirements (This Milestone)

### SETUP-01: SetupWindow 전용 컨트롤러
- [x] ChronoSetupWindowController 클래스 생성 (~400 lines)
- [x] SetupWindow 찾기 (FindSetupWindow)
- [x] 설정 버튼 클릭 (ClickSettingsButton)
- [x] 모니터링 시작 버튼 클릭 (ClickStartButton)
- [x] 카메라 실행 버튼 클릭 (ClickGeneralCamera, ClickNir1, ClickNir2)
- [x] NIR 필터링 토글 (ToggleNirFiltering)
- [x] SetupWindow 닫기 (CloseWindow)

### SETUP-02: 설정 다이얼로그 열기 자동화
- [x] SetupWindow에서 설정 버튼 클릭하여 SettingsDialog 열기
- [x] SettingsDialog가 열렸는지 확인 (WaitForDialogOpen)
- [x] SettingsDialog 닫기 (CloseDialog)
- [x] 설정 변경 후 확인 버튼 클릭 (ClickSaveButton)

### SETUP-03: 완전한 셋업 완료 워크플로우
- [x] 카메라 실행 순서: General → NIR1 → NIR2
- [x] NIR 필터링 상태 확인 및 필요시 토글
- [x] 설정 확인 (데이터 시뮬레이터 설정과 비교)
- [x] 모니터링 시작 버튼 클릭
- [x] MainWindow로 전환 확인 (WaitForMainWindow)
- [x] 전체 CLI 명령: `setup complete-full`

### SETUP-04: 설정값 검증 (데이터 시뮬레이터 vs ChronoView)
- [x] 데이터 시뮬레이터 설정 파일 읽기 (simulator_config.json)
  - `source_line1`, `source_line2`, `target_base`, `move_folder`, `trash_folder`
- [x] ChronoView WorkflowPanel 경로 읽기 (Line1/Line2)
  - SampleName, MoveNir, MoveAllData
- [x] 설정값 비교 로직 구현
  - 폴더 경로 매칭 확인
  - 필수 설정값 존재 확인
- [x] CLI 명령: `setup verify-config` (JSON 출력)

### PERF-01: 테스트 속도 최적화
- [x] 불필요한 connectivity 체크 제거 (기존 명령어에서)
- [x] UI 자동화 명령어 직접 실행 (사전 체크 없음)
- [x] 병렬 실행 가능한 명령어 그룹화
- [x] 타임아웃 기본값 조정 (너무 긴 대기 시간 단축)

### PERF-02: 에이전트 문서 업데이트
- [x] test-executor.md에 셋업 워크플로우 추가
- [x] test-orchestrator.md에 설정 검증 단계 추가
- [x] CLI 명령 참조 테이블 업데이트

### CLI-01: 새로운 CLI 명령어
- [x] `setup open-settings` - SetupWindow에서 설정 열기
- [x] `setup complete-full` - 전체 셋업 완료 (카메라 → 설정 확인 → 시작)
- [x] `setup verify-config` - 설정값 검증
- [x] `setup camera-states` - 카메라 상태 확인

---

## Future Requirements (Deferred)

### SETUP-05: 자동 설정 동기화
- [ ] 데이터 시뮬레이터 설정을 ChronoView에 자동 적용
- [ ] 설정 불일치 시 자동修正 옵션

### PERF-03: 완전한 병렬 테스트
- [ ] 여러 테스트 시나리오 동시 실행
- [ ] 테스트 결과 병합

---

## Out of Scope

- ChronoView 소스 코드 수정 — 외부 자동화만
- 픽셀 기반 화면 인식 — UIA3 API만 사용
- 데이터 시뮬레이터 자체 수정 — 설정 읽기만

---

## Traceability

| REQ-ID | Phase | Status |
|--------|-------|--------|
| SETUP-01 | Phase 22 | ✅ Complete |
| SETUP-02 | Phase 22 | ✅ Complete |
| SETUP-03 | Phase 22 | ✅ Complete |
| SETUP-04 | Phase 22 | ✅ Complete |
| PERF-01 | Phase 23 | ✅ Complete |
| PERF-02 | Phase 23 | ✅ Complete |
| CLI-01 | Phase 23 | ✅ Complete |

---

*Created: 2026-01-20*
