# Task: Camera Comparison Option Implementation

## 1. Implementation
- [x] **Data Model Update** <!-- id: 1 -->
    - [x] Add `CompareToReferenceCamera` property to `ChronoView.Models.DataSequenceSettings` <!-- id: 2 -->
- [x] **UI Update** <!-- id: 3 -->
    - [x] Add `CompareToReferenceCamera` property to `SettingsDialogViewModel` <!-- id: 4 -->
    - [x] Add CheckBox to `SettingsDialog.xaml` binding to the new property <!-- id: 5 -->
- [x] **Core Logic Implementation** <!-- id: 6 -->
    - [x] Modify `FileMatchingEngine.BuildLineGroupsWithOrder` to implement the reference comparison logic <!-- id: 7 -->
        - [x] Add config check for `CompareToReferenceCamera` <!-- id: 8 -->
        - [x] Implement target reference selection (Cam2/3 -> Cam1) <!-- id: 9 -->
        - [x] Implement "Nearest Enabled Preceding" fallback logic <!-- id: 10 -->
        - [x] Implement group lookup and diff calculation <!-- id: 11 -->

## 2. Verification
- [ ] **Unit Tests** <!-- id: 12 -->
    - [x] Create/Update `FileMatchingEngineTests.cs` <!-- id: 13 -->
    - [ ] Test Case: Option Disabled (Sequential) <!-- id: 14 -->
    - [ ] Test Case: Option Enabled (Reference Match) <!-- id: 15 -->
    - [ ] Test Case: Fallback (Cam1 Missing) <!-- id: 16 -->
- [ ] **Manual Verification** <!-- id: 17 -->
    - [ ] Verify UI checkbox state persistence <!-- id: 18 -->
    - [ ] Verify matching behavior with simulated data <!-- id: 19 -->
