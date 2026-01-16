# Outlier Logic Connection Plan

## 1. Issue Summary
- **Current State**: `AbnormalDetectorService` contains Z-Score logic (`AddAndCheckImage`), but it is **never called** by the UI or Data Processing layer (`FileGroupViewModel`).
- **Result**: Image dimension anomalies are never flagged in the UI, regardless of the mathematical threshold.
- **Goal**: Connect the `FileGroupViewModel` to the `AbnormalDetectorService` to enable real-time anomaly detection for image dimensions.

## 2. Proposed Changes

### 2.1 Interface Update (`IAbnormalDetector`)
- No changes needed to the interface signature, but we need to ensure `AbnormalDetectorService` is stateful and thread-safe if accessed from UI threads.

### 2.2 ViewModel Update (`FileGroupViewModel.cs`)
- **Modification**: In `InitializeImagePaths` or `CheckAbnormalStatus`:
    1.  Open the `stitched_original.png` (if exists).
    2.  Read Width/Height.
    3.  Call `_abnormalDetector.AddAndCheckImage(width, height)`.
    4.  Update `IsAbnormal` property and `_abnormalReason` based on the return value.
- **Logic placement**:
    - Ideally, this should happen once when the group is loaded or updated.
    - Since `GetNormalImageSize` already opens the file stream, we should refactor to avoid opening the file twice.

### 2.3 Service Refinement (`AbnormalDetectorService.cs`)
- **Robustness**: Ensure `Reset` or proper buffer management is available if needed (e.g., on "Clear All").
- **Zero Variance Fix**: (Optional but Recommended) Add a safe-guard for Zero Variance (Z=null) cases. If Z is null due to perfect history, check absolute deviation? *For now, we will stick to connecting the existing logic and observing.*

## 3. Verification Plan

### 3.1 Automated Verification
- **Unit Test**: Create a test that:
    1.  Instantiates `AbnormalDetectorService`.
    2.  Feeds it 20 identical images (dim 100x100).
    3.  Feeds it 1 anomaly (dim 200x200).
    4.  Asserts `IsAbnormal` is true.

### 3.2 Manual Verification
- **Simulation Script**: Re-run the python simulation? No, the python simulation verified the *math* separate from the app.
- **App Test**:
    1.  Start ChronoView.
    2.  Point to the test data folder (`.../20260108/normal`).
    3.  Observe that "Abnormal" status is now displayed for the outliers (images 10+).

## 4. Risks
- **Performance**: Opening every image stream on the UI thread (via ViewModel creation) might slow down loading if thousands of groups exist.
    - *Mitigation*: `FileGroupViewModel` currently does this in `GetNormalImageSize` only when the property is accessed (getter)? No, `GetNormalImageSize` is a private helper called by the property getter. Properties `NormalImageSizeLabel` are bound to UI.
    - We should perform the check asynchronously or carefully to avoid UI freeze.
