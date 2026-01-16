# Normal Folder Image Delay Impact Analysis

## 1. Overview
This document analyzes the side effects and operational behavior of the proposed solution for the "Normal Folder Image Delay" issue.
**Proposed Solution**: Do not add Normal folders to `_knownFiles` in `FileWatcherService` if the `stitched_original.png` file is missing. This forces the Polling mechanism to re-evaluate the folder in subsequent cycles.

## 2. Event Firing Behavior

### 2.1. The Event Loop
Since `_knownFiles` prevents the Poller from processing already-discovered items, skipping this addition results in the following loop:
1.  **T+0s (Poll)**: Poller finds Normal folder. `stitched_original.png` is missing.
    -   Event Fired: `FileSystemEventArgs(Created, FolderPath)`
    -   Action: Not added to `_knownFiles`.
2.  **T+2s (Poll)**: Poller finds Normal folder again (since it's not in `_knownFiles`). `stitched_original.png` still missing.
    -   Event Fired: `FileSystemEventArgs(Created, FolderPath)` (Duplicate)
    -   Action: Not added to `_knownFiles`.
3.  **... Repeats every polling interval (default 2s) ...**
4.  **T+N (Image Approx)**: Image file created.
    -   **FSW Path**: FileSystemWatcher detects `stitched_original.png` immediately.
    -   **Poll Path**: Next Poll cycle sees the folder. `stitched_original.png` now exists.
    -   Event Fired: `FileSystemEventArgs(Created, FolderPath)`
    -   Action: **Added to `_knownFiles`**. Loop terminates.

### 2.2. Event Volume
-   For a 40-second delay with a 2-second polling interval: **~20 duplicate events** will be fired for each Normal folder.
-   If multiple lines are active, this multiplies by the number of active empty folders.

## 3. Downstream Processing Impact

### 3.1. MonitoringOrchestrator
The `MonitoringOrchestrator` receives every event.
-   It determines the file type and calls `CreateOrUpdateGroupAsync`.
-   It logs "Processed group..." **only if** `CreateOrUpdateGroupAsync` returns a non-null group.

### 3.2. GroupManager (The Guard Rail)
`GroupManager` has a deduplication mechanism:
```csharp
private readonly HashSet<string> _processedFiles = new(StringComparer.OrdinalIgnoreCase);

// ... inside CreateOrUpdateGroupAsync ...
if (_processedFiles.Contains(filePath))
{
    _logger.LogDebug("Common: File already processed, skipping: {Path}", filePath);
    return null;
}
_processedFiles.Add(filePath);
```

**Scenario Analysis**:
1.  **First Event**: `_processedFiles` does not contain the path. Group is created (potentially with missing image). Path added to `_processedFiles`. Returns `Group`.
2.  **Subsequent Events (Loop)**: `_processedFiles` contains the path. Returns `null`.
    -   **Log**: "Common: File already processed, skipping: {Path}" (Debug level).

### 3.3. Correctness Verification
-   **Group Creation**: The group is created immediately upon the first folder detection.
-   **Image Update**: When the image finally arrives, `MonitoringOrchestrator` handles it via `FileNamingHelper.IsStitchedImage` check:
    ```csharp
    if (FileNamingHelper.IsStitchedImage(eventArgs.FullPath))
    {
        await _imageCache.HandleStitchedImageCaptureAsync(...);
        // ...
    }
    ```
    This path bypasses the `GroupManager` blocking issue for the *image data* itself (it loads into cache). The subsequent call to `CreateOrUpdateGroupAsync` (using the folder path) will be blocked by `GroupManager`, but this is acceptable since the group structure doesn't need to change, only the cache needs to be populated.

## 4. Potential Risks & Side Effects

### 4.1. Log Spam (Debug Level)
-   **Risk**: High volume of Debug logs.
-   **Impact**: If Debug logging is enabled in production, logs will be flooded with "File already processed" messages (20 per folder).
-   **Mitigation**: Ensure Log Level is set to Information or Warning in production.

### 4.2. Performance
-   **Disk I/O**: `File.Exists` is called for every pending folder every 2 seconds.
    -   Impact: Negligible for typical SSDs and small distinct folder counts (e.g., < 100 pending folders).
-   **CPU**: Event object allocation and string comparisons.
    -   Impact: Negligible.

### 4.3. Race Conditions
-   **Scenario**: Poller fires event just as FSW fires image event.
-   **Outcome**: `GroupManager` lock handles thread safety. `_imageCache` is also thread-safe. `processedFiles` ensures the group implementation logic runs only once.

## 5. Conclusion

The proposed solution (Skipping `_knownFiles` addition) is **Technically Safe** but **Inefficient**.

-   **Pros**:
    -   Solves the delay issue without complex state management (Pending lists).
    -   Self-healing: Eventually adds to `_knownFiles` when image appears.
-   **Cons**:
    -   Generates unnecessary event loop and log noise (Debug level).
    -   Relies on `GroupManager`'s deduplication logic to prevent chaos.

**Recommendation**:
Proceed with this implementation as a robust fix, provided that Debug logging noise is acceptable. It leverages existing safeguards (`_processedFiles`) to handle the side effects of the heuristic.
