---
Task: implement_script_based_abnormal_detection
Created: 2026-01-12
Status: Draft
Summary: Refactor for C# native adaptive detection with corrected scope
---

# Adaptive Abnormal Detection - Implementation Plan

## 1. Goal Description

Enhance the **existing synchronous `AbnormalDetectorService`** validation logic.
The system will maintain a local history of image statistics (per context) to adapt to varying sample sizes, while **preserving the existing NIR-only detection logic**.
This avoids breaking changes to the interface and reuses the existing DI infrastructure.

## 2. User Review Required

> [!NOTE]
> **Strategy Change**: Instead of replacing the service, we will **enhance the existing `AbnormalDetectorService`**.
> **Interface**: Remains **Synchronous**. No async refactoring required for ViewModels.

## 3. Proposed Changes

### 3.1 Core / Analytics
#### [MODIFY] [AbnormalDetectorService.cs](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/Core/Analytics/AbnormalDetectorService.cs)
- Integrate `AbnormalHistoryManager` for state persistence.
- Implement Z-score logic with moving window (currently stubbed or basic).
- **CRITICAL**: Preserve `IsGroupAbnormal` NIR-only detection logic.
- Add Context identification logic (Regex on filenames).

#### [NEW] [AbnormalHistoryManager.cs](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/Core/Analytics/AbnormalHistoryManager.cs)
- Manages `abnormal_history.json`.
- Thread-safe Load/Save operations.

### 3.2 Configuration
#### [MODIFY] [ApplicationConfiguration.cs](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/Models/ApplicationConfiguration.cs)
- Add `AbnormalDetectionWindowSize` (default: 10).
- Reuse `ZScoreThreshold` (default: 3.0).

### 3.3 UI / ViewModels
#### [MODIFY] [FileGroupViewModel.cs](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/UI/ViewModels/FileGroupViewModel.cs)
- Call `detector.AddAndCheckImage()` when image size is loaded.
- Keep existing synchronous calls.

#### [MODIFY] [SettingsDialogViewModel.cs](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/UI/ViewModels/SettingsDialogViewModel.cs)
- Add UI for `AbnormalDetectionWindowSize` and `ZScoreThreshold`.

## 4. Verification Plan

### 4.1 Automated Tests
- **Unit Test**: `AbnormalDetectorServiceTests`
  - Verify NIR-only detection (True).
  - Verify Normal group detection (False).
  - Verify Z-score calculation with Window=10.
  - Verify History persistence.

### 4.2 Manual Verification
- Clear `abnormal_history.json`.
- Load 10 normal images.
- Load 1 abnormal image.
- Verify detection.

