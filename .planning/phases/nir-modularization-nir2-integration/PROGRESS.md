# NIR Modularization & NIR2 Integration - 진행 상황

**계획 파일**: `C:\Users\redli\.claude\plans\parallel-roaming-karp.md`

**시작일**: 2026-01-29
**마일스톤**: v1.6
**최종 업데이트**: 2026-01-29

---

## 개요

1. **기존 NIR1 로직 모듈화**: Line 1 NIR 관련 코드를 별도 모듈로 분리하여 삭제하기 쉽게 만듦
2. **NIR2 구현**: NIR2도 동일한 인터페이스/함수명으로 구현하여 호환성 확보
3. **NIR2 기능**: API 기반 실시간 데이터 수집, CSV 저장, 덩어리 감지, FileGroup 연결

---

## Phase 진행 상황

| Phase | 내용 | 상태 | 완료일 |
|-------|------|------|--------|
| Phase 0 | FileMatchingEngine 분리 (1072줄 → 600줄 미만) | ✅ 완료 | - |
| Phase 1 | NIR 인터페이스 정의 (INirMatcher, INirDataProvider, INirDisplayHandler) | ✅ 완료 | 2026-01-29 |
| Phase 2 | NIR1 모듈로 분리 (Refactoring) | ✅ 완료 | 2026-01-29 |
| Phase 3 | FileMatchingEngine에서 NIR 매칭 분리 | ✅ 완료 | 2026-01-29 |
| Phase 4 | NIR2 데이터 모델 및 설정 | ✅ 완료 | 2026-01-29 |
| Phase 4.5 | CSV 파일 관리 (Nir2CsvManager) | ✅ 완료 | 2026-01-29 |
| Phase 5 | NIR2 Data Collector | ✅ 완료 | 2026-01-29 |
| Phase 6 | NIR2 Provider & Matcher | ✅ 완료 | 2026-01-29 |
| Phase 7 | UI - NIR2 파싱 버튼 | ✅ 완료 | 2026-01-29 |
| Phase 8 | Nir2DataCollector 통합 | ✅ 완료 | 2026-01-29 |
| Phase 9 | UI 표시 (임시) | ✅ 완료 | 2026-01-29 |
| Phase 10 | 한국어 로컬라이제이션 | ✅ 완료 | 2026-01-29 |
| Phase 11 | Glossary 업데이트 | ✅ 완료 | 2026-01-29 |

**진행률: 11/11 Phases 완료 (100%)**

---

## Phase 4.5 완료 내용 ✅

### 생성된 파일
- `Core/NIR/Line2/Nir2CsvManager.cs` (245줄)

### 기능 상세
| 메서드 | 설명 |
|--------|------|
| `CreateNewCsvFile()` | 타임스탬프 기반 파일 생성 (`sensor_data_yyyyMMdd_HHmmss.csv`) |
| `WriteRow(Nir2Sample)` | 단일 샘플 기록 (버퍼링: 50샘플마다 플러시) |
| `WriteRows(IEnumerable)` | 대량 쓰기 지원 |
| `CloseFile()` | 파일 닫기 (항상 플러시 보장) |
| `IsFileOpen` | 파일 열림 상태 확인 |
| `CurrentFilePath` | 현재 CSV 파일 경로 |

### 설계 결정
**버퍼링 전략**: 100ms 폴링 간격 × 50 버퍼 = 5초마다 디스크 I/O
- 장점: I/O 성능 ~50배 향상 (10초당 100회 → 2회 플러시)
- 안전성: 정상 종료 시 `CloseFileInternal()`에서 항상 플러시
- 데이터 손실 위험: 비정상 종료 시 최대 5초분 (50샘플) 손실 가능

### 스레드 안전성
- 모든 파일 작업이 `lock (_lock)`으로 보호됨
- `Dispose()` 패턴 구현 (finalizer 포함)

---

## Phase 5 완료 내용 ✅

### 생성된 파일

#### 1. `Nir2ChunkDetector.cs` (159줄)
상태 머신 기반 덩어리 감지기

| 상태 | 설명 |
|------|------|
| `Idle` | 덩어리가 없는 상태 |
| `Collecting` | 샘플 수집 중 (Presence == 1) |
| `Complete` | 덩어리 완료 |

**상태 전이**:
- `Idle → Collecting`: Presence가 2→1로 변화할 때
- `Collecting → Complete`: Presence가 1→2로 변화할 때
- `Complete → Idle`: 자동 전이

| 기능 | 설명 |
|------|------|
| `ProcessSample(Nir2Sample)` | 샘플 처리 및 상태 전이 |
| `ChunkCompleted` 이벤트 | 덩어리 완료 시 알림 |
| `Reset()` | 상태 머신 리셋 |
| `GetDiagnosticInfo()` | 디버깅용 상태 정보 |

#### 2. `Nir2DataCollector.cs` (362줄)
API 폴링 기반 데이터 수집기

| 상태 | 설명 |
|------|------|
| `Stopped` | 수집 중지 |
| `Starting` | 시작 중 |
| `Running` | 활성 수집 중 |
| `Stopping` | 중지 중 |

| 기능 | 설명 |
|------|------|
| `StartAsync()` | 수집 시작 (CSV 파일 생성, 상태 리셋) |
| `StopAsync()` | 수집 중지 (CSV 파일 닫기) |
| `RunCollectionLoopAsync()` | 메인 폴링 루프 |
| `PollApiAsync()` | API 호출 + 재시도 정책 |
| `StateChanged` 이벤트 | 상태 변경 알림 |
| `ChunkDetected` 이벤트 | 덩어리 감지 알림 |

### 재시도 정책 (지수 백오프)
```csharp
private static int CalculateBackoffDelay(int errorCount)
{
    const int baseDelay = 100;  // 100ms 기본
    const int maxDelay = 5000;  // 5초 최대
    int delay = baseDelay * (int)Math.Pow(2, Math.Min(errorCount, 6));
    return Math.Min(delay, maxDelay);
}
```

| 시도 | 지연 시간 |
|------|----------|
| 1회차 | 100ms |
| 2회차 | 200ms |
| 3회차 | 400ms |
| 4회차 | 800ms |
| 5회차 | 1600ms |
| 6회차+ | 5000ms (최대) |

### 통계
- `SamplesCollected`: 수집된 샘플 수
- `ChunksDetected`: 감지된 덩어리 수
- `CurrentCsvPath`: 현재 CSV 파일 경로

---

## Phase 1 완료 내용

### 생성된 인터페이스
- `Core/NIR/Interfaces/INirMatcher.cs` - NIR 매칭 인터페이스
- `Core/NIR/Interfaces/INirDataProvider.cs` - NIR 데이터 로드 인터페이스
- `Core/NIR/Interfaces/INirDisplayHandler.cs` - NIR UI 표시 인터페이스
- `Core/NIR/Shared/NirAggregationStrategy.cs` - 덩어리 연산 전략 enum

---

## Phase 2 완료 내용

### Core/NIR/Shared/ (공통 유틸리티)
| 파일 | 설명 |
|------|------|
| `INirFileResolver.cs` | NIR 파일 리졸버 인터페이스 |
| `NirSpectrumParser.cs` | 스펙트럼 파싱 (버그 수정: protein/moisture 변수 추가) |
| `SpcTxtNirFileResolver.cs` | .spc/.txt 파일 세트 리졸버 |
| `NirGraphGenerator.cs` | ScottPlot 그래프 생성 |
| `NirSpectrumFilter.cs` | 5-criteria 필터링 |
| `NirAggregationStrategy.cs` | First, Second, Median, Last, Mean |

### Core/NIR/Line1/ (Line1 구현체)
| 파일 | 설명 |
|------|------|
| `FileBasedNirProvider.cs` | 파일 기반 데이터 제공 (INirDataProvider 구현) |
| `FileBasedNirMatcher.cs` | 파일 기반 매칭 (INirMatcher 구현) |
| `NirDisplayHandler.cs` | NIR 그래프 표시 (INirDisplayHandler 구현) |

### 네임스페이스 업데이트
다음 파일에서 `ChronoView.Core.Nir` → `ChronoView.Core.NIR.Shared`로 변경:
- `App.xaml.cs`
- `NirFilteringService.cs`
- `FileGroupViewModel.cs`
- `GroupManager.cs`
- `SetupWindowViewModel.cs`
- `InitialScanner.cs`
- `MonitoringOrchestrator.cs`
- `FileGroupMediaLoader.cs`

### 커밋
```
commit 2b731c3
refactor(nir): Modularize NIR code into NIR.Shared and NIR.Line1
```

---

## Phase 3 완료 내용

### 수정된 파일
- `Core/FileMatching/FileMatchingEngine.cs`
  - `INirMatcher` 파라미터 추가
  - `BuildLineGroups()` 및 `BuildLineGroupsLegacy()`에 `INirMatcher` 전달
  - `INirMatcher`를 사용하여 NIR 매칭 수행 (fallback 로직 보존)
- `Core/FileMatching/FileGroupMatcherService.cs`
  - `INirMatcher` 의존성 주입 추가
  - `FileMatchingEngine.MatchFiles()`에 `INirMatcher` 전달
- `App.xaml.cs`
  - `INirMatcher` → `FileBasedNirMatcher` DI 등록
  - `using ChronoView.Core.NIR.Line1;` 및 `using ChronoView.Core.NIR.Interfaces;` 추가

### 리팩토링 내용
- `FileMatchingEngine`의 NIR 매칭 로직을 `INirMatcher.MatchNirToGroups()` 호출로 교체
- `INirMatcher`가 null일 경우 기존 인라인 로직을 fallback으로 사용
- NIR1/NIR2 구현체를 쉽게 교체할 수 있는 구조 확보

---

## Phase 4 완료 내용

### 생성된 파일
- `Core/NIR/Line2/Nir2Sample.cs` (70줄)
  - API에서 받은 NIR2 샘플 데이터 모델
  - 속성: Timestamp, Protein, Moisture, Presence
  - 유효성 검사 메서드 포함
- `Core/NIR/Line2/Nir2Chunk.cs` (160줄)
  - 덩어리 모델 (샘플 컬렉션)
  - Aggregation 전략별 연산 메서드 (First, Second, Median, Last, Mean)
- `Core/Configuration/Nir2Settings.cs` (101줄)
  - NIR2 런타임 설정 모델
  - 속성: ApiUrl, CsvDirectory, PollingInterval, AggregationStrategy, HttpTimeout, MaxRetries, IsEnabled

### 수정된 파일
- `Models/ApplicationConfiguration.cs`
  - `Nir2Settings` 속성 추가
  - `using ChronoView.Core.Configuration;` 추가
- `Core/Configuration/DefaultConfiguration.cs`
  - NIR2 기본값 주석 추가

---

## Phase 6 완료 내용 ✅

### 생성된 파일

#### 1. `ApiBasedNirProvider.cs` (185줄)
API 기반 NIR2 데이터 제공자 (INirDataProvider 구현)

| 기능 | 설명 |
|------|------|
| `RegisterChunk(chunkId, chunk)` | 완료된 덩어리를 캐시에 등록 |
| `LoadNirData(filePath)` | 캐시에서 덩어리 데이터 로드 (format: "chunk:{chunkId}") |
| `LoadNirDataAsync(filePath)` | 비동기 로드 |
| `GenerateThumbnailAsync()` | NIR2는 텍스트 표시 사용하므로 null 반환 |
| `GetCachedChunkIds()` | 캐시된 덩어리 ID 목록 |
| `RemoveChunk(chunkId)` | 캐시에서 덩어리 제거 |
| `ClearCache()` | 모든 캐시 비우기 |
| `CachedChunkCount` | 캐시된 덩어리 수 |

**데이터 변환**: Nir2Chunk → NirSpectrum
- `FilePath`: "chunk:{chunkId}" 형식
- `Protein`, `Moisture`: 덩어리의 집계된 값
- `Metadata`: chunk_id, line_number, sample_count, timestamps

#### 2. `ChunkBasedNirMatcher.cs` (225줄)
덩어리 기반 NIR2 매칭 (INirMatcher 구현)

| 기능 | 설명 |
|------|------|
| `RegisterChunk(chunk)` | 완료된 덩어리를 매칭 대기열에 등록 |
| `MatchNirToGroups()` | 덩어리를 FileGroup에 매칭 (timestamp 근접 기반) |
| `GetAvailableNirFiles()` | 대기 중인 덩어리 목록 반환 |
| `PendingChunkCount` | 매칭 대기 중인 덩어리 수 |
| `ConsumedChunkCount` | 이미 소비된 덩어리 수 |
| `ClearChunks()` | 모든 덩어리 상태 초기화 |
| `GetDiagnosticInfo()` | 디버깅용 상태 정보 |

**매칭 로직**:
1. 덩어리의 AggregatedTimestamp (또는 StartedAt)와 FileGroup의 CreatedAt 비교
2. 가장 가까운 시간차의 FileGroup 선택 (nirMaxDiffSeconds 이내)
3. FileGroup의 Metadata에 NIR2 데이터 저장
4. 매칭 실패 시 NIR-only FileGroup 생성

### 설계 결정

**캐싱 vs 파일 기반**:
- NIR1: 파일 시스템에 .spc/.txt 저장 → 나중에 로드 가능
- NIR2: API에서 실시간 수집 → 메모리 캐시 사용
- ChunkBasedNirMatcher는 대기열(_pendingChunks) 관리
- ApiBasedNirProvider는 조회 캐시(_chunkCache) 관리

**중복 방지**:
- _consumedChunkIds로 이미 매칭된 덩어리 추적
- GetAvailableNirFiles()에서 consumed 필터링

---

## Phase 7 완료 내용 ✅

### 수정된 파일

#### 1. `SystemControlViewModel.cs`
NIR2 파싱 상태 관리 및 Command 추가

| 추가/수정 | 설명 |
|-----------|------|
| `Nir2ParsingState` 속성 | NIR2 파싱 상태 (CameraState) |
| `ToggleNir2ParsingCommand` | 시작/중지 토글 Command |
| `ExecuteToggleNir2Parsing()` | 파싱 시작/중지 로직 (Phase 8에서 DataCollector 연결 예정) |

#### 2. `WorkflowPanel.xaml`
NIR2 파싱 버튼 추가 (NIR Filtering 위에 위치)

```xml
<!-- NIR2 Parsing Toggle -->
<Grid Margin="0,0,0,4">
    <TextBlock Text="{x:Static res:Strings.Status_Nir2Parsing}" .../>
    <Ellipse Fill="{Binding Control.Nir2ParsingState, Converter={StaticResource CameraStateToIndicatorBrush}}" .../>
    <Button Command="{Binding Control.ToggleNir2ParsingCommand}" .../>
</Grid>
```

#### 3. `Strings.resx`, `Strings.ko.resx`, `Strings.Designer.cs`
로컬라이제이션 문자열 추가

| 문자열 | 값 |
|--------|-----|
| `Status_Nir2Parsing` | "NIR2 파싱:" |
| `Log_Info_Nir2Parsing_Started` | "NIR2 파싱 시작됨" |
| `Log_Info_Nir2Parsing_Stopped` | "NIR2 파싱 중지됨" |

### UI 구조

WorkflowPanel의 Camera Status Section에서 버튼 순서:
1. Normal (General Camera)
2. NIR
3. NIR 2
4. **NIR2 파싱** ← Phase 7에서 추가
5. NIR 필터링

### Phase 8 연결 예정

ExecuteToggleNir2Parsing()의 TODO 주석:
```csharp
// TODO: Phase 8 - Call Nir2DataCollector.StartAsync()
// TODO: Phase 8 - Call Nir2DataCollector.StopAsync()
```

---

## Phase 8 완료 내용 ✅

### 수정된 파일

#### 1. `App.xaml.cs`
DI 컨테이너에 NIR2 서비스 등록

| 등록 | 설명 |
|------|------|
| `ApiBasedNirProvider` (Singleton) | NIR2 데이터 제공자 |
| `ChunkBasedNirMatcher` (Singleton) | NIR2 덩어리 매칭 |
| `Nir2DataCollector` (Singleton) | NIR2 데이터 수집기 |

```csharp
// NIR2 Services (Singleton - shared state for data collection)
services.AddSingleton<ApiBasedNirProvider>();
services.AddSingleton<ChunkBasedNirMatcher>();
services.AddSingleton<Nir2DataCollector>(sp =>
{
    var config = sp.GetRequiredService<ApplicationConfiguration>();
    var logger = sp.GetRequiredService<ILogger<Nir2DataCollector>>();
    var settings = config.Nir2Settings ?? new Core.Configuration.Nir2Settings();
    return new Nir2DataCollector(settings, logger);
});
```

#### 2. `SystemControlViewModel.cs`
Nir2DataCollector 주입 및 연결

| 추가/수정 | 설명 |
|-----------|------|
| `_nir2DataCollector`, `_nir2DataProvider` 필드 | DI 주입 |
| `OnNir2CollectorStateChanged()` | StateChanged 이벤트 핸들러 |
| `OnNir2ChunkDetected()` | ChunkDetected 이벤트 → ApiBasedNirProvider.RegisterChunk |
| `UpdateNir2ParsingState()` | Nir2CollectorState → CameraState 변환 |
| `ExecuteToggleNir2Parsing()` | StartAsync()/StopAsync() 호출 |

이벤트 흐름:
```
Nir2DataCollector.ChunkDetected
    → OnNir2ChunkDetected()
        → ApiBasedNirProvider.RegisterChunk()
            → Chunk 캐시에 저장
                → ChunkBasedNirMatcher가 FileGroup에 매칭
```

#### 3. `MainWindowViewModel.cs`
생성자 파라미터에 Nir2DataCollector, ApiBasedNirProvider 추가

#### 4. `Strings.ko.resx`
덩어리 감지 로그 메시지 추가

| 문자열 | 값 |
|--------|-----|
| `Log_Info_Nir2_ChunkDetected` | "[NIR2] 덩어리 감지: {0}, 샘플={1}, 단백질={2}%, 수분={3}%" |

### 데이터 플로우

```
[User] → NIR2 파싱 버튼 클릭
    ↓
ExecuteToggleNir2Parsing()
    ↓
Nir2DataCollector.StartAsync()
    ↓
API 폴링 → 샘플 수집 → CSV 기록
    ↓
Nir2ChunkDetector 덩어리 완료 감지
    ↓
ChunkDetected 이벤트 발생
    ↓
OnNir2ChunkDetected() → ApiBasedNirProvider.RegisterChunk()
    ↓
캐시에 덩어리 저장 (chunk:chunkId)
    ↓
FileMatchingEngine에서 ChunkBasedNirMatcher 사용
    ↓
FileGroup에 NIR2 데이터 연결
```

---

## Phase 9 완료 내용 ✅

### 수정된 파일

#### 1. `FileGroupViewModel.cs` (267줄)
NIR2 표시 속성 추가

| 추가/수정 | 설명 |
|-----------|------|
| `_nirDataProvider` 필드 | INirDataProvider 의존성 주입 |
| `HasNir2` 속성 | NirKey가 "chunk:"로 시작하는지 확인 |
| `Nir2Protein` 속성 | 단백질 값 (Metadata에서 조회) |
| `Nir2Moisture` 속성 | 수분 값 (Metadata에서 조회) |
| `Nir2DisplayText` 속성 | UI 표시용 문자열 ("P: 12.5% M: 8.3%") |
| `LoadNir2DataAsync()` | Provider에서 NIR2 데이터 로드 |

#### 2. `SharedResources.xaml`
NIR2 템플릿 추가

```xml
<DataTemplate x:Key="Nir2FileTemplate">
    <!-- 녹색 배경 (#2a4a2a)으로 NIR2와 NIR1 구분 -->
    <!-- 단백질/수분 텍스트 표시 -->
</DataTemplate>
```

#### 3. `FileGroupDataGrid.xaml.cs`
Line 2에서 NIR2 템플릿 사용

```csharp
DataType.NIR => "Nir2FileTemplate",  // Line 2 uses NIR2 template
```

#### 4. `DashboardViewModel.cs`
INirDataProvider 의존성 주입 추가

| 추가/수정 | 설명 |
|-----------|------|
| `_nirDataProvider` 필드 | INirDataProvider 주입 |
| 생성자 파라미터 | `INirDataProvider? nirDataProvider = null` |
| `OnGroupCreated()` | FileGroupViewModel 생성 시 Provider 전달 |

### UI 구조

Line 1: NIR 그래프 표시 (NirFileTemplate)
Line 2: NIR2 단백질/수분 텍스트 표시 (Nir2FileTemplate, 녹색 배경)

---

## Phase 10 완료 내용 ✅

한국어 로컬라이제이션은 Phase 7에서 완료되었습니다. 추가할 문자열이 없음.

---

## Phase 11 완료 내용 ✅

### 수정된 파일
- `docs/architecture/glossary.md`

### 추가된 용어

| 클래스 | 설명 |
|--------|------|
| `FileBasedNirProvider` | Line 1 NIR 데이터 제공자 (파일 기반) |
| `FileBasedNirMatcher` | Line 1 NIR 매칭 (파일 기반) |
| `NirDisplayHandler` | Line 1 NIR 표시 핸들러 (그래프) |
| `Nir2Sample` | NIR2 샘플 데이터 모델 (API 응답) |
| `Nir2Chunk` | NIR2 청크 모델 (집계된 샘플) |
| `Nir2CsvManager` | NIR2 CSV 파일 관리자 |
| `Nir2ChunkDetector` | NIR2 청크 감지기 (상태 머신) |
| `Nir2DataCollector` | NIR2 데이터 수집기 (API 폴링) |
| `ApiBasedNirProvider` | Line 2 NIR 데이터 제공자 (API 기반) |
| `ChunkBasedNirMatcher` | Line 2 NIR 매칭 (청크 기반) |

| 인터페이스 | 설명 |
|-----------|------|
| `INirDataProvider` | NIR 데이터 로드 및 처리 인터페이스 |
| `INirMatcher` | NIR 매칭 작업 인터페이스 |
| `INirDisplayHandler` | NIR UI 표시 처리 인터페이스 |

| 도메인 개념 | 설명 |
|-----------|------|
| NIR1 (Line 1 NIR) | 파일 기반 Line 1 NIR 데이터 (.spc/.txt) |
| NIR2 (Line 2 NIR) | API 기반 Line 2 NIR 데이터 (실시간 수집) |
| NIR2 Chunk | 시간에 걸쳐 집계된 NIR2 샘플 컬렉션 |
| NIR2 Sample | 단일 NIR2 데이터 포인트 |

---

## 프로젝트 완료 요약 ✅

**목표 달성**: NIR 모듈화 및 NIR2 통합 완료

### 주요 성과
1. **NIR1 모듈화**: Line 1 NIR 코드를 별도 모듈로 분리하여 삭제/재적용이 쉬운 구조 확보
2. **NIR2 구현**: API 기반 실시간 NIR2 데이터 수집, CSV 저장, 덩어리 감지 구현
3. **인터페이스 통합**: NIR1/NIR2가 동일한 인터페이스를 사용하여 호환성 확보
4. **UI 통합**: Line 1은 그래프, Line 2는 텍스트(단백질/수분) 표시
5. **문서화**: Glossary 업데이트로 SSOT 유지

### 파일 구조 (최종)

```
ChronoView/Core/NIR/
├── Interfaces/               (공통 인터페이스)
│   ├── INirMatcher.cs          ✅
│   ├── INirDataProvider.cs     ✅
│   └── INirDisplayHandler.cs   ✅
│
├── Line1/                    (Line 1 NIR - 파일 기반)
│   ├── FileBasedNirProvider.cs      ✅
│   ├── FileBasedNirMatcher.cs       ✅
│   └── NirDisplayHandler.cs         ✅
│
├── Line2/                    (Line 2 NIR - API 기반)
│   ├── Nir2Sample.cs                 ✅ (70줄)
│   ├── Nir2Chunk.cs                  ✅ (160줄)
│   ├── Nir2CsvManager.cs             ✅ (245줄)
│   ├── Nir2ChunkDetector.cs          ✅ (159줄)
│   ├── Nir2DataCollector.cs          ✅ (362줄)
│   ├── ApiBasedNirProvider.cs        ✅ (185줄)
│   └── ChunkBasedNirMatcher.cs       ✅ (225줄)
│
└── Shared/                   (공통 유틸리티)
    ├── INirFileResolver.cs         ✅
    ├── NirSpectrumParser.cs        ✅
    ├── SpcTxtNirFileResolver.cs    ✅
    ├── NirAggregationStrategy.cs   ✅
    ├── NirGraphGenerator.cs        ✅
    └── NirSpectrumFilter.cs        ✅
```

**진행률: 11/11 Phases 완료 (100%)**

---

## 참고 사항

- **모듈화 목적**: Line 1 NIR 제거/재적용이 쉬워야 함
- **호환성**: NIR1과 NIR2는 동일한 인터페이스/함수명 사용
- **파일 크기**: 각 파일 600줄 미만 유지
- **UI**: NIR2는 텍스트 표시 (단백질/수분), 나중에 Nir2DisplayTemplate만 수정하여 그래프로 변경 가능
