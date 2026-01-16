# Requirements: Refactor NIR2 Structure

## 1. Overview
Split the current `Nir2CameraLauncher` into two distinct services to separate program launching from file filtering logic.

## 2. Requirements
### 2.1 Refactoring
- **Rename**: Existing `Nir2CameraLauncher` -> `Nir2FilteringService`.
- **Split**: Remove program launching logic from `Nir2FilteringService`.
- **Create**: New `Nir2CameraLauncher` dedicated to launching the NIR 2 program (mimicking `NirCameraLauncher`).

### 2.2 Integration
- **DI**: Register both services in `App.xaml.cs`.
- **ViewModels**: Update `SetupWindowViewModel` and `SystemControlViewModel` to inject and use the appropriate service for each specific function (Launching vs. Filtering).

## 3. Constraints
- Must maintain existing filtering behavior (Input/Output paths, Logic).
- Must maintain existing logging levels (Debug for detail, Info for status).
