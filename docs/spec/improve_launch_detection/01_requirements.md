---
Task: improve_launch_detection
Created: 2026-01-14
Status: Draft
Summary: Improve external program launch detection to ensure 'Wait' state persists until program is ready.
Research Required: Yes
---

# Improve Launch Detection - Requirements

## 1. Goal

### 1.1 Primary Goal

The "Wait" (Starting) state in the Camera Status UI must persist until the external program is visibly initialized (Main Window appeared and active), ensuring the user clearly perceives that the program execution has completed.

### 1.2 Success Criteria

- [ ] "Wait" state persists until the external program's main window is fully created (`MainWindowHandle` is non-zero).
- [ ] "Wait" state persists until the program is ready for input (`WaitForInputIdle`).
- [ ] "Wait" state is visible for at least 1000ms (minimum duration) to ensure user notices the transition.
- [ ] If program fails to start (e.g. crash on startup), UI correctly reverts to "Start" (Stopped) instead of getting stuck in "Stop" (Running).
- [ ] Applies to all 3 camera launchers (General, NIR 1, NIR 2).

## 2. Constraints

### 2.1 Technical Constraints

- **Process API**: Must use `.NET Process` class capabilities (`WaitForInputIdle`, `MainWindowHandle`).
- **Non-blocking**: Extensions to launch logic must remain asynchronous and not freeze the UI.
- **Compatibility**: Must work with existing camera applications (WPF/WinForms/Native Windows).

### 2.2 Non-Goals (Out of Scope)

- Modifying the external camera programs themselves.
- Deep integration (e.g. inter-process communication beyond process monitoring).

## 3. Assumptions

- External programs are GUI applications (have a message loop/window) that respond to `WaitForInputIdle`.
- The `Process` object remains valid long enough to check initialization status.

## 4. Dependencies

- None.

## Approval

- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
