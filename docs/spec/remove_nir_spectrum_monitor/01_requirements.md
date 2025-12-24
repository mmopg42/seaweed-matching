---
Task: remove_nir_spectrum_monitor
Created: 2025-12-23
Status: Draft
Summary: Remove redundant NirSpectrumMonitor class in favor of NirSpectrumFilter
Research Required: No
---

# Remove NIR Spectrum Monitor - Requirements

## 1. Goal

### 1.1 Primary Goal

Remove the redundant `NirSpectrumMonitor` class and all its dependencies, consolidating NIR filtering functionality into `NirSpectrumFilter`.

### 1.2 Success Criteria

- [ ] `NirSpectrumMonitor.cs` file is deleted
- [ ] DI registration for `NirSpectrumMonitor` is removed from `App.xaml.cs`
- [ ] All references to `NirSpectrumMonitor` in `SetupWindowViewModel.cs` are removed
- [ ] Application builds successfully without errors
- [ ] Existing NIR filtering functionality via `NirSpectrumFilter` remains intact
- [ ] No unused code remains related to `NirSpectrumMonitor`

## 2. Constraints

### 2.1 Technical Constraints

- Must not modify `NirSpectrumFilter` which contains the actual implementation
- Must not affect existing NIR filtering functionality through `Nir2CameraLauncher`
- Must maintain all existing functionality visible to users

### 2.2 Business Constraints

- This is a pure code cleanup task with no user-visible changes
- Must complete quickly (estimated 30 minutes)

### 2.3 Non-Goals (Out of Scope)

- Modifying `NirSpectrumFilter` implementation
- Changing NIR filtering logic or algorithms
- Refactoring other NIR-related classes
- Modifying `Nir2CameraLauncher` which uses `NirSpectrumFilter`

## 3. Background

### Problem Statement

The codebase contains two NIR-related classes with confusing names:

1. **`NirSpectrumFilter`** (ACTIVE): Contains actual filtering logic ported from Python, with methods to analyze spectrum data and detect seaweed presence. Used by `Nir2CameraLauncher`.

2. **`NirSpectrumMonitor`** (DEAD CODE): Contains only empty TODO methods with no actual implementation. Injected into `SetupWindowViewModel` but never called (the method `ExecuteNirSpectrumMonitorAsync` exists but has no command binding).

### Evidence of Redundancy

**NirSpectrumMonitor.cs (Lines 19-45)**:
- Only contains TODO comments and placeholder logging
- No actual business logic implemented
- Method `StartMonitoringAsync()` just logs "TODO: Implement NIR spectrum monitoring logic here"

**SetupWindowViewModel.cs**:
- Line 28: Field `_nirSpectrumMonitor` declared
- Line 53: Injected via constructor
- Line 185-212: Method `ExecuteNirSpectrumMonitorAsync()` defined but **never called**
- No command binding exists for NIR Spectrum Monitor functionality

**App.xaml.cs**:
- Line 217: Registered as singleton in DI container but serves no purpose

### Why NirSpectrumFilter is the Correct Implementation

- Contains complete filtering algorithm (sliding window analysis, Y-variation detection)
- Actively used by `Nir2CameraLauncher` for real filtering operations
- Has proper data structures (`FilterResult`, `YVariationRegion`)
- Properly integrated with `NirSpectrumParser`

## 4. Assumptions

- The original intention was to implement monitoring logic but it was never completed
- `NirSpectrumFilter` successfully handles all NIR-related functionality
- No other parts of the codebase depend on `NirSpectrumMonitor` (verified via grep search)
- The dead code was left behind during development and forgotten

## 5. Dependencies

### 5.1 Blocked By

None - this is a standalone cleanup task.

### 5.2 Blocks

None - this does not block other work but improves code maintainability.

---

## Approval

- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
- [ ] Scope boundaries are clear
- [ ] All blocking dependencies identified

**Next Step**: 03_plan.md (skipping 02_research as no investigation needed)
