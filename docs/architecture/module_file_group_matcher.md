---
Owner: Development Team
Last Updated: 2024-12-14
Related PRs: []
Code Ref: (FileGroupMatcherService.cs)
---

# FileGroupMatcher - File Grouping Algorithm

## Overview

`FileGroupMatcherService` implements the core file grouping algorithm, matching files (Normal folders, NIR files, Camera files) into logical groups based on timestamp correlation. This is a C# port of Python's `group_manager.py`.

## Key Components

- **File**: `ChronoView/Core/FileMatching/FileGroupMatcherService.cs`
- **Interface**: `IFileGroupMatcher`
- **Namespace**: `ChronoView.Core.FileMatching`
- **Related**: Ported from `script/domain/group_manager.py`

## Contracts

### Inputs
- **UnmatchedFiles**: Structure containing:
  - `NirFiles`: Dictionary<string, Dictionary<string, string>> (line -> nirKey -> path)
  - `NormalFolders`: Dictionary<string, Dictionary<string, string>> (line -> folderName -> path)
  - `CameraFiles`: Dictionary<string, List<TimestampedFile>> (camKey -> files)
- **Configuration**: `MatchingConfiguration` with time windows and paths

### Outputs
- **FileGroup** list with:
  - `GroupId`: Sequential identifier (`group_001`, `group_002`, etc.)
  - `NormalFolder`: Folder name (e.g., "C251201T140543_0")
  - `NirKey`: NIR filename without extension (e.g., "20251201T140543")
  - `CameraFiles`: Dictionary of camera file paths
  - `Timestamp`: Group creation time
  - `LineNumber`: Production line (1 or 2)

### Errors/Exceptions
- Logs warnings for files without valid timestamps
- Returns empty group for invalid input
- Never throws exceptions (defensive programming)

### Side Effects
- None (pure grouping logic)
- Logs debug/info messages via ILogger

## Logic Flow

### Core Algorithm (BuildAllGroups)

```
1. Build Line 1 groups (Normal folders, NIR, Cam 1-3)
2. Build Line 2 groups (Normal2 folders, NIR2, Cam 4-6)
3. Combine all groups
4. Sort by timestamp (ascending)
5. Assign sequential GroupIds (group_001, group_002, ...)
```

### Line Group Building (BuildLineGroups)

**PRIMARY: Normal Folder-based Groups**
```
For each Normal folder:
  1. Create group with NormalFolder name
  2. Extract timestamp from folder name
  3. Match Cam1 (or Cam4) within time window
  4. Match Cam2/3 (or Cam5/6) based on Cam1 timestamp
  → Result: 1 Normal folder = 1 Group
```

**SECONDARY: NIR Attachment**
```
For each NIR file:
  1. Find closest group WITHOUT NIR
  2. Check time difference <= NirMatchTimeDiff (default: 1.0s)
  3. Attach to closest match OR create NIR-only group
```

**TERTIARY: Remaining Camera Files**
```
For each unmatched camera file:
  → Create cam-only group
```

**FINAL: Time-based Sorting**
```
Sort all groups (Normal, NIR-only, Cam-only) by timestamp
→ Ensures chronological order regardless of type
```

## Group Types

### Type 1: Normal-based Group (Most Common)
```csharp
{
    GroupId = "group_001",
    NormalFolder = "C251201T140543_0",
    MainImagePath = "Z:\path\C251201T140543_0\stitched_original.png",
    NirKey = "20251201T140543",  // MAY be null
    NirFilePath = "path\20251201T140543.spc",  // MAY be null
    CameraFiles = { 
        ["cam1"] = "20251201_140548_001.jpg",
        ["cam2"] = "20251201_140548_002.jpg"
    },
    Timestamp = 2025-12-01 14:05:43,
    HasNir = true,  // or false if no NIR matched
    LineNumber = 1
}
```
**Identifier**: `NormalFolder` name is PRIMARY key

### Type 2: NIR-only Group
```csharp
{
    GroupId = "group_005",
    NormalFolder = null,
    MainImagePath = null,
    NirKey = "20251201T140615",  // NOT null
    NirFilePath = "path\20251201T140615.spc",
    CameraFiles = {},  // empty
    Timestamp = 2025-12-01 14:06:15,
    HasNir = true,
    LineNumber = 1
}
```
**Identifier**: `NirKey` is PRIMARY key (when NormalFolder is null)

### Type 3: Cam-only Group
```csharp
{
    GroupId = "group_003",
    NormalFolder = null,
    MainImagePath = null,
    NirKey = null,
    NirFilePath = null,
    CameraFiles = { ["cam1"] = "20251201_140600_001.jpg" },
    Timestamp = 2025-12-01 14:06:00,
    HasNir = false,
    LineNumber = 1
}
```
**Identifier**: Camera file timestamp + line number

## Dependencies

### Internal
- `UnmatchedFiles` (ChronoView.Core.FileMatching): Input structure
- `FileGroup` (ChronoView.Models): Output model
- `MatchingConfiguration` (ChronoView.Core.FileMatching): Configuration

### External
- `System.Linq`: Sorting and filtering
- `System.IO`: File path operations
- `Microsoft.Extensions.Logging.ILogger`: Logging

### Config/Env
- `Configuration.NirMatchTimeDiff`: NIR matching tolerance (default: 1.0 seconds)
- `Configuration.CamMatchMinDiff`: Camera min time difference (default: 4.0 seconds)
- `Configuration.CamMatchMaxDiff`: Camera max time difference (default: 6.0 seconds)
- `Configuration.UseCamTimeMatching`: Enable time-based camera matching (default: true)

### Runtime Resources
- Filesystem (read): Reads file timestamps via File.GetLastWriteTime
- Memory: Builds all groups in memory before returning
- CPU: Sorting algorithm (O(n log n))

## Dependents
<!-- VERIFY: grep -rn "IFileGroupMatcher" ChronoView/ -->
<!-- VERIFY: grep -rn "FileGroupMatcherService" ChronoView/ -->

- **Used By**: 
  - `MonitoringOrchestrator` (ChronoView/Core/FileWatching/MonitoringOrchestrator.cs): Calls `MatchFilesAsync()`
  - Dependency injection (App.xaml.cs): Registered as singleton

- **Shared Variables/Constants**: None (stateless except _groupCounter, _consumedNirKeys)

- **Exported Interfaces**: 
  - `IFileGroupMatcher.MatchFilesAsync()`: Main grouping method

## Critical Behaviors

### ⚠️ GroupId is NOT Stable

**GroupIds are reassigned on EVERY call to MatchFilesAsync**:
```csharp
// Line 87-90
groups = groups.OrderBy(g => g.CreatedAt).ToList();
for (int i = 0; i < groups.Count; i++)
{
    groups[i].GroupId = $"group_{(i + 1):D3}";  // ← REGENERATED
}
```

**Implications**:
- GroupId is **NOT** a unique identifier for matching
- GroupId is **display-only** and changes based on sort order
- **DO NOT** use GroupId to find existing groups
- **USE** `NormalFolder` or `NirKey` as stable identifiers

### Matching Time Tolerances

**NIR Matching** (line 254):
```csharp
if (absDiff <= Configuration.NirMatchTimeDiff)  // default: 1.0 second
{
    // Attach to existing group
}
```

**Camera Matching** (line 349):
```csharp
if (diff >= Configuration.CamMatchMinDiff &&   // default: 4.0 seconds
    diff <= Configuration.CamMatchMaxDiff)     // default: 6.0 seconds
{
    // Match camera to normal folder
}
```

### Timestamp Extraction Priority

**Normal Folders** (line 421-459):
1. Regex pattern: `C251204T111028` → `yyMMddTHHmmss`
2. Regex pattern: `C20240115_143022` → `yyyyMMdd_HHmmss`
3. Fallback: null (skipped)

**NIR Files** (line 464-489):
1. Regex pattern: `20250926T103033` → `yyyyMMddTHHmmss`
2. Fallback: `File.GetLastWriteTime()` (C# version)

**Camera Files** (passed in via TimestampedFile):
1. Regex from filename: `20250120_162932` → `yyyyMMdd_HHmmss`
2. File.GetLastWriteTime()
3. DateTime.Now (with warning log)

## Failure Modes & Recovery

### Common Failures
1. **Invalid folder name format**: Skipped, logs warning
2. **NIR file without timestamp**: Falls back to LastWriteTime
3. **Camera file without timestamp**: Uses current time (with warning)

### Recovery Strategy
- All errors are logged, never thrown
- Invalid files are skipped gracefully
- Empty groups never created (defensive checks)

## Edge Cases

1. **Multiple NIR files with same timestamp**: First available NIR matches
2. **Normal folder without camera match**: Group created without cameras
3. **Camera file without Normal/NIR**: Cam-only group created
4. **Identical timestamps**: Stable sort preserves insertion order

## Impact / Touchpoints
<!-- VERIFY: grep -rn "MatchFilesAsync" ChronoView/ -->
<!-- VERIFY: grep -rn "BuildAllGroups" ChronoView/ -->

> [!IMPORTANT]
> **Before modifying this code, you MUST:**
> 1. Understand that GroupId is regenerated on every call
> 2. Use NormalFolder or NirKey as stable identifiers, NOT GroupId
> 3. Verify verification commands below

- `MonitoringOrchestrator.PerformInitialScanAsync`: Calls for initial scan with ALL files
- `MonitoringOrchestrator.CreateOrUpdateGroupAsync`: Calls for SINGLE file real-time matching
- `FileGroup.GroupId`: Generated here, NOT in FileGroup model

> [!WARNING]
> **Changing the matching logic will affect:**
> - How files are grouped at startup (initial scan)
> - How single files are matched in real-time
> - GroupId numbering sequence
> - UI display order (sorted by timestamp)

## Real-time Matching Challenge

**Problem**: `MatchFilesAsync` is designed for BATCH processing (all files at once), not INCREMENTAL processing (one file at a time).

**When called with single file**:
- Returns 1 group with GroupId = "group_001"
- Timestamp extracted from that file
- **NO knowledge of existing groups**

**Solution in MonitoringOrchestrator**:
- Must find existing group by `NormalFolder` or `NirKey` match
- Cannot rely on GroupId (always "group_001" for single file)
- Must use timestamp + line number as fallback

**Correct Matching Strategy**:
```csharp
// Match by stable identifier, NOT GroupId
if (!string.IsNullOrEmpty(newGroup.NormalFolder))
{
    existingGroup = _activeGroups.Values
        .FirstOrDefault(g => g.NormalFolder == newGroup.NormalFolder);
}
else if (!string.IsNullOrEmpty(newGroup.NirKey))
{
    existingGroup = _activeGroups.Values
        .FirstOrDefault(g => g.NirKey == newGroup.NirKey);
}
```

## Related Docs

- `module_monitoring_orchestrator.md`: Uses this for file matching
- `glossary.md`: Definitions of FileGroup, NormalFolder, NirKey, etc.

## Changelog

- **2024-12-14**: Initial architecture documentation
  - Documented grouping algorithm from Python group_manager.py
  - Clarified GroupId instability and matching strategy
  - Added real-time matching challenge section
