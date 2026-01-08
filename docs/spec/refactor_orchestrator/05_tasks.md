# Refactor MonitoringOrchestrator Phase 3 - Tasks

## Pre-flight Checklist
- [x] Architecture docs exist (MonitoringOrchestrator)
- [x] Spec-Lock clear (01 exists)
- [x] Impact scope identified (`MonitoringOrchestrator.cs`, `App.xaml.cs`)

## Phase 3: GroupManager Extraction
1. [ ] Create `IGroupManager.cs` interface.
2. [ ] Create `GroupManager.cs` implementation.
    - [ ] Move `ConcurrentDictionary<string, FileGroup> _activeGroups`
    - [ ] Move `int _nextGroupId`
    - [ ] Move `List<(string, DateTime, DateTime)> _pendingNirFiles`
    - [ ] Move `FindMatchingExistingGroup` logic (Match 1, 2, 3).
    - [ ] Move `MergeGroups` logic.
    - [ ] Move `CreateOrUpdateGroupAsync` core logic.
    - [ ] Implement `ResetState()` for `Clear()` calls.
3. [ ] Register `IGroupManager` in `App.xaml.cs`.
4. [ ] Inject `IGroupManager` into `MonitoringOrchestrator`.
5. [ ] Refactor `MonitoringOrchestrator` to delegate to `IGroupManager`.
6. [ ] Remove redundant methods from `MonitoringOrchestrator`.
7. [ ] Build and verify.
