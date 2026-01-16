---
Task: implement_script_based_abnormal_detection
Created: 2026-01-12
Status: Draft
Summary: Implementation tasks for C# adaptive detection
---

# Tasks: Adaptive Abnormal Detection (C#)

## 1. Preparation & Setup
- [ ] **Infrastructure**: Create `ChronoView/Core/Analytics/AbnormalHistoryManager.cs`. [NEW]
- [ ] **Infrastructure**: Create `ChronoView/Core/Analytics/IAbnormalDetector.cs` (Ensure exists/Create). [NEW]
- [ ] **Unit Test**: Test `AbnormalHistoryManager` (Load/Save/Update). [NEW]

## 2. Core Implementation
- [ ] **Configuration**: Add `AbnormalDetectionWindowSize` to `ApplicationConfiguration`. [MODIFY]
- [ ] **Service Enhancement**: Modify `AbnormalDetectorService.cs` to integrate History & Z-score. [MODIFY]
- [ ] **Data Integrity**: Modify `GroupManager.createGroupFromSingleFile` to perform recursive search for `stitched_original.png` ensuring `MainImagePath` is correct. [MODIFY]
- [ ] **Infrastructure**: Create `AbnormalHistoryManager.cs`. [NEW]

## 3. UI Integration
- [ ] **ViewModel**: Modify `FileGroupViewModel.cs`:
  - Simply use `FileGroup.MainImagePath` (guaranteed by Core).
  - Call `AddAndCheckImage` when image is loaded. [MODIFY]
- [ ] **Settings**: Add UI for `ZScoreThreshold` and `WindowSize` in `SettingsDialogViewModel.cs` / `SettingsDialog.xaml`. [MODIFY]

## 4. Verification
- [ ] **Manual Test**: Load "Normal" dataset then "Abnormal" dataset. Verify detection.
- [ ] **Persistence Test**: Restart app, verify history loaded.
