# 이벤트 기반 vs 폴링 기반 코드 분석

## 개요

현재 코드베이스는 파일 감시와 관련하여 이벤트 기반과 폴링 기반이 혼재되어 있습니다. 이 문서는 각 부분이 어떤 방식으로 동작하는지 상세히 분석합니다.

**작성일**: 2025-01-17  
**목적**: 이벤트 기반으로 전환하기 위한 현재 상태 파악

---

## 1. C# 코드 (ChronoView)

### 1.1 FileWatcherService.cs - 하이브리드 방식

**위치**: `ChronoView/Core/FileWatching/FileWatcherService.cs`

#### 이벤트 기반 부분

```239:256:ChronoView/Core/FileWatching/FileWatcherService.cs
private FileSystemWatcher CreateWatcher(string path)
{
    var watcher = new FileSystemWatcher(path)
    {
        NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
        IncludeSubdirectories = true,
        InternalBufferSize = 64 * 1024 // 64KB buffer
    };

    // Subscribe to all events
    watcher.Created += OnFileSystemEvent;
    watcher.Changed += OnFileSystemEvent;
    watcher.Deleted += OnFileSystemEvent;
    watcher.Renamed += OnFileSystemEvent;
    watcher.Error += OnWatcherError;

    return watcher;
}
```

- **방식**: `FileSystemWatcher`를 사용한 이벤트 구독
- **이벤트 타입**: `Created`, `Changed`, `Deleted`, `Renamed`, `Error`
- **처리**: `OnFileSystemEvent` 메서드에서 이벤트를 받아 `_eventChannel`에 버퍼링

```258:311:ChronoView/Core/FileWatching/FileWatcherService.cs
private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
{
    try
    {
        // Diagnostic logging: confirm FileSystemWatcher is firing
        _logger.LogDebug("FileSystemEvent detected: {ChangeType} - {Path}", e.ChangeType, e.FullPath);

        // CRITICAL FIX: Normal folder detection via stitched_original.png
        // When stitched_original.png is created, report parent folder as Normal folder instead
        var fileName = Path.GetFileName(e.FullPath);
        FileSystemEventArgs eventToBuffer = e;

        if (!string.IsNullOrEmpty(fileName) &&
            fileName.Equals("stitched_original.png", StringComparison.OrdinalIgnoreCase) &&
            e.ChangeType == WatcherChangeTypes.Created)
        {
            var parentDir = Path.GetDirectoryName(e.FullPath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                var parentDirName = Path.GetFileName(parentDir);

                if (!string.IsNullOrEmpty(parentDirName) &&
                    parentDirName.StartsWith("C", StringComparison.OrdinalIgnoreCase) &&
                    parentDirName.Contains('T'))
                {
                    // Convert to parent folder event
                    var grandParentDir = Path.GetDirectoryName(parentDir) ?? parentDir;
                    eventToBuffer = new FileSystemEventArgs(
                        WatcherChangeTypes.Created,
                        grandParentDir,
                        parentDirName);

                    _logger.LogInformation("Detected stitched_original.png → Normal folder event: {Folder}", parentDir);
                }
            }
        }

        // Write to channel for buffering (non-blocking)
        if (_eventChannel.Writer.TryWrite(eventToBuffer))
        {
            Interlocked.Increment(ref _eventCount);
            _lastEventTime = DateTime.UtcNow;
            _logger.LogDebug("Event buffered successfully");
        }
        else
        {
            _logger.LogWarning("Failed to buffer file system event: {Path}", eventToBuffer.FullPath);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error handling file system event: {Path}", e.FullPath);
    }
}
```

#### 폴링 기반 부분

```166:171:ChronoView/Core/FileWatching/FileWatcherService.cs
// Start Polling Timer if enabled
if (_options.EnablePolling)
{
    _pollingTimer = new System.Threading.Timer(PollDirectories, null, _options.PollingIntervalMs, _options.PollingIntervalMs);
    _logger.LogInformation("Polling started with interval {Interval}ms", _options.PollingIntervalMs);
}
```

- **방식**: `System.Threading.Timer`를 사용한 주기적 폴링
- **설정**: `FileWatcherOptions.EnablePolling`이 `true`일 때만 활성화
- **인터벌**: `FileWatcherOptions.PollingIntervalMs` (기본값: 5000ms)

```443:535:ChronoView/Core/FileWatching/FileWatcherService.cs
private void PollDirectories(object? state)
{
    if (!IsWatching) return;

    int newFilesDetected = 0;
    _logger.LogDebug("Polling iteration started for {Count} paths", _watchedPaths.Count);

    try
    {
        foreach (var path in _watchedPaths)
        {
            if (!Directory.Exists(path)) continue;

            // Use EnumerateFiles for lower memory usage
            // Note: EnumerateFiles can throw if permission denied, handle gracefully
            try 
            {
                foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                {
                    bool isNew = false;
                    lock (_lockObject)
                    {
                        if (!_knownFiles.Contains(file))
                        {
                            // Found a new file!
                            // NOTE: We don't add to _knownFiles here. 
                            // We let ProcessEventsAsync -> ShouldProcessEvent handle the add.
                            // This prevents race conditions where we add here but event is delayed.
                            isNew = true; 
                        }
                    }

                    if (isNew)
                    {
                        // Create event and push to channel
                        var directory = Path.GetDirectoryName(file);
                        var fileName = Path.GetFileName(file);
                        if (directory != null)
                        {
                            FileSystemEventArgs args;

                            // CRITICAL: Apply same stitched_original.png → folder conversion as OnFileSystemEvent
                            if (fileName.Equals("stitched_original.png", StringComparison.OrdinalIgnoreCase))
                            {
                                var parentDirName = Path.GetFileName(directory);
                                if (!string.IsNullOrEmpty(parentDirName) &&
                                    parentDirName.StartsWith("C", StringComparison.OrdinalIgnoreCase) &&
                                    parentDirName.Contains('T'))
                                {
                                    // Convert to parent folder event
                                    var grandParentDir = Path.GetDirectoryName(directory) ?? directory;
                                    args = new FileSystemEventArgs(
                                        WatcherChangeTypes.Created,
                                        grandParentDir,
                                        parentDirName);

                                    _logger.LogInformation("Polling: Detected stitched_original.png → Normal folder event: {Folder}", directory);
                                }
                                else
                                {
                                    // Not a Normal folder pattern, use regular file event
                                    args = new FileSystemEventArgs(WatcherChangeTypes.Created, directory, fileName);
                                    _logger.LogDebug("Polling detected new file: {Path}", file);
                                }
                            }
                            else
                            {
                                // Regular file event
                                args = new FileSystemEventArgs(WatcherChangeTypes.Created, directory, fileName);
                                _logger.LogDebug("Polling detected new file: {Path}", file);
                            }

                            newFilesDetected++;

                            // Try write to channel (reusing existing mechanism)
                            _eventChannel.Writer.TryWrite(args);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error polling directory: {Path}", path);
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in polling task");
    }
    
    _logger.LogDebug("Polling iteration completed. New files detected: {Count}", newFilesDetected);
}
```

- **동작**: 주기적으로 디렉토리를 스캔하여 `_knownFiles`와 비교
- **목적**: 네트워크 드라이브(SMB) 등에서 `FileSystemWatcher`가 신뢰성 없을 때 보완

#### Health Check Timer (폴링)

```94:96:ChronoView/Core/FileWatching/FileWatcherService.cs
// Set up health check timer (30 seconds)
_healthCheckTimer = new System.Threading.Timer(PerformHealthCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
```

```537:573:ChronoView/Core/FileWatching/FileWatcherService.cs
private void PerformHealthCheck(object? state)
{
    try
    {
        if (!IsWatching)
        {
            return;
        }

        var timeSinceLastEvent = DateTime.UtcNow - _lastEventTime;
        var eventCount = Interlocked.Exchange(ref _eventCount, 0);

        // Check if watchers are still alive
        var aliveWatchers = _watchers.Count(w => w.EnableRaisingEvents);
        if (aliveWatchers == 0)
        {
            HealthStatus = WatcherHealthStatus.Unhealthy;
            _logger.LogWarning("No active file watchers detected");
        }
        else if (aliveWatchers < _watchers.Count)
        {
            HealthStatus = WatcherHealthStatus.Degraded;
            _logger.LogWarning("Some file watchers are not active: {Active}/{Total}", aliveWatchers, _watchers.Count);
        }
        else
        {
            HealthStatus = WatcherHealthStatus.Healthy;
        }

        _logger.LogDebug("Health check: Status={Status}, Events={EventCount}, TimeSinceLastEvent={TimeSinceLastEvent}",
            HealthStatus, eventCount, timeSinceLastEvent);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error performing health check");
    }
}
```

- **방식**: 30초마다 주기적으로 상태 체크
- **목적**: FileSystemWatcher가 정상 동작하는지 모니터링

---

## 2. Python 코드 (script)

### 2.1 WatchdogManager - 순수 이벤트 기반 ✅

**위치**: `script/infrastructure/watchdog_manager.py`

```78:124:script/infrastructure/watchdog_manager.py
def start_watchdog(self):
    """
    Watchdog 감시 시작
    
    모든 설정된 폴더에 대해 watchdog observer를 시작합니다.
    """
    self.stop_watchdog()
    
    try:
        self.observer = Observer()
        
        # 모든 폴더 타입 순회
        folder_types = ["normal", "normal2", "nir", "nir2", 
                      "cam1", "cam2", "cam3", "cam4", "cam5", "cam6"]
        
        for folder_type in folder_types:
            # 일반카메라의 경우 실제 경로 계산
            if folder_type in ["normal", "normal2"]:
                folder = self.get_effective_path(folder_type)
            else:
                folder = self.settings.get(folder_type, "")
            
            if folder and os.path.isdir(folder):
                # 재귀 옵션 결정
                recursive = self.should_use_recursive(folder_type)

                # 이벤트 핸들러 생성 및 스케줄링
                handler = FolderEventHandler(
                    self.event_signal,  # ✓ Signal 객체 전달
                    folder_type,
                    self.settings
                )
                self.observer.schedule(handler, folder, recursive=recursive)
                
                # 로그 출력
                mode_str = "재귀 감시" if recursive else "단일 레벨 감시"
                self.log_callback(f"[Watchdog] {folder_type}: {folder} ({mode_str})")
        
        self.observer.start()
        self.log_callback("[Watchdog] 폴더 감시 시작 완료")
        
    except Exception as e:
        self.log_callback(f"[ERROR] Watchdog 시작 실패: {e}")
        print(f"[ERROR] Watchdog 시작 실패: {e}", flush=True)
        import traceback
        traceback.print_exc()
```

- **방식**: `watchdog` 라이브러리의 `Observer` 사용
- **이벤트 처리**: `FolderEventHandler.on_any_event`에서 Qt Signal으로 전달
- **특징**: 순수 이벤트 기반, 폴링 없음

```34:50:script/infrastructure/watchdog_manager.py
def on_any_event(self, event):
    """
    모든 파일 시스템 이벤트 처리

    watchdog 스레드에서 실행되며, Qt Signal을 emit하여
    메인 GUI 스레드로 이벤트를 안전하게 전달합니다.
    """
    if event.is_directory:
        return

    event_type = event.event_type  # 'created', 'modified', 'deleted', 'moved'
    src_path = event.src_path

    # ✓ Signal emit (스레드 안전)
    # Qt가 자동으로 메인 스레드로 전환하여 슬롯 실행
    if self.event_signal:
        self.event_signal.file_changed.emit(event_type, src_path, self.folder_type)
```

### 2.2 FileCountWorker - 하이브리드 방식

**위치**: `script/services/file_count_worker.py`

#### 이벤트 기반 부분

```16:26:script/services/file_count_worker.py
class CountFolderEventHandler(FileSystemEventHandler):
    """파일 시스템 변화 감지 핸들러"""
    def __init__(self, worker):
        super().__init__()
        self.worker = worker

    def on_any_event(self, event):
        """파일 시스템 변화가 감지되면 워커에 알림"""
        if not event.is_directory:
            self.worker.trigger_count()
```

- **방식**: watchdog 이벤트로 변화 감지
- **동작**: 이벤트 발생 시 `trigger_count()` 호출

#### 폴링 기반 부분

```131:193:script/services/file_count_worker.py
def run(self):
    """백그라운드 스레드 실행"""
    while self.is_running:
        try:
            # 비활성화 상태면 카운트하지 않음
            if not self.is_enabled:
                self.msleep(500)  # 0.5초 대기
                continue

            # watchdog 시작 (활성화되어 있을 때만)
            if not self.observer or not self.observer.is_alive():
                self.start_watchdog()

            current_time = time.time()
            time_since_last_change = current_time - self.last_change_time

            # 1. 변화가 감지되었거나
            # 2. 10초 이상 변화가 없을 때 한 번 확인
            if self.needs_count or time_since_last_change >= 10.0:
                # ✅ camera 하위폴더 옵션 적용하여 실제 경로 가져오기
                normal_base = self.settings.get("normal", "")
                normal_use_camera = self.settings.get("use_camera_subfolder_normal", False)
                normal_path = get_effective_path(normal_base, normal_use_camera)

                normal2_base = self.settings.get("normal2", "")
                normal2_use_camera = self.settings.get("use_camera_subfolder_normal2", False)
                normal2_path = get_effective_path(normal2_base, normal2_use_camera)
                nir_path = self.settings.get("nir", "")
                nir2_path = self.settings.get("nir2", "")
                cam1_path = self.settings.get("cam1", "")
                cam2_path = self.settings.get("cam2", "")
                cam3_path = self.settings.get("cam3", "")
                cam4_path = self.settings.get("cam4", "")
                cam5_path = self.settings.get("cam5", "")
                cam6_path = self.settings.get("cam6", "")

                # 파일 개수 카운트
                nir_count = self._count_nir_files(nir_path)
                nir2_count = self._count_nir_files(nir2_path)
                normal_count = self._count_folders(normal_path)
                normal2_count = self._count_folders(normal2_path)
                cam1_count = self._count_image_files(cam1_path)
                cam2_count = self._count_image_files(cam2_path)
                cam3_count = self._count_image_files(cam3_path)
                cam4_count = self._count_image_files(cam4_path)
                cam5_count = self._count_image_files(cam5_path)
                cam6_count = self._count_image_files(cam6_path)

                # Signal 발생 (메인 스레드로 결과 전달)
                self.counts_updated.emit(nir_count, nir2_count, normal_count, normal2_count, cam1_count, cam2_count, cam3_count, cam4_count, cam5_count, cam6_count)

                # 플래그 초기화
                self.needs_count = False
                if time_since_last_change >= 10.0:
                    self.last_change_time = current_time

        except Exception as e:
            # 에러가 발생해도 스레드는 계속 실행
            print(f"[FileCountWorker] 카운트 오류: {e}")

        # 짧은 대기 (CPU 과부하 방지)
        self.msleep(100)  # 0.1초마다 체크 (실제 카운트는 변화가 있거나 10초마다만)
```

- **방식**: `while` 루프 + `msleep(100)` (0.1초마다 체크)
- **폴링 조건**: 
  - `needs_count` 플래그가 설정되었거나 (이벤트 기반 트리거)
  - 10초 이상 변화가 없을 때 (폴링)
- **문제점**: 이벤트가 발생해도 0.1초마다 루프를 돌면서 체크하는 폴링 방식

### 2.3 FileMatcherWorker - 폴링 기반

**위치**: `script/domain/file_matcher.py`

```342:374:script/domain/file_matcher.py
def run(self):
    """백그라운드 스레드 실행"""
    while self.is_running:
        try:
            # 비활성화 상태면 스캔하지 않음
            if not self.is_enabled:
                self.msleep(500)  # 0.5초 대기
                continue

            current_time = time.time()
            time_since_last_scan = current_time - self.last_scan_time

            # 10초마다 또는 변화 감지 시 스캔
            if self.needs_scan or time_since_last_scan >= 10.0:
                # 백그라운드에서 풀스캔 실행
                unmatched = self.file_matcher.scan_and_build_unmatched(self.settings)

                # Signal 발생 (메인 스레드로 자동 전달)
                self.scan_completed.emit(unmatched)

                # 플래그 초기화
                self.needs_scan = False
                self.last_scan_time = current_time

        except Exception as e:
            # 에러가 발생해도 스레드는 계속 실행
            print(f"[FileMatcherWorker] 스캔 오류: {e}")
            import traceback
            traceback.print_exc()

        # 0.1초마다 체크 (실제 스캔은 변화가 있거나 10초마다만)
        self.msleep(100)
```

- **방식**: `while` 루프 + `msleep(100)` (0.1초마다 체크)
- **폴링 주기**: 10초마다 전체 스캔
- **목적**: WSL 환경에서 watchdog 이벤트가 발생하지 않는 문제 해결
- **문제점**: 순수 폴링 방식, 이벤트 기반이 아님

### 2.4 MonitoringApp - 하이브리드 방식

**위치**: `script/apps/monitoring_app.py`

#### 이벤트 기반 부분

```178:182:script/apps/monitoring_app.py
# watchdog 이벤트 디바운스 타이머 (설정 인터벌로 동작)
self.update_timer = QTimer(self)
self.update_timer.setTimerType(Qt.TimerType.CoarseTimer)
self.update_timer.setSingleShot(True)
self.update_timer.timeout.connect(self.process_event_queue)
```

- **방식**: `QTimer` (단발성, SingleShot)
- **목적**: watchdog 이벤트를 디바운스하여 일괄 처리
- **특징**: 이벤트가 발생할 때마다 타이머가 재시작됨 (디바운스)

#### 폴링 기반 부분

```189:192:script/apps/monitoring_app.py
# ✅ Watchdog 상태 모니터링 타이머 (30초마다 확인)
self.watchdog_monitor_timer = QTimer(self)
self.watchdog_monitor_timer.timeout.connect(self.watchdog_manager.check_status)
self.watchdog_monitor_timer.setInterval(30000)  # 30초
```

- **방식**: `QTimer` (주기적)
- **주기**: 30초마다
- **목적**: watchdog observer가 살아있는지 체크

### 2.5 기타 폴링 기반 컴포넌트들

#### NIRStatusWidget

**위치**: `script/ui/components/nir_status_widget.py`

```73:77:script/ui/components/nir_status_widget.py
def _init_timer(self):
    """타이머 초기화"""
    self.timer = QTimer(self)
    self.timer.timeout.connect(self.update_status)
    self.timer.start(self.update_interval)
```

- **방식**: `QTimer` (주기적)
- **주기**: `update_interval_ms` (기본값: 2000ms)
- **목적**: NIR 상태 주기적 업데이트

#### FileCountMonitor

**위치**: `script/infrastructure/monitoring/file_count_monitor.py`

```31:34:script/infrastructure/monitoring/file_count_monitor.py
self.update_timer = QTimer(self)
self.update_timer.setInterval(1000)  # 1초마다 업데이트 (렉 방지)
self.update_timer.timeout.connect(self.update_counts)
self.update_timer.start()
```

- **방식**: `QTimer` (주기적)
- **주기**: 1초마다
- **목적**: 파일 개수 주기적 업데이트

#### MemoryMonitor

**위치**: `script/debug/memory_monitor.py`

```28:30:script/debug/memory_monitor.py
self.timer = QTimer()
self.timer.timeout.connect(self._check_memory)
self.timer.start(self.interval * 1000)
```

- **방식**: `QTimer` (주기적)
- **목적**: 메모리 사용량 주기적 체크

---

## 3. 요약

### 이벤트 기반 컴포넌트 ✅

1. **WatchdogManager** (Python)
   - `watchdog` 라이브러리 사용
   - 순수 이벤트 기반

2. **FileSystemWatcher** (C#)
   - `FileSystemWatcher` 클래스 사용
   - 이벤트 구독 방식

3. **MonitoringApp.update_timer** (Python)
   - 디바운스용 타이머 (이벤트 트리거)

### 폴링 기반 컴포넌트 ❌

1. **FileWatcherService.PollDirectories** (C#)
   - `System.Threading.Timer`로 주기적 스캔
   - 네트워크 드라이브 대응용

2. **FileCountWorker.run()** (Python)
   - `while` 루프 + `msleep(100)`
   - 10초마다 또는 이벤트 트리거 시 카운트

3. **FileMatcherWorker.run()** (Python)
   - `while` 루프 + `msleep(100)`
   - 10초마다 전체 스캔

4. **MonitoringApp.watchdog_monitor_timer** (Python)
   - 30초마다 watchdog 상태 체크

5. **FileWatcherService.PerformHealthCheck** (C#)
   - 30초마다 health check

6. **NIRStatusWidget.timer** (Python)
   - 2초마다 상태 업데이트

7. **FileCountMonitor.update_timer** (Python)
   - 1초마다 파일 개수 업데이트

8. **MemoryMonitor.timer** (Python)
   - 주기적 메모리 체크

### 하이브리드 컴포넌트 ⚠️

1. **FileWatcherService** (C#)
   - 이벤트 기반: `FileSystemWatcher`
   - 폴링 기반: `PollDirectories` (옵션)

2. **FileCountWorker** (Python)
   - 이벤트 기반: watchdog 이벤트 감지
   - 폴링 기반: `while` 루프로 주기적 체크

---

## 4. 문제점 분석

### 4.1 불필요한 폴링

1. **FileCountWorker**: watchdog 이벤트가 발생해도 0.1초마다 루프를 돌면서 체크
   - **개선**: 이벤트 발생 시 즉시 카운트하도록 변경

2. **FileMatcherWorker**: 10초마다 무조건 전체 스캔
   - **개선**: watchdog 이벤트 기반으로 변경, 필요 시에만 스캔

3. **Health Check / Status Monitor**: 주기적 체크
   - **개선**: 이벤트 기반으로 변경하거나, 최소한 주기를 늘림

### 4.2 중복 감시

- **FileWatcherService**: 이벤트와 폴링이 동시에 동작하여 중복 감지 가능
- **FileCountWorker**: watchdog 이벤트와 폴링이 혼재

### 4.3 리소스 낭비

- 주기적 스캔으로 인한 CPU/디스크 I/O 낭비
- 불필요한 타이머들

---

## 5. 이벤트 기반 전환 방향

### 우선순위 1: 핵심 파일 감시

1. **FileCountWorker**: watchdog 이벤트만 사용, 폴링 제거
2. **FileMatcherWorker**: watchdog 이벤트 기반으로 변경, 폴링 제거
3. **FileWatcherService**: 폴링 옵션 제거 또는 기본 비활성화

### 우선순위 2: 상태 모니터링

1. **Health Check**: 이벤트 기반으로 변경 (예: watchdog 에러 이벤트)
2. **Watchdog Status Monitor**: watchdog 라이브러리의 상태 이벤트 활용

### 우선순위 3: UI 업데이트

1. **NIRStatusWidget**: 이벤트 기반 업데이트로 변경
2. **FileCountMonitor**: FileCountWorker의 Signal만 사용

---

## 6. 참고 자료

- `docs/architecture/module_file_watcher_service.md`: FileWatcherService 아키텍처 문서
- `docs/spec/fix-missing-data-display/`: 폴링 기능 추가 스펙
- `docs/spec/event_processing_optimization/`: 이벤트 처리 최적화 스펙







