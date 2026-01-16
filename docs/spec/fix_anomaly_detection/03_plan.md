# Implement Robust Anomaly Detection

## Goal
Replace the unstable Z-score based anomaly detection with a robust **Percent Deviation** method using a Clean Baseline, and optimize configuration loading performance.

## User Review Required
> [!IMPORTANT]
> **Logic Change**: The detection logic will change from Statistical Z-score to **Percent Deviation**.
> - Old: `Z > 2.0` (Relative to StdDev, highly unstable with outliers)
> - New: `Deviation > 12.0%` (Relative to Median, stable)
>
> **Configuration Impact**:
> - `ZScoreThreshold` setting will now be interpreted as **Percent Threshold** (e.g., 12.0 = 12%).
> - Default `WindowSize` will be increased from 10 to **40** to provide a more stable baseline.
> - **Action**: Existing users may need to adjust their threshold if they were relying on strict Z-score values (though 2.0 was likely too sensitive anyway).

## Proposed Changes

### Core Logic (`AbnormalDetectorService.cs`)
- [ ] **OPTIMIZE**: Remove `RefreshConfiguration()` from `AddAndCheckImage`. Implement event subscription to `ConfigurationManager.ConfigurationChanged` or single-load pattern.
- [ ] **REFACTOR**: Replace `CalculateZScore` with `CalculatePercentDeviation`. **Delete** the old Z-score logic entirely as it is unstable.
- [ ] **RENAME**: Rename `ZScoreWidth/Height` to `DevPctWidth/Height` in `IAbnormalDetector`, `ImageMetadata`, and `AbnormalDetectorService`.
- [ ] **FEATURE**: Implement **Clean Baseline** policy (Abnormal samples are NOT added to history).
- [ ] **FEATURE**: Implement **Adaptive Reset** (10 consecutive abnormals -> Reset baseline).

### Configuration (`ApplicationConfiguration.cs`)
- [ ] **UPDATE**: Change default `AbnormalDetectionWindowSize` from `10` to `40`.
- [ ] **RENAME**: Rename `ZScoreThreshold` to `AbnormalPercentThreshold` is `ApplicationConfiguration.cs`, `SettingsDialogViewModel.cs`, and `SettingsDialog.xaml`.
- [ ] **DOC**: Update comments for `ZScoreThreshold` to reflect it is now "Percent Threshold".

## Verification Plan

### Automated Tests (`AbnormalDetectorServiceTests.cs`)
- [ ] **UPDATE**: `When_Adding_Normal_Images_Should_Detect_Abnormal`
    - Verify that a 15% deviation is detected as abnormal (Threshold=12%).
- [ ] **NEW**: `When_Abnormal_Detected_Should_Not_Pollute_History`
    - Add normal samples, then an abnormal sample.
    - Verify that subsequent normal samples are still judged against the original baseline (Median shouldn't shift).
- [ ] **NEW**: `When_Process_Drifts_Should_Reset_After_10_Consecutive`
    - Simulate a camera move (drastic dimension change).
    - Verify that after 10 samples of the "new" dimension, the system accepts it as Normal.

### Manual Verification
1. **Simulation**: Run `oneoff/test_smart_mad.py` (updated to C# logic) with `image_sizes.json` data.
2. **UI Test**:
    - Start Monitoring.
    - Trigger abnormal images (e.g. cover camera).
    - Verify "Abnormal" log appears.
    - Check if performance is stable (no lag from config reading).
