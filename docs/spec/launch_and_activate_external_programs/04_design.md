---
Task: launch_and_activate_external_programs
Created: 2026-01-11
Status: Approved
Depends On: 03_plan.md
---

# 외부 프로그램 실행 및 활성화 - Detailed Design

## 1. Component Designs

### 1.1 WindowActivationHelper

> Win32 API를 사용하여 지정된 프로세스의 창을 활성화하고 최상단으로 가져오는 유틸리티 클래스.

#### Interface (from Plan)

```csharp
public static void ActivateProcessWindow(Process process)
```

#### Detailed Logic

```pseudo
function ActivateProcessWindow(process):
    // 1. 프로세스 및 핸들 유효성 검사
    if process is null or process.HasExited:
        return

    handle = process.MainWindowHandle
    if handle == 0:
        // 메인 창 핸들을 찾지 못한 경우 (창이 아직 안 떴거나 없는 유형)
        return

    // 2. 창 상태 확인 (최소화 여부)
    // SW_RESTORE (9): 창을 원래 크기로 복원하고 활성화함
    ShowWindow(handle, 9)

    // 3. 최상단 활성화
    SetForegroundWindow(handle)
```

#### Win32 P/Invoke Definitions

```csharp
[DllImport("user32.dll")]
private static extern bool SetForegroundWindow(IntPtr hWnd);

[DllImport("user32.dll")]
private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

[DllImport("user32.dll")]
private static extern IntPtr GetForegroundWindow();

private const int SW_RESTORE = 9;
```

#### Additional Method: IsProcessWindowInForeground

```pseudo
function IsProcessWindowInForeground(process):
    if process is null or process.HasExited:
        return false
    
    handle = process.MainWindowHandle
    if handle == 0:
        return false
    
    currentForeground = GetForegroundWindow()
    return handle == currentForeground
```

> [!NOTE]
> 이 메서드는 프로세스 창이 현재 최상단에 활성화되어 있는지 확인합니다.

---

### 1.2 ProcessFinder (Helper Method)

> 실행 중인 프로세스를 경로 기반으로 검색하는 공통 메서드. 각 Launcher에서 재사용.

#### Detailed Logic (with Exception Handling)

```pseudo
function FindExistingProcess(path):
    fileName = Path.GetFileNameWithoutExtension(path)
    processes = Process.GetProcessesByName(fileName)
    
    for p in processes:
        try:
            // 경로까지 일치하는지 확인 (권한 문제 발생 가능)
            if p.MainModule?.FileName equals path (case-insensitive):
                return p
        catch (Win32Exception, InvalidOperationException):
            // 권한 부족 또는 프로세스 접근 불가 시
            // 프로세스 이름만 일치하고 유효한 창이 있으면 반환
            if p.MainWindowHandle != IntPtr.Zero:
                return p
    
    return null
```

> [!NOTE]
> `MainModule.FileName` 접근 시 관리자 권한 프로세스나 32/64-bit 불일치로 `Win32Exception`이 발생할 수 있음. 이 경우 프로세스 이름과 유효한 창 핸들만으로 판단.

---

### 1.3 Launcher 클래스 확장 (GeneralCameraLauncher, NirCameraLauncher, Nir2CameraLauncher)

> 외부 프로그램을 실행하거나 이미 실행 중인 경우 활성화하도록 확장. 종료(Terminate) 기능 추가.

#### Interface (Modified)

```csharp
public bool IsActive { get; }
public Process? CurrentProcess { get; } // Or IsForeground property using Helper
public async Task<(bool Success, string Message)> LaunchAsync()
public async Task TerminateAsync()
```

#### Detailed Logic

```pseudo
function GetProcess():
    return _process

function TerminateAsync():
    if _process is running:
        _process.Kill()
        _process.WaitForExit()
        StatusChanged?.Invoke(this, false)

function LaunchAsync():
    // 1. 설정 로드 및 경로 확인
    config = _configManager.LoadConfiguration()
    path = GetProgramPath(config)
    
    // ... (경로 검증 생략) ...
    
    // 3. 실행 중인 프로세스 검색
    existingProcess = FindExistingProcess(path)
    
    if existingProcess is running:
        _process = existingProcess
        _logger.Log("Existing process found, activating...")
        WindowActivationHelper.ActivateProcessWindow(_process)
        StatusChanged?.Invoke(this, true)
        StartMonitoring()
        return (true, "Program activated")
        
    // 4. 실행
    _process = StartProcess(path)
    if _process is successful:
        StatusChanged?.Invoke(this, true)
        StartMonitoring()
        return (true, "Program launched")
    else:
        return (false, "Launch failed")
```

---

### 1.4 SystemControlViewModel

> 메인 대시보드 사이드바에서 사용할 실행 명령 및 NIR2 상태 추가. 종료 확인 로직 구현.

#### Command Execution Logic

```pseudo
function ExecuteLaunchAsync(launcher, name):
    // 1. 이미 실행 중인지 확인
    if launcher.IsActive and launcher.GetProcess() is not null:
        process = launcher.GetProcess()
        
        // 2. 이미 맨 앞에 있는지 확인
        if WindowActivationHelper.IsProcessWindowInForeground(process):
            // 3. 종료 확인 다이얼로그
            if MessageBox.Show("종료하시겠습니까?", YesNo) == Yes:
                await launcher.TerminateAsync()
                LogRequested?.Invoke(Info, "System", name + " terminated by user.")
                return
            else:
                return // 취소

    // 4. 실행 또는 활성화 (Launcher 내부에서 처리)
    (success, message) = await launcher.LaunchAsync()
    if not success:
        LogRequested?.Invoke(Error, "System", message)
    else:
        LogRequested?.Invoke(Info, "System", message)
```

---

## 2. UI Design (WorkflowPanel.xaml)

### 2.1 Updated Camera Status Section

세 개의 외부 프로그램 상태 표시 및 실행 버튼 배치:

```xml
<StackPanel Margin="10">
    <TextBlock Text="{x:Static res:Strings.Panel_SystemStatus}" FontWeight="Bold" Margin="0,0,0,8"/>
    
    <!-- General Camera -->
    <StackPanel Orientation="Horizontal" Margin="0,0,0,4">
        <TextBlock Text="{x:Static res:Strings.Status_Normal}" Width="80"/>
        <Ellipse Width="10" Height="10" Fill="{Binding Control.GeneralCameraForeground}" Margin="0,0,5,0"/>
        <TextBlock Text="{Binding Control.GeneralCameraStatus}" Width="80"/>
        <Button Content="▶" Width="30" Height="22" 
                Command="{Binding Control.LaunchGeneralCameraCommand}"
                ToolTip="실행 및 활성화" Margin="4,0"/>
    </StackPanel>
    
    <!-- NIR Camera 1 -->
    <StackPanel Orientation="Horizontal" Margin="0,0,0,4">
        <TextBlock Text="{x:Static res:Strings.Status_NIR}" Width="80"/>
        <Ellipse Width="10" Height="10" Fill="{Binding Control.NirCameraForeground}" Margin="0,0,5,0"/>
        <TextBlock Text="{Binding Control.NirCameraStatus}" Width="80"/>
        <Button Content="▶" Width="30" Height="22" 
                Command="{Binding Control.LaunchNir1CameraCommand}"
                ToolTip="실행 및 활성화" Margin="4,0"/>
    </StackPanel>
    
    <!-- NIR Camera 2 (신규 추가) -->
    <StackPanel Orientation="Horizontal" Margin="0,0,0,12">
        <TextBlock Text="NIR 2" Width="80"/>
        <Ellipse Width="10" Height="10" Fill="{Binding Control.Nir2CameraForeground}" Margin="0,0,5,0"/>
        <TextBlock Text="{Binding Control.Nir2CameraStatus}" Width="80"/>
        <Button Content="▶" Width="30" Height="22" 
                Command="{Binding Control.LaunchNir2CameraCommand}"
                ToolTip="실행 및 활성화" Margin="4,0"/>
    </StackPanel>
    
    <!-- NIR Filtering Toggle (기존 유지) -->
    <StackPanel Orientation="Horizontal" Margin="0,0,0,12">
        ...
    </StackPanel>
</StackPanel>
```

---

## 3. Edge Cases

| Case | Behavior | Implementation |
|------|----------|----------------|
| 경로 미설정 | 실행 실패 메시지 표시 | `LaunchAsync` 진입 시 검증 |
| 파일 없음 | 실행 실패 메시지 표시 | `File.Exists` 검증 |
| 동일 이름 프로세스 여러 개 | 경로(Path)가 정확히 일치하는 인스턴스 우선 | `FindExistingProcess` 로직 |
| 관리자 권한 필요한 프로그램 | `UseShellExecute = true`로 승격 유도 | `ProcessStartInfo` 설정 |
| MainModule 접근 권한 부족 | 예외 catch 후 창 핸들로 대체 판단 | try-catch in `FindExistingProcess` |
| **창이 맨 앞에 있을 때 버튼 클릭** | 종료 확인 다이얼로그 표시 | `IsProcessWindowInForeground` 확인 후 `MessageBox` |
| **종료 확인에서 "예" 클릭** | 프로그램 종료 (`Process.Kill`) | `result == MessageBoxResult.Yes` |
| **종료 확인에서 "아니오" 클릭** | 그대로 유지, 아무 동작 없음 | 조기 반환 |

---

## 4. Testing Strategy

### Unit Tests

| Test | Input | Expected |
|------|-------|----------|
| test_find_process_by_path | 유효한 경로 | 실행 중인 해당 프로세스 객체 반환 |
| test_find_process_permission_denied | 권한 부족 프로세스 | 창 핸들 기반으로 반환 |
| test_launch_if_not_running | 프로그램 종료 상태 | `Process.Start` 호출됨 |
| test_activate_if_running | 프로그램 실행 상태 | `WindowActivationHelper` 호출됨 |

---

## Approval

- [x] All components have detailed pseudo-code
- [x] Error handling specified for all failure modes
- [x] UI layout defined (including NIR2)
- [x] Edge cases covered
- [x] Test cases defined

> [!NOTE]
> **Reviewer Feedback (2026-01-11)**: 공통 로직을 Helper로 분리하여 기존 Launcher 중복(90%)을 개선. `UseShellExecute=true` 사용으로 인한 권한 상승 프로세스에 대한 예외 처리 전략 타당. 구현 시 try-catch 블록이 `Win32Exception`을 정확히 처리하도록 주의.

**Next Step**: 05_tasks.md
