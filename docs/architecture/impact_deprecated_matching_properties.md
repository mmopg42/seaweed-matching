---
Owner: Development Team
Last Updated: 2024-12-16
Related PRs: []
Code Ref: 2024-12-16 (Phase 5 - Deprecated Properties Cleanup)
---

# Impact Analysis: Deprecated Matching Properties

## Overview

This document tracks the deprecation of legacy time-window matching properties in favor of the unified `DataSequenceSettings` configuration system. As of 2024-12-16 (Phase 5 of refactor-matching-logic spec), three properties in `MatchingSettings` have been marked as `[Obsolete]` and removed from the UI.

## Deprecated Properties

### 1. NirTimeWindowSeconds
- **Type**: `int`
- **Default**: `300` (5 minutes)
- **Obsolete Message**: `"Use DataSequenceSettings.GetMaxDelay(DataType.NIR) instead"`
- **Status**: Marked `[Obsolete]` in code, functional for backward compatibility
- **UI Status**: Removed from Settings dialog

### 2. CameraTimeWindowSeconds
- **Type**: `int`
- **Default**: `2` seconds
- **Obsolete Message**: `"Use DataSequenceSettings.GetMaxDelay(DataType.Cam1) instead"`
- **Status**: Marked `[Obsolete]` in code, functional for backward compatibility
- **UI Status**: Removed from Settings dialog

### 3. NormalFolderTimeWindowSeconds
- **Type**: `int`
- **Default**: `120` (2 minutes)
- **Obsolete Message**: `"Use DataSequenceSettings.GetMaxDelay(DataType.Normal) instead"`
- **Status**: Marked `[Obsolete]` in code, functional for backward compatibility
- **UI Status**: Removed from Settings dialog

## Migration Path

### For Users
**Old Way** (Advanced Tab → Matching Options):
```
NIR Time Window: 300 seconds
Camera Time Window: 2 seconds
Normal Folder Time Window: 120 seconds
```

**New Way** (Data Sequence Tab):
```
Data Sequence Configuration:
- NIR: Min=0s, Max=300s
- Cam1: Min=0s, Max=2s
- Normal: Min=0s, Max=120s
```

### For Developers
**Old Code** (generates CS0618 warning):
```csharp
var nirWindow = config.MatchingSettings.NirTimeWindowSeconds;
var camWindow = config.MatchingSettings.CameraTimeWindowSeconds;
```

**New Code** (recommended):
```csharp
var nirWindow = config.DataSequenceSettings.GetMaxDelay(DataType.NIR);
var camWindow = config.DataSequenceSettings.GetMaxDelay(DataType.Cam1);
```

## Files Modified (Phase 5)

### 1. ChronoView/Models/ApplicationConfiguration.cs
**Changes**:
- Added `[Obsolete]` attribute to three properties
- Updated XML documentation to indicate deprecation
- Properties remain functional (not removed)

**Code Location**: Lines 79-97 (MatchingSettings class)

### 2. ChronoView/UI/Views/SettingsDialog.xaml
**Changes**:
- Removed "Matching Options (DEPRECATED)" section from Advanced tab
- Removed explanatory TextBlock about deprecation
- Data Sequence tab remains unchanged and functional

**Code Location**: Advanced tab (previously ~line 148)

### 3. ChronoView/Core/Configuration/ConfigurationManager.cs
**Changes**:
- Removed validation for `NirTimeWindowSeconds` (negative check)
- Removed validation for `CameraTimeWindowSeconds` (negative check)
- Added validation for `DataSequenceSettings` (calls `Validate()` method)
- Throws `ConfigurationValidationException` if DataSequenceSettings validation fails

**Code Location**: `ValidateApplicationConfiguration()` method (~line 230)

## Current Usage (Still Using Deprecated Properties)

<!-- VERIFY: grep -rn "NirTimeWindowSeconds" ChronoView/ -->
<!-- VERIFY: grep -rn "CameraTimeWindowSeconds" ChronoView/ -->
<!-- VERIFY: grep -rn "NormalFolderTimeWindowSeconds" ChronoView/ -->

### MonitoringOrchestrator.cs
**Location**: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs` (lines ~1294-1296)

**Usage**: Fallback tolerance values when `DataSequenceSettings` is null

```csharp
private int GetMatchingTolerance(FileType fileType)
{
    // Try DataSequenceSettings first
    if (Configuration?.DataSequenceSettings != null)
    {
        var dataType = MapFileTypeToDataType(fileType);
        return Configuration.DataSequenceSettings.GetMaxDelay(dataType);
    }

    // Fallback to deprecated properties (generates CS0618 warnings)
    return fileType switch
    {
        FileType.Nir => Configuration?.MatchingSettings.NirTimeWindowSeconds ?? 300,
        FileType.Camera => Configuration?.MatchingSettings.CameraTimeWindowSeconds ?? 2,
        FileType.Normal => Configuration?.MatchingSettings.NormalFolderTimeWindowSeconds ?? 120,
        _ => 0
    };
}
```

**Why Still Used**: Provides backward compatibility for configurations without DataSequenceSettings

**Expected Warnings**: CS0618 (3 occurrences) - This is intentional and acceptable

## Backward Compatibility Strategy

### Configuration Loading
- Old config files with deprecated properties will load successfully
- Values are preserved in memory
- No data loss during config migration

### Configuration Saving
- Deprecated properties are still serialized to JSON
- Ensures old versions of the application can read new config files
- JSON serialization includes `[Obsolete]` properties by default

### Runtime Behavior
- If `DataSequenceSettings` is null → falls back to deprecated properties
- If `DataSequenceSettings` exists → uses new system (deprecated properties ignored)
- Gradual migration: users can update at their own pace

## Verification Commands

```bash
# Find all usages of deprecated properties
grep -rn "NirTimeWindowSeconds" ChronoView/
grep -rn "CameraTimeWindowSeconds" ChronoView/
grep -rn "NormalFolderTimeWindowSeconds" ChronoView/

# Check for CS0618 warnings in build output
dotnet build ChronoView/ChronoView.csproj 2>&1 | grep "CS0618"

# Verify DataSequenceSettings is being used
grep -rn "DataSequenceSettings.GetMaxDelay" ChronoView/
grep -rn "DataSequenceSettings.GetMinDelay" ChronoView/
```

## Expected Build Warnings

After Phase 5 completion, the following warnings are **expected and correct**:

```
CS0618: 'MatchingSettings.NirTimeWindowSeconds' is obsolete: 
        'Use DataSequenceSettings.GetMaxDelay(DataType.NIR) instead'
        
CS0618: 'MatchingSettings.CameraTimeWindowSeconds' is obsolete: 
        'Use DataSequenceSettings.GetMaxDelay(DataType.Cam1) instead'
        
CS0618: 'MatchingSettings.NormalFolderTimeWindowSeconds' is obsolete: 
        'Use DataSequenceSettings.GetMaxDelay(DataType.Normal) instead'
```

**Location**: `MonitoringOrchestrator.cs` (lines 1294, 1295, 1296)

**Why Acceptable**: These are intentional fallback paths for backward compatibility

## Future Removal Plan

### Phase 1 (Current - 2024-12-16)
- ✅ Mark properties as `[Obsolete]`
- ✅ Remove from UI
- ✅ Update validation logic
- ✅ Document migration path

### Phase 2 (Future - TBD)
- Monitor usage via telemetry/logs
- Ensure all users have migrated to DataSequenceSettings
- Provide migration tool if needed

### Phase 3 (Future - TBD)
- Remove `[Obsolete]` properties entirely
- Remove fallback logic from MonitoringOrchestrator
- Breaking change: requires major version bump

## Impact on Other Modules

### Low Impact
- **FileGroupMatcherService**: Does not use these properties directly
- **FileMatchingEngine**: Uses DataSequenceSettings exclusively (no fallback)
- **UI ViewModels**: No direct usage

### Medium Impact
- **MonitoringOrchestrator**: Uses as fallback (intentional, documented)
- **ConfigurationManager**: Validation logic updated

### High Impact
- **SettingsDialog**: UI removed (user-facing change)
- **User Configurations**: Existing configs remain valid but deprecated

## Related Documents

- [module_monitoring_orchestrator.md](module_monitoring_orchestrator.md) - Uses deprecated properties as fallback
- [glossary.md](glossary.md) - Official naming and deprecation status
- [.kiro/specs/refactor-matching-logic/requirements.md](../.kiro/specs/refactor-matching-logic/requirements.md) - Original requirements
- [.kiro/specs/refactor-matching-logic/tasks.md](../.kiro/specs/refactor-matching-logic/tasks.md) - Phase 5 implementation tasks

## Changelog

- 2024-12-16: Initial creation (Phase 5 completion)
  - Documented three deprecated properties
  - Recorded UI changes
  - Documented backward compatibility strategy
  - Added verification commands
