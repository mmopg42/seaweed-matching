# Tasks - Refine Setup UI

## Context
Refine the Setup Window UI by removing the status message mechanism and adding a Default Settings feature.

## Task List



### 3. Setup Window Refinement
- [ ] **Update `SetupWindowViewModel.cs`** <!-- id: 5 -->
    - [ ] Remove `StatusMessage`, `StatusVisibility` properties.
    - [ ] Remove `ShowStatus` method.
    - [ ] Remove `ShowStatus` calls from all commands (`Launch`, `Toggle`).
    - [ ] Update `NirFilterStatus` to use White color for all states.
    - [ ] Add "Processing..." state for NIR filter toggle.

- [ ] **Update `SetupWindow.xaml`** <!-- id: 6 -->
    - [ ] Remove Status Bar border.
    - [ ] Remove Color triggers for NIR filter status (make it static White).

### 4. Verification
- [ ] **Manual Test** <!-- id: 7 -->
    - [ ] Verify Status Bar is gone.
    - [ ] Verify Default Button clears paths.
