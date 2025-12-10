# Task 4.2 Implementation Summary: SettingsDialog Browse Button

## Overview
Task 4.2에서는 설정 다이얼로그(`SettingsDialog.xaml`)의 **"Browse..." 버튼과 `FolderBrowserDialog` 기능을 연결**했습니다. 이를 통해 사용자는 직접 경로를 입력하는 대신 폴더 선택 창을 통해 오타 없이 경로를 설정할 수 있습니다.

---

## Key Implementation Details

### 1. ViewModel (SettingsDialogViewModel.cs)
- **Command State**: `BrowsePathCommand`가 이미 `RelayCommand<string>(ExecuteBrowsePath)`로 초기화되어 있음을 확인하고 활용했습니다.
- **Method Logic**: `ExecuteBrowsePath(string? pathType)`는 `WpfMessageBox` 및 `System.Windows.Forms.FolderBrowserDialog`를 사용하여 폴더를 선택하고, 선택된 경로를 해당 속성(`NirPath`, `Camera1Path` 등)에 할당합니다.

### 2. View (SettingsDialog.xaml)
기존에 비어있던 11개의 "Browse..." 버튼에 `Command`와 `CommandParameter`를 바인딩했습니다.

```xml
<!-- Example: NIR 1 Path -->
<Button Content="Browse..." Width="80" 
        Command="{Binding BrowsePathCommand}" 
        CommandParameter="nir1"/>

<!-- Example: Camera 1 Path -->
<Button Content="Browse..." Width="80" 
        Command="{Binding BrowsePathCommand}" 
        CommandParameter="cam1"/>
```

#### Mapped Parameters:
| Button Label | CommandParam | Property Updated |
|--------------|--------------|------------------|
| NIR 1 Path | `nir1` | `NirPath` |
| Normal 1 Path | `normal1` | `NormalPath` |
| Camera 1 Path | `cam1` | `Camera1Path` |
| ... | ... | ... |
| Camera 6 Path | `cam6` | `Camera6Path` |
| Output Path | `output` | `OutputPath` |

---

## Verification
- **Build**: Successful
- **Binding Check**: 각 버튼이 고유한 `CommandParameter`를 가지고 있어 올바른 속성을 업데이트하도록 구성됨.

---

## Phase 4 Progress
| Task | Description | Status |
|------|-------------|--------|
| 4.1 | DragSelectBehavior 연결 | ✅ COMPLETED |
| 4.2 | SettingsDialog Browse 버튼 구현 | ✅ COMPLETED |
| 4.3 | MainWindow.xaml 수정 (StatusBar, Tab) | TBD |
| 4.4 | Line2 탭 구현 | TBD |
