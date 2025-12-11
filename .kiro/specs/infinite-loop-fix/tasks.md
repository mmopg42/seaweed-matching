# Implementation Plan: Infinite Loop Fix

## Task List

- [x] 1. Add duplicate detection guard in MainWindowViewModel.OnGroupCreated





  - Implement FirstOrDefault check to detect existing groups before adding
  - Add warning log when duplicate is detected
  - Add early return to skip duplicate additions
  - Ensure existing error handling remains intact
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 1.1 Write unit test for duplicate group detection






  - **Property 2: Event Handler Idempotence**
  - **Validates: Requirements 2.4, 4.1, 4.2**
  - Test that calling OnGroupCreated twice with same GroupId only adds once
  - Verify warning is logged on duplicate attempt
  - _Requirements: 2.4, 4.1, 4.2_

- [ ]* 1.2 Write unit test for new group addition
  - Test that OnGroupCreated successfully adds new groups
  - Verify group is added to correct line collection based on LineNumber
  - _Requirements: 4.5_

- [x] 2. Add duplicate prevention in MonitoringOrchestrator.PerformInitialScanAsync





  - Add ContainsKey check before adding to _activeGroups
  - Only raise GroupCreated event for new groups
  - Add debug logging for skipped duplicates
  - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [ ]* 2.1 Write unit test for orchestrator duplicate prevention
  - **Property 3: No Duplicate Event Raising**
  - **Validates: Requirements 5.1, 5.2, 5.3**
  - Test that calling PerformInitialScanAsync twice doesn't raise duplicate events
  - Mock event handler to count event invocations
  - _Requirements: 5.1, 5.2, 5.3_

- [ ] 3. Implement event rate limiting mechanism
  - Create EventRateLimiter class with time-window tracking
  - Add ShouldThrottle method with configurable thresholds
  - Integrate rate limiter into OnGroupCreated
  - Add error logging when throttling occurs
  - _Requirements: 6.2, 6.5_

- [ ]* 3.1 Write unit test for event rate limiter
  - **Property 4: Event Rate Bounds**
  - **Validates: Requirements 6.2, 6.5**
  - Test that excessive events (>10 in 5 seconds) are throttled
  - Test that events outside the window are allowed
  - _Requirements: 6.2, 6.5_

- [ ] 4. Add comprehensive diagnostic logging
  - Add entry/exit logging in OnGroupCreated with timestamps
  - Log collection counts before and after additions
  - Add structured logging for duplicate detection
  - Log event rate statistics periodically
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 5. Add collection consistency validation
  - Implement validation method to check FileGroups vs Line1Groups/Line2Groups
  - Add periodic validation during monitoring
  - Add repair logic for inconsistencies
  - Log errors when inconsistencies are detected
  - _Requirements: 4.3, 4.5_

- [ ]* 5.1 Write property test for collection consistency
  - **Property 5: Collection Consistency**
  - **Validates: Requirements 4.3, 4.5**
  - Generate random group additions and verify line collections match
  - Test that LineNumber 1 groups are in Line1Groups
  - Test that LineNumber 2 groups are in Line2Groups
  - _Requirements: 4.3, 4.5_

- [ ] 6. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ]* 6.1 Write integration test for end-to-end duplicate prevention
  - **Property 1: Group Uniqueness in UI Collections**
  - **Validates: Requirements 2.1, 2.2, 2.3**
  - Test full monitoring startup with duplicate file groups
  - Verify each GroupId appears exactly once in FileGroups
  - Test refresh operation doesn't create duplicates
  - _Requirements: 2.1, 2.2, 2.3, 6.1_

- [ ] 7. Add UI responsiveness monitoring
  - Implement timer to track event processing duration
  - Add warning if OnGroupCreated takes > 100ms
  - Add metrics for groups processed per second
  - _Requirements: 6.1, 6.3_

- [ ] 8. Update RefreshAsync to prevent duplicates
  - Verify that GroupRemoved events are raised before new scan
  - Ensure _activeGroups is cleared properly
  - Add logging to track refresh operation flow
  - _Requirements: 5.5_

- [ ] 9. Final Checkpoint - Verify fix with real data
  - Ensure all tests pass, ask the user if questions arise.
  - Test with the actual data that caused the infinite loop
  - Monitor logs for "Duplicate group detected" warnings
  - Verify UI remains responsive during initial scan
  - Confirm no infinite loop occurs
  - _Requirements: 6.1, 6.3, 6.4_

## Implementation Notes

### Critical Path
Tasks 1 and 2 are the critical fixes that directly address the infinite loop. These should be implemented first and tested thoroughly before proceeding with the additional safety mechanisms.

### Testing Strategy
- Unit tests verify individual components work correctly
- Property-based tests verify correctness properties hold across many inputs
- Integration tests verify the full system behaves correctly
- Manual testing with real data confirms the fix resolves the original issue

### Rollback Strategy
If issues are discovered:
1. Task 1 can be rolled back independently (revert OnGroupCreated changes)
2. Task 2 is defensive and safe to keep even if Task 1 is rolled back
3. Tasks 3-8 are enhancements and can be disabled without affecting core functionality
