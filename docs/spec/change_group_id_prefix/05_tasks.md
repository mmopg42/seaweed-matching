---
Task: change_group_id_prefix
Created: 2026-01-08
Status: In Progress
Depends On: 04_design.md
---

# Tasks: Group ID Refactoring

- [x] **Specs & Plan** <!-- id: 0 -->
    - [x] Create Research <!-- id: 1 -->
    - [x] Create Plan <!-- id: 2 -->
    - [x] Create Design <!-- id: 3 -->

- [x] **Implementation** <!-- id: 4 -->
    - [x] **Core Components** (~30m) <!-- id: 5 -->
        - [x] Create folder `ChronoView/Core/GroupIdGeneration/` <!-- id: 6 -->
        - [x] Implement `IGroupIdGenerator` interface <!-- id: 7 -->
        - [x] Implement `GlobalGroupIdGenerator` (Legacy, 1-based index) <!-- id: 8 -->
        - [x] Implement `LineBasedGroupIdGenerator` (New, 1-based index) <!-- id: 9 -->

    - [x] **Service Integration** (~45m) <!-- id: 10 -->
        - [x] Update `FileGroupMatcherService` to inject `IGroupIdGenerator` <!-- id: 11 -->
            - [x] Add constructor parameter `IGroupIdGenerator` <!-- id: 30 -->
            - [x] Pass generator to `FileMatchingEngine.MatchFiles()` <!-- id: 31 -->
            - [x] Update `ResetState()` to call `_idGenerator.Reset()` <!-- id: 32 -->
        - [x] Update `GroupManager` to inject `IGroupIdGenerator` and remove internal counters <!-- id: 12 -->
            - [x] Remove `_nextGroupId` and `_nextGroupIdByLine` fields <!-- id: 33 -->
            - [x] Update `ResetState()` to call `_idGenerator.Reset()` <!-- id: 34 -->

    - [x] **Engine Refactoring** (~30m) <!-- id: 13 -->
        - [x] Update `FileMatchingEngine.MatchFiles` signature to accept `IGroupIdGenerator` <!-- id: 14 -->
        - [x] Update `FileMatchingEngine` logic to use generator <!-- id: 15 -->

    - [x] **DI Wiring** (~15m) <!-- id: 16 -->
        - [x] Update `App.xaml.cs`: Register Generator using Factory Pattern (depends on `UseLineSpecificGroupId`) <!-- id: 17 -->
        - [x] Update `App.xaml.cs`: Update `FileGroupMatcherService` registration to inject generator <!-- id: 18 -->

- [ ] **Test & Verification** (~60m) <!-- id: 19 -->
    - [ ] **New Unit Tests** <!-- id: 26 -->
        - [ ] Test `GlobalGroupIdGenerator.GenerateNextId()` returns correct format (`group_001`) <!-- id: 27 -->
        - [ ] Test `LineBasedGroupIdGenerator.GenerateNextId()` returns correct format (`line1_001`) <!-- id: 28 -->
        - [ ] Test `Reset()` correctly resets counters <!-- id: 29 -->
    - [ ] **Fix Breaking Tests** <!-- id: 20 -->
        - [ ] Identify tests using hardcoded `group_` IDs <!-- id: 21 -->
        - [ ] Refactor tests to support `GlobalGroupIdGenerator` or update assertions <!-- id: 22 -->
    - [ ] **Manual Verification** <!-- id: 23 -->
        - [ ] Verify Real-time mode (Line-specific vs Global) <!-- id: 24 -->
        - [ ] Verify Batch mode (File Drop) consistency <!-- id: 25 -->

