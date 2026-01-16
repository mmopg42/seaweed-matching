---
Task: fix_general_cam_thumbnail_delay
Created: 2026-01-13
Status: Draft
Depends On: 01_requirements.md
---

# Fix General Camera Thumbnail Delay - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: Why is the delayed image not detected? | **`FileWatcherService` rewrites image events to folder events.** When `stitched_original.png` is created/renamed, `FileWatcherService` converts it to a `Created` event for the *parent folder*. `MonitoringOrchestrator` receives this folder event, fails the `IsStitchedImage` check (skipping fast capture), and then ignores the event because the folder group already exists. | High |
| Q2: What is the event type for delayed files? | Based on the user's simulation (`os.rename` / move), the event is likely `Renamed` or `Created` depending on the staging path location. However, `MonitoringOrchestrator` ignores `Renamed` (raw event). But `FileWatcherService` currently intercepts this and converts it to `Created` (Folder). So `MonitoringOrchestrator` sees `Created` (Folder), effectively masking the actual file arrival. | High |
| Q3: Does `Changed` event update the UI? | No. Because `FileWatcherService` converts the image event to a folder event, `MonitoringOrchestrator` thinks it's just the folder being "created" again. Since the folder path is identical to the existing group, `MergeGroups` finds no changes, and `GroupUpdated` is suppressed. | High |
| Q4: Will removing the event rewrite cause duplicate rows? | **No.** `MonitoringOrchestrator` (lines 264-272) explicitly normalizes `stitched_original.png` events to use the **Parent Folder Path** as the grouping key. `GroupManager` also has duplicate protection. Both the "Folder Created" event and the subsequent "Image Created" event will map to the *same* `NormalFolder` key, triggering an *update* to the existing row instead of creating a new one. | High |

## 2. Detailed Findings

### 2.1 Q1: Why is the delayed image not detected?

**Method**: Analysis of `FileWatcherService.cs` (lines 382-399) and `MonitoringOrchestrator.cs`.

**Findings**:
- `FileWatcherService.HandleEvent` explicitly checks for `stitched_original.png`.
- If found, it creates a **new** `FileSystemEventArgs` pointing to the **Parent Folder** with `WatcherChangeTypes.Created`.
- `MonitoringOrchestrator.ProcessSingleEventAsync` receives this *Folder* event.
- It checks `FileNamingHelper.IsStitchedImage(eventArgs.FullPath)`.
- Since `FullPath` is now the folder path, this returns `false`.
- **Fast Capture (`HandleStitchedImageCaptureAsync`) is SKIPPED.**
- The logic proceeds to `CreateOrUpdateGroupAsync(FolderFullPath)`.
- The group for this folder already exists (created when the empty folder first appeared).
- `GroupManager` sees no data change (Key is same, Path is same).
- No `GroupUpdated` event is fired.
- Result: UI assumes folder is still empty.

**Conclusion**: The logic intended to "group" events in `FileWatcherService` is hiding the critical `stitched_original.png` arrival event from the Orchestrator.

### 2.2 Q2: Rename vs Create handling?

**Method**: User provided simulation code uses `os.rename` and `move`.

**Findings**:
- Even if the raw OS event is `Renamed`, `FileWatcherService` subscribes to it (`watcher.Renamed += ...`).
- It then flows into `HandleEvent`.
- `HandleEvent` converts *any* `stitched_original.png` event into a `Created` event for the folder.
- So `MonitoringOrchestrator` never sees the `Renamed` event for the image, nor the `Created` event for the image. It only sees `Created` for the folder.

### 2.3 `MonitoringOrchestrator` Missing `Renamed` Handler

**Findings**:
- `MonitoringOrchestrator` *also* has a bug where it ignores raw `Renamed` events (switch case missing).
- However, due to the `FileWatcherService` rewrite, this bug is currently masked (the event becomes `Created` of folder).
- If we fix `FileWatcherService` to pass the raw image event, we **must also fix** `MonitoringOrchestrator` to handle `Renamed` (in case the file arrival is a rename), or `Created`.

### 2.4 Duplicate Rows Analysis

**Concern**: If we stop converting Image events to Folder events, will we get two rows (one for folder, one for image)?

**Analysis**:
- `MonitoringOrchestrator.cs`:
  ```csharp
  if (FileNamingHelper.IsStitchedImage(eventArgs.FullPath)) {
      // ... fast capture ...
      var parentFolder = Path.GetDirectoryName(eventArgs.FullPath);
      if (... IsNormalFolder(parentFolder)) {
          processPath = parentFolder; // <--- Forces path to be Folder Path
          fileType = FileType.Normal;
      }
  }
  ```
- This logic ensures that even if the input is `.../stitched_original.png`, the `processPath` passed to `GroupManager` is `.../`.
- `GroupManager` uses this path as the Key.
- Since the Key matches the existing Group (created when folder appeared), it merges the data.

**Conclusion**: Duplicate rows will **not** occur.

## 3. Code Analysis

### 3.1 Relevant Existing Code

| File | Component | Relevance | Notes |
|------|-----------|-----------|-------|
| `FileWatcherService.cs` | `HandleEvent` | Event Rewrite | Converts `stitched_original.png` -> Folder Created. **Root Cause of missing thumbnail.** |
| `MonitoringOrchestrator.cs` | `ProcessSingleEventAsync` | Event Handling | Uses `IsStitchedImage` for Fast Capture. Ignores `Renamed`. Normalizes path to prevent duplicates. |
| `MonitoringOrchestrator.cs` | `CreateOrUpdateGroupAsync` | Status Update | Fails to detect change if only `MainImagePath` file appearance changes but path string logic remains same (lazy check). |

## 4. Recommendations

### Primary Recommendation

**Fix both `FileWatcherService` and `MonitoringOrchestrator`**:

1.  **Modify `FileWatcherService.cs`**:
    - Remove the logic that attempts to convert `stitched_original.png` events into Folder events.
    - Let the raw `stitched_original.png` event pass through to the Orchestrator.

2.  **Modify `MonitoringOrchestrator.cs`**:
    - Add support for `WatcherChangeTypes.Renamed` in the switch statement (treat as `Created`/`Changed`).
    - Ensure `ProcessSingleEventAsync` handles `Renamed` events correctly so `IsStitchedImage` check works on the new path.
    - (The existing `IsStitchedImage` check is before the switch, so it should work fine once the event path is correct).

**Result**:
- When `stitched_original.png` appears (delayed):
- `FileWatcherService` fires event for `.../stitched_original.png`.
- `MonitoringOrchestrator` sees `IsStitchedImage` = true.
- Calls `_imageCache.HandleStitchedImageCaptureAsync`.
- Captures image -> Updates Cache -> `OnGroupUpdated`.
- UI refreshes with new thumbnail.
- `MonitoringOrchestrator` converts path to parent folder.
- `GroupManager` updates existing group (no duplicate row).

## 6. Unanswered Questions

None.

---

## Approval

- [x] All questions from requirements addressed
- [x] Evidence provided for conclusions
- [x] Recommendations are actionable
- [x] Risks identified

**Next Step**: 03_plan.md
