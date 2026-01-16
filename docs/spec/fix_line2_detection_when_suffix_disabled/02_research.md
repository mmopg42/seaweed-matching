---
Task: fix_line2_detection_when_suffix_disabled
Created: 2026-01-08
Status: In Progress
---

# Fix Line 2 Detection - Research

## 1. Investigation Results

### 1.1 Data Flow Analysis

```
User clicks Start →
  MonitoringOrchestrator.StartMonitoringAsync() →
    InitialScanner.ScanAndSortFilesAsync() →
      For each DataType.Normal:
        Scan Normal1Path → Add to files list
        Scan Normal2Path → Add to files list  ← Q: Are these actually being added?
    For each file in sorted list:
      EventProcessor.ProcessAsync() →
        GroupManager.CreateOrUpdateGroupAsync() →
          DetermineLineNumber() ← Q: Returns 1 or 2?
```

### 1.2 Key Findings from Code Review

#### Finding 1: NormalFolderHelper.DetermineLineNumber appears correct

```csharp
// When useSuffix=false, goes directly to path-based:
var parentDir = Path.GetDirectoryName(folderPath);
if (IsSubPath(parentDir, normal1Path)) return 1;
if (IsSubPath(parentDir, normal2Path)) return 2;
```

**Expected behavior**: If `folderPath` = `D:\Normal2\C251203T155907_0`:
- `parentDir` = `D:\Normal2`
- `IsSubPath(D:\Normal2, D:\Normal2)` = **true** → return 2 ✅

#### Finding 2: InitialScanner filter might be the issue

```csharp
// Current code in InitialScanner.cs:
if (NormalFolderHelper.IsValidNormalFolder(folderName, config.UseFolderSuffix, expectedLine: 2))
{
    files.Add(folder);
}
```

The **IsValidNormalFolder** function when `useSuffix=false`:
```csharp
if (!useSuffix)
    return true;  // Accepts any valid pattern, ignores expectedLine!
```

**This is correct** - when suffix disabled, all folders pass. So filtering is not the issue.

#### Finding 3: Suspected Real Issue - EventProcessor or path configuration

The logs show ONLY `_0` suffix folders:
```
C251203T155907_0
C251203T155910_0
...
```

**Two possibilities**:
1. **Normal2Path contains only `_0` folders** (user data issue)
2. **Normal2Path is empty or wrong** (configuration issue)
3. **Normal2Path folders are being scanned but filtered elsewhere**

## 2. Debug Plan

### Step 1: Verify Configuration
```powershell
# Check what Normal1Path and Normal2Path are set to
# In SettingsDialog or config.json
```

### Step 2: Verify Folder Contents
```powershell
# Check what folders exist in Normal2Path
Get-ChildItem -Directory "<Normal2Path>"
```

### Step 3: Add Debug Logging to InitialScanner
Add temporary logging to see if Normal2Path folders are being scanned:
```csharp
_logger.LogInformation("Scanning Normal2Path: {Path}, Found {Count} folders", 
    config.Normal2Path, folders.Length);
```

## 3. Hypotheses

| # | Hypothesis | Likelihood | How to Verify |
|---|------------|------------|---------------|
| 1 | Normal2Path is not configured | High | Check config.json |
| 2 | Normal2Path folder is empty | Medium | Check filesystem |
| 3 | EventProcessor skips Line 2 | Low | Add debug log |
| 4 | DetermineLineNumber returns wrong value | Low | Unit test |

## 4. Immediate Action Needed

**User must verify**:
1. Is `Normal2Path` correctly set in Settings dialog?
2. Does the folder contain any Normal folders?
3. Are these folders named correctly (C...T... pattern)?

---

**Next Step**: After user confirms config, proceed to 03_plan.md or rollback
