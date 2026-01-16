---
Task: improve_launch_detection
Created: 2026-01-14
Status: Completed
Depends On: 05_tasks.md
---

# Improve Launch Detection - Report

## 1. Summary

Implemented the hybrid launch detection strategy to ensure "Wait..." state persists until external programs are visibly initialized. Created a reusable `ProcessExtensions.WaitForWindowAsync()` helper method and applied it to all 3 camera launchers.

## 2. Changes Implemented

### 2.1 New Component

| File | Purpose |
|------|---------|
| `ChronoView/Helpers/ProcessExtensions.cs` | Extension method `WaitForWindowAsync` that combines detection + minimum delay |

**Detection Logic**:
1. `WaitForInputIdle()` - Wait for message loop start
2. Loop polling `MainWindowHandle` until non-zero (window created)
3. `Task.Delay(1000)` - Minimum 1 second visibility guarantee

Uses `Task.WhenAll()` to execute detection and minimum delay in parallel, completing when **both** are done.

### 2.2 Modified Components

| File | Change |
|------|--------|
| `GeneralCameraLauncher.cs` | Added `WaitForWindowAsync` call after `Process.Start()` |
| `NirCameraLauncher.cs` | Same as above |
| `Nir2CameraLauncher.cs` | Same as above |

## 3. Verification Results

### 3.1 Build Verification
- **Status**: Passed
- **Output**: 0 Errors, 14 Warnings (unrelated)

### 3.2 Expected Behavior
- User clicks "Start" → Button shows "Wait..." (Orange) for at least 1 second
- After program window appears AND 1 second elapsed → Button shows "Stop" (Red)

## 4. Next Steps

- Manual testing with actual camera programs to verify user experience.
