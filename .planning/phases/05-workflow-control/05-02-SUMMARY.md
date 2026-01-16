# Phase 05-02: ChronoWorkflowController Implementation

## Summary

This plan successfully implemented the ChronoWorkflowController class for camera control automation and added CLI workflow commands. The implementation follows the ChronoToolbarController pattern established in Phase 03.

## Implementation

### Task 1: ChronoWorkflowController Class
**File:** `skills_scripts/ui_automation/ChronoWorkflowController.cs`
**Commit:** `45022b5` (amended in `bd60b08`)

Created a new controller class following ChronoToolbarController pattern with:

**Constructor Pattern:**
- `ChronoWorkflowController(UIA3Automation automation)` - injection constructor
- `ChronoWorkflowController()` - parameterless creating own UIA3Automation
- Implements IDisposable for cleanup

**Methods Implemented:**
| Method | Purpose |
|--------|---------|
| `FindMainWindow()` | Delegates to ChronoWindowFinder |
| `FindWorkflowPanel(Window?)` | Finds WorkflowPanel by Name/ClassName containing "WorkflowPanel" |
| `FindCameraButton(AutomationElement?, string)` | Finds buttons by text substring |
| `ClickButton(AutomationElement?)` | Uses FlaUI InvokePattern for clicking |
| `ClickGeneralCameraButton()` | Clicks General camera launch button ("실행") |
| `ClickNirCameraButton()` | Clicks NIR 1 camera launch button |
| `ClickNir2CameraButton()` | Clicks NIR 2 camera launch button |
| `ToggleNir2Filtering()` | Toggles NIR Filtering state ("사용"/"미사용") |
| `GetCameraStates()` | Reads all camera state indicators |

### Task 2: CLI Workflow Commands
**File:** `skills_scripts/ui_automation/Program.cs`
**Commit:** `bd60b08`

Added `workflow` command with subcommands following the toolbar command pattern:

| Subcommand | Action |
|-----------|--------|
| `workflow launch-general` | Click General Camera button |
| `workflow launch-nir` | Click NIR 1 Camera button |
| `workflow launch-nir2` | Click NIR 2 Camera button |
| `workflow toggle-filtering` | Toggle NIR Filtering |
| `workflow camera-states` | Read all camera states (supports `--json`) |

Added using alias: `Workflow = SkillsScripts.UiAutomation.ChronoWorkflowController`

## Technical Details

### Camera Button Discovery

Camera buttons have dynamic text based on state:
- **NotReady:** "실행" (launch)
- **Ready:** "실행 및 활성화" (launch and activate)
- **Active:** "중지" (stop)

The controller searches for buttons containing:
- "실행" (launch) first
- Falls back to "중지" (stop) if not found

### State Indicator Reading

State indicators are WPF Ellipse elements appearing as:
- `ControlType.Custom`
- `ClassName: "Ellipse"`

State mapping by Fill color:
- Gray (#808080): NotReady
- Green (#00FF00): Ready
- Blue (#0078D4): Active

Current implementation detects Ellipse elements and maps them to camera names via nearby TextBlock labels.

### Differences from XAML to UI Automation Tree

| Aspect | XAML | UI Automation Tree |
|--------|------|-------------------|
| WorkflowPanel | UserControl (no AutomationId) | ControlType.Custom, Name="WorkflowPanel" |
| Ellipse indicators | `Ellipse` elements | ControlType.Custom, ClassName="Ellipse" |
| Button text | Dynamic via converter | Bound to actual display text ("실행", "중지") |
| No AutomationId | None set | AutomationId is null |

## Usage

```bash
# Build the automation tool
dotnet build skills_scripts/ui_automation/ui_automation.csproj

# Read camera states (requires ChronoView running)
./skills_scripts/ui_automation/bin/Debug/net10.0-windows/ui_automation.exe workflow camera-states

# Click General camera button
./skills_scripts/ui_automation/bin/Debug/net10.0-windows/ui_automation.exe workflow launch-general

# Toggle NIR Filtering
./skills_scripts/ui_automation/bin/Debug/net10.0-windows/ui_automation.exe workflow toggle-filtering

# Get states as JSON
./skills_scripts/ui_automation/bin/Debug/net10.0-windows/ui_automation.exe workflow camera-states --json
```

## Key Decisions

1. **Substring matching for buttons** - Camera button text changes based on state, so searches use "실행" (launch) which is present in both "실행" and "실행 및 활성화"

2. **Ellipse detection via ClassName** - WPF Ellipse elements appear as ControlType.Custom with ClassName="Ellipse" in UI Automation

3. **Camera identification via labels** - Since multiple cameras have similar buttons, state detection maps Ellipse positions to nearby TextBlock labels ("일반", "NIR", "NIR 2")

4. **State detection simplified** - Initial implementation marks states as "Detected" rather than reading actual colors; color-based state reading requires deeper inspection

## Files Modified

| File | Changes |
|------|---------|
| `skills_scripts/ui_automation/ChronoWorkflowController.cs` | New file, ~370 lines |
| `skills_scripts/ui_automation/Program.cs` | Added using alias, ~80 lines for workflow commands |

## Verification

- [x] Build succeeds: `dotnet build skills_scripts/ui_automation/ui_automation.csproj`
- [x] Follows ChronoToolbarController pattern consistently
- [x] All CLI commands implemented per plan
- [ ] workflow camera-states shows all camera states (requires running ChronoView for verification)
- [ ] workflow launch-general clicks General camera button (requires running ChronoView)

## Next Steps

For subsequent plans in Phase 05:
1. **05-03:** Implement path TextBox automation (reading/writing sample move paths)
2. **05-04:** Implement Expander control automation (expand/collapse sections)
3. **Integration testing:** Test camera clicking with actual ChronoView instance

## Commits

- `45022b5`: feat(05-02): create ChronoWorkflowController class for camera control
- `bd60b08`: feat(05-02): add CLI workflow commands for camera control
