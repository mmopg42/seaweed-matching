# Debugging Delete Logic - Implementation Plan

## Goal Description
Fix the issue where attempting to delete only selected components (e.g., only "Normal" data) from a selected row results in the entire row (all data) being deleted. This is caused by `MainWindowViewModel` logic conflating `IsSelected` (Row Selection) with "Full Delete".

## User Review Required
- **Breaking Change**: The definition of "Full Delete" effectively changes. Previously, selecting a row meant "Delete Everything". Now, selecting a row means "Include this row for deletion", and what actually gets deleted depends on the child checkboxes.
- **Behavior Change**: Unchecking a child component content inside a selected row will NO LONGER optionally unselect the row. The row remains selected unless explicitly unchecked.

## Proposed Changes
### UI Logic
#### [MODIFY] FileGroupViewModel.cs
- **Decouple Child-to-Parent Selection**:
    - `IsSelected` (Row) setter: Propagates value to all children (Convenience).
    - Child setters (`IsNormalSelected`, etc.): Do NOT update `IsSelected`. They only update their own state.
- **Refactoring**:
    - Introduce `SetChildProperty<T>` helper to avoid code duplication in child properties.
- **New Properties**:
    - Add `IsFullySelected` (Calculated): Returns true if all available components are selected.
    - Add `HasAnySelectedComponent` (Calculated): Returns true if any component is selected.

#### [MODIFY] MainWindowViewModel.cs
- **Update `ExecuteDeleteWithConfirmation`**:
    - **Filtering**: Only consider groups where `IsSelected == true`. (User Constraint: Row selection determines participation).
    - **Classification**:
        - "Full Delete": `g.IsSelected && g.IsFullySelected`.
        - "Partial Delete": `g.IsSelected && !g.IsFullySelected && g.HasAnySelectedComponent`.
    - Update the Confirmation Message login to reflect these precise counts.

## Verification Plan

### Automated Tests
- **[NEW] Unit Test in `ViewModelTests.cs`**:
    - `FileGroupViewModel_PartialSelection_IndependentOfParent`:
        - Set `IsSelected = true` -> Assert all children true.
        - Set `IsNormalSelected = false`.
        - Assert `IsSelected` REMAINS `true`.
        - Assert `IsNormalSelected` is `false`.
    - `MatchingStats` logic verification (if applicable).

### Manual Verification
- **Launch Application**: `dotnet run`.
- **Scenario 1: Partial Delete**:
    - Select Row (All Check).
    - Uncheck "Normal".
    - Click Delete.
    - Confirm Message says "Partial Delete".
    - Confirm Row remains selected in UI (if not deleted entirely).
    - Confirm Log: "Deleting NIR, Cam1..." (Normal skipped).
- **Scenario 2: Row Exclusion**:
    - Manually check "Normal", "NIR" on an unchecked row.
    - Click Delete.
    - Expected: NOTHING happens (or message says 0 items).
