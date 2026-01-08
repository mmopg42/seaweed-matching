---
Task: Refactor MonitoringOrchestrator Phase 3
Created: 2026-01-04
Status: Draft
Depends On: 01_requirements.md
---

# Refactor MonitoringOrchestrator Phase 3 - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| Extract GroupManager | `GroupManager.cs` | Build check, code review |
| Maintain Match 1/2/3 logic | Moved to `GroupManager` | Manual verification of file matching |
| Reduce file size | Moving ~600 lines out | Line count check |

## 1. Architecture Overview

### 1.1 System Context
`GroupManager` will handle the stateful management of `FileGroup` objects, including matching new files to existing groups and merging data. `MonitoringOrchestrator` will coordinate between `IFileWatcher`, `IInitialScanner`, and the new `IGroupManager`.

### 1.2 Component Diagram
```
[MonitoringOrchestrator]
      │
      ├─► [IInitialScanner]
      ├─► [IFileWatcher]
      └─► [IGroupManager] (New)
              │
              └─► [IFileGroupMatcher]
```

## 2. Components

### 2.1 New Components
| Component | Type | Location | Purpose |
|-----------|------|----------|---------|
| `IGroupManager` | Interface | `Core/FileWatching/IGroupManager.cs` | Group lifecycle contract |
| `GroupManager` | Class | `Core/FileWatching/GroupManager.cs` | Group management implementation |

### 2.2 Modified Components
| Component | Location | Changes |
|-----------|----------|---------|
| `MonitoringOrchestrator` | `MonitoringOrchestrator.cs` | Remove group management methods, inject `IGroupManager` |
| `App.xaml.cs` | `App.xaml.cs` | Register `IGroupManager` |

## 3. Interface Definitions

### 3.1 IGroupManager
```csharp
public interface IGroupManager
{
    IEnumerable<FileGroup> ActiveGroups { get; }
    FileGroup? FindGroupById(string groupId);
    Task<FileGroup?> CreateOrUpdateGroupAsync(string filePath, FileType fileType, ApplicationConfiguration config);
    bool RemoveGroup(string groupId);
    void Clear();
    event EventHandler<FileGroup> GroupCreated;
    event EventHandler<FileGroup> GroupUpdated;
    event EventHandler<string> GroupRemoved;
}
```

## 4. Key Design Decisions

### 4.1 Group Manager Responsibility
**Context**: Should `GroupManager` handle the lock or should `Orchestrator`?
**Decision**: `GroupManager` should encapsulate its own thread-safety.
**Rationale**: Easier to test in isolation and reduces complexity in the Orchestrator.

## 5. Risks
- Match 3 logic is highly complex and depends on `DataSequenceSettings`.
- Event order and temporal constraints must be preserved.
