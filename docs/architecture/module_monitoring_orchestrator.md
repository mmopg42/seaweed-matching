---
Owner: Development Team
Last Updated: 2025-12-16
Related PRs: []
Code Ref: 2025-12-16 (Sequential Scan timestamp-ordered processing)
---

# MonitoringOrchestrator - Real-time File Matching Fix

## Overview

The `MonitoringOrchestrator` class orchestrates file monitoring workflows, including initial scan, file matching, and group creation. This document details the 2024-12-14 refactoring that fixed the real-time file matching logic to use the same algorithm as the initial scan.

## Key Components

- **File**: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs`
- **Class**: `MonitoringOrchestrator`
- **Namespace**: `ChronoView.Core.FileWatching`
- **Dependencies**: `IFileGroupMatcher`, `IFileWatcher`

## Problem Fixed

### Before (Inconsistent Matching Logic)

The application used two different matching algorithms:

1. **Initial Scan** (`PerformInitialScanAsync`): Used `FileGroupMatcher.MatchFilesAsync()` - creates optimal groups based on timestamps and file relationships
2. **Real-time Monitoring** (`CreateOrUpdateGroupAsync`): Used simple time-window matching - added any file within 300 seconds (5 minutes) to the first matching group

This caused all consecutive new files to be added to the same group instead of creating separate groups.

### After (Consistent Matching Logic)

Both initial scan and real-time monitoring now use `FileGroupMatcher.MatchFilesAsync()` for consistent grouping behavior.

## Contracts

### Inputs
- **Configuration**: `ApplicationConfiguration` containing path settings and time windows
- **File Events**: File system events from `IFileWatcher`
- **File Paths**: Absolute paths to NIR/Normal/Camera files

### Outputs
- **Groups**: `FileGroup` objects containing matched files
- **Events**: `GroupCreated`, `GroupUpdated`, `GroupRemoved` events

### Errors/Exceptions
- `InvalidOperationException`: When configuration is not set
- Logs warnings for files that cannot be matched

### Side Effects
- Updates `_activeGroups` dictionary (thread-safe with locks)
- Raises events for UI updates
- Logs matching operations

## Logic Flow

### User Workflows (2025-12-15)

**Refresh Button** (Independent Operation):
- Purpose: Scan existing files only, no real-time monitoring
- Flow:
  1. Clear all existing groups from `_activeGroups`
  2. Call `PerformInitialScanAsync()`
  3. Populate UI with scanned groups
  4. Does NOT start FileWatcher
- Use Case: Update UI after external file changes

**Start Button** (Full Monitoring):
- Purpose: Initial scan + real-time monitoring
- Flow:
  1. Call `PerformInitialScanAsync()`
  2. Start `FileWatcher` for real-time detection
  3. Set `_isMonitoring = true`
  4. Continue monitoring until Stop
- Use Case: Begin production monitoring session

### Initial Scan (NEEDS REFACTORING - 2025-12-15)

> [!WARNING]
> **Known Issue**: Initial scan does NOT use Match 3 timestamp-based matching
> - Currently creates all groups via `FileGroupMatcher.MatchFilesAsync()` in one batch
> - Does NOT call `FindMatchingExistingGroup()` → Match 3 is skipped
> - Result: NIR and Camera files create separate groups even with matching timestamps
> - **Fix planned**: See `docs/spec/fix-initial-scan-matching/`

**Current Flow** (problematic):
1. Scan all configured directories for files
2. Collect files into `UnmatchedFiles` structure
3. Call `FileGroupMatcher.MatchFilesAsync()` with all files
4. Store resulting groups in `_activeGroups`
5. Raise `GroupCreated` events for UI

**Planned Fix**:
1. Get `DataSequenceSettings.GetOrderedTypes()`
2. Process files sequentially by priority order
3. First type → `CreateNewGroupAsync()`
4. Later types → `CreateOrUpdateGroupAsync()` (uses Match 3)

### Real-time Processing (Enhanced 2025-12-15)
1. Receive file system event (Created/Changed/Deleted)
2. **Call `CreateOrUpdateGroupAsync()`**:
   - Create `UnmatchedFiles` for single file using `CreateUnmatchedFilesForSingleFile()`
   - Call `FileGroupMatcher.MatchFilesAsync()` (same as initial scan)
   - Check if returned group matches existing group via `FindMatchingExistingGroup()`
     - Match 1: NormalFolder
     - Match 2: NirKey
     - **Match 3**: Timestamp + Tolerance + Temporal Ordering ✓
   - If match found: merge using `MergeGroups()`, raise `GroupUpdated`
   - If no match: add new group, raise `GroupCreated`

## Dependencies

### Internal
- `IFileGroupMatcher` (ChronoView.Core.FileMatching): Core matching algorithm
- `IFileWatcher` (ChronoView.Core.FileWatching): File system monitoring
- `FileGroup` (ChronoView.Models): Group data model
- `UnmatchedFiles` (ChronoView.Core.FileMatching): Input structure for matcher

### External
- `Microsoft.Extensions.Logging.ILogger`: Logging
- `System.IO`: File system operations
- `System.Threading.Tasks`: Async operations

### Config/Env
- `ApplicationConfiguration.DataSequenceSettings`: Primary source for time-based matching tolerances
- `ApplicationConfiguration.MatchingSettings.NirTimeWindowSeconds`: **DEPRECATED** NIR matching tolerance (default: 300) - fallback only
- `ApplicationConfiguration.MatchingSettings.CameraTimeWindowSeconds`: **DEPRECATED** Camera matching tolerance (default: 2) - fallback only
- `ApplicationConfiguration.MatchingSettings.NormalFolderTimeWindowSeconds`: **DEPRECATED** Normal folder matching tolerance (default: 120) - fallback only
- `ApplicationConfiguration.MatchingSettings.*Path`: Monitored directory paths
- `ApplicationConfiguration.WorkflowSettings.EnableNetworkDrivePolling`: Enable polling (default: true)
- `ApplicationConfiguration.WorkflowSettings.PollingIntervalMs`: Polling interval (default: 5000)

### Runtime Resources
- Filesystem (read): Scans configured directories
- Memory: Maintains `_activeGroups` dictionary
- CPU: File matching algorithm

## Dependents
<!-- VERIFY: grep -rn "MonitoringOrchestrator" ChronoView/ -->
<!-- VERIFY: grep -rn "IMonitoringOrchestrator" ChronoView/ -->

- **Used By**: 
  - `MainWindowViewModel` (ChronoView/ViewModels/MainWindowViewModel.cs): Primary consumer
  - Dependency injection container (App.xaml.cs): Service registration

- **Shared Variables/Constants**: 
  - `_activeGroups`: Dictionary of active file groups (thread-safe)
  - `_processedFiles`: Debouncing dictionary for file events

- **Exported Interfaces**: 
  - `IMonitoringOrchestrator`: Public interface
  - Events: `GroupCreated`, `GroupUpdated`, `GroupRemoved`, `FileGroupsCreated`, `FileGroupsUpdated`, `MonitoringError`

## Key Methods

### 1. RefreshAsync (Enhanced 2025-12-15)
**Purpose**: Re-scan existing files without starting real-time monitoring

**Before** (2024-12-14):
```csharp
if (!_isMonitoring)
{
    _logger.LogWarning("Cannot refresh - monitoring is not active");
    return; // ❌ Couldn't refresh unless Start was pressed
}
```

**After** (2025-12-15):
```csharp
// ✅ Works independently - no monitoring check
// Clear existing groups
lock (_lockObject) { /* remove all */ }

// Perform new scan (whether monitoring is active or not)
var result = await PerformInitialScanAsync(cancellationToken);
```

**User Intent**:
- Refresh = Initial scan ONLY
- Start = Initial scan + Real-time monitoring

### 2. CreateOrUpdateGroupAsync (Refactored)
**Before**:
```csharp
// Simple time-based matching
var timestamp = ExtractTimestamp(filePath, fileType);
var existingGroup = _activeGroups.Values
    .FirstOrDefault(g => IsMatchingTimestamp(g, timestamp.Value, fileType));
if (existingGroup != null)
{
    UpdateGroupWithFile(existingGroup, filePath, fileType);
}
```

**After**:
```csharp
// Uses FileGroupMatcher for consistency
var unmatchedFiles = CreateUnmatchedFilesForSingleFile(filePath, fileType);
var matchedGroups = await _fileGroupMatcher.MatchFilesAsync(unmatchedFiles);
var newGroup = matchedGroups.FirstOrDefault();
var existingGroup = FindMatchingExistingGroup(newGroup);
if (existingGroup != null)
{
    MergeGroups(existingGroup, newGroup);
}
```

### 3. CreateUnmatchedFilesForSingleFile (New Helper Method)
- **Purpose**: Converts a single file into `UnmatchedFiles` structure for FileGroupMatcher
- **Input**: `string filePath`, `FileType fileType`
- **Output**: `UnmatchedFiles?` (null if conversion fails)
- **Logic**: 
  - Determines line number (1 or 2) based on path
  - Formats file data according to type (NIR/Normal/Camera)
  - Returns structure ready for `FileGroupMatcher.MatchFilesAsync()`

### 4. FindMatchingExistingGroup (Enhanced 2025-12-15)
- **Purpose**: Finds existing group that matches the newly created group
- **Input**: `FileGroup newGroup`
- **Output**: `FileGroup?` (null if no match)
- **Logic**: Three-tier matching strategy with Non-Duplicate Filters
  1. **Match 1: By NormalFolder** (primary stable identifier) - **UPDATED 2025-12-15**
     - `NormalFolder` is unique per Normal folder (e.g., "C251201T140543_0")
     - Most reliable matching method
     - **Non-Duplicate Filter**: `.Where(g => !HasDataType(g, newGroupDataType))`
     - Only matches groups that DON'T already have the new data type
     - Prevents camera files from overwriting existing camera data
  2. **Match 2: By NirKey** (secondary stable identifier) - **UPDATED 2025-12-15**
     - `NirKey` is unique per NIR file (e.g., "20251201T140543")
     - Used for NIR-only groups
     - **Non-Duplicate Filter**: `.Where(g => !HasDataType(g, newGroupDataType))`
     - Only matches groups that DON'T already have the new data type
     - Consistent with Match 1 behavior
  3. **Match 3: By Timestamp + LineNumber + Priority** (UPDATED 2025-12-16)
     - Uses `DataSequenceSettings` tolerances and order
     - Matches groups within absolute time tolerance (no temporal ordering)
       - Example: NIR(Order=1, Time=14:05:58) matches Normal(Order=2, Time=14:05:58) if |diff| ≤ 50s
       - Order determines matching priority, NOT chronological order
     - Filters candidates within tolerance window
     - **Non-Duplicate Filter**: `.Where(g => !HasDataType(g, newGroupDataType))`
     - Prevents multiple files of same type from overwriting same group
     - Example: If adding Cam1, only match groups without Cam1 yet
  4. **No match** → return null (will create new group)
  - Thread-safe (uses lock)

**Match 3 Details** (2025-12-15):
- **Why added**: Handle out-of-order file arrivals using configurable tolerances
- **Temporal Ordering**: Prevents matching files that arrived in wrong sequence order
- **Tolerance-based**: Uses per-type `TimeToleranceSeconds` from DataSequenceSettings
- **Priority-aware**: Only matches with higher-priority (earlier-arriving) types

### 5. MergeGroups (New Helper Method) - **CRITICAL FIX 2025-12-16**
- **Purpose**: Merges new group data into existing group
- **Input**: `FileGroup existingGroup`, `FileGroup newGroup`
- **Output**: void (modifies existingGroup in-place)
- **Logic**:
  - **CRITICAL FIX (2025-12-16)**: Merges stable identifiers FIRST
    - Copies `NormalFolder` if present in newGroup and empty in existingGroup
    - Copies `NirKey` if present in newGroup and empty in existingGroup
    - **WHY CRITICAL**: Non-Duplicate Filter relies on `HasDataType()` which checks these fields
    - Without this, groups appear to "not have" data types even after merging
    - Example: NIR group merges Normal data but `NormalFolder` stays null → `HasDataType(g, Normal)` returns false → next Normal overwrites!
  - Copies NIR file path if present
  - Copies Main Image path if present
  - **Defensive camera merge** (2025-12-15):
    - Only adds camera files if key doesn't exist or is empty
    - Logs warning when skipping merge (duplicate key)
    - Logs debug when successfully merging
    - Prevents overwriting existing camera data
  - Preserves existing data (never overwrites)

## Failure Modes & Recovery

### Common Failures
1. **File locked/inaccessible**: Logged as warning, operation skipped
2. **Timestamp extraction fails**: File not matched, logged as warning
3. **FileGroupMatcher returns no groups**: Logged as warning, file not added

### Recovery Strategy
- All errors are caught and logged
- Failed operations don't crash the application
- Monitoring continues even if individual files fail

## Edge Cases

1. **Multiple files with identical timestamps**: FileGroupMatcher handles via internal logic
2. **GroupId collision**: Timestamp-based fallback matching (within 1 second tolerance)
3. **Camera file without matching group**: Creates minimal standalone group
4. **Files arriving faster than processing**: Debouncing via `_processedFiles` dictionary

## Impact / Touchpoints
<!-- VERIFY: grep -rn "CreateOrUpdateGroupAsync" ChronoView/ -->
<!-- VERIFY: grep -rn "FileGroupMatcher" ChronoView/ -->

> [!IMPORTANT]
> **Before modifying this code, you MUST:**
> 1. Run ALL verification commands in this document
> 2. Check each location listed below

- `ProcessFileEventsAsync` (line 819): Calls `CreateOrUpdateGroupAsync()` for file events
- `PerformInitialScanAsync` (line 216): Uses same `FileGroupMatcher.MatchFilesAsync()` for initial scan
- `MainWindowViewModel`: Subscribes to `GroupCreated`/`GroupUpdated` events
- `FileGroupMatcher`: Must return consistent GroupId format for matching to work

> [!WARNING]
> **Changing the matching logic will affect:**
> - How files are grouped in real-time monitoring
> - UI updates (which groups appear/update)
> - File operation planning (which files belong together)

## Related Docs

- `module_file_group_matcher.md`: Core matching algorithm documentation
- `module_file_watcher_service.md`: File system monitoring implementation
- `impact_deprecated_matching_properties.md`: Deprecated properties migration guide
- `.kiro/specs/refactor-matching-logic/`: Refactoring spec and tasks

## Verification Commands

To verify this documentation matches current code:

```bash
# Check CreateOrUpdateGroupAsync usage
grep -rn "CreateOrUpdateGroupAsync" ChronoView/Core/FileWatching/

# Check FileGroupMatcher usage
grep -rn "MatchFilesAsync" ChronoView/Core/FileWatching/

# Check helper methods exist
grep -rn "CreateUnmatchedFilesForSingleFile" ChronoView/Core/FileWatching/
grep -rn "FindMatchingExistingGroup" ChronoView/Core/FileWatching/
grep -rn "MergeGroups" ChronoView/Core/FileWatching/

# Verify active groups dictionary
grep -rn "_activeGroups" ChronoView/Core/FileWatching/
```

## UI Refresh Fix (2024-12-14)

### Problem: Orphan Groups in UI

**Symptom**: After files deleted externally, Refresh left empty groups in UI
- User deletes all data files from filesystem
- UI shows 25 groups, but backend `_activeGroups` only has 1
- Refresh only removes 1 group (the one in backend)
- Other 24 groups orphaned in UI

**Root Cause**:
```csharp
// Old code (MainWindowViewModel.ExecuteRefreshAsync)
// Note: We do NOT explicitly clear file groups here.
// Orchestrator.RefreshAsync will trigger GroupRemoved events which will clear the UI.

// ❌ Problem: Backend only removes groups it knows about!
// Orphaned UI groups won't get GroupRemoved events
```

**Fix Applied**:
```csharp
// Clear ALL UI groups BEFORE refresh
await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
{
    FileGroups.Clear();
    Line1Groups.Clear();
    Line2Groups.Clear();
});

// Then refresh - repopulates with only existing files
await _orchestrator.RefreshAsync(cancellationToken);
```

**Why this fix was needed**:
- Backend `_activeGroups` is source of truth
- UI can get out of sync when files deleted externally (without proper events)
- Relying on GroupRemoved events assumes backend knows about all UI groups
- Explicit clear ensures UI and backend are synchronized after Refresh

**Modified File**: `ChronoView/UI/ViewModels/MainWindowViewModel.cs` (line 1314-1327)

## Changelog

- **2025-12-16 (Late Night #3)**: CRITICAL REFACTOR - FileMatchingEngine Now Uses DataSequenceSettings Order
  - **Problem**: FileMatchingEngine had hardcoded Normal→Cam1→Cam2→Cam3 matching logic
    - Cam1 always matched to Normal timestamp (ignored Order)
    - Cam2/Cam3 always matched to Cam1 timestamp (ignored Order)
    - NIR matched to all groups (ignored Order)
    - Completely ignored user-configured DataSequenceSettings.Order
  - **Root Cause**: Legacy Normal-centric architecture from initial Python port
    - `BuildLineGroups()` forced Normal as anchor, then attached cameras in hardcoded order
    - No use of `GetOrderedTypes()` or dynamic matching
  - **Solution**: Complete refactor of `BuildLineGroups()`
    - **NEW**: `BuildLineGroupsWithOrder()` - Uses DataSequenceSettings.Order dynamically
    - Collects files by DataType (NIR, Normal, Cam1, Cam2, Cam3)
    - Processes files in Order sequence (e.g., NIR(1)→Normal(2)→Cam1(3)→Cam2(4)→Cam3(5))
    - Each file matches to previous Order's DataType within tolerance
    - Example: Cam2(Order=4) matches Cam1(Order=3), NOT hardcoded Cam1
    - **Legacy fallback**: `BuildLineGroupsLegacy()` preserves old logic if no DataSequenceSettings
  - **Additional Fixes**:
    - Fixed NIR timestamp extraction: `run_\d+` → `run_\d` (greedy → single digit)
    - Added helper methods: `HasDataTypeInGroup()`, `MergeFileIntoGroup()`, `CreateGroupFromFile()`
  - **Result**: FileMatchingEngine now respects user-configured Order for ALL data types
  - **Code Locations**:
    - `FileMatchingEngine.cs:175-327` (BuildLineGroupsWithOrder - NEW)
    - `FileMatchingEngine.cs:332-625` (BuildLineGroupsLegacy - refactored from old BuildLineGroups)
    - `FileMatchingEngine.cs:868` (NIR regex fix)
    - `FileMatchingEngine.cs:904-1006` (Helper methods)
  - **Impact**: Both initial scan (via FileGroupMatcher) and real-time (via MonitoringOrchestrator) now use correct Order

- **2025-12-16 (Late Night #2)**: CRITICAL FIX - Removed Temporal Ordering Constraint
  - **Problem**: NIR files couldn't match to Normal folders even with matching timestamps
  - **Root Cause**: Temporal Ordering Constraint assumed Order=1 files arrive BEFORE Order=2 files
    - Reality: NIR(Order=1) timestamp is 14:05:58, but Normal(Order=2) starts at 14:05:50
    - NIR 14:05:58 should match Normal 14:05:58 (±50s tolerance)
    - Constraint rejected match because Normal.Time < NIR.Time violated "Order=1 must be earlier" rule
  - **Solution**: Removed Temporal Ordering Constraint (Lines 1185-1187)
    - Changed to absolute time difference: `Math.Abs((newGroup.Timestamp - g.Timestamp).TotalSeconds)`
    - Now matches ANY group within tolerance range, regardless of which came first
    - Order only determines matching priority, NOT timestamp order
  - **Result**: 136 rows → ~100 rows (expected)
  - **Code Location**: `MonitoringOrchestrator.cs:1178` (Match 3 time difference check)
  - **Impact**: Order now means "importance/priority", not "temporal sequence"

- **2025-12-16 (Late Night)**: CRITICAL FIX - Removed FileMatchingEngine from Real-time Matching
  - **Problem**: `CreateOrUpdateGroupAsync` was using `FileMatchingEngine` (via `_fileGroupMatcher.MatchFilesAsync()`)
  - **Root Cause**: FileMatchingEngine uses legacy Normal-centric logic
    - Camera files matched based on Cam1 timestamp, not DataSequenceSettings order
    - Ignored user-configured priority order in DataSequenceSettings
    - Example: User sets Order as NIR→Normal→Cam1, but engine forced Normal→Cam1→Cam2→Cam3
  - **Solution**: Added `CreateGroupFromSingleFile()` helper method
    - Creates FileGroup directly from single file (NIR/Normal/Camera)
    - Extracts timestamp from filename and sets group properties
    - Bypasses FileMatchingEngine completely
    - `FindMatchingExistingGroup` (Match 1/2/3) handles all matching logic
  - **Result**: ALL matching now uses DataSequenceSettings exclusively
  - **Code Locations**:
    - `MonitoringOrchestrator.cs:946-1071` (CreateGroupFromSingleFile - NEW)
    - `MonitoringOrchestrator.cs:775-784` (CreateOrUpdateGroupAsync - MODIFIED)
  - **Impact**: Fixes matching for ALL file types in both initial scan and real-time monitoring
  - **Note**: FileMatchingEngine.cs still exists but is no longer called (can be removed in future cleanup)

- **2024-12-16**: Updated documentation for deprecated matching properties (Phase 5)
  - Marked `NirTimeWindowSeconds`, `CameraTimeWindowSeconds`, `NormalFolderTimeWindowSeconds` as DEPRECATED
  - Added reference to `impact_deprecated_matching_properties.md`
  - `GetMatchingTolerance()` now uses DataSequenceSettings as primary source
  - Deprecated properties used only as fallback for backward compatibility

- **2025-12-16 (Evening)**: CRITICAL REDESIGN - Sequential Scan Timestamp-Ordered Processing
  - **Problem**: Sequential scan processed files by DataType order, not timestamp order
  - **Root Cause**: 
    - Old logic: Process all Normal → all NIR → all Cameras
    - This violated temporal ordering when files arrived out of sequence
    - Example: Normal(14:05:43) processed before NIR(14:05:42) → NIR couldn't match
  - **Solution**: Complete redesign of Sequential Scan
    1. Scan all files from all data types
    2. Extract timestamps for each file
    3. **Sort by timestamp (chronological order)**
    4. Process files in timestamp order using `CreateOrUpdateGroupAsync`
  - **Benefits**:
    - Respects actual temporal sequence of data collection
    - Match 3 Temporal Ordering Constraint works correctly
    - Adapts automatically when DataSequenceSettings order changes
    - Example: If user changes NIR to Order=1, system automatically expects NIR.Time ≤ Normal.Time
  - **Code Location**: `MonitoringOrchestrator.cs:~495` (PerformSequentialInitialScanAsync)
  - **Impact**: Fixes grouping for ALL scenarios with min=0, max=50 tolerances

- **2025-12-16**: CRITICAL FIX - MergeGroups Missing Stable Identifiers
  - **Problem**: Sequential initial scan still merged all files into first group despite Non-Duplicate Filters
  - **Root Cause**: `MergeGroups()` didn't copy `NormalFolder` and `NirKey` fields
    - NIR group created with `NormalFolder = null`
    - First Normal folder merged via Match 3, but `NormalFolder` stayed null
    - `HasDataType(g, Normal)` checks `!string.IsNullOrEmpty(g.NormalFolder)` → returns false
    - Second Normal folder sees group as "doesn't have Normal yet" → overwrites!
  - **Fix**: Added stable identifier merging to `MergeGroups()`
    - Copies `NormalFolder` if present in newGroup and empty in existingGroup
    - Copies `NirKey` if present in newGroup and empty in existingGroup
    - Now `HasDataType()` correctly detects merged data types
  - **Code Location**: `MonitoringOrchestrator.cs:~1100` (MergeGroups method)
  - **Impact**: Non-Duplicate Filters now work correctly - each data type instance creates separate groups

- **2025-12-15 (Night)**: CRITICAL FIX - Sequential Scan Overwrite Issue
  - **Problem**: Sequential initial scan merged all files into first group
  - **Root Cause**: Match 1, 2, 3 didn't have Non-Duplicate Filters
    - All Normal folders matched to same NIR group (Range=0~50s, all within range)
    - All Camera files matched to same group
    - Result: Only 1 group created instead of 4+ groups
  - **Fix**: Added Non-Duplicate Filter to Match 1 and Match 2
    - `.Where(g => !HasDataType(g, newGroupDataType))` added to both
    - Match 3 already had this filter
    - Now: Already merged groups are excluded from matching
  - **Result**: Each file type instance creates or updates unique groups
  - **Code Locations**:
    - `MonitoringOrchestrator.cs:~950` (Match 1 Non-Duplicate Filter)
    - `MonitoringOrchestrator.cs:~975` (Match 2 Non-Duplicate Filter)
  - **Impact**: Fixes both initial scan and real-time monitoring grouping
  - **NOTE**: This fix was incomplete - see 2025-12-16 fix above

- **2025-12-15 (Evening)**: CRITICAL FIX - Camera Overwrite in Match 1/2 and MergeGroups
  - **Problem**: Camera files were being overwritten in real-time monitoring
  - **Root Cause 1**: Match 1 and Match 2 didn't have Non-Duplicate Filters
    - Match 1 (NormalFolder) matched camera files to existing Normal groups without checking data type
    - Match 2 (NirKey) had same issue
    - Only Match 3 had the Non-Duplicate Filter
  - **Root Cause 2**: MergeGroups unconditionally overwrote camera dictionary keys
    - `existingGroup.CameraFiles[key] = value` replaced existing values
    - No defensive check before assignment
  - **Fix 1**: Added Non-Duplicate Filter to Match 1 and Match 2
    - `.Where(g => !HasDataType(g, newGroupDataType))` added to both
    - Consistent behavior across all three matching strategies
    - Added diagnostic logging for duplicate detection
  - **Fix 2**: Made MergeGroups defensive
    - Added `ContainsKey()` check before camera file assignment
    - Only merges if key doesn't exist or is empty
    - Logs warning when skipping merge, debug when successful
  - **Result**: Each camera file now creates or updates a unique group
  - **Code Locations**:
    - `MonitoringOrchestrator.cs:~950` (Match 1 Non-Duplicate Filter)
    - `MonitoringOrchestrator.cs:~975` (Match 2 Non-Duplicate Filter)
    - `MonitoringOrchestrator.cs:~1103` (MergeGroups defensive logic)
  - **Impact**: Fixes camera overwrite for ALL file types in real-time monitoring

- **2024-12-14 (Night)**: Fixed Ghost Group Issue
  - **CRITICAL FIX**: `PerformInitialScanAsync` now strictly filters camera files
  - Logic: Only files matching `YYYYMMDD_HHMMSS` naming convention are processed
  - Reason: `Directory.GetFiles` was picking up system files (e.g. Thumbs.db), creating empty groups
  - Result: Garbage files are now ignored and logged as Debug messages

- **2024-12-14 (Night)**: Fixed GroupId Collision Issue
  - **CRITICAL FIX**: Implemented `_nextGroupId` counter in MonitoringOrchestrator
  - Logic: Assigns unique sequential IDs (e.g. group_002) to new groups from real-time events
  - Reason: `FileGroupMatcher` always returns 'group_001' for single files, causing overwrites
  - Result: New groups are correctly created with unique IDs

- **2024-12-14 (Night)**: Fixed Data Detection Logic (Normal/NIR)
  - **Problem**: `DetermineFileType` relied on hardcoded substrings ("Normal", "NIR") and incorrect config properties
  - **Fix**: Updated to strict prefix matching against `config.MatchingSettings` paths (Normal1Path, etc.)
  - **Result**: Files are now correctly classified regardless of folder naming conventions

- **2024-12-14 (Evening)**: Fixed UI Refresh orphan group issue
  - Modified `MainWindowViewModel.ExecuteRefreshAsync()` to clear all UI groups before refresh
  - Prevents orphan groups when backend doesn't know about some groups
  - Related: External file deletion without proper events

- **2024-12-14 (Afternoon)**: Fixed real-time matching with stable identifiers
  - **CRITICAL FIX**: `FindMatchingExistingGroup()` now uses NormalFolder/NirKey instead of GroupId
  - Reason: GroupId is regenerated on every MatchFilesAsync call - NOT stable
  - Removes unstable GroupId matching and unreliable timestamp fallback
  - Ensures new files match to correct existing groups or create new ones

- **2025-12-15**: Fixed RefreshAsync to Work Independently
  - **Breaking Change**: Removed `_isMonitoring` check from `RefreshAsync()`
  - **User Intent Clarification**:
    - **Refresh Button**: Initial scan ONLY (no real-time monitoring)
    - **Start Button**: Initial scan + Real-time monitoring
  - **Modified Files**: `MonitoringOrchestrator.cs`, `MainWindowViewModel.cs`
  - **Result**: Refresh now works without requiring Start button first

- **2025-12-15 (Evening)**: CRITICAL FIX - Race Condition in CreateOrUpdateGroupAsync
  - **Problem**: Multiple images from the same Normal folder arriving simultaneously created duplicate groups
  - **Root Cause**: TOCTOU (Time-Of-Check-Time-Of-Use) race condition
    - `FindMatchingExistingGroup()` was called OUTSIDE the lock
    - Thread A checks for match → not found
    - Thread B checks for match → not found (Thread A hasn't added yet)
    - Both threads create separate groups for the same folder
  - **Fix**: Moved lock acquisition to BEFORE `FindMatchingExistingGroup()`
    - Lock now protects the entire atomic operation: check + add
    - Events (`OnGroupCreated`, `OnGroupUpdated`) moved outside lock to avoid deadlocks
  - **Code Location**: `MonitoringOrchestrator.cs:786-822` (CreateOrUpdateGroupAsync)
  - **Impact**: Fixes group overwriting issue for ALL file types (Normal, NIR, Camera)

- **2025-12-15 (Evening)**: CRITICAL FIX - Match 3 Non-Duplicate Filter
  - **Problem**: Multiple files of same type (e.g., Cam1, Cam2, Cam3) were all matching to the SAME group
  - **Root Cause**: Match 3 didn't check if group already had that data type → kept overwriting
  - **Example Failure**: 5 Cam1 files all matched to first NIR group instead of 5 different groups
  - **Fix**: Added `.Where(g => !HasDataType(g, newGroupDataType))` filter to Match 3 candidates
  - **Result**: Each data instance now matches to a UNIQUE group without that data type yet
  - **Code Location**: `MonitoringOrchestrator.cs:995` (Match 3 candidate filtering)

- **2025-12-15 (Evening)**: CRITICAL FIX - Camera Dictionary Key Mismatch
  - **Problem**: Camera files were incorrectly identified as `DataType.Normal` instead of `Cam1/Cam2/Cam3`
  - **Root Cause**: Key mismatch in `DetermineDataTypeForGroup()` and `HasDataType()`
    - Storing: `group.CameraFiles["cam1"]` (with "cam" prefix)
    - Checking: `ContainsKey("1")` (without "cam" prefix) → ALWAYS FALSE
  - **Result**: All Camera files fell back to `DataType.Normal`, causing wrong priority matching
  - **Fix**: Updated both methods to use consistent `"cam1"`, `"cam2"`, `"cam3"` keys
  - **Code Locations**:
    - `MonitoringOrchestrator.cs:1718-1723` (DetermineDataTypeForGroup)
    - `MonitoringOrchestrator.cs:1737-1739` (HasDataType)
  - **Impact**: Camera files now correctly match with proper priority order in Match 3

- **2025-12-15**: Added Match 3 Timestamp-Based Matching
  - **Feature**: `FindMatchingExistingGroup()` now includes Match 3 logic
  - **Goal**: Match files by timestamp + tolerance when NormalFolder/NirKey unavailable
  - **Key Addition**: **Temporal Ordering Constraint** - files must arrive in DataSequenceSettings order
  - **Integration**: Uses `DataSequenceSettings.GetTolerance()` for per-type tolerances
  - **Helper Methods**: `DetermineDataTypeForGroup()`, `HasDataType()`, `GetPriority()`
  - **Limitation**: Match 3 only works in real-time mode, NOT initial scan (see Warning above)

- **2025-12-15**: Added Network Drive Polling Support
  - **Feature**: `StartAsync` now configures `FileWatcherService` with polling options
  - **Goal**: Enable reliable detection of files on network drives (`Z:`)
  - **Integration**: Injection of `WorkflowSettings` polling configuration

- **2024-12-14 (Morning)**: Refactored `CreateOrUpdateGroupAsync` to use `FileGroupMatcher` for consistency
  - Added `CreateUnmatchedFilesForSingleFile()` helper method
  - Added `FindMatchingExistingGroup()` helper method (later fixed for stable IDs)
  - Added `MergeGroups()` helper method
  - Removed reliance on `IsMatchingTimestamp()` for real-time matching
  - Fixed issue where all new files were added to same group
