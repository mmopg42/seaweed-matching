# Phase 2: Progressive Image Loading - Detailed Tasks (REVISED)

**작성일**: 2025-12-12  
**상태**: ✅ Architecture Finalized - Ready to Implement  
**방식**: UI Layer Approach - MainWindowViewModel handles image loading  
**변경 이유**: Clean Architecture 준수, WPF Dispatcher 의존성 제거  

---

## ⚠️ ARCHITECTURE DECISION

**기존 계획 (폐기)**: MonitoringOrchestrator에서 이미지 로딩  
**새로운 계획 (채택)**: MainWindowViewModel에서 이미지 로딩  

**이유**:
- ✅ Clean Architecture 준수 (Core → UI 의존성 제거)
- ✅ MVVM 패턴과 자연스럽게 일치
- ✅ Dispatcher 주입 불필요 (ViewModel은 이미 UI 컨텍스트)
- ✅ 테스트 용이성 (MonitoringOrchestrator 순수 비즈니스 로직)
- ✅ 확장성 (다른 UI 프레임워크 지원 가능)

**참고 문서**: [`docs/trouble/phase2_architecture_issue.md`](../../trouble/phase2_architecture_issue.md)

---

## 전체 진행률

- **Task 2.1**: [ ] 0/3 - Core Layer (MonitoringOrchestrator) 수정
- **Task 2.2**: [ ] 0/4 - UI Layer (MainWindowViewModel) 구현
- **Task 2.3**: [ ] 0/2 - Integration & Testing

**전체**: [ ] 0/9 완료 (0%)

---

## Task 2.1: Core Layer - MonitoringOrchestrator 수정 (1-2시간)

### Task 2.1.1: Placeholder 초기화만 유지 ✅ KEEP

**목표**: CreateNewGroupAsync에서 Placeholder 초기화 (이미 완료)

**파일**: [`MonitoringOrchestrator.cs`](../../../../ChronoView/Core/FileWatching/MonitoringOrchestrator.cs)

**상태**: ✅ **COMPLETED** (이미 구현됨 - 유지)

**작업 내용**:

1. ✅ PlaceholderImageHelper 사용
2. ✅ FileGroup 생성 시 Placeholder 할당
   ```csharp
   newGroup.MainImageThumbnail = PlaceholderImageHelper.PlaceholderImage;
   newGroup.NirGraphThumbnail = PlaceholderImageHelper.PlaceholderImage;
   // Camera placeholders도 동일
   ```

**검증**:
- [x] PlaceholderImageHelper.PlaceholderImage 접근 가능
- [x] 빌드 성공

**예상 시간**: 0분 (이미 완료)

---

### Task 2.1.2: UI 관련 코드 제거 🗑️ REMOVE

**목표**: MonitoringOrchestrator에서 Dispatcher/BitmapSource 관련 코드 제거

**파일**: [`MonitoringOrchestrator.cs`](../../../../ChronoView/Core/FileWatching/MonitoringOrchestrator.cs)

**상태**: [ ] Not Started

**작업 내용**:

1. [ ] **생성자에서 제거**
   ```csharp
   // BEFORE (제거할 것)
   public MonitoringOrchestrator(
       ...
       IImageProcessor imageProcessor,      // ❌ 제거
       NirGraphGenerator nirGraphGenerator,  // ❌ 제거
       ...)
   
   // AFTER
   public MonitoringOrchestrator(
       IFileGroupMatcher fileGroupMatcher,
       IFileWatcher fileWatcher,
       ILogger<MonitoringOrchestrator> logger)
   ```

2. [ ] **필드 제거**
   ```csharp
   // ❌ 제거할 필드들
   private readonly IImageProcessor _imageProcessor;
   private readonly NirGraphGenerator _nirGraphGenerator;
   private static readonly SemaphoreSlim _imageLoadingSemaphore = ...;
   ```

3. [ ] **LoadGroupImagesProgressivelyAsync 메서드 전체 삭제** (147줄)
   - Main 이미지 로딩 로직
   - NIR 그래프 로딩 로직
   - Camera 이미지 로딩 로직
   - ConvertBytesToBitmapSource 헬퍼 메서드

4. [ ] **using 제거**
   ```csharp
   // ❌ 제거
   using ChronoView.Core.ImageProcessing;
   using ChronoView.Core.Nir;
   using System.Windows;
   using System.Windows.Media.Imaging;
   ```

5. [ ] **CreateOrUpdateGroupAsync에서 호출 제거**
   ```csharp
   // BEFORE
   OnGroupCreated(newGroup);
   _ = LoadGroupImagesProgressivelyAsync(newGroup); // ❌ 제거
   
   // AFTER
   OnGroupCreated(newGroup); // ✅ 이벤트 발생만
   ```

**검증**:
- [ ] 빌드 성공 (WPF 참조 에러 해결)
- [ ] MonitoringOrchestrator에 System.Windows 의존성 없음
- [ ] OnGroupCreated 이벤트는 여전히 발생

**예상 시간**: 30분

**의존성**: 없음

---

### Task 2.1.3: 이벤트 확인 및 문서화 📝 VERIFY

**목표**: GroupCreated/GroupUpdated 이벤트가 올바르게 동작하는지 확인

**파일**: [`MonitoringOrchestrator.cs`](../../../../ChronoView/Core/FileWatching/MonitoringOrchestrator.cs)

**상태**: [ ] Not Started

**작업 내용**:

1. [ ] **이벤트 정의 확인**
   ```csharp
   public event EventHandler<FileGroup>? GroupCreated;
   public event EventHandler<FileGroup>? GroupUpdated;
   public event EventHandler<string>? GroupRemoved;
   ```

2. [ ] **OnGroupCreated 호출 확인**
   ```csharp
   private void OnGroupCreated(FileGroup group)
   {
       GroupCreated?.Invoke(this, group);
   }
   ```

3. [ ] **CreateOrUpdateGroupAsync에서 적절히 호출되는지 확인**

**검증**:
- [ ] 이벤트 정의 존재
- [ ] 새 그룹 생성 시 OnGroupCreated 호출
- [ ] 기존 그룹 업데이트 시 OnGroupUpdated 호출

**예상 시간**: 30분

**의존성**: Task 2.1.2 완료

---

## Task 2.2: UI Layer - MainWindowViewModel 구현 (3-4시간)

### Task 2.2.1: MainWindowViewModel에 서비스 주입 ⚠️ NEW

**목표**: IImageProcessor, NirGraphGenerator, Semaphore 추가

**파일**: [`MainWindowViewModel.cs`](../../../../ChronoView/UI/ViewModels/MainWindowViewModel.cs)

**상태**: [ ] Not Started

**작업 내용**:

1. [ ] **필드 추가**
   ```csharp
   private readonly IImageProcessor _imageProcessor;
   private readonly NirGraphGenerator _nirGraphGenerator;
   private readonly SemaphoreSlim _imageLoadingSemaphore;
   ```

2. [ ] **생성자 업데이트**
   ```csharp
   public MainWindowViewModel(
       IMonitoringOrchestrator orchestrator,
       IImageProcessor imageProcessor,
       NirGraphGenerator nirGraphGenerator,
       IAbnormalDetector abnormalDetector,
       ApplicationConfiguration configuration,
       ILogger<MainWindowViewModel> logger)
   {
       _orchestrator = orchestrator;
       _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
       _nirGraphGenerator = nirGraphGenerator ?? throw new ArgumentNullException(nameof(nirGraphGenerator));
       _abnormalDetector = abnormalDetector;
       _configuration = configuration;
       _logger = logger;
       
       // CPU throttling: Limit concurrent image loading
       _imageLoadingSemaphore = new SemaphoreSlim(
           Environment.ProcessorCount, 
           Environment.ProcessorCount);
       
       // Subscribe to events
       _orchestrator.GroupCreated += OnGroupCreated;
       _orchestrator.GroupUpdated += OnGroupUpdated;
   }
   ```

3. [ ] **Using 추가**
   ```csharp
   using ChronoView.Core.ImageProcessing;
   using ChronoView.Core.Nir;
   using ChronoView.Helpers;
   using System.Windows.Media.Imaging;
   using System.IO;
   ```

**검증**:
- [ ] 빌드 성공
- [ ] DI 컨테이너 등록 업데이트 필요 (App.xaml.cs)

**예상 시간**: 1시간

**의존성**: Task 2.1.2 완료

---

### Task 2.2.2: 이벤트 핸들러 구현 ⚠️ NEW

**목표**: GroupCreated 이벤트 수신 및 progressive loading 시작

**파일**: [`MainWindowViewModel.cs`](../../../../ChronoView/UI/ViewModels/MainWindowViewModel.cs)

**상태**: [ ] Not Started

**작업 내용**:

1. [ ] **OnGroupCreated 핸들러**
   ```csharp
   private void OnGroupCreated(object sender, FileGroup group)
   {
       // UI thread check (ViewModel은 이미 UI 컨텍스트)
       Application.Current.Dispatcher.VerifyAccess(); // Debug assertion
       
       // 1. Add to ObservableCollection immediately
       //    (group already has placeholders from MonitoringOrchestrator)
       FileGroups.Add(group);
       
       _logger.LogInformation("Group {GroupId} added to UI with placeholders", group.GroupId);
       
       // 2. Start progressive image loading (Fire-and-forget)
       _ = LoadGroupImagesProgressivelyAsync(group);
   }
   ```

2. [ ] **OnGroupUpdated 핸들러**
   ```csharp
   private void OnGroupUpdated(object sender, FileGroup group)
   {
       // FileGroup은 이미 ObservableCollection에 있으므로
       // PropertyChanged 이벤트만 발생시키면 UI 자동 업데이트
       _logger.LogInformation("Group {GroupId} updated", group.GroupId);
   }
   ```

**검증**:
- [ ] 새 그룹 생성 시 FileGroups에 추가됨
- [ ] UI에 즉시 표시됨 (placeholder 포함)

**예상 시간**: 30분

**의존성**: Task 2.2.1 완료

---

### Task 2.2.3: LoadGroupImagesProgressivelyAsync 구현 ⚠️ NEW

**목표**: 개별 이미지 로딩 로직 (MonitoringOrchestrator에서 이동)

**파일**: [`MainWindowViewModel.cs`](../../../../ChronoView/UI/ViewModels/MainWindowViewModel.cs)

**상태**: [ ] Not Started

**작업 내용**:

1. [ ] **메서드 시그니처**
   ```csharp
   /// <summary>
   /// Load images progressively for a file group with error isolation and CPU throttling.
   /// Runs on background threads but updates UI properties (PropertyChanged triggers UI update).
   /// </summary>
   private async Task LoadGroupImagesProgressivelyAsync(FileGroup group)
   ```

2. [ ] **Semaphore 획득 및 해제**
   ```csharp
   await _imageLoadingSemaphore.WaitAsync();
   try
   {
       // Image loading logic
   }
   finally
   {
       _imageLoadingSemaphore.Release();
   }
   ```

3. [ ] **Main 이미지 로딩**
   ```csharp
   // Main Image
   if (!string.IsNullOrEmpty(group.MainImagePath) && File.Exists(group.MainImagePath))
   {
       try
       {
           // Heavy I/O in background
           var thumbnailBytes = await Task.Run(() =>
               _imageProcessor.GenerateThumbnailAsync(group.MainImagePath, 120, 90));
           
           // Convert to BitmapSource (can be done on UI or background thread)
           var thumbnail = ConvertBytesToBitmapSource(thumbnailBytes);
           thumbnail.Freeze(); // CRITICAL: Make thread-safe and immutable
           
           // Update property (triggers PropertyChanged → UI update)
           group.MainImageThumbnail = thumbnail;
           
           _logger.LogInformation("Main image loaded for {GroupId}", group.GroupId);
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Failed to load main image for {GroupId}: {Path}",
               group.GroupId, group.MainImagePath);
           
           group.MainImageThumbnail = PlaceholderImageHelper.ErrorIcon;
       }
   }
   ```

4. [ ] **NIR Graph 로딩**
   ```csharp
   // NIR Graph
   if (!string.IsNullOrEmpty(group.NirFilePath) && File.Exists(group.NirFilePath))
   {
       try
       {
           var spectrum = await Task.Run(() =>
               NirFileReader.LoadSpectrum(group.NirFilePath));
           
           if (spectrum != null && spectrum.IsValid())
           {
               var graphImage = NirGraphGenerator.GenerateGraph(spectrum, 250, 100);
               graphImage?.Freeze();
               
               group.NirGraphThumbnail = graphImage ?? PlaceholderImageHelper.ErrorIcon;
               _logger.LogInformation("NIR graph loaded for {GroupId}", group.GroupId);
           }
           else
           {
               throw new InvalidDataException("Invalid NIR spectrum");
           }
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Failed to load NIR graph for {GroupId}: {Path}",
               group.GroupId, group.NirFilePath);
           
           group.NirGraphThumbnail = PlaceholderImageHelper.ErrorIcon;
       }
   }
   ```

5. [ ] **Camera 이미지 로딩**
   ```csharp
   // Camera Images (cam1-cam6)
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
               
               _logger.LogInformation("Camera {CameraKey} loaded for {GroupId}",
                   cameraKey, group.GroupId);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Failed to load camera {CameraKey} for {GroupId}: {Path}",
                   cameraKey, group.GroupId, cameraPath);
               
               group.CameraThumbnails[cameraKey] = PlaceholderImageHelper.ErrorIcon;
           }
       }
   }
   ```

6. [ ] **완료 로그**
   ```csharp
   _logger.LogInformation("Progressive loading completed for group {GroupId}", group.GroupId);
   ```

**검증**:
- [ ] 각 이미지가 독립적으로 로딩됨
- [ ] 에러 발생 시 다른 이미지는 계속 로딩
- [ ] PropertyChanged로 UI 자동 업데이트
- [ ] Semaphore로 동시 로딩 제한

**예상 시간**: 1.5-2시간

**의존성**: Task 2.2.2 완료

---

### Task 2.2.4: ConvertBytesToBitmapSource 헬퍼 추가 ⚠️ NEW

**목표**: byte[] → BitmapSource 변환 (MonitoringOrchestrator에서 이동)

**파일**: [`MainWindowViewModel.cs`](../../../../ChronoView/UI/ViewModels/MainWindowViewModel.cs)

**상태**: [ ] Not Started

**작업 내용**:

1. [ ] **헬퍼 메서드 구현**
   ```csharp
   /// <summary>
   /// Converts a byte array (JPEG) to a BitmapSource for UI display.
   /// CRITICAL: Must call Freeze() on the result for thread-safety.
   /// </summary>
   private static BitmapSource ConvertBytesToBitmapSource(byte[] imageBytes)
   {
       if (imageBytes == null || imageBytes.Length == 0)
           throw new ArgumentException("Image bytes cannot be null or empty", nameof(imageBytes));
       
       using (var stream = new MemoryStream(imageBytes))
       {
           var bitmap = new BitmapImage();
           bitmap.BeginInit();
           bitmap.CacheOption = BitmapCacheOption.OnLoad;
           bitmap.StreamSource = stream;
           bitmap.EndInit();
           bitmap.Freeze(); // Make thread-safe and immutable
           return bitmap;
       }
   }
   ```

**검증**:
- [ ] BitmapSource 생성 성공
- [ ] Freeze() 호출로 스레드 안전성 확보

**예상 시간**: 30분

**의존성**: Task 2.2.3과 동시 진행 가능

---

## Task 2.3: Integration & Testing (1-2시간)

### Task 2.3.1: DI 컨테이너 업데이트 🔧 UPDATE

**목표**: App.xaml.cs에서 MainWindowViewModel DI 등록 업데이트

**파일**: [`App.xaml.cs`](../../../../ChronoView/App.xaml.cs)

**상태**: [ ] Not Started

**작업 내용**:

1. [ ] **MainWindow ViewModel 등록 수정**
   ```csharp
   // App.xaml.cs
   services.AddSingleton<MainWindowViewModel>(sp => new MainWindowViewModel(
       sp.GetRequiredService<IMonitoringOrchestrator>(),
       sp.GetRequiredService<IImageProcessor>(),      // 추가
       sp.GetRequiredService<NirGraphGenerator>(),    // 추가
       sp.GetRequiredService<IAbnormalDetector>(),
       sp.GetRequiredService<ApplicationConfiguration>(),
       sp.GetRequiredService<ILogger<MainWindowViewModel>>()
   ));
   ```

2. [ ] **MonitoringOrchestrator 등록 복원**
   ```csharp
   // BEFORE (제거 전)
   services.AddSingleton<IMonitoringOrchestrator>(sp => new MonitoringOrchestrator(
       sp.GetRequiredService<IFileGroupMatcher>(),
       sp.GetRequiredService<IFileWatcher>(),
       sp.GetRequiredService<IImageProcessor>(),      // ❌ 제거
       sp.GetRequiredService<NirGraphGenerator>(),    // ❌ 제거
       sp.GetRequiredService<ILogger<MonitoringOrchestrator>>()
   ));
   
   // AFTER
   services.AddSingleton<IMonitoringOrchestrator>(sp => new MonitoringOrchestrator(
       sp.GetRequiredService<IFileGroupMatcher>(),
       sp.GetRequiredService<IFileWatcher>(),
       sp.GetRequiredService<ILogger<MonitoringOrchestrator>>()
   ));
   ```

**검증**:
- [ ] 앱 시작 성공
- [ ] DI 에러 없음

**예상 시간**: 30분

**의존성**: Task 2.1.2, 2.2.1 완료

---

### Task 2.3.2: 통합 테스트 🧪 TEST

**목표**: 전체 시스템 통합 테스트

**상태**: [ ] Not Started

**테스트 시나리오**:

1. [ ] **단일 파일 생성**
   - NIR 파일 생성 → 새 그룹 생성
   - Placeholder 즉시 표시 (< 200ms)
   - 실제 이미지 점진적 로딩 확인

2. [ ] **Normal 폴더 생성**
   - 기존 그룹 업데이트
   - Main 이미지 로딩 확인

3. [ ] **Camera 파일 생성**
   - 기존 그룹에 Camera 이미지 추가
   - Camera thumbnail 표시 확인

4. [ ] **에러 시나리오**
   - 손상된 이미지 → Error Icon 표시
   - 다른 이미지는 정상 로딩 확인

5. [ ] **대량 파일 (100개)**
   - CPU 사용률 < 80%
   - 모든 그룹 표시
   - 이미지 점진적 로딩

**검증**:
- [ ] 그룹 표시 < 200ms
- [ ] 첫 이미지 < 500ms
- [ ] 모든 이미지 < 2초
- [ ] 에러 격리 동작
- [ ] UI 반응성 유지

**예상 시간**: 1시간

**의존성**: 모든 Task 완료

---

## 빌드 & 테스트

### 중간 빌드 (각 Task 후)
- [ ] Task 2.1.2 후 빌드 (MonitoringOrchestrator)
- [ ] Task 2.2.1 후 빌드 (MainWindowViewModel)
- [ ] Task 2.2.3 후 빌드 (LoadGroupImagesProgressivelyAsync)
- [ ] Task 2.3.1 후 빌드 (DI 설정)

### 최종 통합 테스트
- [ ] 단일 파일 생성 → 그룹 + Placeholder 표시 (< 200ms)
- [ ] 이미지 점진적 로딩 (Main → NIR → Cameras)
- [ ] 모든 이미지 로딩 완료 (< 2초)
- [ ] 에러 시나리오 (손상된 파일, 접근 불가)
- [ ] 대량 파일 (100개, CPU < 80%)

---

## 완료 기준

### Must Have
- [x] FileGroup에 Thumbnail 속성
- [x] PlaceholderImageHelper 구현
- [ ] MonitoringOrchestrator에서 UI 코드 제거
- [ ] MainWindowViewModel에 이미지 로딩 구현
- [ ] Semaphore CPU throttling (UI 레이어)
- [ ] 이벤트 기반 통신 (Core → UI)
- [ ] 빌드 성공
- [ ] 통합 테스트 통과

### Performance
- [ ] 그룹 표시 < 200ms
- [ ] 첫 이미지 < 500ms
- [ ] 모든 이미지 < 2초
- [ ] CPU < 80% (100 files)

---

## 알려진 이슈

| 이슈 | 상태 | 해결 방안 |
|-----|------|----------|
| DI 등록 업데이트 필요 | Pending | Task 2.3.1에서 처리 |
| BitmapSource Freeze() 필수 | Pending | 모든 이미지에 Freeze() 호출 |

---

## 다음 단계

1. Task 2.1.2부터 순차적으로 구현
2. 각 Task 후 빌드 확인
3. 완료 후 Phase 2 완료 보고서 작성
4. 성능 측정 및 최적화

---

**변경 이력**:
- 2025-12-12: 방안 1 채택으로 전면 수정
- 기존 MonitoringOrchestrator 중심 → MainWindowViewModel 중심으로 변경
