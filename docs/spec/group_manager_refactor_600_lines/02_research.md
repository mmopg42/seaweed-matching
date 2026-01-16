# GroupManager 600줄 리팩토링 리서치 (02_research)

> 갱신: 2026-01-14  
> 메모: 본 문서는 “제안/권고/가설”이 아니라 **실제 코드에서 확인된 사실**과 **구현 간 불일치(정의 차이)**만 기록한다.

## 1. 범위/대상
- **대상 파일(핵심)**: `ChronoView/Core/FileWatching/GroupManager.cs` (현재 약 1,160줄 수준)
- **중복/SSOT 관련 파일(근거 수집)**:
  - `ChronoView/Helpers/FileNamingHelper.cs`
  - `ChronoView/Helpers/NormalFolderHelper.cs`
  - `ChronoView/Core/FileMatching/FileMatchingEngine.cs`
  - `ChronoView/Core/FileWatching/InitialScanner.cs`
  - `ChronoView/Core/FileWatching/FolderTimestampCache.cs` (`ITimestampCache`)

## 1.1 변화 요약(재리서치 트리거)
- `GroupManager.FindMatchingExistingGroup`에서 아래 로직이 확인된다:
  - `orderedTypes`의 **인덱스(currentIndex) 기반 선/후행 결정**
  - `CompareToReferenceCamera` 옵션에 따른 **참조 카메라(예: Cam2/3 → Cam1) 강제**
  - Line2 대응을 위한 **카메라 타입 매핑(`MapCameraTypeForLine`)**
  - 전방 매칭 시 **후행 타입 보유 그룹만 후보**로 인정
  - 후방 매칭 시 **(NIR 제외) 선행 타입 부재 후보를 즉시 제외**
  - (추가로) 후방 매칭에서 “후행 검증(successor validation)” 루프가 존재

## 2. 현재 관찰: GroupManager가 “너무 많은 책임”을 가진다
`GroupManager`는 단일 파일 안에서 아래 관심사를 동시에 수행한다:
- **상태 관리**: `_activeGroups`, `_pendingNirFiles`, `_processedFiles`, `_lastAssignedGroup`
- **스레드/락 관리**: `_lockObject`, `lock(existingGroup)` 등의 동시성 제어
- **입력 해석**: 파일/폴더 → `FileGroup` 템플릿 생성(`CreateGroupFromSingleFile`)
- **라인 판정**: `DetermineLineNumber` (Normal/NIR/Camera 모두 포함)
- **매칭 정책**: `FindMatchingExistingGroup` (전/후행, 윈도우, 후보정렬, 로그)
- **Eviction 정책**: `CheckEvictionNeeded`
- **타임스탬프/타입 유틸**: `DetermineDataTypeForGroup`, `NormalizeForSequence`, `GetTimestampForDataType`, `HasDataType`
- **시퀀스/라인 매핑 유틸**: `MapCameraTypeForLine` (Line2 카메라 매핑)
- **로그 메시지 조립 유틸**: (최근 추가된) `FormatHms`, `GetAnchorFileName`, `GetFileNameForDataTypeInGroup`, `BuildTimeWindowRejectionReason` 등

결과적으로 “600줄 원칙” 위반뿐 아니라, **SSOT 관점에서 같은 판단/파싱/슬롯체크가 여러 군데 존재**하여 회귀 위험이 커진다.

## 3. 중복/SSOT 위반 맵(핵심 발견)
아래는 “같은 개념이 어디에 중복 구현되었고, 구현이 서로 다르게 정의되어 있는지”를 정리한다.

### 3.1 타임스탬프 추출 (Timestamp Extraction)
- **중복 위치**
  - `FileNamingHelper`: `ExtractTimestamp*` 계열(정규식 기반, Normal/Camera/NIR)
  - `FileMatchingEngine`: `ExtractTimestampFromFolderName`, `ExtractTimestampFromNirKey` (자체 정규식/파싱)
  - `GroupManager`: `CreateGroupFromSingleFile`, `GetTimestampForDataType` 에서 `FileNamingHelper.ExtractTimestamp(...)`를 여러 경로로 호출
  - `InitialScanner`: `FileNamingHelper.ExtractTimestamp(...)` 사용

- **구현 차이(코드로 확인됨)**
  - Normal 폴더 포맷 지원 범위:
    - `FileNamingHelper.ExtractTimestampFromNormalFolderName`: `yyMMddTHHmmss` 기반
    - `FileMatchingEngine.ExtractTimestampFromFolderName`: `yyMMddTHHmmss` + `yyyyMMdd_HHmmss` 두 패턴 파싱 로직이 존재
  - NIR 키 파싱:
    - `FileNamingHelper`는 파일명에서 `(\d{8}T\d{6})`만 뽑아 `yyyyMMddTHHmmss`로 파싱
    - `FileMatchingEngine`는 `run_\d(\d{8}T\d{6})` 등 더 명시적 케이스를 갖는다

- **추가 확인 필요(코드만으로는 판정 불가)**
  - 실제 입력 데이터에 `CYYYYMMDD_HHMMSS` 형태 Normal 폴더가 존재하는지(존재 여부는 코드만으로 확정 불가)

### 3.2 “그룹이 특정 DataType을 이미 갖고 있는가” (Slot Occupancy / HasDataType)
- **중복 위치**
  - `GroupManager.HasDataType(FileGroup, DataType)`
  - `FileMatchingEngine.HasDataTypeInGroup(FileGroup, DataType)`

- **구현 차이(코드로 확인됨)**
  - NIR 판정:
    - `GroupManager`: `DataType.NIR => group.HasNir`
    - `FileMatchingEngine`: `DataType.NIR => group.HasNir && !string.IsNullOrEmpty(group.NirKey)`
  - 카메라 판정의 엄격성:
    - `FileMatchingEngine`는 `ContainsKey` + `값이 비어있지 않음`까지 확인
    - `GroupManager`는 대체로 `ContainsKey`만 확인

- **파생 사실(정의 차이로 인해 발생)**
  - 동일한 `FileGroup` 상태라도 “점유(Has)” 판정 결과가 엔진별로 달라질 수 있다(정의가 다름).
  - 따라서 초기/배치(`FileMatchingEngine`)와 실시간(`GroupManager`)에서 “후보 제외/허용” 조건이 동일하다고 단정할 수 없다.

- **추가 확인 필요(코드만으로는 판정 불가)**
  - NIR/Camera 점유의 “의도된 정의”가 무엇인지(요구/운영 관점 결정 필요)

### 3.3 라인 판정 (Line Decision)
- **중복 위치/연관**
  - `NormalFolderHelper.DetermineLineNumber`: Normal 폴더 라인 판정 SSOT 후보
  - `FileGroup.GetLineNumberFromNormalFolder`: `NormalFolderHelper` 래핑(간접 중복)
  - `GroupManager.DetermineLineNumber`: Normal 외에도 NIR/Camera까지 포함하는 “거대 라인 판정”
  - `InitialScanner`: Normal은 `NormalFolderHelper.IsValidNormalFolder`로 필터링, Cam은 Cam1에 Cam4까지 묶는 특수 처리(라인/타입 매핑 정책이 흩어짐)

- **구현 사실(코드로 확인됨)**
  - Normal 폴더 라인 판정 함수가 `NormalFolderHelper.DetermineLineNumber`에 존재한다.
  - `GroupManager.DetermineLineNumber`는 Normal 외에도 NIR/Camera 경로 기반 라인 판정을 포함한다.
  - `InitialScanner.ScanFilesForDataTypeAsync`는 카메라 스캔에서 일부 타입에 대해 Line2 경로를 함께 스캔하는 분기가 존재한다.

### 3.4 시퀀스 해석/참조카메라 옵션/라인2 카메라 매핑 (Sequence Interpretation)
- **중복/분산 위치**
  - `GroupManager.FindMatchingExistingGroup`:
    - `orderedTypes` + `currentIndex`로 선/후행 계산
    - `CompareToReferenceCamera` 옵션 적용
    - `MapCameraTypeForLine`로 Line2 실데이터 타입(Cam4~6)으로 변환
  - `FileMatchingEngine`:
    - `orderedTypes`를 순회하며 매칭하고,
    - `CompareToReferenceCamera` 옵션을 별도 분기로 적용하며,
    - Line2는 `camKeys = ["cam4","cam5","cam6"]`를 사용하되, **설정 타입은 Cam1~3 중심으로 해석**(주석/구현에 “line2가 Cam1~3으로 내부 매핑”이라는 전제가 존재)
  - `InitialScanner`:
    - Cam1 스캔 시 Cam4Path도 포함(동일 정책이 또 다른 형태로 존재)

- **불일치(중요)**
  - “선행/후행 타입 결정” 알고리즘이 서로 다르다:
    - `GroupManager`: index 기반(현재 타입이 시퀀스에 없으면 매칭 자체를 중단)
    - `FileMatchingEngine`: 순회 기반(항목별로 직전 타입을 기본 선행으로 사용, 옵션 시 Cam2/3만 Cam1 참조)
  - “기준 타임스탬프” 사용이 다르다:
    - `GroupManager`: 선행/후행 타입의 **타입별 타임스탬프**를 우선 사용(`GetTimestampForDataType`)
    - `FileMatchingEngine`: 다수 경로에서 **group.Timestamp(그룹 앵커)** 기반 비교가 섞인다(구현상 `timeDiff = file.Timestamp - g.Timestamp`)
  - Line2 매핑이 서로 다른 형태로 구현되어 있다:
    - `GroupManager`: 명시적 매핑 함수(`MapCameraTypeForLine`)
    - `FileMatchingEngine`: 라인별 키(cam4~6) + “내부적으로 Cam1~3 설정을 사용”이라는 전제(암묵적 매핑)
    - `InitialScanner`: Cam1에 Cam4Path를 섞는 방식(암묵적 매핑)

- **추가 확인 필요(코드만으로는 판정 불가)**
  - 초기/배치와 실시간에서 “동일 입력 → 동일 그룹 구성”이 요구사항인지(동일성 기준 정의 필요)

## 4. GroupManager 내부 중복(“파일 안에서도” 중복이 있다)
최근 로그 명확화 작업 등으로 `GroupManager` 내부에 아래 유틸이 늘어났다:
- `GetFileNameForGroup(FileGroup, DataType)` (기존)
- `GetFileNameForDataTypeInGroup(FileGroup, DataType)` (최근 추가)
- `GetAnchorFileName(FileGroup)` (최근 추가)

이는 “로그를 위해 파일명을 얻는다”라는 동일 목적의 함수가 2~3개로 쪼개져 **내부 SSOT도 깨질 위험**이 있다.

**구현 사실(코드로 확인됨)**
- `GroupManager` 내부에 “파일명 추출/표시 목적”의 메서드가 복수 존재한다:
  - `GetFileNameForGroup`
  - `GetFileNameForDataTypeInGroup`
  - `GetAnchorFileName`

## 4.1 “Camera”는 어디서 나오나? (사용처/의미 리서치)
코드베이스에는 **제네릭 타입 `DataType.Camera`**가 정의되어 있고, 일부 코드 경로에서 반환/표시된다.

- **정의**
  - `ChronoView/Models/DataSequenceSettings.cs`의 `public enum DataType`에 `Camera`가 포함됨(“Generic camera category”).
- **표시/로그 경로**
  - `GroupManager.GetFriendlyColumnName(DataType.Camera, …)`는 `"Camera"`를 반환 → UI 로그에 “Camera”가 찍힐 수 있음.
- **생성 경로(대표)**
  - `GroupManager.DetermineDataTypeForGroup`는 Cam1~Cam6 키가 없으면 `DataType.Camera`로 폴백.
  - `FileNamingHelper.IdentifyDataType`는 카메라 파일 패턴이면 `DataType.Camera` 반환(특정 Cam1~6까지는 구분하지 않음).

- **추가 확인 필요(코드만으로는 판정 불가)**
  - 사용자가 관찰한 “Camera가 잘못 생성되어 쓰인다” 케이스가 실제로 어느 입력/경로에서 발생하는지(재현 로그/샘플 필요)

## 4.2 라인별 변수(키) 혼재 문제: nir1/nir2, normal1/normal2
요구사항대로 NIR/Normal은 라인별로 2개가 필요하며, 키를 **명시적으로 2개**로 통일해야 한다.

- **현재 관찰**
  - `FileMatchingEngine`는 라인별 키를 `nir1/nir2`, `normal1/normal2`로 사용한다.
  - 반면 `UnmatchedFiles`의 주석은 `nir, nir2` / `normal, normal2`처럼 혼재되어 있어 오해를 유발한다.

**구현 사실(코드로 확인됨)**
- `FileMatchingEngine`에서 라인 키는 `nir1/nir2`, `normal1/normal2`로 하드코딩되어 있다.
- `UnmatchedFiles`의 주석은 과거에 `nir, nir2` / `normal, normal2` 표현이 혼재되어 있었고, 이는 코드 사용 키와 불일치할 수 있다.

## 5. 미확정/추가 확인 필요 목록(코드만으로 결론 불가)
- **Has 점유 정의**: NIR/Camera의 “점유”를 무엇으로 정의하는 것이 요구사항에 맞는지(현재 구현이 엔진별로 다름)
- **Normal 폴더 포맷**: `CYYYYMMDD_HHMMSS` 형태가 실제 입력에 존재하는지
- **“Camera” 폴백 재현**: `DataType.Camera`가 실제로 UI/매칭에 나타나는 재현 케이스(로그/샘플 필요)


