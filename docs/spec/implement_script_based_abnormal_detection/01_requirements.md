---
Task: implement_script_based_abnormal_detection
Created: 2026-01-12
Status: Draft
Summary: Implement adaptive abnormal detection (Moving Window N=10) in C#
Research Required: No
---

# Adaptive Abnormal Detection - Requirements

## 1. Goal

### 1.1 Primary Goal

Implement a **C# Native, Statistical Abnormal Detection** system.
The system will detect abnormalities based on **deviation from the recent history (last 10 samples)** maintained in a local JSON file. This replaces the absolute threshold logic and avoids external Python scripts.

### 1.2 Success Criteria

- [ ] **Deviation Logic**: Detection is based on Z-score/Deviation from the moving average of the last 10 images.
- [ ] **State Persistence**: History is saved to `abnormal_history.json` to persist across restarts.
- [ ] **Specific Case**: The reported 1936x4272 image is detected as abnormal because it deviates from recent history.
- [ ] **Native Performance**: No external process overhead. Logic runs within the application process.
- [ ] **Configurability**: Threshold (Sigma) and Window Size are configurable via `ApplicationConfiguration`.

### 1.3 Preserved Features (Must Keep)

- [ ] **NIR-only Detection**: Groups with NIR data but no camera/normal data must be flagged as abnormal. This existing logic must be preserved.

## 2. Constraints

### 2.1 Technical Constraints

- **Language**: C# (.NET 10.0).
- **Architecture**: `IAbnormalDetector` implementation (`AdaptiveAbnormalDetector`).
- **State Management**: Local JSON file. Thread-safe access required.

### 2.2 Business Constraints

- Logic should be robust against "Cold Start" (first few images).

### 2.3 Non-Goals (Out of Scope)

- Running external Python scripts.

## 3. Assumptions

- Image dimensions are sufficient for detecting this specific abnormality.
- **Target Scope**: Detection targets the `MainImage` (Normal/Stitched) only. Individual camera images are not analyzed for size deviation.
