# Analysis: Termination & Activation Logic Defects

## 1. Issue Overview
The user reports two critical failures despite previous "fixes":
1.  **Zombie Termination**: The system logs successful termination ("process exited"), but the target application window remains visible. It requires a second "ON -> OFF" cycle to actually close.
2.  **Activation Failure**: Clicking "No" (Do not terminate, just bring to front) fails to bring the window to the foreground, leaving it obscured.

## 2. Root Cause Analysis

### 2.1 Termination Failure (The "Zombie" Window)
**Observation**:
- Log: `Terminating General Camera (PID: 52600)` -> `General Camera deactivated (process exited)`.
- Reality: Window stays open.
- **Hypothesis A (Wrong Process)**: The PID 52600 might be a wrapper/launcher process (e.g., a splash screen or starter executable) that spawns the *real* UI process and then exits or stays idle. `Process.Kill()` kills the wrapper, but the child UI process survives.
    - *Evidence*: `drygim_viewer_251103.exe` sounds like a specific build/viewer.
- **Hypothesis B (Access Denied / Partial Kill)**: The process is running with higher privileges or in a way that `Kill()` sends a signal that is ignored or handled gracefully (unlikely for `Kill`, but possible if it's `CloseMainWindow`). The code uses `Kill()`, so this is less likely unless it's a wrapper.
- **Hypothesis C (Ghost Handle)**: The `_process` variable holds a reference to an old instance that *already* exited, while a *new* instance took its place (e.g., auto-restart).

**Verdict**: Most likely **Hypothesis A (Process Tree)**. We need to kill the process *and its children*.

### 2.2 Activation Failure (Foreground)
**Observation**: `AttachThreadInput` was implemented, but activation still fails.
- **Possible Cause A**: `MainWindowHandle` is `0`. If the target app is minimized to tray or is a multi-window app, `MainWindowHandle` might be invalid.
- **Possible Cause B**: `ShowWindow(SW_RESTORE)` is not enough. Some apps need `SW_SHOW` or `SW_SHOWMAXIMIZED`.
- **Possible Cause C**: Timing. `AttachThreadInput` might need a slight delay or retry.
- **Possible Cause D**: `SwitchToThisWindow` (legacy API) works better for stubborn windows.

**Verification**: We need to check if `MainWindowHandle` is valid in the logs.

## 3. Proposed Solutions

### 3.1 Fix Termination (Recursive Kill)
Instead of just `_process.Kill()`, use the system `taskkill` command or a recursive C# method to kill the process tree.
*Command*: `taskkill /F /T /PID {pid}`
*C#*: `_process.Kill(true)` (.NET Core 3.0+ supports `Kill(bool entireProcessTree)`).
*Action*: Update `Launcher` classes to use `_process.Kill(true)`.

### 3.2 Fix Activation (Stronger Force)
1.  **Verify Handle**: Log `MainWindowHandle` before attempting activation.
2.  **Use `SwitchToThisWindow`**: This Win32 API is often more "aggressive" than `SetForegroundWindow` for user-initiated switches.
3.  **Minimize-Restore Toggle**: Sometimes forcing Minimize then Restore triggers the window manager to re-evaluate Z-order.

## 4. Plan Updates
1.  Update `Launcher.TerminateAsync()` to use `Kill(true)` (Tree Kill).
2.  Update `WindowActivationHelper` to include `SwitchToThisWindow` and better handle checks.
