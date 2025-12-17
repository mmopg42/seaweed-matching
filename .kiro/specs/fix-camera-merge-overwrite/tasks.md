---
Task: Fix Camera File Merge Overwrite Issue
Created: 2025-12-15
Status: Not Started
Depends On: design.md
---

# Fix Camera File Merge Overwrite Issue - Implementation Plan

## Progress
- Total: 7 | Completed: 0 | Remaining: 7

## Phase 1: Update Glossary

- [ ] 1.1 Update `docs/architecture/glossary.md` with new terms
  - Add Match 1, Match 2, Match 3 concept definitions
  - Add Non-Duplicate Filter concept definition
  - _Requirements: Design Section "Naming Conventions"_

- [ ] 1.2 Verify no naming conflicts
  - Run: `grep -rn "Match 1\|Match 2\|Match 3" docs/architecture/`
  - Ensure terms are used consistently
  - _Requirements: Design Section "Naming Conventions"_

## Phase 2: Fix Match 1 - Add Non-Duplicate Filter

- [ ] 2.1 Modify FindMatchingExistingGroup() - Match 1 section
  - Location: `MonitoringOrchestrator.cs` line ~920
  - Add `DetermineDataTypeForGroup(newGroup)` call
  - Add `.Where(g => !HasDataType(g, newGroupDataType))` filter
  - Add diagnostic logging for duplicate detection
  - _Requirements: Design Section 1 "Fix Match 1"_

- [ ] 2.2 Test Match 1 duplicate filtering
  - Create unit test: Match 1 with existing data type returns null
  - Create unit test: Match 1 with different data type returns match
  - Verify: `dotnet test --filter "Match1"`
  - _Requirements: Design "Testing Strategy"_

## Phase 3: Fix Match 2 - Add Non-Duplicate Filter

- [ ] 3.1 Modify FindMatchingExistingGroup() - Match 2 section
  - Location: `MonitoringOrchestrator.cs` line ~950
  - Add `DetermineDataTypeForGroup(newGroup)` call
  - Add `.Where(g => !HasDataType(g, newGroupDataType))` filter
  - Add diagnostic logging for duplicate detection
  - _Requirements: Design Section 2 "Fix Match 2"_

- [ ] 3.2 Test Match 2 duplicate filtering
  - Create unit test: Match 2 with existing data type returns null
  - Create unit test: Match 2 with different data type returns match
  - Verify: `dotnet test --filter "Match2"`
  - _Requirements: Design "Testing Strategy"_

## Phase 4: Make MergeGroups Defensive

- [ ] 4.1 Modify MergeGroups() camera merge logic
  - Location: `MonitoringOrchestrator.cs` line ~1103
  - Add `ContainsKey()` check before assignment
  - Add check for empty existing value
  - Add warning log when skipping merge
  - Add debug log when successfully merging
  - _Requirements: Design Section 3 "Make MergeGroups Defensive"_

- [ ] 4.2 Test MergeGroups defensive behavior
  - Create unit test: MergeGroups preserves existing camera keys
  - Create unit test: MergeGroups adds new camera keys
  - Create unit test: MergeGroups logs warning on skip
  - Verify: `dotnet test --filter "MergeGroups"`
  - _Requirements: Design "Testing Strategy"_

## Phase 5: Integration Testing

- [ ] 5.1 Test real-time monitoring with multiple camera files
  - Setup: 8 Normal folders in test directory
  - Action: Simulate 8 Cam1 files arriving sequentially
  - Assert: 8 separate groups created
  - Assert: No "Skipping camera merge" warnings in logs
  - Verify: Manual test or integration test
  - _Requirements: Design "Testing Strategy - Integration Tests"_

## Phase 6: Documentation Updates

- [ ] 6.1 Update `docs/architecture/module_monitoring_orchestrator.md`
  - Update Match 1 section with Non-Duplicate Filter description
  - Update Match 2 section with Non-Duplicate Filter description
  - Update MergeGroups section with defensive behavior description
  - Add Changelog entry: "2025-12-15: Fixed camera overwrite in Match 1/2 and MergeGroups"
  - _Requirements: Design "Architecture Documentation Plan"_

- [ ] 6.2 Verify documentation consistency
  - Run: `grep -rn "Match 1\|Match 2\|Match 3" docs/architecture/`
  - Ensure all references use glossary terms
  - Check for conflicting definitions
  - _Requirements: Design "Architecture Documentation Plan"_

## Phase 7: Final Verification

- [ ] 7.1 Run full test suite
  - Execute: `dotnet test`
  - Ensure all tests pass
  - Check for any regressions
  - _Requirements: All previous phases_

- [ ] 7.2 Manual end-to-end test
  - Start application with real configuration
  - Monitor 8 Normal folders
  - Add camera files in real-time
  - Verify UI shows 8 separate groups
  - Check logs for correct behavior
  - _Requirements: Design "Testing Strategy - Manual Testing"_

---
**Status**: [ ] Approved
