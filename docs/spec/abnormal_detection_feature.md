# 이상치 탐지 기능 명세서

## 개요

이상치 탐지 기능은 통계적 z-score 기반 알고리즘을 사용하여 이미지 크기 이상과 데이터 그룹 이상을 자동으로 감지하는 기능입니다. 슬라이딩 윈도우 방식을 통해 최근 데이터의 분포를 분석하여 이상치를 판정합니다.

**업데이트**: 2025-01-06  
**관련 요구사항**: Requirements 5.3, 5.7, 5.8

---

## 목차

1. [기능 개요](#기능-개요)
2. [아키텍처](#아키텍처)
3. [핵심 알고리즘](#핵심-알고리즘)
4. [구현 상세](#구현-상세)
5. [UI 통합](#ui-통합)
6. [설정](#설정)
7. [사용 예시](#사용-예시)
8. [테스트](#테스트)
9. [향후 개선 사항](#향후-개선-사항)

---

## 기능 개요

### 주요 기능

1. **이미지 크기 이상 탐지**
   - 이미지의 가로/세로 크기를 분석하여 이상치 판정
   - z-score 기반 통계적 분석
   - 슬라이딩 윈도우로 최근 데이터 패턴 추적

2. **데이터 그룹 이상 탐지**
   - NIR-only 그룹 감지 (카메라 데이터 없이 NIR만 있는 경우)
   - 정상적인 촬영 프로세스 위반 감지

3. **실시간 적응**
   - 라인 조건 변화에 자동 적응
   - 초기 데이터 재평가 가능

### 탐지 기준

1. **이미지 크기 이상**
   - z-score 절대값이 임계값(기본 3.0)을 초과하는 경우
   - 가로 또는 세로 중 하나라도 임계값 초과 시 이상치로 판정

2. **NIR-only 그룹**
   - 카메라 파일이 없고 NIR 파일만 존재하는 그룹
   - 정상적인 촬영 프로세스에서는 카메라와 NIR이 함께 생성되어야 함

---

## 아키텍처

### 클래스 구조

```
IAbnormalDetector (인터페이스)
    └── AbnormalDetectorService (C# 구현)
    
AbnormalDetector (Python 구현 - 레거시)
```

### 의존성 주입

**위치**: `ChronoView/App.xaml.cs`

```csharp
services.AddSingleton<IAbnormalDetector, AbnormalDetectorService>();
```

### 주요 컴포넌트

1. **IAbnormalDetector** (`ChronoView/Core/Analytics/IAbnormalDetector.cs`)
   - 이상치 탐지 인터페이스
   - 이미지 크기 분석 및 그룹 이상 판정 메서드 정의

2. **AbnormalDetectorService** (`ChronoView/Core/Analytics/AbnormalDetectorService.cs`)
   - C# 구현체
   - z-score 계산 및 이상치 판정 로직

3. **FileGroupViewModel** (`ChronoView/UI/ViewModels/FileGroupViewModel.cs`)
   - UI 통합
   - `IsAbnormal` 속성으로 이상치 상태 표시

---

## 핵심 알고리즘

### Z-Score 계산

**공식**:
```
z-score = (value - mean) / std

여기서:
- value: 판정할 값 (이미지 가로/세로 크기)
- mean: 최근 데이터의 평균
- std: 최근 데이터의 표준편차
```

**판정 기준**:
- `|z| > threshold` (기본 3.0) → 이상치
- `|z| ≤ threshold` → 정상
- z = 0: 평균과 동일
- z = 1: 평균보다 1σ 위
- z = -2: 평균보다 2σ 아래

### 슬라이딩 윈도우

**동작 방식**:
1. 최근 N개(기본 100개) 데이터만 유지
2. 새 데이터가 들어오면 가장 오래된 데이터 제거
3. 항상 "최근 패턴"을 기준으로 판정

**장점**:
- 라인 조건이 서서히 변해도 자동 적응
- 초기 데이터가 나중에 재평가됨

**예시**:
```
시점 1 (10개): 평균=200, 값=220 → z=1.5 → 정상
시점 50 (50개): 평균=180, 값=220 → z=3.2 → 이상치!
                (분포가 명확해지면서 재평가)
```

### 최소 샘플 수

- 최소 샘플 수(기본 10개) 미만이면 판정 보류
- 충분한 데이터가 쌓일 때까지 판정하지 않음
- 판정 보류 시 z-score는 `null` 반환

---

## 구현 상세

### IAbnormalDetector 인터페이스

**위치**: `ChronoView/Core/Analytics/IAbnormalDetector.cs`

```csharp
public interface IAbnormalDetector
{
    /// <summary>
    /// Add image dimensions and check if abnormal
    /// </summary>
    /// <returns>Tuple of (isAbnormal, zScoreWidth, zScoreHeight)</returns>
    (bool IsAbnormal, double? ZScoreWidth, double? ZScoreHeight) AddAndCheckImage(int width, int height);

    /// <summary>
    /// Check if a file group is abnormal
    /// </summary>
    bool IsGroupAbnormal(FileGroup group);

    /// <summary>
    /// Reset the detection buffers
    /// </summary>
    void Reset();

    int WindowSize { get; set; }
    int MinSamples { get; set; }
    double Threshold { get; set; }
}
```

### AbnormalDetectorService 구현

**위치**: `ChronoView/Core/Analytics/AbnormalDetectorService.cs`

**주요 메서드**:

#### 1. AddAndCheckImage

```csharp
public (bool IsAbnormal, double? ZScoreWidth, double? ZScoreHeight) AddAndCheckImage(int width, int height)
```

**동작**:
1. 최소 샘플 수 미만이면 버퍼에 추가하고 판정 보류 (`false, null, null` 반환)
2. 이전 데이터를 기준으로 z-score 계산 (현재 값 제외)
3. z-score 계산 불가능 시 정상으로 간주
4. `|z| > threshold` 이면 이상치로 판정
5. 버퍼에 추가하고 윈도우 크기 유지

**반환값**:
- `IsAbnormal`: 이상치 여부
- `ZScoreWidth`: 가로 크기의 z-score (판정 보류 시 `null`)
- `ZScoreHeight`: 세로 크기의 z-score (판정 보류 시 `null`)

#### 2. IsGroupAbnormal

```csharp
public bool IsGroupAbnormal(FileGroup group)
```

**판정 기준**:
- NIR-only 그룹: 카메라 파일이 없고 NIR 파일만 존재하는 경우 → `true`
- 그 외: `false`

**구현 로직**:
```csharp
bool hasCamera = !string.IsNullOrEmpty(group.NormalFolder) || 
                (group.CameraFiles != null && group.CameraFiles.Count > 0);
bool hasNir = group.HasNir && !string.IsNullOrEmpty(group.NirKey);

if (!hasCamera && hasNir)
{
    return true; // NIR-only 그룹은 이상치
}
return false;
```

#### 3. CalculateZScore (내부 메서드)

```csharp
private double? CalculateZScore(double value, List<int> valuesList)
```

**동작**:
1. 리스트가 비어있거나 2개 미만이면 `null` 반환
2. 평균 계산: `sum(values) / len(values)`
3. 분산 계산: `sum((x - mean)^2) / len(values)`
4. 표준편차 계산: `sqrt(variance)`
5. 표준편차가 0이면 `null` 반환 (모든 값이 동일)
6. z-score 계산: `(value - mean) / std`

#### 4. MaintainWindowSize (내부 메서드)

```csharp
private void MaintainWindowSize()
```

**동작**:
- 버퍼 크기가 `WindowSize`를 초과하면 가장 오래된 데이터 제거

### 초기화 파라미터

```csharp
public AbnormalDetectorService(int windowSize = 100, int minSamples = 10, double threshold = 3.0)
```

- `windowSize`: 슬라이딩 윈도우 크기 (기본 100)
- `minSamples`: 최소 샘플 수 (기본 10)
- `threshold`: z-score 임계값 (기본 3.0 = 3σ)

---

## UI 통합

### FileGroupViewModel 통합

**위치**: `ChronoView/UI/ViewModels/FileGroupViewModel.cs`

**주요 속성**:

```csharp
private bool _isAbnormal;
private string? _abnormalReason;

public bool IsAbnormal 
{ 
    get => _isAbnormal || Status == GroupStatus.Abnormal; 
    private set { if (SetProperty(ref _isAbnormal, value)) OnPropertyChanged(nameof(StatusText)); } 
}

public string StatusText => IsAbnormal ? "Abnormal" : (IsGroupComplete() ? "Complete" : "Pending");
```

**이상치 확인 로직**:

```csharp
private void CheckAbnormalStatus()
{
    if (_abnormalDetector == null) return;
    try 
    { 
        IsAbnormal = _abnormalDetector.IsGroupAbnormal(_fileGroup);
        _abnormalReason = IsAbnormal ? "Detected" : null;
    } 
    catch 
    { 
        IsAbnormal = false; 
    }
}
```

**생성자에서 주입**:

```csharp
public FileGroupViewModel(
    FileGroup fileGroup, 
    IImageProcessor imageProcessor, 
    IMonitoringOrchestrator? orchestrator, 
    IAbnormalDetector? abnormalDetector,  // 주입
    ApplicationConfiguration? configuration, 
    ILogger<FileGroupViewModel>? logger, 
    Action<LogSeverity, string, string>? uiLog)
{
    _abnormalDetector = abnormalDetector;
    // ...
    CheckAbnormalStatus();
}
```

### DashboardViewModel 통합

**위치**: `ChronoView/UI/ViewModels/DashboardViewModel.cs`

**이상치 개수 집계**:

```csharp
public int AbnormalCount => FileGroups.Count(g => g.IsAbnormal);
```

### UI 표시

**DataGrid 행 스타일**:
- 이상치 그룹: **Light Yellow** (`#fff3cd`) 배경
- **Amber** (`#ffc107`) 왼쪽 테두리 (2px)

**Status 컬럼**:
- 이상치 그룹: "Abnormal" 표시

---

## 설정

### ApplicationConfiguration

**위치**: `ChronoView/Models/ApplicationConfiguration.cs`

```csharp
public class MatchingSettings
{
    /// <summary>
    /// Enable abnormal condition detection.
    /// </summary>
    public bool EnableAbnormalDetection { get; set; } = true;

    /// <summary>
    /// Z-score threshold for abnormal detection.
    /// </summary>
    public double ZScoreThreshold { get; set; } = 2.0;
}
```

**설정 파일 예시** (`config.json`):

```json
{
  "matchingSettings": {
    "enableAbnormalDetection": true,
    "zScoreThreshold": 2.0
  }
}
```

**참고**: 
- C# 구현에서는 `AbnormalDetectorService` 생성 시 `threshold` 파라미터로 설정
- 현재 설정 파일의 `zScoreThreshold`는 UI에서만 사용될 수 있음
- 실제 detector 인스턴스는 DI 컨테이너에서 생성 시 기본값(3.0) 사용

---

## 사용 예시

### 기본 사용 (이미지 크기 분석)

```csharp
var detector = new AbnormalDetectorService();

// 데이터가 실시간으로 들어올 때마다 판정
var result = detector.AddAndCheckImage(width: 200, height: 150);

if (result.IsAbnormal)
{
    Console.WriteLine($"이상치 감지! z-score: ({result.ZScoreWidth}, {result.ZScoreHeight})");
}
```

### 그룹 이상 판정

```csharp
var detector = new AbnormalDetectorService();

var group = new FileGroup
{
    GroupId = "group_001",
    HasNir = true,
    NirKey = "20240115_143022",
    NormalFolder = "", // 카메라 데이터 없음
    CameraFiles = new Dictionary<string, string>() // 카메라 파일 없음
};

bool isAbnormal = detector.IsGroupAbnormal(group);
// isAbnormal = true (NIR-only 그룹)
```

### 커스텀 설정

```csharp
// 더 엄격한 기준 (2σ)
var strictDetector = new AbnormalDetectorService(threshold: 2.0);

// 더 큰 윈도우 (200개)
var largeWindowDetector = new AbnormalDetectorService(windowSize: 200);

// 빠른 판정 (최소 5개)
var fastDetector = new AbnormalDetectorService(minSamples: 5);
```

### 상태 초기화

```csharp
detector.Reset(); // 버퍼 초기화
```

---

## 테스트

### 테스트 파일

**위치**: `ChronoView.Tests/Core/Analytics/AbnormalConditionDetectionPropertyTests.cs`

### 주요 테스트 케이스

1. **ZScoreCalculationShouldBeConsistent**
   - 동일한 입력에 대해 일관된 결과 생성 검증

2. **AbnormalDetectionShouldTriggerOnHighZScore**
   - 높은 z-score에서 이상치 감지 트리거 검증

3. **NormalVariationsShouldNotTriggerAbnormalDetection**
   - 정상적인 변동은 이상치로 감지되지 않음 검증

4. **NirOnlyGroupsShouldBeAbnormal**
   - NIR-only 그룹이 이상치로 감지되는지 검증

5. **GroupsWithCameraDataShouldNotBeAbnormal**
   - 카메라 데이터가 있는 그룹은 정상으로 판정되는지 검증

6. **DetectorShouldDeferJudgmentUntilMinSamples**
   - 최소 샘플 수 미만에서는 판정 보류 검증

7. **SlidingWindowShouldMaintainSizeLimit**
   - 슬라이딩 윈도우 크기 유지 검증

8. **ResetShouldClearAllState**
   - Reset 메서드가 모든 상태를 초기화하는지 검증

---

## 향후 개선 사항

### 1. MAD(Median Absolute Deviation) 방식

**현재**: 평균과 표준편차 기반 z-score  
**개선**: 중앙값과 MAD 기반 이상치 탐지

**장점**:
- 이상치가 많이 섞인 데이터에 더 강건
- 평균보다 중앙값이 이상치에 덜 민감

### 2. 그룹별 독립 Detector

**현재**: 단일 detector로 모든 그룹 분석  
**개선**: 라인1/라인2 별도 detector

**장점**:
- 라인별 특성에 맞는 정확한 판정
- 라인 간 조건 차이 자동 대응

### 3. 로그/통계 수집

**개선 사항**:
- 이상치 유형별 발생 빈도 추적
- z-score 분포 히스토그램
- 이상치 발생 패턴 분석

### 4. 동적 Threshold 조정

**현재**: 고정된 threshold 값  
**개선**: 데이터 품질에 따라 자동 조정

**방법**:
- 데이터 분포 분석
- 이상치 발생률 모니터링
- threshold 자동 최적화

### 5. 이미지 크기 분석 통합

**현재**: `AddAndCheckImage`는 있으나 실제로 사용되지 않음  
**개선**: FileGroup의 MainImage 크기를 분석하여 이상치 판정

**구현 방향**:
```csharp
// FileGroupViewModel에서 이미지 로드 시
var imageInfo = LoadImageInfo(group.MainImagePath);
if (imageInfo != null)
{
    var result = _abnormalDetector.AddAndCheckImage(imageInfo.Width, imageInfo.Height);
    if (result.IsAbnormal)
    {
        IsAbnormal = true;
        _abnormalReason = $"Image size abnormal (z: {result.ZScoreWidth:F2}, {result.ZScoreHeight:F2})";
    }
}
```

### 6. 설정 UI 추가

**개선 사항**:
- Settings Dialog에 이상치 탐지 설정 추가
- WindowSize, MinSamples, Threshold 조정 가능
- 실시간 적용 또는 재시작 필요 안내

---

## 참고 자료

- **Python 구현**: `script/services/abnormal_detector.py`
- **기존 문서**: `docs/modules/abnormal_detector.md`
- **UI 통합 설계**: `docs/spec/ui-service-integration-gaps/design.md`
- **테스트**: `ChronoView.Tests/Core/Analytics/AbnormalConditionDetectionPropertyTests.cs`

---

## 변경 이력

- **2025-01-06**: 종합 문서 작성
- **2025-12-04**: Python 구현 API 변경 (반환값 확장)
- **2025-12-03**: C# 구현 추가 및 UI 통합


