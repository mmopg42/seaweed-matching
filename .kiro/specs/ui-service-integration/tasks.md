# Implementation Plan

## Overview

This implementation plan connects the UI (Task 10) with core services (Tasks 1-9) through dependency injection, event wiring, and proper threading. Each task builds incrementally to create a fully functional application.

**IMPORTANT NOTE**: Task 10 from the original migration plan was marked as complete, but analysis revealed that only the UI layout was implemented. Many functional components and integrations were left as TODO comments. This plan addresses:

1. **Service Integration**: Connecting all TODO command implementations to actual services
2. **Missing UI Components**: Implementing DragSelectBehavior, ImagePreviewDialog, LogPanel features that were specified but not coded
3. **Abnormal Detection**: Integrating the AbnormalDetectorService with UI display (status indicators, highlighting, statistics)
4. **Event Wiring**: Establishing all event subscriptions between services and ViewModels
5. **Threading**: Implementing proper Dispatcher usage for background operations

This plan essentially completes Task 10 by implementing all the functionality that was designed but not coded.

## Tasks

- [x] 1. Setup Dependency Injection Container





  - Modify App.xaml.cs to create ServiceCollection
  - Register all core services as singletons (ConfigurationManager, FileWatcherService, FileGroupMatcherService, ImageProcessingService, StatisticsService, MonitoringOrchestrator)
  - Register file operation services as transient (FileOperationService, PathManagementService)
  - Register ViewModels as transient (MainWindowViewModel, SettingsDialogViewModel)
  - Register Views as transient (MainWindow, SettingsDialog)
  - Configure logging (Debug, Console, minimum level Information)
  - Modify OnStartup to resolve MainWindow from service provider
  - Add OnExit to dispose service provider
  - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [x] 1.1 Write unit tests for DI container configuration


  - Test all services can be resolved
  - Test singleton services return same instance
  - Test transient services return different instances
  - Test service disposal on application exit
  - _Requirements: 1.1, 1.3, 1.4_

- [x] 2. Add Service Dependencies to MainWindowViewModel





  - Add constructor parameters for all required services (MonitoringOrchestrator, IStatisticsService, IConfigurationManager, IFileOperationService, IPathManagementService, ILogger)
  - Store service references in private fields
  - Remove parameterless constructor (DI will provide dependencies)
  - Update MainWindow.xaml to remove inline ViewModel instantiation
  - Modify MainWindow.xaml.cs to accept MainWindowViewModel via constructor
  - _Requirements: 1.2, 14.1_

- [x] 2.1 Write unit tests for ViewModel service injection


  - Test ViewModel can be constructed with all dependencies
  - Test ViewModel stores service references correctly
  - Mock all services for isolated testing
  - _Requirements: 1.2_

- [x] 3. Implement MonitoringOrchestrator Events
  - Add GroupCreated event (EventHandler<FileGroup>)
  - Add GroupRemoved event (EventHandler<string>)
  - Add GroupUpdated event (EventHandler<FileGroup>)
  - Add MonitoringError event (EventHandler<string>)
  - Raise GroupCreated when FileGroupMatcher creates a group
  - Raise GroupRemoved when a group is deleted
  - Raise GroupUpdated when a group's files change
  - Raise MonitoringError when file watching or matching fails
  - _Requirements: 4.1, 14.2_

- [x] 3.1 Write unit tests for orchestrator events
  - Test events are raised at appropriate times
  - Test event arguments contain correct data
  - Test multiple subscribers receive events
  - _Requirements: 4.1_

- [x] 4. Implement StatisticsService Events and Methods





  - Add FileCountsUpdated event (EventHandler<FileCountStatistics>)
  - Add MatchingStatisticsUpdated event (EventHandler<MatchingStatistics>)
  - Implement StartMonitoringAsync(ApplicationConfiguration config)
  - Implement StopMonitoringAsync()
  - Implement GetCurrentFileCounts()
  - Implement GetCurrentMatchingStats()
  - Start background file counting worker in StartMonitoringAsync
  - Raise FileCountsUpdated when counts change (debounced to 500ms)
  - Calculate and raise MatchingStatisticsUpdated when groups change
  - _Requirements: 6.1, 6.2, 7.1, 7.2_

- [x] 4.1 Write unit tests for statistics service


  - Test file counting logic
  - Test matching statistics calculation
  - Test event raising with correct data
  - Test debouncing of rapid updates
  - _Requirements: 6.1, 7.1_

- [x] 5. Subscribe to Events in MainWindowViewModel





  - Subscribe to MonitoringOrchestrator.GroupCreated in constructor
  - Subscribe to MonitoringOrchestrator.GroupRemoved in constructor
  - Subscribe to MonitoringOrchestrator.MonitoringError in constructor
  - Subscribe to StatisticsService.FileCountsUpdated in constructor
  - Subscribe to StatisticsService.MatchingStatisticsUpdated in constructor
  - Implement IDisposable and unsubscribe in Dispose()
  - Use Dispatcher.InvokeAsync in all event handlers for UI updates
  - _Requirements: 4.2, 6.3, 7.3, 15.2_

- [x] 5.1 Write property test for event subscription cleanup



  - **Property 3: Event Subscription Cleanup**
  - **Validates: Requirements 15.5**

- [x] 6. Implement Start Command with Service Integration




  - Modify ExecuteStart to be async (ExecuteStartAsync)
  - Load configuration from ConfigurationManager
  - Call MonitoringOrchestrator.StartAsync(config)
  - Call StatisticsService.StartMonitoringAsync(config)
  - Set IsMonitoring = true on success
  - Handle exceptions and display error messages
  - Log start operation
  - _Requirements: 2.1, 2.2, 2.5_

- [x] 6.1 Write unit tests for Start command

  - Test command calls orchestrator and statistics service
  - Test IsMonitoring is set to true
  - Test error handling when start fails
  - Test command state updates (Start disabled, Stop enabled)
  - _Requirements: 2.1, 2.3_

- [x] 7. Implement Stop Command with Service Integration





  - Modify ExecuteStop to be async (ExecuteStopAsync)
  - Call MonitoringOrchestrator.StopAsync()
  - Call StatisticsService.StopMonitoringAsync()
  - Set IsMonitoring = false on success
  - Handle exceptions and display error messages
  - Log stop operation
  - _Requirements: 3.1, 3.2, 3.5_

- [x] 7.1 Write unit tests for Stop command


  - Test command calls orchestrator and statistics service
  - Test IsMonitoring is set to false
  - Test error handling when stop fails
  - Test command state updates (Stop disabled, Start enabled)
  - _Requirements: 3.1, 3.3_

- [x] 8. Implement FileGroup Event Handlers





  - Implement OnGroupCreated(object sender, FileGroup group)
  - Create FileGroupViewModel from FileGroup
  - Use Dispatcher.InvokeAsync to add to FileGroups collection
  - Add to Line1Groups or Line2Groups based on LineNumber
  - Call UpdateStatistics() after adding
  - Log group creation
  - Implement OnGroupRemoved(object sender, string groupId)
  - Use Dispatcher.InvokeAsync to remove from collections
  - Call UpdateStatistics() after removing
  - Log group removal
  - _Requirements: 4.2, 4.3, 4.4, 4.5_

- [x] 8.1 Write property test for file group collection synchronization



  - **Property 5: File Group Collection Synchronization**
  - **Validates: Requirements 4.4, 15.3**

- [x] 9. Enhance FileGroupViewModel with Image Loading
  - Add IImageProcessor dependency to constructor
  - Add BitmapSource properties for all images (MainImageThumbnail, NirImageThumbnail, Cam1-6Thumbnails)
  - Implement LoadThumbnailsAsync() method
  - Call ImageProcessor.GenerateThumbnailAsync for each image path
  - Use Dispatcher.InvokeAsync to set BitmapSource properties
  - Handle missing files with placeholder image
  - Implement IDisposable to cancel loading on disposal
  - Add CancellationTokenSource for cancellation support
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [x] 9.1 Write property test for image loading cancellation
  - **Property 6: Image Loading Cancellation**
  - **Validates: Requirements 5.5, 15.4**

- [x] 10. Update DataGrid Image Bindings





  - Change Image Source bindings from string paths to BitmapSource properties
  - Bind to MainImageThumbnail instead of MainImagePath
  - Bind to NirImageThumbnail instead of NirImagePath
  - Bind to Cam1-6Thumbnails instead of Cam1-6ImagePaths
  - Add loading indicator (ProgressBar or spinner) while images load
  - Test image display in DataGrid
  - _Requirements: 5.1, 5.2_

- [x] 10.1 Implement Missing UI Components from Task 10


  - **NOTE**: Task 10 was marked complete but several UI components are not implemented
  - Implement DragSelectBehavior attached behavior for multi-row selection in DataGrid
  - Implement ImagePreviewDialog for full-size image display with EXIF rotation handling
  - Implement LogPanel custom control with file logging capability
  - Add log search functionality to LogPanel
  - Add log level filtering to LogPanel
  - Add auto-scroll control to LogPanel
  - Implement progress indicators for long-running operations (ProgressBar or ProgressRing)
  - Implement drag-and-drop functionality for path configuration in Settings dialog
  - Add tooltip display based on configuration setting
  - Test all new UI components
  - _Requirements: 1.3, 5.1, 16.1-16.15_

- [x] 10.2 Implement Abnormal Condition Detection and Display


  - **NOTE**: Abnormal detection logic exists but UI integration is incomplete
  - Verify AbnormalDetectorService is integrated with FileGroupMatcherService
  - Ensure z-score analysis runs on file group creation
  - Add abnormal status indicator to FileGroupViewModel (visual flag in DataGrid)
  - Update Status column to show "Abnormal" for detected anomalies
  - Add abnormal condition count to statistics bar
  - Highlight abnormal groups with different color in DataGrid
  - Add abnormal condition details to log panel when detected
  - Test abnormal detection with test data (outlier timestamps, missing files)
  - _Requirements: 5.3, 5.7, 5.8_

- [x] 11. Implement Statistics Event Handlers
  - Implement OnFileCountsUpdated(object sender, FileCountStatistics stats)
  - Use Dispatcher.InvokeAsync to update count properties
  - Update NirCount, Nir2Count, NormalCount, Normal2Count, Cam1-6Count
  - Implement OnMatchingStatsUpdated(object sender, MatchingStatistics stats)
  - Use Dispatcher.InvokeAsync to update statistics properties
  - Update TotalGroups, WithNirCount, WithoutNirCount, FailedCount
  - Update MatchRate property
  - Update Line1 and Line2 statistics if in separated mode
  - _Requirements: 6.3, 6.4, 7.3, 7.4, 7.5_

- [ ] 11.1 Write property test for statistics update atomicity
  - **Property 7: Statistics Update Atomicity**
  - **Validates: Requirements 6.4, 7.4**

- [x] 12. Implement FileOperationService
  - Create FileOperationService class implementing IFileOperationService
  - Implement MoveFileGroupAsync(FileGroup, destinationPath, progress, cancellationToken)
  - Move all files in group to destination
  - Report progress for each file
  - Handle file conflicts (skip, overwrite, rename)
  - Rollback on failure (move files back to original location)
  - Return OperationResult with success status and counts
  - Implement DeleteFileGroupAsync(FileGroup, progress, cancellationToken)
  - Delete all files in group
  - Report progress for each file
  - Handle locked files gracefully
  - Return OperationResult with success status and counts
  - _Requirements: 8.2, 8.3, 9.2, 9.3_

- [ ] 12.1 Write property test for file operation rollback
  - **Property 8: File Operation Rollback**
  - **Validates: Requirements 8.4, 9.5**

- [x] 13. Implement Move Command with FileOperationService
  - Modify ExecuteMove to be async (ExecuteMoveAsync)
  - Check if SelectedGroup is not null
  - Get destination path from configuration
  - Create Progress<OperationProgress> to update UI
  - Create CancellationTokenSource for cancellation
  - Call FileOperationService.MoveFileGroupAsync
  - Display progress dialog during operation
  - On success, remove group from collections
  - On failure, display error message
  - Log operation result
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

- [ ] 13.1 Write unit tests for Move command
  - Test command calls FileOperationService
  - Test progress reporting
  - Test group removal on success
  - Test error handling on failure
  - Test cancellation support
  - _Requirements: 8.1, 8.2_

- [x] 14. Implement Delete Command with FileOperationService
  - Modify ExecuteDelete to be async (ExecuteDeleteAsync)
  - Check if SelectedGroup is not null
  - Display confirmation dialog
  - If confirmed, create Progress<OperationProgress>
  - Create CancellationTokenSource for cancellation
  - Call FileOperationService.DeleteFileGroupAsync
  - Display progress dialog during operation
  - On success, remove group from collections
  - On failure, display error message
  - Log operation result
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

- [ ] 14.1 Write unit tests for Delete command
  - Test command shows confirmation dialog
  - Test command calls FileOperationService
  - Test progress reporting
  - Test group removal on success
  - Test error handling on failure
  - _Requirements: 9.1, 9.2_

- [x] 15. Implement PathManagementService
  - Create PathManagementService class implementing IPathManagementService
  - Implement GeneratePathsFromDate(dateString, config)
  - Parse date string (YYYYMMDD format)
  - Generate folder paths using configured pattern (e.g., "D:/Data/{date}/NIR1")
  - Return dictionary of folder type to path
  - Implement ValidatePaths(paths)
  - Check if paths are valid directory paths
  - Check if parent directories exist
  - Return true if all paths are valid
  - Implement CreateSampleFoldersAsync(sampleName, config)
  - Create sample folder structure in configured locations
  - Create subfolders if needed (e.g., NIR, Normal, Cam1-6)
  - Return true if all folders created successfully
  - _Requirements: 11.1, 11.2, 12.1, 12.2_

- [ ] 15.1 Write property test for path auto-config determinism
  - **Property 10: Path Auto-Config Determinism**
  - **Validates: Requirements 11.2**

- [x] 16. Implement Path Auto Config Command
  - Modify ExecutePathAutoConfig to use PathManagementService
  - Get date input from DateInput property
  - Validate date format
  - Call PathManagementService.GeneratePathsFromDate
  - Update configuration with generated paths
  - Display success message
  - Handle invalid date format with error message
  - Log operation
  - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5_

- [ ] 16.1 Write unit tests for Path Auto Config command
  - Test date parsing and validation
  - Test path generation
  - Test configuration update
  - Test error handling for invalid dates
  - _Requirements: 11.1, 11.2_

- [x] 17. Implement Create Sample Folder Command
  - Modify ExecuteCreateSampleFolder to be async (ExecuteCreateSampleFolderAsync)
  - Get sample folder name from SampleFolderName property
  - Validate folder name (no invalid characters)
  - Call PathManagementService.CreateSampleFoldersAsync
  - Display success message in log panel
  - Handle creation failure with error message
  - Log operation
  - _Requirements: 12.1, 12.2, 12.3, 12.4_

- [ ] 17.1 Write unit tests for Create Sample Folder command
  - Test folder name validation
  - Test folder creation
  - Test error handling for invalid names
  - Test error handling for creation failures
  - _Requirements: 12.1, 12.2_

- [x] 18. Implement Refresh Command
  - Modify ExecuteRefresh to be async (ExecuteRefreshAsync)
  - Display "Refreshing..." status message
  - Clear FileGroups, Line1Groups, Line2Groups collections
  - Call MonitoringOrchestrator.RefreshAsync()
  - Orchestrator performs full directory scan
  - New groups will be added via GroupCreated events
  - Display "Refresh complete" status message
  - Handle errors with error message
  - Log operation
  - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5_

- [ ] 18.1 Write unit tests for Refresh command
  - Test collections are cleared
  - Test orchestrator.RefreshAsync is called
  - Test new groups are added via events
  - Test error handling
  - _Requirements: 13.1, 13.2_

- [ ] 19. Implement Settings Dialog Save Functionality
  - Modify SettingsDialogViewModel to track configuration changes
  - Add IsDirty property to track if settings changed
  - Implement ValidateConfiguration() method
  - Check folder paths exist
  - Check numeric values are in valid ranges
  - Return validation errors if any
  - Modify SettingsDialog.OK_Click to call ViewModel.SaveConfiguration()
  - ViewModel calls ConfigurationManager.SaveConfigurationAsync
  - If save succeeds, set DialogResult = true
  - If save fails, display error and keep dialog open
  - Notify MainWindowViewModel of configuration change
  - MainWindowViewModel restarts services with new configuration
  - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_

- [ ] 19.1 Write property test for configuration persistence reliability
  - **Property 9: Configuration Persistence Reliability**
  - **Validates: Requirements 10.3**

- [ ] 20. Implement Window State Persistence
  - Add WindowState property to ApplicationConfiguration
  - Add WindowPosition (Left, Top) properties
  - Add WindowSize (Width, Height) properties
  - Modify MainWindow.Window_Loaded to restore state
  - Load configuration from ConfigurationManager
  - Set window position and size from configuration
  - Check if position is on-screen (handle multi-monitor changes)
  - If off-screen, center window on primary screen
  - Modify MainWindow.Window_Closing to save state
  - Get current window position and size
  - Update configuration
  - Call ConfigurationManager.SaveConfigurationAsync
  - _Requirements: 16.1, 16.2, 16.3, 16.4, 16.5_

- [ ] 20.1 Write property test for window state restoration
  - **Property 12: Window State Restoration**
  - **Validates: Requirements 16.1, 16.4**

- [ ] 21. Implement Comprehensive Error Handling
  - Add try-catch blocks to all command implementations
  - Catch specific exceptions (FileNotFoundException, UnauthorizedAccessException, etc.)
  - Log all exceptions with full context
  - Display user-friendly error messages for user-facing operations
  - Add error entries to log panel with Error severity
  - Implement ShowErrorMessageAsync helper method
  - Display MessageBox with error details
  - Implement global exception handlers in App.xaml.cs
  - Handle AppDomain.UnhandledException
  - Handle TaskScheduler.UnobservedTaskException
  - Handle Dispatcher.UnhandledException
  - Log critical errors and display crash dialog
  - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5_

- [ ] 21.1 Write property test for error propagation completeness
  - **Property 11: Error Propagation Completeness**
  - **Validates: Requirements 14.1, 14.2**

- [ ] 22. Implement Threading and Synchronization
  - Review all background operations use Task.Run
  - Review all UI updates from background threads use Dispatcher.InvokeAsync
  - Add CancellationTokenSource to MainWindowViewModel
  - Pass cancellation token to all async operations
  - Cancel all operations in Window_Closing
  - Add SemaphoreSlim for operations that need mutual exclusion
  - Test UI responsiveness during long operations
  - Test application closes cleanly with operations in progress
  - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5_

- [ ] 22.1 Write property test for UI thread marshalling safety
  - **Property 2: UI Thread Marshalling Safety**
  - **Validates: Requirements 15.2**

- [ ] 23. Integration Testing and Validation
  - Test complete Start-Monitor-Stop workflow
  - Start monitoring
  - Create test files in monitored folders
  - Verify groups appear in UI
  - Verify thumbnails load
  - Verify statistics update
  - Stop monitoring
  - Verify clean shutdown
  - Test Move operation end-to-end
  - Create test group
  - Execute move
  - Verify files moved
  - Verify group removed from UI
  - Test Settings save and reload
  - Modify settings
  - Save
  - Restart application
  - Verify settings restored
  - Test error scenarios
  - Invalid paths
  - Locked files
  - Insufficient permissions
  - Verify error messages displayed
  - _Requirements: All requirements_

- [ ] 23.1 Write property test for service lifecycle consistency
  - **Property 1: Service Lifecycle Consistency**
  - **Validates: Requirements 1.1, 1.4**

- [ ] 23.2 Write property test for command state consistency
  - **Property 4: Command State Consistency**
  - **Validates: Requirements 2.3, 3.3**

- [ ] 24. Final Checkpoint - Complete Integration Validation
  - Run all unit tests and verify they pass
  - Run all property-based tests and verify they pass
  - Run all integration tests and verify they pass
  - Manually test all UI buttons and verify they work
  - Test with real file system data
  - Verify no memory leaks (run for extended period)
  - Verify no cross-thread exceptions
  - Verify application starts and stops cleanly
  - Verify configuration persists correctly
  - Verify error handling works as expected
  - **Verify all Task 10 components are now functional**:
    - DragSelectBehavior works for multi-selection
    - ImagePreviewDialog opens and displays full-size images
    - LogPanel shows logs with search and filtering
    - Abnormal groups are highlighted and counted
    - Progress indicators appear during operations
    - Drag-and-drop works in Settings dialog
  - Update IMPLEMENTATION_GAP_ANALYSIS.md with completion status
  - Mark all gaps as RESOLVED
  - Ensure all tests pass, ask the user if questions arise.

## Implementation Notes

### Testing Configuration

- Property-based tests configured to run minimum 100 iterations
- Each property-based test tagged with: `**Feature: ui-service-integration, Property {number}: {property_text}**`
- Unit tests focus on specific examples, edge cases, and error conditions
- Property tests verify universal properties across all inputs
- Integration tests verify complete workflows

### Service Injection Pattern

All ViewModels follow this pattern:

```csharp
public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IService _service;
    
    public MainWindowViewModel(IService service)
    {
        _service = service;
        _service.SomeEvent += OnSomeEvent;
    }
    
    public void Dispose()
    {
        _service.SomeEvent -= OnSomeEvent;
    }
}
```

### Threading Pattern

All background operations follow this pattern:

```csharp
private async Task ExecuteOperationAsync()
{
    try
    {
        // Background work
        var result = await Task.Run(() => _service.DoWork(), _cancellationToken);
        
        // UI update
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Property = result;
        });
    }
    catch (OperationCanceledException)
    {
        // Expected - no error
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Operation failed");
        await ShowErrorMessageAsync("Operation failed", ex.Message);
    }
}
```

### Error Handling Pattern

All commands follow this pattern:

```csharp
private async Task ExecuteCommandAsync()
{
    try
    {
        // Operation logic
    }
    catch (SpecificException ex)
    {
        _logger.LogError(ex, "Specific error occurred");
        await ShowErrorMessageAsync("Operation failed", ex.Message);
        AddLogMessage(LogSeverity.Error, "Source", $"Error: {ex.Message}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error");
        await ShowErrorMessageAsync("Unexpected error", "Please check logs");
        AddLogMessage(LogSeverity.Error, "System", $"Unexpected: {ex.Message}");
    }
}
```

## Success Criteria

- All 26 tasks completed (24 main tasks + 2 Task 10 completion tasks)
- All unit tests passing
- All property-based tests passing
- All integration tests passing
- All UI buttons functional (no more TODO comments)
- All Task 10 UI components implemented and working:
  - DragSelectBehavior for multi-row selection
  - ImagePreviewDialog for full-size image viewing
  - LogPanel with search, filtering, and file logging
  - Abnormal condition detection and visual indicators
  - Progress indicators for all long operations
  - Drag-and-drop path configuration
- No TODO comments remaining in code
- No cross-thread exceptions
- No memory leaks
- Application starts and stops cleanly
- Configuration persists correctly
- Error handling works as expected
- Abnormal detection integrated and visible in UI
- IMPLEMENTATION_GAP_ANALYSIS.md updated with "RESOLVED" status

## Task 10 Completion Checklist

The following items from original Task 10 specification are now addressed in this plan:

✅ **Already Implemented in Task 10**:
- MainWindow XAML layout with DataGrid
- Statistics bars (file counts, matching stats)
- Tab control (Line1, Line2, Combined)
- Left sidebar (workflow control, data status)
- Detail preview panel
- Message log DataGrid
- Toolbar buttons
- Status bar
- Settings dialog basic structure

❌ **NOT Implemented in Task 10 (Addressed in This Plan)**:
- Task 10.1: DragSelectBehavior, ImagePreviewDialog, LogPanel features, Progress indicators, Drag-and-drop
- Task 10.2: Abnormal condition UI integration (highlighting, status display, statistics)
- Tasks 1-9: Service integration (all commands are TODO)
- Tasks 6-7: Start/Stop functionality
- Tasks 8-9: Image loading
- Tasks 11: Statistics updates
- Tasks 13-14: Move/Delete operations
- Tasks 16-17: Path management
- Task 18: Refresh functionality
- Task 19: Settings save
- Task 20: Window state persistence
