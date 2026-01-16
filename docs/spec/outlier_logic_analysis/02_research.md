# Outlier Logic Research

## 1. Overview
The system employs two distinct types of "outlier" or "anomaly" detection logic:
1.  **Image Dimension Anomaly Detection**: Identifies images with inconsistent dimensions (Width/Height) compared to the recent history.
2.  **NIR Spectrum Filtering**: Determines whether a spectrum file represents valid data ("Seaweed") or noise/background, triggering file retention or deletion.

## 2. Image Dimension Anomaly Detection
Implemented in `ChronoView.Core.Analytics.AbnormalDetectorService`.

### 2.1 Algorithm
- **Method**: Z-Score (Standard Score) analysis.
- **Windowing**: Uses a sliding window of the most recent samples.
- **Parameters**:
    - `WindowSize`: 100 samples (default).
    - `MinSamples`: 10 samples (minimum required to start detection).
    - `Threshold`: 3.0 (Z-score limit).

### 2.2 Logic Flow
1.  **Data Collection**: Buffers `width` and `height` of incoming images.
2.  **Z-Score Calculation**:
    $$ Z = \frac{x - \mu}{\sigma} $$
    Where $\mu$ is mean and $\sigma$ is standard deviation of the buffer.
3.  **Determination**:
    - If $|Z_{width}| > 3.0$ OR $|Z_{height}| > 3.0$, the image is flagged as **Abnormal**.
4.  **Group Handling**:
    - A FileGroup is also considered abnormal if it is "NIR-only" (Has NIR data but no Camera/Normal data).

### 2.3 Usage
- Injected into `MainWindowViewModel` -> `FileGroupViewModel`.
- Used to set the `IsAbnormal` property on `FileGroupViewModel`, likely triggering visual indicators in the Dashboard UI.

## 3. NIR Spectrum Filtering
Implemented in `ChronoView.Core.Nir.NirSpectrumFilter`.

### 3.1 Goal
To filter out "empty" or "noise" data (Non-Seaweed) from valid Seaweed data.

### 3.2 Algorithm
Uses a **5-Criteria Scoring System** based on statistical properties of the spectral intensity values.

#### Pre-processing
- **Wavelength Filter**: Only analyzes data within the range **4500nm ~ 6500nm**.
- **Sliding Window**: Analyzes specific windows within the data (Size: 800, Stride: 100).

#### Criteria
| Criterion | Metric | Threshold | Condition |
|-----------|--------|-----------|-----------|
| 1. `y_range` | Global Max - Min | `0.035` | $\ge$ |
| 2. `y_std` | Global Std Dev | `0.010` | $\ge$ |
| 3. `window_800_mean` | Avg of Window Ranges | `0.025` | $\ge$ |
| 4. `window_800_std` | Std Dev of Window Ranges | `0.010` | $\ge$ |
| 5. `window_800_max` | Max of Window Ranges | `0.035` | $\ge$ |

### 3.3 Pass Condition
- **Pass**: At least **4 out of 5** criteria must be met.
- **Fail**: Fewer than 4 criteria met.

### 3.4 Usage in Workflow
Implemented in `ChronoView.Core.ProgramLaunching.Nir2CameraLauncher`.

1.  **Monitoring**: Watches for new `.txt` files in the configured monitor path.
2.  **Analysis**: Runs `NirSpectrumFilter.AnalyzeSpectrum`.
3.  **Action**:
    - **Pass (Seaweed Found)**:
        - **MOVE** `.txt` file to destination.
        - **MOVE** paired `.spc` file to destination.
    - **Fail (No Seaweed)**:
        - **DELETE** `.txt` file.
        - **DELETE** paired `.spc` file.

## 4. Summary Table

| Feature | Target | Method | Action on Detection |
|---------|--------|--------|---------------------|
| **Abnormal Detector** | Image Dimensions | Z-Score (> 3.0) | UI Flag (Visual Warning) |
| **NIR Spectrum Filter** | Spectral Data Validity | 5-Criteria Statistics | File Move (Pass) / Delete (Fail) |
