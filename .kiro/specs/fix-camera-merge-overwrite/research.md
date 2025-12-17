---
Task: Fix Camera File Merge Overwrite Issue
Created: 2025-12-15
Status: Draft
Depends On: requirements.md
---

# Fix Camera File Merge Overwrite Issue - Research Findings

## 1. Investigation Summary

Investigated the real-time file monitoring flow to understand why camera files are being overwritten despite the Match 3 Non-Duplicate Filter being in place.

## 2. Question Answers

### Q1: Where does the overwrite happen?
**Method**: Code analysis of MonitoringOrchestrator.cs
**Findings**: 
- Location: `MergeGroups()` method at line 1103
- Code: `existingGroup.CameraFiles[camera.Key] = camera.Value;`
- This unconditionally overwrites any existing value for the camera key
**Conclusion**: The overwrite happens in MergeGroups, not in the matching logic
**Evidence**: 
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

### Q2: Why does Match 3 Non-Duplicate Filter not prevent this?
**Method**: Code analysis of FindMatchingExistingGroup flow
**Findings**:
- Match 3 filter at line 1020: `.Where(g => !HasDataType(g, newGroupDataType))`
- This filter DOES work correctly - it excludes groups that already have the data type
- However, there are TWO other matching paths that bypass Match 3:
  - **Match 1**: By NormalFolder (line ~920)
  - **Match 2**: By NirKey (line ~950)
- These matches don't check for duplicate data types!

**Conclusion**: Match 3 filter works, but Match 1 and Match 2 bypass it
**Evidence**: 
```csharp
// Match 1: By NormalFolder (NO duplicate check)
var match = _activeGroups.Values
    .FirstOrDefault(g => g.NormalFolder == newGroup.NormalFolder);
if (match != null)
{
    _logger.LogInformation("Match 1: Found by NormalFolder={Folder}", newGroup.NormalFolder);
    return match;  // ← Returns without checking HasDataType!
}

// Match 2: By NirKey (NO duplicate check)
if (!string.IsNullOrEmpty(newGroup.NirKey))
{
    match = _activeGroups.Values
        .FirstOrDefault(g => g.NirKey == newGroup.NirKey);
    if (match != null)
    {
        _logger.LogInformation("Match 2: Found by NirKey={Key}", newGroup.NirKey);
        return match;  // ← Returns without checking HasDataType!
    }
}
```

### Q3: Should MergeGroups skip merging if camera key already exists?
**Method**: Review architecture documentation and intended behavior
**Findings**:
- From `docs/architecture/module_monitoring_orchestrator.md`:
  - "Preserves existing data and adds new file paths"
  - MergeGroups is intended to ADD data, not REPLACE
- NIR and MainImage merging uses conditional assignment:
  ```csharp
  if (!string.IsNullOrEmpty(newGroup.NirFilePath))
  {
      existingGroup.NirFilePath = newGroup.NirFilePath;  // Only if new has value
  }
  ```
- Camera merging should follow the same pattern

**Conclusion**: Yes, MergeGroups should skip if camera key already exists
**Evidence**: Architecture doc states "Preserves existing data"

### Q4: What is the correct behavior when a camera file arrives for a group that already has that camera type?
**Method**: Analyze user requirements and workflow
**Findings**:
- User scenario: 8 Normal folders, each should get its own camera files
- Expected: 8 separate groups, each with unique camera files
- Current bug: All camera files merge into first group
- Root cause: Match 1 (NormalFolder) matches camera files to existing Normal groups

**Conclusion**: Option B - Create new group (but Match 1/2 prevent this)
**Recommendation**: 
1. Add duplicate check to Match 1 and Match 2
2. Make MergeGroups defensive (skip existing keys)
3. Both fixes together ensure correctness

## 3. Code Analysis Results

### Relevant Existing Code

| File | Function/Class | Relevance |
|------|----------------|-----------|
| `MonitoringOrchestrator.cs` | `FindMatchingExistingGroup()` | Contains Match 1, 2, 3 logic |
| `MonitoringOrchestrator.cs` | `MergeGroups()` | Performs the overwrite |
| `MonitoringOrchestrator.cs` | `HasDataType()` | Helper to check if group has data type |
| `MonitoringOrchestrator.cs` | `DetermineDataTypeForGroup()` | Determines primary data type |

### Dependencies Identified
<!-- VERIFY: grep -rn "MergeGroups" ChronoView/ -->
- `MergeGroups()` is called from:
  - `CreateOrUpdateGroupAsync()` at line ~800
  - Only location - single call site

<!-- VERIFY: grep -rn "FindMatchingExistingGroup" ChronoView/ -->
- `FindMatchingExistingGroup()` is called from:
  - `CreateOrUpdateGroupAsync()` at line ~792
  - `PerformSequentialInitialScanAsync()` (indirectly via CreateOrUpdateGroupAsync)

### Glossary Check
- Existing related terms in glossary: None specific to this issue
- New terms to add:
  - `Match 1`: Matching by NormalFolder identifier
  - `Match 2`: Matching by NirKey identifier
  - `Match 3`: Matching by timestamp with tolerance and priority
  - `Non-Duplicate Filter`: Filter that excludes groups already containing a data type

## 4. Recommendations

### Primary Fix: Add Duplicate Check to Match 1 and Match 2
**Rationale**: Match 3 already has the filter, but Match 1 and Match 2 bypass it
**Implementation**:
```csharp
// Match 1: By NormalFolder - ADD duplicate check
var newGroupDataType = DetermineDataTypeForGroup(newGroup);
var match = _activeGroups.Values
    .Where(g => g.NormalFolder == newGroup.NormalFolder)
    .Where(g => !HasDataType(g, newGroupDataType))  // ← ADD THIS
    .FirstOrDefault();

// Match 2: By NirKey - ADD duplicate check  
if (!string.IsNullOrEmpty(newGroup.NirKey))
{
    match = _activeGroups.Values
        .Where(g => g.NirKey == newGroup.NirKey)
        .Where(g => !HasDataType(g, newGroupDataType))  // ← ADD THIS
        .FirstOrDefault();
}
```

### Secondary Fix: Make MergeGroups Defensive
**Rationale**: Defense-in-depth - prevent overwrites even if matching logic has bugs
**Implementation**:
```csharp
// Merge camera files - ONLY if key doesn't exist
foreach (var camera in newGroup.CameraFiles)
{
    if (!string.IsNullOrEmpty(camera.Value))
    {
        // Only add if key doesn't exist yet
        if (!existingGroup.CameraFiles.ContainsKey(camera.Key) || 
            string.IsNullOrEmpty(existingGroup.CameraFiles[camera.Key]))
        {
            existingGroup.CameraFiles[camera.Key] = camera.Value;
        }
        else
        {
            _logger.LogWarning("Skipping camera merge: {Key} already exists in group {GroupId}", 
                camera.Key, existingGroup.GroupId);
        }
    }
}
```

### Testing Strategy
1. **Unit Test**: Test MergeGroups with existing camera keys
2. **Integration Test**: Test real-time monitoring with multiple camera files
3. **Manual Test**: Run with 8 Normal folders + camera files, verify 8 groups created

## 5. Unanswered Questions
None - all questions resolved.

---
**Status**: [ ] Approved
