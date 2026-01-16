# Plan: Line Separation Fix

## Goal
Strictly separate the grouping and matching logic for "Line 1" and "Line 2" to prevent cross-line matching and sequence enforcement errors.

## User Review Required
> [!IMPORTANT]
> This change enforces strict isolation. If there were any "intentional" cross-line features (e.g., using a camera from Line 1 as a fallback for Line 2), they will be disabled.

## Proposed Changes

### Core Logic
#### [MODIFY] [GroupManager.cs](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/Core/FileWatching/GroupManager.cs)
- **State Management**:
    - Change `_lastAssignedGroup` from `Dictionary<DataType, string>` to `Dictionary<(DataType, int), string>` to track sequence separately for each line.
- **Matching Logic**:
    - In `FindMatchingExistingGroup`, strictly filter candidates by `LineNumber`.
    - Ensure `DetermineLineNumber` is called and respected *before* any matching attempts.
    - Update logging to explicitly include `[Line X]` prefix for all operations.
- **Clean Up**:
    - Update `RemoveGroup` and `Clear` methods to respect the new state structure if necessary.

#### [VERIFY] [NormalFolderHelper.cs](file:///c:/workspace/seaweed/gui_kiro_v2/ChronoView/Helpers/NormalFolderHelper.cs)
- Verify that `DetermineLineNumber` identifies `_0` as Line 1 and `_1` as Line 2 correctly. (Confirmed in research)

## Verification Plan

### Automated Tests
- **New Test**: `ChronoView.Tests/Core/FileWatching/GroupManagerReferenceTests.cs` (or similar)
    - **Scenario 1 (Line Isolation)**:
        1. Create `Line1` Group (Normal `_0`).
        2. Simulate `Line2` Camera file arrival.
        3. Assert that `Line2` Camera does NOT match `Line1` Group, even if timestamps are close.
        4. Assert that `Line2` Camera creates a new `Line2` Group.
    - **Scenario 2 (Sequence)**:
        1. Create `Group A` (Line 1).
        2. Create `Group B` (Line 2).
        3. Create `Group C` (Line 1).
        4. Verify that `Group C` checks sequence against `Group A` (Line 1), ignoring `Group B` (Line 2).

### Manual Verification
1. **Setup**:
    - Prepare test folders `normal1` and `normal2`.
    - Drop `C..._0` folder into `normal1`.
    - Drop `C..._1` folder into `normal2`.
    - Drop `Cam4` file (Line 2) with timestamp matching `_0` (Line 1 group).
2. **Execution**:
    - Launch Application.
    - Observe Logs.
3. **Expected Result**:
    - `Cam4` should NOT match `_0` group.
    - `Cam4` should match `_1` group (if exists and valid) or create new `Line 2` group.
    - Logs should show `[Line 1] ...` and `[Line 2] ...` distinctly.
