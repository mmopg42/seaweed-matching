# Task 4.3 Implementation Summary: MainWindow.xaml Modifications

## Overview
Task 4.3에서는 `MainWindow.xaml`을 수정하여 **사용자 피드백(진행률 표시)**과 **데이터 상호작용(양방향 선택 바인딩)**을 개선했습니다.

## Key Changes

### 1. StatusBar Enhancements
- **ProgressBar Added**: 하단 StatusBar에 진행률 표시줄을 추가하여 장기 실행 작업(예: Auto Config, Refresh)의 상태를 시각적으로 전달합니다.
- **Dynamic Visibility**: `IsOperationInProgress` 속성과 연결되어, 작업 중일 때만 표시되도록 설정했습니다.
```xml
<ProgressBar Value="{Binding ProgressValue}" 
             Visibility="{Binding IsOperationInProgress, Converter={StaticResource BooleanToVisibilityConverter}}" .../>
```

### 2. DataGrid Interaction
- **Two-Way Selection Binding**: 모든 DataGrid(Line 1, Line 2)가 공유하는 `FileGroupRowStyle`에 `IsSelected` 속성을 `TwoWay` 모드로 바인딩했습니다.
```xml
<Style x:Key="FileGroupRowStyle" TargetType="DataGridRow">
    <Setter Property="IsSelected" Value="{Binding IsSelected, Mode=TwoWay}"/>
    <!-- ... -->
</Style>
```
- **Effect**: UI에서 행을 선택하면 ViewModel의 `IsSelected` 속성이 업데이트되고, 반대로 ViewModel에서 상태를 변경하면 UI에 반영됩니다. 이는 "Move"나 "Delete" 같은 일괄 작업 시 정확한 대상 선택을 보장합니다.

## Verification
- **Build**: Successful
- **XAML Check**: ProgressBar와 Style Setter가 올바른 위치에 삽입되었음을 확인했습니다.

## Next Steps
- **Task 4.4**: Line 2 탭 구현 및 검증 (이미 UI 틀은 존재하므로 동작 검증 위주)
