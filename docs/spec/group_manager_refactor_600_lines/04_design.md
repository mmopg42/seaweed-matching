# GroupManager 600줄 리팩토링 설계 (04_design)

## 1. 설계 목표
- `GroupManager`를 **상태/락/이벤트 오케스트레이션** 중심으로 축소한다.
- “규칙/정책/유틸”을 분리하여 **SSOT(단일 진실원천)**을 달성한다.
- 초기/배치(`FileMatchingEngine`)와 실시간(`GroupManager`)의 **규칙 동일성**을 유지/강화한다.

## 2. 핵심 설계 원칙(SSOT)
- **동일한 규칙은 한 곳에서만 구현**한다(선/후행 도출, 라인2 카메라 매핑, 타입별 타임스탬프 등).
- `GroupManager`가 락/상태를 소유하므로, 규칙 컴포넌트는 가능한 한 **순수 함수(Pure)**로 설계한다.

## 3. “Camera”의 의미(명확화)
코드베이스에는 `DataType.Camera`(Generic camera category)가 존재한다.
- 정의: `ChronoView/Models/DataSequenceSettings.cs`의 `public enum DataType { …, Camera }`
- 사용처(대표):
  - `GroupManager.DetermineDataTypeForGroup`의 폴백 반환값
  - `GroupManager.GetFriendlyColumnName`에서 `"Camera"`로 표시
  - `FileNamingHelper.IdentifyDataType`가 카메라 파일 패턴이면 `DataType.Camera` 반환
  - `FileWatcherService.GetSensorPriority`에서 `detectedType`가 카메라일 때 우선순위로 취급

**설계 결론(가이드)**:
- `DataType.Camera`는 “특정 Cam1~Cam6로 분류되지 않은 카메라”의 **폴백**이다.
- 요구사항/리서치 관점에서 `DataType.Camera`가 UI/로그/매칭에 나타나는 것은 대체로
  - “분류에 필요한 정보(라인/카메라 인덱스)가 부족”하거나,
  - “정책/구현 불일치로 인해 폴백이 새는”
  신호일 수 있으므로, 리팩토링 단계에서 **발생 경로를 축소/격리**해야 한다.

## 4. 라인별 키(variables) SSOT: nir1/nir2, normal1/normal2
현재 “라인별 NIR/Normal”은 **2개가 필요**하며, 키는 다음으로 통일한다:
- **NIR**: `nir1`, `nir2`
- **Normal**: `normal1`, `normal2`
- **Camera**: `cam1`~`cam6`

> 주의: `FileMatchingEngine`는 이미 `nir1/nir2`, `normal1/normal2`를 사용한다.  
> 반면 `UnmatchedFiles`의 주석이 `nir, nir2`/`normal, normal2`처럼 혼재되어 있어 문서/코드 일관성이 깨질 수 있다.

## 5. 신규 컴포넌트 설계(인터페이스/의존성)

### 5.1 `IGroupManagerLogFormatter` (로그 포매터)
**목적**: `GroupManager`에서 “문자열 조립”을 제거하고 라인을 줄인다.

```csharp
namespace ChronoView.Core.FileWatching;

public interface IGroupManagerLogFormatter
{
    string FormatMatchExcluded(
        string columnName,
        string fileName,
        string candidateGroupId,
        string reason,
        int lineNumber);

    string FormatMatchFailed(string columnName, string fileName, string basis, int lineNumber);
    string FormatMatchSuccess(string columnName, string fileName, string groupId, string reason, string targetName, double absDiffSeconds, int lineNumber);
    string FormatEviction(string columnName, string fileName, string occupiedGroupId, string movedGroupId, int lineNumber);
}
```

의존성:
- 입력은 문자열/숫자만 받게 하고, `FileGroup` 의존은 최소화(순수 포맷 유지)

### 5.2 `ISequenceRuleResolver` (시퀀스 선/후행 도출 규칙, SSOT: S1)
**목적**: index 기반 + reference camera 옵션 규칙을 단일화한다.

```csharp
namespace ChronoView.Core.FileWatching;

public interface ISequenceRuleResolver
{
    (DataType? predecessorSeqType, DataType? successorSeqType) Resolve(
        IReadOnlyList<DataType> orderedTypes,
        DataType normalizedCurrentType,
        bool compareToReferenceCamera);
}
```

### 5.3 `ILineCameraTypeMapper` (라인2 카메라 매핑, SSOT: S2)
**목적**: Cam1~3(설정/시퀀스) ↔ Cam4~6(라인2 실제) 변환 규칙 단일화.

```csharp
namespace ChronoView.Core.FileWatching;

public interface ILineCameraTypeMapper
{
    DataType MapSeqToLineType(DataType seqCameraType, int lineNumber);
    DataType NormalizeLineToSeqType(DataType lineCameraType);
}
```

### 5.4 `IGroupTimestampResolver` (타입별 기준 타임스탬프, SSOT: S3)
**목적**: “무슨 타입을 기준으로 비교할지”의 기준 타임스탬프 산출을 단일화한다.

```csharp
namespace ChronoView.Core.FileWatching;

public interface IGroupTimestampResolver
{
    DateTime? TryResolve(FileGroup group, DataType type);
}
```

구현 가이드:
- `FileNamingHelper`를 SSOT로 사용하고, 필요 시 포맷 확장(`yyyyMMdd_HHmmss` 지원 여부 포함)을 여기서 흡수한다.

### 5.5 `IGroupDataSlots` (HasDataType/파일명/빈값 규칙 SSOT)
**목적**: `GroupManager.HasDataType`와 `FileMatchingEngine.HasDataTypeInGroup`의 불일치를 제거한다.

```csharp
namespace ChronoView.Core.FileWatching;

public interface IGroupDataSlots
{
    bool Has(FileGroup group, DataType type);
    string GetFileName(FileGroup group, DataType type);
}
```

## 6. High-Risk 결정(필수 게이트)
SSOT 통일 전에 아래 결정을 “의도된 동작”으로 확정해야 한다.

### 6.1 HasDataType(NIR/Camera) 엄격성
- 선택지 A(느슨): `HasNir == true`면 점유로 본다
- 선택지 B(엄격): `HasNir && NirKey 비어있지 않음`이 점유다
- 카메라 슬롯도 동일(키 존재만 vs 값 비어있지 않음)

**결정 절차(권고)**:
- 초기 스캔 결과 vs 실시간 매칭 결과를 비교하는 테스트/로그 재현으로 현재 행동을 고정
- “어느 쪽이 버그/의도”인지 운영 데이터/요구사항으로 결정

### 6.2 Normal 폴더 포맷 지원 범위
- `CYYMMDDTHHMMSS(_N)`만 유지할지
- `CYYYYMMDD_HHMMSS`까지 지원할지(현재 `FileMatchingEngine`에는 구현 흔적 존재)

## 7. 의존성 방향(권장)
- `GroupManager` → (규칙/포맷 인터페이스) → 구현체
- `FileMatchingEngine`도 동일 규칙 구현체를 재사용하도록 조정(가능하면)
- 규칙 구현체는 `Models`(FileGroup/DataType)와 `Helpers(FileNamingHelper)`에만 의존


