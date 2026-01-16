---
Task: fix_anomaly_detection
Created: 2026-01-13
Status: Draft
Depends On: N/A
---

# Fix Anomaly Detection - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: Why does the current Z-score logic fail to detect obvious outliers? | **Outlier Pollution**: Caught outliers are added to history, inflating Standard Deviation and shifting Mean, masking subsequent outliers. **Std=0 Bug**: Perfect history causes logic to return "Normal". | High |
| Q2: What is the correct statistical method for this use case? | **Clean Baseline + Independent Percent Deviation**. Statistical methods (Z-score, MAD) are too sensitive to small noise when variation is low, or too slow to react when pollution occurs. | High |
| Q3: How should history be managed? | Use a sliding window but **only add- **Strict Mode**: With a "Clean Baseline", once a baseline is established, it will only change if more Normal samples are added. | High |

## 2. Detailed Findings

### 2.1 Q1: Why does the current Z-score logic fail to detect obvious outliers?

**Method**: 
1. Analyzed `AbnormalDetectorService.cs` implementation.
2. Simulated the logic using the provided `image_sizes.json` data via a Python script.
3. Traced the sequence of Z-scores.

**Findings**:
- **Flaw 1 (Std=0 Handling)**: Code returns `(false, null, null)` when StdDev is 0. If the history is perfect (e.g., all 1896), an outlier (1528) generates `Std=0` -> Returns Normal.
- **Flaw 2 (Outlier Pollution)**: The system adds **every** image to the history buffer, even likely outliers. 
    - Sequence: `[..., 1896, 1896]` -> `1528` (Missed or added) -> History now has large variance.
    - Next outlier `1616`: The StdDev is now large (~108). The distance (1850 - 1616 = 234) is only `2.16 * StdDev`. 
    - If threshold > 2.2, `1616` is ignored.
    - As more outliers enter, the "Normal" range expands significantly.

**Evidence**:
- Code: `if (!zScoreWidth.HasValue) return (false, ...)` (Line 127).
- Simulation Output:
  ```text
  1528       1885.60    13.41      -26.67     ABNORMAL 
  1616       1850.40    108.28     -2.16      ABNORMAL (Low Z-score)
  1520       1823.20    127.82     -2.37      ABNORMAL (Low Z-score)
  ```
  *Note: The first outlier (1528) exploded the StdDev from ~13 to ~108, reducing the sensitivity by 8x.*

**Conclusion**: The combination of `Std=0` bug and non-robust statistics (Mean/StdDev) allows outliers to "pollute" the baseline.

---

### 2.2 Q2: What is the correct statistical method?

**Method**: Literature review of Robust Statistics for anomaly detection.

**Findings**:
- **Standard Z-score**: Vulnerable to outliers. Single outlier shifts Mean and inflates StdDev.
- **Robust Z-score (MAD)**: Uses **Median** and **Median Absolute Deviation**.
    - $MAD = Median(|X_i - Median(X)|)$
    - $Robust Z = \frac{0.6745 (X - Median)}{MAD}$
- **Comparison**:
    - If history has 1 outlier in 10 samples, Median remains unchanged. Mean shifts significantly.
    - MAD remains small. StdDev explodes.
    - MAD correctly identifies subsequent outliers because the denominator (MAD) stays small.

**Conclusion**: Switch to **Stable Baseline Percent Deviation**.

---

### 2.3 Q3: Why did MAD-based Z-score fail during testing?

**Method**: 
1. Simulated MAD (Median Absolute Deviation) logic on user data.
2. Observed results for keys like `184715`.

**Findings**:
- **Median Shift**: Even if Median is robust, if >50% of the window is occupied by outliers (e.g., during a sequence of bad 김 images), the Median itself shifts to the bad value.
- **Sensitivity Issue**: When 김 indices are very stable (Width=1896, 1896, 1896...), MAD becomes 0. A small natural variation (e.g. 1px) then results in an "Infinite" Z-score, causing false positives.

**Conclusion**: Purely statistical methods are too fragile for this specific industrial application where "Normal" is a fixed specification.

---

### 2.4 Q4: Does Aspect Ratio provide a better signal?

**Method**: Calculated $Ratio = Width / Height$.

**Findings**:
- **Dimensional Compensation**: If both Width and Height shrink (e.g. 1528x1888 vs 1896x2112), the Ratio might only change by ~10% (0.80 vs 0.89).
- **Masking Error**: This masks significant absolute shrinkage, making it harder to detect "small 김" if the shape is preserved.

**Conclusion**: Width and Height must be checked **independently**.

---

## 3. Code Analysis

### 3.1 Relevant Existing Code

| File | Component | Relevance | Notes |
|------|-----------|-----------|-------|
| `AbnormalDetectorService.cs` | `CalculateZScore` | Core logic to replace | Currently uses Mean/StdDev |
| `AbnormalDetectorService.cs` | `AddAndCheckImage` | Workflow | Need to fix `Std=0` handling logic |
| `AbnormalHistoryManager.cs` | `GetHistory` | Data Source | Existing circular buffer logic is fine |

### 3.2 Impact Analysis

- **AbnormalDetectorService**: Logic change only. Input/Output contracts remain same.
- **Configuration**: `Threshold` semantics change. Z-score threshold of 2.0 corresponds to roughly 3.5 in Modified Z-score? No, typically 3.5 is used as a cutoff for Modified Z-score.
    - *Risk*: Existing users might need config update.
    - *Mitigation*: Modified Z-score is scaled by 0.6745 to be consistent with Standard Z-score for normal distributions. So 3.0 threshold should still work similarly for normal data.

## 4. Options Analysis

### Pollution Prevention Policy
To maintain a stable baseline, we strictly control what enters the history:
- $If \ Deviation\% > Threshold$:
    - Result: **ABNORMAL**.
    - Action: **Discard** (do not add to history).
- $Else$:
    - Result: **Normal**.
    - Action: **Add** to history, update baseline.

### Option A: Fix Z-Score (Exclude Outliers from History)

**Description**: Keep using Mean/StdDev, but do not add detected abnormalities to history.

**Pros**: Standard deviation stays small.
**Cons**: If the process legitimately shifts (e.g. new camera setting), the system locks up (rejects everything) forever. Requires a "Reset" button or complex "Drift" logic.

### Option B: Use Robust Z-Score (MAD)

**Description**: Use Median and MAD on the sliding window.

**Pros**: Tolerates outliers in history (up to 50%).
**Cons**: Slightly more complex calculation (sorting required).

### Comparison Matrix

| Criteria | Option A (MAD Z-Score) | Option B (Percent Deviation + Clean Baseline) |
|----------|------------------------|------------------------------------------------|
| Robustness | High | Extreme |
| Adaptability | Medium | High (via Consecutive Reset) |
| Intuitive Threshold | Low (Z-score values) | High (Percent change) |
| False Positive Rate | Medium (Strict near 0) | Low |
| **Recommendation** | | **Option B** |

## 5. Recommendations

### Primary Recommendation

Implement **Stable Baseline Percent Deviation**.
- Track Width and Height independently.
- **Baseline**: Median of the last 10 **NORMAL** samples.
- **Threshold**: 12.0% deviation (Configurable).
- **Rule**: $Abnormal = (W_{dev} > 12\%) \lor (H_{dev} > 12\%)$.
- **Pollution Prevention**: If Abnormal, do NOT add to history.

### Secondary Recommendations

- Update `ApplicationConfiguration` to rename `ZScoreThreshold` to `AbnormalPercentThreshold` (or maintain mapping for compatibility).
- Set default threshold to **12.0**.

---

## 6. Configuration Binding Analysis

### 6.1 Current Status Verification
**User Request**: "Values are hardcoded, change them to load from options."
**Code Investigation (`AbnormalDetectorService.cs`)**:
- **Z-Score Threshold**: mapped to `config.MatchingSettings.ZScoreThreshold` (Default: 2.0). 
  - *Status*: **Already Linked**.
- **Window Size**: mapped to `config.MatchingSettings.AbnormalDetectionWindowSize` (Default: 10).
  - *Status*: **Already Linked**.
- **MinSamples**: Hardcoded to `5` (`MinSamplesDefault`).
  - *Status*: **Hardcoded** (but not exposed in UI).

### 6.2 Performance Issue Identified
The `AddAndCheckImage` method calls `RefreshConfiguration()` **every time** an image is processed:
```csharp
public (bool, double?, double?) AddAndCheckImage(...)
{
    RefreshConfiguration(); // <- Loads JSON from disk EVERY TIME!
    lock (_lock) { ... }
}
```
- **Impact**: High I/O overhead. In a high-speed production line, reading `config.json` for every frame is a performance bottleneck.
- **Fix**: Use `ConfigurationChanged` event or Singleton config injection.

### 6.3 Conclusion
- The user's assumption ("hardcoded") is partially incorrect; the values *are* read from config.
- However, the implementation is inefficient.
- **Action Item**: 
    1. Confirm `SettingsDialogViewModel` saves to the same path (Verified: uses `ConfigurationManager`).
    2. Refactor `AbnormalDetectorService` to load config only once or when changed.
