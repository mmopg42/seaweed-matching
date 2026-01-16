---
Task: fix_general_cam_thumbnail_delay
Created: 2026-01-13
Status: Draft
Depends On: 03_plan.md
---

# Fix General Camera Thumbnail Delay - Detailed Design

## 1. Component Designs

### 1.1 FileWatcherService

> Core file watching service. Modifying `HandleEvent` to stop rewriting image events.

#### Interface (unchanged)

```csharp
void HandleEvent(FileSystemEventArgs e)
```

#### Detailed Logic

```pseudo
function HandleEvent(e):
    // ========== INPUT VALIDATION ==========
    if e is null: return

    // ========== CORE LOGIC ==========
    
    // Step 1: Initialize eventToFire with original event
    eventToFire = e

    // [REMOVE] The logic that rewrites stitched_original.png -> Folder Event
    /* 
    if Name Is "stitched_original.png":
       eventToFire = new event(GrandParent, ParentName)
    */ 
    // ^^^ This block is DELETED ^^^

    // Step 2: Thread Safety & Deduplication
    acquire_lock(_lockObject):
        // 2.1 Check if already processed
        if _knownFiles.contains(eventToFire.FullPath):
            return 

        // 2.2 Filtering (Normal Folders)
        folderName = Path.GetFileName(eventToFire.FullPath)
        
        // Logic for "Normal Folder without Image" Filtering
        if IsNormalFolderName(folderName) AND Directory.Exists(eventToFire.FullPath):
            stitchedPath = Path.Combine(FullPath, "stitched_original.png")
            if NOT File.Exists(stitchedPath):
                // Folder created, but image not there yet. 
                // Fire event (so UI shows placeholder), but DON'T add to _knownFiles.
                Invoke FileChanged(eventToFire)
                return 

        // 2.3 Add to Known Files
        _knownFiles.Add(eventToFire.FullPath)
    
    // Step 3: Fire Event (Outside lock usually, or inside depending on implementation - code does it after lock logic)
    Invoke FileChanged(eventToFire)
```

#### State Variables

- `_knownFiles`: HashSet tracking processed files to prevent duplicates.

#### Thread Safety

- **Crucial**: All access to `_knownFiles` must be inside `lock (_lockObject)`.
- `FileChanged` invocation should happen after the lock or be thread-safe.

#### Error Handling

- Standard exception handling in caller `ProcessWatcherEvent`.

---

### 1.2 MonitoringOrchestrator

> Orchestrates file processing. Modifying `ProcessSingleEventAsync` to handle `Renamed` events.

#### Interface (unchanged)

```csharp
Task ProcessSingleEventAsync(FileSystemEventArgs eventArgs, CancellationToken token)
```

#### Detailed Logic

```pseudo
function ProcessSingleEventAsync(eventArgs):
    // ========== DETECT & FAST CAPTURE ==========
    
    // Step 1: Fast Capture check is already here (Existing code)
    // IMPORTANT: Because FileWatcherService now sends the RAW image path,
    // IsStitchedImage(eventArgs.FullPath) will correctly return TRUE.
    if FileNamingHelper.IsStitchedImage(eventArgs.FullPath):
         // [NEW] Log that Fast Capture is triggered
         LogInformation("Fast Capture triggered for {Path}", eventArgs.FullPath)
         
         _imageCache.HandleStitchedImageCaptureAsync(...)
         
         // Path Normalization (Existing logic)
         // processingPath becomes parent folder
         processingPath = Path.GetDirectoryName(eventArgs.FullPath)
    else:
         processingPath = eventArgs.FullPath

    // ========== EVENT SWITCH ==========
    
    switch eventArgs.ChangeType:
        case Created:
        case Changed:
        case Renamed: // [NEW] Add this case (Handles 'Renamed' events from Watcher)
             // Step 2: Handle Creation/Update
             // Note: Polling events are always 'Created'. 
             // Renamed events come from FileSystemWatcher.
             
             group = GroupManager.CreateOrUpdateGroupAsync(processingPath)
             
             if group != null:
                 MarkFileAsProcessed(processingPath)
                 LogInformation("Group created/updated: {Id}", group.GroupId)
                 // GroupManager fires GroupCreated/Updated events internally
             break
             
        case Deleted:
             // (Existing logic)
             break
             
        default:
             LogWarning("Unknown event type")
```

#### State Transitions

- No state changes for the orchestrator itself.

#### Thread Safety

- Uses `SemaphoreSlim` (existing) to serialize event processing.

#### Error Handling

- `CreateOrUpdateGroupAsync` handles file access errors internally.

---

## 2. Integration Points

### 2.1 FileWatcherService → MonitoringOrchestrator

#### Call Sequence

1.  **Old Behavior**:
    - File System: `Renamed` "temp.png" -> "stitched_original.png"
    - `FileWatcher`: Intercepts. Rewrites to `Created` "ParentFolder".
    - `Orchestrator`: Receives `Created` "ParentFolder".
    - `IsStitchedImage` check: **False** (b/c it's a folder).
    - `GroupManager`: "ParentFolder" exists. No data change. **Ignores**.

2.  **New Behavior**:
    - File System: `Renamed` "temp.png" -> "stitched_original.png"
    - `FileWatcher`: Intercepts. Passes `Renamed` "stitched_original.png".
    - `Orchestrator`: Receives `Renamed` "stitched_original.png".
    - `IsStitchedImage` check: **True**.
    - **Fast Capture**: Loads image immediately.
    - Normalizes path to "ParentFolder".
    - `GroupManager`: Called with "ParentFolder". Updates internal state.
    - UI: `Fast Capture` callback triggers UI update.

## 3. Edge Cases & Boundary Conditions

| Case | Input | Expected Behavior |
|------|-------|-------------------|
| **Standard Creation** | `Created` "stitched_original.png" | Valid. Fast Capture triggers. Group updates. |
| **Delayed Creation (Rename)** | `Renamed` "stitched_original.png" | Valid. Fast Capture triggers. Group updates. |
| **Folder Only (No Image)** | `Created` "Folder" | Valid. Fast Capture skipped. Group created (placeholder). |
| **Image Deletion** | `Deleted` "stitched_original.png" | `FileWatcherService` passes raw event. Orchestrator handles `Deleted`. |

## 4. Testing Strategy

### 4.1 Unit Test Cases

No unit tests for `FileWatcherService` (it's hard to mock `FileSystemWatcher`).
Focus on Manual Verification.

### 4.2 Manual Verification Steps

1.  **Setup**: Use the user-provided Python simulation script (`make_files.py` variant).
    - Logic: Create Folder -> Sleep 10s -> Create `stitched_original.png` (via Rename/Move).
2.  **Action**: Run ChronoView. Start simulation.
3.  **Verify**:
    - [ ] Folder appears immediately (Placeholder).
    - [ ] After 10s, Image appears **automatically**.
    - [ ] No duplicate lines in the grid.
    - [ ] Logs show "Fast Capture" triggered.

---

## 8. Open Questions

- [x] All design questions resolved.

## Approval

- [x] All components have detailed pseudo-code
- [x] Integration points defined
- [x] Edge cases covered
- [x] Testing strategy defined

**Next Step**: 05_tasks.md
