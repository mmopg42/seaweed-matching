---
Task: investigate_folder_based_matching
Created: 2026-01-13
Status: Draft
Depends On: 01_requirements.md
---

# Investigate Folder Based Matching - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: How does `FileWatcherService` distinguish between folder creation and file creation? | It uses `NotifyFilters.DirectoryName` and checks `Directory.Exists`. Critically, it **detects and fires events for Normal Folders** even if they are empty. | High |
| Q2: Does the system use folder name for timestamps? | **Yes.** `FileNamingHelper` extracts the timestamp directly from the Normal Folder's name, not the inner image file. | High |
| Q3: Is matching triggered immediately upon folder creation? | **Yes.** `MonitoringOrchestrator` treats the folder as a valid `Normal` input and calls `GroupManager.CreateOrUpdateGroupAsync` immediately. | High |
| Q4: How are empty folders handles? | `GroupManager` creates a `FileGroup` with `NormalFolder` path but empty `MainImagePath` (initially) if the image is missing during recursive search. | High |

## 2. Detailed Findings

### 2.1 Q1: How does `FileWatcherService` handle folder events?

**Method**: Code analysis of `ChronoView/Core/FileWatching/FileWatcherService.cs`.

**Findings**:
- `FileWatcherService` is configured to watch for `NotifyFilters.DirectoryName` (Line 59).
- `ShouldProcessEvent` (Line 352) explicitly returns `true` for folders if they match the `IsNormalFolderName` pattern.
- `HandleEvent` (Line 391) checks if the detected item is a Normal Folder.
    - It attempts to check for `stitched_original.png`.
    - **Crucially**, even if the image does NOT exist (Line 394), it explicitly fires the `FileChanged` event (Line 398) after logging "Normal folder without image".
    - This means the downstream systems receive an event as soon as the empty folder is created.

**Evidence**:
- `FileWatcherService.cs` Lines 391-400:
  ```csharp
  if (IsNormalFolderName(folderName) && Directory.Exists(eventToFire.FullPath))
  {
      var stitchedPath = Path.Combine(eventToFire.FullPath, "stitched_original.png");
      if (!File.Exists(stitchedPath))
      {
          // ...
          FileChanged?.Invoke(this, eventToFire); // <--- FIRES EVENT ANYWAY
          return;
      }
  }
  ```

### 2.2 Q2: Where does the timestamp come from?

**Method**: Code analysis of `GroupManager.cs` and `FileNamingHelper.cs`.

**Findings**:
- `MonitoringOrchestrator` identifies the empty folder as `FileType.Normal` (Line 642).
- `GroupManager.CreateGroupFromSingleFile` calls `FileNamingHelper.ExtractTimestamp(processPath, "Normal")`.
- `FileNamingHelper.ExtractTimestamp` delegates to `ExtractTimestampFromFolderName`.
- The timestamp is parsed entirely from the folder string (e.g., "C250113T140000").
- The system does **not** check the file creation time of the folder, nor does it wait for the image file's timestamp.

**Evidence**:
- `FileNamingHelper.cs` Line 148: `return ExtractTimestampFromFolderName(filePath);`
- `GroupManager.cs` Line 225: `var timestamp = FileNamingHelper.ExtractTimestamp(...)`

### 2.3 Q3: Why is the "Match done first"?

**Method**: Integration flow analysis.

**Findings**:
1.  **Event Trigger**: `FileWatcher` detects `C250113...` folder creation -> Fires Event.
2.  **Processing**: `MonitoringOrchestrator` receives event -> determines it's a `Normal` type -> calls `GroupManager`.
3.  **Group Creation**: `GroupManager` creates a new `FileGroup` using the timestamp from the **folder name**.
4.  **Matching**: `FindMatchingExistingGroup` runs immediately using this timestamp.
5.  **Result**: A group is formed/updated based on the folder's timestamp.
6.  **Later**: When `stitched_original.png` is created inside that folder, `FileWatcher` might detect it (as a file change), or if the logic filters it out (Line 365 sends explicit "Filtering file inside Normal folder" for files *inside* normal folders), the system relies on the initial folder event.
    - *Wait*: `FileWatcherService` Line 364 filters files *inside* Normal folders, **unless** it is `stitched_original.png` (Line 342).
    - So when the image arrives, another event fires for `stitched_original.png`.
    - `MonitoringOrchestrator` handles this (Line 261 "FAST CAPTURE"). It updates the group.
    - But the **initial match** was already established by the folder event.

**Conclusion**: The system is Architected to "Pre-match" based on the folder expecting the image to arrive shortly.

## 3. Code Analysis

### 3.1 Relevant Existing Code

| File | Component | Relevance | Notes |
|------|-----------|-----------|-------|
| `FileWatcherService.cs` | `HandleEvent` | Trigger point | Explicitly fires events for empty Normal folders. |
| `MonitoringOrchestrator.cs` | `DetermineFileType` | Type ID | Identifies folders as `FileType.Normal`. |
| `GroupManager.cs` | `CreateGroupFromSingleFile` | Logic | Extracts timestamp from folder path. |
| `FileNamingHelper.cs` | `ExtractTimestamp` | Parsing | Parses time from Name string. |

### 3.2 Implication
This behavior explains why "Folder itself is created matched". If the folder creation time (in name) and actual image capture time differ significantly, or if the "Matching" logic depends on exact timing, this "Early Matching" locks the group using the Folder's name-timestamp.

## 4. Recommendations

If the goal is to **Wait for the Image** before matching (to ensure the image exists or to use the image's timestamp, though the timestamp likely still comes from the folder name in this Naming Convention):

### Option A: Ignore Empty Folders in Watcher
Modify `FileWatcherService.cs` to **NOT** fire `FileChanged` if `stitched_original.png` is missing in a Normal Folder.
- **Pros**: Matches only when data is ready.
- **Cons**: Might delay UI showing "Incoming data" if that's a desired feature.

### Option B: Mark as "Pending Content"
Allow the Group to be created but mark it as `Incomplete` or `PendingImage`. Do not perform "Final Matching" (pairing with NIR/Cameras) until the image arrives.
- **Pros**: UI can show "Receiving..."
- **Cons**: Complex state management.

### Primary Recommendation
Current behavior seems intentional for "Folder-based Data Management". If the user wants to change this, **Option A** is the cleanest fix: **Only trigger processing when the actual image file (`stitched_original.png`) appears.**
This would involve changing `FileWatcherService.cs` to return/skip invoking event if `stitchedPath` does not exist (remove Line 398 logic).

## 5. Unanswered Questions
None. The mechanism is clear.
