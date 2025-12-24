---
Owner: Development Team
Last Updated: 2025-12-15
Purpose: Single Source of Truth for naming conventions
---

# Glossary

> **CRITICAL**: All architecture documents MUST use terms exactly as defined here.  
> If you need a new term, ADD IT HERE FIRST before using in any document.

## How to Use This Glossary

1. **Before writing any architecture doc**: Search this file for existing terms
2. **Before introducing new naming**: Add to appropriate section below
3. **If conflict found**: Update ALL documents to match glossary

## Verification Command
```bash
# Check if a term exists in glossary
grep -n "term_name" docs/architecture/glossary.md

# Find all usages of a term across docs
grep -rn "term_name" docs/architecture/
```

---

## Classes & Interfaces

| Official Name | Description | Defined In |
|---------------|-------------|------------|
| `MonitoringOrchestrator` | Orchestrates file monitoring workflows | `module_monitoring_orchestrator.md` |
| `IMonitoringOrchestrator` | Interface for MonitoringOrchestrator | `module_monitoring_orchestrator.md` |
| `FileGroupMatcherService` | Implements file grouping algorithm | `module_file_group_matcher.md` |
| `IFileGroupMatcher` | Interface for file matching algorithm | `module_file_group_matcher.md` |
| `FileGroup` | Model representing a group of matched files | `ChronoView.Models` |
| `IFileWatcher` | Interface for file system monitoring | `ChronoView.Core.FileWatching` |
| `UnmatchedFiles` | Input structure for file matcher | `ChronoView.Core.FileMatching` |
| `ApplicationConfiguration` | Main application configuration model | `ChronoView.Models` |
| `FileWatcherOptions` | Configuration options for file watcher | `ChronoView.Core.FileWatching` |
| `FileWatcherService` | Hybrid file monitoring service implementation | `ChronoView.Core.FileWatching` |
| `FileMatchingEngine` | Standalone matching engine with pure logic (stateless, framework-agnostic) | `module_file_matching_engine.md` |
| `NirSpectrumFilter` | NIR spectrum filtering using 5-criteria scoring system | `ChronoView.Core.Nir` |
| `CriteriaResult` | Individual criterion evaluation result with value, threshold, and pass/fail status | `ChronoView.Core.Nir` |
| `FilterResult` | Result of NIR spectrum analysis with detailed scoring information | `ChronoView.Core.Nir` |

> **Naming Convention**: Use `PascalCase` for classes and interfaces.

---

## Methods & Functions

| Official Name | Signature | Description | Defined In |
|---------------|-----------|-------------|------------|
| `CreateOrUpdateGroupAsync` | `(string filePath, FileType fileType) -> Task<FileGroup?>` | Creates new group or updates existing based on file | `module_monitoring_orchestrator.md` |
| `CreateUnmatchedFilesForSingleFile` | `(string filePath, FileType fileType) -> UnmatchedFiles?` | Converts single file to UnmatchedFiles structure | `module_monitoring_orchestrator.md` |
| `FindMatchingExistingGroup` | `(FileGroup newGroup) -> FileGroup?` | Finds existing group matching new group | `module_monitoring_orchestrator.md` |
| `MergeGroups` | `(FileGroup existingGroup, FileGroup newGroup) -> void` | Merges new group data into existing group | `module_monitoring_orchestrator.md` |
| `PerformInitialScanAsync` | `(CancellationToken) -> Task<OrchestrationResult>` | Performs initial file scan | `module_monitoring_orchestrator.md` |
| `ProcessFileEventsAsync` | `(List<FileSystemEventArgs>) -> Task<List<FileGroup>>` | Processes file system events | `module_monitoring_orchestrator.md` |
| `MatchFilesAsync` | `(UnmatchedFiles) -> Task<IEnumerable<FileGroup>>` | Core file matching algorithm | `module_file_group_matcher.md` |
| `BuildAllGroups` | `(UnmatchedFiles) -> List<FileGroup>` | Main grouping logic (private) | `module_file_group_matcher.md` |
| `BuildLineGroups` | `(UnmatchedFiles, int lineNumber) -> List<FileGroup>` | Per-line grouping logic (private) | `module_file_group_matcher.md` |

> **Naming Convention**: Use `PascalCase` for methods, async methods end with `Async`.

---

## Variables & Properties

| Official Name | Type | Description | Used In |
|---------------|------|-------------|---------|
| `_activeGroups` | `Dictionary<string, FileGroup>` | Dictionary of active file groups (thread-safe) | `module_monitoring_orchestrator.md` |
| `_processedFiles` | `Dictionary<string, DateTime>` | Debouncing dictionary for file events | `module_monitoring_orchestrator.md` |
| `GroupId` | `string` | Sequential identifier (regenerated on each MatchFilesAsync call) | `module_file_group_matcher.md` |
| `NormalFolder` | `string?` | Normal folder name (STABLE identifier) | `module_file_group_matcher.md` |
| `NirKey` | `string?` | NIR filename without extension (STABLE identifier) | `module_file_group_matcher.md` |
| `Timestamp` | `DateTime` | Group timestamp (from matched files) | `FileGroup` model |
| `LineNumber` | `int` | Production line number (1 or 2) | `FileGroup` model |
| `HasNir` | `bool` | Indicates if group has NIR file | `FileGroup` model |
| `NirFilePath` | `string?` | Path to NIR .spc file | `FileGroup` model |
| `MainImagePath` | `string?` | Path to main image folder | `FileGroup` model |
| `CameraFiles` | `Dictionary<string, string>` | Camera files (key: "cam1"-"cam6", value: file path) | `FileGroup` model |

> **Naming Convention**: Use `_camelCase` for private fields, `PascalCase` for public properties.

---

## Configuration Keys

| Official Name | Type | Default | Description | Used In |
|---------------|------|---------|-------------|---------|
| `MatchingSettings.NirMatchTimeDiff` | `double` | `1.0` | NIR attachment tolerance (seconds) | `module_file_group_matcher.md` |
| `MatchingSettings.NirTimeWindowSeconds` | `int` | `300` | **DEPRECATED** Time window for NIR file matching (seconds) - Use DataSequenceSettings instead | `impact_deprecated_matching_properties.md` |
| `MatchingSettings.CamMatchMinDiff` | `double` | `4.0` | Camera matching minimum time difference (seconds) | `module_file_group_matcher.md` |
| `MatchingSettings.CamMatchMaxDiff` | `double` | `6.0` | Camera matching maximum time difference (seconds) | `module_file_group_matcher.md` |
| `MatchingSettings.CameraTimeWindowSeconds` | `int` | `2` | **DEPRECATED** Time window for camera file matching (seconds) - Use DataSequenceSettings instead | `impact_deprecated_matching_properties.md` |
| `MatchingSettings.NormalFolderTimeWindowSeconds` | `int` | `120` | **DEPRECATED** Time window for normal folder matching (seconds) - Use DataSequenceSettings instead | `impact_deprecated_matching_properties.md` |
| `MatchingSettings.Nir1Path` | `string` | `""` | NIR files path for Line 1 | `module_monitoring_orchestrator.md` |
| `MatchingSettings.Nir2Path` | `string` | `""` | NIR files path for Line 2 | `module_monitoring_orchestrator.md` |
| `MatchingSettings.Normal1Path` | `string` | `""` | Normal image folders path for Line 1 | `module_monitoring_orchestrator.md` |
| `MatchingSettings.Normal2Path` | `string` | `""` | Normal image folders path for Line 2 | `module_monitoring_orchestrator.md` |
| `MatchingSettings.Camera1Path` | `string` | `""` | Camera 1 files path | `module_monitoring_orchestrator.md` |
| `MatchingSettings.Camera2Path` | `string` | `""` | Camera 2 files path | `module_monitoring_orchestrator.md` |
| `MatchingSettings.Camera3Path` | `string` | `""` | Camera 3 files path | `module_monitoring_orchestrator.md` |
| `MatchingSettings.Camera4Path` | `string` | `""` | Camera 4 files path | `module_monitoring_orchestrator.md` |
| `MatchingSettings.Camera5Path` | `string` | `""` | Camera 5 files path | `module_monitoring_orchestrator.md` |
| `MatchingSettings.Camera6Path` | `string` | `""` | Camera 6 files path | `module_monitoring_orchestrator.md` |

> **Naming Convention**: Use `PascalCase.PascalCase` for nested config properties.

---

## Enumerations

| Official Name | Values | Description | Defined In |
|---------------|--------|-------------|------------|
| `FileType` | `Unknown`, `Nir`, `Normal`, `Camera` | Type of file being processed | `module_monitoring_orchestrator.md` |
| `MatchingStrategy` | `DataSequenceBased`, `Sequential` | Strategy for file matching (time-based vs sequential) | `module_file_matching_engine.md` |

> **Naming Convention**: Use `PascalCase` for enum names and values.

---

## Events

| Official Name | Args Type | Description | Defined In |
|---------------|-----------|-------------|------------|
| `GroupCreated` | `FileGroup` | Raised when new group is created | `module_monitoring_orchestrator.md` |
| `GroupUpdated` | `FileGroup` | Raised when existing group is updated | `module_monitoring_orchestrator.md` |
| `GroupRemoved` | `string` (GroupId) | Raised when group is removed | `module_monitoring_orchestrator.md` |
| `FileGroupsCreated` | `FileGroupsCreatedEventArgs` | Batch event for multiple groups created | `module_monitoring_orchestrator.md` |
| `FileGroupsUpdated` | `FileGroupsUpdatedEventArgs` | Batch event for multiple groups updated | `module_monitoring_orchestrator.md` |
| `MonitoringError` | `string` (error message) | Raised when monitoring error occurs | `module_monitoring_orchestrator.md` |

> **Naming Convention**: Use `PascalCase`, past tense verbs (Created, Updated, Removed).

---

## Domain Concepts

| Term | Definition | Related Docs |
|------|------------|--------------|
| File Group | A collection of related files (NIR, normal images, camera images) with matching timestamps | `module_monitoring_orchestrator.md` |
| Real-time Monitoring | Continuous file system watching and immediate group creation/update | `module_monitoring_orchestrator.md` |
| Initial Scan | One-time scan of all files at monitoring start | `module_monitoring_orchestrator.md` |
| File Matching | Algorithm that groups files based on timestamps and types | `module_monitoring_orchestrator.md` |
| Line Number | Production line identifier (1 or 2) determining file source | `module_monitoring_orchestrator.md` |
| Orchestrator | Component that coordinates multiple services/workflows | `module_monitoring_orchestrator.md` |
| Polling | Periodic file system scan to detect changes missed by real-time watcher | `module_file_watcher_service.md` |
| Silent Baseline Scan | Initial scan of files at startup without raising events | `module_file_watcher_service.md` |
| Match 1 | Matching strategy by NormalFolder identifier (primary stable identifier) | `module_monitoring_orchestrator.md` |
| Match 2 | Matching strategy by NirKey identifier (secondary stable identifier) | `module_monitoring_orchestrator.md` |
| Match 3 | Matching strategy by timestamp with tolerance and priority order | `module_monitoring_orchestrator.md` |
| Non-Duplicate Filter | Filter that excludes groups already containing a specific data type | `module_monitoring_orchestrator.md` |
| Standalone Matching Module | Pure matching logic extracted from service layer, independently testable | `module_file_matching_engine.md` |
| DataSequence-Based Matching | Matching strategy using DataSequenceSettings for time-based decisions | `module_file_matching_engine.md` |
| Sequential Matching | Fallback matching strategy with no time constraints (when DataSequenceSettings is null) | `module_file_matching_engine.md` |
| Chip (UI) | Small, rounded rectangle displaying a label-value pair for statistics | `design_guidelines.md` |
| Toolbar Button (UI) | Icon + label button in transparent style with hover effect | `design_guidelines.md` |
| Panel (UI) | Container area with light gray background (`#f5f5f5`) | `design_guidelines.md` |
| Statistics Bar (UI) | Horizontal bar displaying multiple chips with file counts or matching status | `design_guidelines.md` |
| Abnormal (UI) | State indicating data mismatch or quality issue (highlighted in yellow) | `design_guidelines.md` |
| y_range | Overall Y intensity range in NIR wavelength window 4500-6500nm | NIR filtering algorithm |
| y_std | Standard deviation of Y intensities in NIR wavelength window | NIR filtering algorithm |
| window_800_mean | Mean of Y ranges across all 800nm sliding windows | NIR filtering algorithm |
| window_800_std | Standard deviation of Y ranges across 800nm windows | NIR filtering algorithm |
| window_800_max | Maximum Y range found in any 800nm window | NIR filtering algorithm |

---

## Deprecated Terms (Do Not Use)

| Deprecated | Use Instead | Reason | Deprecated Date |
|------------|-------------|--------|-----------------|
| `IsMatchingTimestamp` | Use `FileGroupMatcher.MatchFilesAsync` | Simple time check replaced by full matcher algorithm | 2024-12-14 |
| `UpdateGroupWithFile` | `MergeGroups` | More descriptive name for merging operation | 2024-12-14 |


## Changelog

- 2025-12-23: Added NIR filtering algorithm terms (upgrade_nir_filtering_algorithm spec)
  - Added `NirSpectrumFilter`, `CriteriaResult`, `FilterResult` classes
  - Added domain concepts: y_range, y_std, window_800_mean, window_800_std, window_800_max
  - Upgraded from simple Y-range check to 5-criteria scoring system
- 2024-12-16: Marked deprecated matching properties (Phase 5 - refactor-matching-logic)
  - Marked `NirTimeWindowSeconds`, `CameraTimeWindowSeconds`, `NormalFolderTimeWindowSeconds` as DEPRECATED
  - Added reference to `impact_deprecated_matching_properties.md`
  - All time-based matching now uses DataSequenceSettings
- 2024-12-16: Added FileMatchingEngine terms for refactor-matching-logic spec
  - Added `FileMatchingEngine` class
  - Added `MatchingStrategy` enum (DataSequenceBased, Sequential)
  - Added domain concepts: Standalone Matching Module, DataSequence-Based Matching, Sequential Matching
- 2025-12-15: Added matching strategy terms (Match 1, Match 2, Match 3, Non-Duplicate Filter)
- 2025-12-15: Added UI/UX design terms (Chip, Toolbar Button, Panel, Statistics Bar, Abnormal)
- 2025-12-15: Added FileWatcherService terms (Polling, Silent Baseline Scan)
- 2024-12-14: Initial creation with MonitoringOrchestrator terms
  - Added classes, methods, config keys
  - Added deprecated terms from refactoring
