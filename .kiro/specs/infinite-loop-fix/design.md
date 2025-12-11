# Design Document: Infinite Loop Fix

## Overview

The ChronoView application is experiencing an infinite loop where the same file groups are being created and added to the UI collections repeatedly. Analysis of the logs shows that `group_028` (with NIR file `run_120251204T111140.spc`) and `group_029` (with NIR file `run_120251204T111140A.txt`) are being created in an endless cycle.

The root cause is that the `OnGroupCreated` event handler in `MainWindowViewModel` lacks duplicate detection logic. When the `MonitoringOrchestrator` raises the `GroupCreated` event multiple times for the same group (possibly due to file system events or refresh operations), the UI blindly adds the group each time without checking if it already exists.

This design document outlines a comprehensive solution involving:
1. Adding duplicate detection guards in the UI event handler
2. Ensuring the MonitoringOrchestrator doesn't raise duplicate events
3. Adding defensive logging and diagnostics
4. Implementing event rate limiting as a safety mechanism

## Architecture

### Current Flow (Problematic)

```
MonitoringOrchestrator.PerformInitialScanAsync()
  └─> Creates FileGroup instances
  └─> Stores in _activeGroups dictionary
  └─> Raises GroupCreated event for each group
      └─> MainWindowViewModel.OnGroupCreated()
          └─> Creates FileGroupViewModel
          └─> Adds to FileGroups collection (NO DUPLICATE CHECK)
          └─> Adds to Line1Groups/Line2Groups (NO DUPLICATE CHECK)
          └─> Triggers LoadThumbnailsAsync()
          └─> [LOOP REPEATS IF EVENT RAISED AGAIN]
```

### Proposed Flow (Fixed)

```
MonitoringOrchestrator.PerformInitialScanAsync()
  └─> Creates FileGroup instances
  └─> Checks _activeGroups for duplicates
  └─> Only raises GroupCreated for NEW groups
      └─> MainWindowViewModel.OnGroupCreated()
          └─> Checks if GroupId already exists in FileGroups
          └─> If duplicate: Log warning and RETURN
          └─> If new: Create FileGroupViewModel
          └─> Add to collections
          └─> Trigger LoadThumbnailsAsync()
```

## Components and Interfaces

### 1. MainWindowViewModel.OnGroupCreated (Primary Fix)

**Current Implementation:**
```csharp
private void OnGroupCreated(object? sender, FileGroup group)
{
    _logger.LogDebug("GroupCreated event received for group {GroupId}", group.GroupId);
    
    WpfApplication.Current.Dispatcher.InvokeAsync(() =>
    {
        var config = _configManager.LoadConfiguration<ApplicationConfiguration>();
        var viewModel = new FileGroupViewModel(group, ...);
        
        FileGroups.Add(viewModel);  // NO DUPLICATE CHECK!
        
        if (group.LineNumber == 1)
            Line1Groups.Add(viewModel);
        else if (group.LineNumber == 2)
            Line2Groups.Add(viewModel);
            
        _ = viewModel.LoadThumbnailsAsync();
        UpdateStatistics();
    });
}
```

**Proposed Implementation:**
```csharp
private void OnGroupCreated(object? sender, FileGroup group)
{
    _logger.LogDebug("GroupCreated event received for group {GroupId}", group.GroupId);
    
    WpfApplication.Current.Dispatcher.InvokeAsync(() =>
    {
        try
        {
            // GUARD: Check for duplicate before creating ViewModel
            var existingGroup = FileGroups.FirstOrDefault(g => g.GroupId == group.GroupId);
            if (existingGroup != null)
            {
                _logger.LogWarning("Duplicate GroupCreated event for {GroupId} - skipping", group.GroupId);
                AddLogMessage(LogSeverity.Warning, "GroupManager", 
                    $"Duplicate group {group.GroupId} detected - skipped");
                return;
            }
            
            var config = _configManager.LoadConfiguration<ApplicationConfiguration>();
            var viewModel = new FileGroupViewModel(group, ...);
            
            FileGroups.Add(viewModel);
            
            if (group.LineNumber == 1)
                Line1Groups.Add(viewModel);
            else if (group.LineNumber == 2)
                Line2Groups.Add(viewModel);
                
            _ = viewModel.LoadThumbnailsAsync();
            UpdateStatistics();
            AddLogMessage(LogSeverity.Info, "GroupManager", $"Added group {group.GroupId}");
            _logger.LogInformation("Group {GroupId} added to UI collections", group.GroupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling GroupCreated event for group {GroupId}", group.GroupId);
            AddLogMessage(LogSeverity.Error, "GroupManager", 
                $"Error adding group {group.GroupId}: {ex.Message}");
        }
    });
}
```

### 2. MonitoringOrchestrator.PerformInitialScanAsync (Secondary Fix)

**Issue Analysis:**
The orchestrator stores groups in `_activeGroups` dictionary and raises `GroupCreated` events. However, there's a potential issue where:
1. Groups are added to `_activeGroups` 
2. Events are raised for each group
3. If the method is called again (or if there's a race condition), the same groups might be raised again

**Current Code:**
```csharp
// Store groups and raise individual events
lock (_lockObject)
{
    foreach (var group in groupList)
    {
        _activeGroups[group.GroupId] = group;  // Overwrites if exists
        OnGroupCreated(group);  // Always raises event!
    }
}
```

**Proposed Fix:**
```csharp
// Store groups and raise individual events ONLY for new groups
lock (_lockObject)
{
    foreach (var group in groupList)
    {
        // Check if group already exists
        if (_activeGroups.ContainsKey(group.GroupId))
        {
            _logger.LogDebug("Group {GroupId} already exists in active groups - skipping event", 
                group.GroupId);
            continue;
        }
        
        _activeGroups[group.GroupId] = group;
        OnGroupCreated(group);
    }
}
```

### 3. Event Rate Limiting (Safety Mechanism)

Add a rate limiter to detect and prevent runaway event loops:

```csharp
private class EventRateLimiter
{
    private readonly Dictionary<string, Queue<DateTime>> _eventTimestamps = new();
    private readonly TimeSpan _window = TimeSpan.FromSeconds(5);
    private readonly int _maxEventsInWindow = 10;
    
    public bool ShouldThrottle(string groupId)
    {
        lock (_eventTimestamps)
        {
            if (!_eventTimestamps.ContainsKey(groupId))
            {
                _eventTimestamps[groupId] = new Queue<DateTime>();
            }
            
            var queue = _eventTimestamps[groupId];
            var now = DateTime.UtcNow;
            
            // Remove old timestamps outside the window
            while (queue.Count > 0 && (now - queue.Peek()) > _window)
            {
                queue.Dequeue();
            }
            
            // Check if we're over the limit
            if (queue.Count >= _maxEventsInWindow)
            {
                return true;  // Throttle!
            }
            
            queue.Enqueue(now);
            return false;
        }
    }
}
```

## Data Models

No changes to data models required. The fix is purely in event handling logic.

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Group Uniqueness in UI Collections
*For any* FileGroup with a given GroupId, that GroupId should appear at most once in the FileGroups collection at any point in time.
**Validates: Requirements 2.1, 2.2, 2.3**

### Property 2: Event Handler Idempotence
*For any* GroupCreated event raised multiple times with the same GroupId, only the first event should result in a UI collection addition.
**Validates: Requirements 2.4, 4.1, 4.2**

### Property 3: No Duplicate Event Raising
*For any* group in the _activeGroups dictionary, the GroupCreated event should be raised exactly once during a single scan operation.
**Validates: Requirements 5.1, 5.2, 5.3**

### Property 4: Event Rate Bounds
*For any* GroupId, the number of GroupCreated events raised within a 5-second window should not exceed 10.
**Validates: Requirements 6.2, 6.5**

### Property 5: Collection Consistency
*For any* FileGroupViewModel in FileGroups, if its LineNumber is 1, it should also exist in Line1Groups; if LineNumber is 2, it should exist in Line2Groups.
**Validates: Requirements 4.3, 4.5**

## Error Handling

### Duplicate Detection Errors
- **Scenario**: Duplicate group detected in OnGroupCreated
- **Handling**: Log warning, skip addition, continue processing
- **User Impact**: None (transparent)

### Event Storm Detection
- **Scenario**: More than 10 events for the same group in 5 seconds
- **Handling**: Throttle events, log error, show warning to user
- **User Impact**: Warning message, monitoring may pause temporarily

### Collection Inconsistency
- **Scenario**: Group exists in FileGroups but not in Line1Groups/Line2Groups
- **Handling**: Log error, attempt to repair by adding to correct line collection
- **User Impact**: Minimal, automatic recovery

## Testing Strategy

### Unit Tests

1. **Test_OnGroupCreated_SkipsDuplicates**
   - Arrange: Add a group to FileGroups
   - Act: Call OnGroupCreated with the same GroupId
   - Assert: Collection count remains 1, warning is logged

2. **Test_OnGroupCreated_AddsNewGroups**
   - Arrange: Empty FileGroups collection
   - Act: Call OnGroupCreated with a new group
   - Assert: Group is added, count is 1

3. **Test_PerformInitialScanAsync_NoDuplicateEvents**
   - Arrange: Mock file system with groups
   - Act: Call PerformInitialScanAsync twice
   - Assert: GroupCreated event raised only once per unique GroupId

4. **Test_EventRateLimiter_ThrottlesExcessiveEvents**
   - Arrange: EventRateLimiter instance
   - Act: Call ShouldThrottle 15 times for same GroupId within 5 seconds
   - Assert: Returns true after 10th call

### Integration Tests

1. **Test_EndToEnd_NoDuplicateUIGroups**
   - Arrange: Start monitoring with test data
   - Act: Trigger initial scan
   - Assert: Each group appears exactly once in UI collections

2. **Test_RefreshOperation_ClearsAndRebuilds**
   - Arrange: Monitoring active with groups
   - Act: Call RefreshAsync
   - Assert: Groups are cleared then rebuilt without duplicates

### Property-Based Tests

1. **Property_GroupUniqueness**
   - Generate: Random sequences of GroupCreated events (including duplicates)
   - Property: FileGroups.Select(g => g.GroupId).Distinct().Count() == FileGroups.Count
   - Validates: Property 1

2. **Property_EventIdempotence**
   - Generate: Random GroupId and call OnGroupCreated N times (N > 1)
   - Property: FileGroups.Count(g => g.GroupId == generatedId) <= 1
   - Validates: Property 2

## Implementation Notes

### Priority Order
1. **Critical**: Add duplicate check in OnGroupCreated (MainWindowViewModel)
2. **High**: Add duplicate check in PerformInitialScanAsync (MonitoringOrchestrator)
3. **Medium**: Add event rate limiting
4. **Low**: Add comprehensive diagnostics logging

### Performance Considerations
- The `FirstOrDefault` check in OnGroupCreated is O(n) but acceptable since:
  - Typical group counts are < 1000
  - Check happens on UI thread which is already async
  - Alternative (Dictionary lookup) would require maintaining separate index

### Backward Compatibility
- No breaking changes to public APIs
- Existing event subscribers continue to work
- Additional logging is additive only

## Deployment Considerations

### Testing Before Deployment
1. Run unit tests to verify duplicate detection
2. Run integration tests with real file system data
3. Monitor logs for "Duplicate group detected" warnings
4. Verify UI remains responsive during initial scan

### Rollback Plan
If issues occur:
1. Revert OnGroupCreated changes
2. Keep MonitoringOrchestrator changes (they're defensive)
3. Monitor for original infinite loop symptoms

### Monitoring After Deployment
- Watch for "Duplicate group detected" warnings in logs
- Monitor UI responsiveness during startup
- Track GroupCreated event counts per scan operation
- Alert if event rate limiter triggers frequently
