# Requirements: NIR Visualization

## 1. Overview
This document defines the requirements for visualizing Near-Infrared (NIR) spectrum data in the ChronoView application. The goal is to parse `.txt` files containing waveform data and display generated graphs in the UI widgets for NIR items, which currently lack visual representation.

## 2. Data Source
*   **File Pattern**:
    *   Primary Match: `.spc` file `run_120251204T111028.spc` -> `.txt` file `run_120251204T111028A.txt` (Suffix `A` before extension).
    *   Fallback Match: `run_120251204T111028.spc` -> `run_120251204T111028.txt` (Exact name match).
    *   **Verified**: Files use lowercase prefix `run_` (not `Run_`).
*   **File Format (Verified)**:
    *   **Delimiters**: Space-separated values (multiple spaces collapsed).
    *   **Structure**: Columnar data with 2 columns: `Wavelength` (cm⁻¹, X-axis) and `Intensity` (Y-axis).
    *   **Headers**: Lines starting with `#` (Chinese characters in test data: `#时间:`, `#XUnit:`) must be skipped.
    *   **Data Range (Example)**:
        *   Wavelength: 4000.00 - 9994.16 cm⁻¹ (~1951 data points)
        *   Intensity: -0.244920 to 0.043478 (negative values possible)

## 3. Functional Requirements
1.  **Waveform Parsing**:
    *   Implement `NirSpectraParser` to read `.txt` files.
    *   **Robustness**: Must handle varying whitespace and potential header lines without crashing.
    *   **Validation**: If fewer than 10 valid data points are found, treat as invalid/empty.
2.  **Graph Generation**:
    *   **Library**: `ScottPlot 5.0.42` (Core + WPF packages) for high-performance rendering.
    *   **Output**: Static `BitmapSource` (Thumbnail, Frozen for UI thread safety).
    *   **Resolution**: Configurable in Settings (User Requirement: "Horizontally long"). Default: **250x100 pixels**.
    *   **Configuration**: Width and Height must be adjustable via UI Options.
    *   **Styling**: Minimalist
        *   Line: Thin (LineWidth=1), Blue (#0078d4)
        *   Background: Light gray (#f5f5f5)
        *   No axis labels, no grid, frameless layout
    *   **API Notes**:
        *   Use `ScottPlot.Color.FromHex()` instead of `FromARGB()` (ScottPlot 5.0 API change)
        *   Use `Plot.Add.Signal()` for efficient waveform rendering
        *   Set `signal.Data.XOffset` and `signal.Data.Period` to map wavelength range correctly
3.  **UI Display**:
    *   Async loading to preventing UI freeze.
    *   **Placeholder**: Show "Loading..." spinner or empty space while processing.
    *   **Error State**: Show a generic "Graph Unavailable" or "Error" icon if parsing fails.
4.  **Configuration**:
    *   `EnableNirGraph` (bool): Toggle in Settings. **Default: True**.
    *   If disabled, skip file reading entirely.

## 4. Non-Functional Requirements (Performance & Safety)
*   **Caching Strategy**:
    *   Implement **LRU Cache** (Least Recently Used) or a detailed `MemoryCache` for generated bitmaps.
    *   **Capacity**: Limit to ~100-200 recent graphs to prevent memory leaks during long scrolls.
    *   **Cache Key**: Full file path + Modification Timestamp.
*   **Concurrency**:
    *   All parsing/rendering must occur on `Task.Run()` (ThreadPool).
    *   Resulting Bitmaps must be `Frozen` (`Freeze()`) before passing to UI thread.
*   **Resilience**:
    *   Timeout protection (e.g., abort if parsing takes > 500ms).
    *   Corrupt file handling: catch exceptions, log warning, return null/error image.

## 5. Constraints
*   **Target OS**: Windows (WPF).
*   **Language**: C# (.NET).
*   **Safety**: If parsing fails or the file is malformed, display a placeholder error image or empty state instead of crashing.
