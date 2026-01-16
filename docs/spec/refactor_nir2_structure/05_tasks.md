# Tasks: Refactor NIR2 Structure

- [ ] Rename `Nir2CameraLauncher.cs` to `NirFilteringService.cs`
    - [ ] Rename file
    - [ ] Rename class to `NirFilteringService`
    - [ ] Remove `LaunchAsync` and process monitoring code
    - [ ] Clean up constructor (remove unneeded deps for filtering if any)

- [ ] Create `Nir2CameraLauncher.cs`
    - [ ] Copy `NirCameraLauncher` pattern
    - [ ] Update to use NIR2 config paths
    - [ ] Set "NIR Camera 2" as program name

- [ ] Update Dependency Injection (`App.xaml.cs`)
    - [ ] Register `NirFilteringService`
    - [ ] Register `Nir2CameraLauncher` (new)

- [ ] Refactor `SystemControlViewModel`
    - [ ] Inject `NirFilteringService`
    - [ ] Update `ToggleNir2FilteringCommand` usage
    - [ ] Update status monitoring references

- [ ] Refactor `SetupWindowViewModel`
    - [ ] Inject `NirFilteringService`
    - [ ] Update `ToggleNirFilteringCommand` usage
    - [ ] Ensure `LaunchNir2CameraCommand` uses `Nir2CameraLauncher`

- [ ] Verify Build
    - [ ] Run `dotnet build`
