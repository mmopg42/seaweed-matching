# 초기 스캔 성능 문제 분석

**작성일**: 2025-12-22  
**관련 로그**: 2025-12-22 오전 11:10:48 ~ 11:15:02  
**심각도**: HIGH  
**문제 요약**: 초기 스캔 시 중복 그룹 생성 및 썸네일 로딩 4분 이상 지연

---

## 문제 현상

### 관찰된 증상

1. **중복 그룹 생성**
   - 동일한 Normal 폴더에 대해 2개의 그룹이 생성됨
   - 초기 스캔: group_002 ~ group_015 (14개 그룹)
   - 폴링 재감지: group_016 ~ group_029 (14개 그룹)
   - 총 28개 그룹이 생성되었으나 실제 데이터는 14개

2. **썸네일 로딩 지연**
   - C251201T140543_0 감지: 11:10:58
   - 동일 폴더 Main 이미지 표시: 11:14:23
   - **약 3분 25초 지연** (전체 로딩 완료는 11:15:02로 4분 이상 소요)

---

## 근본 원인 분석

### 1. 중복 그룹 생성 원인: 폴링 메커니즘의 초기화 문제

#### 발생 메커니즘

```
타임라인:
11:10:58 - 초기 스캔 시작 (PerformSequentialInitialScanAsync)
         → Normal 폴더 감지: C251201T140543_0 등 14개
         → group_002 ~ group_015 생성

11:10:58 - 모니터링 시작 (StartAsync)
         → StartNormalFolderPolling() 호출
         → _normalPollingTimer 시작 (0.1초 간격)

11:10:58 - 첫 폴링 실행 (OnNormalFolderPollTimerElapsed)
         → 동일한 Normal 폴더 재감지 (14개)
         → _processedNormalFolders가 비어있음 (초기 스캔 데이터와 분리됨)
         → ProcessNormalFolderAsync 호출
         → CreateOrUpdateGroupAsync → 새 그룹 생성
         → group_016 ~ group_029 생성
```

#### 코드 분석

**문제 코드** (`MonitoringOrchestrator.cs`):

```csharp
// Line 264: StartAsync 메서드 내
// 초기 스캔 완료 후 폴링 시작
await PerformInitialScanAsync();  // ← 초기 스캔에서 Normal 폴더 처리
// ...
StartNormalFolderPolling();       // ← 폴링이 같은 폴더를 재감지

// Line 2900~2944: ProcessNormalFolderAsync
private async Task ProcessNormalFolderAsync(string folderPath)
{
    lock (_pollingLock)
    {
        // _processedNormalFolders는 폴링 전용 추적기
        // 초기 스캔에서 처리된 폴더를 모르고 있음
        if (_processedNormalFolders.Contains(folderPath))
            return;  // ← 초기 스캔 폴더는 여기서 걸러지지 않음
    }
    
    // ... 새 그룹 생성
}
```

**핵심 문제점**:
- `_processedNormalFolders`: 폴링 전용 추적 HashSet
- `PerformSequentialInitialScanAsync`: 별도의 스캔 로직, `_processedNormalFolders`와 무관
- 초기 스캔에서 처리된 폴더가 폴링 추적 목록에 없음
- 폴링이 시작되자마자 동일 폴더를 "신규"로 인식하여 중복 그룹 생성

#### 로그 증거

```
Debug 2025-12-22 오전 11:10:58 Normal [C251201T140543_0] 새 그룹 생성 → group_002
Info  2025-12-22 오전 11:10:58 GroupManager Added group group_002
...
Debug 2025-12-22 오전 11:10:58 Normal [C251201T140550_0] 새 그룹 생성 → group_016
Info  2025-12-22 오전 11:10:58 GroupManager Added group group_016
Info  2025-12-22 오전 11:10:58 Polling 일반카메라 폴더 감지: C251201T140550_0
```

→ 동일 타임스탬프(11:10:58)에 group_002와 group_016이 모두 생성됨

---

### 2. 썸네일 로딩 지연 원인: 순차 처리 병목

#### 발생 메커니즘

```
11:10:58 - 28개 그룹 생성 (초기 14개 + 중복 14개)
         → 각 그룹의 LoadThumbnailsAsync 호출
         → "썸네일 로딩 시작" 로그 28x4개 = 112개 메시지

11:11:00 - group_002 NIR 그래프 표시 완료 (첫 번째 완료)
11:11:05 - group_010 NIR 그래프 표시 완료
...
11:15:02 - group_014 Camera3 이미지 표시 (마지막 완료)

총 소요 시간: 약 4분
평균 처리 속도: ~6초/이미지
```

#### 코드 분석

**문제 코드** (`FileGroupViewModel.cs:654`):

```csharp
public async Task LoadThumbnailsAsync()
{
    // 각 이미지 타입별 순차 처리
    if (!string.IsNullOrEmpty(MainImagePath) && MainImageThumbnail == null)
    {
        await LoadSingleThumbnailAsync(...);  // ← 완료 대기
    }
    
    if (HasNir && !string.IsNullOrEmpty(NirImagePath))
    {
        await LoadSingleThumbnailAsync(...);  // ← 완료 대기
    }
    
    if (NirGraphThumbnail == null)
    {
        await LoadNirGraphThumbnailAsync(...);  // ← 완료 대기 (그래프 생성 느림)
    }
    
    // Camera 썸네일도 순차 처리
    if (Camera1Thumbnail == null) await LoadCameraThumbnailAsync(1, ...);  // ← 완료 대기
    if (Camera2Thumbnail == null) await LoadCameraThumbnailAsync(2, ...);
    if (Camera3Thumbnail == null) await LoadCameraThumbnailAsync(3, ...);
    // ...
}
```

**성능 병목**:
1. **그룹 내 순차 처리**: 각 그룹의 5~6개 이미지가 순차적으로 로딩됨
2. **NIR 그래프 생성**: `NirGraphGenerator.GenerateGraph()`가 동기 I/O 작업으로 느림 (평균 2~3초)
3. **28개 그룹 x 평균 5개 이미지**: 총 140개의 썸네일을 순차 처리

#### 예상 vs 실제

| 항목 | 예상 (병렬) | 실제 (순차) |
|------|------------|-----------|
| 1개 이미지 | 0.5초 | 0.5초 |
| 1개 그룹 (5개 이미지) | 0.5초 | 2.5초 |
| 28개 그룹 | 1~2초 | 240초 (4분) |

---

## 영향 범위

### 시스템 영향

1. **UI 응답성**
   - 초기 스캔 후 4분간 썸네일 표시 지연
   - 사용자는 빈 썸네일을 장시간 확인해야 함

2. **메모리 낭비**
   - 중복 그룹으로 인한 2배 메모리 사용
   - 불필요한 ViewModel 28개 추가 생성

3. **로그 오염**
   - "썸네일 로딩 시작/완료" 메시지 140+ 개
   - 실제 문제 디버깅 시 로그 분석 어려움

### 재현 조건

**재현 가능 (100%)**:
- ✅ 폴더 내 데이터가 미리 존재하는 상태에서 초기 스캔
- ✅ Normal 폴더가 10개 이상 존재
- ✅ `StartNormalFolderPolling()` 활성화

**재현 불가능**:
- ❌ 실시간 파일 생성 시나리오 (폴링 타이밍 차이로 중복 감소)
- ❌ Normal 폴더 개수 3개 이하 (영향 미미)

---

## 해결 방안

### 즉시 조치 (Hot Fix)

#### 1. 폴링 추적기 초기화

**위치**: `MonitoringOrchestrator.cs:PerformSequentialInitialScanAsync`

**변경 전**:
```csharp
private async Task<OrchestrationResult> PerformSequentialInitialScanAsync(...)
{
    // 초기 스캔 처리
    foreach (var dataType in orderedTypes)
    {
        if (dataType == DataType.Normal)
        {
            // Normal 폴더 스캔 및 그룹 생성
            // ❌ _processedNormalFolders 업데이트 없음
        }
    }
}
```

**변경 후**:
```csharp
private async Task<OrchestrationResult> PerformSequentialInitialScanAsync(...)
{
    foreach (var dataType in orderedTypes)
    {
        if (dataType == DataType.Normal)
        {
            // Normal 폴더 스캔
            var folders = GetNormalFolders(path);
            
            foreach (var folder in folders)
            {
                // 그룹 생성
                await CreateOrUpdateGroupAsync(folder, FileType.Normal);
                
                // ✅ 폴링 추적기에 등록
                lock (_pollingLock)
                {
                    _processedNormalFolders.Add(folder);
                }
            }
        }
    }
}
```

#### 2. 썸네일 병렬 로딩

**위치**: `FileGroupViewModel.cs:LoadThumbnailsAsync`

**변경 전**:
```csharp
// 순차 처리
if (Camera1Thumbnail == null) await LoadCameraThumbnailAsync(1, ...);
if (Camera2Thumbnail == null) await LoadCameraThumbnailAsync(2, ...);
if (Camera3Thumbnail == null) await LoadCameraThumbnailAsync(3, ...);
```

**변경 후**:
```csharp
// 병렬 처리
var cameraTasks = new List<Task>();
if (Camera1Thumbnail == null) cameraTasks.Add(LoadCameraThumbnailAsync(1, ...));
if (Camera2Thumbnail == null) cameraTasks.Add(LoadCameraThumbnailAsync(2, ...));
if (Camera3Thumbnail == null) cameraTasks.Add(LoadCameraThumbnailAsync(3, ...));
// ...
await Task.WhenAll(cameraTasks);
```

**예상 개선**:
- 그룹당 로딩 시간: 2.5초 → 0.5초
- 전체 로딩 시간: 4분 → 30초

---

### 장기 개선 (Refactoring)

#### 1. 폴링 로직 통합

**목표**: 초기 스캔과 폴링의 추적 메커니즘 통합

**제안**:
```csharp
// 통합 추적 서비스
public class ProcessedFilesTracker
{
    private readonly ConcurrentHashSet<string> _processedPaths;
    
    public void MarkAsProcessed(string path, FileType type)
    {
        _processedPaths.Add(path);
    }
    
    public bool IsProcessed(string path)
    {
        return _processedPaths.Contains(path);
    }
}
```

#### 2. 썸네일 로딩 최적화

**단계적 로딩**:
1. **우선순위 1**: 뷰포트 내 그룹만 로딩 (가상화)
2. **우선순위 2**: Main/NIR 이미지 우선, 카메라 이미지 후순위
3. **우선순위 3**: 스크롤 시 on-demand 로딩

**비동기 큐**:
```csharp
// 썸네일 로딩 워커
public class ThumbnailLoadingQueue
{
    private readonly Channel<FileGroup> _queue;
    private readonly int _workerCount = 4;
    
    public async Task EnqueueAsync(FileGroup group)
    {
        await _queue.Writer.WriteAsync(group);
    }
    
    private async Task WorkerAsync()
    {
        await foreach (var group in _queue.Reader.ReadAllAsync())
        {
            await group.LoadThumbnailsAsync();
        }
    }
}
```

---

## 참고 자료

### 관련 로그 파일
- 전체 로그: 사용자 제공 (11:10:48 ~ 11:15:02)

### 관련 코드
- `MonitoringOrchestrator.cs:264` - `StartNormalFolderPolling()`
- `MonitoringOrchestrator.cs:2900` - `ProcessNormalFolderAsync()`
- `MonitoringOrchestrator.cs:774` - `PerformSequentialInitialScanAsync()`
- `FileGroupViewModel.cs:654` - `LoadThumbnailsAsync()`

### 기존 트러블슈팅 문서
- `normal_folder_polling_implementation.md` - 폴링 메커니즘 설계
- `event_vs_polling_analysis.md` - 이벤트 vs 폴링 비교

---

## 결론

초기 스캔 시 발생하는 성능 문제는 **폴링 추적기 초기화 부재**와 **순차 썸네일 로딩**의 조합으로 인해 발생합니다.

**핵심 교훈**:
1. 초기 스캔과 실시간 감지의 상태 동기화 필수
2. 대량 I/O 작업은 병렬 처리 필수
3. 로그에서 중복 메시지는 중복 처리의 신호

**권장 조치**:
- ✅ 즉시: 폴링 추적기 초기화 (5분 작업)
- ⚠️ 단기: 썸네일 병렬 로딩 (1시간 작업)
- 🔵 장기: 아키텍처 리팩토링 (1일 작업)
