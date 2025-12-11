# Requirements Document

## Introduction

The ChronoView application is experiencing an infinite loop where the same file groups (group_028 and group_029) are being created repeatedly. The logs show that `FileGroupViewModel` is being created and added to UI collections in an endless cycle, causing the application to become unresponsive. This spec addresses the root cause analysis and resolution of this critical bug.

## Glossary

- **ChronoView**: The WPF application for monitoring and managing file groups
- **FileGroup**: A model representing a collection of related files (NIR, Normal, Camera files)
- **FileGroupViewModel**: The ViewModel wrapper for FileGroup that handles UI presentation
- **MonitoringOrchestrator**: Service responsible for coordinating file monitoring and group creation
- **GroupCreated Event**: Event fired when a new file group is detected/created
- **UI Thread**: The WPF dispatcher thread responsible for UI updates
- **Event Handler**: Method that responds to events (OnGroupCreated)

## Requirements

### Requirement 1

**User Story:** As a developer, I want to identify the root cause of the infinite loop, so that I can implement an effective fix.

#### Acceptance Criteria

1. WHEN analyzing the log output THEN the system SHALL identify that group_028 and group_029 are being created repeatedly
2. WHEN examining the event flow THEN the system SHALL trace the path from file detection to UI update
3. WHEN reviewing the code THEN the system SHALL identify any circular event subscriptions or missing guards
4. WHEN checking for duplicate prevention THEN the system SHALL verify if groups are being deduplicated before UI addition
5. WHEN analyzing the MonitoringOrchestrator THEN the system SHALL verify if the same FileGroup instance is being raised multiple times

### Requirement 2

**User Story:** As a developer, I want to prevent duplicate group creation events, so that the same group is not added to the UI multiple times.

#### Acceptance Criteria

1. WHEN a GroupCreated event is raised THEN the system SHALL check if the group already exists in the active collections
2. WHEN a duplicate group is detected THEN the system SHALL skip adding it to the UI collections
3. WHEN checking for duplicates THEN the system SHALL use the GroupId as the unique identifier
4. WHEN a group already exists THEN the system SHALL log a warning instead of adding it again
5. WHEN the initial scan completes THEN the system SHALL not re-raise events for groups that were already created

### Requirement 3

**User Story:** As a developer, I want to ensure event handlers are not causing re-entrant calls, so that event processing remains stable.

#### Acceptance Criteria

1. WHEN an event handler executes THEN the system SHALL not trigger the same event recursively
2. WHEN UI updates occur THEN the system SHALL not cause file system changes that trigger new events
3. WHEN collections are modified THEN the system SHALL not raise events that cause the same modification
4. WHEN async operations complete THEN the system SHALL not re-trigger the original event
5. WHEN the Dispatcher.InvokeAsync executes THEN the system SHALL complete without triggering new GroupCreated events

### Requirement 4

**User Story:** As a developer, I want to add defensive guards in the event handler, so that duplicate additions are prevented at the UI layer.

#### Acceptance Criteria

1. WHEN OnGroupCreated is called THEN the system SHALL check if a FileGroupViewModel with the same GroupId already exists
2. WHEN a duplicate is found in FileGroups collection THEN the system SHALL skip the addition and log a warning
3. WHEN a duplicate is found in Line1Groups or Line2Groups THEN the system SHALL skip the addition
4. WHEN checking for existence THEN the system SHALL use LINQ FirstOrDefault with GroupId comparison
5. WHEN a non-duplicate group is detected THEN the system SHALL proceed with normal addition logic

### Requirement 5

**User Story:** As a developer, I want to review the MonitoringOrchestrator's event raising logic, so that I can ensure events are only raised once per group.

#### Acceptance Criteria

1. WHEN PerformInitialScanAsync completes THEN the system SHALL raise GroupCreated events only for newly discovered groups
2. WHEN groups are stored in _activeGroups dictionary THEN the system SHALL check for duplicates before adding
3. WHEN OnGroupCreated is called in the orchestrator THEN the system SHALL verify the group is not already in _activeGroups
4. WHEN the same group is encountered multiple times THEN the system SHALL update the existing entry instead of creating a new one
5. WHEN RefreshAsync is called THEN the system SHALL clear existing groups before scanning to prevent duplicates

### Requirement 6

**User Story:** As a user, I want the application to remain responsive during monitoring, so that I can interact with the UI without freezing.

#### Acceptance Criteria

1. WHEN file groups are being created THEN the UI SHALL remain responsive
2. WHEN an infinite loop is detected THEN the system SHALL break the loop and log an error
3. WHEN monitoring starts THEN the system SHALL complete the initial scan within a reasonable time (< 30 seconds for typical datasets)
4. WHEN groups are added to the UI THEN the system SHALL batch updates to avoid excessive UI refreshes
5. WHEN the application detects abnormal event rates THEN the system SHALL throttle or pause event processing

### Requirement 7

**User Story:** As a developer, I want comprehensive logging of the event flow, so that I can diagnose similar issues in the future.

#### Acceptance Criteria

1. WHEN a GroupCreated event is raised THEN the system SHALL log the GroupId and source
2. WHEN a duplicate is detected THEN the system SHALL log a warning with the GroupId
3. WHEN a group is successfully added THEN the system SHALL log an info message
4. WHEN an event handler completes THEN the system SHALL log the execution time
5. WHEN abnormal patterns are detected THEN the system SHALL log error messages with diagnostic information
