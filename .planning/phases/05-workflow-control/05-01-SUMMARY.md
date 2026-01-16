# Phase 05-01: WorkflowPanel UI Structure Documentation

## Summary

This plan successfully identified and documented the WorkflowPanel UI structure for automation. The `FindWorkflowPanel` method and `inspect-workflow` CLI command were implemented to enable camera and path automation.

## Implementation

### Task 1: FindWorkflowPanel Method
**File:** `skills_scripts/ui_automation/UiAutomation.cs`
**Commit:** `06d8eec`

Added `FindWorkflowPanel` method that:
- Takes `Window? mainWindow` as parameter
- Searches for `ControlType.Custom` with Name or ClassName containing "WorkflowPanel"
- Returns `AutomationElement?` if found, null otherwise
- Logs control type, name, automationId, className when found

### Task 2: CLI inspect-workflow Command
**File:** `skills_scripts/ui_automation/Program.cs`
**Commit:** `3fb0964`

Added `inspect workflow` subcommand that:
- Finds ChronoView MainWindow via ChronoWindowFinder
- Calls FindWorkflowPanel to locate the panel
- Lists element tree at depth=3 using ListElements()
- Outputs ControlType, Name, AutomationId, ClassName for each element

## WorkflowPanel Structure (from XAML Analysis)

Based on `ChronoView/UI/Controls/WorkflowPanel.xaml`, the panel contains:

### 1. Camera Status Section (Expander)
**Header:** `{x:Static res:Strings.Panel_CameraStatus}` (localized)

**Camera Buttons:**
| Camera Type | Binding | Button Content | Command |
|-------------|---------|----------------|---------|
| General (Normal) | `Control.GeneralCameraState` | CameraStateToButtonText | `LaunchGeneralCameraCommand` |
| NIR 1 | `Control.NirCameraState` | CameraStateToButtonText | `LaunchNir1CameraCommand` |
| NIR 2 | `Control.Nir2CameraState` | CameraStateToButtonText | `LaunchNir2CameraCommand` |
| NIR Filtering | `Control.Nir2FilteringState` | CameraStateToButtonText | `ToggleNir2FilteringCommand` |

Each camera row has:
- Label TextBlock (e.g., "일반", "NIR", "NIR 2", "NIR 필터링")
- Ellipse indicator (12x12) with fill bound to state
- Button (60x24) with bound content, background, enabled state

### 2. Sample Move Settings Section (Expander)
**Header:** `{Binding SampleMoveSettingsHeader}`

**Line 1 Settings** (visible when `IsLine1Tab` or `IsCombinedTab`):
| Label | TextBox Binding |
|-------|-----------------|
| `{x:Static res:Strings.Sample_Name}` | `{Binding Line1SampleName}` |
| `{x:Static res:Strings.Sample_MoveNIR}` | `{Binding Line1MoveNir}` |
| `{x:Static res:Strings.Sample_MoveAllData}` | `{Binding Line1MoveAllData}` |

**Line 2 Settings** (visible when `IsLine2Tab` or `IsCombinedTab`):
| Label | TextBox Binding |
|-------|-----------------|
| `{x:Static res:Strings.Sample_Name}` | `{Binding Line2SampleName}` |
| `{x:Static res:Strings.Sample_MoveNIR}` | `{Binding Line2MoveNir}` |
| `{x:Static res:Strings.Sample_MoveAllData}` | `{Binding Line2MoveAllData}` |

### 3. Data Status Section (Expander)
**Header:** `{x:Static res:Strings.Panel_DataStatus}`

**Statistics Displays:**
| Label | Value Binding | FontWeight | Foreground |
|-------|---------------|------------|------------|
| `{x:Static res:Strings.Status_TotalGroups}` | `{Binding Dashboard.TotalGroups}` | Bold | Default |
| `{x:Static res:Strings.Label_WithNIR}` | `{Binding Dashboard.WithNirCount}` | Bold | Default |
| `{x:Static res:Strings.Label_WithoutNIR}` | `{Binding Dashboard.WithoutNirCount}` | Bold | Default |
| `{x:Static res:Strings.Label_Abnormal}` | `{Binding Dashboard.AbnormalCount}` | Bold | Orange |
| `{x:Static res:Strings.Status_Failures}` | `{Binding Dashboard.FailedCount}` | Bold | Red |

## Key Findings

1. **No explicit AutomationIds** - The XAML does not set AutomationId properties on any controls. Element identification must rely on:
   - ControlType (Button, TextBlock, Edit/TextBox)
   - Name property (localized text from Strings.resx)
   - Parent-child relationships

2. **Korean localization** - Camera labels and button text come from `Strings.resx`:
   - `Status_Normal` -> "일반"
   - `Status_NIR` -> "NIR"
   - `Status_NirFiltering` -> "NIR 필터링"

3. **Dynamic visibility** - Line1/Line2 settings visibility controlled by `IsLine1Tab`, `IsLine2Tab`, `IsCombinedTab` boolean properties with BoolToVisibilityConverter

4. **Button state converters** - Camera button appearance determined by `CameraStateToButtonText`, `CameraStateToButtonBackground`, `CameraStateToIsEnabled` converters

## Usage

```bash
# Build the automation tool
dotnet build skills_scripts/ui_automation/ui_automation.csproj

# Run the inspect-workflow command (requires ChronoView running)
./skills_scripts/ui_automation/bin/Debug/net10.0-windows/ui_automation.exe inspect workflow
```

## Next Steps

For subsequent plans in Phase 05:
1. **05-02:** Implement camera button automation (clicking launch/toggle buttons)
2. **05-03:** Implement path TextBox automation (reading/writing path values)
3. **05-04:** Implement Expander control automation (expand/collapse)

## Verification

- [x] Build succeeds: `dotnet build skills_scripts/ui_automation/ui_automation.csproj`
- [ ] inspect-workflow command runs without errors (requires running ChronoView)
- [x] Element hierarchy documented from XAML analysis
