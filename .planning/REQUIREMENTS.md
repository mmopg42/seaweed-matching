# Milestone v1.4 Requirements

## Test Agent Architecture & Reliability

---

## Active Requirements (This Milestone)

### ERROR-01: Exit Code 진단 및 해결
- [x] Exit Code 1이 발생하는 원인 파악
- [x] 에러 메시지 개선 (현재: "Error: Exit code 1"만 출력)
- [x] 어떤 명령어가 실패했는지 명확히 표시
- [x] 실패 사유를 설명하는 메시지 추가

### STATUS-01: Data Simulator Status Endpoint
- [x] `--status` CLI 옵션 추가
- [x] JSON 형식으로 상태 반환
  ```json
  {
    "status": "running|completed|idle|error",
    "progress": 85.5,
    "items_created": 376,
    "simulation_id": "uuid",
    "last_activity": "2026-01-21T18:30:45"
  }
  ```
- [x] 현재 실행 중인 시뮬레이션 ID 추적
- [x] 완료 여부를 불리언이 아닌 상태로 반환

### LOG-01: 동적 로그 경로 해결
- [x] `%APPDATA%\ChronoView\Logs\` 하위에서 최신 폴더 자동 탐색
- [x] 날짜별 폴더 `{YYYYMMDD}` 자동 검색 로직
- [x] log-analyst 에이전트에 동적 경로 해결 추가
- [x] Windows/WSL 경로 호환성 유지

### DELEGATE-01: Orchestrator 역할 분할 수정
- [ ] test-orchestrator가 직접 Bash 명령 실행하지 않도록 수정
- [ ] 실행 작업은 test-executor에게 위임
- [ ] 로그 분석은 log-analyst에게 위임
- [ ] orchestrator는 시나리오 정의와 결과 종합만 담당
- [ ] 위임 패턴 검증 테스트 통과

---

## Out of Scope

- ChronoView 소스 코드 수정 — 외부 자동화만
- 테스트 에이전트 완전 재작성 — 점진적 개선만

---

## Traceability

| REQ-ID | Phase | Status |
|--------|-------|--------|
| ERROR-01 | Phase 24 | Complete |
| STATUS-01 | Phase 25 | Complete |
| LOG-01 | Phase 26 | Complete |
| DELEGATE-01 | Phase 27 | Pending |

---

*Created: 2026-01-21*
