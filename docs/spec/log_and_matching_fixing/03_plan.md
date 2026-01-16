# 로그 및 매칭 수정 구현 계획 (03_plan)

## 목표
Line2 카메라(`cam4~cam6`)가 런타임에서 **제네릭 `DataType.Camera`로 뭉개지는 문제**를 해결하여,
- **로그에 카메라가 정확히 식별**되도록 한다. (Cam1~Cam6)
- 동일 그룹 내에서 Line2 카메라들이 **서로 “같은 슬롯을 공유하는 것처럼” 경쟁**하여 발생하는 **무로그 스킵 / 잘못된 그룹 배정**을 방지한다.
- 후보 탈락/스킵의 사유가 가능한 한 **UI 로그로 설명**되도록 한다.

## 설계 결정(전제 반영)
`CLAUDE.md`의 전제(“Cam4~6는 Line2의 Cam1~3이며 설정(지연 윈도우)을 공유”)를 반영하여,
- **설정(DataSequenceSettings)은 Cam1~3만을 기준**으로 유지한다. (UI가 Cam4~6를 숨기는 현재 구조와 일치)
- **표시/로그는 Line2일 때 Cam4~6로 표기**한다.
- 매칭에서 사용하는 “순서/지연 윈도우” 조회는 **Cam4~6를 Cam1~3으로 정규화(normalize)해서 조회**한다.

## 변경 범위(제안)

### ChronoView.Core
#### [MODIFY] `ChronoView/Core/FileWatching/GroupManager.cs`
- **타입 판정**: `DetermineDataTypeForGroup`가 `cam4~cam6`를 인식할 수 있어야 한다.
- **점유/슬롯 체크**: `HasDataType`가 `Cam4~Cam6`도 정확히 검사할 수 있어야 한다.
- **설정 공유 정규화**: 매칭 로직에서 `GetOrder / GetMinDelay / GetMaxDelay`를 조회할 때,
  - Line1: Cam1~Cam3 그대로 사용
  - Line2: (표시는 Cam4~Cam6이더라도) **조회는 Cam1~Cam3으로 매핑**하여 사용
  - 이를 위한 작은 헬퍼(예: `NormalizeForSequence(type, lineNumber)`)를 추가한다.
- **로그 가시성 개선(필요 시)**: 후보를 `HasDataType(...)=true`로 스킵하는 지점에 최소한의 UI 로그를 추가해,
  “왜 group_001을 보지 않고 넘어갔는지”가 추적 가능하게 한다. (요구사항 Q3)

#### [MODIFY] `ChronoView/Core/FileMatching/FileMatchingEngine.cs`
- **일관성 확보**: `HasDataTypeInGroup` 메서드도 `GroupManager.HasDataType`과 동일하게 `Cam4~Cam6` 케이스를 처리하도록 수정한다. (중복 위험 방지)

## 작업 순서(권장)
1. `DetermineDataTypeForGroup`에 `cam4~cam6` 케이스 추가
2. `HasDataType` (GroupManager) 및 `HasDataTypeInGroup` (FileMatchingEngine)에 `Cam4~Cam6` 케이스 추가
3. (핵심) 매칭에서 사용하는 타입을 “설정 공유 전제”에 맞게 정규화하여 `GetOrder/Delay` 조회
4. (선택) “무로그 스킵” 지점의 UI 로그 보강

## 검증 계획

### 수동 검증(재현 기반)
- **시나리오 A (로그 식별성)**:
  - Line2 카메라 파일(`cam4/cam5/cam6`) 유입 시 UI 로그에 `Camera`가 아니라 **Cam4~Cam6**가 출력되는지 확인
- **시나리오 B (문제 재현 케이스: group_001 스킵 방지)**:
  - Line2에서 `cam4`가 먼저 `group_001`에 들어간 상태에서 `cam5`가 들어올 때,
    - `group_001`이 “카메라가 이미 있음”으로 제네릭 점유 처리되어 스킵되지 않고,
    - `group_001`의 `cam5` 슬롯으로 정상 배정되는지 확인
- **시나리오 C (탈락/스킵 사유 로그)**:
  - 후보 그룹이 시간창/순서 조건으로 탈락하는 경우 `[매칭제외]` 로그가 남는지
  - 후보 그룹이 “이미 해당 타입이 존재”하여 스킵되는 경우에도 최소한의 설명 로그가 남는지(선택 적용 시)

---

## (선택) 탭별 UI 로그 표시 필터링
로그는 파일/내부 컬렉션에 **전부 저장**하되, 사용자가 보는 UI에서만 탭(Line1/Line2/통합)별로 필터링하여 표시한다.

- **규칙 정의**: `04_design.md`의 “부록 A. 탭별(UI) 로그 필터링 규칙”을 따른다.
- **UI 로그 필드 정책**: UI 로그는 사용자용이므로 Source는 제거하고, 대신 Line(1/2/공통)을 포함한다.
- **검증**:
  - 통합 탭에서 전체 로그가 보이는지
  - Line1 탭에서 Line1+공통만 보이는지
  - Line2 탭에서 Line2+공통만 보이는지
