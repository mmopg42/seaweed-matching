---
Task: fix_normal_suffix_usage
Created: 2026-01-08
Status: Draft
Depends On: 03_plan.md
---

# Fix Normal Folder Suffix Usage - Detailed Design

## 1. Component Designs

### 1.1 InitialScanner

> Responsible for initial file scanning and sorting based on configuration.

#### Interface (Modified)

```csharp
// Existing
public async Task<List<(string FilePath, DataType Type, DateTime Timestamp)>> ScanAndSortFilesAsync(ApplicationConfiguration config, CancellationToken cancellationToken)
```

#### Detailed Logic

```pseudo
function ScanAndSortFilesAsync(config, token):
    // ... existing logic ...

    // Case DataType.Normal
    if config.Normal1Path exists:
        files1 = GetDirectories(config.Normal1Path)
        // [NEW LOGIC: Delegate to Helper]
        files1 = files1.Where(f => NormalFolderHelper.IsValidNormalFolder(f.Name, config.MatchingSettings.UseFolderSuffix, 1))
        results.Add(files1 as Type=Normal)

    if config.Normal2Path exists:
        files2 = GetDirectories(config.Normal2Path)
        // [NEW LOGIC: Delegate to Helper]
        files2 = files2.Where(f => NormalFolderHelper.IsValidNormalFolder(f.Name, config.MatchingSettings.UseFolderSuffix, 2))
        results.Add(files2 as Type=Normal)
```

#### Verification
- **Test**: Set `UseFolderSuffix=true`. Create `Normal1Path/ERR_1`. Verify `ERR_1` is NOT included.
- **Test**: Set `UseFolderSuffix=false`. Create `Normal1Path/ERR_1`. Verify `ERR_1` IS included.

---

### 1.2 [NEW] NormalFolderHelper

> **Refactoring**: Centralized logic for Normal folder validation and line determination to avoid duplication.

#### Interface
```csharp
public static class NormalFolderHelper
{
    // Determines line number based on Config (Suffix vs Path)
    public static int DetermineLineNumber(
        string folderPath, 
        bool useSuffix, 
        string? normal1Path, 
        string? normal2Path);

    // Validates folder name/suffix based on expected line
    public static bool IsValidNormalFolder(
        string name, 
        bool useSuffix, 
        int? expectedLine = null);
        
    // (Internal) Path normalization helper
    private static bool IsSubPath(string child, string parent);
}
```

#### Logic
1.  **DetermineLineNumber**:
    *   IF `useSuffix`: Check `_0` (Line 1), `_1` (Line 2).
    *   FALLBACK: Use path check (Normal1Path vs Normal2Path).
    *   DEFAULT: Line 1.
2.  **IsValidNormalFolder**:
    *   First check basic pattern (e.g. `C\d{6}T\d{6}`).
    *   IF `useSuffix`:
        *   If `expectedLine` provided: Ensure suffix matches (`_0` or `_1`).
        *   If `expectedLine` null: Ensure *either* suffix exists.

---

### 1.3 FileWatcherService

> Handles background file watching and polling.

#### Interface (Modified)

```csharp
// Current
private bool IsNormalFolderName(string name)

// Modified
private bool IsNormalFolderName(string name, bool useSuffix, int? expectedLine = null) // expectedLine optional for strict checking
```

#### Detailed Logic

```pseudo
function IsNormalFolderName(name, useSuffix, expectedLine):
    // Delegate to Helper
    return NormalFolderHelper.IsValidNormalFolder(name, useSuffix, expectedLine)
```

#### Integration
- `PerformSilentScan`: Retrieve `UseFolderSuffix` from config. Call `IsNormalFolderName(name, config.UseFolderSuffix)`.
- `OnPollTick`: Same updates.

---

### 1.3 GroupManager

> Determines which group a file belongs to and its line number.

#### Interface (Modified logic only)

```csharp
private int DetermineLineNumber(string filePath, FileType fileType, ApplicationConfiguration config)
```

#### Detailed Logic

```pseudo
function DetermineLineNumber(filePath, fileType, config):
    useSuffix = config.MatchingSettings.UseFolderSuffix
    
    // Case: Normal File
    if fileType == Normal:
        return NormalFolderHelper.DetermineLineNumber(
            filePath, 
            useSuffix, 
            config.MatchingSettings.Normal1Path, 
            config.MatchingSettings.Normal2Path)

    // Other types (NIR, Camera) logic remains...
```

#### Path Normalization Logic
(Moved to Helper)

---

### 1.5 FileGroup

> Static helper for extracting line info.

#### Interface (Modified)

```csharp
public static int GetLineNumberFromNormalFolder(string folderName, bool useSuffix, string parentPath = null, string normal1Path = null, string normal2Path = null)
```

#### Detailed Logic
```pseudo
    return NormalFolderHelper.DetermineLineNumber(folderName, useSuffix, normal1Path, normal2Path)
```

---

### 1.6 StatisticsService

> Counts files for dashboard stats.

#### Detailed Logic in `GetFileCountsAsync`

```pseudo
    // Line 1
    // Filter logic handled by Helper inside CountDirectories? 
    // Or pass a predicate. 
    // Recommend: Update CountDirectoriesInDirectoryAsync to accept a predicate or use suffix logic locally using Helper.
    
    predicate1 = (name) => NormalFolderHelper.IsValidNormalFolder(name, useSuffix, 1)
    count1 = CountDirectoriesInDirectoryAsync(Normal1Path, predicate1)

    // Line 2
    predicate2 = (name) => NormalFolderHelper.IsValidNormalFolder(name, useSuffix, 2)
    count2 = CountDirectoriesInDirectoryAsync(Normal2Path, predicate2)
```

---

### 1.7 FileMatchingEngine

> Pure logic engine for matching files. Primarily used in Testing/Batch scenarios (Legacy), while `GroupManager` handles real-time logic. Modifying for consistency.

#### Interface (Modified)

```csharp
public static List<FileGroup> MatchFiles(
    UnmatchedFiles unmatchedFiles,
    DataSequenceSettings? dataSequenceSettings,
    MatchingConfiguration matchingConfig, // [CHANGED] Added config parameter
    HashSet<string> consumedNirKeys,
    ILogger? logger = null,
    Action<LogSeverity, string, string>? uiLog = null)
```

#### Detailed Logic Changes

**Design Decision**:
`FileMatchingEngine` assumes the input `UnmatchedFiles` has already separated normal files into `"normal1"` and `"normal2"` buckets (likely by the caller, e.g., Tests or legacy scanner using `InitialScanner` logic).

**Logic Update**:
1.  **Strict Mode (`UseFolderSuffix=true`)**:
    *   When processing `"normal1"` bucket: Verify each folder ends with `_0`. If not, log warning and skip (or fallback to path check if robust).
    *   When processing `"normal2"` bucket: Verify each folder ends with `_1`.
2.  **Path Mode (`UseFolderSuffix=false`)**:
    *   Trust the input extraction. If it's in the `"normal1"` bucket, treat as Line 1.

**Key: UnmatchedFiles Population**
*   The responsibility of segregating files into `"normal1"`/`"normal2"` lies with the *caller* (e.g., `MonitoringOrchestrator` or Test Setup).
*   Since `InitialScanner` is being fixed (Section 1.1) to respect `UseFolderSuffix`, any consumer using it to populate `UnmatchedFiles` will automatically be correct.

#### 2. Integration Points

### 2.1 Configuration → Components

Global `UseFolderSuffix` setting flows down to all services.

### 2.2 Suffix vs Path Priority

**Rule**:
- IF `UseFolderSuffix == true` THEN **Require Suffix** (strict).
- IF `UseFolderSuffix == false` THEN **Ignore Suffix**, Check Path.

> Note: User feedback recommended "If true, check suffix; else check path".
> My design: If true, *strict* check matches expectation. If logic finds a file in `Normal1Path` but missing `_0` when `UseFolderSuffix=true`, strictly it should be invalid or at least not "Line 1".
> However, for robustness, if it's in `Normal1Path`, it's almost certainly Line 1.
> **Refined Logic**:
> `if (UseFolderSuffix && hasSuffix) { use suffix }`
> `else { use path }`
> This handles the "UseFolderSuffix=true" but "folder has no suffix" edge case gracefully (fallback to path).

## 3. Edge Cases & Boundary Conditions

| Case | Input | Expected | Implementation |
|------|-------|----------|----------------|
| Suffix=False, Folder=`path/to/img_0` | `_0` exists | Line determined by path (Normal1Path vs Normal2Path) | Ignore suffix, check `StartsWith` |
| Suffix=True, Folder=`path/to/img` | No suffix | Fallback to path (or invalid?) | Fallback to path with warning |
| Path Overlap | `Normal1`=`C:/Data`, `Normal2`=`C:/Data/Line2` | Ambiguous logic | Use `IsBaseOf` strictly, favor longest match? (Out of scope, assume distinct paths as per constraints) |

## 4. Testing Strategy

### 4.1 Unit Tests (Mock Config)

- **Test `IsNormalFolderName`**:
  - `(name="A_0", suffix=true)` -> True
  - `(name="A", suffix=true)` -> False (or True if regex relaxed? Original regex required `_(\d+)$`. My design says specific check. Legacy `IsNormalFolderName` checked `C...T...`. New check adds suffix validation.)
  - `(name="A_0", suffix=false)` -> True (Regex ignores suffix requirements)

- **Test `DetermineLineNumber`**:
  - Setup: `Normal1=C:/A`, `Normal2=C:/B`.
  - Case 1: `Suffix=True`, File=`C:/A/Img_0`. -> Line 1.
  - Case 2: `Suffix=True`, File=`C:/A/Img_1`. -> Line 2 (Cross-path? Should warn. Suffix wins in legacy logic so maybe keep that priority).
  - Case 3: `Suffix=False`, File=`C:/A/Img_1`. -> Line 1 (Path wins).

---
