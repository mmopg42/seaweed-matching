# 로그 및 매칭 수정 설계 (04_design)

## 1. 개요
`GroupManager`가 Line2 카메라(`cam4~cam6`)를 충분히 구분하지 못해 `DataType.Camera`로 떨어지는 경우,
- UI 로그가 “Camera”로 뭉개지고
- 그룹 매칭에서 “이미 카메라가 존재함”으로 판단되어 후보가 **무로그로 스킵**될 수 있다.

본 설계는 다음을 달성한다:
- 로그/표시에서 Cam4~Cam6가 명확히 드러나도록 개선
- Line2 카메라가 서로 “제네릭 카메라 슬롯”을 공유하지 않도록 분리
- 동시에, 설계 전제(**Cam4~6는 Cam1~3의 Line2 버전이며 설정을 공유**)와 현재 설정 UI 구조( Cam4~6 숨김 )에 맞게 동작

## 2. 핵심 설계 결정: “표시 타입”과 “설정/매칭 타입” 분리
현재 코드베이스에는 두 가지 흐름이 공존한다:
- **실시간 매칭/UI 갱신 흐름**: `GroupManager` (CreateOrUpdateGroupAsync → FindMatchingExistingGroup)
- **초기/배치 매칭 흐름**: `FileMatchingEngine` (Line2의 cam4~6를 Cam1~3으로 매핑해서 처리하는 흔적 존재)

따라서 본 수정에서는 타입을 두 층으로 다룬다.

### 2.1 표시/로그 타입 (Display Type)
- UI 로그에서 “Cam4/Cam5/Cam6”으로 보여주기 위한 타입
- `GroupManager.GetFriendlyColumnName`은 이미 `DataType.Cam4~Cam6`를 지원한다.

### 2.2 설정/매칭 타입 (Sequence Type)
- `DataSequenceSettings`의 Order/Delay를 조회하는 용도의 타입
- 설계 전제에 따라 **Line2 카메라는 Cam1~3 설정을 공유**해야 하므로,
  - Line1: Cam1~Cam3 그대로 사용
  - Line2: Cam4~Cam6를 **Cam1~Cam3으로 정규화**하여 설정을 조회

이를 위해 “정규화 헬퍼”를 추가한다(구현은 `GroupManager` 내부 private method로 충분):
- 예: `NormalizeForSequence(DataType type, int lineNumber)`  
  - Line2 && type == Cam4 → Cam1  
  - Line2 && type == Cam5 → Cam2  
  - Line2 && type == Cam6 → Cam3  
  - 그 외 그대로 반환

## 3. 상세 설계

### 3.1 `GroupManager.DetermineDataTypeForGroup` 수정 (표시 타입 판정)
#### 문제
현재 구현은 `cam1~cam3`만 검사하고 나머지는 `DataType.Camera`로 폴백한다.

#### 변경
`cam4~cam6`도 명시적으로 검사하여 `DataType.Cam4~Cam6`를 반환하도록 확장한다.

### 3.2 `GroupManager.HasDataType` 수정 (슬롯 점유 체크)
#### 문제
현재 구현에는 `Cam4~Cam6` 케이스가 없다.
또한 `DataType.Camera => group.CameraFiles.Count > 0`는 “카메라가 하나라도 있으면 점유”로 판정하여,
타입이 `DataType.Camera`로 뭉개질 때 Line2 카메라들이 서로 경쟁하는 근본 원인이 된다.

#### 변경
- `Cam4~Cam6` 케이스 추가 (`cam4/cam5/cam6` 키 존재 여부)
- 가능하면 `DataType.Camera`는 “디버그/레거시” 성격으로만 남기고,
  실 매칭에서는 CamX 타입이 사용되도록(DetermineDataTypeForGroup가 더 이상 Camera로 떨어지지 않도록) 한다.

### 3.3 `FindMatchingExistingGroup` 내부에서의 설정 조회 정규화
#### 문제
`FindMatchingExistingGroup`는 다음을 수행한다:
- 새 데이터의 타입을 구함 (`DetermineDataTypeForGroup`)
- 그 타입으로 `GetPriority/GetMinDelay/GetMaxDelay`를 조회해 선후행/윈도우를 계산

그런데 설정 UI는 Cam4~Cam6를 숨기고 저장하지 않기 때문에,
Line2에서 타입이 Cam4~Cam6로 “정상 구분”되면 오히려 설정 조회가 실패(기본값)할 수 있다.

#### 변경
- “표시 타입”은 `Cam4~Cam6`를 유지하되,
- `GetPriority/GetMinDelay/GetMaxDelay`에 들어가는 타입은 `NormalizeForSequence()`로 정규화한 타입을 사용한다.

> 결과적으로 Line2 카메라는 “표시는 Cam4~6”, “설정은 Cam1~3 공유”라는 전제가 코드에 반영된다.

### 3.4 “무로그 스킵”의 가시성 개선(요구사항 Q3)
#### 문제
후보 루프에서 `if (HasDataType(candidate, newGroupType)) continue;`가 UI 로그 없이 스킵된다.
사용자는 group_001이 왜 배제됐는지 추적이 어렵다.

#### 변경(권장)
continue 전에 최소한의 UI 로그를 추가한다.
- 예: `[매칭제외] {colName}: {fileName} -> {candidateId} 제외 (사유: 이미 {type} 존재)`

### 3.5 `FileMatchingEngine.HasDataTypeInGroup` 동기화
#### 문제
`GroupManager.HasDataType`만 수정하면 초기 스캔(`FileMatchingEngine`)과 실시간 스캔(`GroupManager`) 간 동작 불일치가 발생한다.

#### 변경
`FileMatchingEngine.HasDataTypeInGroup` 메서드에도 `Cam4~Cam6` 케이스를 동일하게 추가하여 일관성을 유지한다. (중복 위험 방지)

## 4. 리스크 및 호환성
- **코드 중복 위험**: `GroupManager`와 `FileMatchingEngine`에 유사한 로직(`HasDataType`, `DetermineDataType`)이 중복되어 있다. 추후 공통 유틸리티(`DataTypeResolver` 등)로 추출하는 것을 고려해야 한다.
- **스파게티 코드 위험**: `FindMatchingExistingGroup` 메서드가 이미 160라인을 초과하여 복잡하다. 본 변경(정규화, 로그 추가)으로 더 복잡해질 수 있으므로, 기능 검증 후 리팩토링이 필요할 수 있음을 인지한다.
> [!CAUTION]
> `FindMatchingExistingGroup` 추가 변경 시 400 LoC 가이드라인(파일 사이즈) 준수에 주의.
- **타입 분리(표시 vs 설정) 누락 시**: Line2 카메라가 Cam4~6로 구분되더라도 설정 조회가 꼬여 매칭이 불안정해질 수 있다.
- **초기/배치 매칭과의 일관성**: `FileMatchingEngine` 업데이트를 통해 초기/실시간 동작을 일치시킨다.

## 5. 검증/테스트
### 5.1 수동 시나리오(필수)
- Line2에서 cam4가 들어간 `group_001`에 cam5가 뒤이어 들어올 때,
  - group_001이 “이미 카메라 있음”으로 스킵되지 않고 정상 배정되는지
  - UI 로그에 “Camera”가 아니라 “Cam4/Cam5”로 표시되는지

### 5.2 자동 테스트(권장)
- `GroupManager`의 매칭 핵심 로직은 현재 클래스 내부 상태(그룹 딕셔너리)에 의존하므로 단위테스트 작성이 까다로울 수 있다.
  가능하다면 “정규화 함수”와 “HasDataType/DetermineDataTypeForGroup”에 대해 최소 단위테스트를 추가한다.

## 6. 작업 항목(Tasks)
1. `DetermineDataTypeForGroup`: cam4~cam6 인식 추가
2. `HasDataType`: Cam4~Cam6 케이스 추가
3. `FindMatchingExistingGroup`: 설정 조회 정규화 및 `NormalizeForSequence` 헬퍼 구현
4. `FileMatchingEngine.HasDataTypeInGroup`: Cam4~Cam6 케이스 추가 (일관성)
5. (선택) “HasDataType로 인한 continue”에 UI 로그 추가

---

## 부록 A. 탭별(UI) 로그 필터링 규칙 (Phase 2로 연기)
> 본 내용은 별도 스펙 또는 Phase 2에서 다루기로 결정됨. (관심사 분리 및 범위 관리)
