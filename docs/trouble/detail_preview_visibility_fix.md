# Detail Preview Display Fix

## Problem
Detail Preview 패널이 더블클릭 시 나타나지 않는 문제

**증상**:
- 더블클릭 이벤트 정상 실행 (로그 확인)
- `ExecuteOpenDetailView` 커맨드 정상 실행 (로그 확인)
- `DetailPreviewViewModel.IsVisible = true` 설정됨 (로그 확인)
- 하지만 UI에 아무것도 표시되지 않음

## Root Cause
`MainWindow.xaml`의 Grid 레이아웃 문제:

```xml
<Grid.RowDefinitions>
    <RowDefinition Height="*"/>       <!-- Row 0: TabControl -->
    <RowDefinition Height="Auto"/>    <!-- Row 1: DetailPreviewControl ← 문제! -->
    <RowDefinition Height="150"/>     <!-- Row 2: Message Log -->
</Grid.RowDefinitions>
```

**Why Auto Height Fails:**
1. 초기 상태: `DetailPreviewControl`의 `IsVisible=false`
2. Border의 Visibility는 `Collapsed`
3. Grid Row의 `Height="Auto"`는 자식 요소 크기에 맞춤
4. Collapsed 요소는 크기가 0 → Row 높이 = 0
5. `IsVisible=true`로 변경되어도 WPF가 Row 높이를 재계산하지 않음
6. 결과: 공간이 없어서 보이지 않음

## Solution
Row 높이를 고정값으로 변경:

```xml
<RowDefinition Height="250"/>
```

**Why This Works:**
- Row가 항상 250px 높이 확보
- `IsVisible=false`일 때 Border가 Collapsed되어 공간은 비어있음
- `IsVisible=true`가 되면 Border가 즉시 표시됨

## Alternative Solutions

### Option 1: MinHeight on Control (Not Recommended)
```xml
<controls:DetailPreviewControl Grid.Row="1" MinHeight="250" .../>
```
- Auto height와 함께 사용 가능
- 하지만 여전히 레이아웃 재계산 이슈 가능성

### Option 2: Trigger-based Layout (Complex)
- Style Trigger로 IsVisible 변경 시 Row 높이 변경
- 너무 복잡하고 유지보수 어려움

### Option 3: Fixed Height (Selected) ✅
- 가장 단순하고 확실한 방법
- Detail Preview는 고정 높이가 적합

## Files Modified
- [`MainWindow.xaml`](file:///c:/workspace/seaweed/gui_kiro/ChronoView/MainWindow.xaml#L360) - Changed Row 1 height from Auto to 250

## Testing
1. Run application
2. Click Start
3. Double-click any DataGrid row
4. **Expected**: Detail Preview panel appears at bottom
5. Click X button on preview
6. **Expected**: Panel disappears
