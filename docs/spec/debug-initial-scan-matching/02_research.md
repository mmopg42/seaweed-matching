# Initial Scan Matching Debug - Research Findings

## 1. Investigation Summary

Analyzed startup logs from 2025-12-15 19:01 and reviewed `MonitoringOrchestrator.cs` code to understand two critical issues:
1. **Groups starting at `group_053` instead of `group_001`**
2. **Camera files showing "path missing" instead of being matched**

## 2. Log Analysis

### 2.1 Log Evidence

```
Debug 2025-12-15 오후 7:01:31  Group  VM created group_053 HasNir=True Normal= Main= CamCount=0
Debug 2025-12-15 오후 7:01:31  Thumb  Cam1 path missing for group_053
Debug 2025-12-15 오후 7:01:31  Thumb  Cam2 path missing for group_053
Debug 2025-12-15 오후 7:01:31  Thumb  Cam3 path missing for group_053
...
Debug 2025-12-15 오후 7:01:40  Group  VM created group_054 HasNir=False Normal= Main= CamCount=1
```

**Key Observations**:
- group_053: `HasNir=True`, `Normal=` (empty), `Main=` (empty), `CamCount=0` → NIR-only group
- group_054: `HasNir=False`, `Normal=` (empty), `CamCount=1` → Camera-only group
- No groups have all data types combined
- Cameras are not being matched to NIR/Normal groups

## 3. Code Analysis

### 3.1 Group Counter Logic

**File**: `MonitoringOrchestrator.cs`

```csharp
// Line 29: Counter initialization
private int _nextGroupId = 1;

// Line 466: Counter update after initial scan
var maxId = groupList
    .Select(g => {
        var idPart = g.GroupId.Replace("group_", "");
        if (int.TryParse(idPart, out int id)) return id;
        return 0;
    })
    .Max();
_nextGroupId = maxId + 1;
_logger.LogInformation("Updated next GroupId counter to {NextGroupId}", _nextGroupId);
```

**Finding**: Counter starts at 1 but gets updated to `maxId + 1` after initial scan completes. If FileGroupMatcher created groups 001-052, then `_nextGroupId` would be set to 53.

**Root Cause**: The counter is working correctly, but 52 groups were created by initial scan before the logs started showing. This suggests:
- Either the scan created 52 separate groups (one per file)
- Or there were leftover groups from a previous session

### 3.2 Sequential Initial Scan Logic

**File**: `MonitoringOrchestrator.cs`

```csharp
// Line 261-264: Check if DataSequenceSettings exists
if (_currentConfig?.DataSequenceSettings != null)
{
    _logger.LogInformation("Using sequential scan with Match 3 (DataSequenceSettings detected)");
    return await PerformSequentialInitialScanAsync(cancellationToken);
}
```

**Expected**: Should see "Using sequential scan" log if DataSequenceSettings is configured.
**From user logs**: No such log appears → Either:
1. DataSequenceSettings is null
2. Or logs are from AFTER initial scan completed

### 3.3 Camera File Scanner (Sequential Scan)

**File**: `MonitoringOrchestrator.cs`

```csharp
// Line 625-642: Camera file scanning
case DataType.Cam1:
    AddCameraFilesForScan(files, config.Camera1Path);
    break;
case DataType.Cam2:
    AddCameraFilesForScan(files, config.Camera2Path);
    break;
// ... (Cam3-6 similar)

// Line 652-667: Helper function
private void AddCameraFilesForScan(List<string> files, string? cameraPath)
{
    if (string.IsNullOrEmpty(cameraPath) || !Directory.Exists(cameraPath))
        return;  // ← CRITICAL: Returns silently if path is empty

    var cameraFiles = Directory.GetFiles(cameraPath, "*.*", SearchOption.AllDirectories);
    foreach (var file in cameraFiles)
    {
        var fileName = Path.GetFileName(file);
        if (System.Text.RegularExpressions.Regex.IsMatch(fileName, @"\d{8}_\d{6}"))
        {
            files.Add(file);
        }
    }
}
```

**Finding**: If `Camera1Path` through `Camera6Path` are null or empty, files will be empty list → No camera files scanned → "Cam path missing" in UI.

### 3.4 Non-Duplicate Filter (Match 3)

**File**: `MonitoringOrchestrator.cs`, Line 995

```csharp
var candidates = _activeGroups.Values
    .Where(g => g.LineNumber == newGroup.LineNumber)
    .Where(g => HasDataType(g, dataType))  // Has the earlier data type
    .Where(g => !HasDataType(g, newGroupDataType))  // ← CRITICAL FIX (2025-12-15)
    .Where(g => {
        var timeDiff = (newGroup.Timestamp - g.Timestamp).TotalSeconds;
        return timeDiff >= minDelay && timeDiff <= maxDelay;
    })
    // ... temporal ordering constraint ...
    .ToList();
```

**Purpose**: Prevents multiple files of the same type from overwriting the same group slot.

**Example**: If adding Cam1, only match groups that DON'T already have Cam1.

**Finding**: This filter is working as designed, but may prevent matching if:
- Camera files arrive in wrong order (e.g., Cam1 arrives BEFORE NIR)
- Or if data sequence order doesn't include cameras at correct priority

### 3.5 HasDataType Function

**File**: `MonitoringOrchestrator.cs`, Line 1734

```csharp
private bool HasDataType(FileGroup group, DataType dataType)
{
    return dataType switch
    {
        DataType.NIR => group.HasNir,
        DataType.Normal => !string.IsNullOrEmpty(group.NormalFolder),
        DataType.Cam1 => group.CameraFiles.ContainsKey("cam1") && !string.IsNullOrEmpty(group.CameraFiles["cam1"]),
        DataType.Cam2 => group.CameraFiles.ContainsKey("cam2") && !string.IsNullOrEmpty(group.CameraFiles["cam2"]),
        DataType.Cam3 => group.CameraFiles.ContainsKey("cam3") && !string.IsNullOrEmpty(group.CameraFiles["cam3"]),
        DataType.Cam4 => group.CameraFiles.ContainsKey("cam4") && !string.IsNullOrEmpty(group.CameraFiles["cam4"]),
        DataType.Cam5 => group.CameraFiles.ContainsKey("cam5") && !string.IsNullOrEmpty(group.CameraFiles["cam5"]),
        DataType.Cam6 => group.CameraFiles.ContainsKey("cam6") && !string.IsNullOrEmpty(group.CameraFiles["cam6"]),
        _ => false
    };
}
```

**Finding**: Function correctly checks if group has specific camera file.

## 4. Root Cause Analysis

### 4.1 Issue #1: Groups Starting at 053

**Hypothesis 1**: Initial scan created 52 separate groups (one per file)
- **Evidence**: Logs show `group_053`, `group_054`, `group_055` being created
- **Cause**: Files not matching correctly → Each file becomes separate group
- **Why**: Likely camera paths not configured → No camera files to match

**Hypothesis 2**: Counter not reset between sessions
- **Evidence**: Counter is instance variable (`_nextGroupId = 1`)
- **Refutation**: Counter IS reset on each app restart (line 29)
- **Conclusion**: NOT the cause

**VERDICT**: Most likely 52 groups were created during initial scan because files weren't matching correctly.

### 4.2 Issue #2: Camera Files Not Detected

**Hypothesis 1**: Camera paths not configured in `appsettings.json`
- **Evidence**: `AddCameraFilesForScan` returns silently if `cameraPath` is null/empty (line 654-655)
- **Result**: Camera file lists empty → No camera files in sequential scan
- **UI shows**: "Cam path missing" (because `CameraFiles` dictionary is empty)

**Hypothesis 2**: Camera files exist but don't match naming convention
- **Evidence**: Regex filter `\d{8}_\d{6}` (line 662)
- **Required format**: `YYYYMMDD_HHMMSS` (e.g., `20251201_140549.bmp`)
- **If mismatch**: Files skipped during scan

**Hypothesis 3**: DataSequenceSettings doesn't include camera types
- **Evidence**: If cameras not in sequence, they won't be processed
- **Result**: Even if files scanned, they won't be matched to groups

**VERDICT**: Most likely Camera1Path through Camera6Path are NOT configured in settings.

## 5. Verification Commands

### 5.1 Check Camera Path Configuration

```bash
# Search for Camera path settings in code
rg "Camera[1-6]Path" c:\workspace\seaweed\gui_kiro\ChronoView\
```

### 5.2 Check appsettings.json

```bash
# View current configuration
cat c:\workspace\seaweed\gui_kiro\ChronoView\appsettings.json
```

### 5.3 Check DataSequenceSettings

```bash
# Search for DataSequenceSettings usage
rg "DataSequenceSettings" c:\workspace\seaweed\gui_kiro\ChronoView\Core\
```

## 6. Recommended Next Steps

### 6.1 Immediate Actions

1. **Verify camera path configuration**
   - Check if `Camera1Path` through `Camera6Path` are configured in `appsettings.json`
   - If missing, add them to MatchingSettings

2. **Verify DataSequenceSettings**
   - Check if camera types (Cam1-6) are included in sequence
   - Verify order and tolerances are correct

3. **Review full startup logs**
   - Look for "Starting sequential scan" message
   - Look for "DataSequenceSettings: LOADED" message
   - This will confirm which scan mode was used

### 6.2 Design Improvements

If camera paths are NOT configured:
- Add UI validation in SettingsDialog to show warnings for unconfigured paths
- Add startup validation to log clear warnings about missing paths
- Consider adding "Quick Setup" wizard for first-time users

---

**Status**: [x] Completed
**Next Step**: Create 03_plan.md based on root cause findings
