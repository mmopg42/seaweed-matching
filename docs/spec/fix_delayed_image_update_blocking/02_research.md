---
Task: fix_delayed_image_update_blocking
Created: 2026-01-13
Status: Draft
Depends On: 01_requirements.md
---

# Fix Delayed Image Update Blocking - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: Why does `GroupManager` ignore the second event? | `_processedFiles` HashSet blocks any path already seen. | High |
| Q2: How does path normalization interact with deduplication? | Normalizing image path to folder path causes a collision in the deduplication list. | High |
| Q3: Granular deduplication? | Yes, we should likely check if the file is an image vs folder or check if data actually changed. | High |

## 2. Detailed Findings

### 2.1 Q1: Why does `GroupManager` ignore the second event (image creation)?

**Method**: Code analysis of `GroupManager.cs` and `MonitoringOrchestrator.cs`.

**Findings**:
- `GroupManager.cs` (Line 72-82) contains a `lock` block that checks if `filePath` is in `_processedFiles`.
- If present, it returns `null` immediately, skipping all matching and update logic.
- When a folder is first created, it is added to `_processedFiles`.

**Evidence**:
`GroupManager.cs:72-82`:
```csharp
lock (_lockObject)
{
    if (_processedFiles.Contains(filePath))
    {
        _logger.LogDebug("Common: File already processed, skipping: {Path}", filePath);
        return null; // <--- BLOCKS HERE
    }
    else
    {
         _processedFiles.Add(filePath);
    }
}
```

---

### 2.2 Q2: How does `MonitoringOrchestrator` path normalization interact with `GroupManager`'s deduplication?

**Method**: Trace the event flow in `MonitoringOrchestrator.ProcessSingleEventAsync`.

**Findings**:
- When `stitched_original.png` is detected, `IsStitchedImage` is true.
- The `processPath` is changed to `parentFolder` (the Normal folder).
- `CreateOrUpdateGroupAsync` is then called with this `parentFolder` path.
- Since the folder was likely already processed during its creation event, `GroupManager` rejects it.

**Evidence**:
`MonitoringOrchestrator.cs:267-272`:
```csharp
var parentFolder = Path.GetDirectoryName(eventArgs.FullPath);
if (!string.IsNullOrEmpty(parentFolder) && FileNamingHelper.IsNormalFolder(Path.GetFileName(parentFolder)))
{
    processPath = parentFolder; // <--- Path normalized to folder
    fileType = FileType.Normal;
}
```

## 3. Code Analysis

### 3.1 Relevant Existing Code

| File | Component | Relevance | Notes |
|------|-----------|-----------|-------|
| `GroupManager.cs` | `CreateOrUpdateGroupAsync` | Deduplication logic | Aggressive check blocks updates |
| `MonitoringOrchestrator.cs` | `ProcessSingleEventAsync` | Event normalization | Maps image events to folder paths |
| `EventProcessor.cs` | `ShouldSkipEvent` | Debouncing | 2-second window might also block quick updates |

## 4. Recommendations

### Primary Recommendation

Modify `GroupManager.CreateOrUpdateGroupAsync` to allow the processing to continue if the data in the group actually changes, or specifically exclude Normal folder paths from the global `_processedFiles` permanent block if they are not "Complete".

Better yet: **Remove the early exit in `GroupManager` and rely on `MergeGroups` to determine if `GroupUpdated` should be fired.** The current `_processedFiles` check is redundant with the event-level deduplication in `FileWatcherService` and `EventProcessor`, but it's more harmful because it's persistent across the entire monitoring session.

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Duplicate UI Updates | High | Low | `MergeGroups` returns `false` if no state changed, preventing redundant events. |
| Performance hit | Low | Low | The matching logic is relatively fast compared to I/O. |

---

## Approval

- [ ] All questions from requirements addressed
- [ ] Evidence provided for conclusions

**Next Step**: 03_plan.md
