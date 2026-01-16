# GroupManager 600줄 리팩토링 요구사항서

## 1. 배경 / 문제정의
`ChronoView/Core/FileWatching/GroupManager.cs`가 600줄 원칙을 크게 초과(약 1,100줄)하여,
- 변경 시 영향 범위를 파악하기 어렵고
- 테스트하기 어려우며
- 동일/유사 로직이 여러 위치에 중복되어 SSOT(단일 진실원천) 원칙을 위반할 위험이 크다.

본 작업은 “코드를 더 예쁘게”가 아니라 **불필요/중복 제거 + 책임 분리**를 통해 **정량 목표(600줄 이하)**를 달성하는 리팩토링이다.

## 2. 목표 (Goals)
- **G1. 파일 길이 목표**: `GroupManager.cs`를 **600줄 이하**로 축소한다.
- **G2. SSOT 준수**: “같은 규칙/계산/판정”은 **한 곳에서만 정의**되도록 중복을 제거한다.
- **G3. 기능 보존**: 현재 동작(그룹 생성/업데이트/삭제, 라인 분리, 매칭, eviction, pending NIR, 중복처리, 로그/이벤트)을 유지한다.
- **G4. 가독성/유지보수성**: 책임(Concern) 단위로 파일/타입을 분리하고, 핵심 플로우를 읽기 쉽게 만든다.

## 3. 제약사항 (Constraints)
- **C1. 외부 계약 유지**:
  - `IGroupManager` 공개 API, 이벤트(`GroupCreated/Updated/Removed`, `Log`) 시그니처 유지
  - UI 로그 포맷의 핵심 키워드(`[매칭성공]`, `[매칭제외]`, `[매칭실패]`, `[Eviction]`, `[LineDecision]` 등) 유지(필드 추가는 허용)
- **C2. 스레드 안전성 유지**:
  - 기존 락 전략(`_lockObject`, `lock(existingGroup)` 등) 훼손 금지
  - `_activeGroups`의 동시성/스냅샷 접근 패턴 유지
- **C3. 성능 악화 금지(회귀 방지)**:
  - 매 파일 이벤트 처리에서 불필요한 FS 접근/정규식/파싱 반복을 늘리지 않는다.
  - 기존 캐시(`ITimestampCache`)의 이점을 유지한다.
- **C4. 런타임 동작 불변 우선**: 구조/이동/삭제는 하되, 로직의 의미를 바꾸는 변경은 별도 스펙으로 분리한다.

## 4. 성공 조건 / 수용 기준 (Acceptance Criteria)
- **A1. 라인 수**: `GroupManager.cs`가 **600줄 이하**다(주석 포함).
- **A2. SSOT**:
  - 타임스탬프 추출 규칙은 `FileNamingHelper` 등 **단일 헬퍼**로 통일되어 있고,
  - Normal 폴더 라인 결정은 `NormalFolderHelper` 등 **단일 헬퍼**를 사용하며,
  - “매칭 규칙(순서/허용시차/후행검증)”의 공통 계산은 가능한 한 **단일 모듈**로 모인다.
- **A3. 중복 제거**: 동일 목적의 함수가 `GroupManager`와 다른 엔진/헬퍼에 중복 구현되어 있지 않다(아래 ‘조사 항목’에서 정의).
- **A4. 행동 보존**: 기본 시나리오에서 그룹 생성/업데이트/eviction/매칭 실패가 기존과 동일하게 발생한다.
- **A5. 테스트/정적검사**: 기존 테스트가 통과하며(가능한 범위 내), 수정한 파일에 새 린트/컴파일 에러가 없다.

## 5. 비목표 (Non-Goals)
- 매칭 알고리즘 자체 변경(예: 우선순위 규칙 변경, eviction 기준 변경)
- 로그 레벨/로그 시스템 전체 구조 변경(단, 중복 유틸 정리는 가능)
- UI/뷰모델 구조 개선
- 파일 감시/스캐닝 방식의 변경

## 6. 달성해야 하는 “조건” (What must remain true)
- **라인 분리 보장**: Line1/Line2 간 데이터 섞임 금지(필터/키/경로 규칙 유지).
- **그룹 ID/순서성 규칙 유지**: `_lastAssignedGroup`를 통한 “컬럼 내 순서성”의 의미를 바꾸지 않는다.
- **NIR 처리 유지**:
  - NIR leader 허용 여부에 따른 pending 큐 동작 유지
  - pending NIR 매칭 윈도우 로직 유지
- **Normal 이미지 재처리 동작 유지**: stitched 이미지 존재 여부에 따른 재처리 스킵/진행 로직 유지
- **Eviction 플로우 유지**: “victim clone -> victim reset+merge -> evicted group 등록 -> 이벤트 발행” 순서 유지

## 7. 조사/확인이 필요한 것들 (Investigate / To Find)
요구사항 달성을 위해 아래 항목을 “찾아서” 중복/불필요를 판정해야 한다(리서치 문서에서 근거/결론 기록 예정).

### 7.1 SSOT 후보(중복 가능성이 높은 로직)
- **타임스탬프 추출**
  - 후보 SSOT: `ChronoView/Helpers/FileNamingHelper.cs`
  - 확인할 것: `GroupManager` 내부의 `GetTimestampForDataType`, `CreateGroupFromSingleFile` 등에서 파싱/추출이 중복되는지, 일관성이 깨질 위험이 있는지
- **Normal 폴더 라인 판정**
  - 후보 SSOT: `ChronoView/Helpers/NormalFolderHelper.cs`
  - 확인할 것: `GroupManager.DetermineLineNumber`가 Normal 외 타입(NIR/Camera)도 함께 처리하며 책임이 과도한지, 라인 판정 로직을 분리할 수 있는지
- **매칭 엔진(순수 로직)**
  - 후보 SSOT: `ChronoView/Core/FileMatching/FileMatchingEngine.cs` (설명: “stateless, independently testable”)
  - 확인할 것: 실시간 매칭(`GroupManager`)과 초기/배치 매칭(`FileMatchingEngine`)이 같은 규칙을 공유해야 하는 범위(공통 모듈로 뽑을 수 있는지)

### 7.2 쓸모없거나 제거 가능한 코드의 후보
- 사용되지 않는 private 메서드/분기/로깅(Trace/Debug) 과다
- 같은 정보를 두 번 저장/계산하는 필드/캐시
- “UI 로그 포맷 유틸”처럼 `GroupManager` 핵심 책임(그룹 상태 관리)과 무관한 문자열 조립 코드

### 7.3 책임 분리 단위(어떤 파일/클래스로 뺄지)
- **라인 결정/경로 규칙**: `LineResolver`(가칭) 혹은 기존 Helper 확장
- **매칭 후보 평가/거절 사유 생성**: `GroupMatchingPolicy`(가칭)
- **Eviction 판단**: `EvictionPolicy`(가칭)
- **로그 메시지 조립(비즈니스 로직과 분리)**: `GroupManagerLogFormatter`(가칭)
  - 조건: 기존 로그 키워드 유지, 필드 추가는 가능

### 7.4 변경 안전장치(회귀 위험 포인트)
- 락 범위 변경으로 인한 레이스/데드락
- `_activeGroups` 스냅샷 사용 위치 변경으로 인한 일관성 문제
- 타임스탬프 기준(그룹 앵커 vs 타입별 파일) 변경으로 인한 매칭 결과 변화

## 8. 산출물 (Deliverables)
- `docs/spec/group_manager_refactor_600_lines/01_requirements.md` (본 문서)
- (후속) 리서치 문서: 중복/불필요 근거, SSOT 선택 근거, 분리 설계안
- (후속) 코드 변경: `GroupManager.cs` 600줄 이하 달성 + 관련 신규 파일/클래스 추가


