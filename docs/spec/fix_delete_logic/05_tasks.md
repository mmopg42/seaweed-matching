# Task: Fix Delete Logic

- [/] Create Spec Documents
    - [x] Requirements
    - [x] Plan
    - [x] Design
- [ ] Implement Fix
    - [ ] Add `SetChildProperty` helper in `FileGroupViewModel`
    - [ ] Update `IsSelected` setter to decouple from children (Master Switch)
    - [ ] Update Child setters to update local state only
    - [ ] Add `IsFullySelected` and `HasAnySelectedComponent` properties
    - [ ] Update `MainWindowViewModel.ExecuteDeleteWithConfirmation` to use new logic
- [ ] Verify
    - [ ] Add Unit Test: `IsSelected_SetsChildren`
    - [ ] Add Unit Test: `ChildUncheck_DoesNotAffectParent`
    - [ ] Manual Verification: Partial Delete Scenario
