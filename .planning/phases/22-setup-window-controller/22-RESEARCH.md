# Phase 22 Research: Setup Window Controller

**Research Date:** 2026-01-20
**Phase:** 22 - Setup Window Controller
**Goal:** SetupWindow 전용 컨트롤러와 설정 다이얼로그 자동화

---

## 1. SetupWindow UI Structure

### Window Properties
- **Title:** "Setup - ChronoView Pro" (SetupWindow.xaml:4)
- **WindowStyle:** None, AllowsTransparency: True
- **Detection:** Use substring "Setup" in ChronoWindowFinder.FindSetupWindow() (already exists)

### Key UI Elements with AutomationId

| Element | AutomationId | Command/Binding |
|---------|--------------|-----------------|
| Settings Button | `SetupSettingsButton` | OpenSettingsCommand |
| General Camera Button | `SetupGeneralCameraButton` | LaunchGeneralCameraCommand |
| NIR1 Camera Button | `SetupNir1CameraButton` | LaunchNir1CameraCommand |
| NIR2 Camera Button | `SetupNir2CameraButton` | LaunchNir2CameraCommand |
| NIR Filtering Toggle | `SetupNirFilteringButton` | ToggleNirFilteringCommand |
| Start Monitoring | `SetupStartButton` | StartCommand |
| Minimize Button | `SetupMinimizeButton` | Click event |
| Close Button | `SetupCloseButton` | Click event |

### Button Detection Notes
- Settings button has **no Name** property - only has an Image child icon
- `ChronoSettingsController.FindSetupWindowSettingsButton()` already handles this (lines 327-387)
- Uses Image child detection as fallback, then size-based detection (35-45px width/height)

---

## 2. Existing Infrastructure

### ChronoWindowFinder (ChronoWindowFinder.cs)
- ✅ `FindSetupWindow()` - already implemented (line 53-56)
- ✅ `WaitForWindow(string, int)` - reusable for waiting for SetupWindow
- ✅ `WaitForWindowToClose(string, int)` - reusable for monitoring MainWindow transition

### ChronoSettingsController (ChronoSettingsController.cs)
- ✅ `FindSetupWindowSettingsButton()` - detects Settings button via Image child
- ✅ `OpenSettingsDialog()` - already tries SetupWindow first before MainWindow (line 91-161)
- Pattern: Check SetupWindow first, then MainWindow for Settings button

### Command Registry Pattern
All command handlers implement `ICommandHandler`:
```csharp
public interface ICommandHandler
{
    void RegisterCommands(RootCommand rootCommand);
}
```

Existing handlers:
- WindowsCommands (windows find/main/setup/dialog)
- ToolbarCommands (toolbar start/stop/refresh/settings)
- SettingsCommands (settings open/close/tab paths)
- WorkflowCommands (workflow camera/paths)
- FileOpsCommands (fileops move/delete)
- AppLifecycleCommands (app launch/stop/restart/status)

---

## 3. Requirements Analysis

### SETUP-01: SetupWindow 전용 컨트롤러
Create `ChronoSetupWindowController` class (~400 lines) following same pattern as `ChronoSettingsController`:

**Core Methods:**
- `FindSetupWindow()` - reuse from ChronoWindowFinder
- `ClickSettingsButton()` - click SetupSettingsButton (opens SettingsDialog)
- `ClickStartButton()` - click SetupStartButton (transitions to MainWindow)
- `ClickGeneralCamera()` - click SetupGeneralCameraButton
- `ClickNir1()` - click SetupNir1CameraButton
- `ClickNir2()` - click SetupNir2CameraButton
- `ToggleNirFiltering()` - click SetupNirFilteringButton
- `CloseWindow()` - click SetupCloseButton

**Design Pattern:**
```csharp
public class ChronoSetupWindowController : IDisposable
{
    private readonly UIA3Automation _automation;
    private readonly ChronoWindowFinder _windowFinder;
    // ... methods following ChronoSettingsController pattern
}
```

### SETUP-02: 설정 다이얼로그 열기 자동화
- Already partially implemented in ChronoSettingsController.OpenSettingsDialog()
- Need to verify SetupWindow -> SettingsDialog flow works end-to-end
- Add WaitForDialogOpen() method for explicit verification

### SETUP-03: 완전한 셋업 왌료 워크플로우
**Required Sequence:**
1. Click General Camera (LaunchGeneralCameraCommand)
2. Click NIR1 (LaunchNir1CameraCommand)
3. Click NIR2 (LaunchNir2CameraCommand)
4. Optionally toggle NIR filtering
5. Verify settings (or open SettingsDialog to confirm)
6. Click Start button (SetupStartButton)
7. Wait for MainWindow to appear (use WaitForWindow("ChronoView Pro"))

**CLI Command:** `setup complete-full`

### SETUP-04: 설정값 검증
**Simulator Config Structure:**
Need to locate/read simulator config JSON file for path comparison.

From STATE.md blockers:
- "SetupWindow에서 설정을 열어서 모니터링 시작 버튼을 누르지 못함"
- "껐다가 다시 켰을 때 아무런 작동도 안 됨"

Root cause: "SetupWindow 전용 컨트롤러 부재"

---

## 4. Implementation Considerations

### Button Finding Strategies
1. **By AutomationId:** Most reliable for SetupWindow buttons
   - `SetupSettingsButton`, `SetupGeneralCameraButton`, etc.
2. **By Image Child:** Fallback for Settings button
3. **By Size:** Fallback for title bar buttons (35-45px)

### State Detection
- Camera buttons may have visual state changes after launching programs
- NIR Filtering button shows current status: "NIR 필터: [ON/OFF]"
- Start button is IsDefault="True" - can use Enter key

### Transition Handling
- SetupWindow → MainWindow transition happens after Start button click
- Need to wait for SetupWindow to close AND MainWindow to appear
- Use `WaitForWindowToClose("Setup")` + `WaitForWindow("ChronoView Pro")`

---

## 5. Dependencies

### Internal Dependencies
- `FlaUI.Core.AutomationElements.Window`
- `FlaUI.UIA3.UIA3Automation`
- `ChronoWindowFinder` (already exists)
- `ChronoSettingsController` (for SettingsDialog interaction)

### External Dependencies
- FlaUI 5.x (already in project)
- System.CommandLine (for CLI commands)

---

## 6. Open Questions

1. **Simulator Config Location:** Where is `simulator_config.json` stored?
   - Need to search for config file with `source_line1`, `source_line2`, `target_base` keys
   - May need to create if not exists

2. **Camera Launch Verification:** How to verify cameras launched successfully?
   - May need to check for external process windows
   - Or assume success if button click completes

3. **Settings Comparison:** What specific settings need verification?
   - Paths from simulator config vs ChronoView WorkflowPanel
   - Need to map simulator config keys to ChronoView settings

---

## 7. Reference Files

### ChronoView Source
- `ChronoView/UI/Views/SetupWindow.xaml` - UI structure and AutomationId values
- `ChronoView/UI/Views/MainWindow.xaml` - Target window after setup

### Automation Infrastructure
- `skills_scripts/ui_automation/ChronoWindowFinder.cs` - Window detection
- `skills_scripts/ui_automation/ChronoSettingsController.cs` - SettingsDialog interaction
- `skills_scripts/ui_automation/Commands/ICommandHandler.cs` - Handler interface
- `skills_scripts/ui_automation/Commands/CommandRegistry.cs` - Registration pattern

### Related Commands
- `skills_scripts/ui_automation/Commands/WindowsCommands.cs` - Window find commands
- `skills_scripts/ui_automation/Commands/SettingsCommands.cs` - Settings dialog commands
- `skills_scripts/ui_automation/Commands/AppLifecycleCommands.cs` - App launch/stop

---

## 8. Next Steps

1. Create `ChronoSetupWindowController.cs` following `ChronoSettingsController` pattern
2. Create `SetupCommands.cs` handler class for CLI commands
3. Register `SetupCommands` in `Program.cs` CommandRegistry
4. Implement `setup open-settings`, `setup complete-full`, `setup camera-states` commands
5. Add config verification logic (locate simulator config first)

---

*Research Complete - Ready for Planning Phase*
