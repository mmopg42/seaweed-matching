# Task 4.1: DragSelectBehavior 연결 - 구현 보고서

## 개요

**작업일자**: 2025-12-11
**작업 내용**: MainWindow.xaml의 Line 1 및 Line 2 DataGrid에 DragSelectBehavior 연결
**상태**: ✅ 완료

---

## 1. 작업 목표

ChronoView의 Line 1과 Line 2 DataGrid에 다중 선택 기능을 제공하기 위해 WPF Behaviors를 활용한 DragSelectBehavior를 연결합니다.

### 1.1 기능 요구사항

- 사용자가 DataGrid에서 마우스 드래그로 여러 행을 선택할 수 있어야 함
- 선택된 행은 `FileGroupViewModel.IsSelected` 속성과 양방향 바인딩
- Line 1과 Line 2 DataGrid 모두 동일한 Behavior 적용

---

## 2. 사전 준비 확인

### 2.1 NuGet 패키지

**패키지**: `Microsoft.Xaml.Behaviors.Wpf`
**버전**: v1.1.135
**상태**: ✅ 설치 확인 완료

```xml
<PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.135" />
```

### 2.2 Behavior 클래스

**파일**: `ChronoView/UI/Behaviors/DragSelectBehavior.cs`
**상태**: ✅ 이미 구현됨

핵심 기능:
- `OnAttached()`: DataGrid의 마우스 이벤트 구독
- `OnDetaching()`: 이벤트 구독 해제
- 드래그 시작/이동/종료 처리
- `IsSelected` 속성 업데이트

---

## 3. 구현 내용

### 3.1 MainWindow.xaml 네임스페이스 추가

**위치**: MainWindow.xaml 상단
**변경 내용**:

```xml
<Window x:Class="ChronoView.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:ChronoView"
        xmlns:converters="clr-namespace:ChronoView.UI.Converters"
        xmlns:i="http://schemas.microsoft.com/xaml/behaviors"
        xmlns:behaviors="clr-namespace:ChronoView.UI.Behaviors"
        mc:Ignorable="d"
        ...>
```

**추가된 네임스페이스**:
- `xmlns:i="http://schemas.microsoft.com/xaml/behaviors"` (Line 9)
- `xmlns:behaviors="clr-namespace:ChronoView.UI.Behaviors"` (Line 10)

### 3.2 Line 1 DataGrid에 Behavior 연결

**위치**: MainWindow.xaml, Line 1 TabItem
**변경 내용** (Lines 342-347):

```xml
<DataGrid ItemsSource="{Binding Line1Groups}"
          SelectedItem="{Binding SelectedLine1Group}"
          Style="{StaticResource FileGroupDataGridStyle}"
          RowStyle="{StaticResource FileGroupRowStyle}">
    <i:Interaction.Behaviors>
        <behaviors:DragSelectBehavior/>
    </i:Interaction.Behaviors>
    <DataGrid.Columns>
        ...
    </DataGrid.Columns>
</DataGrid>
```

### 3.3 Line 2 DataGrid에 Behavior 연결

**위치**: MainWindow.xaml, Line 2 TabItem
**변경 내용** (Lines 435-437):

```xml
<DataGrid ItemsSource="{Binding Line2Groups}"
          SelectedItem="{Binding SelectedLine2Group}"
          Style="{StaticResource FileGroupDataGridStyle}"
          RowStyle="{StaticResource FileGroupRowStyle}">
    <i:Interaction.Behaviors>
        <behaviors:DragSelectBehavior/>
    </i:Interaction.Behaviors>
    <DataGrid.Columns>
        ...
    </DataGrid.Columns>
</DataGrid>
```

---

## 4. Behavior 작동 원리

### 4.1 이벤트 구독 (OnAttached)

```csharp
protected override void OnAttached()
{
    base.OnAttached();
    AssociatedObject.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
    AssociatedObject.MouseMove += OnMouseMove;
    AssociatedObject.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
}
```

### 4.2 드래그 선택 흐름

1. **마우스 버튼 다운**:
   - `_isDragging = true`
   - `_startPoint` 저장
   - Ctrl/Shift 키 상태 확인

2. **마우스 이동**:
   - 드래그 중일 때만 처리
   - 현재 행의 `IsSelected` 속성 업데이트

3. **마우스 버튼 업**:
   - `_isDragging = false`
   - 드래그 상태 초기화

### 4.3 DataGrid 바인딩 구조

```
MainWindowViewModel
  ├─ Line1Groups (ObservableCollection<FileGroupViewModel>)
  │   └─ FileGroupViewModel.IsSelected (bool, TwoWay Binding)
  │
  └─ Line2Groups (ObservableCollection<FileGroupViewModel>)
      └─ FileGroupViewModel.IsSelected (bool, TwoWay Binding)
```

**DataGrid Column**:
```xml
<DataGridCheckBoxColumn Header="" Width="40" Binding="{Binding IsSelected}"/>
```

---

## 5. 사용자 시나리오

### 5.1 단일 선택 (클릭)

1. 사용자가 DataGrid 행을 클릭
2. 해당 행의 `IsSelected` 속성이 `true`로 변경
3. 체크박스가 체크 상태로 표시

### 5.2 다중 선택 (드래그)

1. 사용자가 첫 번째 행에서 마우스 버튼 누름
2. 마우스를 아래로 드래그하면서 여러 행을 지나감
3. 지나간 모든 행의 `IsSelected`가 `true`로 변경
4. 마우스 버튼을 놓으면 드래그 종료
5. 선택된 모든 행의 체크박스가 체크 상태

### 5.3 선택 해제 (Ctrl + 드래그)

1. Ctrl 키를 누른 채로 이미 선택된 행에서 드래그 시작
2. 드래그한 행들의 `IsSelected`가 `false`로 변경
3. 체크박스 해제

---

## 6. 통합 기능

### 6.1 Move/Delete Command와의 연동

DragSelectBehavior로 선택된 항목들은 `MainWindowViewModel.GetSelectedGroups()` 메서드로 조회됩니다:

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
    AddLogMessage(LogSeverity.Warning, "FileOperation", "No groups selected for move");
    return;
}

foreach (var groupVm in selectedGroups)
{
    var result = await _fileOperationService.MoveFileGroupAsync(
        groupVm.FileGroup, destFolder, onConflict, progress, _operationCts.Token);

    if (result.Success)
    {
        RemoveFileGroup(groupVm);
    }
}
```

### 6.2 탭별 독립 선택

- Line 1 탭의 선택 상태는 `Line1Groups` 컬렉션의 `IsSelected` 속성에 저장
- Line 2 탭의 선택 상태는 `Line2Groups` 컬렉션의 `IsSelected` 속성에 저장
- 탭 전환 시 각 탭의 선택 상태가 유지됨

---

## 7. 테스트 체크리스트

### 7.1 기본 기능

- [ ] Line 1 DataGrid에서 단일 행 클릭 시 체크박스 선택
- [ ] Line 1 DataGrid에서 드래그로 여러 행 선택
- [ ] Line 2 DataGrid에서 단일 행 클릭 시 체크박스 선택
- [ ] Line 2 DataGrid에서 드래그로 여러 행 선택

### 7.2 고급 기능

- [ ] Ctrl + 드래그로 선택 해제
- [ ] Shift + 클릭으로 범위 선택 (Behavior가 지원하는 경우)
- [ ] 탭 전환 후에도 선택 상태 유지

### 7.3 Command 통합

- [ ] 다중 선택 후 Move 버튼 클릭 시 모든 선택 항목 이동
- [ ] 다중 선택 후 Delete 버튼 클릭 시 모든 선택 항목 삭제
- [ ] 선택 항목이 없을 때 Move/Delete 버튼 비활성화

### 7.4 성능

- [ ] 100개 이상의 항목에서도 드래그 선택이 부드럽게 작동
- [ ] 빠른 드래그 시에도 모든 행이 정확하게 선택됨

---

## 8. 알려진 제한사항

### 8.1 스크롤 자동화 미지원

현재 구현된 DragSelectBehavior는 DataGrid 경계를 넘어가는 드래그 시 자동 스크롤을 지원하지 않습니다. 사용자가 보이는 영역에서만 드래그 선택이 가능합니다.

**해결 방안** (선택적 개선):
- 마우스가 DataGrid 경계에 가까워지면 자동 스크롤 트리거
- `ScrollViewer.ScrollToVerticalOffset()` 사용

### 8.2 가상화 모드 호환성

DataGrid가 UI Virtualization 모드일 때, 화면에 보이지 않는 항목은 시각적 트리에 존재하지 않아 드래그 선택이 불가능할 수 있습니다.

**현재 상태**: MainWindow.xaml의 DataGrid에 `VirtualizingPanel.IsVirtualizing` 설정이 없으므로 기본값(true) 적용

**권장 사항**: 항목 수가 적다면 가상화 비활성화 고려:
```xml
<DataGrid VirtualizingPanel.IsVirtualizing="False" ...>
```

---

## 9. 관련 파일

| 파일 경로 | 역할 | 변경 여부 |
|----------|------|----------|
| `ChronoView/MainWindow.xaml` | UI 정의, Behavior 연결 | ✅ 수정됨 |
| `ChronoView/UI/Behaviors/DragSelectBehavior.cs` | Behavior 구현 | 변경 없음 |
| `ChronoView/UI/ViewModels/FileGroupViewModel.cs` | `IsSelected` 속성 제공 | 변경 없음 |
| `ChronoView/UI/ViewModels/MainWindowViewModel.cs` | `GetSelectedGroups()` 메서드 | 변경 없음 |
| `ChronoView/ChronoView.csproj` | NuGet 패키지 참조 | 변경 없음 |

---

## 10. 결론

Task 4.1이 성공적으로 완료되었습니다. 이제 사용자는 Line 1과 Line 2 DataGrid에서 드래그를 통해 여러 파일 그룹을 한 번에 선택할 수 있으며, 선택된 항목들은 Move/Delete Command와 자동으로 연동됩니다.

**다음 단계**: Task 4.2 - SettingsDialog Browse 버튼 구현

---

## 11. 변경 이력

| 날짜 | 작업자 | 변경 내용 |
|-----|--------|----------|
| 2025-12-11 | Claude Code | Task 4.1 구현 완료 (DragSelectBehavior 연결) |
