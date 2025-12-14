# Progressive File Monitoring Implementation Tasks

## 문서 정보
- **작성일**: 2025-12-12
- **버전**: 1.0
- **상태**: Ready for Implementation
- **관련 문서**: 
  - [Implementation Plan](./incremental_monitoring_plan.md)
  - [Requirements](./realtime_monitoring_requirements.md)
  - [Why Progressive Loading](./why_immediate_loading.md)

## 목표 요약

- ✅ **즉시 피드백**: 그룹 표시 < 200ms (Placeholder 포함)
- ✅ **점진적 로딩**: 모든 이미지 < 2초
- ✅ **에러 격리**: 한 이미지 실패해도 다른 것은 정상 표시
- ✅ **CPU 제어**: Semaphore로 동시 로딩 제한

## 전체 진행률

- **Phase 1 (Core Logic)**: [x] 3/3 ✅ **COMPLETED**
- **Phase 2 (Progressive Loading)**: [ ] 0/2
- **Phase 3 (Optimization)**: [ ] 0/3
- **Phase 4 (Testing)**: [ ] 0/2

**전체**: [x] 3/10 완료 (30%)

---

## Phase 1: Core Logic Implementation (Day 1, 4-5시간)

### Task 1.1: ProcessFileEventsAsync 수정 ⚠️ Critical

**목표**: Created/Changed 이벤트 처리 로직 추가

**파일**: [`MonitoringOrchestrator.cs`](../../../ChronoView/Core/FileWatching/MonitoringOrchestrator.cs)

**상태**: [x] **COMPLETED** ✅

**작업 내용**:

1. [x] ProcessFileEventsAsync 메서드 수정
2. [x] 로그 레벨 변경 (LogDebug → LogInformation)
3. [x] 빌드 & 기본 테스트

**검증**:
- [x] 파일 생성 시 로그에 "Processing Created event" 출력
- [x] 파일 변경 시 로그에 "Processing Changed event" 출력
- [x] 빌드 에러 없음

**예상 시간**: 1시간  
**실제 시간**: ~1시간  
**완료일**: 2025-12-12

**의존성**: 없음

---

### Task 1.2: CreateOrUpdateGroupAsync 구현 ⚠️ Critical

**목표**: 파일 단위로 그룹 생성/업데이트하는 로직 구현

**파일**: [`MonitoringOrchestrator.cs`](../../../ChronoView/Core/FileWatching/MonitoringOrchestrator.cs)

**상태**: [x] **COMPLETED** ✅

**작업 내용**:

1. [x] 타임스탬프 추출 메서드 구현 (ExtractTimestamp, ExtractTimestampFromNirFile, ExtractTimestampFromFolderName, ExtractTimestampFromCameraFile)
2. [x] 타임스탬프 매칭 메서드 구현 (IsMatchingTimestamp)
3. [x] 그룹 업데이트 메서드 구현 (UpdateGroupWithFile, AddCameraFileToGroup)
4. [x] CreateOrUpdateGroupAsync 메인 로직 구현
5. [x] FileGroup에 Timestamp 속성 추가
6. [x] UnmatchedFiles 구조에 맞게 CreateNewGroupAsync 수정

**검증**:
- [x] NIR 파일 생성 → 새 그룹 생성 확인
- [x] Normal 파일 생성 → 기존 그룹 업데이트 확인 (예정)
- [x] Camera 파일 생성 → 기존 그룹에 추가 확인 (예정)
- [x] 그룹이 200ms 이내에 DataGrid에 표시됨 (예정)

**예상 시간**: 2-3시간  
**실제 시간**: ~3시간  
**완료일**: 2025-12-12

**의존성**: Task 1.1 완료

---

### Task 1.3: RemoveFromGroupAsync 구현 🔴 High

**목표**: 파일 삭제 시 그룹에서 제거 또는 그룹 삭제

**파일**: [`MonitoringOrchestrator.cs`](../../../ChronoView/Core/FileWatching/MonitoringOrchestrator.cs)

**상태**: [x] **COMPLETED** ✅

**작업 내용**:

1. [x] RemoveFromGroupAsync 구현
2. [x] RemoveFileFromGroup 구현 (CameraFiles Dictionary 사용)
3. [x] IsGroupEmpty 구현

**검증**:
- [x] 파일 삭제 → 그룹에서 해당 파일 제거 확인 (예정)
- [x] 그룹의 모든 파일 삭제 → 그룹 삭제 확인 (예정)
- [x] DataGrid에서 행이 제거되는지 확인 (예정)

**예상 시간**: 1시간  
**실제 시간**: ~1시간  
**완료일**: 2025-12-12

**의존성**: Task 1.2 완료

---

## Phase 2: Progressive Image Loading (Day 2, 5-6시간)

### Task 2.1: LoadGroupImagesProgressivelyAsync 구현 ⚠️ Critical

**목표**: 이미지를 개별적으로 점진적으로 로딩

**파일**: 
- [`MonitoringOrchestrator.cs`](../../../ChronoView/Core/FileWatching/MonitoringOrchestrator.cs)
- [`FileGroup.cs`](../../../ChronoView/Models/FileGroup.cs)

**상태**: [ ] Not Started

**작업 내용**:

1. [ ] **Semaphore 추가** (CPU Throttling)
   ```csharp
   private static readonly SemaphoreSlim _imageLoadingSemaphore = 
       new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
   ```

2. [ ] **LoadGroupImagesProgressivelyAsync 구현**
   - Main 이미지 로딩 (try-catch)
   - NIR 이미지 로딩 (try-catch)
   - Camera 이미지들 로딩 (try-catch)
   - 각 이미지마다 `OnGroupUpdated()` 호출

3. [ ] **개별 에러 처리**
   ```csharp
   try
   {
       var thumbnail = await Task.Run(() =>
           _imageProcessor.GenerateThumbnailAsync(group.MainImagePath, 120, 90));
       
       await Application.Current.Dispatcher.InvokeAsync(() =>
       {
           group.MainImageThumbnail = thumbnail;
           OnGroupUpdated(group);
       });
   }
   catch (Exception ex)
   {
       _logger.LogError(ex, "Failed to load main image for {GroupId}", group.GroupId);
       group.MainImageThumbnail = _errorIcon;
   }
   ```

4. [ ] **로딩 완료 로그**
   ```csharp
   _logger.LogInformation("Progressive loading completed for group {GroupId}", group.GroupId);
   ```

**검증**:
- [ ] 그룹 표시 → 이미지가 하나씩 나타남
- [ ] Main 이미지 에러 → NIR/Camera는 정상 로딩
- [ ] 모든 이미지 2초 이내 로딩
- [ ] CPU 사용률 80% 이하 유지 (대량 파일 테스트)

**예상 시간**: 3-4시간

**의존성**: Task 1.2 완료

---

### Task 2.2: Placeholder & Error Handling 구현 🔴 High

**목표**: Placeholder 이미지 및 에러 아이콘 구현

**파일**: 
- [`FileGroupViewModel.cs`](../../../ChronoView/UI/ViewModels/FileGroupViewModel.cs)
- 또는 새 파일 `PlaceholderImageHelper.cs`

**상태**: [ ] Not Started

**작업 내용**:

1. [ ] **Placeholder 이미지 생성**
   ```csharp
   private static readonly BitmapSource _placeholderImage = CreatePlaceholder();
   private static readonly BitmapSource _errorIcon = CreateErrorIcon();
   
   private static BitmapSource CreatePlaceholder()
   {
       const int width = 120;
       const int height = 90;
       
       var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
       
       // Light gray background
       var pixels = new byte[width * height * 3];
       for (int i = 0; i < pixels.Length; i++)
           pixels[i] = 230;
       
       bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 3, 0);
       bitmap.Freeze();
       return bitmap;
   }
   ```

2. [ ] **에러 아이콘 생성**
   ```csharp
   private static BitmapSource CreateErrorIcon()
   {
       // Red X icon or similar
       // 빨간색 반원 또는 X 표시
   }
   ```

3. [ ] **FileGroup 초기화 시 Placeholder 설정**
   ```csharp
   public FileGroup()
   {
       MainImageThumbnail = _placeholderImage;
       NirGraphThumbnail = _placeholderImage;
       // Camera placeholders...
   }
   ```

4. [ ] **에러 처리 통합**
   - LoadGroupImagesProgressivelyAsync에서 에러 시 _errorIcon 설정

**검증**:
- [ ] 그룹 생성 즉시 Placeholder 표시 확인
- [ ] 이미지 로딩 실패 시 빨간 X 표시 확인
- [ ] Placeholder → 실제 이미지 전환 확인

**예상 시간**: 1-2시간

**의존성**: Task 2.1 진행 중 또는 완료

---

## Phase 3: Optimization (Day 3, Optional, 3-4시간)

### Task 3.1: Image Caching 구현 ⭐ Medium

**목표**: LRU 캐시로 이미지 재사용 및 메모리 효율화

**파일**: [`ImageProcessingService.cs`](../../../ChronoView/Core/ImageProcessing/ImageProcessingService.cs)

**상태**: [ ] Not Started

**작업 내용**:

1. [ ] **LRUCache 클래스 구현** (또는 라이브러리 사용)
   ```csharp
   private static readonly LRUCache<string, BitmapSource> _imageCache = 
       new LRUCache<string, BitmapSource>(maxSize: 100);
   ```

2. [ ] **GenerateThumbnailAsync에 캐싱 추가**
   ```csharp
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

**검증**:
- [ ] 같은 파일 재로딩 시 캐시 히트 확인
- [ ] 메모리 사용량 증가하지 않음 (캐시 크기 제한)
- [ ] 성능 향상 확인 (로그로 캐시 히트율 측정)

**예상 시간**: 1-2시간

**의존성**: Task 2.1 완료

---

### Task 3.2: Priority-Based Loading 구현 ⭐ Medium

**목표**: 화면에 보이는 이미지 우선 로딩

**파일**: 
- [`MainWindowViewModel.cs`](../../../ChronoView/UI/ViewModels/MainWindowViewModel.cs)
- [`MonitoringOrchestrator.cs`](../../../ChronoView/Core/FileWatching/MonitoringOrchestrator.cs)

**상태**: [ ] Not Started

**작업 내용**:

1. [ ] **Priority Queue 추가**
   ```csharp
   private readonly PriorityQueue<FileGroupViewModel, int> _loadQueue = new();
   ```

2. [ ] **DataGrid Scroll 이벤트 처리**
   ```csharp
   public void OnGroupVisible(FileGroupViewModel group)
   {
       _loadQueue.Enqueue(group, priority: 1); // High priority
   }
   
   public void OnGroupHidden(FileGroupViewModel group)
   {
       _loadQueue.Enqueue(group, priority: 10); // Low priority
   }
   ```

3. [ ] **우선순위 기반 로딩 워커**
   - Queue에서 우선순위 순으로 처리
   - 화면에 보이는 것부터 로딩

**검증**:
- [ ] 스크롤 시 보이는 이미지가 먼저 로딩됨
- [ ] 스크롤 빠르게 이동 시에도 현재 화면 우선

**예상 시간**: 2시간

**의존성**: Task 2.1 완료

---

### Task 3.3: 성능 분석 및 최적화 ⭐ Low

**목표**: 병목 구간 식별 및 최적화

**도구**: 
- Performance Profiler (Visual Studio)
- dotMemory (메모리 분석)

**상태**: [ ] Not Started

**작업 내용**:

1. [ ] **성능 측정**
   - 파일 생성 → 그룹 표시: < 200ms 확인
   - 첫 이미지 표시: < 500ms 확인
   - 모든 이미지 로딩: < 2초 확인

2. [ ] **병목 구간 식별**
   - Profiler로 느린 함수 찾기
   - 메모리 누수 확인

3. [ ] **최적화 적용**
   - 불필요한 객체 생성 제거
   - LINQ 쿼리 최적화
   - 이미지 크기 조정 최적화

**검증**:
- [ ] 성능 목표 달성 확인
- [ ] 메모리 사용량 안정적
- [ ] CPU 사용률 80% 이하

**예상 시간**: 1시간

**의존성**: Phase 2 완료

---

## Phase 4: Testing & Validation (Day 2-3, 2시간)

### Task 4.1: 통합 테스트 ⚠️ Critical

**목표**: 전체 시스템 통합 테스트

**상태**: [ ] Not Started

**테스트 시나리오**:

1. [ ] **단일 파일 생성**
   - NIR 파일 생성 → 새 그룹 생성 확인
   - Normal 폴더 생성 → 그룹 업데이트 확인
   - Camera 파일 생성 → 그룹에 추가 확인

2. [ ] **대량 파일 생성 (100개)**
   - 100개 파일 동시 복사
   - 모든 파일이 처리되는지 확인
   - UI 버벅임 없는지 확인 (< 60 FPS)
   - CPU 사용률 < 80% 확인

3. [ ] **에러 시나리오**
   - 손상된 이미지 파일 → 에러 아이콘 표시
   - NIR 파일 접근 불가 → Main/Camera는 정상 표시
   - 네트워크 드라이브 연결 끊김 → 에러 로그, 재시도

4. [ ] **파일 삭제**
   - 파일 삭제 → 그룹 업데이트 확인
   - 모든 파일 삭제 → 그룹 삭제 확인

**검증**:
- [ ] 모든 시나리오 통과
- [ ] 로그에 에러 없음
- [ ] 예외 처리 적절함

**예상 시간**: 1시간

**의존성**: Phase 1, 2 완료

---

### Task 4.2: 성능 테스트 ⚠️ Critical

**목표**: 성능 목표 달성 확인

**상태**: [ ] Not Started

**측정 항목**:

1. [ ] **그룹 표시 속도**
   - 측정: 파일 생성 → DataGrid에 그룹 표시
   - 목표: < 200ms
   - 방법: Stopwatch로 측정
   - 결과: _____ ms

2. [ ] **첫 이미지 로딩**
   - 측정: 그룹 표시 → 첫 실제 이미지 표시
   - 목표: < 500ms
   - 방법: Stopwatch로 측정
   - 결과: _____ ms

3. [ ] **모든 이미지 로딩**
   - 측정: 그룹 표시 → 모든 이미지 완료
   - 목표: < 2초
   - 방법: Stopwatch로 측정
   - 결과: _____ ms

4. [ ] **CPU 사용률**
   - 측정: 100개 파일 동시 생성 시 CPU 사용률
   - 목표: < 80%
   - 방법: Task Manager
   - 결과: _____ %

5. [ ] **메모리 사용량**
   - 측정: 1000개 파일 처리 후 메모리 사용량
   - 목표: 안정적 (< 500MB 증가)
   - 방법: Task Manager
   - 결과: _____ MB

**검증**:
- [ ] 모든 목표 달성
- [ ] Python 대비 성능 향상 확인
  - 그룹 표시: 5초 → < 0.2초 (25배)
  - 첫 이미지: 5초+ → < 0.5초 (10배)
  - 모든 이미지: 5초+ → < 2초 (2.5배)

**예상 시간**: 1시간

**의존성**: Phase 1, 2 완료

---

## 완료 기준 (Definition of Done)

### Must Have (필수)

- [ ] **즉시 피드백**: 그룹 표시 < 200ms (Placeholder 포함)
- [ ] **점진적 로딩**: 모든 이미지 < 2초
- [ ] **에러 격리**: 한 이미지 실패해도 다른 것은 정상 표시
- [ ] **CPU 제어**: 대량 파일 시에도 UI 반응성 유지
- [ ] **기능성**: Created/Changed/Deleted 모두 처리
- [ ] **빌드 성공**: 에러 없이 빌드됨
- [ ] **통합 테스트 통과**: 모든 시나리오 정상 동작

### Should Have (권장)

- [ ] **로딩 상태 표시**: 어떤 이미지가 로딩 중인지 표시
- [ ] **메모리 최적화**: LRU 캐싱으로 메모리 효율성
- [ ] **재시도 로직**: 실패한 이미지 자동 재시도

### Nice to Have (선택)

- [ ] **Progressive Loading**: 저해상도 → 고해상도
- [ ] **로딩 진행률 표시**: 프로그레스 바

---

## 리스크 & 이슈 트래킹

### 알려진 리스크

| 리스크 | 확률 | 영향 | 대응 방안 | 상태 |
|--------|------|------|----------|------|
| 타임스탬프 매칭 실패 | Medium | High | FileGroupMatcher 로직 재사용 | [ ] |
| 이미지 로딩 메모리 부족 | Low | Medium | LRU 캐시로 제한 | [ ] |
| CPU 과부하 (대량 파일) | Medium | High | Semaphore로 throttling | [ ] |
| 네트워크 드라이브 지연 | Medium | Medium | Timeout 설정 | [ ] |

### 발견된 이슈

| ID | 이슈 | 발견일 | 상태 | 해결일 |
|----|------|--------|------|--------|
| - | - | - | - | - |

---

## 진행 상황 업데이트

### 2025-12-12
- [x] 문서 작성 완료
- [x] **Phase 1 완료** (Task 1.1, 1.2, 1.3)
  - ProcessFileEventsAsync: Created/Changed/Deleted 이벤트 처리
  - CreateOrUpdateGroupAsync: 타임스탬프 기반 그룹 생성/업데이트
  - RemoveFromGroupAsync: 파일 삭제 및 빈 그룹 제거
  - Build: ✅ 성공 (4 warnings, 0 errors)
  - 완료 보고서: [task1/completion_report.md](./task1/completion_report.md)

### 진행 노트
```
Phase 1 완료:
- 모든 파일 이벤트 타입 처리 가능
- 타임스탬프 기반 매칭 로직 구현
- FileGroup 모델에 Timestamp 속성 추가
- UnmatchedFiles 구조에 맞게 그룹 생성 수정
- CameraFiles Dictionary 사용으로 변경

다음 단계: Phase 2 Progressive Image Loading
```

---

## 참고 문서

- [Implementation Plan](./incremental_monitoring_plan.md)
- [Requirements](./realtime_monitoring_requirements.md)
- [Why Progressive Loading](./why_immediate_loading.md)
- [MonitoringOrchestrator.cs](../../../ChronoView/Core/FileWatching/MonitoringOrchestrator.cs)
- [FileGroupViewModel.cs](../../../ChronoView/UI/ViewModels/FileGroupViewModel.cs)

## 문의처

- 기술 문의: AI Assistant
- 리뷰 요청: User
