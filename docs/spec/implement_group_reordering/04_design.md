---
Task: implement_group_reordering
Created: 2026-01-13
Status: Draft
Depends On: 01_requirements.md
---

# Group Reordering (Eviction) - Detailed Design

## 1. Component Designs

### 1.1 GroupManager - Eviction Logic

> Modify `CreateOrUpdateGroupAsync` to detect and handle eviction scenarios.

#### Interface (Existing - Modified)

```csharp
public async Task<FileGroup?> CreateOrUpdateGroupAsync(string filePath, FileType fileType, ApplicationConfiguration config)
```

#### New Method: CheckAndPerformEviction

```csharp
private (bool ShouldEvict, FileGroup? VictimGroup) CheckEvictionNeeded(
    FileGroup newGroup, 
    IEnumerable<FileGroup> candidateGroups,
    ApplicationConfiguration config)
```

#### Preconditions

- `newGroup` has valid timestamp and data type
- `candidateGroups` are filtered by line number
- No matching group found via normal matching logic

#### Postconditions

- If eviction needed: returns victim group to evict from
- If no eviction: returns null, new group is created normally

#### Detailed Logic

```pseudo
function CreateOrUpdateGroupAsync(filePath, fileType, config):
    // ... existing logic to create newGroupTemplate ...
    
    lock (_lockObject):
        existingGroup = FindMatchingExistingGroup(newGroupTemplate, config)
        
        if existingGroup != null:
            // Normal merge logic
            MergeGroups(existingGroup, newGroupTemplate)
            return existingGroup
        
        // ========== NEW: EVICTION CHECK ==========
        evictionResult = CheckEvictionNeeded(newGroupTemplate, activeGroups, config)
        
        if evictionResult.ShouldEvict:
            victimGroup = evictionResult.VictimGroup
            
            // Step 1: Clone victim group to new ID (encapsulated logic)
            newGroupId = _idGenerator.GenerateNextId(victimGroup.LineNumber)
            evictedGroup = victimGroup.CloneWithNewId(newGroupId)
            
            // Step 2: Reset victim group and assign new file
            victimGroup.ResetData() // Clear all file paths and flags
            MergeGroups(victimGroup, newGroupTemplate)
            RaiseLog($"[Eviction] {newGroupTemplate.GroupId} took {victimGroup.GroupId}, evicting old data to {evictedGroup.GroupId}")
            
            // Step 3: Register evicted group
            _activeGroups[newGroupId] = evictedGroup
            
            // Step 4: Fire events
            GroupUpdated?.Invoke(victimGroup)
            // Note: Use GroupCreated for the "new" group that holds old data
            GroupCreated?.Invoke(evictedGroup)
            
            return victimGroup
        
        // Normal: create new group
        newGroupId = _idGenerator.GenerateNextId(lineNumber)
        _activeGroups[newGroupId] = newGroupTemplate
        GroupCreated?.Invoke(newGroupTemplate)
        return newGroupTemplate


function CheckEvictionNeeded(newGroup, activeGroups, config):
    newType = DetermineDataTypeForGroup(newGroup)
    newOrder = GetPriority(newType, config)
    orderedTypes = config.DataSequenceSettings.GetOrderedTypes()
    
    // Find groups where:
    // 1. newGroup can't match (already checked in FindMatchingExistingGroup)
    // 2. newGroup's expected predecessor range is AFTER existing group's timestamp
    
    foreach candidate in FilterByLine(activeGroups, newGroup.LineNumber):
        candidateType = DetermineDataTypeForGroup(candidate)
        candidateOrder = GetPriority(candidateType, config)
        
        // Only consider eviction if candidate has lower-order type (earlier in sequence)
        if candidateOrder >= newOrder:
            continue
        
        // Get delay settings for newGroup's type
        normalizedNewType = NormalizeForSequence(newType)
        minDelay = config.DataSequenceSettings.GetMinDelay(normalizedNewType)
        maxDelay = config.DataSequenceSettings.GetMaxDelay(normalizedNewType)
        
        // Calculate expected predecessor range
        // newGroup expects predecessor at: [newGroup.T - maxDelay, newGroup.T - minDelay]
        expectedPredMin = newGroup.Timestamp.AddSeconds(-maxDelay)
        expectedPredMax = newGroup.Timestamp.AddSeconds(-minDelay)
        
        candidateTimestamp = GetTimestampForDataType(candidate, candidateType)
        
        if candidateTimestamp.HasValue:
            // If candidate is AFTER our expected range, we should precede it
            if candidateTimestamp.Value > expectedPredMax:
                // Candidate is "too late" for us - we should take its position
                RaiseLog($"[Eviction Check] {newGroup} (T={newGroup.Timestamp}) expects predecessor before T={expectedPredMax}, but {candidate.GroupId} has T={candidateTimestamp}. Eviction needed.")
                return (true, candidate)
    
    return (false, null)
```

#### State Transitions

```
Normal Flow:
  NewFile → FindMatch → [No Match] → CreateNewGroup

Eviction Flow:
  NewFile → FindMatch → [No Match] → CheckEviction → [Evict] 
    → TakeVictimGroup → CreateGroupForEvictedData
```

---

### 1.2 FileGroup

> Modify `FileGroup` model to support safe cloning and resetting for eviction.

#### Interface (New Methods)

```csharp
public FileGroup CloneWithNewId(string newGroupId)
public void ResetData()
```

#### Detailed Logic

```pseudo
function CloneWithNewId(newGroupId):
    return new FileGroup
    {
        GroupId = newGroupId,
        NormalFolder = this.NormalFolder,
        NirKey = this.NirKey,
        Timestamp = this.Timestamp,
        LineNumber = this.LineNumber,
        HasNir = this.HasNir,
        // Deep copy dictionary to prevent reference issues
        CameraFiles = new Dictionary<string, string>(this.CameraFiles)
    }

function ResetData():
    this.NormalFolder = null
    this.NirKey = null
    this.HasNir = false
    this.CameraFiles.Clear()
    // Timestamp/LineNumber might need to be kept or reset depending on usage, 
    // but usually reset for fresh assignment. 
    // In this context, MergeGroups will overwrite them anyway.
```

---

## 2. Edge Cases & Boundary Conditions

| Case | Input | Expected Behavior |
|------|-------|-------------------|
| min=0 | Normal(T=45), Cam1(T=48) | No eviction (0 delay means same-time OK) |
| min=5, diff=3 | Normal(T=45), Cam1(T=48) | Eviction: Cam1 takes group, Normal evicted |
| Multiple files in group | Group has Normal+NIR, Cam1 arrives | Evict entire group contents |
| Empty group | Victim group is empty | Skip eviction (shouldn't happen) |

---

## 3. Data Structures

### ExtractGroupData Result

```csharp
record EvictedData(
    string? NirFilePath,
    string? NormalFolder,
    Dictionary<string, string> CameraFiles,
    DateTime Timestamp,
    int LineNumber
);
```

---

## 4. Testing Strategy

### 4.1 Unit Test Cases

| Test Name | Setup | Input | Expected |
|-----------|-------|-------|----------|
| `Test_Eviction_MinDelay5_DiffLessThan5` | Group has Normal(T=45) | Cam1(T=48), delay 5-9s | Normal evicted to new group |
| `Test_NoEviction_MinDelay0` | Group has Normal(T=45) | Cam1(T=48), delay 0-5s | Both coexist (or match) |
| `Test_Eviction_MultipleFilesInGroup` | Group has Normal+NIR | Cam1 evicts | Entire group evicted |

### 4.2 Manual Verification

1. Set Cam1 delay to min=5, max=9
2. Let Normal (T=45) create group
3. Send Cam1 (T=48)
4. **Expected**: UI shows Cam1 in earlier row, Normal in later row
5. Logs show `[Eviction]` message

---

## 5. Open Questions

- [x] All design questions resolved

---

## Approval

- [x] All components have detailed pseudo-code
- [x] Error handling specified
- [x] Edge cases covered
- [x] Test cases defined

**Next Step**: 05_tasks.md
