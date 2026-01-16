# Outlier Logic Investigation Report

## 1. Issue Description
User reported that outlier detection (AbnormalDetectorService) is not working as expected for a test dataset (`.../20260108/normal`) where outliers occur with 50% probability from the 10th image onwards.

## 2. Investigation Analysis

### 2.1 Simulation Results
We simulated the `AbnormalDetectorService` logic (Window=100, MinSamples=10, Threshold=3.0) against the provided dataset.

**Findings:**
1.  **Initialization Phase (1-10)**: 
    - The detector correctly skips judgment for the first 10 samples (MinSamples=10).
    - Status: `SKIP` (Correct).
    
2.  **Detection Phase (11+)**:
    - The simulation showed that images identified by the user as "outliers" were **NOT consistency flagged as Abnormal** by the system.
    - **Reason 1: Z-Score Threshold**: The default threshold of `3.0` (3 Sigma) is statistically very conservative. It requires the value to deviate significantly from the mean relative to the standard deviation.
        - Example: If typical variation is ±16px (StdDev ≈ 15), an anomaly must be ±45px away from the mean to trigger.
        - If the "outlier" is only slightly different (e.g., 20-30px difference), it yields a Z-score of ~1.3 to 2.0, which is considered **Normal** by the current logic.
    - **Reason 2: Variance Normalization (Potential)**: If specific sequences of identical images occur, the variance drops to 0. In this state, the system defines `Z-Score = null` and defaults to **Normal**. This means the *first* major outlier after a stable period is **ignored** because it's impossible to calculate how "abnormal" it is without a non-zero historical variance.

### 2.2 Data Observation
The dataset contained width variations in the range of 1936px ~ 1968px for the "normal" set.
- Mean Width: ~1960 px
- Std Dev: ~15 px
- **Outlier Threshold**: |Val - 1960| > (3 * 15) => Deviation > 45px.
- Any image with Width between **1915px and 2005px** is considered **Normal**.

## 3. Conclusion
The current logic is functioning *as designed*, but the design parameters (Threshold=3.0) or the mathematical approach (Z-score with 0-variance handling) may not match the user's *expectation* for testing or specific operational targets.

## 4. Recommendations
1.  **Adjust Threshold**: Lower the Z-Score threshold (e.g., from `3.0` to `2.0` or `1.5` aka 95% or 87% confidence interval) to make detection more sensitive.
2.  **Absolute Difference Check**: Add a fallback "Absolute Difference" check.
    - Example: If `abs(current - mean) > 50px`, flag as abnormal regardless of Standard Deviation. This prevents the "Zero Variance" loophole where obvious outliers are missed because the history was *too* perfect.
3.  **Configurability**: Expose `Threshold` and `Method` options in the UI `Settings` so users can tune sensitivity without code changes.
