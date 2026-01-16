# Fix Delete Logic Requirements

## 1. Problem Statement
Currently, the `FileGroupViewModel` has a logic flaw where selecting or deselecting the Group (Row) propagates the selection state to all child components (Normal, NIR, Cams). Conversely, there is no logic to handle partial deselection of children without re-triggering the parent's propagation logic, leading to an inability to perform partial deletions.

## 2. Goals
- **Enable Partial Selection**: Users must be able to select/deselect individual components (e.g., just "Normal") without the parent row forcing all other components to match that state.
- **Correct Parent State**: The parent row's `IsSelected` state should reflect the aggregate state logically, or at least not interfere with partial selections.
- **Partial Deletion**: The system must support deleting only the selected components.

## 3. Scope
- **Component**: `FileGroupViewModel.cs`
- **Feature**: Selection Logic (`IsSelected`, `IsNormalSelected`, etc.)

## 4. Constraints
- Must not break existing "Select All" functionality (Row click).
- Must be verified with manual tests and unit tests.
