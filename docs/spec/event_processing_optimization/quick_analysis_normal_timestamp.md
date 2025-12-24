# Quick Code Analysis - Normal Folder Timestamp Extraction

## Current Implementation Status

### ✅ GOOD NEWS: Timestamp extraction from folder name ALREADY EXISTS

```csharp
// MonitoringOrchestrator.cs:1522-1567
private DateTime? ExtractTimestampFromFolderName(string? folderPath)
{
    // Pattern 1: C + 6 digits (date) + T + 6 digits (time)
    // Example: C251204T111028
    var match = Regex.Match(folderName, @"C(\d{6}T\d{6})");
    
    // Pattern 2: C + 8 digits (date) + _ + 6 digits (time)  
    // Example: C20240115_143022
    match = Regex.Match(folderName, @"C(\d{8}_\d{6})");
}
```

**User's folder format**: `C251216T214727_0`
- Base pattern: `C251216T214727` ✅ Matches Pattern 1
- Suffix: `_0` (line number)

### ⚠️ PROBLEM: Timestamp extraction happens TOO LATE

**Current Flow**:
```
1. stitched_original.png created
2. Polling detects (1s delay)
3. Event queued in channel
4. Sequential processing (queue wait)
5. CreateOrUpdateGroupAsync called
6. THEN ExtractTimestampFromFolderName called  ← happens here
7. External program already deleted file!
```

### 🎯 SOLUTION: Extract timestamp IMMEDIATELY upon detection

**Optimized Flow**:
```
1. Folder created (C251216T214727_0/)
2. FileSystemWatcher OR Polling detects
3. IMMEDIATELY extract timestamp from FOLDER PATH  ← move here!
4. Store (folderPath, timestamp) in fast cache
5. Queue for processing
6. External program deletes file (we don't care, we already have timestamp)
7. Processing uses cached timestamp
```

## Implementation Strategy

### Option A: Immediate Extraction in FileWatcherService (Preferred)
Extract timestamp the moment folder is detected, before queuing.

```csharp
// FileWatcherService.cs - OnFileSystemEvent or PollDirectories
if (isNormalFolder)
{
    var timestamp = ExtractTimestampFromFolderName(folderPath);
    // Cache it immediately
    _normalFolderTimestamps[folderPath] = timestamp;
    
    // Then queue for processing
    _eventChannel.Writer.TryWrite(args);
}
```

### Option B: Watch folder creation instead of file creation
Currently: Wait for `stitched_original.png` to appear
Better: Detect folder creation directly

```csharp
// FileSystemWatcher with NotifyFilter.DirectoryName
watcher.NotifyFilter = NotifyFilters.DirectoryName;
watcher.Created += OnFolderCreated;  // Immediate trigger
```

### Recommended Approach: Hybrid
1. Watch for folder creation (immediate trigger)
2. Extract timestamp from folder name (no file dependency)
3. Cache timestamp in memory
4. Process when ready (file can be deleted, we still have data)

## Impact on Requirements

### Updated Success Criteria
- ✅ Timestamp extraction from folder name: **Already implemented**
- ⚠️ Extraction timing: **Needs to move earlier in pipeline**
- ❌ File dependency: **Must be eliminated**

### Key Changes Needed
1. Move `ExtractTimestampFromFolderName` to earlier stage
2. Add folder creation watching (not file creation)
3. Cache extracted timestamps
4. Remove dependency on `stitched_original.png` existence during processing
