---
Task: improve_launch_detection
Created: 2026-01-14
Status: Draft
Depends On: 01_requirements.md, 02_research.md
---

# Improve Launch Detection - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| Persist "Wait" until MainWindowHandle non-zero | `ProcessExtensions.WaitForWindowAsync` loop | Debugger breakpoint check |
| Persist "Wait" until WaitForInputIdle | `ProcessExtensions.WaitForWindowAsync` call | Launch app, verify delay |
| Persist for at least 1000ms | `Task.Delay(1000)` in Helper | Stopwatch timing of "Wait" state |
| Handle Launch Failure | Existing `try-catch` blocks | Simulate failure, check state reversion |
| Applies to all 3 launchers | Refactor in 3 Launcher classes | Code review |

## 1. Architecture Overview

### 1.1 System Context

Refactoring the launch logic in `*Launcher` classes to include a "Wait for Visibility" step. This introduces a new reusable helper method.

### 1.2 Logic Flow

```
[UI Command]
    │
    ▼
Set State = Starting (Wait...)
    │
    ▼
Process.Start() ──► [External App Starts]
    │
    ▼
Task.WhenAll (Parallel Execution)
 ┌───────────────────────┬────────────────────────┐
 │ Detection Task        │  Minimum Delay Task    │
 │ 1. WaitForInputIdle   │  1. Wait 1000ms        │
 │ 2. Poll WindowHandle  │                        │
 └───────────┬───────────┴───────────┬────────────┘
             └───────────┬───────────┘
                         ▼
Set State = Running (Stop)
```

## 2. Components

### 2.1 New Components

| Component | Type | Location | Purpose |
|-----------|------|----------|---------|
| `ProcessExtensions` | Class (Static) | `ChronoView/Helpers/ProcessExtensions.cs` | Provides `WaitForWindowAsync` method |

### 2.2 Modified Components

| Component | Location | Changes |
|-----------|----------|---------|
| `GeneralCameraLauncher` | `Core/ProgramLaunching/` | Update `LaunchAsync` to usage `WaitForWindowAsync` |
| `NirCameraLauncher` | `Core/ProgramLaunching/` | Same as above |
| `Nir2CameraLauncher` | `Core/ProgramLaunching/` | Same as above |

## 3. Interface Definitions

### 3.1 ProcessExtensions

```csharp
public static class ProcessExtensions
{
    /// <summary>
    /// Waits for the process to open a window and be ready for input, 
    /// with a guaranteed minimum duration for UI stability.
    /// </summary>
    public static async Task WaitForWindowAsync(
        this Process process, 
        int minDurationMs = 1000, 
        int timeoutMs = 10000, 
        CancellationToken cancellationToken = default);
}
```

## 4. Key Design Decisions

### 4.1 Hybrid Wait Strategy

**Context**: Users perceive "Wait" disappearing too fast as a glitch. `Process.Start` is instantaneous.

**Options**:
A. Just `WaitForInputIdle`: Too fast for small apps, might confuse user.
B. Just `Task.Delay(1000)`: Good UX, but might be shorter than actual load time.
C. Hybrid (`Max(Detection, Delay)`): Best of both worlds.

**Decision**: Option C (Hybrid)

**Rationale**: Guarantees visibility (UX) while ensuring the app is actually ready (Logic). The '1000ms' acts as a visual debouncer.

### 4.2 Reusable Extension Method

**Context**: 3 different Launchers share the same logic.
**Decision**: Extract to `ProcessExtensions`.
**Rationale**: Avoid code duplication and centralize the "Detection" logic (which might get complex with Python consoles).

## 5. Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Python Console mistaken for GUI | Low | `WaitForInputIdle` returns early. `MinDelay(1000)` mitigates the visual glitch. |
| Process exits during wait | Medium | `WaitForWindowAsync` should handle `HasExited` gracefully. |

## 6. Open Questions

none.
