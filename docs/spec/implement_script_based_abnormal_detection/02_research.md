---
Task: implement_script_based_abnormal_detection
Created: 2026-01-12
Status: Draft
Summary: Research on stateful C# adaptive logic
---

# Adaptive Abnormal Detection - Research

## 1. Logic Transition: C# Native
- **Decision**: Logic moves from Python to C#.
- **Requirement**: Maintain moving window of last 10 samples per context (Folder/Line).

## 2. Technical Decisions

### 2.1 State Management
- **Persistence**: `State/abnormal_history.json` (or in AppData).
- **Structure**:
  ```json
  {
    "Lines": {
      "Cam1": { "History": [2400, 2405, 2395, ...], "LastUpdated": "..." },
      "Cam2": { ... }
    }
  }
  ```
- **Context Key**: derived from `FileGroup.NormalFolder` text (e.g., regex to capture "Cam1", "Line1"). If parsing fails, fallback to "Default".

### 2.2 Algorithm (Z-Score)
- **Mean (μ)**: Average of last N samples.
- **StdDev (σ)**: Standard deviation.
- **IsAbnormal**: `abs(Value - μ) > Threshold * σ`.
- **Threshold**: Default 3.0.
- **MinSamples**: Minimum 5 samples before enabling detection (to build baseline).

### 2.3 Implementation Details
- **Dependency**: `Newtonsoft.Json` or `System.Text.Json`.
- **Locking**: `ReaderWriterLockSlim` or simple `lock` since UI is single process. 
- **Async**: Disk I/O should be async.

## 3. Class Design
- `AdaptiveAbnormalDetector` : `IAbnormalDetector`
- `AbnormalHistoryManager`: Handles loading/saving JSON.
