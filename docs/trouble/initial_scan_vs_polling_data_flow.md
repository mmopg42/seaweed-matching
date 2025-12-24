# 초기 스캔과 폴링 데이터 흐름 분석

**작성일**: 2025-12-22  
**목적**: 초기 스캔 데이터가 유지되는지 확인 및 폴링 메커니즘과의 관계 분석

---

## 주요 발견사항

### ✅ 초기 스캔 데이터는 유지됨

**코드 근거** (`MonitoringOrchestrator.cs:714-728`):

```csharp
// Store groups and raise individual events
lock (_lockObject)
{
    foreach (var group in groupList)
    {
        // Check if group already exists to prevent duplicate events
        if (_activeGroups.ContainsKey(group.GroupId))
        {
            _logger.LogDebug("Group {GroupId} already exists in active groups - skipping event", 
                group.GroupId);
            continue;
        }
        
        _activeGroups[group.GroupId] = group;  // ← 초기 스캔 데이터 저장
        OnGroupCreated(group);
    }
}
```

**결론**: 
- 초기 스캔에서 생성된 그룹은 `_activeGroups`에 영구 저장됨
- 실시간 스캔(폴링/이벤트)은 **기존 데이터를 버리지 않고** 병합/추가함

---

## 데이터 흐름 상세 분석

### 1. 초기 스캔 단계 (StartAsync → PerformInitialScanAsync)

```
[StartAsync 호출]
    ↓
[PerformSequentialInitialScanAsync 실행]
    ↓
[파일 스캔 및 그룹 생성]
    - Normal 폴더: C251201T140543_0 등 14개
    - NIR 파일, Camera 파일 등
    ↓
[_activeGroups에 저장]  ← group_002 ~ group_015
    - _activeGroups["group_002"] = FileGroup {...}
    - _activeGroups["group_003"] = FileGroup {...}
    - ...
    ↓
[OnGroupCreated 이벤트 발생]
    - UI에 그룹 표시
```

**핵심**: 초기 스캔 데이터는 `_activeGroups` 딕셔너리에 저장되어 유지됨

---

### 2. 폴링 시작 단계 (StartNormalFolderPolling)

```
[StartAsync → Line 264]
    ↓
[StartNormalFolderPolling() 호출]
    ↓
[_normalPollingTimer 시작]
    - 간격: 100ms (0.1초)
    - 이벤트: OnNormalFolderPollTimerElapsed
    ↓
[PollNormalFoldersAsync 실행]
    ↓
[Normal 폴더 재스캔]
    - Directory.GetDirectories(...) 호출
    - 패턴: C{YYMMDD}T{HHMMSS}_{0|1}
    ↓
[ProcessNormalFolderAsync 호출]
    ↓
⚠️ [_processedNormalFolders 확인]
    - if (_processedNormalFolders.Contains(folderPath)) return;
    - ❌ 초기 스캔 폴더는 여기에 없음!
    ↓
[CreateGroupFromNormalFolderAsync 호출]
    ↓
[CreateOrUpdateGroupAsync 호출]
    ↓
[중복 그룹 생성]  ← group_016 ~ group_029
    - 초기 스캔과 동일한 Normal 폴더
    - 하지만 FindMatchingExistingGroup이 실패하여 새 그룹 생성
```

**핵심 문제점**:
- `_processedNormalFolders`: 폴링 전용 추적기 (초기 스캔 데이터와 분리됨)
- 초기 스캔 폴더가 폴링에서 "신규"로 인식됨

---

### 3. FindMatchingExistingGroup 로직 검증

**코드 위치**: `MonitoringOrchestrator.cs:1096`

```csharp
lock (_lockObject)
{
    FileGroup? existingGroup = FindMatchingExistingGroup(newGroup);
    
    if (existingGroup != null)
    {
        // Merge new data into existing group
        // ...
    }
    else
    {
        // ❌ 여기서 새 그룹 생성
        _activeGroups[newGroup.GroupId] = newGroup;
        OnGroupCreated(newGroup);
    }
}
```

**질문**: 왜 `FindMatchingExistingGroup`이 초기 스캔 그룹을 찾지 못했는가?

**가능성 1 - 타임스탬프 매칭 실패**:
- 초기 스캔: 폴더명에서 타임스탬프 추출 → `2025-12-01 14:05:43`
- 폴링 스캔: 동일 폴더명 → 동일 타임스탬프 **예상**
- 하지만 매칭 tolerance 범위 밖으로 판단될 수 있음

**가능성 2 - Normal 폴더 경로 불일치**:
- 초기 스캔: `"Z:\...\시뮬\normal\C251201T140543_0"`
- 폴링 스캔: 경로 정규화 차이로 다른 문자열로 인식

**가능성 3 - 그룹 ID 충돌**:
- 폴링에서 새 그룹 생성 시 `_nextGroupId` 사용
- 초기 스캔 그룹과 다른 GroupId로 생성됨

---

## WSL 환경 폴링 필수성

### 사용자 요구사항

> 1. 반드시 폴링을 써야함: Normal과 NIR은 WSL 안에 있고 이벤트 기반으로는 감지 불가
> 2. 통일성을 위해 다른 항목에도 폴링 사용

### 현재 아키텍처

**이벤트 기반 감지** (FileWatcher + FileSystemWatcher):
```csharp
// Line 233-240
_fileWatcher.FileChanged += OnFileChanged;
await _fileWatcher.StartWatchingAsync(watchPaths, watcherOptions);
```

**폴링 기반 감지** (타이머):
```csharp
// Line 264
StartNormalFolderPolling();

// Line 2820-2848
_normalPollingTimer = new System.Timers.Timer(100); // 0.1초
_normalPollingTimer.Elapsed += OnNormalFolderPollTimerElapsed;
```

**문제점**:
- 이벤트와 폴링이 **병행** 실행됨
- Normal 폴더는 이벤트 + 폴링 둘 다 감지됨
- WSL 환경에서는 이벤트가 작동하지 않을 수 있지만, 로컬 환경에서는 중복 감지 발생

---

## 해결 방안

### Option 1: 폴링 추적기 초기화 (간단, Hot Fix)

**목적**: 초기 스캔 데이터를 폴링 추적기에 등록하여 중복 방지

**구현**:
```csharp
// PerformSequentialInitialScanAsync 내
foreach (var folder in normalFolders)
{
    await CreateGroupFromDataFileAsync(folder, dataType);
    
    // ✅ 폴링 추적기에 등록
    lock (_pollingLock)
    {
        _processedNormalFolders.Add(folder);
    }
}
```

**장점**:
- ✅ 빠른 수정 (5분)
- ✅ 중복 그룹 생성 즉시 해결

**단점**:
- ⚠️ 근본적 아키텍처 문제는 해결 안 됨
- ⚠️ 이벤트 기반과 폴링 병행 시 여전히 복잡도 높음

---

### Option 2: 통합 폴링 아키텍처 (권장, 장기)

**목적**: 모든 파일 타입을 폴링으로 통일하여 일관성 확보

**설계**:

```csharp
// 1. FileWatcher 비활성화 (WSL 환경에서 작동 안 하므로)
// await _fileWatcher.StartWatchingAsync(...);  // ← 제거

// 2. 통합 폴링 타이머 시작
StartUnifiedPolling(config);

// 3. 폴링 메서드
private async Task PollAllFilesAsync()
{
    // NIR 파일 폴링
    await PollNirFilesAsync();
    
    // Normal 폴더 폴링
    await PollNormalFoldersAsync();
    
    // Camera 파일 폴링
    await PollCameraFilesAsync();
}

// 4. 통합 추적기
private readonly HashSet<string> _processedFiles = new();

private async Task ProcessFileAsync(string filePath, FileType fileType)
{
    lock (_pollingLock)
    {
        if (_processedFiles.Contains(filePath)) return;
        _processedFiles.Add(filePath);
    }
    
    await CreateOrUpdateGroupAsync(filePath, fileType);
}
```

**장점**:
- ✅ WSL 환경과 로컬 환경 모두 일관된 동작
- ✅ 이벤트/폴링 병행으로 인한 복잡도 제거
- ✅ 초기 스캔과 실시간 스캔 로직 통일 가능

**단점**:
- ⚠️ 대규모 리팩토링 필요 (1~2일)
- ⚠️ 폴링 간격에 따른 감지 지연 (100ms → 실시간성 유지 가능)

---

### Option 3: 하이브리드 접근 (중간)

**목적**: WSL 경로만 폴링, 로컬 경로는 이벤트 유지

**구현**:
```csharp
// 1. 경로별 감지 방식 구분
var wslPaths = new List<string>();      // Normal, NIR (폴링)
var localPaths = new List<string>();     // Camera (이벤트)

foreach (var path in watchPaths)
{
    if (IsWSLPath(path))
        wslPaths.Add(path);
    else
        localPaths.Add(path);
}

// 2. 이벤트는 로컬 경로만
if (localPaths.Count > 0)
    await _fileWatcher.StartWatchingAsync(localPaths, watcherOptions);

// 3. 폴링은 WSL 경로만
if (wslPaths.Count > 0)
    StartPollingForPaths(wslPaths);
```

**장점**:
- ✅ 최소한의 변경으로 WSL 지원
- ✅ 로컬 파일은 즉시 감지 (이벤트)

**단점**:
- ⚠️ 복잡도 증가 (이벤트+폴링 병행)
- ⚠️ WSL 경로 판단 로직 필요

---

## 권장 조치

### 즉시 조치 (1시간 이내)

**Option 1 적용**: 폴링 추적기 초기화

```csharp
// MonitoringOrchestrator.cs:PerformSequentialInitialScanAsync
foreach (var folder in normalFolders)
{
    await CreateGroupFromDataFileAsync(folder, dataType);
    
    // ✅ 추가
    lock (_pollingLock)
    {
        _processedNormalFolders.Add(folder);
        _logger.LogDebug("Added Normal folder to polling tracker: {Folder}", folder);
    }
}
```

---

### 장기 조치 (1주일 내)

**Option 2 적용**: 통합 폴링 아키텍처로 마이그레이션

**단계**:
1. **설계 문서 작성** (1일)
   - 폴링 간격 최적화 (100ms ~ 500ms)
   - 메모리 사용량 예측
   - 성능 테스트 계획

2. **구현** (2일)
   - `UnifiedPollingService` 클래스 생성
   - FileWatcher 의존성 제거
   - 통합 추적기 구현

3. **테스트** (1일)
   - WSL 환경 테스트
   - 로컬 환경 테스트
   - 성능 벤치마크

4. **배포** (0.5일)
   - Feature flag로 점진적 배포
   - 모니터링 및 롤백 준비

---

## 참고 자료

### 관련 코드
- `MonitoringOrchestrator.cs:142` - `PerformInitialScanAsync` 호출
- `MonitoringOrchestrator.cs:264` - `StartNormalFolderPolling` 호출
- `MonitoringOrchestrator.cs:714-728` - `_activeGroups` 저장 로직
- `MonitoringOrchestrator.cs:2926-2948` - `ProcessNormalFolderAsync`

### 관련 문서
- `initial_scan_performance_issue.md` - 초기 스캔 성능 문제
- `normal_folder_polling_implementation.md` - 폴링 구현 설계
- `event_vs_polling_analysis.md` - 이벤트 vs 폴링 비교

---

## 결론

**핵심 발견**:
1. ✅ 초기 스캔 데이터는 `_activeGroups`에 유지됨 (버려지지 않음)
2. ❌ 폴링 추적기 `_processedNormalFolders`가 초기 스캔 데이터를 모름
3. ❌ 폴링이 동일 폴더를 재감지하여 중복 그룹 생성

**권장 방향**:
- **즉시**: 폴링 추적기 초기화 (Hot Fix)
- **장기**: 통합 폴링 아키텍처 (근본 해결)
