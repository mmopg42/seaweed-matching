# Incremental File Monitoring Implementation Plan

## 목표

1. **즉시 피드백**: 파일 감지 → 그룹 표시 < 200ms (Placeholder 포함)
2. **점진적 로딩**: 이미지를 개별적으로 순차 로딩 (< 2초)
3. **안정성**: 에러 격리, CPU 제어, UI 반응성 유지
4. **확장성**: 파일 수와 무관하게 일정한 성능

## 현재 문제점

### 발견된 이슈

[`MonitoringOrchestrator.ProcessFileEventsAsync`](file:///c:/workspace/seaweed/gui_kiro/ChronoView/Core/FileWatching/MonitoringOrchestrator.cs#L420-L499)에서:

```csharp
// Handle file deletion
if (eventArgs.ChangeType == WatcherChangeTypes.Deleted)
{
    // ... deletion 처리 ...
}
// For now, trigger a re-scan to update groups for other events
// In a more sophisticated implementation, we would update specific groups
// ❌ Created/Changed 이벤트는 아무것도 하지 않음!
```

**결과**: 파일 생성/변경 시 GUI가 업데이트되지 않음

## 구현 전략

### Architecture

```
FileSystemWatcher
    ↓ (< 100ms)
OnFileChanged Event
    ↓
ProcessFileEventsAsync
    ├─ Created → CreateOrUpdateGroupAsync
    ├─ Changed → UpdateGroupThumbnailAsync
    └─ Deleted → RemoveFromGroupAsync
        ↓ (< 50ms)
GUI Update (ObservableCollection)
    ↓ (< 20ms)
DataGrid 반영
```

### Performance Breakdown

| 단계 | 시간 | 방법 |
|------|------|------|
| 파일 감지 | < 100ms | FileSystemWatcher (OS 네이티브) |
| 파일 타입 판단 | < 10ms | DetermineFileType() |
| 그룹 매칭 | < 50ms | 타임스탬프 기반 검색 |
| GUI 추가 | < 20ms | ObservableCollection.Add() |
| **총 렉** | **< 200ms** | **체감상 즉시** |

## 구현 단계

### Phase 1: Core Logic (우선순위: 높음)

#### 1.1 ProcessFileEventsAsync 수정

**파일**: [`MonitoringOrchestrator.cs`](file:///c:/workspace/seaweed/gui_kiro/ChronoView/Core/FileWatching/MonitoringOrchestrator.cs)

**수정 내용**:

```csharp
public async Task<List<FileGroup>> ProcessFileEventsAsync(List<FileSystemEventArgs> events)
{
    var updatedGroups = new List<FileGroup>();

    foreach (var eventArgs in events)
    {
        if (ShouldSkipEvent(eventArgs)) continue;
        
        MarkFileAsProcessed(eventArgs.FullPath);
        var fileType = DetermineFileType(eventArgs.FullPath);
        
        if (fileType == FileType.Unknown) continue;

        _logger.LogInformation("Processing {ChangeType} event for {FileType}: {Path}",
            eventArgs.ChangeType, fileType, eventArgs.FullPath);

        switch (eventArgs.ChangeType)
        {
            case WatcherChangeTypes.Created:
            case WatcherChangeTypes.Changed:
                var group = await CreateOrUpdateGroupAsync(eventArgs.FullPath, fileType);
                if (group != null)
                {
                    updatedGroups.Add(group);
                }
                break;

            case WatcherChangeTypes.Deleted:
                await RemoveFromGroupAsync(eventArgs.FullPath);
                break;
        }
    }

    if (updatedGroups.Count > 0)
    {
        OnFileGroupsUpdated(new FileGroupsUpdatedEventArgs
        {
            UpdatedGroups = updatedGroups,
            Timestamp = DateTime.UtcNow
        });
    }

    return updatedGroups;
}
```

**예상 시간**: 1시간

#### 1.2 CreateOrUpdateGroupAsync 구현

**새 메서드 추가**:

```csharp
private async Task<FileGroup?> CreateOrUpdateGroupAsync(string filePath, FileType fileType)
{
    try
    {
        // 1. 타임스탬프 추출
        var timestamp = ExtractTimestamp(filePath, fileType);
        if (timestamp == null)
        {
            _logger.LogWarning("Could not extract timestamp from {Path}", filePath);
            return null;
        }

        // 2. 기존 그룹 찾기 (타임스탬프 기반)
        FileGroup? existingGroup = null;
        lock (_lockObject)
        {
            existingGroup = _activeGroups.Values
                .FirstOrDefault(g => IsMatchingTimestamp(g, timestamp.Value, fileType));
        }

        // 3-A. 기존 그룹 업데이트
        if (existingGroup != null)
        {
            UpdateGroupWithFile(existingGroup, filePath, fileType);
            OnGroupUpdated(existingGroup);
            return existingGroup;
        }

        // 3-B. 새 그룹 생성
        var newGroup = await CreateNewGroupAsync(filePath, fileType, timestamp.Value);
        if (newGroup != null)
        {
            lock (_lockObject)
            {
                _activeGroups[newGroup.GroupId] = newGroup;
            }
            OnGroupCreated(newGroup);
            return newGroup;
        }

        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating/updating group for {Path}", filePath);
        return null;
    }
}

private DateTime? ExtractTimestamp(string filePath, FileType fileType)
{
    return fileType switch
    {
        FileType.Nir => ExtractTimestampFromNirFile(filePath),
        FileType.Normal => ExtractTimestampFromFolderName(Path.GetDirectoryName(filePath)),
        FileType.Camera => ExtractTimestampFromCameraFile(filePath),
        _ => null
    };
}

private bool IsMatchingTimestamp(FileGroup group, DateTime timestamp, FileType fileType)
{
    var tolerance = fileType == FileType.Nir 
        ? TimeSpan.FromSeconds(_currentConfig?.MatchingSettings.NirTimeWindowSeconds ?? 300)
        : TimeSpan.FromSeconds(_currentConfig?.MatchingSettings.CameraTimeWindowSeconds ?? 60);

    return Math.Abs((group.Timestamp - timestamp).TotalSeconds) < tolerance.TotalSeconds;
}

private void UpdateGroupWithFile(FileGroup group, string filePath, FileType fileType)
{
    switch (fileType)
    {
        case FileType.Nir:
            group.NirFilePath = filePath;
            group.HasNir = true;
            break;
        case FileType.Normal:
            group.MainImagePath = filePath;
            break;
        case FileType.Camera:
            AddCameraFileToGroup(group, filePath);
            break;
    }
}

private async Task<FileGroup?> CreateNewGroupAsync(string filePath, FileType fileType, DateTime timestamp)
{
    // FileGroupMatcher를 사용하여 단일 파일로 그룹 생성
    var unmatchedFiles = new UnmatchedFiles();
    
    switch (fileType)
    {
        case FileType.Nir:
            unmatchedFiles.NirFiles.Add(filePath);
            break;
        case FileType.Normal:
            unmatchedFiles.NormalFolders.Add(Path.GetDirectoryName(filePath) ?? filePath);
            break;
        case FileType.Camera:
            // Camera files are usually attached to existing groups
            return null;
    }

    var groups = await _fileGroupMatcher.MatchFilesAsync(unmatchedFiles);
    return groups.FirstOrDefault();
}
```

**예상 시간**: 2-3시간

#### 1.3 RemoveFromGroupAsync 구현

**새 메서드 추가**:

```csharp
private async Task RemoveFromGroupAsync(string filePath)
{
    lock (_lockObject)
    {
        var affectedGroups = _activeGroups.Values
            .Where(g => ContainsFile(g, filePath))
            .ToList();

        foreach (var group in affectedGroups)
        {
            // 파일 제거
            RemoveFileFromGroup(group, filePath);

            // 그룹이 비었으면 삭제
            if (IsGroupEmpty(group))
            {
                _activeGroups.Remove(group.GroupId);
                OnGroupRemoved(group.GroupId);
            }
            else
            {
                OnGroupUpdated(group);
            }
        }
    }

    await Task.CompletedTask;
}

private void RemoveFileFromGroup(FileGroup group, string filePath)
{
    if (group.NirFilePath == filePath)
    {
        group.NirFilePath = null;
        group.HasNir = false;
    }
    else if (group.MainImagePath == filePath)
    {
        group.MainImagePath = null;
    }
    // Camera files...
}

private bool IsGroupEmpty(FileGroup group)
{
    return string.IsNullOrEmpty(group.NirFilePath) 
        && string.IsNullOrEmpty(group.MainImagePath)
        && (group.CameraFiles?.Count ?? 0) == 0;
}
```

**예상 시간**: 1시간

### Phase 2: Progressive Image Loading (우선순위: 높음)

> **⚠️ ARCHITECTURE DECISION (2025-12-12)**  
> **기존 계획**: MonitoringOrchestrator에서 이미지 로딩 (폐기)  
> **새로운 계획**: MainWindowViewModel에서 이미지 로딩 (채택)  
> **이유**: Clean Architecture 준수, WPF Dispatcher 의존성 제거  
> **참고**: [`docs/trouble/phase2_architecture_issue.md`](../../trouble/phase2_architecture_issue.md)

#### 2.1 Responsibility Distribution

**MonitoringOrchestrator (Core Layer)**:
- ✅ 파일 감지 및 그룹 관리
- ✅ Placeholder 초기화 (PlaceholderImageHelper 사용)
- ✅ 이벤트 발생 (GroupCreated, GroupUpdated)
- ❌ BitmapSource 로딩 (UI 관심사)
- ❌ Dispatcher 사용 (UI 관심사)

**MainWindowViewModel (UI Layer)**:
- ✅ GroupCreated 이벤트 구독
- ✅ 이미지 점진적 로딩 (Semaphore throttling)
- ✅ byte[] → BitmapSource 변환
- ✅ PropertyChanged 트리거로 UI 업데이트
- ✅ 에러 처리 (Error Icon 표시)

#### 2.2 MonitoringOrchestrator Implementation

**파일**: `MonitoringOrchestrator.cs`

**수정 내용**:

```csharp
private async Task<FileGroup?> CreateOrUpdateGroupAsync(string filePath, FileType fileType)
{
    try
    {
        // 1. 타임스탬프 추출
        var timestamp = ExtractTimestamp(filePath, fileType);
        if (timestamp == null) return null;

        // 2. 기존 그룹 찾기
        FileGroup? existingGroup = null;
        lock (_lockObject)
        {
            existingGroup = _activeGroups.Values
                .FirstOrDefault(g => IsMatchingTimestamp(g, timestamp.Value, fileType));
        }

        // 3-A. 기존 그룹 업데이트
        if (existingGroup != null)
        {
            UpdateGroupWithFile(existingGroup, filePath, fileType);
            OnGroupUpdated(existingGroup);
            return existingGroup;
        }

        // 3-B. 새 그룹 생성
        var newGroup = await CreateNewGroupAsync(filePath, fileType, timestamp.Value);
        if (newGroup != null)
        {
            // Placeholder 초기화
            newGroup.MainImageThumbnail = PlaceholderImageHelper.PlaceholderImage;
            newGroup.NirGraphThumbnail = PlaceholderImageHelper.PlaceholderImage;
            
            // 그룹 먼저 GUI에 추가 (< 200ms)
            lock (_lockObject)
            {
                _activeGroups[newGroup.GroupId] = newGroup;
            }
            
            // 이벤트 발생 → UI Layer가 처리
            OnGroupCreated(newGroup); // ← 즉시 표시! (Placeholder 상태)
            
            // ✅ 이미지 로딩은 MainWindowViewModel이 담당
            
            return newGroup;
        }

        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating/updating group for {Path}", filePath);
        return null;
    }
}
```

**예상 시간**: 1시간 (제거 작업)

#### 2.3 MainWindowViewModel Implementation

**파일**: `MainWindowViewModel.cs`

**전략**: 그룹을 **즉시** GUI에 추가 (Placeholder 포함) → 이미지는 **개별적**으로 백그라운드 로딩

**구현**:

```csharp
// Constructor
public MainWindowViewModel(
    IMonitoringOrchestrator orchestrator,
    IImageProcessor imageProcessor,
    NirGraphGenerator nirGraphGenerator,
    ...)
{
    _orchestrator = orchestrator;
    _imageProcessor = imageProcessor;
    _nirGraphGenerator = nirGraphGenerator;
    
    // CPU 제어용 Semaphore (UI Layer에서 관리)
    _imageLoadingSemaphore = new SemaphoreSlim(
        Environment.ProcessorCount, 
        Environment.ProcessorCount);
    
    // 이벤트 구독
    _orchestrator.GroupCreated += OnGroupCreated;
    _orchestrator.GroupUpdated += OnGroupUpdated;
}

// Event Handler
private void OnGroupCreated(object sender, FileGroup group)
{
    // 1. ObservableCollection에 즉시 추가 (Placeholder 상태)
    FileGroups.Add(group);
    
    _logger.LogInformation("Group {GroupId} added to UI with placeholders", group.GroupId);
    
    // 2. 백그라운드에서 이미지 점진적 로딩 (Fire-and-forget)
    _ = LoadGroupImagesProgressivelyAsync(group);
}

// Progressive Loading (MainWindowViewModel에서 실행)
private async Task LoadGroupImagesProgressivelyAsync(FileGroup group)
{
    await _imageLoadingSemaphore.WaitAsync();
    
    try
    {
        // Main 이미지 (개별 로딩, 에러 격리)
        if (!string.IsNullOrEmpty(group.MainImagePath) && File.Exists(group.MainImagePath))
        {
            try
            {
                // Heavy I/O in background thread
                var thumbnailBytes = await Task.Run(() =>
                    _imageProcessor.GenerateThumbnailAsync(group.MainImagePath, 120, 90));
                
                // Convert to BitmapSource (UI thread context)
                var thumbnail = ConvertBytesToBitmapSource(thumbnailBytes);
                thumbnail.Freeze(); // Thread-safe
                
                // Update property (PropertyChanged → UI update)
                group.MainImageThumbnail = thumbnail;
                
                _logger.LogInformation("Main image loaded for {GroupId}", group.GroupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load main image for {GroupId}", group.GroupId);
                group.MainImageThumbnail = PlaceholderImageHelper.ErrorIcon; // 에러 아이콘
            }
        }

        // NIR 이미지 (개별 로딩, 에러 격리)
        if (!string.IsNullOrEmpty(group.NirFilePath) && File.Exists(group.NirFilePath))
        {
            try
            {
                var spectrum = await Task.Run(() =>
                    NirFileReader.LoadSpectrum(group.NirFilePath));
                
                var graphImage = NirGraphGenerator.GenerateGraph(spectrum, 250, 100);
                graphImage?.Freeze();
                
                group.NirGraphThumbnail = graphImage ?? PlaceholderImageHelper.ErrorIcon;
                
                _logger.LogInformation("NIR graph loaded for {GroupId}", group.GroupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load NIR graph for {GroupId}", group.GroupId);
                group.NirGraphThumbnail = PlaceholderImageHelper.ErrorIcon;
            }
        }

        // Camera 이미지들 (개별 로딩)
        foreach (var cameraKvp in group.CameraFiles)
        {
            var cameraKey = cameraKvp.Key;
            var cameraPath = cameraKvp.Value;
            
            if (!string.IsNullOrEmpty(cameraPath) && File.Exists(cameraPath))
            {
                try
                {
                    var thumbnailBytes = await Task.Run(() =>
                        _imageProcessor.GenerateThumbnailAsync(cameraPath, 120, 90));
                    
                    var thumbnail = ConvertBytesToBitmapSource(thumbnailBytes);
                    thumbnail.Freeze();
                    
                    group.CameraThumbnails[cameraKey] = thumbnail;
                    
                    _logger.LogInformation("Camera {Key} loaded for {GroupId}", cameraKey, group.GroupId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load camera {Key} for {GroupId}", cameraKey, group.GroupId);
                    group.CameraThumbnails[cameraKey] = PlaceholderImageHelper.ErrorIcon;
                }
            }
        }
        
        _logger.LogInformation("Progressive loading completed for group {GroupId}", group.GroupId);
    }
    finally
    {
        _imageLoadingSemaphore.Release();
    }
}

private static BitmapSource ConvertBytesToBitmapSource(byte[] imageBytes)
{
    using (var stream = new MemoryStream(imageBytes))
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze(); // CRITICAL: Thread-safe
        return bitmap;
    }
}
```

**예상 시간**: 3-4시간

#### 2.4 Key Benefits

**Architecture**:
- ✅ Core Layer는 UI-agnostic (WPF 참조 없음)
- ✅ MVVM 패턴 준수 (데이터 로딩은 ViewModel)
- ✅ 테스트 용이 (MonitoringOrchestrator 단위 테스트 가능)
- ✅ 확장성 (다른 UI 프레임워크 지원 가능)

**Performance**:
- ✅ Placeholder로 즉시 피드백 (< 200ms)
- ✅ Semaphore로 CPU throttling
- ✅ 에러 격리 (한 이미지 실패해도 다른 것 로딩)
- ✅ Freeze()로 스레드 안전성 확보

**예상 시간**: 3-4시간 (이미지 로딩 구현)

### Phase 3: Optimization (우선순위: 중간)

#### 3.1 Priority-Based Loading

**보이는 이미지 먼저 로딩**:

```csharp
private readonly PriorityQueue<FileGroupViewModel, int> _loadQueue = new();

public void OnGroupVisible(FileGroupViewModel group)
{
    _loadQueue.Enqueue(group, priority: 1); // High priority
}

public void OnGroupHidden(FileGroupViewModel group)
{
    _loadQueue.Enqueue(group, priority: 10); // Low priority
}
```

**예상 시간**: 2시간

#### 3.2 Image Caching

**메모리 캐시 추가**:

```csharp
private static readonly LRUCache<string, BitmapSource> _imageCache = 
    new LRUCache<string, BitmapSource>(maxSize: 100);

public async Task<BitmapSource> GenerateThumbnailAsync(string path, int w, int h)
{
    var cacheKey = $"{path}_{w}x{h}";
    
    if (_imageCache.TryGet(cacheKey, out var cached))
        return cached;
    
    var thumbnail = await InternalGenerateThumbnailAsync(path, w, h);
    _imageCache.Add(cacheKey, thumbnail);
    
    return thumbnail;
}
```

**예상 시간**: 1시간

### Phase 4: Testing & Validation (우선순위: 높음)

#### 4.1 Performance Testing

**측정 항목**:
- [ ] 파일 생성 → GUI 표시: < 200ms
- [ ] 이미지 로딩: < 2초
- [ ] 100개 파일 동시 생성 시 성능
- [ ] 메모리 사용량 (이미지 캐시)

#### 4.2 Integration Testing

**시나리오**:
1. NIR 파일 생성 → 새 그룹 생성 확인
2. Normal 폴더 생성 → 그룹 업데이트 확인
3. Camera 파일 생성 → 그룹에 추가 확인
4. 파일 삭제 → 그룹 업데이트/삭제 확인

**예상 시간**: 2시간

## 구현 순서

### Day 1 (4-5시간)
1. ✅ ProcessFileEventsAsync 수정 (1시간)
2. ✅ CreateOrUpdateGroupAsync 구현 (2-3시간)
3. ✅ 빌드 & 기본 테스트 (1시간)

### Day 2 (5-6시간)
4. ✅ RemoveFromGroupAsync 구현 (1시간)
5. ✅ LoadGroupImagesProgressivelyAsync 구현 (3-4시간)
6. ✅ Placeholder & Error Handling (1-2시간)
7. ✅ 통합 테스트 (1시간)

### Day 3 (Optional, 3-4시간)
8. ⭐ Priority-Based Loading (2시간)
9. ⭐ Image Caching (1시간)
10. ⭐ 성능 최적화 (1시간)

## 예상 결과

### 성능 목표

| 항목 | 목표 | 예상 달성률 |
|------|------|----------|
| 그룹 즉시 표시 (Placeholder) | < 200ms | ✅ 100% |
| 첫 이미지 표시 | < 500ms | ✅ 95% |
| 모든 이미지 로딩 | < 2초 | ✅ 90% |
| UI 반응성 (CPU 제어) | 부드러움 | ✅ 100% |
| 에러 격리 | 안정적 | ✅ 100% |
| 메모리 효율 | 안정적 | ✅ 95% |

### Python vs C# 비교

| 항목 | Python GUI | C# GUI |
|------|-----------|--------|
| 그룹 표시 | 5초 | < 0.2초 |
| 첫 이미지 | 5초+ | < 0.5초 |
| 모든 이미지 | 5초+ | < 2초 |
| UI 렉 | 심함 | 없음 (Throttling) |
| 에러 처리 | 전체 실패 | 개별 격리 |
| **사용자 경험** | **나쁨** | **우수** |

## 리스크 & 대응

### 리스크

1. **타임스탬프 매칭 실패**
   - 대응: FileGroupMatcher의 기존 로직 재사용
   - 백업: 매칭 실패 시 로그 남기고 Refresh 트리거

2. **이미지 로딩 메모리 부족**
   - 대응: LRU 캐시로 메모리 제한
   - 백업: 오래된 이미지 자동 해제

3. **동시성 이슈**
   - 대응: lock (_lockObject) 사용
   - 백업: ConcurrentDictionary로 변경

## 성공 기준

### Must Have (필수)
- ✅ 그룹이 즉시 표시됨 (< 200ms, Placeholder 포함)
- ✅ 이미지가 점진적으로 나타남 (< 2초)
- ✅ 에러 시에도 다른 이미지는 정상 표시 (에러 격리)
- ✅ 대량 파일 시에도 UI 반응성 유지 (CPU Throttling)

### Should Have (권장)
- ⭐ 이미지 로딩 캐싱 (같은 파일 재사용)
- ⭐ 메모리 효율적인 LRU 캐시
- ⭐ 로딩 진행 상태 로그

### Nice to Have (선택)
- 🎯 Progressive loading (저해상도 → 고해상도)
- 🎯 로딩 진행률 표시

## Rollback Plan

구현 중 문제 발생 시:

1. **Phase 1 실패**: Debouncing 방식으로 대체
2. **Phase 2 실패**: 이미지 없이 그룹만 표시
3. **전체 실패**: 기존 Refresh 방식 유지 + 로그 개선
