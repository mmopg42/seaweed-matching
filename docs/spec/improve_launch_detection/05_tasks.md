---
Task: improve_launch_detection
Created: 2026-01-14
Status: Draft
Depends On: 03_plan.md
---

# Improve Launch Detection - Implementation Tasks

## Phase 1: Core Implementation

### 1.1 Process Helper

- [ ] Create file: `ChronoView/Helpers/ProcessExtensions.cs`
- [ ] Implement `WaitForWindowAsync` method
  - [ ] Implement `Task.WhenAll` logic (Detection + MinDelay)
  - [ ] Implement `WaitForInputIdle`
  - [ ] Implement looping check for `MainWindowHandle`
  - [ ] Handle `CancellationToken` and timeouts

**Verify**: Code compiles.

---

## Phase 2: Refactoring Launchers

### 2.1 General Camera Launcher

- [ ] Modify `ChronoView/Core/ProgramLaunching/GeneralCameraLauncher.cs`
- [ ] Update `LaunchAsync` to usage `WaitForWindowAsync`
- [ ] Ensure `process` object is valid before calling extension

**Verify**: Launch "General Camera" -> Wait persists 1s+.

---

### 2.2 NIR Camera 1 Launcher

- [ ] Modify `ChronoView/Core/ProgramLaunching/NirCameraLauncher.cs`
- [ ] Update `LaunchAsync` to usage `WaitForWindowAsync`

**Verify**: Launch "NIR Camera" -> Wait persists 1s+.

---

### 2.3 NIR Camera 2 Launcher

- [ ] Modify `ChronoView/Core/ProgramLaunching/Nir2CameraLauncher.cs`
- [ ] Update `LaunchAsync` to usage `WaitForWindowAsync`

**Verify**: Launch "NIR Camera 2" -> Wait persists 1s+.

---

## Phase 3: Verification

### 3.1 Functional Testing

- [ ] Run Application
- [ ] Launch each camera
- [ ] Observe "Wait..." indicator duration (should not be instant)
- [ ] Verify actual program window appears
- [ ] Test with simulated "fast launch" (e.g. Notepad) if possible, or rely on actual apps.

### 3.2 Regression Testing

- [ ] Verify "Stop" button works correctly (Process termination)
- [ ] Verify re-launch works correctly

---

## Completion Log

| Task | Completed | Notes |
|------|-----------|-------|
| Plan | [Date] | |
| Implementation | | |
| Verification | | |
