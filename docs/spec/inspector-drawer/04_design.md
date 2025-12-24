---
Task: inspector_drawer
Created: 2024-12-19
Status: Draft
Depends On: 03_plan.md
---

# Inspector Drawer - Detailed Design

## 1. Component Designs

### 1.1 InspectorDrawerViewModel

> ViewModel for managing drawer state and data.

#### Interface (from Plan)

```csharp
public class InspectorDrawerViewModel : ViewModelBase
{
    public FileGroup? SelectedGroup { get; set; }
    public bool IsPinned { get; set; }
    public bool IsExpanded { get; set; }
    public double DrawerHeight { get; set; }
    public ICommand PinCommand { get; }
    public ICommand CloseCommand { get; }
    // ... image properties
}
```

#### Preconditions

- MainWindowViewModel에서 초기화됨
- ImageManager 서비스가 DI 컨테이너에 등록되어 있음

#### Postconditions

- SelectedGroup이 설정되면 이미지 로딩이 시작됨
- IsExpanded가 true이면 UI가 표시됨
- CloseCommand 실행 시 IsExpanded = false

#### Detailed Logic

```pseudo
class InspectorDrawerViewModel:
    // ========== PROPERTIES ==========
    private FileGroup? _selectedGroup = null
    private bool _isPinned = false
    private bool _isExpanded = false
    private double _drawerHeight = 200.0
    private BitmapImage? _nirImage = null
    private BitmapImage? _generalCameraImage = null
    // ... other image properties
    
    // ========== SELECTED GROUP SETTER ==========
    function set SelectedGroup(value: FileGroup?):
        if _selectedGroup == value:
            return  // No change
        
        _selectedGroup = value
        
        if value is null:
            IsExpanded = false
            ClearAllImages()
        else:
            if not IsPinned or _previousGroup != value:
                IsExpanded = true
                LoadImagesAsync(value)
        
        _previousGroup = value
        NotifyPropertyChanged("SelectedGroup")
    
    // ========== IMAGE LOADING ==========
    async function LoadImagesAsync(group: FileGroup):
        try:
            // Load NIR image
            if group.NirFilePath is not null and File.Exists(group.NirFilePath):
                nirImage = await ImageManager.LoadImageAsync(group.NirFilePath)
                NirImage = nirImage
            else:
                NirImage = null
            
            // Load General Camera image
            if group.GeneralCameraFilePath is not null and File.Exists(group.GeneralCameraFilePath):
                generalImage = await ImageManager.LoadImageAsync(group.GeneralCameraFilePath)
                GeneralCameraImage = generalImage
                GeneralCameraImageSize = GetImageSize(generalImage)
            else:
                GeneralCameraImage = null
                GeneralCameraImageSize = ""
            
            // Load Camera1-3 images
            LoadCameraImageAsync(group.Camera1FilePath, "Camera1")
            LoadCameraImageAsync(group.Camera2FilePath, "Camera2")
            LoadCameraImageAsync(group.Camera3FilePath, "Camera3")
            
            // Update file names
            NirFileName = Path.GetFileName(group.NirFilePath) ?? ""
            NirSpcPath = group.NirSpcPath ?? ""
            NirTxtPath = group.NirTxtPath ?? ""
            GeneralCameraFileName = Path.GetFileName(group.GeneralCameraFilePath) ?? ""
            Camera1FileName = Path.GetFileName(group.Camera1FilePath) ?? ""
            Camera2FileName = Path.GetFileName(group.Camera2FilePath) ?? ""
            Camera3FileName = Path.GetFileName(group.Camera3FilePath) ?? ""
            
            // Update group info
            GroupId = group.GroupId
            GroupStatus = group.IsComplete ? "✓완료" : "진행중"
            
        catch error:
            LogError("Failed to load images for group {GroupId}: {Error}", group.GroupId, error)
            // Set images to null on error
    
    // ========== PIN COMMAND ==========
    function PinCommand_Execute():
        IsPinned = not IsPinned
        LogInfo("Drawer pinned: {IsPinned}", IsPinned)
    
    // ========== CLOSE COMMAND ==========
    function CloseCommand_Execute():
        if not IsPinned:
            IsExpanded = false
            SelectedGroup = null
        else:
            // If pinned, just unpin and close
            IsPinned = false
            IsExpanded = false
            SelectedGroup = null
    
    // ========== CLEAR IMAGES ==========
    function ClearAllImages():
        NirImage = null
        GeneralCameraImage = null
        Camera1Image = null
        Camera2Image = null
        Camera3Image = null
        // Clear file names
        NirFileName = ""
        GeneralCameraFileName = ""
        // ... clear all file name properties
```

#### State Variables

| Variable | Type | Initial | Purpose |
|----------|------|---------|---------|
| `_selectedGroup` | FileGroup? | null | Currently selected group |
| `_isPinned` | bool | false | Whether drawer is pinned |
| `_isExpanded` | bool | false | Whether drawer is visible |
| `_drawerHeight` | double | 200.0 | Current drawer height (160-260px) |
| `_previousGroup` | FileGroup? | null | Previous group for pin logic |

#### State Transitions

```
CLOSED (IsExpanded=false, SelectedGroup=null)
  └─[row click]─> OPENED (IsExpanded=true, SelectedGroup=group)
       │
       ├─[pin click]─> PINNED (IsPinned=true)
       │    │
       │    ├─[row click]─> PINNED (updates SelectedGroup, stays pinned)
       │    └─[pin click]─> OPENED (IsPinned=false)
       │
       └─[x click]─> CLOSED
```

#### Thread Safety

- UI 스레드에서만 접근 (WPF 바인딩)
- 이미지 로딩은 비동기이지만 UI 업데이트는 Dispatcher를 통해 수행

#### Error Handling

| Error | Detection | Handling | Recovery |
|-------|-----------|----------|----------|
| 이미지 파일 없음 | File.Exists() == false | Image property = null | UI에 "이미지 없음" 표시 |
| 이미지 로딩 실패 | ImageManager throws | Catch, log, Image = null | UI에 에러 아이콘 표시 |
| 그룹 데이터 null | SelectedGroup == null | IsExpanded = false | 드로어 닫힘 |

---

### 1.2 InspectorDrawer UserControl

> XAML UserControl for drawer UI rendering.

#### Interface (from Plan)

```xml
<UserControl x:Class="ChronoView.UI.Controls.InspectorDrawer"
             DataContext="{Binding InspectorDrawerViewModel}">
    <!-- Drawer content -->
</UserControl>
```

#### Preconditions

- DataContext가 InspectorDrawerViewModel로 설정됨
- MainWindow.xaml의 Grid Row 2에 배치됨

#### Postconditions

- IsExpanded가 true이면 Visibility.Visible
- IsExpanded가 false이면 Visibility.Collapsed
- 이미지가 로드되면 Image 컨트롤에 표시됨

#### Detailed Layout Structure

```xml
<UserControl>
    <Border Background="#F5F5F5" 
            BorderBrush="#E0E0E0" 
            BorderThickness="0,1,0,1">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>  <!-- Resize handle -->
                <RowDefinition Height="Auto"/>  <!-- Header -->
                <RowDefinition Height="*"/>      <!-- Content -->
            </Grid.RowDefinitions>
            
            <!-- Resize Handle -->
            <Border Grid.Row="0" 
                    Height="5" 
                    Background="Transparent"
                    Cursor="SizeNS"
                    MouseDown="ResizeHandle_MouseDown"
                    MouseMove="ResizeHandle_MouseMove"
                    MouseUp="ResizeHandle_MouseUp">
                <Border Background="#D0D0D0" 
                        Height="1" 
                        VerticalAlignment="Center"
                        Margin="20,0"/>
            </Border>
            
            <!-- Header -->
            <Border Grid.Row="1" 
                    Background="Transparent" 
                    Padding="12,8">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    
                    <!-- Left: Title and Status -->
                    <StackPanel Grid.Column="0" Orientation="Horizontal">
                        <TextBlock Text="▼" FontSize="12" VerticalAlignment="Center" Margin="0,0,8,0"/>
                        <TextBlock Text="상세 프리뷰" FontSize="13" VerticalAlignment="Center" Margin="0,0,8,0"/>
                        <TextBlock Text="{Binding GroupId}" 
                                   FontSize="15" 
                                   FontWeight="Bold"
                                   VerticalAlignment="Center" 
                                   Margin="0,0,8,0"/>
                        <TextBlock Text="{Binding GroupStatus}" 
                                   FontSize="13" 
                                   VerticalAlignment="Center"/>
                    </StackPanel>
                    
                    <!-- Right: Buttons -->
                    <StackPanel Grid.Column="1" Orientation="Horizontal">
                        <Button Content="[고정]" 
                                Command="{Binding PinCommand}"
                                Style="{StaticResource ToolbarButtonStyle}"
                                ToolTip="고정: 다른 행을 클릭해도 프리뷰 유지"/>
                        <Button Content="[x]" 
                                Command="{Binding CloseCommand}"
                                Style="{StaticResource ToolbarButtonStyle}"
                                ToolTip="닫기"/>
                    </StackPanel>
                </Grid>
            </Border>
            
            <!-- Content: Images and File Names -->
            <ScrollViewer Grid.Row="2" 
                         VerticalScrollBarVisibility="Auto"
                         HorizontalScrollBarVisibility="Auto"
                         Padding="12,8">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>  <!-- Images -->
                        <RowDefinition Height="Auto"/>  <!-- File Names -->
                    </Grid.RowDefinitions>
                    
                    <!-- Image Row -->
                    <UniformGrid Grid.Row="0" 
                                 Rows="1" 
                                 Columns="5"
                                 Margin="0,0,0,12">
                        <!-- NIR Image -->
                        <Border BorderBrush="#D0D0D0" 
                                BorderThickness="1" 
                                Margin="4"
                                Background="White">
                            <Grid>
                                <Image Source="{Binding NirImage}" 
                                       Stretch="Uniform"
                                       MinWidth="200"
                                       MinHeight="120"/>
                                <TextBlock Text="NIR 그래프" 
                                           FontSize="11"
                                           HorizontalAlignment="Center"
                                           VerticalAlignment="Top"
                                           Background="#80000000"
                                           Foreground="White"
                                           Padding="4,2"
                                           Margin="4"/>
                            </Grid>
                        </Border>
                        
                        <!-- General Camera Image -->
                        <Border BorderBrush="#D0D0D0" 
                                BorderThickness="1" 
                                Margin="4"
                                Background="White">
                            <Grid>
                                <Image Source="{Binding GeneralCameraImage}" 
                                       Stretch="Uniform"
                                       MinWidth="200"
                                       MinHeight="120"/>
                                <TextBlock Text="일반카메라" 
                                           FontSize="11"
                                           HorizontalAlignment="Center"
                                           VerticalAlignment="Top"
                                           Background="#80000000"
                                           Foreground="White"
                                           Padding="4,2"
                                           Margin="4"/>
                            </Grid>
                        </Border>
                        
                        <!-- Camera 1-3 Images (similar structure) -->
                        <!-- ... -->
                    </UniformGrid>
                    
                    <!-- File Name Row -->
                    <Grid Grid.Row="1">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        
                        <!-- NIR File Names (Vertical) -->
                        <StackPanel Grid.Column="0" Margin="4">
                            <TextBlock Text="{Binding NirFileName}" 
                                       FontSize="13"
                                       TextWrapping="Wrap"/>
                            <TextBlock Text="{Binding NirSpcPath, StringFormat='spc: {0}'}" 
                                       FontSize="12"
                                       Foreground="#666666"
                                       Margin="0,2,0,0"/>
                            <TextBlock Text="{Binding NirTxtPath, StringFormat='txt: {0}'}" 
                                       FontSize="12"
                                       Foreground="#666666"
                                       Margin="0,2,0,0"/>
                        </StackPanel>
                        
                        <!-- General Camera File Name -->
                        <StackPanel Grid.Column="1" Margin="4">
                            <TextBlock Text="{Binding GeneralCameraFileName}" 
                                       FontSize="13"
                                       TextWrapping="Wrap"/>
                            <TextBlock Text="{Binding GeneralCameraImageSize, StringFormat='크기: {0}'}" 
                                       FontSize="12"
                                       Foreground="#666666"
                                       Margin="0,2,0,0"/>
                        </StackPanel>
                        
                        <!-- Camera 1-3 File Names (similar) -->
                        <!-- ... -->
                    </Grid>
                </Grid>
            </ScrollViewer>
        </Grid>
    </Border>
</UserControl>
```

#### Code-Behind Logic

```pseudo
class InspectorDrawer:
    private bool _isResizing = false
    private double _resizeStartY = 0
    private double _resizeStartHeight = 0
    
    // ========== RESIZE HANDLE EVENTS ==========
    function ResizeHandle_MouseDown(sender, e: MouseButtonEventArgs):
        _isResizing = true
        _resizeStartY = e.GetPosition(this).Y
        viewModel = DataContext as InspectorDrawerViewModel
        _resizeStartHeight = viewModel.DrawerHeight
        CaptureMouse()
        e.Handled = true
    
    function ResizeHandle_MouseMove(sender, e: MouseEventArgs):
        if _isResizing:
            currentY = e.GetPosition(this).Y
            delta = _resizeStartY - currentY
            viewModel = DataContext as InspectorDrawerViewModel
            newHeight = _resizeStartHeight + delta
            viewModel.DrawerHeight = Math.Clamp(newHeight, 160, 260)
            e.Handled = true
    
    function ResizeHandle_MouseUp(sender, e: MouseButtonEventArgs):
        if _isResizing:
            _isResizing = false
            ReleaseMouseCapture()
            e.Handled = true
```

#### Error Handling

| Error | Detection | Handling | User Sees |
|-------|-----------|----------|-----------|
| Image binding null | Image.Source == null | 빈 Border 표시 | 빈 이미지 영역 |
| ViewModel null | DataContext == null | Visibility.Collapsed | 드로어 숨김 |
| Resize 범위 초과 | Height < 160 or > 260 | Math.Clamp 적용 | 높이 제한됨 |

---

### 1.3 MainWindowViewModel Integration

> Integration with MainWindowViewModel for group selection.

#### Modifications

```pseudo
class MainWindowViewModel:
    // ========== NEW PROPERTIES ==========
    private InspectorDrawerViewModel _inspectorDrawerViewModel
    private FileGroup? _selectedGroup = null
    
    property InspectorDrawerViewModel:
        get: return _inspectorDrawerViewModel
    
    property SelectedGroup:
        get: return _selectedGroup
        set:
            if _selectedGroup == value:
                return
            _selectedGroup = value
            NotifyPropertyChanged("SelectedGroup")
            
            // Update drawer if not pinned or different group
            if not _inspectorDrawerViewModel.IsPinned:
                _inspectorDrawerViewModel.SelectedGroup = value
            else if value != null:
                // If pinned, only update if explicitly requested
                _inspectorDrawerViewModel.SelectedGroup = value
    
    // ========== CONSTRUCTOR MODIFICATION ==========
    function constructor(...):
        // ... existing initialization ...
        _inspectorDrawerViewModel = new InspectorDrawerViewModel(imageManager, logger)
    
    // ========== DATAGRID SELECTION HANDLER ==========
    function DataGrid_SelectionChanged(sender, e: SelectionChangedEventArgs):
        if sender is DataGrid dg:
            if dg.SelectedItem is FileGroup group:
                SelectedGroup = group
            else:
                SelectedGroup = null
```

---

## 2. Integration Points

### 2.1 MainWindow.xaml → InspectorDrawer

#### Call Sequence

```
1. MainWindow.xaml Grid Row 2에 InspectorDrawer 배치
2. DataContext="{Binding InspectorDrawerViewModel}" 바인딩
3. Visibility="{Binding IsExpanded, Converter=BooleanToVisibilityConverter}"
4. Height="{Binding DrawerHeight}"
```

#### Data Contract

```
InspectorDrawerViewModel (from MainWindowViewModel)
  ├─ SelectedGroup: FileGroup?
  ├─ IsExpanded: bool
  ├─ IsPinned: bool
  ├─ DrawerHeight: double (160-260)
  └─ Image properties: BitmapImage?
```

### 2.2 MainWindowViewModel → InspectorDrawerViewModel

#### Call Sequence

```
1. User clicks DataGrid row
2. MainWindowViewModel.SelectedGroup = clicked group
3. InspectorDrawerViewModel.SelectedGroup = MainWindowViewModel.SelectedGroup
4. InspectorDrawerViewModel loads images
5. UI updates via data binding
```

---

## 3. Edge Cases & Boundary Conditions

| Case | Input | Expected Behavior | Implementation |
|------|-------|-------------------|----------------|
| 그룹에 이미지 없음 | FileGroup with null file paths | 빈 이미지 영역 표시 | Image.Source == null 처리 |
| 이미지 로딩 중 | SelectedGroup 변경 중 | 로딩 인디케이터 또는 이전 이미지 유지 | IsLoading 상태 추가 가능 |
| 드로어 높이 최소 | DrawerHeight = 160 | 더 이상 줄어들지 않음 | Math.Clamp(160, 260) |
| 드로어 높이 최대 | DrawerHeight = 260 | 더 이상 늘어나지 않음 | Math.Clamp(160, 260) |
| 고정 상태에서 다른 행 클릭 | IsPinned = true, 새 그룹 선택 | SelectedGroup 업데이트, IsPinned 유지 | 조건부 업데이트 로직 |
| 여러 행 빠르게 클릭 | 연속 클릭 | 마지막 선택된 그룹만 표시 | SelectedGroup setter에서 처리 |

---

## 4. Resource Management

### 4.1 Image Loading

- 비동기 로딩으로 UI 블로킹 방지
- 이미지 캐싱은 ImageManager에 위임
- SelectedGroup이 null이 되면 이미지 해제

### 4.2 Memory Management

- 이전 그룹 이미지는 GC가 처리
- BitmapImage는 WPF가 자동으로 관리
- 큰 이미지는 스케일링하여 표시

---

## 5. Performance Considerations

| Operation | Expected Time | Memory | Notes |
|-----------|---------------|--------|-------|
| 드로어 열기 | < 50ms | - | UI 렌더링만 |
| 이미지 로딩 (캐시 있음) | < 100ms | ~5MB | 5개 이미지 |
| 이미지 로딩 (캐시 없음) | < 500ms | ~5MB | 디스크 I/O |
| 그룹 변경 | < 100ms | - | 이미지 교체 |

### Optimization Notes

- 이미지 로딩은 Task.Run으로 백그라운드 처리
- 이미지 크기는 표시 크기에 맞게 조정
- 드로어가 닫혀있을 때는 이미지 로딩하지 않음

---

## 6. Testing Strategy

### 6.1 Unit Test Cases

| Test Name | Input | Expected | Verifies |
|-----------|-------|----------|----------|
| test_selected_group_updates_drawer | Set SelectedGroup | DrawerViewModel.SelectedGroup updated | Group selection flow |
| test_pin_prevents_auto_close | Pin, then select other group | Drawer stays open with new group | Pin functionality |
| test_close_unpins | Pin, then close | IsPinned = false, IsExpanded = false | Close command |
| test_resize_clamps_height | Resize to 150px | Height = 160px (min) | Resize limits |
| test_image_loading_null_path | Group with null file paths | Images = null, no exception | Error handling |

### 6.2 Integration Test Cases

| Test Name | Setup | Action | Expected |
|-----------|-------|--------|----------|
| test_row_click_opens_drawer | MainWindow with groups | Click DataGrid row | Drawer visible, images load |
| test_resize_persists | Open drawer, resize to 220px | Close and reopen | Height = 220px |
| test_pin_keeps_drawer_open | Pin drawer, click other row | Drawer shows new group, stays open | Pin behavior |

### 6.3 Manual Verification Steps

```
1. Setup: MainWindow 실행, 파일 그룹 데이터 로드
2. Action: DataGrid에서 그룹 행 클릭
3. Verify: 드로어가 펼쳐지고 선택한 그룹의 이미지가 표시됨
   Expected: NIR, 일반카메라, Cam1-3 이미지가 가로로 배치됨

4. Setup: 드로어 열림
5. Action: 드로어 상단 경계선 드래그
6. Verify: 드로어 높이가 160~260px 범위에서 변경됨
   Expected: 높이 조절 가능, 범위 제한됨

7. Setup: 드로어 열림
8. Action: [고정] 버튼 클릭
9. Verify: 다른 행 클릭 시에도 드로어가 열려있음
   Expected: 고정 상태 유지, 새 그룹 정보 표시

10. Setup: 드로어 열림 (고정됨)
11. Action: [x] 버튼 클릭
12. Verify: 드로어가 닫힘
    Expected: IsExpanded = false, Visibility.Collapsed
```

---

## 7. Security Considerations

| Concern | Risk | Mitigation |
|---------|------|------------|
| 파일 경로 노출 | Low | UI에만 표시, 로깅 시 민감 정보 마스킹 |
| 이미지 파일 접근 | Low | 기존 ImageManager 권한 체계 활용 |

---

## 8. Open Questions

- [x] 모든 디자인 질문 해결됨

---

## Approval

- [ ] All components have detailed pseudo-code
- [ ] Error handling specified for all failure modes
- [ ] State management documented
- [ ] Thread safety addressed
- [ ] Edge cases covered
- [ ] Test cases defined
- [ ] No open questions

**Next Step**: 05_tasks.md


