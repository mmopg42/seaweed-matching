---
Task: Fix Camera File Merge Overwrite Issue
Created: 2025-12-15
Status: Draft
Depends On: requirements.md, research.md
---

# Fix Camera File Merge Overwrite Issue - Design

## Overview

This design addresses the camera file overwrite issue in real-time monitoring by adding Non-Duplicate Filters to Match 1 and Match 2, and making MergeGroups defensive against overwrites. The fix ensures each camera file is added to a unique group, preventing data loss.

## Architecture

### Current Flow (Buggy)
```
Camera File Arrives
    ↓
CreateOrUpdateGroupAsync()
    ↓
FileGroupMatcher.MatchFilesAsync() → Returns newGroup
    ↓
FindMatchingExistingGroup(newGroup)
    ↓
Match 1: NormalFolder? → YES → Return (NO duplicate check!) ❌
    ↓
MergeGroups(existingGroup, newGroup)
    ↓
existingGroup.CameraFiles[key] = value  ← OVERWRITES! ❌
```

### Fixed Flow
```
Camera File Arrives
    ↓
CreateOrUpdateGroupAsync()
    ↓
FileGroupMatcher.MatchFilesAsync() → Returns newGroup
    ↓
FindMatchingExistingGroup(newGroup)
    ↓
Match 1: NormalFolder + HasDataType? → NO (already has Cam1) → Continue ✓
    ↓
Match 2: NirKey + HasDataType? → NO (already has Cam1) → Continue ✓
    ↓
Match 3: Timestamp + HasDataType? → NO (already has Cam1) → Continue ✓
    ↓
No Match Found → Create New Group ✓
```

## Components and Interfaces

### Modified Components

| Component | Location | Changes |
|-----------|----------|---------|
| `FindMatchingExistingGroup()` | `MonitoringOrchestrator.cs:~900` | Add duplicate checks to Match 1 and Match 2 |
| `MergeGroups()` | `MonitoringOrchestrator.cs:~1082` | Add defensive check before overwriting camera keys |

### Unchanged Components
- `HasDataType()` - Already works correctly
- `DetermineDataTypeForGroup()` - Already works correctly
- `CreateOrUpdateGroupAsync()` - No changes needed
- Match 3 logic - Already has Non-Duplicate Filter

## Data Models

No data model changes required. Uses existing:
- `FileGroup` - Contains `CameraFiles` dictionary
- `DataType` enum - Used for type checking

## Detailed Design

### 1. Fix Match 1: Add Non-Duplicate Filter

**Location**: `MonitoringOrchestrator.cs`, `FindMatchingExistingGroup()` method, Match 1 section

**Current Code** (Line ~920):
```csharp
// Match 1: By NormalFolder (primary stable identifier)
if (!string.IsNullOrEmpty(newGroup.NormalFolder))
{
    var match = _activeGroups.Values
        .FirstOrDefault(g => g.NormalFolder == newGroup.NormalFolder);
    
    if (match != null)
    {
        _logger.LogInformation("Match 1: Found by NormalFolder={Folder}", newGroup.NormalFolder);
        return match;
    }
}
```

**Fixed Code**:
```csharp
// Match 1: By NormalFolder (primary stable identifier)
if (!string.IsNullOrEmpty(newGroup.NormalFolder))
{
    var newGroupDataType = DetermineDataTypeForGroup(newGroup);
    
    var match = _activeGroups.Values
        .Where(g => g.NormalFolder == newGroup.NormalFolder)
        .Where(g => !HasDataType(g, newGroupDataType))  // ← ADD: Non-Duplicate Filter
        .FirstOrDefault();
    
    if (match != null)
    {
        _logger.LogInformation("Match 1: Found by NormalFolder={Folder}, DataType={Type}", 
            newGroup.NormalFolder, newGroupDataType);
        return match;
    }
    else if (_activeGroups.Values.Any(g => g.NormalFolder == newGroup.NormalFolder))
    {
        // Group exists but already has this data type
        _logger.LogDebug("Match 1: NormalFolder={Folder} exists but already has {Type}", 
            newGroup.NormalFolder, newGroupDataType);
    }
}
```

**Rationale**: 
- Prevents matching to groups that already have the camera type
- Consistent with Match 3 behavior
- Allows camera files to create new groups when needed

### 2. Fix Match 2: Add Non-Duplicate Filter

**Location**: `MonitoringOrchestrator.cs`, `FindMatchingExistingGroup()` method, Match 2 section

**Current Code** (Line ~950):
```csharp
// Match 2: By NirKey (secondary stable identifier)
if (!string.IsNullOrEmpty(newGroup.NirKey))
{
    var match = _activeGroups.Values
        .FirstOrDefault(g => g.NirKey == newGroup.NirKey);
    
    if (match != null)
    {
        _logger.LogInformation("Match 2: Found by NirKey={Key}", newGroup.NirKey);
        return match;
    }
}
```

**Fixed Code**:
```csharp
// Match 2: By NirKey (secondary stable identifier)
if (!string.IsNullOrEmpty(newGroup.NirKey))
{
    var newGroupDataType = DetermineDataTypeForGroup(newGroup);
    
    var match = _activeGroups.Values
        .Where(g => g.NirKey == newGroup.NirKey)
        .Where(g => !HasDataType(g, newGroupDataType))  // ← ADD: Non-Duplicate Filter
        .FirstOrDefault();
    
    if (match != null)
    {
        _logger.LogInformation("Match 2: Found by NirKey={Key}, DataType={Type}", 
            newGroup.NirKey, newGroupDataType);
        return match;
    }
    else if (_activeGroups.Values.Any(g => g.NirKey == newGroup.NirKey))
    {
        // Group exists but already has this data type
        _logger.LogDebug("Match 2: NirKey={Key} exists but already has {Type}", 
            newGroup.NirKey, newGroupDataType);
    }
}
```

**Rationale**: Same as Match 1 - consistency across all matching strategies

### 3. Make MergeGroups Defensive

**Location**: `MonitoringOrchestrator.cs`, `MergeGroups()` method

**Current Code** (Line ~1103):
```csharp
// Merge camera files
foreach (var camera in newGroup.CameraFiles)
{
    if (!string.IsNullOrEmpty(camera.Value))
    {
        existingGroup.CameraFiles[camera.Key] = camera.Value;  // ← OVERWRITES!
    }
}
```

**Fixed Code**:
```csharp
// Merge camera files (defensive - never overwrite existing)
foreach (var camera in newGroup.CameraFiles)
{
    if (!string.IsNullOrEmpty(camera.Value))
    {
        // Only add if key doesn't exist or is empty
        if (!existingGroup.CameraFiles.ContainsKey(camera.Key) || 
            string.IsNullOrEmpty(existingGroup.CameraFiles[camera.Key]))
        {
            existingGroup.CameraFiles[camera.Key] = camera.Value;
            _logger.LogDebug("Merged camera {Key} into group {GroupId}", 
                camera.Key, existingGroup.GroupId);
        }
        else
        {
            _logger.LogWarning("Skipping camera merge: {Key} already exists in group {GroupId} (value={Existing})", 
                camera.Key, existingGroup.GroupId, existingGroup.CameraFiles[camera.Key]);
        }
    }
}
```

**Rationale**:
- Defense-in-depth: Prevents overwrites even if matching logic has bugs
- Consistent with "Preserves existing data" principle from architecture docs
- Provides clear logging when skipping merge

## Error Handling

### New Error Scenarios
1. **Camera key already exists during merge**
   - Handling: Log warning, skip merge, preserve existing value
   - User impact: No data loss, clear diagnostic message

### Existing Error Handling (Unchanged)
- File matching failures: Logged as warnings
- Group creation failures: Logged as errors
- Thread safety: Protected by existing `_lockObject`

## Testing Strategy

### Unit Tests
1. **Test MergeGroups with existing camera keys**
   - Setup: Create group with Cam1
   - Action: Merge new group with Cam1
   - Assert: Original Cam1 value preserved, warning logged

2. **Test Match 1 with duplicate data type**
   - Setup: Create group with NormalFolder + Cam1
   - Action: Find match for new group with same NormalFolder + Cam1
   - Assert: No match found (returns null)

3. **Test Match 2 with duplicate data type**
   - Setup: Create group with NirKey + Cam1
   - Action: Find match for new group with same NirKey + Cam1
   - Assert: No match found (returns null)

### Integration Tests
1. **Real-time monitoring with multiple camera files**
   - Setup: 8 Normal folders
   - Action: Add 8 Cam1 files sequentially
   - Assert: 8 separate groups created, no overwrites

2. **Mixed file arrival order**
   - Setup: Empty state
   - Action: Add Normal → Cam1 → Cam1 → Cam1
   - Assert: First Cam1 merges, next two create new groups

### Manual Testing
1. Start monitoring with 8 Normal folders
2. Add camera files in real-time
3. Verify UI shows 8 separate groups
4. Check logs for "Skipping camera merge" warnings (should be none in normal operation)

## Architecture Documentation Plan

### New Architecture Docs to Create
None - will update existing document

### Existing Docs to Update

| Document | Changes Required |
|----------|------------------|
| `docs/architecture/module_monitoring_orchestrator.md` | Update Match 1, Match 2, and MergeGroups sections |
| `docs/architecture/module_monitoring_orchestrator.md` | Add to Changelog: "2025-12-15: Fixed camera overwrite in Match 1/2" |
| `docs/architecture/README.md` | No changes (no new documents) |
| `docs/architecture/glossary.md` | Add Match 1, Match 2, Match 3, Non-Duplicate Filter terms |

## Naming Conventions

### New Terms to Add to Glossary

| Official Name | Type | Description | Used In |
|---------------|------|-------------|---------|
| `Match 1` | Concept | Matching strategy by NormalFolder identifier | `module_monitoring_orchestrator.md` |
| `Match 2` | Concept | Matching strategy by NirKey identifier | `module_monitoring_orchestrator.md` |
| `Match 3` | Concept | Matching strategy by timestamp with tolerance | `module_monitoring_orchestrator.md` |
| `Non-Duplicate Filter` | Concept | Filter excluding groups with existing data type | `module_monitoring_orchestrator.md` |

### Existing Glossary Terms to Use
- `FileGroup` - Data model for file groups
- `DataType` - Enum for file types (NIR, Normal, Cam1-6)
- `MonitoringOrchestrator` - Main orchestration class

## Configuration Changes

No configuration changes required.

## Performance Considerations

### Impact Analysis
- **Match 1/2 Performance**: Minimal - adds one additional `.Where()` filter
- **MergeGroups Performance**: Minimal - adds one `ContainsKey()` check per camera
- **Memory**: No change - same data structures
- **Thread Safety**: No change - existing lock protection sufficient

### Optimization Opportunities
None identified - changes are already optimal.

---
**Status**: [ ] Approved
