# Tasks: Line Separation Fix

- [x] **Specs & Plan** <!-- id: 0 -->
    - [x] Create Requirements <!-- id: 1 -->
    - [x] Create Plan <!-- id: 2 -->
    - [x] Create Design <!-- id: 3 -->

- [x] **Implementation** <!-- id: 4 -->
    - [x] Modify `GroupManager.cs` <!-- id: 5 -->
        - [x] Update `_lastAssignedGroup` definition to include `LineNumber` key <!-- id: 6 -->
        - [x] Implement `FilterByLine` helper method to reduce duplication <!-- id: 14 -->
        - [x] Update `CreateOrUpdateGroupAsync` to populate new dictionary key <!-- id: 7 -->
        - [x] Update `FindMatchingExistingGroup` to use `FilterByLine` and new dictionary key <!-- id: 8 -->
        - [x] Update logging to include `[Line X]` prefix (Update `RaiseLog` signature) <!-- id: 9 -->
        - [x] Ensure `DetermineLineNumber` is consistently called <!-- id: 10 -->
        - [x] Fix Log Prefix Duplication (Remove prefix from RaiseLog) <!-- id: 11 -->
        - [x] Implement Line-Specific Group ID (Settings & GroupManager logic) <!-- id: 12 -->

- [x] **Technical Debt** <!-- id: 15 -->
    - [x] Create a follow-up issue/task for splitting `GroupManager.cs` (currently >700 LOC) <!-- id: 16 -->

- [ ] **Verification** <!-- id: 11 -->
    - [ ] **Scenario A (UseFolderSuffix=true)**: Verify `_0`/`_1` correctly separates lines in shared/separate folders <!-- id: 17 -->
    - [ ] **Scenario B (UseFolderSuffix=false)**: Verify `_0`/`_1` is IGNORED and line is determined by Path only <!-- id: 18 -->
    - [ ] **Scenario C (NirKey Collision)**: Verify that identical NirKeys in different lines DO NOT cross-match <!-- id: 19 -->
    - [ ] **Scenario D (Log Prefix)**: Verify `[Line X]` appears only once <!-- id: 20 -->
    - [ ] **Scenario E (Group ID)**: Verify independent sequence when option enabled <!-- id: 21 -->
