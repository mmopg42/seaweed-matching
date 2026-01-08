---
Task: inspector_drawer
Created: 2024-12-19
Status: Draft
Depends On: 01_requirements.md
---

# Inspector Drawer - Implementation Plan

## 1. Overview

로그 패널 바로 위에 위치하는 접히는 드로어 패널을 구현합니다. 사용자가 파일 그룹 행을 클릭하면 해당 그룹의 상세 정보를 크게 볼 수 있습니다.

## 2. Architecture

### 2.1 Component Structure

```
MainWindow.xaml
├── Grid (Main Layout)
    ├── Row 0: TabControl (기존)
    ├── Row 1: GridSplitter (기존)
    ├── Row 2: InspectorDrawer (NEW) ← 추가
    ├── Row 3: GridSplitter (NEW) ← 추가 (드로어 리사이즈용)
    └── Row 4: LogPanel (기존)
```

### 2.2 Components

#### 2.2.1 InspectorDrawer UserControl

**Location**: `ChronoView/UI/Controls/InspectorDrawer.xaml`

**Responsibilities**:
- 드로어 UI 렌더링
- 이미지 프리뷰 표시
- 메타데이터 표시
- 고정/닫기 버튼 처리
- 높이 리사이즈 UI 제공

**Properties**:
- `SelectedGroup`: 현재 선택된 FileGroup 객체
- `IsPinned`: 고정 상태 (bool)
- `IsExpanded`: 펼침 상태 (bool)
- `DrawerHeight`: 드로어 높이 (double, 160~260px)

#### 2.2.2 InspectorDrawerViewModel

**Location**: `ChronoView/UI/ViewModels/InspectorDrawerViewModel.cs`

**Responsibilities**:
- 선택된 그룹 데이터 관리
- 이미지 로딩 상태 관리
- 고정/펼침 상태 관리
- 높이 조절 로직

**Properties**:
- `SelectedGroup`: FileGroup (nullable)
- `IsPinned`: bool
- `IsExpanded`: bool
- `DrawerHeight`: double
- `NirImage`: BitmapImage
- `GeneralCameraImage`: BitmapImage
- `Camera1Image`: BitmapImage
- `Camera2Image`: BitmapImage
- `Camera3Image`: BitmapImage
- `NirFileName`: string
- `NirSpcPath`: string
- `NirTxtPath`: string
- `GeneralCameraFileName`: string
- `GeneralCameraImageSize`: string
- `Camera1FileName`: string
- `Camera2FileName`: string
- `Camera3FileName`: string
- `GroupId`: string
- `GroupStatus`: string

**Commands**:
- `PinCommand`: 고정 토글
- `CloseCommand`: 드로어 닫기

#### 2.2.3 MainWindowViewModel Integration

**Modifications**:
- `SelectedGroup` 속성 추가 (FileGroup, nullable)
- `InspectorDrawerViewModel` 인스턴스 추가
- 그룹 행 클릭 시 `SelectedGroup` 업데이트
- `InspectorDrawerViewModel`에 선택된 그룹 전달

## 3. UI Layout

### 3.1 InspectorDrawer Layout

```
┌─────────────────────────────────────────────────────────────┐
│ ▼ 상세 프리뷰  group_008  ✓완료  [고정]              [x] │ ← 헤더
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  [NIR 그래프 크게]  [일반카메라 크게]  [Cam1 크게]           │
│  [Cam2 크게]  [Cam3 크게]                                    │
│                                                               │
│  [NIR 파일명]      [일반카메라 파일명]  [Cam1 파일명]         │
│  [spc]             [이미지 크기]        [Cam2 파일명]         │
│  [txt]                              [Cam3 파일명]             │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 Visual Specifications

**배경색**: `#F5F5F5` 또는 `#F7F7F7`

**보더**: `BorderThickness="0,1,0,1"`, `BorderBrush="#E0E0E0"`

**폰트 크기**:
- 헤더: 12~13px
- 값: 13~14px
- Group ID: 14~16px Bold

**이미지 프리뷰**:
- 가로로 넓은 비율 (예: 16:9 또는 4:3)
- 높이는 드로어 높이의 60~70% 정도
- 각 이미지는 최소 200px 너비

**레이아웃**:
- 헤더: Horizontal StackPanel (왼쪽: 제목/상태, 오른쪽: 버튼)
- 이미지 영역: UniformGrid 또는 WrapPanel (가로 배치)
- 파일명 영역: 각 이미지 아래 TextBlock

## 4. Data Flow

### 4.1 Group Selection Flow

```
1. User clicks DataGrid row
   ↓
2. MainWindowViewModel.SelectedGroup = clicked group
   ↓
3. InspectorDrawerViewModel.SelectedGroup = MainWindowViewModel.SelectedGroup
   ↓
4. InspectorDrawerViewModel loads images (async)
   ↓
5. InspectorDrawer UI updates
   ↓
6. InspectorDrawer.IsExpanded = true (if not pinned to different group)
```

### 4.2 Image Loading Flow

```
1. InspectorDrawerViewModel.SelectedGroup changed
   ↓
2. For each image type (NIR, GeneralCamera, Cam1-3):
   a. Get file path from SelectedGroup
   b. Request image from ImageManager
   c. Update corresponding Image property
   ↓
3. UI binds to Image properties and displays
```

## 5. State Management

### 5.1 Drawer States

```
CLOSED: IsExpanded = false, SelectedGroup = null
OPENED: IsExpanded = true, SelectedGroup != null
PINNED: IsPinned = true, IsExpanded = true
```

### 5.2 State Transitions

```
CLOSED ──[row click]──> OPENED
OPENED ──[pin click]──> PINNED
OPENED ──[x click]──> CLOSED
PINNED ──[row click]──> PINNED (stays, updates SelectedGroup)
PINNED ──[pin click]──> OPENED
PINNED ──[x click]──> CLOSED
```

## 6. Resize Behavior

### 6.1 Resize Handle

- 드로어 상단에 5px 높이의 리사이즈 핸들 영역
- 마우스 커서가 `Cursors.SizeNS`로 변경됨
- 드래그 시 `DrawerHeight` 업데이트
- 최소 높이: 160px
- 최대 높이: 260px

### 6.2 Implementation

```csharp
// MouseDown on resize handle
private void ResizeHandle_MouseDown(object sender, MouseButtonEventArgs e)
{
    _isResizing = true;
    _resizeStartY = e.GetPosition(this).Y;
    _resizeStartHeight = DrawerHeight;
    CaptureMouse();
}

// MouseMove during resize
private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
{
    if (_isResizing)
    {
        double delta = _resizeStartY - e.GetPosition(this).Y;
        double newHeight = _resizeStartHeight + delta;
        DrawerHeight = Math.Clamp(newHeight, 160, 260);
    }
}

// MouseUp
private void ResizeHandle_MouseUp(object sender, MouseButtonEventArgs e)
{
    _isResizing = false;
    ReleaseMouseCapture();
}
```

## 7. Integration Points

### 7.1 MainWindow.xaml Changes

**Grid RowDefinitions 수정**:
```xml
<Grid.RowDefinitions>
    <RowDefinition Height="*"/>           <!-- TabControl -->
    <RowDefinition Height="5"/>           <!-- GridSplitter -->
    <RowDefinition Height="Auto" MinHeight="0" MaxHeight="260"/>  <!-- InspectorDrawer -->
    <RowDefinition Height="5"/>           <!-- Resize Splitter -->
    <RowDefinition Height="150" MinHeight="50"/>  <!-- LogPanel -->
</Grid.RowDefinitions>
```

**InspectorDrawer 추가**:
```xml
<controls:InspectorDrawer Grid.Row="2" 
                          DataContext="{Binding InspectorDrawerViewModel}"
                          Visibility="{Binding IsExpanded, Converter={StaticResource BooleanToVisibilityConverter}}"/>
```

### 7.2 MainWindowViewModel Changes

**Properties 추가**:
```csharp
public InspectorDrawerViewModel InspectorDrawerViewModel { get; }
public FileGroup? SelectedGroup 
{ 
    get => _selectedGroup;
    set 
    {
        SetProperty(ref _selectedGroup, value);
        if (!InspectorDrawerViewModel.IsPinned || InspectorDrawerViewModel.SelectedGroup != value)
        {
            InspectorDrawerViewModel.SelectedGroup = value;
        }
    }
}
```

**DataGrid SelectionChanged 이벤트**:
```csharp
private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (sender is DataGrid dg && dg.SelectedItem is FileGroup group)
    {
        SelectedGroup = group;
    }
}
```

## 8. Error Handling

| Scenario | Handling |
|----------|----------|
| 이미지 파일이 없음 | 빈 이미지 영역 표시 또는 "이미지 없음" 텍스트 |
| 이미지 로딩 실패 | 에러 아이콘 또는 "로딩 실패" 메시지 |
| 그룹 데이터가 null | 드로어 닫기 |
| 리사이즈 중 드래그 범위 초과 | 최소/최대 높이로 클램프 |

## 9. Performance Considerations

- 이미지 로딩은 비동기로 처리
- 이미지 캐싱은 기존 ImageManager 활용
- 드로어가 닫혀있을 때는 이미지 로딩하지 않음
- 선택된 그룹이 변경될 때만 이미지 재로딩

---

## Approval

- [ ] Plan reviewed and approved
- [ ] All components identified
- [ ] Integration points clear
- [ ] State management defined
- [ ] Error handling considered

**Next Step**: 04_design.md




