---
Task: fix_delayed_image_update_blocking
Created: 2026-01-13
Status: Draft
Depends On: 03_plan.md
---

# Fix Delayed Image Update Blocking - Design

## 1. Logic Modification

### 1.1 GroupManager.CreateOrUpdateGroupAsync

Current logic:
```csharp
if (_processedFiles.Contains(filePath)) return null;
_processedFiles.Add(filePath);
```

Proposed logic:
```csharp
// Remove global deduplication check here.
// Let the internal matching and MergeGroups handle duplicates.
// FileGroup existingGroup = FindMatchingExistingGroup(...)
// isDataChanged = MergeGroups(existingGroup, newGroupTemplate);
```

Rationale: `_processedFiles` was a "Band-Aid" fix for duplicate events, but it broke the legitimate "Update" scenario. `MergeGroups` already returns `false` if nothing changed, which is the correct way to deduplicate at the logic level.

### 1.2 EventProcessor Debouncing

Modify `ShouldSkipEvent` to be less aggressive for Normal folders or images.

Proposed change:
```csharp
if (isStitchedImage) return false; // Never debounce images
```

## 2. Implementation Details

### GroupManager.cs
```csharp
public async Task<FileGroup?> CreateOrUpdateGroupAsync(...)
{
    // [DELETE] 
    /*
    lock (_lockObject)
    {
        if (_processedFiles.Contains(filePath)) return null;
        _processedFiles.Add(filePath);
    }
    */
    
    // ... rest of the matching logic ...
}
```

### EventProcessor.cs
```csharp
public bool ShouldSkipEvent(...)
{
    // ...
    if (Path.GetFileName(eventArgs.FullPath).Equals("stitched_original.png", StringComparison.OrdinalIgnoreCase))
        return false;
    // ...
}
```

---

## Approval

- [ ] Design matches plan
- [ ] Logic changes are clear

**Next Step**: 05_tasks.md
