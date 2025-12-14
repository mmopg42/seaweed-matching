# Phase 2 Progressive Image Loading - Architecture Issue

**발생일**: 2025-12-12  
**상태**: 🔴 Blocked  
**심각도**: High  

---

## 📋 현재 상황

### 구현 완료 항목
- ✅ Task 2.1.1: `FileGroup` 모델에 Thumbnail 속성 추가
- ✅ Task 2.2.1: `PlaceholderImageHelper` 생성 (회색 placeholder, 빨간 error icon)
- ✅ Task 2.1.2: `MonitoringOrchestrator` 생성자에 `IImageProcessor`, `NirGraphGenerator` 주입
- ✅ Task 2.1.3: `LoadGroupImagesProgressivelyAsync` 메서드 구현 (147줄)
  - Semaphore를 이용한 CPU throttling
  - Main/NIR/Camera 이미지 개별 로딩 로직
  - 각 이미지마다 독립적인 try-catch 에러 격리
  - `ConvertBytesToBitmapSource` 헬퍼 메서드

### 빌드 에러
```
error CS0234: 'System' 형식 또는 네임스페이스 이름이 'System.Windows' 네임스페이스에 없습니다.
```

**에러 발생 위치**: `MonitoringOrchestrator.cs:828`
```csharp
await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
{
    group.MainImageThumbnail = thumbnail;
    OnGroupUpdated(group);
});
```

---

## 🔍 근본 원인 분석

### 아키텍처 레이어 구조

```
┌─────────────────────────────────────┐
│        UI Layer (ChronoView.UI)     │
│  - MainWindow, ViewModels           │
│  - WPF dependencies (Application)   │
└─────────────────────────────────────┘
              ↓ depends on
┌─────────────────────────────────────┐
│      Core Layer (ChronoView.Core)   │
│  - MonitoringOrchestrator ⬅️ HERE   │
│  - Business Logic                   │
│  - NO WPF dependencies              │
└─────────────────────────────────────┘
              ↓ depends on
┌─────────────────────────────────────┐
│    Models Layer (ChronoView.Models) │
│  - FileGroup, Configuration         │
└─────────────────────────────────────┘
```

### 문제점

1. **레이어 의존성 위반**
   - `MonitoringOrchestrator`는 Core 레이어에 위치
   - `System.Windows.Application`은 WPF UI 레이어에 속함
   - Core → UI 의존성은 **클린 아키텍처 위반**

2. **WPF 참조 부재**
   - ChronoView.Core 프로젝트에 WPF 참조가 없음
   - `System.Windows.Application`을 사용할 수 없음
   - 빌드 에러 발생

3. **설계 의도와 충돌**
   - Tasks.md는 MonitoringOrchestrator에서 구현하라고 명시
   - 하지만 UI 스레드 업데이트가 필요 → UI 레이어 작업
   - **요구사항과 아키텍처가 충돌**

---

## 💡 해결 방안

### 방안 1: UI 레이어로 이미지 로딩 이동 (MainWindowViewModel)

#### 구현 방식
```csharp
// MainWindowViewModel.cs
private void OnGroupCreated(object sender, FileGroup group)
{
    // Initialize with placeholders
    group.MainImageThumbnail = PlaceholderImageHelper.PlaceholderImage;
    group.NirGraphThumbnail = PlaceholderImageHelper.PlaceholderImage;
    // ... camera placeholders

    // Start progressive loading
    _ = LoadGroupImagesAsync(group);
}

private async Task LoadGroupImagesAsync(FileGroup group)
{
    await _semaphore.WaitAsync();
    try
    {
        // Load Main image
        var thumbnailBytes = await _imageProcessor.GenerateThumbnailAsync(...);
        var thumbnail = ConvertBytesToBitmapSource(thumbnailBytes);
        
        // UI thread - no Dispatcher needed (already in UI context)
        group.MainImageThumbnail = thumbnail;
        // PropertyChanged event will update UI
        
        // ... NIR, Camera images
    }
    finally
    {
        _semaphore.Release();
    }
}
```

#### 장점 ✅
1. **아키텍처 준수**: UI 작업을 UI 레이어에서 처리 (올바른 레이어 분리)
2. **Dispatcher 불필요**: ViewModel은 이미 UI 스레드 컨텍스트
3. **간단한 구현**: Dispatcher.InvokeAsync 호출 제거 가능
4. **테스트 용이**: MonitoringOrchestrator는 순수 비즈니스 로직만 담당

#### 단점 ❌
1. **이벤트 핸들링 추가**: GroupCreated 이벤트 구독 필요
2. **책임 분산**: 이미지 로딩 로직이 ViewModel에 분산
3. **Tasks.md와 불일치**: MonitoringOrchestrator에서 구현하라는 명시와 다름
4. **코드 이동 필요**: 이미 작성한 147줄을 다시 옮겨야 함

---

### 방안 2: Dispatcher를 생성자로 주입

#### 구현 방식
```csharp
// MonitoringOrchestrator.cs
private readonly System.Windows.Threading.Dispatcher _dispatcher;

public MonitoringOrchestrator(
    IFileGroupMatcher fileGroupMatcher,
    IFileWatcher fileWatcher,
    IImageProcessor imageProcessor,
    NirGraphGenerator nirGraphGenerator,
    System.Windows.Threading.Dispatcher dispatcher, // 추가
    ILogger<MonitoringOrchestrator> logger)
{
    // ...
    _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
}

// Usage in LoadGroupImagesProgressivelyAsync
await _dispatcher.InvokeAsync(() =>
{
    group.MainImageThumbnail = thumbnail;
    OnGroupUpdated(group);
});
```

#### DI 등록
```csharp
// App.xaml.cs
services.AddSingleton(Dispatcher.CurrentDispatcher);
services.AddSingleton<MonitoringOrchestrator>();
```

#### 장점 ✅
1. **Tasks.md 준수**: MonitoringOrchestrator에서 구현 유지
2. **최소 변경**: 생성자만 수정, 기존 코드 재활용
3. **명시적 의존성**: Dispatcher 필요성이 명확히 드러남
4. **DI 패턴**: 의존성 주입으로 관리

#### 단점 ❌
1. **아키텍처 위반**: Core 레이어가 UI 레이어 타입(Dispatcher)에 의존
2. **WPF 참조 필요**: ChronoView.Core에 WPF 참조 추가 필요
3. **플랫폼 결합**: Core가 WPF에 강하게 결합됨 (다른 UI 프레임워크 사용 불가)
4. **테스트 어려움**: Dispatcher 모킹 필요

---

### 방안 3: SynchronizationContext 사용

#### 구현 방식
```csharp
// MonitoringOrchestrator.cs
private readonly SynchronizationContext? _syncContext;

public MonitoringOrchestrator(
    IFileGroupMatcher fileGroupMatcher,
    IFileWatcher fileWatcher,
    IImageProcessor imageProcessor,
    NirGraphGenerator nirGraphGenerator,
    ILogger<MonitoringOrchestrator> logger,
    SynchronizationContext? syncContext = null) // 옵셔널
{
    // ...
    _syncContext = syncContext ?? SynchronizationContext.Current;
}

// Usage
if (_syncContext != null)
{
    _syncContext.Post(_ =>
    {
        group.MainImageThumbnail = thumbnail;
        OnGroupUpdated(group);
    }, null);
}
else
{
    // Direct call (for testing or non-UI scenarios)
    group.MainImageThumbnail = thumbnail;
    OnGroupUpdated(group);
}
```

#### 장점 ✅
1. **플랫폼 독립적**: WPF, WinForms, Avalonia 등 모두 지원
2. **테스트 가능**: null일 때 동기 실행으로 테스트 가능
3. **표준 API**: .NET 표준 라이브러리 사용
4. **WPF 참조 불필요**: System.Threading.SynchronizationContext

#### 단점 ❌
1. **여전히 아키텍처 위반**: Core가 UI 스레드 개념에 의존
2. **복잡성 증가**: null 체크, 에러 처리 추가 필요
3. **Post는 비동기 void**: 예외 처리가 까다로움
4. **타이밍 이슈**: Post는 fire-and-forget이라 await 불가

---

## 🎯 권장 사항

### ✅ **방안 1: UI 레이어로 이미지 로딩 이동** (강력 추천)

#### 이유

1. **클린 아키텍처 준수**
   - 각 레이어가 자신의 책임만 담당
   - Core는 비즈니스 로직, UI는 프레젠테이션
   - 장기적으로 유지보수가 쉬움

2. **실제 WPF 패턴과 일치**
   - MVVM 패턴에서 ViewModel이 데이터 로딩 담당
   - PropertyChanged 이벤트로 자동 UI 업데이트
   - Dispatcher 관리가 자연스러움

3. **테스트 용이성**
   - MonitoringOrchestrator는 단위 테스트 가능
   - UI 로직은 통합 테스트에서 검증
   - 모킹 불필요

4. **확장성**
   - 다른 UI 프레임워크 지원 가능
   - Non-UI 시나리오(서버, CLI)에서도 Core 재사용 가능

#### 구현 계획

1. **MonitoringOrchestrator 역할 재정의**
   ```csharp
   // MonitoringOrchestrator는 파일 감지 + 그룹 관리만
   public async Task<FileGroup?> CreateOrUpdateGroupAsync(...)
   {
       // ... 그룹 생성/업데이트
       
       // Placeholder 초기화
       newGroup.MainImageThumbnail = PlaceholderImageHelper.PlaceholderImage;
       
       // 이벤트 발생 (UI에게 알림)
       OnGroupCreated(newGroup);
       
       return newGroup;
   }
   ```

2. **MainWindowViewModel에서 이미지 로딩**
   ```csharp
   public MainWindowViewModel(
       IMonitoringOrchestrator orchestrator,
       IImageProcessor imageProcessor,
       NirGraphGenerator nirGraphGenerator,
       ...)
   {
       orchestrator.GroupCreated += OnGroupCreated;
   }
   
   private void OnGroupCreated(object sender, FileGroup group)
   {
       // Add to ObservableCollection (already on UI thread)
       FileGroups.Add(new FileGroupViewModel(group, ...));
       
       // Start progressive loading
       _ = LoadGroupImagesAsync(group);
   }
   
   private async Task LoadGroupImagesAsync(FileGroup group)
   {
       // ... 이미지 로딩 로직 (이미 작성한 코드 재사용)
   }
   ```

3. **Tasks.md 업데이트**
   - 문서의 의도를 재해석: "Progressive Loading 기능 구현"
   - 위치는 아키텍처에 맞게 조정 가능

---

## 🚫 비권장 방안

### ❌ **방안 2: Dispatcher 주입** - 사용하지 말 것

**이유**:
- 아키텍처 위반이 DI로 숨겨지는 것일 뿐, 근본 문제 해결 안 됨
- ChronoView.Core에 WPF 참조 추가는 **심각한 아키텍처 오염**
- 향후 확장성 제로 (WPF에 영구 종속)

### ⚠️ **방안 3: SynchronizationContext** - 절충안

**사용 가능 시나리오**:
- 정말로 Core에서 UI 업데이트가 필요한 경우
- 플랫폼 독립성이 중요한 경우

**하지만**:
- 현재 상황에서는 방안 1이 더 나음
- 복잡도 대비 이득이 적음

---

## 📝 결론

### 최종 추천: **방안 1 - UI 레이어로 이동**

**이유 요약**:
1. ✅ 클린 아키텍처 준수 (가장 중요)
2. ✅ WPF MVVM 패턴과 자연스럽게 일치
3. ✅ 테스트 가능, 유지보수 쉬움
4. ✅ 확장성 확보 (다른 UI 프레임워크 지원 가능)
5. ⚠️ Tasks.md와 약간 다르지만, "올바른 구현"이 우선

### Tasks.md 해석

원래 의도:
- "Progressive Loading **기능**을 구현하라"
- 반드시 MonitoringOrchestrator에 있어야 한다는 제약이 아님

조정된 해석:
- MonitoringOrchestrator: 파일 감지 + 그룹 생성 + Placeholder 초기화
- MainWindowViewModel: 실제 이미지 로딩 + UI 업데이트
- **협력하여 Progressive Loading 기능 제공**

---

## 🔄 다음 단계

1. [ ] 사용자 의사 결정 대기
2. [ ] 방안 1 선택 시:
   - MonitoringOrchestrator에서 UI 업데이트 코드 제거
   - MainWindowViewModel에 LoadGroupImagesAsync 추가
   - GroupCreated 이벤트 구독
   - Tasks.md 업데이트 (역할 명확화)
3. [ ] 빌드 & 테스트
4. [ ] Task 2.1.4, 2.2.2 계속 진행

---

**작성**: AI Assistant  
**날짜**: 2025-12-12  
**카테고리**: Architecture Issue
