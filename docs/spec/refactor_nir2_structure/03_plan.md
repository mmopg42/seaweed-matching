# Plan: Refactor NIR2 Structure

## 1. Context
Currently, `Nir2CameraLauncher` is responsible for both:
1.  Launching the "NIR Camera 2" executable (although implementation was mixed).
2.  Monitoring a folder for files and filtering them (logic from `Nir2CameraLauncher.cs`).

We need to separate these into `Nir2CameraLauncher` (pure launcher) and `Nir2FilteringService` (pure logic/filtering).

## 2. Component Changes

### 2.1 File Renaming & Refactoring
- **Target**: `ChronoView/Core/ProgramLaunching/Nir2CameraLauncher.cs`
- **Action**: 
    1. Rename file to `Nir2FilteringService.cs`.
    2. Rename class to `Nir2FilteringService`.
    3. Remove `LaunchAsync` and `Process` related fields.
    4. Keep `StartFilteringAsync` logic.

### 2.2 New Component Creation
- **Target**: `ChronoView/Core/ProgramLaunching/Nir2CameraLauncher.cs` (New File)
- **Action**: 
    1. Create new class based on `NirCameraLauncher`.
    2. Adapt for "NIR Camera 2" (Config paths, Logger).

### 2.3 Dependency Injection
- **Target**: `ChronoView/App.xaml.cs`
- **Action**: Register both `Nir2CameraLauncher` and `Nir2FilteringService`.

### 2.4 Usage Updates
- **SystemControlViewModel**: Use `Nir2FilteringService` for the "Filter" toggle.
- **SetupWindowViewModel**: Use `Nir2CameraLauncher` for "Launch" button, `Nir2FilteringService` for "Filter" button.

## 3. Risk Assessment
- **Breaking References**: Renaming the class will break all current usages. This is intended but requires careful "Find All References" equivalent updates.
- **Config mapping**: Ensure we don't lose config path mappings during the switch.

## 4. Verification
- **Build**: Must succeed.
- **Runtime**: 
    - Verify "Launch NIR 2" starts the process (or attempts to).
    - Verify "Toggle Filter" starts the file watcher.
