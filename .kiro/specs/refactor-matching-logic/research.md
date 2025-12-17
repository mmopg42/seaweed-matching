---
Task: Refactor Matching Logic
Created: 2024-12-16
Status: Draft
Depends On: requirements.md
---

# Refactor Matching Logic - Research Findings

## 1. Investigation Summary

Investigated the current matching system architecture, dependencies, and usage patterns to understand:
1. How FileGroupMatcherService is currently used
2. What depends on the deprecated MatchingSettings properties
3. The relationship between MatchingConfiguration and DataSequenceSettings
4. Impact zones for extracting matching logic into a standalone module

## 2. Question Answers

### Q1: Where is FileGroupMatcherService currently used?

**Method**: Code analysis using grep search
**Findings**:
- **Dependency Injection**: Registered in App.xaml.cs and test setup files
- **Direct Usage**: MonitoringOrchestrator uses IFileGroupMatcher interface
- **Test Usage**: Multiple test files instantiate FileGroupMatcherService directly

**Conclusion**: FileGroupMatcherService is used through DI in production code, but tests create instances directly. The IFileGroupMatcher interface is the primary contract.

**Evidence**:
```bash
# Production usage
grep -rn "IFileGroupMatcher" ChronoView/Core/
# Result: MonitoringOrchestrator.cs line 20, 39

# DI registration
grep -rn "AddSingleton<IFileGroupMatcher>" ChronoView/
# Result: App.xaml.cs line 184-186
```

### Q2: Are the deprecated MatchingSettings properties (NirTimeWindowSeconds, etc.) actually used?

**Method**: Code analysis using grep search
**Findings**:
- **Defined in**: ApplicationConfiguration.cs (MatchingSettings class)
- **Validated in**: ConfigurationManager.cs (validation logic)
- **Used in**: MonitoringOrchestrator.cs line 1284-1285 (for real-time matching tolerance)
- **Tested in**: Multiple test files verify persistence and validation

**Conclusion**: These properties ARE still being used in MonitoringOrchestrator for real-time file matching, despite being marked as deprecated. They need to be migrated to DataSequenceSettings before removal.

**Evidence**:
```csharp
// MonitoringOrchestrator.cs line 1284-1285
var tolerance = fileType == FileType.Nir
    ? TimeSpan.FromSeconds(_currentConfig?.MatchingSettings.NirTimeWindowSeconds ?? 300)
    : TimeSpan.FromSeconds(_currentConfig?.MatchingSettings.CameraTimeWindowSeconds ?? 60);
```

### Q3: How does MatchingConfiguration relate to DataSequenceSettings?

**Method**: Code analysis of IFileGroupMatcher.cs and FileGroupMatcherService.cs
**Findings**:
- **MatchingConfiguration** contains:
  - DataSequenceSettings (optional)
  - Path configurations (Nir1Path, Normal1Path, Camera1-6Path, etc.)
  - Folder suffix options (UseFolderSuffix, UseCameraSubfolderNormal, etc.)
- **FileGroupMatcherService** uses DataSequenceSettings for time-based matching
- **Fallback behavior**: If DataSequenceSettings is null, uses sequential matching

**Conclusion**: MatchingConfiguration is a wrapper that includes DataSequenceSettings plus path/folder configuration. The matching logic already prefers DataSequenceSettings over deprecated properties.

**Evidence**:
```csharp
// FileGroupMatcherService.cs line 261-268
// Get camera matching time window from DataSequenceSettings
double camMinDiff = Configuration.DataSequenceSettings?.GetMinDelay(DataType.Cam1) ?? 0.0;
double camMaxDiff = Configuration.DataSequenceSettings?.GetMaxDelay(DataType.Cam1) ?? 50.0;

// If no DataSequenceSettings, use sequential matching
if (Configuration.DataSequenceSettings == null)
{
    var item = queue[0];
    queue.RemoveAt(0);
    return item;
}
```

### Q4: What are the dependencies of FileGroupMatcherService?

**Method**: Code analysis of FileGroupMatcherService.cs
**Findings**:
- **Direct dependencies**:
  - ILogger<FileGroupMatcherService> (optional, for logging)
  - System.IO (for File.GetLastWriteTime)
  - System.Text.RegularExpressions (for timestamp parsing)
- **Data dependencies**:
  - UnmatchedFiles (input)
  - FileGroup (output)
  - DataSequenceSettings (configuration)
  - MatchingConfiguration (configuration wrapper)
- **No WPF dependencies**: Already framework-agnostic

**Conclusion**: FileGroupMatcherService is already mostly independent. The main coupling is through MatchingConfiguration which includes path information not needed for pure matching logic.

**Evidence**: No System.Windows.* imports found in FileGroupMatcherService.cs

### Q5: What is the impact of extracting matching logic into a standalone module?

**Method**: Dependency analysis
**Findings**:
- **Affected files**:
  - FileGroupMatcherService.cs (will become thin wrapper)
  - MonitoringOrchestrator.cs (uses IFileGroupMatcher)
  - All test files (may need updates)
- **Breaking changes**: None if we maintain IFileGroupMatcher interface
- **Benefits**:
  - Easier to test matching logic in isolation
  - Clearer separation of concerns
  - Reusable in other contexts

**Conclusion**: Extraction can be done with minimal impact by keeping IFileGroupMatcher interface unchanged and making FileGroupMatcherService delegate to the new standalone module.

**Evidence**:
```bash
# Only one production file uses IFileGroupMatcher
grep -rn "IFileGroupMatcher" ChronoView/Core/ --exclude-dir=FileMatching
# Result: MonitoringOrchestrator.cs only
```

## 3. Code Analysis Results

### Relevant Existing Code

| File | Function/Class | Relevance |
|------|----------------|-----------|
| `ChronoView/Core/FileMatching/FileGroupMatcherService.cs` | `FileGroupMatcherService` | Contains all matching logic to be extracted |
| `ChronoView/Core/FileMatching/IFileGroupMatcher.cs` | `IFileGroupMatcher` | Interface to maintain for backward compatibility |
| `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs` | `MonitoringOrchestrator` | Uses IFileGroupMatcher, also uses deprecated properties |
| `ChronoView/Models/ApplicationConfiguration.cs` | `MatchingSettings` | Contains deprecated properties to remove |
| `ChronoView/Core/Configuration/ConfigurationManager.cs` | `ValidateConfiguration` | Validates deprecated properties |

### Dependencies Identified

<!-- VERIFY: grep -rn "FileGroupMatcherService" ChronoView/ -->
<!-- VERIFY: grep -rn "IFileGroupMatcher" ChronoView/Core/ -->
<!-- VERIFY: grep -rn "NirTimeWindowSeconds\|CameraTimeWindowSeconds" ChronoView/ -->

**FileGroupMatcherService dependencies**:
- Used by: MonitoringOrchestrator (via IFileGroupMatcher interface)
- Registered in: App.xaml.cs (DI container)
- Tested in: Multiple test files

**Deprecated properties usage**:
- MonitoringOrchestrator.cs: Uses NirTimeWindowSeconds and CameraTimeWindowSeconds for real-time matching
- ConfigurationManager.cs: Validates these properties
- Multiple test files: Test persistence and validation

### Glossary Check

Existing related terms in glossary:
- FileGroupMatcherService
- DataSequenceSettings
- MatchingSettings
- UnmatchedFiles
- FileGroup

New terms to add:
- FileMatchingEngine (proposed standalone module name)
- MatchingStrategy (enum: DataSequenceBased, Sequential)

## 4. Recommendations

### Recommendation 1: Two-Phase Approach

**Phase 1**: Extract matching logic into standalone module
- Create FileMatchingEngine.cs with pure matching logic
- Make FileGroupMatcherService a thin wrapper
- Keep IFileGroupMatcher interface unchanged
- No breaking changes

**Phase 2**: Migrate MonitoringOrchestrator to use DataSequenceSettings
- Update MonitoringOrchestrator to use DataSequenceSettings instead of deprecated properties
- Remove deprecated properties from MatchingSettings
- Update SettingsDialog.xaml to remove deprecated UI elements

**Rationale**: Separating into two phases reduces risk and allows testing each change independently.

### Recommendation 2: Standalone Module Design

Create `FileMatchingEngine.cs` with this structure:
```csharp
public class FileMatchingEngine
{
    public static List<FileGroup> MatchFiles(
        UnmatchedFiles unmatchedFiles,
        DataSequenceSettings? sequenceSettings,
        ILogger? logger = null)
    {
        // Pure matching logic here
        // No state, no dependencies on configuration paths
    }
}
```

**Benefits**:
- Static method = no state = easier to test
- Minimal dependencies (only logging)
- Can be called from anywhere
- FileGroupMatcherService becomes a simple adapter

### Recommendation 3: Keep Path Configuration Separate

**Current issue**: MatchingConfiguration mixes matching logic config (DataSequenceSettings) with path config (Nir1Path, Camera1Path, etc.)

**Recommendation**: 
- FileMatchingEngine should NOT need path information
- Paths are only needed by MonitoringOrchestrator for file watching
- Keep MatchingConfiguration for FileGroupMatcherService (backward compatibility)
- FileMatchingEngine only needs DataSequenceSettings

### Recommendation 4: Migration Path for Deprecated Properties

**Step 1**: Add migration logic in ConfigurationManager
```csharp
// If DataSequenceSettings is null but deprecated properties exist, auto-migrate
if (config.MatchingSettings.DataSequenceSettings == null)
{
    config.MatchingSettings.DataSequenceSettings = CreateFromLegacySettings(config.MatchingSettings);
}
```

**Step 2**: Update MonitoringOrchestrator to use DataSequenceSettings

**Step 3**: Mark deprecated properties as [Obsolete]

**Step 4**: Remove deprecated properties in next major version

## 5. Unanswered Questions

None - all questions have been answered through code analysis.

---
**Status**: [ ] Approved
