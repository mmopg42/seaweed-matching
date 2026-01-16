# Group ID Generation Bug Analysis (2026-01-09) - UPDATED

## 1. Issue Summary
**Problem**: `nir1: run_120260109T174703A.txt` (Timestamp 17:47:03) was assigned to `line1_069` instead of the expected `line1_061`.

**User Confirmation**: NIR files arrive first (confirmed by user).

## 2. Root Cause Found ✓

### 2.1 The Sorting Bug in FileWatcherService.cs

**Location**: `FileWatcherService.OnPollTick()` at line 177-178

```csharp
// 2. 파일명 기준 정렬 (타임스탬프가 파일명에 포함됨)
var sortedItems = newItems.OrderBy(item => Path.GetFileName(item.Path)).ToList();
```

**Problem**: This sorts by **filename lexicographically**, not by timestamp!

### 2.2 Filename Formats
| Sensor Type | Filename Pattern          | First Character |
|-------------|---------------------------|-----------------|
| Camera      | `20260109_174717_757.bmp` | `2`             |
| Normal      | `C260109T174719_0`        | `C`             |
| NIR         | `run_120260109T174703A.txt` | `r`           |

### 2.3 Lexicographic Order
```
ASCII: '2' (50) < 'C' (67) < 'r' (114)
```

**Result**: 
1. Camera files (starting with `2`) are processed **FIRST**
2. Normal folders (starting with `C`) are processed **SECOND**
3. NIR files (starting with `r`) are processed **LAST**

This is the **opposite** of the intended timestamp order!

## 3. Log Verification

Original log shows "processing time" (system clock), not file arrival order:
```
17:47:17 - [새 그룹 생성] nir1: run_120260109T174703A.txt -> line1_069
```

By 17:47:17, Camera and Normal files from 17:47:17 onwards had already been processed (because they sort before 'r'), creating groups 061-068, leaving NIR to get 069.

## 4. Solution

### Option A: Extract and Sort by Embedded Timestamp (Recommended)
Modify `OnPollTick` to extract the actual timestamp from filenames and sort by that:

```csharp
// Extract timestamp from filename patterns:
// NIR: run_1YYYYMMDDTHHMMSS... -> YYYYMMDDTHHMMSS
// Camera: YYYYMMDD_HHMMSS_XXX.bmp -> YYYYMMDDHHMMSS
// Normal: CYYMMDDTHHMMSS_X -> YYMMDDTHHMMSS

var sortedItems = newItems
    .OrderBy(item => FileNamingHelper.ExtractTimestamp(item.Path, "polling") ?? DateTime.MaxValue)
    .ThenBy(item => GetSensorPriority(item.Path)) // NIR first at same timestamp
    .ToList();
```

### Option B: Leader-First Grouping
Group files by timestamp bucket, then process Leaders (NIR) first within each bucket.

## 5. Why Logs Appear Correct

The **log timestamps** (17:47:17, 17:47:19, etc.) show **when the log was written**, not when the file was detected. The sorting bug causes:

1. Camera files sorted first → create groups 061-068
2. NIR files sorted last → get leftover ID 069

The user observes NIR logs appearing "first" in time, but that's because NIR processing started at 17:47:17. By that point, Camera processing had already run (earlier in the same poll tick) and consumed IDs 061-068.

## 6. Recommended Fix

Modify `FileWatcherService.OnPollTick()` line 178:

**Before:**
```csharp
var sortedItems = newItems.OrderBy(item => Path.GetFileName(item.Path)).ToList();
```

**After:**
```csharp
// Sort by extracted timestamp, then by sensor priority (NIR first)
var sortedItems = newItems
    .Select(item => (item, Timestamp: ExtractTimestampFromPath(item.Path)))
    .OrderBy(x => x.Timestamp)
    .ThenBy(x => GetSensorPriority(x.item.Path))
    .Select(x => x.item)
    .ToList();
```
