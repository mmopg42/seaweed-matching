---
Task: Refactor MonitoringOrchestrator Phase 3
Created: 2026-01-04
Status: Draft
Depends On: 03_plan.md
---

# Refactor MonitoringOrchestrator Phase 3 - Detailed Design

## 1. Component Designs

### 1.1 GroupManager (Class)

> Manages the lifecycle and state of FileGroups, providing thread-safe operations for matching and merging.

#### Interface
```csharp
public class GroupManager : IGroupManager
{
    public IEnumerable<FileGroup> ActiveGroups { get; }
    public FileGroup? FindGroupById(string groupId);
    public Task<FileGroup?> CreateOrUpdateGroupAsync(string filePath, FileType fileType, ApplicationConfiguration config);
    public bool RemoveGroup(string groupId);
    public void Clear();
    public void ResetState();
    
    public event EventHandler<FileGroup> GroupCreated;
    public event EventHandler<FileGroup> GroupUpdated;
    public event EventHandler<string> GroupRemoved;
}
```

#### State Variables
| Variable | Type | Initial | Purpose |
|----------|------|---------|---------|
| `_activeGroups` | ConcurrentDictionary<string, FileGroup> | empty | Store of active groups |
| `_nextGroupId` | int | 1 | Counter for unique IDs |
| `_pendingNirFiles` | List<(string, DateTime, DateTime)> | empty | NIR files waiting for groups |
| `_lockObject` | object | new object() | Global sync lock |

#### Detailed Logic: CreateOrUpdateGroupAsync
```pseudo
function CreateOrUpdateGroupAsync(filePath, fileType, config):
    try:
        // 1. Resolve Path
        processPath = filePath
        if fileType == Normal and has extension:
            processPath = GetDirectoryName(filePath)
        
        // 2. Create Template Group
        newGroup = CreateGroupFromSingleFile(processPath, fileType)
        if newGroup == null: return null
        
        lock (_lockObject):
            // 3. Find Match
            existingGroup = FindMatchingExistingGroup(newGroup)
            
            if existingGroup != null:
                // 4. Merge
                isDataChanged = MergeGroups(existingGroup, newGroup)
                targetGroup = existingGroup
            else:
                // 5. Create New
                newGroupId = GenerateNextGroupId()
                newGroup.GroupId = newGroupId
                _activeGroups.TryAdd(newGroupId, newGroup)
                targetGroup = newGroup
                isNew = true
        
        // 6. Raise Events (outside lock)
        if isNew:
            Raise GroupCreated(targetGroup)
        else if isDataChanged:
            Raise GroupUpdated(targetGroup)
            
        // 7. Match Pending NIR
        if !targetGroup.HasNir and fileType != Nir:
            if TryMatchPendingNirToGroup(targetGroup):
                Raise GroupUpdated(targetGroup)
                
        return targetGroup
    catch ex:
        Log Error
        return null
```

## 2. Integration Points

### 2.1 MonitoringOrchestrator → GroupManager
Orchestrator will delegate all group state changes to `GroupManager`. Orchestrator remains the coordinator of high-level workflows.

## 3. Edge Cases
- **Duplicate events**: `GroupManager` uses `_lockObject` and `FindMatchingExistingGroup` to prevent duplicate group creation from concurrent file events of the same set.
- **NIR Lead**: If NIR is configured as the first item in sequence, it creates the group. Otherwise, it's queued.

## 4. Test Strategy
- Unit test `GroupManager` with various file sequences (Normal then Cam, Cam then Normal).
- Verify `_nextGroupId` resets on `Clear()`.
