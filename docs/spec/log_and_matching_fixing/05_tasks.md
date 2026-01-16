# Log and Matching Fix Tasks

## Phase 1: Core Logic & Consistency

- [x] **Core Logic Implementation (`GroupManager.cs`)** <!-- id: 0 -->
    - [x] Implement `NormalizeForSequence` helper method (Line2 types -> Line1 types)
    - [x] Update `DetermineDataTypeForGroup` to explicit detect `Cam4`, `Cam5`, `Cam6`
    - [x] Update `HasDataType` to handle `Cam4~Cam6` keys
    - [x] Modify `FindMatchingExistingGroup` to use `NormalizeForSequence` when calling `GetPriority` / `GetMinDelay` / `GetMaxDelay`
    - [x] Add "Silent Skip" UI logs in `FindMatchingExistingGroup` loop

- [x] **Consistency Fix (`FileMatchingEngine.cs`)** <!-- id: 1 -->
    - [x] Update `HasDataTypeInGroup` to handle `Cam4~Cam6` (sync with `GroupManager` logic)

## Phase 2: Verification

- [ ] **Manual Verification** <!-- id: 2 -->
    - [ ] **Scenario A (Log Identity)**: Verify Line2 logs show "Cam4/Cam5/Cam6" instead of "Camera"
    - [ ] **Scenario B (Group Assignment)**: Verify `cam4` followed by `cam5` assigns to the SAME `group_001` (not skipped)
    - [ ] **Scenario C (Skip Logs)**: Verify "Matching Excluded" logs appear when candidates are rejected

## Phase 3: Documentation & Report

- [ ] **Final Report** <!-- id: 3 -->
    - [ ] Create `06_report.md` summarizing changes and verification results

---

## Deferred (Future Phase)
- [ ] **UI Log Filtering** (Moved to separate spec/phase)
    - [ ] Update Log Models to support Line/Scope
    - [ ] Implement UI filtering tabs (Line1/Line2/Combined)
