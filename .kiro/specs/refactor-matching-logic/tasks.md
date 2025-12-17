---
Task: Refactor Matching Logic
Created: 2024-12-16
Status: In Progress
Depends On: plan.md
---

# Refactor Matching Logic - Task Checklist

## Progress
- Total: 13 | Completed: 7 | Remaining: 6

## Phase 1: Glossary and Documentation Setup

- [x] 1.1 Update `docs/architecture/glossary.md` with new terms
  - Add `FileMatchingEngine` (Class): Standalone matching engine with pure logic
  - Add `MatchingStrategy` (Enum): Strategy for matching (DataSequenceBased or Sequential)
  - _Requirements: 4.4_
  - _Completed: 2024-12-16_

- [x] 1.2 Verify no naming conflicts
  - Run: `grep -rn "FileMatchingEngine" docs/architecture/`
  - Run: `grep -rn "MatchingStrategy" docs/architecture/`
  - _Requirements: 4.4_
  - _Completed: 2024-12-16_
  - _Result: No conflicts found - terms added to glossary.md_

## Phase 2: Create Standalone Matching Engine

- [ ] 2.1 Create `ChronoView/Core/FileMatching/FileMatchingEngine.cs`
  - Create static class with MatchFiles method
  - Define MatchingStrategy enum (DataSequenceBased, Sequential)
  - Add DetermineStrategy method
  - Extract all matching logic from FileGroupMatcherService.BuildAllGroups()
  - Include timestamp parsing methods (ExtractTimestampFromFolderName, ExtractTimestampFromNirKey)
  - Include grouping methods (BuildLineGroups, FlattenCamFiles, FindMatchingCamFile, FindMatchingCamFileFromReference, DrainCamToGroups)
  - Include CamFileEntry helper class
  - Make it stateless and framework-agnostic (no WPF dependencies)
  - Accept UnmatchedFiles and DataSequenceSettings as parameters
  - Return List<FileGroup>
  - Add logging for strategy selection (log which strategy is used)
  - _Requirements: 1.1, 1.2, 1.3, 1.6, 5.1, 5.2_

- [ ]* 2.2 Write unit tests for FileMatchingEngine
  - Test DataSequenceBased strategy with valid DataSequenceSettings
  - Test Sequential strategy (DataSequenceSettings = null)
  - Test timestamp parsing for various formats (C251204T111028, C20240115_143022, NIR formats)
  - Test NIR attachment logic with time windows
  - Test camera matching logic (Cam1 to normal, Cam2/3 to Cam1 reference)
  - Test multi-line mode (Line 1 and Line 2 processed separately)
  - Test with null/empty inputs
  - Test edge cases (no matches, multiple matches, boundary conditions)
  - _Requirements: 5.3, 5.4_

## Phase 3: Refactor FileGroupMatcherService to Thin Wrapper

- [ ] 3.1 Refactor `ChronoView/Core/FileMatching/FileGroupMatcherService.cs`
  - Remove BuildAllGroups, BuildLineGroups, and all helper methods (moved to FileMatchingEngine)
  - Modify MatchFilesAsync to delegate to FileMatchingEngine.MatchFiles()
  - Pass Configuration.DataSequenceSettings to FileMatchingEngine
  - Keep state management (_groupCounter, _consumedNirKeys)
  - Update _groupCounter after matching
  - Update _consumedNirKeys for groups with NIR files
  - Maintain IFileGroupMatcher interface (no breaking changes)
  - Keep Configuration property for backward compatibility
  - Keep ResetState(), ResetConsumedNirKeys(), AddConsumedNirKey() methods
  - _Requirements: 1.7, 1.8_

- [ ]* 3.2 Update existing FileGroupMatcherService tests
  - Verify delegation to FileMatchingEngine works correctly
  - Verify state management (_groupCounter, _consumedNirKeys) still works
  - Verify backward compatibility with existing tests
  - Verify IFileGroupMatcher interface contract maintained
  - _Requirements: 1.8_

- [ ] 3.3 Run diagnostics to verify no compilation errors
  - Run: `dotnet build ChronoView/ChronoView.csproj`
  - Fix any compilation errors
  - _Requirements: 1.8_

## Phase 4: Migrate MonitoringOrchestrator to DataSequenceSettings

- [x] 4.1 Update `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs`
  - Add GetMatchingTolerance(FileType fileType) method
  - Add MapFileTypeToDataType(FileType fileType) method
  - Update IsMatchingTimestamp method (line ~1284) to use GetMatchingTolerance
  - GetMatchingTolerance should check if DataSequenceSettings exists
  - If DataSequenceSettings exists: use GetMaxDelay(dataType)
  - If DataSequenceSettings is null: fallback to NirTimeWindowSeconds/CameraTimeWindowSeconds
  - MapFileTypeToDataType should map FileType enum to DataType enum
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_
  - _Completed: 2024-12-16_

- [ ]* 4.2 Write unit tests for MonitoringOrchestrator tolerance methods
  - Test GetMatchingTolerance with valid DataSequenceSettings
  - Test GetMatchingTolerance fallback to deprecated properties (DataSequenceSettings = null)
  - Test MapFileTypeToDataType for all FileType values (Nir, Nir2, Cam1-6, Normal)
  - Verify correct DataType mapping
  - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [x] 4.3 Run diagnostics to verify no compilation errors
  - Run: `dotnet build ChronoView/ChronoView.csproj`
  - Fix any compilation errors
  - _Completed: 2024-12-16_
  - _Result: Build successful with only pre-existing warnings_

## Phase 5: Mark Deprecated Properties and Clean Up UI

- [x] 5.1 Update `ChronoView/Models/ApplicationConfiguration.cs` (MatchingSettings class)
  - Add [Obsolete("Use DataSequenceSettings.GetMaxDelay(DataType.NIR) instead")] to NirTimeWindowSeconds
  - Add [Obsolete("Use DataSequenceSettings.GetMaxDelay(DataType.Cam1) instead")] to CameraTimeWindowSeconds
  - Add [Obsolete("Use DataSequenceSettings.GetMaxDelay(DataType.Normal) instead")] to NormalFolderTimeWindowSeconds
  - Keep properties functional for backward compatibility (don't remove)
  - _Requirements: 2.2_
  - _Completed: 2024-12-16_

- [x] 5.2 Update `ChronoView/UI/Views/SettingsDialog.xaml`
  - Remove "Matching Options (DEPRECATED)" section from Advanced tab (line ~148)
  - Remove associated TextBlock explaining deprecation
  - Verify Data Sequence tab is still present and functional
  - _Requirements: 2.4_
  - _Completed: 2024-12-16_

- [x] 5.3 Update `ChronoView/Core/Configuration/ConfigurationManager.cs`
  - Remove validation for NirTimeWindowSeconds (if exists)
  - Remove validation for CameraTimeWindowSeconds (if exists)
  - Add validation for DataSequenceSettings (if present)
  - Call DataSequenceSettings.Validate() method
  - Throw ConfigurationValidationException if validation fails
  - _Requirements: 2.5_
  - _Completed: 2024-12-16_

- [ ]* 5.4 Update configuration tests
  - Remove tests for deprecated property validation (if they exist)
  - Add tests for DataSequenceSettings validation
  - Verify backward compatibility (deprecated properties still load/save correctly)
  - Test that Obsolete warnings appear when using deprecated properties
  - _Requirements: 2.5_

- [x] 5.5 Run diagnostics to verify no compilation errors
  - Run: `dotnet build ChronoView/ChronoView.csproj`
  - Check for Obsolete warnings (expected for deprecated properties)
  - Verify no other compilation errors
  - _Completed: 2024-12-16_
  - _Result: Build successful. CS0618 warnings for deprecated properties are expected and correct._

## Phase 6: Integration Testing

- [ ]* 6.1 Run full test suite
  - Run: `dotnet test ChronoView.Tests/ChronoView.Tests.csproj`
  - Verify all existing tests still pass
  - Fix any broken tests due to refactoring
  - _Requirements: 1.8_

- [ ]* 6.2 Write integration test for full matching flow
  - Test: UnmatchedFiles → FileMatchingEngine.MatchFiles() → List<FileGroup>
  - Test with valid DataSequenceSettings (DataSequenceBased strategy)
  - Test without DataSequenceSettings (Sequential fallback strategy)
  - Test FileGroupMatcherService delegation to FileMatchingEngine
  - Verify state management works correctly
  - _Requirements: 5.3, 5.4_

## Phase 7: Documentation

- [ ] 7.1 Create `docs/architecture/module_file_matching_engine.md`
  - Document FileMatchingEngine as standalone module
  - Document MatchingStrategy enum (DataSequenceBased, Sequential)
  - Document input/output contracts (UnmatchedFiles → List<FileGroup>)
  - Document matching algorithm (BuildLineGroups, camera matching, NIR attachment)
  - Document how DataSequenceSettings controls matching (GetMinDelay, GetMaxDelay)
  - Document fallback behavior when DataSequenceSettings is null (Sequential strategy)
  - Document that it's stateless and framework-agnostic
  - Include verification commands (grep for usages)
  - Use glossary terms exactly
  - _Requirements: 4.1, 4.2, 4.3_

- [ ] 7.2 Update `docs/architecture/module_file_group_matcher.md`
  - Document FileGroupMatcherService as thin wrapper
  - Document delegation to FileMatchingEngine
  - Document state management (_groupCounter, _consumedNirKeys)
  - Document IFileGroupMatcher interface implementation
  - Document Configuration property and backward compatibility
  - Include verification commands
  - Update dependencies to include FileMatchingEngine
  - _Requirements: 4.1, 4.2, 4.3_

- [ ] 7.3 Update `docs/architecture/module_monitoring_orchestrator.md`
  - Update IsMatchingTimestamp section to reflect DataSequenceSettings usage
  - Document GetMatchingTolerance method
  - Document MapFileTypeToDataType method
  - Document fallback behavior to deprecated properties
  - Update dependencies section (now uses DataSequenceSettings)
  - Update verification commands if needed
  - _Requirements: 4.1, 4.2_

- [ ] 7.4 Update `docs/architecture/README.md`
  - Add module_file_matching_engine.md to module index
  - Update module_file_group_matcher.md entry (now a wrapper)
  - Update high-risk areas if FileMatchingEngine affects many components
  - Ensure all new docs are listed
  - _Requirements: 4.5_

- [ ] 7.5 Verify documentation consistency
  - Run: `grep -rn "FileMatchingEngine" docs/architecture/`
  - Run: `grep -rn "MatchingStrategy" docs/architecture/`
  - Verify all terms match glossary.md definitions
  - Verify no conflicting definitions across documents
  - _Requirements: 4.4_

## Phase 8: Final Verification

- [ ] 8.1 Checkpoint - Ensure all tests pass
  - Run: `dotnet test ChronoView.Tests/ChronoView.Tests.csproj`
  - Verify all tests pass (including new and existing tests)
  - Ask user if questions arise

- [ ] 8.2 Verify no unexpected compilation warnings
  - Run: `dotnet build ChronoView/ChronoView.csproj`
  - Check for Obsolete warnings (expected for deprecated properties - this is OK)
  - Check for other warnings (should be none)
  - Document any unexpected warnings

- [ ] 8.3 Manual verification checklist
  - Launch application and verify it starts without errors
  - Open Settings dialog → Advanced tab
  - Verify "Matching Options (DEPRECATED)" section is removed
  - Open Settings dialog → Data Sequence tab
  - Verify Data Sequence tab is functional and displays correctly
  - Perform a file scan and verify matching still works correctly
  - Verify groups are created with correct timestamps and file associations
  - Check logs for strategy selection messages (DataSequenceBased vs Sequential)

---
**Completion Date**: [To be filled]
