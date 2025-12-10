# Task 4.3: MainWindow.xaml 수정 - 구현 보고서

## 개요

**작업일자**: 2025-12-11
**작업 내용**: MainWindow.xaml의 StatusBar, TabControl, DataGrid 바인딩 확인 및 ProgressBar 추가
**상태**: ✅ 완료

---

## 1. 작업 목표

MainWindow.xaml에서 UI 컨트롤과 ViewModel의 바인딩을 완성하고, 장기 작업(Move/Delete/Refresh) 진행 상태를 표시할 수 있는 ProgressBar를 추가합니다.

### 1.1 요구사항

1. **StatusBar에 ProgressBar 추가**: 파일 작업 진행률을 시각적으로 표시
2. **TabControl 바인딩 확인**: `ActiveTabIndex` 속성과 연동하여 탭 전환 추적
3. **DataGrid SelectedItem 바인딩 확인**: Line1/Line2 선택 항목 관리
4. **IsSelected 바인딩 확인**: 다중 선택을 위한 체크박스 연동

---

## 2. 구현 내용

### 2.1 BoolToVisibilityConverter 추가 (Lines 20-21)

**목적**: `IsOperationInProgress` boolean 속성을 `Visibility` enum으로 변환하여 ProgressBar 표시/숨김 제어

**변경 전**:
```xml
<Window.Resources>
    <!-- Converters -->
    <local:NullToVisibilityConverter x:Key="NullToVisibilityConverter"/>
```

**변경 후**:
```xml
<Window.Resources>
    <!-- Converters -->
    <local:NullToVisibilityConverter x:Key="NullToVisibilityConverter"/>
    <local:BoolToVisibilityConverter x:Key="BoolToVisibilityConverter"/>
```

**구현체**: `ChronoView/Converters/BoolToVisibilityConverter.cs` (기존 존재)

---

### 2.2 StatusBar에 ProgressBar 추가 (Lines 266-276)

**목적**: 파일 이동/삭제/새로고침 등 장기 작업의 진행 상태를 실시간으로 표시

**변경 전**:
```xml
<StatusBarItem HorizontalAlignment="Right">
    <TextBlock Text="{Binding CurrentTime, StringFormat={}{0:HH:mm:ss}}"/>
</StatusBarItem>
```

**변경 후**:
```xml
<StatusBarItem HorizontalAlignment="Right" Margin="10,0,0,0">
    <StackPanel Orientation="Horizontal" Width="200">
        <ProgressBar Width="150" Height="16"
                     Value="{Binding ProgressValue}"
                     Minimum="0" Maximum="100"
                     Visibility="{Binding IsOperationInProgress, Converter={StaticResource BoolToVisibilityConverter}}"/>
        <TextBlock Text="{Binding ProgressValue, StringFormat={}{0:F0}%}"
                   Margin="8,0,0,0" VerticalAlignment="Center"
                   Visibility="{Binding IsOperationInProgress, Converter={StaticResource BoolToVisibilityConverter}}"/>
    </StackPanel>
</StatusBarItem>
<StatusBarItem HorizontalAlignment="Right">
    <TextBlock Text="{Binding CurrentTime, StringFormat={}{0:HH:mm:ss}}"/>
</StatusBarItem>
```

**바인딩 속성**:
- `ProgressValue` (double, 0-100): 현재 진행률 퍼센트
- `IsOperationInProgress` (bool): 작업 진행 여부

**동작**:
1. `IsOperationInProgress == false`: ProgressBar와 퍼센트 텍스트 숨김
2. `IsOperationInProgress == true`: ProgressBar 표시 및 실시간 진행률 업데이트

---

### 2.3 기존 바인딩 확인 (변경 없음)

다음 항목들은 이미 올바르게 구현되어 있음을 확인했습니다:

#### 2.3.1 TabControl SelectedIndex 바인딩 (Line 341)

```xml
<TabControl Grid.Row="0" SelectedIndex="{Binding ActiveTabIndex}">
```

**효과**:
- ViewModel에서 `ActiveTabIndex` 변경 시 자동으로 탭 전환
- `GetSelectedGroups()` 메서드에서 현재 활성 탭 기준 선택 항목 조회

#### 2.3.2 Line 1 DataGrid SelectedItem 바인딩 (Line 344)

```xml
<DataGrid ItemsSource="{Binding Line1Groups}"
          SelectedItem="{Binding SelectedLine1Group}"
          Style="{StaticResource FileGroupDataGridStyle}"
          RowStyle="{StaticResource FileGroupRowStyle}">
```

**효과**: 사용자가 Line 1 탭에서 행 선택 시 `SelectedLine1Group` 속성 자동 업데이트

#### 2.3.3 Line 2 DataGrid SelectedItem 바인딩 (Line 434)

```xml
<DataGrid ItemsSource="{Binding Line2Groups}"
          SelectedItem="{Binding SelectedLine2Group}"
          Style="{StaticResource FileGroupDataGridStyle}"
          RowStyle="{StaticResource FileGroupRowStyle}">
```

**효과**: 사용자가 Line 2 탭에서 행 선택 시 `SelectedLine2Group` 속성 자동 업데이트

#### 2.3.4 DataGridCheckBoxColumn IsSelected 바인딩 (Lines 351, 441)

```xml
<DataGridCheckBoxColumn Header="" Width="40" Binding="{Binding IsSelected}"/>
```

**바인딩 모드**: TwoWay (DataGridCheckBoxColumn의 기본값)

**효과**:
- 사용자가 체크박스 클릭 → `FileGroupViewModel.IsSelected` 업데이트
- ViewModel에서 `IsSelected` 변경 → 체크박스 UI 업데이트
- `DragSelectBehavior`와 연동하여 드래그 선택 지원

---

## 3. ViewModel 연동

### 3.1 MainWindowViewModel 속성

Task 4.3에서 사용하는 ViewModel 속성들 (Phase 1에서 이미 구현됨):

```csharp
// Progress Reporting
public double ProgressValue { get; set; }           // 0-100
public bool IsOperationInProgress { get; set; }     // 작업 진행 여부

// Tab Management
public int ActiveTabIndex { get; set; }             // 0: Line1, 1: Line2, 2: Combined

// Selection Management
public FileGroupViewModel? SelectedLine1Group { get; set; }
public FileGroupViewModel? SelectedLine2Group { get; set; }

// Computed Property
public FileGroupViewModel? SelectedGroup =>
    ActiveTabIndex switch
    {
        0 => SelectedLine1Group,
        1 => SelectedLine2Group,
        _ => null
    };
```

### 3.2 Progress 업데이트 예시

**ExecuteMoveAsync()에서 Progress 리포팅**:
```csharp
var progress = new Progress<OperationProgress>(p =>
{
    ProgressValue = (double)p.ProcessedFiles / p.TotalFiles * 100;
    StatusMessage = $"{p.Status}: {p.CurrentFile}";
});

var result = await _fileOperationService.MoveFileGroupAsync(
    group, destFolder, onConflict, progress, _operationCts.Token);
```

**BeginOperation() / EndOperation()**:
```csharp
private void BeginOperation()
{
    IsOperationInProgress = true;
    ProgressValue = 0;
    // Start/Stop/Refresh 버튼 비활성화
}

private void EndOperation()
{
    IsOperationInProgress = false;
    ProgressValue = 0;
    // 버튼 다시 활성화
}
```

---

## 4. 사용자 시나리오

### 4.1 파일 이동 작업 (Move)

1. 사용자가 Line 1 탭에서 여러 그룹 선택 (DragSelectBehavior 또는 체크박스)
2. "Move" 버튼 클릭
3. **StatusBar 오른쪽에 ProgressBar 표시**:
   - "Copying: file1.jpg" → 33%
   - "Copying: file2.jpg" → 66%
   - "Cleaning up: file3.jpg" → 100%
4. 작업 완료 시 ProgressBar 자동 숨김
5. 선택된 그룹들이 목록에서 제거됨

### 4.2 파일 삭제 작업 (Delete)

1. 사용자가 Line 2 탭에서 그룹 선택
2. "Delete" 버튼 클릭
3. 확인 다이얼로그 표시
4. **ProgressBar로 삭제 진행 상태 표시**:
   - "Copying to quarantine: folder1" → 25%
   - "Deleting original: folder1" → 50%
   - ...
5. 완료 후 ProgressBar 숨김

### 4.3 새로고침 작업 (Refresh)

1. 사용자가 "Refresh" 버튼 클릭
2. **ProgressBar로 전체 스캔 진행률 표시**
3. 모니터링 중단 → 파일 검색 → 그룹 재구성 → 모니터링 재개
4. 완료 후 ProgressBar 숨김

---

## 5. 기술적 세부사항

### 5.1 ProgressBar 가시성 제어

**BoolToVisibilityConverter 동작**:
```csharp
public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
{
    if (value is bool boolValue)
    {
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }
    return Visibility.Collapsed;
}
```

**효과**:
- `IsOperationInProgress == true` → `Visibility.Visible`
- `IsOperationInProgress == false` → `Visibility.Collapsed`

### 5.2 Progress 리포팅 패턴

**IProgress<OperationProgress> 인터페이스**:
```csharp
var progress = new Progress<OperationProgress>(p =>
{
    // UI 스레드에서 자동 실행됨 (SynchronizationContext)
    ProgressValue = (double)p.ProcessedFiles / p.TotalFiles * 100;
});
```

**OperationProgress 구조**:
```csharp
public class OperationProgress
{
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public string CurrentFile { get; set; }
    public string Status { get; set; }  // "Copying", "Cleaning up", etc.
}
```

### 5.3 TabControl과 DataGrid 연동

**ActiveTabIndex 기반 선택 항목 조회**:
```csharp
private IEnumerable<FileGroupViewModel> GetSelectedGroups()
{
    var groups = ActiveTabIndex switch
    {
        0 => Line1Groups,
        1 => Line2Groups,
        _ => Enumerable.Empty<FileGroupViewModel>()
    };
    return groups.Where(g => g.IsSelected);
}
```

**사용 예시** (ExecuteMoveAsync):
```csharp
var selectedGroups = GetSelectedGroups().ToList();
if (selectedGroups.Count == 0)
{
    AddLogMessage(LogSeverity.Warning, "FileOperation", "No groups selected");
    return;
}

foreach (var groupVm in selectedGroups)
{
    // Move operation...
}
```

---

## 6. 빌드 결과

```
✅ 빌드 성공: 경고 31개, 오류 0개
경과 시간: 7.82초
```

**경고**: 모든 경고는 기존 코드에서 발생하는 것으로 Task 4.3 변경사항과 무관

---

## 7. 테스트 체크리스트

### 7.1 ProgressBar 기능

- [ ] Move 작업 중 ProgressBar 표시 확인
- [ ] Delete 작업 중 ProgressBar 표시 확인
- [ ] Refresh 작업 중 ProgressBar 표시 확인
- [ ] 작업 완료 후 ProgressBar 자동 숨김 확인
- [ ] 진행률 퍼센트 텍스트 정확성 확인 (0-100%)

### 7.2 TabControl 연동

- [ ] ViewModel에서 `ActiveTabIndex` 변경 시 탭 자동 전환
- [ ] 사용자가 탭 클릭 시 `ActiveTabIndex` 업데이트 확인
- [ ] `GetSelectedGroups()`가 현재 탭 기준 선택 항목 반환 확인

### 7.3 DataGrid SelectedItem 바인딩

- [ ] Line 1 탭에서 행 선택 시 `SelectedLine1Group` 업데이트
- [ ] Line 2 탭에서 행 선택 시 `SelectedLine2Group` 업데이트
- [ ] `SelectedGroup` 계산 속성이 현재 탭 기준 반환

### 7.4 IsSelected 바인딩

- [ ] 체크박스 클릭 시 `IsSelected` 속성 업데이트
- [ ] DragSelectBehavior로 선택 시 체크박스 자동 체크
- [ ] ViewModel에서 `IsSelected` 변경 시 UI 반영

---

## 8. 알려진 제한사항

### 8.1 ProgressBar 정확도

**현재 구현**: 파일 단위 진행률 (Normal 폴더는 1개로 집계)

**제한사항**: 대용량 Normal 폴더가 있을 경우 진행률이 오래 동안 한 단계에 머물 수 있음

**개선 방안** (선택적):
- 폴더 내 파일 수를 사전 계산하여 더 세밀한 진행률 제공
- 폴더 복사 중 내부 파일 진행률도 리포팅

### 8.2 취소 기능 UI

**현재 상태**: `CancellationToken`은 백엔드에서 지원하지만, UI에 "Cancel" 버튼 없음

**향후 개선**:
- StatusBar에 "Cancel" 버튼 추가
- `IsOperationInProgress == true`일 때만 표시
- 클릭 시 `CancelCurrentOperation()` 호출

---

## 9. 관련 파일

| 파일 경로 | 역할 | 변경 여부 |
|----------|------|----------|
| [ChronoView/MainWindow.xaml](../../ChronoView/MainWindow.xaml) | ✅ 수정 | ProgressBar 추가, BoolToVisibilityConverter 등록 |
| [ChronoView/Converters/BoolToVisibilityConverter.cs](../../ChronoView/Converters/BoolToVisibilityConverter.cs) | Converter 구현 | 변경 없음 (기존 존재) |
| [ChronoView/UI/ViewModels/MainWindowViewModel.cs](../../ChronoView/UI/ViewModels/MainWindowViewModel.cs) | ViewModel | 변경 없음 (Phase 1에서 구현됨) |
| [docs/spec/ui-service-integration-gaps/tasks.md](../tasks.md) | ✅ 수정 | Task 4.3 완료로 표시 |

---

## 10. 다음 단계

Task 4.4: Line2 탭 구현

**주요 작업**:
- Line2 탭 DataGrid는 이미 구현 완료 (Task 4.1에서 DragSelectBehavior 연결됨)
- 확인 필요: Line2 탭의 컬럼 구조가 Line1과 동일한지 검증

Task 4.5: Combined 탭 구현 (우선순위 낮음)

**주요 작업**:
- Grid 레이아웃으로 Line1/Line2 동시 표시
- GridSplitter로 크기 조절 가능하게 구현

---

## 11. 변경 이력

| 날짜 | 작업자 | 변경 내용 |
|-----|--------|----------|
| 2025-12-11 | Claude Code | Task 4.3 구현 완료 (StatusBar ProgressBar 추가) |
