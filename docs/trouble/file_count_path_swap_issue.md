# File Count Display Issue - Configuration Path Swap

**Date**: 2025-12-18
**Status**: Resolved
**Severity**: High
**Root Cause**: NIR and Normal paths swapped in configuration file

---

## 1. Problem Summary

File count statistics in the UI header were not displaying correctly. All counts showed 0 or incorrect values despite files/folders existing in the monitored directories.

---

## 2. Root Cause

**Configuration paths were swapped**:

```json
// INCORRECT - paths are swapped!
"nir1Path": "Z:\\윤태경\\seaweed\\program\\data\\시뮬\\normal",      // ❌ Points to normal folder
"normal1Path": "Z:\\윤태경\\seaweed\\program\\data\\시뮬\\nir",      // ❌ Points to nir folder
```

**Should be**:
```json
// CORRECT
"nir1Path": "Z:\\윤태경\\seaweed\\program\\data\\시뮬\\nir",         // ✅ Points to nir folder
"normal1Path": "Z:\\윤태경\\seaweed\\program\\data\\시뮬\\normal",  // ✅ Points to normal folder
```

---

## 3. Why This Caused the Problem

### 3.1 How StatisticsService Counts Files

`StatisticsService.GetFileCountsAsync()` uses different counting methods:

```csharp
// NIR: Counts FILES in directory
if (!string.IsNullOrEmpty(config.MatchingSettings.Nir1Path))
{
    stats.NirCount = await CountFilesInDirectoryAsync(config.MatchingSettings.Nir1Path);
}

// Normal: Counts SUBDIRECTORIES (folders) in directory
if (!string.IsNullOrEmpty(config.MatchingSettings.Normal1Path))
{
    stats.NormalCount = await CountDirectoriesInDirectoryAsync(config.MatchingSettings.Normal1Path);
}
```

**Code ref**: `ChronoView/Core/Analytics/StatisticsService.cs:158-180`

### 3.2 The Mismatch

With swapped paths:

| Config Key | Points To | Counting Method | Result |
|------------|-----------|-----------------|--------|
| `nir1Path` | `normal` folder | `CountFilesInDirectoryAsync()` | Counts image files in normal subfolders (wrong!) |
| `normal1Path` | `nir` folder | `CountDirectoriesInDirectoryAsync()` | Counts 0 subdirectories in nir (wrong!) |

**Expected structure**:
- NIR folder: Contains `.txt`/`.csv` files directly
- Normal folder: Contains subdirectories like `C251218T120000_0/`

**What happened**:
- `NirCount` displayed count of files in normal folder (random images)
- `NormalCount` displayed 0 (nir folder has no subdirectories)

---

## 4. Investigation Process

### 4.1 Initial Hypothesis (Incorrect)

Initially suspected:
- Service not starting
- Events not firing
- Configuration paths empty

### 4.2 Key Discovery

After reviewing `config.json` at:
```
C:\Users\redli\AppData\Local\ChronoView\ChronoView\config.json
```

Found that paths existed and were non-empty, BUT they were **swapped**.

### 4.3 Verification

Confirmed by checking actual directory structure:
- `Z:\윤태경\seaweed\program\data\시뮬\nir\` - Contains `.txt` files
- `Z:\윤태경\seaweed\program\data\시뮬\normal\` - Contains `C*` subdirectories

---

## 5. Solution

### 5.1 Immediate Fix

Correct the configuration file manually:

**Location**: `%LOCALAPPDATA%\ChronoView\ChronoView\config.json`

**Change**:
```json
{
  "matchingSettings": {
    "nir1Path": "Z:\\윤태경\\seaweed\\program\\data\\시뮬\\nir",        // ✅ FIXED
    "normal1Path": "Z:\\윤태경\\seaweed\\program\\data\\시뮬\\normal",  // ✅ FIXED
    "camera1Path": "Z:\\윤태경\\seaweed\\program\\data\\시뮬\\cam1",
    "camera2Path": "Z:\\윤태경\\seaweed\\program\\data\\시뮬\\cam2",
    "camera3Path": "Z:\\윤태경\\seaweed\\program\\data\\시뮬\\cam3"
  }
}
```

### 5.2 Restart Application

After fixing configuration:
1. Close ChronoView
2. Reopen ChronoView
3. Click "Start" button
4. Counts should appear within 3 seconds

---

## 6. Prevention Measures

### 6.1 Add Configuration Validation (Recommended)

**Issue**: No validation that paths point to correct directory types.

**Proposed Solution**: Add heuristic validation in `ConfigurationManager`:

```csharp
public static void ValidateMatchingPaths(MatchingSettings settings)
{
    // Check NIR path contains .txt or .csv files
    if (!string.IsNullOrEmpty(settings.Nir1Path) && Directory.Exists(settings.Nir1Path))
    {
        var hasNirFiles = Directory.EnumerateFiles(settings.Nir1Path, "*.txt")
            .Concat(Directory.EnumerateFiles(settings.Nir1Path, "*.csv"))
            .Any();
        
        if (!hasNirFiles)
        {
            Logger.LogWarning("Nir1Path does not contain .txt/.csv files. Check if path is correct.");
        }
    }
    
    // Check Normal path contains subdirectories matching C pattern
    if (!string.IsNullOrEmpty(settings.Normal1Path) && Directory.Exists(settings.Normal1Path))
    {
        var hasNormalFolders = Directory.EnumerateDirectories(settings.Normal1Path)
            .Any(d => Path.GetFileName(d).StartsWith("C"));
        
        if (!hasNormalFolders)
        {
            Logger.LogWarning("Normal1Path does not contain C* subdirectories. Check if path is correct.");
        }
    }
}
```

**Benefits**:
- Early warning during startup
- Helps users identify configuration mistakes
- No false positives (only warnings, not errors)

### 6.2 Improve Settings Dialog UI

**Current**: Simple text boxes for paths

**Proposed**: 
- Add "Browse" buttons for path selection
- Show path validation status (✅/⚠️/❌)
- Display sample files/folders found in selected path
- Prevent swapping by showing labels like "NIR Files Path" vs "Normal Folders Path"

---

## 7. Related Issues

- `docs/spec/file_count_fix/01_requirements.md` - Requirements for file count fix
- `docs/spec/file_count_fix/02_research.md` - Investigation that led to this discovery
- `docs/trouble/normal_nir_matching_failure.md` - Previous similar path-related issue

---

## 8. Lessons Learned

1. **Configuration validation is critical** - Silent failures are hard to diagnose
2. **Path semantics matter** - "NIR path" vs "Normal path" have different expectations
3. **UI matters** - Better Settings dialog would prevent this error
4. **Logging is essential** - Without detailed logging, this would be much harder to find

---

## 9. Status

- [x] Root cause identified
- [x] Solution provided
- [ ] Configuration validation implemented (future enhancement)
- [ ] Settings dialog improved (future enhancement)

**Resolution**: User will manually fix configuration file and restart application.
