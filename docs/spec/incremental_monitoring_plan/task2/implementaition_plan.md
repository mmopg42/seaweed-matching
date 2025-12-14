Phase 2: Progressive Image Loading - Implementation Plan
Date: 2025-12-12
Status: Planning
Dependencies: Phase 1 Completed ✅

Overview
Phase 2 implements progressive image loading with placeholders, error isolation, and CPU throttling to provide immediate feedback to users while loading images in the background.

Key Requirements
✅ 즉시 피드백: 그룹 표시 < 200ms (Placeholder 포함)
✅ 점진적 로딩: 모든 이미지 < 2초
✅ 에러 격리: 한 이미지 실패해도 다른 것은 정상 표시
✅ CPU 제어: Semaphore로 동시 로딩 제한

Architecture Decision
Option 1: MonitoringOrchestrator에서 이미지 로딩
Pros:

Single responsibility for file group lifecycle
Centralized image loading logic
Direct control over loading timing
Cons:

MonitoringOrchestrator에 UI 관련 의존성 추가 (IImageProcessor, INirGraphGenerator)
Mixing file watching with image processing
Option 2: MainWindowViewModel에서 이미지 로딩
Pros:

UI layer에서 UI 관련 작업 처리
MonitoringOrchestrator는 파일 감지/그룹 관리에만 집중
ViewModel이 이미 ImageProcessor 접근 가능
Cons:

이벤트 핸들링 추가 필요
추가 레이어 간 통신
✅ Selected: Option 1 - MonitoringOrchestrator에서 이미지 로딩
Justification:

Tasks.md와 incremental_monitoring_plan.md가 MonitoringOrchestrator에서 구현하도록 명시
파일 감지 → 그룹 생성 → 이미지 로딩의 자연스러운 흐름
Phase 1에서 이미 CreateOrUpdateGroupAsync에 주석으로 LoadGroupImagesProgressivelyAsync 호출 계획됨
FileGroup Thumbnail Storage Decision
Problem
FileGroup
 is a model (Models folder) - currently no thumbnail properties
FileGroupViewModel
 has thumbnail properties (_mainImageThumbnail, etc.)
MonitoringOrchestrator works with 
FileGroup
, not 
FileGroupViewModel
Option A: Add Thumbnail Properties to FileGroup Model
public class FileGroup
{
    [JsonIgnore]
    public BitmapSource? MainImageThumbnail { get; set; }
    [JsonIgnore]
    public BitmapSource? NirGraphThumbnail { get; set; }
    [JsonIgnore]
    public Dictionary<string, BitmapSource?> CameraThumbnails { get; set; } = new();
}
Pros: Direct access, simpler
Cons: UI concern in model

Option B: MainWindowViewModel Handles Loading
private void OnGroupCreated(object sender, FileGroup group)
{
    _ = LoadImagesForGroupAsync(group);
}
Pros: Separation of concerns
Cons: Event wiring needed

✅ Selected: Option A
Justification:

Tasks.md lists FileGroup.cs as target file
Simpler, more direct
[JsonIgnore] keeps serialization clean
Performance critical path
Implementation Steps
Task 2.1: LoadGroupImagesProgressivelyAsync (3-4 hours)
1. Update FileGroup Model
Add thumbnail properties with [JsonIgnore]

2. Update MonitoringOrchestrator Constructor
public MonitoringOrchestrator(
    IFileGroupMatcher fileGroupMatcher,
    IFileWatcher fileWatcher,
    IImageProcessor imageProcessor,
    INirGraphGenerator nirGraphGenerator,
    ILogger<MonitoringOrchestrator> logger)
3. Add Semaphore Field
private static readonly SemaphoreSlim _imageLoadingSemaphore = 
    new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
4. Implement LoadGroupImagesProgressivelyAsync
Acquire semaphore
Load Main image (try-catch)
Load NIR graph (try-catch)
Load Camera images 1-6 (try-catch each)
Call OnGroupUpdated after each image
Release semaphore
5. Update CreateOrUpdateGroupAsync
OnGroupCreated(newGroup);
_ = LoadGroupImagesProgressivelyAsync(newGroup); // Fire-and-forget
Task 2.2: Placeholder & Error Handling (1-2 hours)
6. Create PlaceholderImageHelper
New file: Helpers/PlaceholderImageHelper.cs

CreatePlaceholder() - light gray
CreateErrorIcon() - red with white X
7. Initialize Placeholders
Set FileGroup thumbnails to placeholders in constructor

8. Use Error Icons
Update LoadGroupImagesProgressivelyAsync error handling

Verification
 Group appears < 200ms with placeholders
 Images load progressively
 All images < 2 seconds
 Failed image shows error icon
 Other images still load
 CPU < 80% (100 files)
Next Steps
Proceed with implementation based on this plan.

