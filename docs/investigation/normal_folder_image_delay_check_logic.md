# 일반카메라(Normal) 폴더 생성 후 이미지 파일 지연 확인 로직 조사 리포트

## 문서 정보
- **작성일**: 2025-01-06
- **조사 목적**: 일반카메라(normal) 폴더가 생성된 후 20-40초 지연으로 이미지 파일이 생성되는 상황에서, 이미지 파일이 없을 때 다음 폴링에서 재확인하는 로직이 존재하는지 확인
- **조사 범위**: 전체 코드베이스

## 요약

**결론: 일반카메라(normal) 폴더가 생성된 후 이미지 파일이 없을 때 다음 폴링에서 재확인하는 로직이 구현되었습니다.**

**해결 방법**: 이미지 파일(`stitched_original.png`)이 없는 Normal 폴더는 `_knownFiles`에 추가하지 않도록 수정했습니다. 이렇게 하면 폴링이 계속 해당 폴더를 감지하여, 이미지 파일이 생성되면 처리할 수 있습니다.

**중요**: 실시간 감지는 하이브리드 방식(FileSystemWatcher 이벤트 + 폴링)이며, 폴링은 **새로 생성된 파일/폴더를 감지**하는 용도입니다. 이제 이미지 파일이 없는 Normal 폴더는 `_knownFiles`에 추가되지 않아 폴링에서 계속 확인됩니다.

## 실제 환경 동작 특성

### 일반카메라(Normal) 데이터 생성 시퀀스
1. **폴더 생성**: Normal 폴더가 먼저 생성됨 (예: `C251203T155910_0/`)
2. **이미지 생성 지연**: 모델이 사용한 후 **20-40초 후**에 `stitched_original.png` 파일이 생성됨
3. **문제점**: 폴더만 생성되고 이미지 파일이 없는 상태가 일시적으로 존재

### 필요한 로직
- 폴더가 생성되었지만 이미지 파일이 없는 경우
- 해당 폴더를 "펜딩 목록"에 추가
- 다음 폴링 시 해당 폴더의 이미지 파일 존재 여부를 재확인
- 이미지 파일이 생성되면 썸네일 로드 및 그룹 업데이트

## 코드 조사 결과

### 1. InitialScanner.cs - 초기 스캔 로직

**파일 위치**: `ChronoView/Core/FileWatching/InitialScanner.cs`

**관련 코드**:
```113:124:ChronoView/Core/FileWatching/InitialScanner.cs
case DataType.Normal:
    if (!string.IsNullOrEmpty(config.Normal1Path) && Directory.Exists(config.Normal1Path))
    {
        var folders = Directory.GetDirectories(config.Normal1Path);
        files.AddRange(folders);
    }
    if (!string.IsNullOrEmpty(config.Normal2Path) && Directory.Exists(config.Normal2Path))
    {
        var folders = Directory.GetDirectories(config.Normal2Path);
        files.AddRange(folders);
    }
    break;
```

**분석**:
- 초기 스캔 시 Normal 폴더를 스캔하지만, **폴더 내부의 이미지 파일 존재 여부는 확인하지 않음**
- `Directory.GetDirectories()`만 사용하여 폴더 목록을 가져옴
- `stitched_original.png` 파일 존재 여부를 확인하는 로직 없음

### 2. GroupManager.cs - 그룹 생성 로직

**파일 위치**: `ChronoView/Core/FileWatching/GroupManager.cs`

**관련 코드**:
```225:230:ChronoView/Core/FileWatching/GroupManager.cs
case FileType.Normal:
    group.NormalFolder = filePath;
    group.MainImagePath = Path.Combine(filePath, "stitched_original.png");
    group.Timestamp = timestamp.Value;
    group.Status = GroupStatus.Complete;
    break;
```

**분석**:
- Normal 폴더를 처리할 때 `MainImagePath`를 설정하지만, **실제 파일 존재 여부는 확인하지 않음**
- `File.Exists()` 체크 없이 경로만 설정
- 이미지 파일이 없어도 그룹이 생성되고 `Status`가 `Complete`로 설정됨

### 3. FileWatcherService.cs - 하이브리드 감지 방식

**파일 위치**: `ChronoView/Core/FileWatching/FileWatcherService.cs`

#### 3.1 하이브리드 감지 방식

**관련 코드**:
```55:75:ChronoView/Core/FileWatching/FileWatcherService.cs
var watcher = new FileSystemWatcher(path)
{
    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime,
    Filter = "*.*",
    IncludeSubdirectories = true, // We need to see contents of Normal folders
    EnableRaisingEvents = true
};

watcher.Created += (s, e) => ProcessWatcherEvent(e);
watcher.Renamed += (s, e) => ProcessWatcherEvent(e);

_watchers.Add(watcher);
_logger.LogInformation("Started watching {Path}", path);

// 3. Setup Polling
if (options.EnablePolling)
{
    _pollingTimer = new System.Threading.Timer(OnPollTick, null, options.PollingIntervalMs, options.PollingIntervalMs);
    _logger.LogInformation("Polling enabled every {Interval}ms", options.PollingIntervalMs);
}
```

**설정값**:
- `EnablePolling`: 기본값 `true` (`ApplicationConfiguration.cs:368`)
- `PollingIntervalMs`: 기본값 `2000ms` (2초) (`ApplicationConfiguration.cs:373`)

**분석**:
- **하이브리드 방식**: FileSystemWatcher (이벤트 기반) + Polling Timer (폴링 기반)
- FileSystemWatcher: 파일/폴더 생성 시 즉시 이벤트 발생
- Polling Timer: 2초마다 디렉토리 스캔하여 새로 생성된 파일/폴더 감지

#### 3.2 폴링 로직의 한계

**관련 코드**:
```125:170:ChronoView/Core/FileWatching/FileWatcherService.cs
private void OnPollTick(object? state)
{
    if (!_isWatching) return;

    // 1. 모든 새 파일/폴더 수집
    var newItems = new List<(string Path, bool IsDirectory)>();

    foreach (var path in _paths)
    {
        if (!Directory.Exists(path)) continue;

        try
        {
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (ShouldProcessPath(file))
                    newItems.Add((file, false));
            }

            var dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
            foreach (var dir in dirs)
            {
                if (ShouldProcessPath(dir))
                    newItems.Add((dir, true));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Poller tick error for {Path}: {Message}", path, ex.Message);
        }
    }

    // 2. 파일명 기준 정렬 (타임스탬프가 파일명에 포함됨)
    var sortedItems = newItems.OrderBy(item => Path.GetFileName(item.Path)).ToList();

    // 3. 정렬된 순서로 이벤트 발생
    foreach (var (itemPath, isDir) in sortedItems)
    {
        var args = new FileSystemEventArgs(
            WatcherChangeTypes.Created,
            Path.GetDirectoryName(itemPath)!,
            Path.GetFileName(itemPath));
        HandleEvent(args);
    }
}
```

**분석**:
- 폴링은 **새로 생성된 파일/폴더를 감지**하는 용도
- `_knownFiles`에 없는 항목만 처리 (중복 방지)
- **이미 감지된 폴더의 내부 파일이 나중에 생성되는지 재확인하지 않음**
- 폴더가 이미 `_knownFiles`에 있으면, 그 폴더 내부의 새 파일은 폴링에서 감지되지 않음

### 4. MonitoringOrchestrator.cs - 실시간 이벤트 처리

**파일 위치**: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs`

#### 4.1 실시간 이벤트 처리

**관련 코드**:
```252:264:ChronoView/Core/FileWatching/MonitoringOrchestrator.cs
// ⚡ FAST CAPTURE: Check if this is stitched_original.png
if (FileNamingHelper.IsStitchedImage(eventArgs.FullPath))
{
    await _imageCache.HandleStitchedImageCaptureAsync(eventArgs.FullPath, workerId, OnGroupUpdated);
    
    // If it's a stitched image, the "real" path we care about for grouping is the parent folder
    var parentFolder = Path.GetDirectoryName(eventArgs.FullPath);
    if (!string.IsNullOrEmpty(parentFolder) && FileNamingHelper.IsNormalFolder(Path.GetFileName(parentFolder)))
    {
        processPath = parentFolder;
        fileType = FileType.Normal;
    }
}
```

**분석**:
- `stitched_original.png` 파일 생성 이벤트를 감지하면 이미지를 캡처하고 그룹을 업데이트
- 하지만 **폴더만 생성되고 이미지 파일이 없을 때 재확인하는 로직은 없음**
- FileSystemWatcher가 폴더 생성 이벤트를 감지해도, 이미지 파일이 없으면 그룹만 생성되고 썸네일은 로드되지 않음

#### 4.2 초기 스캔 시 이미지 미리 로드

**관련 코드**:
```426:459:ChronoView/Core/FileWatching/MonitoringOrchestrator.cs
// ⚡ PRE-LOAD NORMAL IMAGES INTO CACHE
// This eliminates the 50-second delay when UI loads thumbnails
_logger.LogInformation("Pre-loading Normal images into cache...");
var cacheLoadStopwatch = Stopwatch.StartNew();
int cachedCount = 0, failedCount = 0;

foreach (var group in _groupManager.ActiveGroups)
{
    cancellationToken.ThrowIfCancellationRequested();
    
    if (!string.IsNullOrEmpty(group.NormalFolder))
    {
        var stitchedPath = Path.Combine(group.NormalFolder, "stitched_original.png");
        if (File.Exists(stitchedPath))
        {
            try
            {
                var image = await _imageCache.LoadImageIntoMemoryAsync(stitchedPath);
                if (image != null)
                {
                    cachedCount++;
                }
                else
                {
                    failedCount++;
                }
            }
            catch
            {
                failedCount++;
            }
        }
    }
}
```

**분석**:
- 초기 스캔 완료 후 이미 존재하는 이미지 파일을 미리 캐시에 로드
- `File.Exists()` 체크를 하므로, **이미지 파일이 없으면 스킵됨**
- 하지만 이미지 파일이 나중에 생성되는 경우를 처리하지 않음

### 5. 폴링 관련 코드

**조사 결과**: 현재 코드베이스에는 Normal 폴더를 위한 전용 폴링 로직이 존재하지 않습니다.

**참고 문서**: `docs/trouble/normal_folder_polling_implementation.md`에 폴링 구현에 대한 문서가 있지만, 실제 코드에는 구현되어 있지 않습니다.

**문서에서 언급된 로직**:
- `_pendingNormalFolders`: 이미지 파일을 기다리는 폴더 목록
- `CheckPendingFoldersAsync()`: 펜딩 폴더의 이미지 파일 생성 여부 확인
- `ProcessNormalFolderAsync()`: 폴더 처리 및 이미지 파일 존재 여부 확인

**현재 상태**: 이러한 필드나 메서드는 `MonitoringOrchestrator.cs`에 존재하지 않습니다.


## 현재 동작 방식

### 시나리오 1: 폴더만 생성되고 이미지 파일이 없는 경우

1. **폴더 생성 이벤트 감지**
   - FileSystemWatcher 또는 폴링이 폴더 생성 이벤트를 감지
   - `ProcessSingleEventAsync` 호출

2. **그룹 생성**
   - `CreateOrUpdateGroupAsync` 호출
   - `GroupManager.CreateGroupFromSingleFile`에서 그룹 생성
   - `MainImagePath`는 설정되지만 실제 파일 존재 여부는 확인하지 않음
   - 폴더가 `_knownFiles`에 추가됨

3. **이미지 파일 부재**
   - 이미지 파일이 없으므로 썸네일 로드 실패
   - UI에 썸네일이 표시되지 않음

4. **재확인 없음**
   - 폴더가 이미 `_knownFiles`에 있으므로, 다음 폴링에서도 스킵됨
   - 폴더 내부의 새 파일(`stitched_original.png`)이 생성되어도 폴링에서 감지되지 않음
   - `stitched_original.png` 파일 생성 이벤트가 FileSystemWatcher를 통해 발생해야만 처리됨
   - **하지만 FileSystemWatcher가 이벤트를 놓치면 영구적으로 감지되지 않음**

### 시나리오 2: 이미지 파일 생성 이벤트가 발생하는 경우

1. **이미지 파일 생성 이벤트 감지**
   - FileSystemWatcher가 `stitched_original.png` 생성 이벤트를 감지
   - `ProcessSingleEventAsync`에서 `FileNamingHelper.IsStitchedImage()` 체크

2. **이미지 캡처 및 그룹 업데이트**
   - `_imageCache.HandleStitchedImageCaptureAsync()` 호출
   - 이미지를 캐시에 저장하고 그룹 업데이트

3. **정상 동작**
   - 썸네일이 로드되고 UI에 표시됨

## 문제점 분석

### 1. 폴더 생성 시 이미지 파일 존재 여부 미확인

**현재 동작**:
- 폴더가 생성되면 즉시 그룹 생성
- 이미지 파일 존재 여부를 확인하지 않음
- `MainImagePath`만 설정하고 실제 파일은 확인하지 않음

**문제점**:
- 이미지 파일이 20-40초 후에 생성되는 경우, 그룹은 생성되지만 썸네일이 없음
- 사용자가 빈 썸네일을 보게 됨

### 2. 재확인 로직 부재

**현재 동작**:
- 폴더 생성 후 이미지 파일이 없으면 그대로 둠
- 폴더가 `_knownFiles`에 추가되어 다음 폴링에서 스킵됨
- 폴더 내부의 새 파일은 폴링에서 감지되지 않음 (폴더가 이미 알려진 항목이므로)
- 이미지 파일 생성 이벤트를 FileSystemWatcher에 의존

**문제점**:
- FileSystemWatcher가 이벤트를 놓치면 이미지 파일이 생성되어도 감지하지 못함
- 네트워크 드라이브나 WSL 경로에서는 이벤트가 제대로 전달되지 않을 수 있음
- 폴링이 있어도 이미 감지된 폴더의 내부 파일 변화는 감지하지 못함

### 3. 펜딩 폴더 관리 로직 부재

**현재 상태**:
- 이미지 파일을 기다리는 폴더 목록을 관리하는 로직이 없음
- NIR 파일의 경우 `_pendingNirFiles`가 있지만, Normal 폴더용은 없음

**비교**: NIR 파일 처리
```21:21:ChronoView/Core/FileWatching/GroupManager.cs
private readonly List<(string Path, DateTime Timestamp, DateTime AddedAt)> _pendingNirFiles = new();
```

- NIR 파일은 `_pendingNirFiles`에 저장하고 나중에 매칭 시도
- Normal 폴더는 이러한 펜딩 메커니즘이 없음

### 4. 폴링의 한계

**현재 폴링 동작**:
- 새로 생성된 파일/폴더만 감지 (`_knownFiles`에 없는 항목)
- 이미 감지된 폴더는 `_knownFiles`에 있어서 스킵됨
- 폴더 내부의 새 파일은 폴링에서 감지되지 않음

**필요한 동작**:
- 이미지 파일이 없는 폴더를 별도로 추적
- 폴링 시 해당 폴더의 이미지 파일 존재 여부를 재확인
- 이미지 파일이 생성되면 그룹 업데이트

## 필요한 로직

### 1. 폴더 생성 시 이미지 파일 확인

```csharp
// 의사코드
if (fileType == FileType.Normal)
{
    var stitchedPath = Path.Combine(folderPath, "stitched_original.png");
    if (!File.Exists(stitchedPath))
    {
        // 펜딩 폴더 목록에 추가
        _pendingNormalFolders.Add(folderPath, DateTime.UtcNow);
        return; // 이미지 파일이 생성될 때까지 대기
    }
}
```

### 2. 펜딩 폴더 재확인 로직

```csharp
// 의사코드
private async Task CheckPendingNormalFoldersAsync()
{
    foreach (var pendingFolder in _pendingNormalFolders.ToList())
    {
        var stitchedPath = Path.Combine(pendingFolder.Key, "stitched_original.png");
        if (File.Exists(stitchedPath))
        {
            // 이미지 파일이 생성됨 - 그룹 업데이트
            await UpdateGroupThumbnailAsync(pendingFolder.Key);
            _pendingNormalFolders.Remove(pendingFolder.Key);
        }
        else
        {
            // 타임아웃 체크 (예: 60초)
            if ((DateTime.UtcNow - pendingFolder.Value).TotalSeconds > 60)
            {
                _pendingNormalFolders.Remove(pendingFolder.Key);
            }
        }
    }
}
```

### 3. 펜딩 폴더 재확인 로직 (기존 폴링 활용)

**방안 1**: 기존 폴링 타이머 활용
- `FileWatcherService.OnPollTick`에서 펜딩 폴더도 함께 확인
- 기존 폴링 간격(2초) 활용

**방안 2**: 별도 폴링 타이머 추가
```csharp
// 의사코드
private System.Timers.Timer? _normalFolderPollingTimer;

private void StartNormalFolderPolling()
{
    _normalFolderPollingTimer = new System.Timers.Timer(1000); // 1초마다
    _normalFolderPollingTimer.Elapsed += async (s, e) => 
    {
        await CheckPendingNormalFoldersAsync();
    };
    _normalFolderPollingTimer.Start();
}
```

## 관련 문서

1. **normal_folder_polling_implementation.md**: 폴링 구현에 대한 문서 (실제 코드에는 미구현)
2. **normal_camera_move_logic_issue.md**: 일반카메라 이동 로직 문제 분석
3. **event_processing_optimization/01_requirements.md**: 이벤트 처리 최적화 요구사항

## 결론 및 해결 방법

### 구현된 해결 방법

**아이디어**: 이미지 파일이 없는 Normal 폴더는 `_knownFiles`에 추가하지 않음

**장점**:
- 별도의 펜딩 목록 관리 불필요
- 기존 폴링 메커니즘 활용
- 코드 변경 최소화

### 수정 내용

#### 1. `HandleEvent` 메서드 수정

**파일**: `ChronoView/Core/FileWatching/FileWatcherService.cs`

**변경 사항**:
- Normal 폴더인 경우, `stitched_original.png` 파일이 있을 때만 `_knownFiles`에 추가
- 이미지 파일이 없으면 이벤트는 발생하지만 `_knownFiles`에 추가하지 않음
- 다음 폴링에서 계속 감지됨

**코드**:
```286:304:ChronoView/Core/FileWatching/FileWatcherService.cs
lock (_lockObject)
{
    if (_knownFiles.Contains(eventToFire.FullPath)) return;
    
    // For Normal folders, only add to _knownFiles if stitched_original.png exists
    // This allows polling to keep checking folders without images until they appear
    var folderName = Path.GetFileName(eventToFire.FullPath);
    if (IsNormalFolderName(folderName) && Directory.Exists(eventToFire.FullPath))
    {
        var stitchedPath = Path.Combine(eventToFire.FullPath, "stitched_original.png");
        if (!File.Exists(stitchedPath))
        {
            _logger.LogDebug("Normal folder without image, skipping _knownFiles addition: {Folder}", folderName);
            // Still fire the event, but don't add to _knownFiles so polling can check again
            FileChanged?.Invoke(this, eventToFire);
            return;
        }
    }
    
    _knownFiles.Add(eventToFire.FullPath);
}
```

#### 2. `PerformSilentScan` 메서드 수정

**파일**: `ChronoView/Core/FileWatching/FileWatcherService.cs`

**변경 사항**:
- 초기 스캔 시에도 Normal 폴더는 이미지 파일이 있을 때만 `_knownFiles`에 추가
- 이미지 파일이 없는 폴더는 스킵하여 폴링에서 계속 확인됨

**코드**:
```100:120:ChronoView/Core/FileWatching/FileWatcherService.cs
// Scan files
var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
foreach (var file in files) _knownFiles.Add(file);

// Scan directories (Normal folders)
// Only add Normal folders to _knownFiles if they have stitched_original.png
// This allows polling to keep checking folders without images
var dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
foreach (var dir in dirs)
{
    var dirName = Path.GetFileName(dir);
    if (IsNormalFolderName(dirName))
    {
        var stitchedPath = Path.Combine(dir, "stitched_original.png");
        if (File.Exists(stitchedPath))
        {
            _knownFiles.Add(dir);
        }
        // Skip adding to _knownFiles if image doesn't exist - polling will check again
    }
    else
    {
        // Non-Normal folders: add as usual
        _knownFiles.Add(dir);
    }
}
```

### 동작 방식

1. **폴더 생성 감지**
   - FileSystemWatcher 또는 폴링이 Normal 폴더 생성 감지
   - `HandleEvent` 호출

2. **이미지 파일 확인**
   - Normal 폴더인 경우 `stitched_original.png` 존재 여부 확인
   - 이미지 파일이 있으면 `_knownFiles`에 추가
   - 이미지 파일이 없으면 `_knownFiles`에 추가하지 않음 (이벤트는 발생)

3. **폴링에서 재확인**
   - 다음 폴링(2초마다)에서 `_knownFiles`에 없는 폴더는 계속 감지됨
   - 이미지 파일이 생성되면 `HandleEvent`가 다시 호출됨
   - 이번에는 이미지 파일이 있으므로 `_knownFiles`에 추가됨

4. **그룹 업데이트**
   - 이미지 파일이 생성되면 `stitched_original.png` 이벤트가 발생
   - `MonitoringOrchestrator`에서 이미지 캡처 및 그룹 업데이트

### 장점

1. **간단한 구현**: 별도의 펜딩 목록 관리 불필요
2. **기존 메커니즘 활용**: 폴링 로직 재사용
3. **자동 재확인**: 폴링이 자동으로 이미지 파일 생성 여부 확인
4. **최소한의 코드 변경**: `_knownFiles` 추가 조건만 수정

### 주의사항

- 폴링 간격(기본 2초) 동안 이미지 파일이 생성되어야 감지됨
- FileSystemWatcher가 `stitched_original.png` 생성 이벤트를 감지하면 즉시 처리됨
- 폴링은 백업 메커니즘으로 동작

