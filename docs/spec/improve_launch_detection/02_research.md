---
Task: improve_launch_detection
Created: 2026-01-14
Status: Draft
Depends On: 01_requirements.md
---

# Improve Launch Detection - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: How to detect when app is ready? | Combine `WaitForInputIdle` and `MainWindowHandle` check. | High |
| Q2: Is `WaitForInputIdle` sufficient? | No, it only indicates message loop start, not window visibility. | High |
| Q3: How to guarantee "Wait" visibility? | Enforce a minimum delay (e.g., 1000ms) alongside detection. | High |
| Q4: Will Python console be mistaken for GUI? | Yes, `WaitForInputIdle` triggers on console. Need Title check. | High |

## 2. Detailed Findings

### 2.1 Q1 & Q2: Detection Strategy

**Method**: 
- Analyzed `.NET Process` class documentation.
- Reviewed standard patterns for external automation (e.g. UI Automation).

**Findings**:
- `Process.Start()` returns immediately after process creation.
- `WaitForInputIdle()` waits until the thread is idle (message pump started). This is the first signal of life.
- `MainWindowHandle` property is cached and zero until the window is created. It requires `Process.Refresh()` loops to detect handle creation.

**Conclusion**:
To ensure "Visible Initialization":
1. Call `WaitForInputIdle()`.
2. Loop/Poll until `MainWindowHandle` is non-zero (indicating window exists).

### 2.2 Q3: User Perception

**Method**: UX standard practice review.

**Findings**:
- Even if startup is instant (e.g. 100ms), a momentary flash of "Wait" is confusing.
- A "stable" Wait state (e.g. minimum 500ms-1000ms) confirms to the user that the command was received and is processing.

**Conclusion**:
- Explicitly add `Task.Delay(1000)` (concurrently) to ensure the Wait state is readable.

### 2.3 Q4: Python Console vs GUI

**Method**: Process handle behavior analysis.

**Findings**:
- For `python.exe` (not `pythonw.exe`), the process owns a Console Window.
- `MainWindowHandle` will likely point to this Console Window as soon as it appears.
- `WaitForInputIdle` will return as soon as the Console is ready, which is often before the GUI (PyQt/Tkinter) appears.
- **Risk**: "Wait" might disappear when Console appears, not when GUI appears.

**Conclusion**:
- Simple handle checking is insufficient for Console+GUI apps.
- **Solution**: Check `MainWindowTitle` to differentiate (e.g., wait until title contains "Camera").
- **Fallback**: The 1000ms `MinDelay` mitigates this by masking the split-second gap between Console and GUI.

## 3. Code Analysis

### 3.1 Relevant Existing Code

| File | Component | Relevance | Notes |
|------|-----------|-----------|-------|
| `GeneralCameraLauncher.cs` | `LaunchAsync` | Current implementation | Uses `Process.Start` without wait |
| `NirCameraLauncher.cs` | `LaunchAsync` | Current implementation | Needs identical update |
| `Nir2CameraLauncher.cs` | `LaunchAsync` | Current implementation | Needs identical update |

## 4. Recommendations

### Primary Recommendation

Refactor `LaunchAsync` to usage a **Hybrid Detection** approach:

1. **Set Initial State**: `Starting` (Wait...)
2. **Launch**: `Process.Start()`
3. **Wait Task**: `Task.WhenAll` of:
   - **Detection**: 
     - Basic: `WaitForInputIdle` + Loop `MainWindowHandle > 0`
     - **Advanced**: Loop until `MainWindowTitle` contains expected string (if configured) or matches known GUI signature (vs Console).
   - **Min Delay**: `Task.Delay(1000)` (Minimum visibility guarantee)
4. **Final State**: `Running` (Stop)

This guarantees the user sees "Wait..." for at least 1 second AND until the window actually appears.

### Risks to Address

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Infinite Loop (Window never appears) | Low | High | Add timeout (e.g. 10s) to the loop |
| Console App (No Window) | Low | Medium | Configurable "WaitStrategy" (fallback to just delay) |
| Console vs GUI confusion | Medium | Medium | **Wait for specific Window Title** if critical. Otherwise rely on 1s MinDelay. |

## 5. Unanswered Questions

None.
