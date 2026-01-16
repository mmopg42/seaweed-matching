---
Task: fix_polling_sort_order
Created: 2026-01-09
Status: Draft
Depends On: 03_plan.md
---

# Fix Polling Sort Order - Detailed Design

## 1. Component Designs

### 1.1 ExtractTimestampForSorting

> Auto-detect file type and extract timestamp for sorting purposes.

#### Interface (from Plan)

```csharp
private DateTime? ExtractTimestampForSorting(string path)
```

#### Preconditions

- `path` is a valid file or folder path
- Path contains a recognizable filename pattern

#### Postconditions

- Returns extracted `DateTime` if pattern matches
- Returns `null` if no pattern matches (caller handles fallback)

#### Detailed Logic

```pseudo
function ExtractTimestampForSorting(path):
    // ========== GET FILENAME ==========
    fileName = Path.GetFileName(path)
    if fileName is null or empty:
        return null
    
    // ========== TRY NIR PATTERN ==========
    // Pattern: run_1YYYYMMDDTHHMMSS*.txt or *.spc
    if fileName.StartsWith("run_", ignoreCase):
        return FileNamingHelper.ExtractTimestampFromNirFileName(fileName)
    
    // ========== TRY NORMAL FOLDER PATTERN ==========
    // Pattern: CYYMMDDTHHMMSS_N
    if fileName.StartsWith("C", ignoreCase) and contains "T":
        result = FileNamingHelper.ExtractTimestampFromNormalFolderName(fileName)
        if result is not null:
            return result
    
    // ========== TRY CAMERA PATTERN ==========
    // Pattern: YYYYMMDD_HHMMSS_XXX.bmp/jpg/png
    if fileName contains "_" and has image extension:
        return FileNamingHelper.ExtractTimestampFromCameraFileName(fileName)
    
    // ========== FALLBACK ==========
    return null
```

#### Error Handling

| Error | Detection | Handling |
|-------|-----------|----------|
| Null path | Check at entry | Return null |
| Unparseable filename | Regex fails | Return null (fallback to MaxValue in caller) |

---

### 1.2 GetSensorPriority

> Return sensor priority for secondary sorting based on **DataSequenceSettings** order.

#### Interface (from Plan)

```csharp
private int GetSensorPriority(string path, DataSequenceSettings? sequenceSettings)
```

#### Preconditions

- `path` is a valid file or folder path
- `sequenceSettings` may be null (fallback to default order)

#### Postconditions

- Returns priority from DataSequenceSettings.GetOrder() for the detected DataType
- Returns `int.MaxValue` for unknown types (sorts last)

#### Detailed Logic

```pseudo
function GetSensorPriority(path, sequenceSettings):
    fileName = Path.GetFileName(path)
    if fileName is null or empty:
        return int.MaxValue
    
    // ========== DETECT DATA TYPE ==========
    DataType? detectedType = null
    
    if fileName.StartsWith("run_", ignoreCase):
        detectedType = DataType.NIR
    else if NormalFolderHelper.IsValidNormalFolder(fileName):
        detectedType = DataType.Normal
    else:
        extension = Path.GetExtension(fileName).ToLower()
        if extension in [".bmp", ".jpg", ".jpeg", ".png"]:
            detectedType = DataType.Camera  // Generic camera
    
    // ========== GET ORDER FROM SETTINGS ==========
    if detectedType is null:
        return int.MaxValue
    
    if sequenceSettings is null:
        // Fallback: NIR=1, Normal=2, Camera=3
        return detectedType switch {
            DataType.NIR => 1,
            DataType.Normal => 2,
            _ => 3
        }
    
    // Use configured order
    return sequenceSettings.GetOrder(detectedType.Value)
```

---

### 1.3 OnPollTick (Modified)

> Modified polling method with timestamp-based sorting.

#### Detailed Logic Change

**BEFORE (Line 177-178):**
```csharp
// 2. 파일명 기준 정렬 (타임스탬프가 파일명에 포함됨)
var sortedItems = newItems.OrderBy(item => Path.GetFileName(item.Path)).ToList();
```

**AFTER:**
```pseudo
function OnPollTick(state):
    // ... existing collection logic (lines 148-175) ...
    
    // ========== TIMESTAMP-BASED SORTING ==========
    // Sort by:
    //   1. Extracted timestamp (ascending) - earlier files first
    //   2. DataSequenceSettings order (ascending) - follows configured sequence
    
    // Note: sequenceSettings must be accessible (stored as field or passed)
    sequenceSettings = _currentConfig?.DataSequenceSettings
    
    sortedItems = newItems
        .Select(item => {
            timestamp = ExtractTimestampForSorting(item.Path)
            priority = GetSensorPriority(item.Path, sequenceSettings)
            return (item, timestamp ?? DateTime.MaxValue, priority)
        })
        .OrderBy(x => x.timestamp)   // Primary: timestamp
        .ThenBy(x => x.priority)     // Secondary: DataSequenceSettings order
        .Select(x => x.item)
        .ToList()
    
    // ... existing event firing logic (lines 180-188) ...
```

---

## 2. Integration Points

### 2.1 FileWatcherService → FileNamingHelper

#### Call Sequence

```
1. OnPollTick collects new files
2. For each file, call ExtractTimestampForSorting(path)
3. ExtractTimestampForSorting calls FileNamingHelper methods
4. Sort by returned timestamps + priorities
5. Fire events in sorted order
```

#### Data Contract

```
// Input to FileNamingHelper methods
path: string  // Full path or filename

// Output
DateTime?     // null if pattern doesn't match
```

---

## 3. Edge Cases & Boundary Conditions

| Case | Input | Expected Behavior |
|------|-------|-------------------|
| Unknown file type | `readme.txt` | DateTime.MaxValue, priority 99 (sorts last) |
| NIR and Camera same timestamp | Both at 17:47:03 | NIR processed first (priority 1 < 3) |
| Null filename | Empty path | Skip (return null/99) |
| Mixed Line 1 and Line 2 | Valid paths | Both sorted by timestamp regardless of line |
| Millisecond differences | Camera files with `_XXX` suffix | Same-second files sorted by priority |

---

## 4. Testing Strategy

### 4.1 Unit Test Cases

| Test Name | Input | Expected | Verifies |
|-----------|-------|----------|----------|
| `ExtractTimestamp_Nir` | `run_120260109T174703A.txt` | 2026-01-09 17:47:03 | NIR parsing |
| `ExtractTimestamp_Normal` | `C260109T174719_0` | 2026-01-09 17:47:19 | Normal parsing |
| `ExtractTimestamp_Camera` | `20260109_174717_757.bmp` | 2026-01-09 17:47:17 | Camera parsing |
| `ExtractTimestamp_Unknown` | `readme.txt` | null | Fallback |
| `GetPriority_Nir` | `run_1...` | 1 | NIR priority |
| `GetPriority_Normal` | `C260109T...` | 2 | Normal priority |
| `GetPriority_Camera` | `...bmp` | 3 | Camera priority |

### 4.2 Manual Verification Steps

```
1. Setup: Ensure multiple file types exist in watch folders
   - NIR files (run_1*.txt)
   - Normal folders (C*T*_N)
   - Camera files (*.bmp)

2. Action: Start monitoring and wait for polling cycle

3. Verify: Check UI logs for group creation order
   Expected: NIR files with earlier timestamps create lower group IDs
   
   Log should show:
   - nir1 (17:47:02) -> line1_060
   - nir1 (17:47:03) -> line1_061  (NOT 069)
   - normal (17:47:04) -> matches 061 or creates 062
```

---

## 5. Open Questions

- [x] All design questions resolved

---

## Approval

- [x] All components have detailed pseudo-code
- [x] Error handling specified
- [x] Edge cases covered
- [x] Test cases defined
- [x] No open questions

**Next Step**: 05_tasks.md
