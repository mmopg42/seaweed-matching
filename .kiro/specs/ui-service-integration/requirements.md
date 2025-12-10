# Requirements Document

## Introduction

This specification addresses the critical gap between the implemented UI (Task 10) and the core services (Tasks 1-9) in the ChronoView application. Currently, the UI layout exists but has no functional connections to the underlying services. This feature will establish all necessary service integrations, dependency injection, and event wiring to create a fully functional application.

## Glossary

- **DI Container**: Dependency Injection Container - Microsoft.Extensions.DependencyInjection service provider
- **ViewModel**: View Model in MVVM pattern - mediates between View and business logic
- **Service**: Business logic component that performs specific operations (file watching, image processing, etc.)
- **Command**: ICommand implementation that binds UI actions to ViewModel methods
- **Dispatcher**: WPF thread dispatcher for marshalling operations to UI thread
- **ObservableCollection**: Collection that notifies UI of changes automatically

## Requirements

### Requirement 1

**User Story:** As a developer, I want the application to use dependency injection for all services, so that components are loosely coupled and testable.

#### Acceptance Criteria

1. WHEN the application starts THEN the DI container SHALL register all core services with appropriate lifetimes
2. WHEN a ViewModel is created THEN the DI container SHALL inject all required service dependencies
3. WHEN a service is requested THEN the DI container SHALL provide the same instance for singleton services
4. WHEN the application closes THEN the DI container SHALL dispose all disposable services properly
5. WHEN a service fails to initialize THEN the application SHALL log the error and display a user-friendly message

### Requirement 2

**User Story:** As a user, I want the Start button to begin file monitoring, so that the system can detect and group new files.

#### Acceptance Criteria

1. WHEN the Start button is clicked THEN the MonitoringOrchestrator SHALL start the FileWatcherService
2. WHEN monitoring starts THEN the UI SHALL display "Monitoring..." status message
3. WHEN monitoring is active THEN the Start button SHALL be disabled and Stop button enabled
4. WHEN file system events occur THEN the FileWatcherService SHALL notify the MonitoringOrchestrator
5. WHEN monitoring fails to start THEN the UI SHALL display an error message and remain in stopped state

### Requirement 3

**User Story:** As a user, I want the Stop button to halt file monitoring, so that I can pause the system when needed.

#### Acceptance Criteria

1. WHEN the Stop button is clicked THEN the MonitoringOrchestrator SHALL stop the FileWatcherService
2. WHEN monitoring stops THEN the UI SHALL display "Ready" status message
3. WHEN monitoring is stopped THEN the Stop button SHALL be disabled and Start button enabled
4. WHEN monitoring stops THEN all pending file operations SHALL complete before stopping
5. WHEN stop fails THEN the UI SHALL display an error message and log the failure

### Requirement 4

**User Story:** As a user, I want to see file groups appear in the table as they are detected, so that I can monitor the grouping process in real-time.

#### Acceptance Criteria

1. WHEN a new FileGroup is created THEN the MonitoringOrchestrator SHALL raise a GroupCreated event
2. WHEN a GroupCreated event occurs THEN the MainWindowViewModel SHALL add the group to FileGroups collection
3. WHEN FileGroups collection changes THEN the DataGrid SHALL update automatically via data binding
4. WHEN adding groups from background thread THEN the ViewModel SHALL use Dispatcher to marshal to UI thread
5. WHEN a group is added THEN the statistics SHALL update to reflect the new group count

### Requirement 5

**User Story:** As a user, I want to see thumbnail images for each file group, so that I can visually identify the grouped files.

#### Acceptance Criteria

1. WHEN a FileGroup is displayed THEN the FileGroupViewModel SHALL request thumbnails from ImageProcessingService
2. WHEN thumbnail generation completes THEN the image SHALL appear in the DataGrid cell
3. WHEN thumbnail generation fails THEN a placeholder image SHALL be displayed
4. WHEN thumbnails are loaded THEN they SHALL be cached to avoid redundant processing
5. WHEN image loading occurs THEN it SHALL happen asynchronously without blocking the UI thread

### Requirement 6

**User Story:** As a user, I want to see real-time file count statistics, so that I can monitor how many files are in each folder.

#### Acceptance Criteria

1. WHEN monitoring starts THEN the StatisticsService SHALL begin counting files in monitored folders
2. WHEN file counts change THEN the StatisticsService SHALL raise a FileCountsUpdated event
3. WHEN FileCountsUpdated event occurs THEN the MainWindowViewModel SHALL update count properties
4. WHEN count properties change THEN the statistics bar SHALL display updated values via data binding
5. WHEN statistics updates occur THEN they SHALL use Dispatcher to marshal to UI thread

### Requirement 7

**User Story:** As a user, I want to see matching statistics update automatically, so that I can track grouping success rates.

#### Acceptance Criteria

1. WHEN file groups are created or modified THEN the StatisticsService SHALL recalculate matching statistics
2. WHEN matching statistics change THEN the StatisticsService SHALL raise a MatchingStatisticsUpdated event
3. WHEN MatchingStatisticsUpdated event occurs THEN the MainWindowViewModel SHALL update statistics properties
4. WHEN statistics properties change THEN the matching statistics bar SHALL display updated values
5. WHEN in separated mode THEN the statistics SHALL show independent values for Line1 and Line2

### Requirement 8

**User Story:** As a user, I want the Move button to relocate selected file groups, so that I can organize files into appropriate folders.

#### Acceptance Criteria

1. WHEN a file group is selected THEN the Move button SHALL be enabled
2. WHEN the Move button is clicked THEN the FileOperationService SHALL move all files in the selected group
3. WHEN move operation starts THEN a progress indicator SHALL be displayed
4. WHEN move operation completes THEN the file group SHALL be removed from the display
5. WHEN move operation fails THEN an error message SHALL be displayed and files SHALL remain in original location

### Requirement 9

**User Story:** As a user, I want the Delete button to remove selected file groups, so that I can clean up unwanted files.

#### Acceptance Criteria

1. WHEN a file group is selected THEN the Delete button SHALL be enabled
2. WHEN the Delete button is clicked THEN a confirmation dialog SHALL be displayed
3. WHEN deletion is confirmed THEN the FileOperationService SHALL delete all files in the selected group
4. WHEN delete operation completes THEN the file group SHALL be removed from the display
5. WHEN delete operation fails THEN an error message SHALL be displayed and files SHALL remain

### Requirement 10

**User Story:** As a user, I want the Settings dialog to save my configuration changes, so that my preferences persist across sessions.

#### Acceptance Criteria

1. WHEN the Settings dialog opens THEN it SHALL load current configuration from ConfigurationManager
2. WHEN I modify settings and click OK THEN the SettingsDialogViewModel SHALL validate the changes
3. WHEN validation passes THEN the ConfigurationManager SHALL save the configuration to disk
4. WHEN configuration is saved THEN affected services SHALL be notified to reload settings
5. WHEN validation fails THEN an error message SHALL be displayed and settings SHALL not be saved

### Requirement 11

**User Story:** As a user, I want the Path Auto Config button to automatically set folder paths based on date, so that I can quickly configure monitoring for a specific date.

#### Acceptance Criteria

1. WHEN the Path Auto Config button is clicked THEN the PathManagementService SHALL parse the date input
2. WHEN date is valid THEN the service SHALL construct folder paths using the configured pattern
3. WHEN paths are constructed THEN the SettingsDialogViewModel SHALL update folder path properties
4. WHEN folder paths change THEN the UI SHALL display the updated paths
5. WHEN date is invalid THEN an error message SHALL be displayed

### Requirement 12

**User Story:** As a user, I want the Create Folder button to create sample folders, so that I can prepare the directory structure for new samples.

#### Acceptance Criteria

1. WHEN the Create Folder button is clicked THEN the PathManagementService SHALL validate the sample folder name
2. WHEN validation passes THEN the service SHALL create the folder structure in configured locations
3. WHEN folders are created THEN a success message SHALL be displayed in the log panel
4. WHEN folder creation fails THEN an error message SHALL be displayed
5. WHEN monitoring is active THEN sample folders SHALL be created automatically on start

### Requirement 13

**User Story:** As a user, I want the Refresh button to reload file groups, so that I can see the current state after external changes.

#### Acceptance Criteria

1. WHEN the Refresh button is clicked THEN the MonitoringOrchestrator SHALL perform a full directory scan
2. WHEN scan completes THEN existing file groups SHALL be cleared
3. WHEN scan completes THEN new file groups SHALL be created from current files
4. WHEN refresh is in progress THEN a progress indicator SHALL be displayed
5. WHEN refresh completes THEN the UI SHALL display the updated file groups

### Requirement 14

**User Story:** As a developer, I want proper error handling throughout the service integration, so that failures are logged and communicated to users appropriately.

#### Acceptance Criteria

1. WHEN any service operation fails THEN the error SHALL be logged with full context
2. WHEN a user-facing operation fails THEN a user-friendly error message SHALL be displayed
3. WHEN a critical service fails to initialize THEN the application SHALL display an error and exit gracefully
4. WHEN background operations fail THEN they SHALL not crash the application
5. WHEN errors occur THEN they SHALL be added to the log panel with appropriate severity

### Requirement 15

**User Story:** As a developer, I want proper threading and synchronization, so that background operations don't block the UI and data races are prevented.

#### Acceptance Criteria

1. WHEN file operations execute THEN they SHALL run on background threads using Task.Run
2. WHEN UI updates are needed from background threads THEN Dispatcher.InvokeAsync SHALL be used
3. WHEN multiple threads access shared collections THEN proper synchronization SHALL be used
4. WHEN long-running operations execute THEN the UI SHALL remain responsive
5. WHEN the application closes THEN all background operations SHALL be cancelled gracefully

### Requirement 16

**User Story:** As a user, I want my window position and size to be remembered, so that the application opens in my preferred layout.

#### Acceptance Criteria

1. WHEN the application starts THEN the MainWindow SHALL restore position and size from configuration
2. WHEN the window is moved or resized THEN the new state SHALL be tracked
3. WHEN the application closes THEN the current window state SHALL be saved to configuration
4. WHEN saved position is off-screen THEN the window SHALL open in a default visible location
5. WHEN no saved state exists THEN the window SHALL open with default dimensions centered on screen
