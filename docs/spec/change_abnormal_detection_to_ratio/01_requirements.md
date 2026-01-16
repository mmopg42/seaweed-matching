---
Task: change_abnormal_detection_to_ratio
Created: 2026-01-13
Status: Approved
Summary: Change abnormal detection logic from individual dimension deviation to aspect ratio deviation to handle global scaling correctly.
Research Required: Yes (Completed)
---

# Change Abnormal Detection to Aspect Ratio - Requirements

## 1. Goal

### 1.1 Primary Goal

The system detects abnormal images based on **Aspect Ratio (Width / Height)** deviation instead of individual Width/Height deviations, preventing false positives from global scaling (Zoom).

### 1.2 Success Criteria

- [ ] `AbnormalDetectorService` calculates and tracks Aspect Ratio (Width / Height) for each context.
- [ ] Detection logic uses **Absolute Difference** (`|CurrentRatio - MedianRatio|`) instead of percentage deviation.
- [ ] **Global scaling (Zoom)** where Width and Height change proportionally is **NOT** marked as abnormal.
- [ ] **Distortion/Crop** where ratio changes exceeding `AbnormalRatioThreshold` is **marked as abnormal**.
- [ ] `FileGroupViewModel` displays the Ratio Difference (e.g., "Ratio Diff: 0.35") in the UI.
- [ ] Configuration uses `AbnormalRatioThreshold` (Default 0.3) instead of percentage.

## 2. Constraints

### 2.1 Technical Constraints

- Must maintain the existing context-based history mechanism.
- **Logic Change**: Switch from Percentage-based deviation to Absolute Ratio difference.
- **Configuration Change**: Deprecate `AbnormalPercentThreshold`. Introduce `AbnormalRatioThreshold`.

### 2.2 Business Constraints

- Minimal disruption to existing operation.
- Users understand "Ratio Difference" better than "Percentage of Ratio".

### 2.3 Non-Goals (Out of Scope)

- Detecting overall resolution changes if the ratio remains the same (e.g., 4000x3000 -> 2000x1500 is Normal).

## 3. Questions to Investigate

- [x] Should we use Percentage or Absolute Difference for Ratio? -> **Absolute Difference** selected.
- [x] Should we reuse the existing Threshold config? -> **No**, introduce new `AbnormalRatioThreshold`.

## 4. Assumptions

- Users prefer a simpler "Ratio Diff" metric over complex percentage calculations.
- A threshold of `0.3` is a reasonable starting point (covers 4:3 vs 1:1 difference).

## 5. Dependencies

### 5.1 Blocked By

- None

### 5.2 Blocks

- None
