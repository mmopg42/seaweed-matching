---
Task: Refactor Matching Logic
Created: 2024-12-16
Status: Draft
Summary: Separate matching logic from settings UI, remove deprecated matching options, and clarify matching strategy based on DataSequenceSettings
---

# Refactor Matching Logic - Requirements

## Introduction

The current file matching system has several issues:
1. Matching logic is embedded in FileGroupMatcherService and tightly coupled with the application
2. Deprecated matching options still exist in the Advanced tab
3. It's unclear whether matching uses DataSequenceSettings or legacy time windows
4. Matching logic should be extracted into a standalone, testable module/script that can be maintained independently
5. The matching algorithm should be reusable and not depend on WPF or UI concerns

## Glossary

- **FileGroupMatcherService**: Service responsible for matching files into groups based on timestamp correlation
- **DataSequenceSettings**: Configuration defining expected data arrival order and time tolerances
- **MatchingSettings**: Legacy configuration containing deprecated time window settings
- **UnmatchedFiles**: Collection of files that haven't been grouped yet
- **FileGroup**: A matched set of files (Normal folder, NIR, cameras) representing one data capture event

## Requirements

### Requirement 1

**User Story:** As a developer, I want the matching logic to be extracted into a standalone script/module, so that I can test, maintain, and reuse the matching algorithm independently of the WPF application.

#### Acceptance Criteria

1. THE system SHALL create a new standalone matching module in ChronoView/Core/FileMatching/ that contains all matching logic
2. THE matching module SHALL be framework-agnostic (no WPF dependencies, no UI concerns)
3. THE matching module SHALL accept UnmatchedFiles and DataSequenceSettings as inputs
4. THE matching module SHALL return List<FileGroup> as output
5. THE matching module SHALL use DataSequenceSettings as the single source of truth for time-based matching
6. THE matching module SHALL be independently testable without requiring the full application context
7. THE FileGroupMatcherService SHALL become a thin wrapper that calls the standalone matching module
8. THE system SHALL maintain backward compatibility with existing FileGroup data structures and IFileGroupMatcher interface

### Requirement 2

**User Story:** As a user, I want the Settings dialog to only show relevant configuration options, so that I'm not confused by deprecated or unused settings.

#### Acceptance Criteria

1. THE system SHALL remove deprecated matching options from the Advanced tab in SettingsDialog
2. THE system SHALL remove the following deprecated properties from MatchingSettings:
   - NirTimeWindowSeconds
   - CameraTimeWindowSeconds
   - NormalFolderTimeWindowSeconds
3. THE system SHALL keep only the following in MatchingSettings:
   - EnableAbnormalDetection
   - ZScoreThreshold
   - SupportMultipleLines
   - LineMode
   - EnableNirGraph
   - MoveNir
   - MoveAllData
   - UseCameraSubfolderNormal
   - UseCameraSubfolderNormal2
   - UseFolderSuffix
   - All path properties (Nir1Path, Normal1Path, Camera1-6Path, Nir2Path, Normal2Path, OutputPath)
4. THE system SHALL update SettingsDialog.xaml to remove the deprecated "Matching Options" section
5. THE system SHALL ensure all time-based matching configuration is done through the Data Sequence tab

### Requirement 3

**User Story:** As a developer, I want to verify that matching uses DataSequenceSettings correctly, so that I can trust the matching behavior matches the configured sequence.

#### Acceptance Criteria

1. WHEN FileGroupMatcherService performs matching THEN the system SHALL use DataSequenceSettings.GetMinDelay() and GetMaxDelay() for all time-based decisions
2. WHEN DataSequenceSettings is null or empty THEN the system SHALL fall back to sequential matching (no time constraints)
3. WHEN matching NIR files THEN the system SHALL use DataSequenceSettings.GetMaxDelay(DataType.NIR) as the time window
4. WHEN matching Camera files THEN the system SHALL use DataSequenceSettings.GetMinDelay() and GetMaxDelay() for the respective camera type
5. THE system SHALL log which matching strategy is being used (DataSequenceSettings-based or sequential fallback)

### Requirement 4

**User Story:** As a developer, I want clear architecture documentation for the matching module, so that future modifications don't break the matching logic.

#### Acceptance Criteria

1. THE system SHALL create docs/architecture/module_file_group_matcher.md documenting the matching algorithm
2. THE documentation SHALL include:
   - Overview of matching strategy (standalone module design)
   - How DataSequenceSettings controls matching
   - Fallback behavior when DataSequenceSettings is not configured
   - Input/output contracts (UnmatchedFiles → List<FileGroup>)
   - Dependencies on other modules (should be minimal)
   - Impact zones for future modifications
   - How to test the matching module independently
3. THE documentation SHALL include verification commands to find all usages of the matching module
4. THE system SHALL update docs/architecture/glossary.md with any new terms introduced
5. THE system SHALL update docs/architecture/README.md to reference the new matching module documentation

### Requirement 5

**User Story:** As a developer, I want the standalone matching module to be easily testable, so that I can verify matching behavior without running the full application.

#### Acceptance Criteria

1. THE matching module SHALL have no dependencies on:
   - WPF (System.Windows.*)
   - UI ViewModels
   - Application-level services (except logging)
2. THE matching module SHALL accept all configuration through method parameters (no global state)
3. THE matching module SHALL be deterministic (same inputs → same outputs)
4. THE system SHALL provide unit tests that demonstrate the matching module can be tested independently
5. THE matching module SHALL use dependency injection for ILogger (optional parameter)

---
**Status**: [ ] Approved
