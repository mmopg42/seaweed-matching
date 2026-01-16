# Tasks: Refine Deletion Logic

- [x] Clear Spec-Lock (05_tasks.md created) <!-- id: 1 -->
- [x] Requirements/Plan/Design updated with SSoT + efficiency notes <!-- id: 2 -->
- [ ] **Implementation Phase** <!-- id: 3 -->
    - [ ] Update `SharedResources.xaml` bindings (Mode=TwoWay, UpdateSourceTrigger=PropertyChanged) <!-- id: 4 -->
    - [ ] Update `FileGroupViewModel.cs` selection methods (Row gate in `GetSelectedComponents` + `GetSelectedComponentDetails`) <!-- id: 5 -->
    - [ ] Review/adjust computed selection properties if needed for delete classification (`HasAnySelectedComponent`, `IsFullySelected`) <!-- id: 11 -->
    - [ ] Verify delete confirmation uses the gated methods for details + counts (`MainWindowViewModel`) <!-- id: 12 -->
    - [ ] Verify delete execution cannot delete when row is unchecked (including any non-confirmation call paths) (`FileOperationViewModel`) <!-- id: 6 -->
- [ ] **Verification Phase** <!-- id: 7 -->
    - [ ] Logic/Unit Test: `IsSelected=false` forces empty selected component list <!-- id: 13 -->
    - [ ] Manual Test: Row Uncheck + Partial Check -> No Delete <!-- id: 8 -->
    - [ ] Manual Test: Row Check + Partial Uncheck -> Partial Delete Only <!-- id: 9 -->
    - [ ] Message Check: Verify count accuracy in Dialog (matches gated selection list) <!-- id: 10 ***
