# 실시간 파일 개수 업데이트 문제 심층 분석

## 증상 (Symptom)
- **Refresh (새로고침)**: 파일 개수가 UI에 정상적으로 반영됨 ✓
- **Start (시작)**: 파일 개수가 실시간으로 반영되지 않음 ✗

## ⭐ 추가 컨텍스트 (Critical Context)
**그리드는 정상 작동!**
- 그룹 생성 ✓
- 이미지 표시 ✓
- 실시간 업데이트 ✓

**파일 개수만 0**
- `StatisticsService` → `DashboardViewModel` 경로만 문제

**⭐ 결정적 정보 (2026-01-06 추가)**
- **데이터가 이미 있는 상황에서 테스트**
- **Refresh**: 정확한 값 반환 ✓
- **Start**: 처음부터 0, 이후 변경 없음 ✗
- **새 파일 생성되어도 변화 없음**

**결론**: `MonitorFileCountsAsync` 폴링 루프가:
1. ~~아예 실행되지 않거나~~ (로그로 확인: 시작됨 ✓)
2. `GetFileCountsAsync`가 백그라운드 스레드에서 항상 0을 반환

---

## 📋 로그 분석 결과 (2026-01-06 12:53)

**확인된 로그:**
```
DEBUG: StartMonitoringAsync called, _isMonitoring=False  ✓ 정상 (조기반환 아님)
DEBUG: Starting background file counting task...         ✓ 정상 (Task.Run 호출됨)
Statistics monitoring started                            ✓ 정상
DEBUG: Statistics monitoring started successfully        ✓ 정상
```

**결론:**
- ✅ `StartMonitoringAsync`는 정상 실행됨
- ✅ `_isMonitoring=False`로 조기 반환 아님
- ✅ 폴링 루프 Task.Run() 호출됨
- ❓ **`MonitorFileCountsAsync` 내부 로그가 없음** → 루프 실행 여부 불명

---

## 📋 로그 분석 결과 #2 (2026-01-06 13:04)

**BeginInvoke 수정 후 로그:**
```
★ BEFORE InvokeAsync                  → Deadlock 발생 위치
```
- ❌ `★ INSIDE InvokeAsync callback` 없음
- ❌ `★ AFTER InvokeAsync` 없음
- ❌ 루프가 첫 번째 반복에서 멈춤

**BeginInvoke 수정 적용 → Deadlock 해결**

---

## 📋 로그 분석 결과 #3 (2026-01-06 13:35) ⭐ 현재

**BeginInvoke 수정 후 새 로그:**
```
★ POLL: Nir=2, Normal=12, Cam1=8, Cam2=8, Cam3=8
★ LOOP iteration complete, continuing...   ← 무한 반복
```

**문제점:**
1. ✅ **루프가 정상 작동** (100ms마다 반복)
2. ✅ **`GetFileCountsAsync` 올바른 값 반환** (Nir=2, Normal=12, ...)
3. ❌ **`★ BEFORE BeginInvoke` 로그가 없음** → 이벤트 미발생
4. ❌ **UI 파일 개수 0 유지**

---

## ⭐ 근본 원인 (Root Cause) - 최종

### 시나리오 분석

```
시간순서:
┌─────────────────────────────────────────────────────────────────┐
│ 1. StartMonitoringAsync 호출                                     │
│    → _lastFileCount = null 설정                                   │
│    → Task.Run(MonitorFileCountsAsync) 시작                        │
├─────────────────────────────────────────────────────────────────┤
│ 2. 폴링 루프 첫 번째 반복                                          │
│    → GetFileCountsAsync() → {Nir=2, Normal=12, ...}              │
│    → _lastFileCount == null → TRUE                                │
│    → BeginInvoke(callback) 호출 → 큐에 추가됨                      │
│    → _lastFileCount = {Nir=2, Normal=12, ...} 설정                │
├─────────────────────────────────────────────────────────────────┤
│ 3. 동시에 UI 스레드: Initial Scan 진행 중 (1-2초 소요)             │
│    → 이미지 로딩, 그룹 생성 등으로 바쁨                             │
│    → BeginInvoke callback이 큐에 있지만 실행 안 됨                  │
├─────────────────────────────────────────────────────────────────┤
│ 4. 폴링 루프 두 번째+ 반복                                         │
│    → GetFileCountsAsync() → {Nir=2, Normal=12, ...} (동일)        │
│    → _lastFileCount.Equals(stats) → TRUE                          │
│    → 조건 FALSE → BeginInvoke 호출 안 함                           │
├─────────────────────────────────────────────────────────────────┤
│ 5. Initial Scan 완료 후 UI 스레드 idle                            │
│    → 첫 번째 BeginInvoke callback 실행됨                           │
│    → FileCountsUpdated?.Invoke() 호출                             │
│    → 하지만 파일 개수가 변경되지 않아 UI는 그대로                    │
└─────────────────────────────────────────────────────────────────┘
```

### 핵심 문제

**첫 번째 callback이 실행될 때 이미 `_lastFileCount`가 설정되어 있어서 이후 변경 감지가 안 됨.**

하지만 더 큰 문제는:
**첫 번째 callback이 UI를 업데이트했어야 하는데, 왜 안 됐는가?**

가능한 원인:
1. **`FileCountsUpdated` 이벤트에 구독자가 없었음** (타이밍 이슈)
2. **callback이 아예 실행되지 않음** (UI 스레드가 종료 시점까지 바쁨)
3. **UI 바인딩 문제** (프로퍼티는 변경됐지만 UI 업데이트 안 됨)

---

## 🔧 해결 방안

### Option 1: StartMonitoringAsync에서 즉시 동기적 업데이트 (권장)
```csharp
public async Task StartMonitoringAsync(...)
{
    _currentConfig = config;
    _lastFileCount = null;
    _isMonitoring = true;
    
    // ⭐ 첫 번째 업데이트를 동기적으로 수행
    var initialStats = await GetFileCountsAsync();
    FileCountsUpdated?.Invoke(this, initialStats);  // 직접 호출 (UI 스레드)
    _lastFileCount = initialStats;
    
    // 이후 백그라운드 폴링 시작
    _monitoringTask = Task.Run(() => MonitorFileCountsAsync(...));
}
```

### Option 2: 폴링 루프에서 callback 실행 확인 후 _lastFileCount 설정
```csharp
// BeginInvoke 결과를 변수에 저장하지 않고 직접 실행 확인은 복잡함
// 권장하지 않음
```

### Option 3: ReloadStatsAsync 호출 추가
```csharp
// SystemControlViewModel.StartMonitoringAsync 에서:
await _statsService.StartMonitoringAsync(config);
await _statsService.ReloadStatsAsync(config);  // ⭐ 즉시 강제 업데이트
```

**권장: Option 1** - 가장 깔끔하고 확실한 해결책

## 코드 경로 비교 (Code Path Comparison)

### Refresh 경로 (작동함)
```
RefreshCommand → RefreshMonitoringAsync() → _statsService.ReloadStatsAsync(config)
```
**`ReloadStatsAsync` 내부:**
```csharp
public async Task ReloadStatsAsync(...)
{
    _currentConfig = config;
    var stats = await GetFileCountsAsync();  // 파일 카운트 수집
    
    // ★ 항상 이벤트 발생 ★
    FileCountsUpdated?.Invoke(this, stats);  // 직접 호출 (UI 스레드에서 실행)
    _lastFileCount = stats;
}
```
- **핵심**: `FileCountsUpdated` 이벤트를 **무조건** 발생시킴
- **스레드**: UI 스레드에서 직접 호출됨 (`RefreshCommand`가 UI 스레드에서 실행되므로)

---

### Start 경로 (작동 안 함)
```
StartCommand → StartMonitoringAsync() → _statsService.StartMonitoringAsync(config)
                                              ↓
                              Task.Run() → MonitorFileCountsAsync() [Background Thread]
```
**`MonitorFileCountsAsync` 내부:**
```csharp
private async Task MonitorFileCountsAsync(CancellationToken cancellationToken)
{
    const int updateIntervalMs = 100; // 100ms for real-time updates
    const int debounceMs = 50; // Reduced debounce for responsiveness

    while (!cancellationToken.IsCancellationRequested)
    {
        var stats = await GetFileCountsAsync();  // 파일 카운트 수집

        // ★ 변경이 있을 때만 이벤트 발생 ★
        if (_lastFileCount == null || !_lastFileCount.Equals(stats))
        {
            // Debounce 체크
            var timeSinceLastUpdate = DateTime.Now - _lastFileCountUpdate;
            if (timeSinceLastUpdate.TotalMilliseconds >= debounceMs)
            {
                await _debounceSemaphore.WaitAsync(cancellationToken);
                try
                {
                    _lastFileCount = stats;
                    _lastFileCountUpdate = DateTime.Now;
                    
                    // Marshal to UI thread for event notification
                    if (_dispatcher.CheckAccess())
                    {
                        // Already on dispatcher thread, invoke synchronously
                        FileCountsUpdated?.Invoke(this, stats);
                    }
                    else
                    {
                        // Not on dispatcher thread, marshal to it
                        await _dispatcher.InvokeAsync(() =>
                        {
                            FileCountsUpdated?.Invoke(this, stats);
                        }, DispatcherPriority.Normal);
                    }
                }
                finally
                {
                    _debounceSemaphore.Release();
                }
            }
        }
        
        await Task.Delay(updateIntervalMs, cancellationToken);  // 100ms 간격 폴링
    }
}
```
- **핵심**: `FileCountsUpdated` 이벤트는 **값이 변경되었을 때만** 발생
- **스레드**: Background 스레드에서 `Dispatcher.InvokeAsync`로 UI 스레드에 마샬링

---

## 핵심 차이점 (Critical Differences)

| 항목 | Refresh | Start |
|------|---------|-------|
| 이벤트 발생 조건 | **항상** | 값 변경 시에만 |
| 이벤트 발생 방식 | 직접 호출 | `Dispatcher.InvokeAsync` |
| 실행 스레드 | UI Thread | Background → UI Marshal |

---

## 가능한 원인 (Possible Root Causes)

### 가설 1: ⭐ 폴링 루프가 실행되지 않음 (최우선 확인)
- `_isMonitoring`이 이미 `true`인 경우, `StartMonitoringAsync`가 조기 반환됨 (Line 63-67)
- **조기 반환 시 `_currentConfig`와 `_lastFileCount`가 설정되지 않음!**
- Stop 후 재시작 시 `CancellationTokenSource`가 취소된 상태라면 폴링 루프가 즉시 종료될 수 있음
- 예외가 발생해서 루프가 시작 전에 종료될 수 있음

**검증 방법**: 
1. 로그에 `"DEBUG: Starting background file counting task..."` 메시지가 있는지 확인
2. `MonitorFileCountsAsync` 시작 부분에 로그 추가

### 가설 2: ⚠️ `GetFileCountsAsync`가 올바른 값을 반환하지 않음 (⭐ 가장 유력)
- **중요**: 그리드가 정상 작동하므로 경로 설정 자체는 올바름
- `MonitoringOrchestrator`는 같은 경로를 사용하고 정상 작동함
- 하지만 `StatisticsService.GetFileCountsAsync`는 0을 반환
- **가능한 원인**:
  1. `_currentConfig`가 `GetFileCountsAsync` 호출 시점에 null이어서 디스크에서 다른 설정 로드
  2. Race condition: `Task.Run`이 `_currentConfig` 설정 전에 실행 (가능성 낮음)
  3. `CountFilesInDirectoryAsync`가 예외를 던지고 catch에서 0 반환
  4. 경로는 올바르지만 파일 카운팅 로직에 문제

**검증 방법**: 
- `GetFileCountsAsync` 결과를 로그로 출력
- `_currentConfig`가 null인지 확인
- 각 경로의 존재 여부와 파일 개수를 로그로 확인

### 가설 3: ⚠️ `Dispatcher.InvokeAsync` 호출이 실패하거나 무시됨
- Dispatcher 객체가 올바르게 주입되지 않음
- UI 스레드가 바쁘거나 차단되어 `InvokeAsync` 콜백이 지연됨

**검증 방법**: `MonitorFileCountsAsync` 루프 내에서 이벤트 발생 전후 로그 추가

### 가설 4: ❌ 이벤트 구독자가 없거나 잘못된 인스턴스에 구독됨 (기각됨)
- ~~DI 컨테이너에서 `IStatisticsService`가 Singleton이 아닌 경우, `DashboardViewModel`과 `SystemControlViewModel`이 서로 다른 인스턴스를 사용할 수 있음~~
- **확인 결과**: `App.xaml.cs:203`에서 `services.AddSingleton<IStatisticsService, StatisticsService>()`로 Singleton 등록됨
- **결론**: 인스턴스 불일치 가능성 없음

### 가설 5: ⚠️ `_currentConfig` Race Condition 또는 null 체크 문제 (⭐ 매우 유력)
- `StartMonitoringAsync`에서 `_currentConfig = config`로 설정 (Line 77)
- 바로 다음 줄에서 `Task.Run`으로 백그라운드 태스크 시작 (Line 84)
- `GetFileCountsAsync`에서 `_currentConfig ?? await _configManager.LoadConfigurationAsync()` 사용 (Line 179)
- **문제 시나리오**:
  1. `Task.Run`이 즉시 실행되어 `MonitorFileCountsAsync` 시작
  2. 첫 번째 `GetFileCountsAsync` 호출 시 `_currentConfig`가 아직 null일 수 있음 (매우 드묾)
  3. 또는 `_currentConfig`가 null이면 디스크에서 로드한 다른/오래된 설정 사용
  4. **그리드가 작동하는 이유**: `MonitoringOrchestrator`는 `_currentConfig`를 직접 사용하고, `StartAsync`에서 설정됨
  5. **파일 개수가 0인 이유**: `StatisticsService`가 다른 설정을 사용하거나, 설정이 null

**검증 방법**: 
- `StartMonitoringAsync` 호출 시 `config` 객체 로그 출력
- `GetFileCountsAsync` 내부에서 `_currentConfig`가 null인지 확인하는 로그 추가
- `_currentConfig`와 디스크에서 로드한 설정의 경로 비교

### 가설 6: ⚠️ `GetFileCountsAsync`가 항상 같은 값을 반환
- 경로가 비어있거나 존재하지 않으면 항상 0 반환
- 예외 발생 시 catch에서 로그만 남기고 계속 진행 (Line 242-245)
- 파일이 실제로 증가해도 `GetFileCountsAsync`가 같은 값을 반환하면 변경 감지 실패

**검증 방법**: 
- `GetFileCountsAsync` 결과를 매번 로그로 출력
- 각 경로의 존재 여부와 파일 개수를 로그로 확인

### 가설 7: ⭐ 첫 반복에서 0을 반환하고 이후 변경 없음 (가장 유력)
**시나리오**:
1. `StartMonitoringAsync` 호출 → `_lastFileCount = null` 설정
2. `Task.Run()` → `MonitorFileCountsAsync` 시작
3. **첫 반복**: `GetFileCountsAsync()` → 모든 값 0 반환 (경로 문제 또는 타이밍)
4. `_lastFileCount == null` → true → 이벤트 발생 (값 0으로)
5. `_lastFileCount = {0, 0, 0, ...}`
6. **이후 반복**: `GetFileCountsAsync()` → 여전히 0 반환
7. `_lastFileCount.Equals(stats)` → true (둘 다 0) → 이벤트 미발생

**왜 첫 반복에서 0을 반환하는가?**
- `_currentConfig`가 아직 설정 전일 수 있음 (타이밍 이슈)
- 경로가 존재하지 않거나 비어있음
- 파일 시스템 캐싱으로 인해 즉시 파일을 감지하지 못함

**왜 Refresh는 작동하는가?**
- Refresh 시점에는 이미 파일이 존재하고 경로가 올바름
- UI 스레드에서 동기적으로 실행되어 타이밍 이슈 없음

---

## 권장 디버깅 단계 (Recommended Debugging Steps)

### Step 1: 폴링 루프 실행 확인
`MonitorFileCountsAsync` 시작 부분에 로그 추가:
```csharp
private async Task MonitorFileCountsAsync(CancellationToken cancellationToken)
{
    _logger.LogWarning("★ MonitorFileCountsAsync STARTED");  // 추가
    
    while (!cancellationToken.IsCancellationRequested)
    {
        // ...
```

### Step 2: 이벤트 발생 확인
이벤트 발생 직전에 로그 추가:
```csharp
if (_lastFileCount == null || !_lastFileCount.Equals(stats))
{
    _logger.LogWarning("★ FileCount CHANGED: Cam1={Cam1}", stats.Cam1Count);  // 추가
    // ...
    await _dispatcher.InvokeAsync(() =>
    {
        _logger.LogWarning("★ InvokeAsync EXECUTING");  // 추가
        FileCountsUpdated?.Invoke(this, stats);
    });
}
```

### Step 3: 구독자 호출 확인
`DashboardViewModel.UpdateFileCountStatistics` 시작 부분에 로그 추가:
```csharp
public void UpdateFileCountStatistics(FileCountStatistics e)
{
    _logger.LogWarning("★ UpdateFileCountStatistics CALLED: Cam1={Cam1}", e.Cam1Count);  // 추가
    // ...
}
```

### Step 4: DI 수명 주기 확인 (완료)
`App.xaml.cs:203`에서 확인:
```csharp
// Singleton으로 등록됨 ✓
services.AddSingleton<IStatisticsService, StatisticsService>();
```

### Step 5: `_currentConfig` 설정 확인
`GetFileCountsAsync` 시작 부분에 로그 추가:
```csharp
public async Task<FileCountStatistics> GetFileCountsAsync()
{
    var config = _currentConfig ?? await _configManager.LoadConfigurationAsync<ChronoView.Models.ApplicationConfiguration>();
    _logger.LogWarning("★ GetFileCountsAsync: _currentConfig is null={IsNull}, using config from {Source}", 
        _currentConfig == null, 
        _currentConfig == null ? "disk" : "memory");  // 추가
    
    var stats = new FileCountStatistics();
    // ...
}
```

### Step 6: `GetFileCountsAsync` 반환값 확인
`MonitorFileCountsAsync` 루프 내에서 로그 추가:
```csharp
var stats = await GetFileCountsAsync();
_logger.LogWarning("★ GetFileCountsAsync result: Cam1={Cam1}, Cam2={Cam2}, Cam3={Cam3}", 
    stats.Cam1Count, stats.Cam2Count, stats.Cam3Count);  // 추가

// Only notify if counts have changed
if (_lastFileCount == null || !_lastFileCount.Equals(stats))
{
    _logger.LogWarning("★ FileCount CHANGED: _lastFileCount is null={IsNull}", _lastFileCount == null);  // 추가
    // ...
}
```

---

## 코드 확인 결과 (Code Verification Results)

### ✅ 확인된 사항

1. **DI 등록**: `StatisticsService`는 Singleton으로 등록됨 (`App.xaml.cs:203`)
2. **FileCountStatistics.Equals**: 정상 구현됨 (모든 카운트 필드 비교, `LastUpdated` 제외)
3. **CountFilesInDirectoryAsync**: `SearchOption.AllDirectories` 사용 (이미 해결됨)
4. **Dispatcher 처리**: `CheckAccess()`로 UI 스레드 여부 확인 후 적절히 처리
5. **Debounce 시간**: 실제 코드는 50ms (문서의 500ms는 오기재)

### ❌ 기각된 가설

1. **첫 폴링 전 대기 시간**: 시간이 지나도 업데이트되지 않으므로 기각
2. **DI 인스턴스 불일치**: Singleton으로 등록되어 있으므로 기각

### ⚠️ 추가 확인 필요

1. **`_currentConfig` 설정**: `StartMonitoringAsync`에서 설정되지만, `GetFileCountsAsync` 호출 시점에 null일 수 있음
2. **`GetFileCountsAsync` 반환값**: 항상 같은 값을 반환하는지 확인 필요
3. **예외 처리**: 예외가 발생해도 catch에서 무시되고 계속 진행됨

---

## 요약 (Summary)

| 문제 영역 | 가능성 | 검증 필요 | 상태 |
|-----------|--------|-----------|------|
| 폴링 루프 미실행 | 낮음 | Step 1 로그로 확인 | 가능성 낮음 (그리드 작동) |
| GetFileCountsAsync 반환값 | **⭐ 매우 높음** | Step 6 로그로 확인 | **최우선 확인** |
| Dispatcher 마샬링 실패 | 낮음 | Step 2 로그로 확인 | 가능성 낮음 |
| DI 인스턴스 불일치 | **없음** | Step 4 코드로 확인 | **기각됨** |
| `_currentConfig` Race Condition | **⭐ 매우 높음** | Step 5 로그로 확인 | **최우선 확인** |
| 첫 폴링 대기 시간 | **없음** | - | **기각됨** |

**가장 유력한 원인** (그리드가 작동하므로 경로 자체는 올바름):

1. **`_currentConfig` Race Condition 또는 null 문제** (⭐ 매우 유력)
   - `StartMonitoringAsync`에서 `_currentConfig` 설정 후 바로 `Task.Run` 시작
   - 첫 번째 `GetFileCountsAsync` 호출 시 `_currentConfig`가 null이면 디스크에서 다른 설정 로드
   - 또는 `_currentConfig`가 설정되기 전에 첫 폴링이 실행될 수 있음 (드묾)
   - **증거**: Refresh는 작동 (명시적으로 `_currentConfig` 설정 후 호출), Start는 실패 (백그라운드 태스크)

2. **`GetFileCountsAsync`가 항상 0 반환** (⭐ 매우 유력)
   - 경로는 올바르지만 (`MonitoringOrchestrator`가 작동하므로)
   - `CountFilesInDirectoryAsync`가 예외를 던지고 catch에서 0 반환
   - 또는 파일 카운팅 로직 자체에 문제
   - **증거**: 그리드는 작동하지만 파일 개수는 0

3. **이벤트는 발생하지만 UI에 반영 안 됨** (가능성 낮음)
   - `Dispatcher.InvokeAsync` 실패 또는 구독자 문제
