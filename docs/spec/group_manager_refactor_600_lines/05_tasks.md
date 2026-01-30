# GroupManager 600줄 리팩토링 작업 목록 (05_tasks)

## 목표
- `ChronoView/Core/FileWatching/GroupManager.cs`를 **600줄 이하**로 축소
- **중복/쓸모없는 코드 제거**
- **SSOT(단일 진실원천)** 준수(규칙 레이어 분리/통일)
- 기능/동작/스레드 안전성/성능 **회귀 없음**

---

## 0) High-Risk 결정 게이트(먼저 확정)
> SSOT 통일이 곧 “동작 변경”이 될 수 있으므로, 아래 2개는 **결정 없이는 진행 금지**.

- [ ] **G0-1. HasDataType SSOT(엄격/느슨) 의도 확정**
  - 쟁점:
    - `GroupManager`: NIR 점유 = `HasNir`
    - `FileMatchingEngine`: NIR 점유 = `HasNir && NirKey not empty` (더 엄격)
    - Camera도 동일(키 존재만 vs 값 비어있지 않음)
  - 산출물:
    - “의도된 동작” 결정 문장(왜 이게 맞는지)
    - 그 결정을 고정하는 테스트/재현 로그
  - **완료 기준**: 이후 모든 SSOT는 이 결정에 맞춰 구현된다.

- [ ] **G0-2. Normal 폴더 타임스탬프 포맷 지원 범위 확정**
  - 쟁점:
    - `FileNamingHelper`: `yyMMddTHHmmss` 중심
    - `FileMatchingEngine`: `yyyyMMdd_HHmmss`도 추가 지원 흔적
  - 산출물:
    - 운영 데이터에서 해당 포맷이 실제 존재하는지 근거
    - SSOT(=FileNamingHelper)에서 지원할 포맷 범위 결정
  - **완료 기준**: SSOT 헬퍼의 스펙이 확정된다.

---

## Phase A — Log Formatter 분리(저위험, 빠른 라인 감소)
- [ ] **A1. `GroupManagerLogFormatter`(가칭) 파일 추가**
  - **완료 기준**: 컴파일 가능, 외부 공개 API 변경 없음
- [ ] **A2. `GroupManager`의 로그 문자열 조립 유틸 이동**
  - 이동 대상(우선):
    - `BuildTimeWindowRejectionReason`
    - `FormatHms`
    - `GetAnchorFileName`
    - `GetFileNameForDataTypeInGroup`
  - **완료 기준**: `GroupManager`에서 위 유틸이 제거되고 동일 로그가 생성됨(키워드 유지)
- [ ] **A3. 로그 키워드 유지 확인**
  - `[매칭성공]`, `[매칭제외]`, `[매칭실패]`, `[Eviction]`, `[LineDecision]`
  - **완료 기준**: 키워드 누락/변경 없음(필드 추가는 허용)

---

## Phase B — 규칙 레이어 SSOT(S1~S3) 분리

### S2 먼저: 라인별 카메라 타입 매핑 SSOT
- [ ] **B1. `LineCameraTypeMapper`(가칭) 도입**
  - 입력: `lineNumber`, `seqCameraType(Cam1~3)`
  - 출력: `groupCameraType(Cam1~6)`
  - **완료 기준**: `GroupManager.MapCameraTypeForLine` 제거 또는 위임으로 축소

- [ ] **B2. Line2 매핑 “동기화 방법” 명시 및 적용**
  - 대상 3곳을 같은 규칙으로 정렬:
    - `GroupManager`의 라인2 매핑(`MapCameraTypeForLine`)
    - `FileMatchingEngine`의 `camKeys=["cam4","cam5","cam6"]` + “내부 Cam1~3 해석” 전제
    - `InitialScanner`의 “Cam1 스캔 시 Cam4Path 포함” 류 정책
  - 방법(가이드):
    - “키 선택(캠 파일 dict key)”과 “DataType(설정/매칭 타입)”을 분리하고,
    - `LineCameraTypeMapper`를 SSOT로 두어 3곳이 동일한 매핑을 사용하도록 한다.
  - **완료 기준**: 3곳 중 어디에서도 임의/암묵 매핑이 남지 않는다(SSOT 호출로 통일).

### S1: 시퀀스 선/후행 도출 규칙 SSOT
- [ ] **B3. `SequenceRuleResolver`(가칭) 도입**
  - 입력: `orderedTypes`, `currentType(normalized)`, `CompareToReferenceCamera`
  - 출력: `(predecessorSeqType, successorSeqType)`
  - **완료 기준**: `GroupManager`/`FileMatchingEngine`의 선행 도출 규칙이 동일해진다(같은 구현 공유).

### S3: 타입별 기준 타임스탬프 SSOT
- [ ] **B4. `GroupTimestampResolver`(가칭) 도입**
  - 입력: `FileGroup`, `DataType`
  - 출력: `DateTime?`
  - **완료 기준**: 타입별 timestamp 결정 규칙이 단일화되고, `GroupManager.GetTimestampForDataType`는 위임/축소/제거된다.

- [ ] **B5. `FileMatchingEngine` 자체 파싱 제거(또는 SSOT 호출로 대체)**
  - 대상 후보:
    - `ExtractTimestampFromFolderName`
    - `ExtractTimestampFromNirKey`
  - **완료 기준**: 타임스탬프 파싱 규칙은 `FileNamingHelper`(SSOT)에서만 유지된다.

---

## Phase C — 정책(Policy) 분리(매칭/eviction)
- [ ] **C1. Eviction 정책 분리**
  - `CheckEvictionNeeded`를 `EvictionPolicy`(가칭)로 이동
  - **완료 기준**: `GroupManager`는 상태/락/이벤트 오케스트레이션 중심으로 단순화
- [ ] **C2. Matching 정책 분리(부분 추출부터)**
  - `FindMatchingExistingGroup`에서 아래부터 분리:
    - 후보 필터링/거절 사유 생성
    - 후보 정렬/선택
  - **완료 기준**: `GroupManager`의 거대 메서드 라인 수 감소 + 로직 동일

---

## Phase D — 청소/슬림화 + 600줄 달성
- [ ] **D1. `GroupManager` 내부 중복 메서드 통합/제거**
  - 파일명/타임/슬롯 관련 헬퍼 중복 제거(SSOT 위임)
  - **완료 기준**: 동일 목적 메서드가 2개 이상 존재하지 않음
- [ ] **D2. 사용되지 않는 private 메서드/필드 제거**
  - **완료 기준**: dead code 제거, 기능 영향 없음
- [ ] **D3. 최종 라인 수 체크**
  - **완료 기준**: `GroupManager.cs` **600줄 이하**

---

## 검증(필수) — 누락 보강
- [ ] **V1. 컴파일/기존 테스트 통과**
  - **완료 기준**: 빌드 실패/테스트 실패 없음
- [ ] **V2. 초기 스캔 결과 vs 실시간 매칭 결과 비교 테스트(신규)**
  - 목적: SSOT 통일 후에도 “초기/실시간 정책 불일치”가 생기지 않는지 확인
  - **완료 기준**: 동일 입력(동일 파일 셋)에서 초기/실시간이 같은 그룹 구성을 만든다(허용되는 차이가 있다면 명시).
- [ ] **V3. Line2 카메라 매핑 통일 검증(신규)**
  - Cam4~6가 올바르게 line2 그룹으로만 들어가고, 설정/시퀀스 규칙(Cam1~3 공유)이 유지되는지 확인
  - **완료 기준**: line2 데이터로 재현 시나리오/테스트 통과
- [ ] **V4. 핵심 수동 시나리오**
  - Normal 재처리(이미지 없음/발견)
  - 매칭 성공/실패/매칭제외(시차/순서/후행검증)
  - Eviction 발생 시 이동/이벤트
  - pending NIR 매칭

---

## 라인별 변수(키) SSOT 정리
- [ ] **K1. NIR/Normal 라인 키를 `nir1/nir2`, `normal1/normal2`로 고정**
  - **완료 기준**: 코드/문서/주석이 동일 키를 사용하고 혼재하지 않는다.




