# NIR Filtering Logic Documentation

## 1. Overview
This document describes the algorithm determines whether an NIR spectrum data file contains valid seaweed signals ("Pass") or just background noise/empty data ("Fail"). The logic is implemented in `NirSpectrumFilter.cs`.

## 2. Pre-processing
*   **Input Data**: NIR Spectrum text file (Wavelength, Intensity).
*   **Wavelength Range**: Data is filtered to include only the range **4500 ~ 6500** (unit: cm⁻¹).
*   **Data Validation**: If no data points exist in this range, the file immediately Fails.

## 3. Filtering Criteria (5 Metrics)
The decision is based on **5 statistical metrics** calculated from the intensity values (Y-axis) in the target wavelength range.

### 3.1. Global Metrics
Calculated from the entire dataset within the target range.

1.  **y_range** (Range of Intensity)
    *   Formula: `Max(Y) - Min(Y)`
    *   Threshold: **>= 0.035**
    *   Meaning: The overall amplitude of the signal. Valid signals usually have a larger vertical span.

2.  **y_std** (Standard Deviation)
    *   Formula: Standard deviation of all Y values.
    *   Threshold: **>= 0.010**
    *   Meaning: The overall variability of the signal.

### 3.2. Sliding Window Metrics
The data is analyzed using a sliding window approach to detect local variations.
*   **Window Size**: 800 (spectral unit interval)
*   **Stride**: 100

For each window, the **Range (Max - Min)** of intensity values is calculated. Let's call this list of ranges `window_ranges`.

3.  **window_800_mean**
    *   Formula: Average of `window_ranges`.
    *   Threshold: **>= 0.025**
    *   Meaning: On average, does the signal fluctuate significantly within local windows?

4.  **window_800_std**
    *   Formula: Standard deviation of `window_ranges`.
    *   Threshold: **>= 0.010**
    *   Meaning: Does the magnitude of fluctuation vary across different parts of the spectrum? (Signals often have peaks in specific areas, increasing this value).

5.  **window_800_max**
    *   Formula: Maximum value in `window_ranges`.
    *   Threshold: **>= 0.035**
    *   Meaning: Is there at least one specific region with a very large amplitude change?

## 4. Decision Logic (Pass/Fail)
The file is considered **Valid (Pass)** if specific criteria count is met.

*   **Pass Condition**: 
    1.  **Criteria Count**: **4 or more** out of the 5 criteria must be satisfied.
    2.  **Mandatory Check**: **y_std (Standard Deviation)** must be **>= 0.010**.
    
    > **Logic**: `(Count >= 4) AND (y_std >= 0.010)`

*   **Fail Condition**: Failure to meet either of the above conditions.

## 5. Summary Table

| Metric | Calculator | Pass Threshold | Description |
| :--- | :--- | :--- | :--- |
| **y_range** | `Max - Min` (All) | `>= 0.035` | Overall Amplitude |
| **y_std** | `StdDev` (All) | `>= 0.010` | **Mandatory** Overall Variability |
| **window_mean** | `Mean` of window ranges | `>= 0.025` | Average Local Amplitude |
| **window_std** | `StdDev` of window ranges | `>= 0.010` | Variability of Local Amplitudes |
| **window_max** | `Max` of window ranges | `>= 0.035` | Maximum Local Amplitude |

> [!NOTE]
> **Update**: As of 2026-01-09, `y_std` is a mandatory requirement to filter out high-amplitude noise that lacks overall signal variation.
