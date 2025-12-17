---
Task: Refactor Matching Logic
Created: 2024-12-16
Status: Draft
Depends On: requirements.md, research.md
---

# Refactor Matching Logic - Implementation Plan

## 1. Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│  MonitoringOrchestrator                                     │
│  - Uses IFileGroupMatcher interface                         │
│  - Currently uses deprecated MatchingSettings properties    │
└────────────────┬────────────────────────────────────────────┘
                 │
                 │ IFileGroupMatcher
                 ▼
┌─────────────────────────────────────────────────────────────┐
│  FileGroupMatcherService (Thin Wrapper)                     │
│  - Implements IFileGroupMatcher                             │
│  - Maintains backward compatibility                         │
│  - Delegates to FileMatchingEngine                          │
└────────────────┬────────────────────────────────────────────┘
                 │
                 │ Delegates
                 ▼
┌─────────────────────────────────────────────────────────────┐
│  FileMatchingEngine (NEW - Standalone Module)               │
│  - Pure matching logic                                      │
│  - Static methods                                           │
│  - No state, no WPF dependencies                            │
│  - Input: UnmatchedFiles + DataSequenceSettings             │
│  - Output: List<FileGroup>                                  │
└─────────────────────────────────────────────────────────────┘
```

## 2. Components

### New Components

| Component | Type | Location | Purpose |
|-----------|------|----------|---------|
| `FileMatchingEngine` | Static Class | `ChronoView/Core/FileMatching/FileMatchingEngine.cs` | Pure matching logic, framework-agnostic |
| `MatchingStrategy` | Enum | `ChronoView/Core/FileMatching/FileMatchingEngine.cs` | Enum for matching strategy (DataSequenceBased, Sequential) |

### Modified Components

| Component | Location | Changes |
|-----------|----------|---------|
| `FileGroupMatcherService` | `ChronoView/Core/FileMatching/FileGroupMatcherService.cs` | Refactor to thin wrapper that delegates to FileMatchingEngine |
| `MatchingSettings` | `ChronoView/Models/ApplicationConfiguration.cs` | Mark deprecated properties with [Obsolete] attribute |
| `SettingsDialog.xaml` | `ChronoView/UI/Views/SettingsDialog.xaml` | Remove "Matching Options (DEPRECATED)" section from Advanced tab |
| `ConfigurationManager` | `ChronoView/Core/Configuration/ConfigurationManager.cs` | Remove validation for deprecated properties |
| `MonitoringOrchestrator` | `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs` | Migrate to use DataSequenceSettings instead of deprecated properties |

## 3. Detailed Design

### 3.1 FileMatchingEngine (New Standalone Module)

**Interface**:
```csharp
namespace ChronoView.Core.FileMatching
{
    /// <summary>
    /// Standalone file matching engine with pure matching logic.
    /// Framework-agnostic, stateless, and independently testable.
    /// </summary>
    public static class FileMatchingEngine
    {
        /// <summary>
        /// Match files from unmatched collection into file groups.
        /// </summary>
        /// <param name="unmatchedFiles">Collection of unmatched files</param>
        /// <param name="sequenceSettings">Data sequence configuration (null = sequential matching)</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        /// <returns>List of matched file groups</returns>
        public static List<FileGroup> MatchFiles(
            UnmatchedFiles unmatchedFiles,
            DataSequenceSettings? sequenceSettings,
            ILogger? logger = null);

        /// <summary>
        /// Determine which matching strategy will be used.
        /// </summary>
        public static MatchingStrategy DetermineStrategy(DataSequenceSettings? sequenceSettings);
    }

    /// <summary>
    /// Matching strategy enumeration
    /// </summary>
    public enum MatchingStrategy
    {
        /// <summary>
        /// Use DataSequenceSettings for time-based matching
        /// </summary>
        DataSequenceBased,

        /// <summary>
        /// Sequential matching (no time constraints)
        /// </summary>
        Sequential
    }
}
```

**Logic**: 
1. Extract all matching logic from FileGroupMatcherService.BuildAllGroups() and related methods
2. Make it static and stateless
3. Remove dependency on MatchingConfiguration (only need DataSequenceSettings)
4. Keep all timestamp parsing, grouping, and NIR attachment logic
5. Log which strategy is being used

**Dependencies**: 
- UnmatchedFiles (input)
- FileGroup (output)
- DataSequenceSettings (configuration)
- ILogger (optional)

### 3.2 FileGroupMatcherService (Modified to Thin Wrapper)

**Interface**: Keep IFileGroupMatcher unchanged (backward compatibility)

**Logic**:
```csharp
public async Task<IEnumerable<FileGroup>> MatchFilesAsync(UnmatchedFiles unmatchedFiles)
{
    return await Task.Run(() =>
    {
        // Delegate to standalone engine
        var groups = FileMatchingEngine.MatchFiles(
            unmatchedFiles,
            Configuration.DataSequenceSettings,
            _logger);

        // Update internal state (group counter, consumed NIR keys)
        _groupCounter = groups.Count;
        foreach (var group in groups.Where(g => g.HasNir && !string.IsNullOrEmpty(g.NirKey)))
        {
            _consumedNirKeys.Add(group.NirKey);
        }

        return groups;
    });
}
```

**Dependencies**: 
- FileMatchingEngine (new)
- ILogger
- MatchingConfiguration (for backward compatibility)

### 3.3 MonitoringOrchestrator Migration

**Current code** (line 1284-1285):
```csharp
var tolerance = fileType == FileType.Nir
    ? TimeSpan.FromSeconds(_currentConfig?.MatchingSettings.NirTimeWindowSeconds ?? 300)
    : TimeSpan.FromSeconds(_currentConfig?.MatchingSettings.CameraTimeWindowSeconds ?? 60);
```

**New code**:
```csharp
var tolerance = GetMatchingTolerance(fileType);

private TimeSpan GetMatchingTolerance(FileType fileType)
{
    var sequenceSettings = _currentConfig?.MatchingSettings.DataSequenceSettings;
    
    if (sequenceSettings == null)
    {
        // Fallback to deprecated properties for backward compatibility
        return fileType == FileType.Nir
            ? TimeSpan.FromSeconds(_currentConfig?.MatchingSettings.NirTimeWindowSeconds ?? 300)
            : TimeSpan.FromSeconds(_currentConfig?.MatchingSettings.CameraTimeWindowSeconds ?? 60);
    }

    // Use DataSequenceSettings
    var dataType = MapFileTypeToDataType(fileType);
    var maxDelay = sequenceSettings.GetMaxDelay(dataType);
    return TimeSpan.FromSeconds(maxDelay);
}

private DataType MapFileTypeToDataType(FileType fileType)
{
    return fileType switch
    {
        FileType.Nir => DataType.NIR,
        FileType.Nir2 => DataType.NIR,
        FileType.Cam1 => DataType.Cam1,
        FileType.Cam2 => DataType.Cam2,
        FileType.Cam3 => DataType.Cam3,
        FileType.Cam4 => DataType.Cam4,
        FileType.Cam5 => DataType.Cam5,
        FileType.Cam6 => DataType.Cam6,
        _ => DataType.Normal
    };
}
```

**Dependencies**: DataSequenceSettings

### 3.4 MatchingSettings Deprecation

**Changes**:
```csharp
/// <summary>
/// Time window for NIR file matching in seconds.
/// DEPRECATED: Use DataSequenceSettings instead.
/// </summary>
[Obsolete("Use DataSequenceSettings.GetMaxDelay(DataType.NIR) instead")]
public int NirTimeWindowSeconds { get; set; } = 300;

/// <summary>
/// Time window for camera file matching in seconds.
/// DEPRECATED: Use DataSequenceSettings instead.
/// </summary>
[Obsolete("Use DataSequenceSettings.GetMaxDelay(DataType.Cam1) instead")]
public int CameraTimeWindowSeconds { get; set; } = 2;

/// <summary>
/// Time window for normal folder matching in seconds.
/// DEPRECATED: Use DataSequenceSettings instead.
/// </summary>
[Obsolete("Use DataSequenceSettings.GetMaxDelay(DataType.Normal) instead")]
public int NormalFolderTimeWindowSeconds { get; set; } = 120;
```

### 3.5 SettingsDialog.xaml Cleanup

**Remove this section** from Advanced tab:
```xml
<TextBlock Text="Matching Options (DEPRECATED)" Style="{StaticResource SectionHeaderStyle}"/>
<TextBlock Text="Time-based matching is now configured in the Sequence tab." 
           TextWrapping="Wrap" 
           Foreground="Gray" 
           Margin="0,5,0,10"/>
```

### 3.6 ConfigurationManager Validation Cleanup

**Remove these validations**:
```csharp
if (config.MatchingSettings.NirTimeWindowSeconds < 0)
    throw new ConfigurationValidationException("NirTimeWindowSeconds cannot be negative");
if (config.MatchingSettings.CameraTimeWindowSeconds < 0)
    throw new ConfigurationValidationException("CameraTimeWindowSeconds cannot be negative");
```

**Add new validation**:
```csharp
// Validate DataSequenceSettings if present
if (config.MatchingSettings.DataSequenceSettings != null)
{
    if (!config.MatchingSettings.DataSequenceSettings.Validate(out var errors))
    {
        throw new ConfigurationValidationException(
            $"Invalid DataSequenceSettings: {string.Join(", ", errors)}");
    }
}
```

## 4. Naming Conventions

### New Terms to Add to Glossary

| Term | Type | Description |
|------|------|-------------|
| `FileMatchingEngine` | Class | Standalone matching engine with pure logic |
| `MatchingStrategy` | Enum | Strategy for matching (DataSequenceBased or Sequential) |

### Existing Glossary Terms to Use

- `FileGroupMatcherService` - Service wrapper for matching
- `DataSequenceSettings` - Configuration for data arrival sequence
- `UnmatchedFiles` - Input to matching process
- `FileGroup` - Output of matching process

## 5. Configuration Changes

No new configuration keys. Existing DataSequenceSettings is used.

## 6. Error Handling Strategy

| Error Scenario | Handling |
|----------------|----------|
| UnmatchedFiles is null | Throw ArgumentNullException |
| DataSequenceSettings is null | Use Sequential matching strategy, log warning |
| Invalid DataSequenceSettings | Validate before passing to engine, throw ConfigurationValidationException |
| Timestamp parsing fails | Log warning, skip file or use File.GetLastWriteTime as fallback |

## 7. Testing Strategy

### Unit Tests

- **FileMatchingEngine Tests**:
  - Test with DataSequenceSettings (DataSequenceBased strategy)
  - Test without DataSequenceSettings (Sequential strategy)
  - Test timestamp parsing for various formats
  - Test NIR attachment logic
  - Test camera matching logic
  - Test multi-line mode (Line 1 and Line 2)

- **FileGroupMatcherService Tests**:
  - Test delegation to FileMatchingEngine
  - Test state management (group counter, consumed NIR keys)
  - Test backward compatibility with MatchingConfiguration

- **MonitoringOrchestrator Tests**:
  - Test GetMatchingTolerance with DataSequenceSettings
  - Test GetMatchingTolerance fallback to deprecated properties
  - Test MapFileTypeToDataType

### Integration Tests

- Test full matching flow: UnmatchedFiles → FileMatchingEngine → FileGroup list
- Test configuration migration from deprecated properties to DataSequenceSettings
- Test that existing tests still pass (backward compatibility)

## 8. Architecture Documentation Plan

### New Architecture Docs to Create

| Document | Purpose |
|----------|---------|
| `docs/architecture/module_file_matching_engine.md` | Document the standalone FileMatchingEngine module |

### Existing Docs to Update

| Document | Changes Required |
|----------|------------------|
| `docs/architecture/README.md` | Add FileMatchingEngine to module index |
| `docs/architecture/glossary.md` | Add FileMatchingEngine and MatchingStrategy terms |
| `docs/architecture/module_file_group_matcher.md` | Create new doc for FileGroupMatcherService (wrapper) |
| `docs/architecture/module_monitoring_orchestrator.md` | Update to reflect DataSequenceSettings usage |

---
**Status**: [ ] Approved
