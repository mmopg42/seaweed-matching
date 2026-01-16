---
Task: implement_script_based_abnormal_detection
Created: 2026-01-12
Status: Draft
Summary: Detailed design for AdaptiveAbnormalDetector (C#)
---

# Adaptive Abnormal Detection - Design

## 1. System Architecture

### 1.1 Overview
The system uses an **Internal Statistical Engine**.
It identifies the "Context" of an image (e.g., "Cam1_Line2") from its file path. It maintains a moving window of recent image dimensions for each context.
**Target Image**: The analysis applies strictly to the `MainImagePath` (Normal/Stitched Image), not individual camera raw files.

## 2. Logic Design

### 2.1 Context Identification
- **Strategy**: Regex match on `FileGroup.NormalFolder`.
- **Primary Pattern**: `(Line[12]).*(Cam[1-6])` (e.g., "Line1_Cam1").
- **Fallback**:
  - If distinct "Line" or "Cam" found, use that (e.g., "Cam1").
  - Else: "Global".
- **Key Format**: `<Line>_<Camera>` (e.g., "Line1_Cam1").

### 2.2 Z-Score Algorithm
- **Inputs**: `ZScoreThreshold` (Cfg), `AbnormalDetectionWindowSize` (Cfg).
- **Process**:
  1. Calculate Mean/StdDev of History.
  2. If History count < MinSamples (5), return Normal (building baseline).
  3. `Z = (Value - Mean) / StdDev`.
  4. If `abs(Z) > Threshold`, return Abnormal.

### 2.3 NIR-only Detection (Preserved Logic)

This logic is **independent of Z-score** and runs **first** in `IsGroupAbnormal`:

```csharp
bool hasCamera = !string.IsNullOrEmpty(group.NormalFolder) || 
                 (group.CameraFiles?.Count > 0);
bool hasNir = group.HasNir && !string.IsNullOrEmpty(group.NirKey);

if (!hasCamera && hasNir)
{
    return true; // NIR-only group detected
}
// Return false (Normal) if basic structure is OK.
// Detailed image analysis is done via AddAndCheckImage.
```

## 3. Class Design

### 3.1 AbnormalDetectorService (Existing)
- **Dependencies**: `IConfigurationManager`, `ILogger`, `AbnormalHistoryManager`.
- **Interfaces**: `IAbnormalDetector`.
- **Method**: `AddAndCheckImage` (Synchronous), `IsGroupAbnormal` (Synchronous).
- **Optimization**: Expects `FileGroup.MainImagePath` to be accurate.

### 3.3 GroupManager (Core)
- **Responsibility**: When creating a group, if `MainImagePath` doesn't exist, search recursively in `NormalFolder`.
- **Logic**: 
  ```csharp
  if (!File.Exists(group.MainImagePath)) {
      var found = Directory.GetFiles(group.NormalFolder, "stitched_original.png", SearchOption.AllDirectories).FirstOrDefault();
      if (found != null) group.MainImagePath = found;
  }
  ```

### 3.2 AbnormalHistoryManager
- **Data**: `Dictionary<string, List<SizeData>>`.
- **Persistence**: `Newtonsoft.Json`.
- **Path**: `%APPDATA%\ChronoView\abnormal_history.json`.



## 4. Configuration
- `AbnormalDetectionWindowSize`: int (10).
- `ZScoreThreshold`: double (3.0) [Existing Property reused].

## 5. UI Updates
- **Settings**: Slider for `ZScoreThreshold` (1.0 - 5.0) and `WindowSize` (5 - 50).
