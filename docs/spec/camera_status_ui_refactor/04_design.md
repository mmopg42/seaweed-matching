---
Task: camera_status_ui_refactor
Created: 2026-01-14
Status: Draft
Depends On: 03_plan.md
---

# Camera Status UI Refactor - Detailed Design

## 1. Component Designs

### 1.1 CameraState Enum

> New enum to represent camera program state with 4 values.

#### Definition

```csharp
// Location: ChronoView/Models/CameraState.cs
namespace ChronoView.Models;

public enum CameraState
{
    Stopped,    // Program not running
    Starting,   // Launch in progress
    Running,    // Program running
    Stopping    // Terminate in progress
}
```

---

### 1.2 ISystemControlViewModel Interface Changes

> Remove 6 obsolete properties, add 3 new state properties.

#### Properties to Remove

```csharp
// DELETE these from interface:
string GeneralCameraStatus { get; }
string NirCameraStatus { get; }
string Nir2CameraStatus { get; }
string GeneralCameraButtonText { get; }
string NirCameraButtonText { get; }
string Nir2CameraButtonText { get; }
```

#### Properties to Add

```csharp
// ADD these to interface:
CameraState GeneralCameraState { get; }
CameraState NirCameraState { get; }
CameraState Nir2CameraState { get; }
```

---

### 1.3 SystemControlViewModel Changes

#### Properties to Remove (Full List)

```csharp
// ===== REMOVE: Lines 35-50 (12 properties) =====
private string _genCamStatus = "Deactivated"; 
public string GeneralCameraStatus { get => _genCamStatus; private set => SetProperty(ref _genCamStatus, value); }

private Brush _genCamForeground = Brushes.Gray; 
public Brush GeneralCameraForeground { get => _genCamForeground; private set => SetProperty(ref _genCamForeground, value); }

private string _nirCamStatus = "Deactivated"; 
public string NirCameraStatus { get => _nirCamStatus; private set => SetProperty(ref _nirCamStatus, value); }

private Brush _nirCamForeground = Brushes.Gray; 
public Brush NirCameraForeground { get => _nirCamForeground; private set => SetProperty(ref _nirCamForeground, value); }

private string _nir2CamStatus = "Deactivated"; 
public string Nir2CameraStatus { get => _nir2CamStatus; private set => SetProperty(ref _nir2CamStatus, value); }

private Brush _nir2CamForeground = Brushes.Gray; 
public Brush Nir2CameraForeground { get => _nir2CamForeground; private set => SetProperty(ref _nir2CamForeground, value); }

private string _genCamButtonText = "ON"; 
public string GeneralCameraButtonText { get => _genCamButtonText; private set => SetProperty(ref _genCamButtonText, value); }

private Brush _genCamButtonBg = Brushes.Green; 
public Brush GeneralCameraButtonBackground { get => _genCamButtonBg; private set => SetProperty(ref _genCamButtonBg, value); }

private string _nirCamButtonText = "ON"; 
public string NirCameraButtonText { get => _nirCamButtonText; private set => SetProperty(ref _nirCamButtonText, value); }

private Brush _nirCamButtonBg = Brushes.Green; 
public Brush NirCameraButtonBackground { get => _nirCamButtonBg; private set => SetProperty(ref _nirCamButtonBg, value); }

private string _nir2CamButtonText = "ON"; 
public string Nir2CameraButtonText { get => _nir2CamButtonText; private set => SetProperty(ref _nir2CamButtonText, value); }

private Brush _nir2CamButtonBg = Brushes.Green; 
public Brush Nir2CameraButtonBackground { get => _nir2CamButtonBg; private set => SetProperty(ref _nir2CamButtonBg, value); }
```

#### Properties to Add

```csharp
// ===== ADD: New camera state properties =====
private CameraState _genCamState = CameraState.Stopped;
public CameraState GeneralCameraState 
{ 
    get => _genCamState; 
    private set => SetProperty(ref _genCamState, value); 
}

private CameraState _nirCamState = CameraState.Stopped;
public CameraState NirCameraState 
{ 
    get => _nirCamState; 
    private set => SetProperty(ref _nirCamState, value); 
}

private CameraState _nir2CamState = CameraState.Stopped;
public CameraState Nir2CameraState 
{ 
    get => _nir2CamState; 
    private set => SetProperty(ref _nir2CamState, value); 
}
```

#### StatusChanged Event Handler Updates

```csharp
// ===== REPLACE: Lines 75-77 =====
// BEFORE (verbose):
_generalCameraLauncher.StatusChanged += (s, active) => { 
    GeneralCameraStatus = active ? "Activated" : "Deactivated"; 
    GeneralCameraForeground = active ? Brushes.Green : Brushes.Gray; 
    GeneralCameraButtonText = active ? "OFF" : "ON"; 
    GeneralCameraButtonBackground = active ? Brushes.Red : Brushes.Green; 
};

// AFTER (simplified):
_generalCameraLauncher.StatusChanged += (s, active) => 
{
    // Skip update if in transitional state (avoid race condition)
    if (GeneralCameraState is CameraState.Starting or CameraState.Stopping)
        return;
    GeneralCameraState = active ? CameraState.Running : CameraState.Stopped;
};
```

#### ExecuteLaunchAsync Method Updates

```pseudo
function ExecuteLaunchAsync(launcher, programName, stateProperty):
    // ===== CASE 1: Already running - Stop flow =====
    if launcher.IsActive AND launcher.CurrentProcess != null:
        dialogResult = ShowConfirmDialog("Terminate?")
        
        if dialogResult == Yes:
            stateProperty = CameraState.Stopping  // NEW: Set stopping state
            await launcher.TerminateAsync()
            stateProperty = CameraState.Stopped   // Fallback (event should handle)
            Log("{programName} terminated by user")
        else:
            ActivateWindow(launcher.CurrentProcess)
            Log("{programName} activated")
        return
    
    // ===== CASE 2: Not running - Start flow =====
    stateProperty = CameraState.Starting  // NEW: Set starting state
    
    (success, message) = await launcher.LaunchAsync()
    
    if success:
        stateProperty = CameraState.Running
        Log(message)
    else:
        stateProperty = CameraState.Stopped  // Reset on failure
        Log(Error, "{programName}: {message}")
```

**Implementation Note**: Since we have 3 cameras, create a helper method:

```csharp
private async Task ExecuteCameraLaunchAsync(
    dynamic launcher, 
    string programName, 
    Func<CameraState> getState,
    Action<CameraState> setState)
{
    // Use setState for state updates
}
```

---

### 1.4 Value Converters

> New converters to derive visual properties from CameraState.

#### File Location

`ChronoView/UI/Converters/CameraStateConverters.cs`

#### CameraStateToIndicatorBrushConverter

```csharp
public class CameraStateToIndicatorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CameraState state)
        {
            return state switch
            {
                CameraState.Stopped => Brushes.Gray,
                CameraState.Starting => Brushes.Gold,
                CameraState.Running => Brushes.LimeGreen,
                CameraState.Stopping => Brushes.Gold,
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

#### CameraStateToButtonTextConverter

```csharp
public class CameraStateToButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CameraState state)
        {
            return state switch
            {
                CameraState.Stopped => "Start",
                CameraState.Starting => "...",
                CameraState.Running => "Stop",
                CameraState.Stopping => "...",
                _ => "Start"
            };
        }
        return "Start";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

#### CameraStateToButtonBackgroundConverter

```csharp
public class CameraStateToButtonBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CameraState state)
        {
            return state switch
            {
                CameraState.Stopped => Brushes.ForestGreen,
                CameraState.Starting => Brushes.DarkGray,
                CameraState.Running => Brushes.Crimson,
                CameraState.Stopping => Brushes.DarkGray,
                _ => Brushes.ForestGreen
            };
        }
        return Brushes.ForestGreen;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

#### CameraStateToIsEnabledConverter

```csharp
public class CameraStateToIsEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CameraState state)
        {
            // Disable during transitional states
            return state is CameraState.Stopped or CameraState.Running;
        }
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

---

### 1.5 App.xaml Converter Registration

#### Add to App.xaml Resources

```xml
<Application.Resources>
    <!-- Existing resources... -->
    
    <!-- Camera State Converters -->
    <converters:CameraStateToIndicatorBrushConverter x:Key="CameraStateToIndicatorBrush"/>
    <converters:CameraStateToButtonTextConverter x:Key="CameraStateToButtonText"/>
    <converters:CameraStateToButtonBackgroundConverter x:Key="CameraStateToButtonBackground"/>
    <converters:CameraStateToIsEnabledConverter x:Key="CameraStateToIsEnabled"/>
</Application.Resources>
```

**xmlns required**:
```xml
xmlns:converters="clr-namespace:ChronoView.UI.Converters"
```

---

### 1.6 WorkflowPanel.xaml Changes

#### Current Layout (Lines 17-27)

```xml
<!-- General Camera - BEFORE -->
<StackPanel Orientation="Horizontal" Margin="0,0,0,4">
    <TextBlock Text="{x:Static res:Strings.Status_Normal}" Width="80"/>
    <Ellipse Width="10" Height="10" Fill="{Binding Control.GeneralCameraForeground}" Margin="0,0,5,0"/>
    <TextBlock Text="{Binding Control.GeneralCameraStatus}" Width="80"/>
    <Button Content="{Binding Control.GeneralCameraButtonText}" Width="40" Height="24" 
            Command="{Binding Control.LaunchGeneralCameraCommand}"
            Background="{Binding Control.GeneralCameraButtonBackground}"
            Foreground="White" Margin="4,0" FontWeight="Bold" ToolTip="실행 및 활성화"/>
</StackPanel>
```

#### Target Layout

```xml
<!-- General Camera - AFTER -->
<Grid Margin="0,0,0,4">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="16"/>   <!-- Indicator -->
        <ColumnDefinition Width="*"/>    <!-- Camera Name -->
        <ColumnDefinition Width="Auto"/> <!-- Button -->
    </Grid.ColumnDefinitions>
    
    <Ellipse Grid.Column="0" Width="10" Height="10" VerticalAlignment="Center"
             Fill="{Binding Control.GeneralCameraState, 
                    Converter={StaticResource CameraStateToIndicatorBrush}}"/>
    
    <TextBlock Grid.Column="1" Text="{x:Static res:Strings.Status_Normal}" 
               VerticalAlignment="Center" Margin="4,0"/>
    
    <Button Grid.Column="2" Width="50" Height="24"
            Content="{Binding Control.GeneralCameraState, 
                      Converter={StaticResource CameraStateToButtonText}}"
            Command="{Binding Control.LaunchGeneralCameraCommand}"
            Background="{Binding Control.GeneralCameraState, 
                         Converter={StaticResource CameraStateToButtonBackground}}"
            IsEnabled="{Binding Control.GeneralCameraState, 
                        Converter={StaticResource CameraStateToIsEnabled}}"
            Foreground="White" FontWeight="Bold"/>
</Grid>
```

**Changes Summary:**
- Removed redundant status TextBlock
- Changed container from StackPanel to Grid for better layout control
- Used converters for all dynamic properties
- Added IsEnabled binding for loading state

---

## 2. Edge Cases

| Case | Behavior |
|------|----------|
| Click button during Starting | Button is disabled, no action |
| Click button during Stopping | Button is disabled, no action |
| Process exits unexpectedly | StatusChanged event fires, state → Stopped |
| Launch fails | State reverts to Stopped, error logged |
| Multiple rapid clicks | Only first click registers (button disables) |

---

## 3. Testing Strategy

### 3.1 Existing Unit Tests

Check for existing tests:
```powershell
dotnet test ChronoView.Tests/ChronoView.Tests.csproj --filter "FullyQualifiedName~StartCommand"
dotnet test ChronoView.Tests/ChronoView.Tests.csproj --filter "FullyQualifiedName~StopCommand"
```

### 3.2 New Unit Tests (Optional)

Consider adding tests for converters:
- `CameraStateToIndicatorBrushConverterTests`
- `CameraStateToButtonTextConverterTests`

### 3.3 Manual Verification

1. **Build & Run**:
   ```powershell
   dotnet build ChronoView/ChronoView.csproj
   dotnet run --project ChronoView/ChronoView.csproj
   ```

2. **Visual Verification** - Check camera status section:
   - All 3 cameras show gray indicator and "Start" button initially
   - Layout: [●] [Camera Name] ... [Button]

3. **Start Flow Test**:
   - Click "Start" on General Camera
   - Verify: Button shows "...", indicator turns yellow, button disabled
   - Wait for program to open
   - Verify: Button shows "Stop", indicator turns green, button enabled

4. **Stop Flow Test**:
   - Click "Stop" on running camera
   - Confirm dialog appears
   - Click Yes
   - Verify: Button shows "...", indicator yellows, button disabled
   - Verify: After close, button shows "Start", indicator gray

5. **Cancel Test**:
   - Click "Stop" on running camera
   - Click No in dialog
   - Verify: State unchanged, program brought to foreground

---

## 4. Files to Modify Summary

| File | Action | Changes |
|------|--------|---------|
| `Models/CameraState.cs` | NEW | Create enum |
| `UI/Converters/CameraStateConverters.cs` | NEW | 4 converters |
| `UI/ViewModels/ISystemControlViewModel.cs` | MODIFY | Remove 6, Add 3 properties |
| `UI/ViewModels/SystemControlViewModel.cs` | MODIFY | Remove 12 fields, Add 3, Update handlers |
| `UI/Controls/WorkflowPanel.xaml` | MODIFY | Update 3 camera rows |
| `App.xaml` | MODIFY | Register converters |

---

## 5. Approval Checklist

- [x] All components have detailed pseudo-code
- [x] State management documented
- [x] Edge cases covered
- [x] Test cases defined
- [x] No open questions

**Next Step**: 05_tasks.md after approval
