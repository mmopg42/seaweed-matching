# GroupManager 600줄 리팩토링 실행 계획 (03_plan)

## 1. 목표 재확인(What)
- `ChronoView/Core/FileWatching/GroupManager.cs`를 **600줄 이하**로 축소한다.
- **쓸모없거나 중복된 코드**를 제거/이동/통합한다.
- **SSOT(단일 진실원천)** 원칙을 만족시키도록 “규칙 레이어”를 분리한다.
- 기능/동작/스레드 안전성/성능은 **회귀 없이 유지**한다.

## 2. 핵심 전략(How)
리서치 결과, `GroupManager`는 “상태/락”과 “규칙/정책/유틸”이 한 파일에 혼재되어 있다.  
따라서 1차 목표는 **파일 분할**이 아니라 **SSOT가 되는 규칙 레이어를 먼저 빼고**, `GroupManager`는 **상태/락/이벤트 오케스트레이션**으로 축소한다.

### 2.1 단계 우선순위(라인 수 빠르게 줄이기 → SSOT 확립 → 정책 분리)
- **Phase A (빠른 라인 감소, 저위험)**: 로그 포매팅/문자열 조립 유틸 분리
- **Phase B (SSOT 핵심)**: 규칙 레이어(S1~S3) 분리 및 중복 제거
- **Phase C (정책 분리)**: eviction/matching 정책 컴포넌트 분리(락/상태는 GroupManager 소유 유지)
- **Phase D (청소/슬림화)**: 불필요/중복 제거, 남은 도우미 최소화, 600줄 달성 확인

## 3. 리팩토링 단계별 상세 계획

### Phase A — Log Formatter 분리(가장 빠른 라인 감소)
**목표**: `GroupManager`에서 로그 메시지 조립 유틸을 제거하여 즉시 라인을 줄인다.

- **A1. 신규 파일 추가**: `ChronoView/Core/FileWatching/GroupManagerLogFormatter.cs` (가칭)
  - 책임: `GroupManager`에서 호출하는 로그 메시지 생성 전담
  - 유지 조건: 기존 로그 키워드(`[매칭성공]`, `[매칭제외]`, `[매칭실패]`, `[Eviction]` 등) 보존(필드 추가는 허용)
- **A2. 이동 대상(예시)**:
  - `FormatHms`
  - `BuildTimeWindowRejectionReason`
  - `GetAnchorFileName`
  - `GetFileNameForDataTypeInGroup`
  - (가능하면) `[매칭실패]` 기준 문자열 조립
- **A3. 검증**
  - 컴파일/테스트 통과
  - 로그 키워드 유지 여부(스냅샷 비교는 수동로깅 확인 또는 간단 테스트로 대체)

**완료 기준**: `GroupManager`에서 “로그 전용 유틸”이 사라지고 라인이 유의미하게 감소한다.

---

### Phase B — 규칙 레이어 SSOT(S1~S3) 분리(핵심)
**목표**: “같은 규칙이 여러 곳에서 다르게 구현되는” 구조를 제거한다.

#### S1. 시퀀스 선/후행 도출 규칙(Sequence Interpretation)
- **배경**: `GroupManager`는 index 기반 + 참조카메라 옵션, `FileMatchingEngine`은 순회 기반으로 분산.
- **계획**
  - 신규 컴포넌트(가칭): `SequenceRuleResolver`
  - 입력: `orderedTypes`, `currentType(normalized)`, `CompareToReferenceCamera`
  - 출력: `predecessorSeqType`, `successorSeqType`
  - `GroupManager`와 `FileMatchingEngine`가 동일 로직을 호출하도록 유도(단계적으로 적용)

#### S2. 라인별 카메라 타입 매핑 규칙(Line Camera Mapping)
- **배경**: `GroupManager`는 `MapCameraTypeForLine`, `FileMatchingEngine`은 암묵 전제, `InitialScanner`는 path 스캔 묶음으로 분산.
- **계획**
  - 신규 컴포넌트(가칭): `LineCameraTypeMapper`
  - 입력: `lineNumber`, `seqCameraType(Cam1~3)`
  - 출력: `groupCameraType(Cam1~6)`
  - `GroupManager.MapCameraTypeForLine`을 여기로 이동하고, `InitialScanner`의 “Cam1 스캔에 Cam4Path 혼합” 같은 분산 정책을 정리할 근거를 마련

#### S3. 타입별 기준 타임스탬프 결정 규칙(Timestamp Source)
- **배경**: `GroupManager`는 타입별 timestamp 추출을 우선, `FileMatchingEngine`은 `g.Timestamp` 중심 비교가 섞임.
- **계획**
  - 신규 컴포넌트(가칭): `GroupTimestampResolver`
  - 입력: `FileGroup`, `DataType`
  - 출력: `DateTime?`
  - 구현은 `FileNamingHelper`를 SSOT로 사용(필요 시 helper 기능 확장)

**검증(중요)**:
- Phase B는 기능 회귀 위험이 있으므로,
  - “규칙 로직의 결과가 동일”한지 **로그 재현 시나리오** 또는 **단위 테스트**로 고정한다.
  - 특히 `HasDataType`/타임스탬프 추출/Line2 매핑은 초기/실시간 결과 차이가 발생할 수 있으므로 변경 범위를 작게 쪼갠다.

---

### Phase C — 정책(Policy) 분리(매칭/eviction)
**목표**: `GroupManager`를 “상태+락+이벤트 오케스트레이션”으로 축소한다.

- **C1. Eviction 분리**
  - `CheckEvictionNeeded`를 `EvictionPolicy`(가칭)로 이동
  - 입력은 스냅샷/설정/새 그룹(필수)로 제한
  - `GroupManager`는 “락 안에서 호출”하는 구조 유지(스레드 안전성 제약)
- **C2. Matching 분리(부분 추출)**
  - `FindMatchingExistingGroup` 전체를 한 번에 옮기지 말고, 먼저 아래부터 분리:
    - 후보 필터링/거절 사유 생성
    - 후보 정렬/선택
  - `GroupManager`는 스냅샷 생성/라인 필터/락/상태 갱신만 담당

**검증**:
- 기존 로그의 키워드/의미 유지
- 라인 분리/순서성(`_lastAssignedGroup`) 의미 유지

---

### Phase D — 청소/슬림화 및 600줄 달성
- 사용되지 않는 private 메서드/중복 메서드 제거
- `GroupManager` 파일 내 “도메인 유틸” 잔여분 최소화
- 최종적으로 `GroupManager.cs` **600줄 이하** 확인

## 4. 검증 전략(How to be safe)
- **정적 검증**: 컴파일 + 기존 테스트 통과
- **시나리오 검증(수동)**:
  - Normal 재처리(이미지 없음/발견) 로그/동작
  - Cam 매칭 성공/실패/매칭제외(시차/순서/후행검증) 로그
  - Eviction 발생 시 그룹 이동/이벤트
  - pending NIR 매칭

## 5. 작업 순서(권장)
1) Phase A로 빠르게 라인 감소 및 “로그 유틸 SSOT” 정리  
2) Phase B에서 S2(라인 매핑) → S1(시퀀스 해석) → S3(타임스탬프) 순으로 SSOT화  
3) Phase C에서 eviction → matching 순으로 정책 분리  
4) Phase D에서 정리/삭제로 600줄 달성

## 6. 리스크 및 대응
- **R-락/동시성 회귀**: 락 구조 변경 최소화, “메서드 이동/추출” 위주로 진행
- **R-초기/실시간 정책 불일치 고착**: Phase B에서 규칙 레이어를 SSOT로 먼저 빼서 확산을 막는다
- **R-포맷 호환(타임스탬프)**: `FileMatchingEngine`이 지원하는 포맷을 `FileNamingHelper`로 흡수할지 데이터 근거 확인 후 결정


