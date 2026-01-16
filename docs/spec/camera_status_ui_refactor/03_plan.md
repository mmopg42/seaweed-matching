---
Task: camera_status_ui_refactor
Created: 2026-01-14
Status: Draft
Depends On: 01_requirements.md
---

# Camera Status UI Refactor - Plan

## 1. Architecture Overview

### Current Architecture (Problem)

```
SystemControlViewModel
├── GeneralCameraStatus: string ("Activated"/"Deactivated")
├── GeneralCameraForeground: Brush (Green/Gray)
├── GeneralCameraButtonText: string ("ON"/"OFF")  
├── GeneralCameraButtonBackground: Brush (Red/Green)
├── NirCameraStatus: string
├── NirCameraForeground: Brush
├── NirCameraButtonText: string
├── NirCameraButtonBackground: Brush
├── Nir2CameraStatus: string
├── Nir2CameraForeground: Brush
├── Nir2CameraButtonText: string
└── Nir2CameraButtonBackground: Brush
    (12 properties for just 3 cameras - duplicated pattern)
```

### Target Architecture (Solution)

```
SystemControlViewModel
├── GeneralCameraState: CameraState (Stopped/Loading/Running)
├── NirCameraState: CameraState
├── Nir2CameraState: CameraState
└── (3 properties with value converters for visual)

CameraState enum
├── Stopped  → Gray indicator, "Start" button, Green button
├── Loading  → Yellow indicator, "Starting..."/"Stopping...", Disabled
└── Running  → Green indicator, "Stop" button, Red button
```

## 2. Components to Modify

### 2.1 [NEW] CameraState Enum

**Location**: `ChronoView/Models/CameraState.cs`

```csharp
public enum CameraState
{
    Stopped,    // Program not running
    Starting,   // Launch in progress
    Running,    // Program running
    Stopping    // Terminate in progress
}
```

### 2.2 [MODIFY] SystemControlViewModel

**Location**: `ChronoView/UI/ViewModels/SystemControlViewModel.cs`

**Remove (12 properties):**
- `GeneralCameraStatus`, `GeneralCameraForeground`, `GeneralCameraButtonText`, `GeneralCameraButtonBackground`
- `NirCameraStatus`, `NirCameraForeground`, `NirCameraButtonText`, `NirCameraButtonBackground`
- `Nir2CameraStatus`, `Nir2CameraForeground`, `Nir2CameraButtonText`, `Nir2CameraButtonBackground`

**Add (3 properties):**
- `GeneralCameraState: CameraState`
- `NirCameraState: CameraState`
- `Nir2CameraState: CameraState`

**Modify ExecuteLaunchAsync:**
- Set state to `Starting` before launch
- Set state to `Running` on success
- Set state to `Stopped` on failure

**Modify TerminateAsync flow:**
- Set state to `Stopping` before terminate
- Set state to `Stopped` after terminate

### 2.3 [MODIFY] ISystemControlViewModel

**Location**: `ChronoView/UI/ViewModels/ISystemControlViewModel.cs`

**Remove:**
- `GeneralCameraStatus`, `NirCameraStatus`, `Nir2CameraStatus`
- `GeneralCameraButtonText`, `NirCameraButtonText`, `Nir2CameraButtonText`

**Add:**
- `GeneralCameraState: CameraState`
- `NirCameraState: CameraState`
- `Nir2CameraState: CameraState`

### 2.4 [NEW] CameraState Value Converters

**Location**: `ChronoView/UI/Converters/CameraStateConverters.cs`

```csharp
// CameraStateToIndicatorBrushConverter
Stopped → Gray, Loading → Yellow, Running → Green

// CameraStateToButtonTextConverter
Stopped → "Start", Starting → "Starting...", 
Running → "Stop", Stopping → "Stopping..."

// CameraStateToButtonBackgroundConverter
Stopped → Green, Loading → Gray (disabled look), Running → Red

// CameraStateToIsEnabledConverter
Stopped → true, Loading → false, Running → true
```

### 2.5 [MODIFY] WorkflowPanel.xaml

**Location**: `ChronoView/UI/Controls/WorkflowPanel.xaml`

**Changes per camera row:**
- Remove: `<TextBlock Text="{Binding Control.GeneralCameraStatus}"/>`
- Update: Ellipse Fill to use converter
- Update: Button Content to use converter
- Update: Button Background to use converter
- Add: Button IsEnabled to use converter
- Adjust layout: `[Indicator] [CameraName] [Spacer] [Button]`

### 2.6 [MODIFY] Launcher StatusChanged Event Handling

**Location**: `SystemControlViewModel.cs` constructor

**Current:**
```csharp
_generalCameraLauncher.StatusChanged += (s, active) => {
    GeneralCameraStatus = active ? "Activated" : "Deactivated";
    GeneralCameraForeground = active ? Brushes.Green : Brushes.Gray;
    // ... 4 more property updates
};
```

**Target:**
```csharp
_generalCameraLauncher.StatusChanged += (s, active) => {
    // Only update if not in Loading state (avoid race condition)
    if (GeneralCameraState != CameraState.Starting && 
        GeneralCameraState != CameraState.Stopping)
    {
        GeneralCameraState = active ? CameraState.Running : CameraState.Stopped;
    }
};
```

## 3. Data Flow

```
User clicks button
       ↓
ViewModel sets State = Starting/Stopping
       ↓
ViewModel calls Launcher.LaunchAsync()/TerminateAsync()
       ↓
Launcher fires StatusChanged event
       ↓
ViewModel sets State = Running/Stopped
       ↓
XAML converters update UI automatically
```

## 4. Verification Plan

### 4.1 Build Verification
```powershell
dotnet build ChronoView/ChronoView.csproj
```
- Must compile without errors
- No warnings about unused properties

### 4.2 Existing Tests
```powershell
dotnet test ChronoView.Tests/ChronoView.Tests.csproj --filter "FullyQualifiedName~SystemControl"
```

### 4.3 Manual Testing
1. Start application: `dotnet run --project ChronoView/ChronoView.csproj`
2. Navigate to camera status section
3. Test each camera button:
   - Click Start → Should show "Starting..." with yellow indicator
   - After program starts → Should show "Stop" with green indicator
   - Click Stop → Should show "Stopping..." with yellow indicator
   - After program stops → Should show "Start" with gray indicator
4. Verify button is disabled during Starting/Stopping states

## 5. Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Race condition between UI and launcher events | Check current state before updating from event |
| Breaking existing tests | Update tests alongside implementation |
| Missing converter resources in XAML | Register converters in App.xaml Resources |
