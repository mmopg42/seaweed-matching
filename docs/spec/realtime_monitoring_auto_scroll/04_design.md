---
Task: 실시간 감시 자동 스크롤
Created: 2025-01-06
Status: Draft
Depends On: 03_plan.md
---

# 실시간 감시 자동 스크롤 - 상세 설계

## 1. 개요

### 1.1 목적
실시간 감시 상태(`IsMonitoring = true`)에서 새로운 파일 그룹이 추가될 때, 해당 DataGrid를 자동으로 맨 아래로 스크롤하여 최신 파일 정보를 즉시 확인할 수 있도록 구현합니다.

### 1.2 배경
현재 실시간 감시 중에 새로운 파일 그룹이 추가되어도 DataGrid가 자동으로 스크롤되지 않아, 사용자가 수동으로 스크롤해야 최신 정보를 확인할 수 있습니다. LogPanel에서는 이미 자동 스크롤 기능이 구현되어 있어 동일한 패턴을 적용할 수 있습니다.

### 1.3 범위
- `FileGroupDataGrid` 컨트롤에 스크롤 메서드 추가
- `MainWindow.xaml.cs`에서 CollectionChanged 이벤트 구독 및 스크롤 트리거 구현
- Line1, Line2, Combined 탭의 모든 DataGrid에 적용

## 2. 현재 구조 분석

### 2.1 관련 컴포넌트

#### 2.1.1 UI 컴포넌트
- **MainWindow.xaml**: 4개의 `FileGroupDataGrid` 컨트롤
  - `Line1DataGrid`: Line1 탭의 DataGrid
  - `Line2DataGrid`: Line2 탭의 DataGrid
  - `CombinedLine1DataGrid`: Combined 탭의 Line1 DataGrid
  - `CombinedLine2DataGrid`: Combined 탭의 Line2 DataGrid

#### 2.1.2 ViewModel 계층
- **DashboardViewModel.cs** (Line 139-151): `OnGroupCreated` 메서드에서 새 그룹 추가
  - `Line1Groups.Add(vm)` 또는 `Line2Groups.Add(vm)` 호출
  - `WpfApplication.Current.Dispatcher.InvokeAsync` 사용

- **MainWindowViewModel.cs** (Line 34): `IsMonitoring` 속성 제공
  - `Control.IsMonitoring`을 반환하는 계산된 속성

#### 2.1.3 참고 구현
- **LogPanel.xaml.cs** (Line 99-105): `CollectionChanged` 핸들러에서 자동 스크롤 구현
  ```csharp
  private void OnLogMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
  {
      if (AutoScrollCheckBox.IsChecked == true && LogDataGrid.Items.Count > 0)
      {
          LogDataGrid.ScrollIntoView(LogDataGrid.Items[^1]);
      }
  }
  ```

### 2.2 데이터 흐름

```
MonitoringOrchestrator.GroupCreated 이벤트
  → DashboardViewModel.OnGroupCreated
    → Dispatcher.InvokeAsync (UI 스레드로 마샬링)
      → Line1Groups.Add(vm) 또는 Line2Groups.Add(vm)
        → ObservableCollection.CollectionChanged 이벤트 발생 (UI 스레드)
          → MainWindow.xaml.cs 핸들러
            → IsMonitoring 확인
            → Dispatcher.BeginInvoke (레이아웃 완료 대기)
              → FileGroupDataGrid.ScrollToBottom()
```

### 2.3 Dispatcher 중복 가능성 분석

#### 2.3.1 현재 상황
- **DashboardViewModel.OnGroupCreated** (Line 141): `WpfApplication.Current.Dispatcher.InvokeAsync` 사용
- **ObservableCollection.Add**: UI 스레드에서 호출되면 `CollectionChanged` 이벤트도 UI 스레드에서 발생
- **결론**: `CollectionChanged` 핸들러는 이미 UI 스레드에서 실행됨

#### 2.3.2 Dispatcher 사용 목적
- **첫 번째 Dispatcher.InvokeAsync** (DashboardViewModel): 백그라운드 스레드에서 UI 스레드로 마샬링
- **두 번째 Dispatcher.BeginInvoke** (MainWindow): 레이아웃 완료 대기
  - DataGrid에 새 항목이 추가되고 렌더링이 완료된 후 스크롤해야 함
  - `DispatcherPriority.Render` 사용 권장

#### 2.3.3 중복 가능성 검증
- **문제 없음**: 두 Dispatcher는 서로 다른 목적
  - 첫 번째: 스레드 마샬링
  - 두 번째: 레이아웃 완료 대기
- **성능 영향**: 최소 (레이아웃 완료 후 한 번만 실행)

## 3. 상세 설계

### 3.1 FileGroupDataGrid에 스크롤 메서드 추가

#### 3.1.1 메서드 시그니처

**파일**: `ChronoView/UI/Controls/FileGroupDataGrid.xaml.cs`

```csharp
/// <summary>
/// DataGrid를 맨 아래로 스크롤하여 마지막 항목을 표시합니다.
/// LogPanel 패턴을 참고하여 ScrollIntoView를 사용합니다.
/// </summary>
public void ScrollToBottom()
{
    // 예외 처리: MainDataGrid가 null이거나 항목이 없으면 스킵
    if (MainDataGrid == null || MainDataGrid.Items.Count == 0) return;
    
    // ScrollIntoView를 사용하여 마지막 항목으로 스크롤 (LogPanel 패턴)
    MainDataGrid.ScrollIntoView(MainDataGrid.Items[^1]);
}
```

#### 3.1.2 설계 결정사항

1. **스크롤 방법**: `ScrollIntoView` 사용
   - LogPanel과 동일한 패턴
   - VisualTreeHelper로 ScrollViewer를 찾는 것보다 간단하고 안정적

2. **예외 처리**: null 체크 및 빈 컬렉션 체크
   - `MainDataGrid`가 null인 경우 처리
   - `Items.Count == 0`인 경우 스킵

3. **인덱스 접근**: `Items[^1]` 사용 (C# 8.0 인덱서)
   - 마지막 항목에 직접 접근
   - `Items[Items.Count - 1]`보다 간결

### 3.2 MainWindow에서 CollectionChanged 구독

#### 3.2.1 이벤트 구독 초기화

**파일**: `ChronoView/MainWindow.xaml.cs`

**Window_Loaded 이벤트 핸들러 수정**:
```csharp
private void Window_Loaded(object sender, RoutedEventArgs e)
{
    _logger?.LogInformation("MainWindow loaded");

    // DashboardViewModel의 Line1Groups, Line2Groups 구독
    _viewModel.Dashboard.Line1Groups.CollectionChanged += OnLine1GroupsChanged;
    _viewModel.Dashboard.Line2Groups.CollectionChanged += OnLine2GroupsChanged;

    // Restore window state from configuration
    // This will be implemented when window state manager is available
}
```

#### 3.2.2 CollectionChanged 핸들러 구현

**Line1Groups 변경 핸들러**:
```csharp
private void OnLine1GroupsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
{
    // IsMonitoring 확인: CollectionChanged 핸들러에서 직접 확인
    // 참고: DashboardViewModel.OnGroupCreated에서 이미 Dispatcher.InvokeAsync를 사용하므로
    //       CollectionChanged는 UI 스레드에서 발생함. Dispatcher.BeginInvoke는 레이아웃 완료 대기용
    if (!_viewModel.IsMonitoring) return;
    
    if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
    {
        // DispatcherPriority.Render: 레이아웃 완료 후 실행되어 더 안정적
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
        {
            Line1DataGrid?.ScrollToBottom();
            CombinedLine1DataGrid?.ScrollToBottom();
        }));
    }
}
```

**Line2Groups 변경 핸들러**:
```csharp
private void OnLine2GroupsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
{
    if (!_viewModel.IsMonitoring) return;
    
    if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
    {
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
        {
            Line2DataGrid?.ScrollToBottom();
            CombinedLine2DataGrid?.ScrollToBottom();
        }));
    }
}
```

#### 3.2.3 Window_Closing에서 이벤트 구독 해제

```csharp
private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
{
    _logger?.LogInformation("MainWindow closing");
    
    // Unsubscribe from CollectionChanged events
    if (_viewModel?.Dashboard != null)
    {
        _viewModel.Dashboard.Line1Groups.CollectionChanged -= OnLine1GroupsChanged;
        _viewModel.Dashboard.Line2Groups.CollectionChanged -= OnLine2GroupsChanged;
    }
    
    // Unsubscribe from ViewModel events
    if (_viewModel != null)
    {
        _viewModel.RequestOpenSettings -= OnRequestOpenSettings;
    }

    // Save window state to configuration
    // This will be implemented when window state manager is available
}
```

#### 3.2.4 설계 결정사항

1. **IsMonitoring 확인 위치**: `CollectionChanged` 핸들러에서 직접 확인
   - `SystemControlViewModel.IsMonitoring`은 `SetProperty`로 변경 알림 발생
   - 별도 이벤트 구독 없이 현재 상태 확인이 더 간단하고 안정적

2. **Action 필터링**: `NotifyCollectionChangedAction.Add`만 처리
   - 새 항목 추가 시에만 스크롤
   - Remove, Replace 등은 처리하지 않음

3. **Dispatcher 우선순위**: `DispatcherPriority.Render` 사용
   - 레이아웃 완료 후 실행되어 더 안정적
   - `DispatcherPriority.Input`도 가능하지만 `Render`가 DataGrid 렌더링 완료 후 스크롤하므로 더 적합

4. **DataGrid 인스턴스 매핑**:
   - Line1Groups 변경 → Line1DataGrid, CombinedLine1DataGrid
   - Line2Groups 변경 → Line2DataGrid, CombinedLine2DataGrid
   - Combined 탭의 DataGrid도 동일한 컬렉션을 바인딩하므로 자동으로 스크롤됨

5. **Null 안전성**: Null 조건부 연산자(`?.`) 사용
   - DataGrid 인스턴스가 null인 경우 처리

## 4. 구현 세부사항

### 4.1 변경 파일 목록

1. **ChronoView/UI/Controls/FileGroupDataGrid.xaml.cs**
   - `ScrollToBottom()` 메서드 추가

2. **ChronoView/MainWindow.xaml.cs**
   - `Window_Loaded` 이벤트 핸들러 수정
   - `OnLine1GroupsChanged` 메서드 추가
   - `OnLine2GroupsChanged` 메서드 추가
   - `Window_Closing` 이벤트 핸들러 수정

### 4.2 변경 사항 요약

| 파일 | 변경 내용 | 영향 범위 |
|------|----------|----------|
| `FileGroupDataGrid.xaml.cs` | `ScrollToBottom()` 메서드 추가 | 모든 FileGroupDataGrid 인스턴스 |
| `MainWindow.xaml.cs` | CollectionChanged 이벤트 구독 및 스크롤 트리거 | 실시간 감시 중 새 그룹 추가 시 |

### 4.3 코드 변경 상세

#### 변경 1: FileGroupDataGrid.xaml.cs
- **위치**: 클래스 끝부분 (Line 172 이후)
- **변경 타입**: 새 메서드 추가
- **위험도**: 낮음 (기존 기능에 영향 없음)

#### 변경 2: MainWindow.xaml.cs
- **위치**: 
  - `Window_Loaded` 메서드 수정 (Line 49-55)
  - `Window_Closing` 메서드 수정 (Line 57-69)
  - 새 메서드 2개 추가
- **변경 타입**: 이벤트 구독 및 핸들러 추가
- **위험도**: 낮음 (조건부 실행)

## 5. 테스트 계획

### 5.1 단위 테스트

#### 테스트 케이스 1: ScrollToBottom - 정상 케이스
- **입력**: MainDataGrid에 10개 항목 존재
- **예상 결과**: 마지막 항목으로 스크롤됨
- **검증**: `ScrollIntoView`가 마지막 항목에 대해 호출됨

#### 테스트 케이스 2: ScrollToBottom - 빈 컬렉션
- **입력**: MainDataGrid.Items.Count == 0
- **예상 결과**: 메서드가 조기 반환 (스크롤 안 함)
- **검증**: 예외 발생하지 않음

#### 테스트 케이스 3: ScrollToBottom - Null MainDataGrid
- **입력**: MainDataGrid == null
- **예상 결과**: 메서드가 조기 반환 (예외 발생하지 않음)
- **검증**: NullReferenceException 발생하지 않음

### 5.2 통합 테스트

#### 테스트 케이스 1: 실시간 감시 중 새 그룹 추가
- **시나리오**: 
  1. 모니터링 시작
  2. 새 파일 그룹 추가 (Line1)
  3. 자동 스크롤 확인
- **검증**: 
  - Line1DataGrid가 마지막 항목으로 스크롤됨
  - CombinedLine1DataGrid도 마지막 항목으로 스크롤됨

#### 테스트 케이스 2: 모니터링 중지 시 스크롤 안 됨
- **시나리오**: 
  1. 모니터링 중지
  2. 새 파일 그룹 추가 (프로그래밍 방식)
  3. 스크롤 안 됨 확인
- **검증**: DataGrid가 스크롤되지 않음

#### 테스트 케이스 3: Combined 탭에서도 작동
- **시나리오**: 
  1. 모니터링 시작
  2. Combined 탭 활성화
  3. 새 파일 그룹 추가
  4. 자동 스크롤 확인
- **검증**: CombinedLine1DataGrid 또는 CombinedLine2DataGrid가 스크롤됨

### 5.3 성능 테스트

#### 테스트 케이스 1: 빠른 연속 추가
- **시나리오**: 1초에 10개 그룹 추가
- **검증**: 
  - 성능 저하 없음
  - 마지막 항목으로 정확히 스크롤됨
  - UI 응답성 유지

#### 테스트 케이스 2: 대량 데이터
- **시나리오**: 1000개 이상의 그룹이 있는 상태에서 새 그룹 추가
- **검증**: 스크롤이 빠르게 완료됨

### 5.4 수동 테스트 시나리오

1. **기본 기능 테스트**
   - 모니터링 시작
   - 새 파일 그룹 추가 확인
   - 자동 스크롤 확인

2. **모니터링 상태 테스트**
   - 모니터링 중지 후 새 그룹 추가 → 스크롤 안 됨
   - 모니터링 재시작 후 새 그룹 추가 → 스크롤 됨

3. **탭 전환 테스트**
   - Line1 탭에서 새 그룹 추가 → Line1DataGrid 스크롤
   - Combined 탭으로 전환 → CombinedLine1DataGrid도 스크롤됨

4. **사용자 스크롤 테스트**
   - 사용자가 수동으로 스크롤 위치 변경
   - 새 그룹 추가 시 자동 스크롤됨 (초기 구현)

## 6. 리스크 및 대응 방안

### 6.1 리스크 분석

| 리스크 | 가능성 | 영향도 | 대응 방안 |
|--------|--------|--------|----------|
| Dispatcher 중복으로 인한 성능 저하 | 낮음 | 낮음 | DispatcherPriority.Render 사용으로 최소화 |
| 사용자 스크롤 위치 무시 | 중간 | 중간 | 초기 구현에서는 허용, 필요 시 사용자 스크롤 감지 추가 |
| 빠른 연속 추가 시 스크롤 누락 | 낮음 | 낮음 | Dispatcher.BeginInvoke가 자동으로 최신 요청만 처리 |
| 메모리 누수 (이벤트 구독 해제 누락) | 낮음 | 높음 | Window_Closing에서 명시적으로 구독 해제 |

### 6.2 하위 호환성

- **기존 기능**: 모든 기존 기능에 영향 없음
- **선택적 동작**: `IsMonitoring`이 false일 때는 스크롤하지 않음
- **조건부 실행**: 새 항목 추가 시에만 실행

### 6.3 성능 고려사항

- **Dispatcher 우선순위**: `Render` 사용으로 불필요한 스크롤 최소화
- **조건부 실행**: `IsMonitoring` 확인으로 불필요한 처리 방지
- **Action 필터링**: `Add` 액션만 처리하여 불필요한 스크롤 방지

## 7. 참고사항

### 7.1 LogPanel 패턴
- `LogPanel.xaml.cs`의 `OnLogMessagesCollectionChanged` 메서드를 참고
- `ScrollIntoView`를 직접 호출 (Dispatcher 없음)
- 하지만 DataGrid는 레이아웃 완료를 기다려야 하므로 `Dispatcher.BeginInvoke` 필요

### 7.2 Dispatcher 사용 원칙
- **첫 번째 Dispatcher** (DashboardViewModel): 스레드 마샬링
- **두 번째 Dispatcher** (MainWindow): 레이아웃 완료 대기
- **우선순위**: `Render` 권장 (레이아웃 완료 후 실행)

### 7.3 향후 개선 사항
- **사용자 스크롤 감지**: 사용자가 수동으로 스크롤 위치를 변경한 경우 자동 스크롤 비활성화
- **스크롤 애니메이션**: 부드러운 스크롤 애니메이션 추가
- **설정 옵션**: 자동 스크롤 활성화/비활성화 설정 추가

## 8. 구현 체크리스트

- [ ] `FileGroupDataGrid.xaml.cs`에 `ScrollToBottom()` 메서드 추가
- [ ] `MainWindow.xaml.cs`의 `Window_Loaded`에서 CollectionChanged 이벤트 구독
- [ ] `OnLine1GroupsChanged` 핸들러 구현
- [ ] `OnLine2GroupsChanged` 핸들러 구현
- [ ] `Window_Closing`에서 이벤트 구독 해제
- [ ] 단위 테스트 작성 및 실행
- [ ] 통합 테스트 실행
- [ ] 수동 테스트 수행 (모니터링 시작/중지, 탭 전환)
- [ ] 성능 테스트 (빠른 연속 추가)
- [ ] 코드 리뷰


