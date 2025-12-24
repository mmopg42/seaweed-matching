---
Task: File Count Display Fix
Created: 2025-12-18
Status: Draft
Summary: Fix file count statistics display in UI header to show accurate counts for NIR, Normal, and Camera files
Research Required: Yes
---

# File Count Display Fix - Requirements

## 1. Goal

### 1.1 Primary Goal

The file count statistics bar at the top of the main window displays accurate, real-time counts for NIR, Normal, and Camera files.

### 1.2 Success Criteria

- [ ] NIR file counts (NIR1) display correctly (actual count from NIR directory)
- [ ] Normal folder counts (Normal1) display correctly (actual count of subdirectories)
- [ ] Camera file counts (Cam1, Cam2, Cam3) display correctly (actual counts from respective directories)
- [ ] Counts update within 3 seconds when new files/folders are created
- [ ] Counts reflect actual file system state (no phantom counts or missing files)
- [ ] UI remains responsive during count updates (no UI freezing)
- [ ] Counts reset to 0 when monitoring is stopped
- [ ] Counts refresh correctly when "Refresh" button is clicked

> **Rule**: Counts must match actual file system state within 3 seconds of change.

## 2. Constraints

### 2.1 Technical Constraints

- Must work with existing `StatisticsService` architecture
- Must not modify existing event processing optimization changes
- Must maintain 2-second monitoring interval (configurable if needed)
- Must use background threading for file counting (no UI thread blocking)
- Must work with network drives (Z:\, UNC paths)
- Cannot add significant memory overhead (counts are primitive types)

### 2.2 Business Constraints

- Must fix within current sprint (minimal changes preferred)
- Must not break existing functionality (matching statistics, group display)
- Should reuse existing infrastructure (no new services)

### 2.3 Non-Goals (Out of Scope)

- Line 2 file counts (Nir2, Normal2, Cam4-6) - will be addressed in future if Line 1 works
- Historical count tracking or analytics
- Count persistence across application restarts
- Real-time file count without polling (would require FileSystemWatcher integration)
- Optimizing count performance beyond current 2-second interval

## 3. Questions to Investigate

- [ ] Q1: Is `StatisticsService.StartMonitoringAsync()` being called during application startup?
- [ ] Q2: Are file count events (`FileCountsUpdated`) being raised by `StatisticsService`?
- [ ] Q3: Is `MainWindowViewModel.OnFileCountsUpdated()` receiving the events?
- [ ] Q4: Are the configuration paths (`Nir1Path`, `Normal1Path`, `Camera1-3Path`) set correctly?
- [ ] Q5: Do the configured paths point to existing, accessible directories?
- [ ] Q6: Is there a timing issue between service startup and UI binding?
- [ ] Q7: Are there any exceptions being swallowed in the background monitoring task?

## 4. Current Implementation Analysis

### 4.1 Architecture Flow

```
StatisticsService (Background Task: 2s interval)
    ↓ GetFileCountsAsync()
    ↓ CountFilesInDirectoryAsync() / CountDirectoriesInDirectoryAsync()
    ↓ Detects changes + Debounce (500ms)
    ↓ Raises FileCountsUpdated event (marshaled to UI thread)
    ↓
MainWindowViewModel.OnFileCountsUpdated()
    ↓ Updates NirCount, NormalCount, Cam1Count, Cam2Count, Cam3Count properties
    ↓
UI {Binding NirCount}, {Binding NormalCount}, etc.
    ↓ Displays in Statistics Bar
```

### 4.2 Known Components

**StatisticsService** (`ChronoView/Core/Analytics/StatisticsService.cs`):
- Line 58-73: `StartMonitoringAsync()` - starts background monitoring
- Line 308-366: `MonitorFileCountsAsync()` - 2-second loop, calls `GetFileCountsAsync()`
- Line 150-222: `GetFileCountsAsync()` - counts files/directories per configured path
- Line 368-390: `CountFilesInDirectoryAsync()` - counts files (for NIR, Camera)
- Line 392-414: `CountDirectoriesInDirectoryAsync()` - counts subdirectories (for Normal)

**MainWindowViewModel** (`ChronoView/UI/ViewModels/MainWindowViewModel.cs`):
- Properties: `NirCount`, `Nir2Count`, `NormalCount`, `Normal2Count`, `Cam1Count`-`Cam6Count`
- Line 1108-1133: `OnFileCountsUpdated()` - event handler that updates UI properties
- Subscribes to `_statisticsService.FileCountsUpdated` event

**UI Binding** (`ChronoView/MainWindow.xaml`):
- Lines 167-222: File Count Statistics Bar
- `{Binding NirCount}`, `{Binding NormalCount}`, `{Binding Cam1Count}`, etc.

### 4.3 Potential Issues

Based on code analysis, likely issues:

1. **Service Not Started**: `StatisticsService.StartMonitoringAsync()` may not be called
2. **Configuration Missing**: Paths (`Nir1Path`, `Normal1Path`, etc.) may be null/empty
3. **Path Not Exist**: Configured paths may point to non-existent directories
4. **Event Not Subscribed**: ViewModel may not be subscribing to the event
5. **Thread Marshaling Issue**: Events may not be marshaling to UI thread correctly
6. **Timing Issue**: Service may start before configuration is loaded

## 5. Assumptions

- `StatisticsService` is registered in DI container and injected correctly
- `MainWindowViewModel` has a valid reference to `StatisticsService`
- Application configuration is loaded successfully at startup
- Network drives are accessible (if paths are on network)
- File system permissions allow directory enumeration
- UI thread dispatcher is available for event marshaling

## 6. Dependencies

### 6.1 Blocked By

| Dependency | Status | Owner |
|------------|--------|-------|
| Configuration loading (path values) | Unknown - needs investigation | Configuration system |
| StatisticsService lifecycle | Unknown - needs investigation | Application startup |

### 6.2 Blocks

| Dependent Task | Impact if Delayed |
|----------------|-------------------|
| Normal/NIR display regression fix | Cannot verify file counts if display is broken |
| User acceptance testing | Users need accurate counts to trust the system |

---

## 7. Investigation Plan

### 7.1 Phase 1: Service Lifecycle Check

- [ ] Add logging to `MainWindow.xaml.cs` `Window_Loaded` event
- [ ] Verify `StatisticsService.StartMonitoringAsync()` is called
- [ ] Log configuration paths being passed to service
- [ ] Verify `_statisticsService.FileCountsUpdated` event subscription

### 7.2 Phase 2: Event Flow Verification

- [ ] Add debug logging to `StatisticsService.MonitorFileCountsAsync()`
- [ ] Log each call to `GetFileCountsAsync()` with timestamps
- [ ] Log count results before raising `FileCountsUpdated` event
- [ ] Verify event is being raised (log in StatisticsService)
- [ ] Verify event is received (log in MainWindowViewModel)

### 7.3 Phase 3: Configuration Verification

- [ ] Log all configured paths at startup
- [ ] Verify paths are not null/empty
- [ ] Test `Directory.Exists()` for each path
- [ ] Count files manually to compare with service results

---

## Approval

- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
- [ ] Scope boundaries are clear
- [ ] Investigation questions identified
- [ ] All blocking dependencies identified

**Next Step**: 02_research.md (investigate current behavior and root cause)
