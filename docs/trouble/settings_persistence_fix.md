# Settings Persistence Fix

## Problem
Advanced 옵션에서 matching 옵션을 껐는데 재시작하면 계속 켜진 상태로 돌아옴. 설정이 저장되지 않는 문제.

## Root Cause
1. `SettingsDialog.xaml`에 "Use time-based camera matching" 체크박스 존재 (line 136)
2. `SettingsDialogViewModel`에 `UseCamTimeMatching` 속성 존재 (line 202-206)
3. **BUT** `LoadFromConfiguration()`에서 이 값을 로드하지 않음
4. **AND** `SaveToConfiguration()`에서 이 값을 저장하지 않음
5. **AND** `ApplicationConfiguration.MatchingSettings`에 이 속성이 존재하지 않음

결과: UI에서 변경해도 설정 파일에 저장되지 않음 → 재시작 시 기본값으로 리셋

## Solution

### 1. Added Missing Properties to `MatchingSettings` Model

파일: `ChronoView/Models/ApplicationConfiguration.cs`

```csharp
// Matching Algorithm Options
public bool UseCamTimeMatching { get; set; } = true;
public double CamMatchMinDiff { get; set; } = 4.0;
public double CamMatchMaxDiff { get; set; } = 6.0;
public double NirMatchTimeDiff { get; set; } = 1.0;
public bool UseCameraSubfolderNormal { get; set; } = false;
public bool UseCameraSubfolderNormal2 { get; set; } = false;
public bool UseFolderSuffix { get; set; } = false;
```

### 2. Updated `LoadFromConfiguration()` in SettingsDialogViewModel

파일: `ChronoView/UI/ViewModels/SettingsDialogViewModel.cs`

```csharp
// Load matching options
UseCamTimeMatching = _configuration.MatchingSettings.UseCamTimeMatching;
CamMatchMinDiff = _configuration.MatchingSettings.CamMatchMinDiff;
CamMatchMaxDiff = _configuration.MatchingSettings.CamMatchMaxDiff;
NirMatchTimeDiff = _configuration.MatchingSettings.NirMatchTimeDiff;
NirTimeWindowSeconds = _configuration.MatchingSettings.NirTimeWindowSeconds;
CameraTimeWindowSeconds = _configuration.MatchingSettings.CameraTimeWindowSeconds;

// Load camera subfolder options
UseCameraSubfolderNormal = _configuration.MatchingSettings.UseCameraSubfolderNormal;
UseCameraSubfolderNormal2 = _configuration.MatchingSettings.UseCameraSubfolderNormal2;
UseFolderSuffix = _configuration.MatchingSettings.UseFolderSuffix;
```

### 3. Updated `SaveToConfiguration()` in SettingsDialogViewModel

```csharp
// Save matching options
_configuration.MatchingSettings.UseCamTimeMatching = UseCamTimeMatching;
_configuration.MatchingSettings.CamMatchMinDiff = CamMatchMinDiff;
_configuration.MatchingSettings.CamMatchMaxDiff = CamMatchMaxDiff;
_configuration.MatchingSettings.NirMatchTimeDiff = NirMatchTimeDiff;
_configuration.MatchingSettings.NirTimeWindowSeconds = NirTimeWindowSeconds;
_configuration.MatchingSettings.CameraTimeWindowSeconds = CameraTimeWindowSeconds;

// Save camera subfolder options
_configuration.MatchingSettings.UseCameraSubfolderNormal = UseCameraSubfolderNormal;
_configuration.MatchingSettings.UseCameraSubfolderNormal2 = UseCameraSubfolderNormal2;
_configuration.MatchingSettings.UseFolderSuffix = UseFolderSuffix;
```

## Testing Steps
1. Run application: `dotnet run --project ChronoView/ChronoView.csproj`
2. Click "Settings" button
3. Go to "Advanced" tab
4. Uncheck "Use time-based camera matching"
5. Click "OK" to save
6. Close and restart the application
7. Open Settings > Advanced again
8. **Verify**: "Use time-based camera matching" should still be unchecked ✅

## Build Result
✅ Build succeeded with 16 warnings (pre-existing)

## Files Modified
- [`ApplicationConfiguration.cs`](file:///c:/workspace/seaweed/gui_kiro/ChronoView/Models/ApplicationConfiguration.cs) - Added missing properties to MatchingSettings
- [`SettingsDialogViewModel.cs`](file:///c:/workspace/seaweed/gui_kiro/ChronoView/UI/ViewModels/SettingsDialogViewModel.cs) - Added load/save logic for matching options
