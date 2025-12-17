# Implementation Plan: Fix Real-time Matching with Stable Identifiers

## Overview

Fix `FindMatchingExistingGroup` in `MonitoringOrchestrator.cs` to use stable identifiers (`NormalFolder`, `NirKey`) instead of unstable `GroupId` for matching new files to existing groups.

## Changes Required

### File: `MonitoringOrchestrator.cs`

#### Change 1: Rewrite FindMatchingExistingGroup

**Location**: Lines 586-603 (current implementation)

**Current Logic** (INCORRECT):
```csharp
private FileGroup? FindMatchingExistingGroup(FileGroup newGroup)
{
    lock (_lockObject)
    {
        // Try GroupId match - FAILS (always "group_001" for single file)
        if (_activeGroups.ContainsKey(newGroup.GroupId))
            return _activeGroups[newGroup.GroupId];

        // Timestamp fallback - UNRELIABLE
        return _activeGroups.Values
            .FirstOrDefault(g => 
                g.LineNumber == newGroup.LineNumber && 
                Math.Abs((g.Timestamp - newGroup.Timestamp).TotalSeconds) < 1.0);
    }
}
```

**New Logic** (CORRECT):
```csharp
private FileGroup? FindMatchingExistingGroup(FileGroup newGroup)
{
    lock (_lockObject)
    {
        _logger.LogDebug("FindMatchingExistingGroup: NormalFolder={NormalFolder}, NirKey={NirKey}, LineNumber={LineNumber}",
            newGroup.NormalFolder ?? "null", newGroup.NirKey ?? "null", newGroup.LineNumber);

        // Match 1: By NormalFolder (primary identifier for Normal-based groups)
        if (!string.IsNullOrEmpty(newGroup.NormalFolder))
        {
            var match = _activeGroups.Values
                .FirstOrDefault(g => g.NormalFolder == newGroup.NormalFolder);
            
            if (match != null)
            {
                _logger.LogInformation("Found match by NormalFolder: {GroupId} (NormalFolder={NormalFolder})",
                    match.GroupId, match.NormalFolder);
                return match;
            }
        }

        // Match 2: By NirKey (identifier for NIR-only groups)
        if (!string.IsNullOrEmpty(newGroup.NirKey))
        {
            var match = _activeGroups.Values
                .FirstOrDefault(g => g.NirKey == newGroup.NirKey);
            
            if (match != null)
            {
                _logger.LogInformation("Found match by NirKey: {GroupId} (NirKey={NirKey})",
                    match.GroupId, match.NirKey);
                return match;
            }
        }

        // No match found - will create new group
        _logger.LogInformation("No match found for NormalFolder={NormalFolder}, NirKey={NirKey} - creating new group",
            newGroup.NormalFolder ?? "null", newGroup.NirKey ?? "null");
        return null;
    }
}
```

**Rationale**:
- `NormalFolder` is the **primary stable identifier** for Normal-based groups
- `NirKey` is the **stable identifier** for NIR-only groups
- Cam-only groups have no stable identifier → always create new group (acceptable)
- Removed GroupId matching (unstable - regenerated on each call)
- Removed timestamp fallback (unreliable - different groups can be seconds apart)

#### Change 2: Update CreateOrUpdateGroupAsync Logging

**Location**: Lines 431-472

**Enhancement**: Add more detailed logging for debugging

```csharp
// After line 450 (after getting newGroup from FileGroupMatcher)
_logger.LogInformation("FileGroupMatcher returned: GroupId={GroupId}, NormalFolder={NormalFolder}, NirKey={NirKey}, Timestamp={Timestamp}",
    newGroup.GroupId, 
    newGroup.NormalFolder ?? "null", 
    newGroup.NirKey ?? "null", 
    newGroup.Timestamp);
```

This helps track:
- What identifiers the FileGroupMatcher assigned
- Which matching logic was used (NormalFolder vs NirKey vs none)
- When new groups are created vs existing groups updated

## Testing Strategy

### Test Case 1: Normal Folder Matching

**Setup**:
- Initial scan creates `group_001` with `NormalFolder="C251201T140543_0"`

**Test**:
1. Create new file in `C251201T140543_0` (same Normal folder)
2. Real-time processing triggered

**Expected**:
- Logs: "Found match by NormalFolder: group_001"
- File added to `group_001` (not new group)

### Test Case 2: NIR-only Group Matching

**Setup**:
- Initial scan creates `group_005` with `NirKey="20251201T140615"` (NIR-only)

**Test**:
1. New NIR file `20251201T140615.spc` detected in real-time
2. FileGroupMatcher returns group with same `NirKey`

**Expected**:
- Logs: "Found match by NirKey: group_005"
- NIR updated in `group_005`

### Test Case 3: New Group Creation

**Setup**:
- Initial scan created groups with various Normal folders

**Test**:
1. New Normal folder `C251201T140700_0` appears

**Expected**:
- Logs: "No match found... - creating new group"
- New `group_006` created

### Test Case 4: Cam-only Files

**Setup**:
- Camera file arrives with no matching Normal folder or NIR

**Test**:
1. Cam file `20251201_140800_001.jpg` detected

**Expected**:
- FileGroupMatcher creates Cam-only group
- FindMatchingExistingGroup finds no match (no stable ID)
- New group created
- **Note**: This may create duplicate Cam-only groups (acceptable per requirements)

## Verification Commands

After implementation:

```bash
# Build
dotnet build ChronoView/ChronoView.csproj

# Run and monitor logs
dotnet run --project ChronoView/ChronoView.csproj

# Watch for log patterns:
# - "Found match by NormalFolder"
# - "Found match by NirKey"
# - "No match found... - creating new group"
```

## Rollback Plan

If the fix causes issues:

1. Revert `FindMatchingExistingGroup` to timestamp-based matching:
   ```csharp
   // Emergency rollback - use timestamp with WIDER tolerance
   return _activeGroups.Values
       .FirstOrDefault(g => 
           g.LineNumber == newGroup.LineNumber && 
           Math.Abs((g.Timestamp - newGroup.Timestamp).TotalSeconds) < 5.0);
   ```

2. Investigate why stable identifiers aren't working as expected

## Documentation Updates

After successful implementation:

1. Update `module_monitoring_orchestrator.md`:
   - Document new FindMatchingExistingGroup logic
   - Add examples of stable identifier matching

2. Update architecture README:
   - Mark this fix as verified

3. Create walkthrough.md showing:
   - Before/after logs
   - Screenshots of correct grouping

## Success Criteria

✅ All new Normal folders create new groups  
✅ Files in same Normal folder update existing group  
✅ NIR files match to correct NIR-only groups  
✅ No files incorrectly merged into wrong groups  
✅ Logs clearly show matching decisions  

---

**Estimated Implementation Time**: 30 minutes  
**Risk Level**: Low (only changes matching logic, no data model changes)  
**Dependencies**: None (self-contained change)
