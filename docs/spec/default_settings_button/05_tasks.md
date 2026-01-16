# Tasks - Default Settings Button

## Context
Implement the "Default Settings" button in the Settings Dialog to reset configuration, ensuring all paths are cleared.

## Task List

### 1. Default Configuration Logic
- [ ] **Create `DefaultConfiguration.cs`** <!-- id: 1 -->
    - [ ] Location: `ChronoView/Core/Configuration/DefaultConfiguration.cs`
    - [ ] Implement `GetDefault()` returning `ApplicationConfiguration`.
    - [ ] Ensure all `*Path` properties are set to `string.Empty`.
    - [ ] Ensure `*ProgramPath` properties are set to `string.Empty`.

- [ ] **Unit Test** <!-- id: 2 -->
    - [ ] Create `DefaultConfigurationTests.cs` in `ChronoView.Tests`.
    - [ ] Verify `GetDefault()` returns empty strings for paths.

### 2. Settings Dialog Update
- [ ] **Update `SettingsDialogViewModel.cs`** <!-- id: 3 -->
    - [ ] Add `ResetToDefaultsCommand`.
    - [ ] Implement `ExecuteResetToDefaults` to load from `DefaultConfiguration`.
    - [ ] Refresh properties via `LoadFromConfiguration`.
    - [ ] Add safety logic to clear `DeleteQuarantinePath` after loading.

- [ ] **Update `SettingsDialog.xaml`** <!-- id: 4 -->
    - [ ] Add "Default" (기본값) button to the left of the action area.

### 3. Verification
- [ ] **Manual Test** <!-- id: 5 -->
    - [ ] Verify Default Button clears paths and resets values.
