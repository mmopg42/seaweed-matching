# Task: Fix Anomaly Detection

- [ ] **Core Logic Refactoring** <!-- id: 0 -->
    - [ ] Rename `AddAndCheckImage` return values in `IAbnormalDetector.cs` <!-- id: 1 -->
    - [ ] Update `ImageMetadata.cs` (Rename `ZScoreWidth/Height` to `DevPctWidth/Height`) <!-- id: 2 -->
    - [ ] Refactor `AbnormalDetectorService.cs`: Implement Percent Deviation, Clean Baseline, Adaptive Reset <!-- id: 3 -->
    - [ ] Optimize `AbnormalDetectorService.cs`: Remove `RefreshConfiguration` call, use event <!-- id: 4 -->
- [ ] **Configuration Updates** <!-- id: 5 -->
    - [ ] Update `ApplicationConfiguration.cs`: Rename `ZScoreThreshold` -> `AbnormalPercentThreshold`, default WindowSize 40 <!-- id: 6 -->
    - [ ] Update `SettingsDialogViewModel.cs` and `SettingsDialog.xaml` <!-- id: 7 -->
    - [ ] Check/Fix any other references (e.g. `ImageProcessingService.cs`) <!-- id: 8 -->
- [ ] **Verification** <!-- id: 9 -->
    - [ ] Update `AbnormalDetectorServiceTests.cs` <!-- id: 10 -->
    - [ ] Verify build and run <!-- id: 11 -->
