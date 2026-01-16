---
Task: Change Abnormal Detection to Ratio Based
Created: 2026-01-14
Status: Approved
Depends On: 01_requirements.md (Implicit)
---

# Change Abnormal Detection to Ratio Based - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: How is abnormal detection currently implemented? | Independent Percentage Deviation of Width & Height from Median | High |
| Q2: What does "Ratio Based" likely mean? | Aspect Ratio (Width / Height) stability | High |
| Q3: Why change? | Independent checks flag proportional scaling (e.g. zoom) as abnormal, whereas Aspect Ratio handles it. | Medium |

## 2. Detailed Findings

### 2.1 Current Implementation Analysis

**Method**: Code review of `AbnormalDetectorService.cs` and `ApplicationConfiguration.cs`.

**Findings**:
- **Logic**: Calculates Z-score-like deviation using **Median** as baseline.
- **Formulas**: `DevWidth = |W - MedW| / MedW * 100`, `DevHeight = |H - MedH| / MedH * 100`.
- **Threshold**: `AbnormalPercentThreshold` (Default 12.0%).

**Conclusion**: Detects size changes, even proportional ones (zoom).

---

### 2.2 Proposed "Absolute Ratio Difference" Logic

**Goal**: Detect only when the **Aspect Ratio** changes (distortion, crop error), ignoring overall size changes (zoom).

**New Logic**:
1. Calculate Aspect Ratio `Ratio = Width / Height`.
2. Maintain history of `Ratio`.
3. Calculate Median of Ratio (`MedRatio`).
4. Calculate Absolute Difference: `Diff = |Ratio - MedRatio|`.
5. Compare `Diff > AbnormalRatioThreshold`.

**Why Absolute Difference?**
- Intuitive: "Ratio changed by 0.3" is easy to understand.
- No need for percentage conversion.
- Matches user preference for simplicity.

---

## 3. Impact & Code Cleanup

### 3.1 Deprecated / To Be Removed

The following items are related to the old "Independent Percentage" logic and should be removed or replaced.

| Component | Item | Action |
|-----------|------|--------|
| `ApplicationConfiguration` | `AbnormalPercentThreshold` | **Remove**. Replace with `AbnormalRatioThreshold`. |
| `SettingsDialog.xaml` | Binding `AbnormalPercentThreshold` | **Update** to `AbnormalRatioThreshold`. Update label text. |
| `SettingsDialogViewModel` | Property `AbnormalPercentThreshold` | **Remove**. Add `AbnormalRatioThreshold`. |
| `IAbnormalDetector` | `Threshold` (property) | **Update** meaning implies Ratio threshold now. |
| `IAbnormalDetector` | `AddAndCheckImage` return `(bool, double? devW, double? devH)` | **Update** return to `(bool, double? ratioDiff)`. |
| `ImageMetadata` | `DevPctWidth`, `DevPctHeight` | **Remove**. Unused or replace with `RatioDiff` if needed. |
| `FileGroupViewModel` | `CheckAbnormalStatus` logic | **Update** to use new return signature. |

### 3.2 New Configuration

- **Name**: `AbnormalRatioThreshold`
- **Type**: `double`
- **Default Value**: `0.3`
    - Rationale: 4:3(1.33) -> 1:1(1.0) change is 0.33. Threshold of 0.3 detects this comfortably.

## 4. Recommendations

### Primary Recommendation

**Adopt Absolute Ratio Difference Logic**.

**Action Plan**:
1.  **Config**: Add `AbnormalRatioThreshold` (0.3), remove `AbnormalPercentThreshold`.
2.  **Service**: Update `AbnormalDetectorService` to calculate Ratio and Median Ratio.
3.  **Interface**: Change `AddAndCheckImage` to return `(bool IsAbnormal, double? RatioDiff)`.
4.  **UI**: Update `SettingsDialog` to edit new threshold. Update `FileGroupViewModel` to display "Ratio Diff: 0.3" in abnormal reason.
5.  **Cleanup**: Remove `DevPctWidth/Height` from `ImageMetadata` and other places.

## 5. Unanswered Questions

- None. Logic and scope are clear.

---

## Approval

- [x] All questions from requirements addressed
- [x] Evidence provided for conclusions
- [x] Recommendations are actionable
- [x] Risks identified

**Next Step**: 03_plan.md
