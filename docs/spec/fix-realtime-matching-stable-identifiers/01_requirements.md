# Fix Real-time File Matching with Stable Identifiers

## 📋 Document Information
- **Created**: 2024-12-14
- **Status**: Draft
- **Priority**: High
- **Assigned**: Development Team

## � Domain Knowledge

### Production Lines

This system monitors **two independent production lines**:

| Line | Normal Folders | NIR Files | Camera Files |
|------|----------------|-----------|--------------|
| **Line 1** | `normal1` | `nir1` | `cam1`, `cam2`, `cam3` |
| **Line 2** | `normal2` | `nir2` | `cam4`, `cam5`, `cam6` |

### File Types and Identifiers

**Normal Folders** (일반 카메라):
- Example: `C251201T140543_0` (format: `CyyMMddTHHmmss_lineNumber`)
- Contains `stitched_original.png` (main image)
- **PRIMARY identifier** for groups
- One folder = One group (base)

**NIR Files** (근적외선 분광기):
- Example: `20251201T140543.spc` (format: `yyyyMMddTHHmmss.spc`)
- Key (without extension): `20251201T140543`
- **STABLE identifier** for NIR-only groups
- Attaches to Normal groups within time tolerance

**Camera Files** (복합 카메라):
- Example: `20251201_140548_001.jpg` (format: `yyyyMMdd_HHmmss_seq.jpg`)
- **Line 1**: cam1, cam2, cam3
- **Line 2**: cam4, cam5, cam6
- No stable identifier (matched by timestamp)

### File Timing Relationships

**NIR before Normal**:
- NIR is captured **BEFORE** Normal folder
- Example: NIR at 14:05:04, Normal at 14:05:05
- Tolerance: Normal can be 0~1 second **after** NIR

**Camera after Normal**:
- **cam1 (Line 1) or cam4 (Line 2)** is "reference camera"
- Reference camera: 4~6 seconds **after** Normal folder
- **cam2/3 (Line 1) or cam5/6 (Line 2)**: ±1 second from reference camera

**Example Timeline**:
```
14:05:04 - NIR file created
14:05:05 - Normal folder created (1s after NIR) ✅
14:05:10 - cam1 captured (5s after Normal) ✅
14:05:10 - cam2 captured (same time as cam1) ✅
14:05:11 - cam3 captured (1s after cam1) ✅
```

### Camera Matching Modes

**Configuration**: `UseCamTimeMatching` (default: `true`)

**Mode 1: Time-based Matching** (`UseCamTimeMatching = true`):
- **cam1/cam4** must be 4~6 seconds **after** Normal folder
- **cam2/3 or cam5/6** must be ±1 second from cam1/cam4
- If no file matches time range → skipped (group has no camera file)
- **Pros**: Accurate matching, fewer errors
- **Cons**: Strict timing requirements

**Mode 2: Sequential Matching** (`UseCamTimeMatching = false`):
- **Ignores timestamps completely**
- Just pops first file from queue for each camera
- Normal folder 1 → first cam1 file, first cam2 file, first cam3 file
- **Pros**: Always matches (no orphan files)
- **Cons**: May match wrong files if timing is off

**Example**:
```
Normal folders: [C251201T140543_0, C251201T140600_0]
cam1 queue: [file1@14:05:48, file2@14:06:05]

Time-based mode:
  - C251201T140543_0 (14:05:43) → file1 (14:05:48, +5s) ✅
  - C251201T140600_0 (14:06:00) → file2 (14:06:05, +5s) ✅

Sequential mode:
  - C251201T140543_0 → file1 (first in queue)
  - C251201T140600_0 → file2 (next in queue)
```

## �🎯 Goal

Fix the real-time file matching logic in `MonitoringOrchestrator` to correctly identify and update existing groups by using stable identifiers (`NormalFolder`, `NirKey`) instead of the unstable `GroupId`.

## 🔍 Background

### Current Problem

Real-time file monitoring is incorrectly merging all new files into the same existing group (e.g., `group_001`) instead of creating separate groups for each unique file.

**User Report**:
```
10:27:33 - Created group_008 (C251201T140556_0)
10:27:33 - Created group_009 (C251201T140558_0)
...
10:34:15 - Start load group_001 (repeated 30+ times)
10:34:16 - Start load group_001 (repeated)
10:34:17 - Start load group_001 (repeated)
```

All new files are being added to `group_001` instead of creating new groups or finding the correct existing group.

### Root Cause

**FileGroupMatcher Behavior** (from architecture analysis):
1. `GroupId` is **regenerated** on every call to `MatchFilesAsync()`
2. Single-file calls always return `GroupId = "group_001"`
3. `GroupId` is **NOT** a stable identifier

**FindMatchingExistingGroup Current Logic** (INCORRECT):
```csharp
// Tries to match by GroupId "group_001" - FAILS
if (_activeGroups.ContainsKey(newGroup.GroupId))  
    return _activeGroups[newGroup.GroupId];

// Falls back to timestamp tolerance of 1 second - TOO STRICT
return _activeGroups.Values
    .FirstOrDefault(g => 
        g.LineNumber == newGroup.LineNumber && 
        Math.Abs((g.Timestamp - newGroup.Timestamp).TotalSeconds) < 1.0);
```

When this fails to find a match, it accidentally returns the first group that happens to be within the time window.

### Stable Identifiers

According to `module_file_group_matcher.md`:

**Primary Stable Identifiers**:
1. **NormalFolder**: e.g., "C251201T140543_0" (unique per Normal folder)
2. **NirKey**: e.g., "20251201T140543" (unique per NIR file)

**Group Types**:
- **Normal-based group**: `NormalFolder` is PRIMARY identifier
- **NIR-only group**: `NirKey` is PRIMARY identifier (when `NormalFolder` is null)
- **Cam-only group**: No stable identifier (use timestamp + line number)

## 📝 Requirements

### Functional Requirements

#### FR-1: Match by Stable Identifiers
**Priority**: Critical

`FindMatchingExistingGroup` MUST match groups by stable identifiers in this order:

1. **If `newGroup.NormalFolder` is not null**:
   - Find existing group where `g.NormalFolder == newGroup.NormalFolder`
   - This handles Normal-based groups and updates to existing Normal groups

2. **If no match found and `newGroup.NirKey` is not null**:
   - Find existing group where `g.NirKey == newGroup.NirKey`
   - This handles NIR-only groups

3. **If no match found**:
   - Return null (create new group)
   - Do NOT use timestamp fallback matching

#### FR-2: Remove GroupId Matching
**Priority**: Critical

Remove the GroupId-based matching logic:
```csharp
// DELETE THIS - GroupId is not stable
if (_activeGroups.ContainsKey(newGroup.GroupId))
    return _activeGroups[newGroup.GroupId];
```

#### FR-3: Remove Timestamp Fallback
**Priority**: High

Remove the 1-second timestamp tolerance fallback:
```csharp
// DELETE THIS - Too error-prone
return _activeGroups.Values
    .FirstOrDefault(g => 
        g.LineNumber == newGroup.LineNumber && 
        Math.Abs((g.Timestamp - newGroup.Timestamp).TotalSeconds) < 1.0);
```

**Rationale**: 
- Timestamp matching is unreliable for distinguishing groups
- User's data shows groups 2-4 seconds apart
- FileGroupMatcher already handles timestamp-based NIR attachment internally

### Non-Functional Requirements

#### NFR-1: Logging
**Priority**: Medium

Add comprehensive logging to diagnose matching decisions:
- Log which identifier was used for matching (NormalFolder, NirKey, or none)
- Log when no match is found (new group will be created)
- Use `LogInformation` level for match results

#### NFR-2: Performance
**Priority**: Low

- Matching should complete in < 10ms
- Linear search through `_activeGroups` is acceptable (typically < 100 groups)

#### NFR-3: Backward Compatibility
**Priority**: High

- Existing groups must continue to work
- No changes to `FileGroup` model structure
- No changes to group creation in initial scan

## 🚫 Out of Scope

1. **Changing FileGroupMatcher logic**: The core grouping algorithm should NOT be modified
2. **Changing GroupId assignment**: GroupId remains sequential for display purposes
3. **Camera-only group matching**: These groups have no stable identifier; creating duplicates is acceptable
4. **Group merging**: Only update existing groups, do not merge multiple groups

## ✅ Acceptance Criteria

### AC-1: Correct Group Matching
**Given**: Initial scan created groups:
- `group_001`: NormalFolder="C251201T140543_0"
- `group_002`: NormalFolder="C251201T140555_0"
- `group_003`: NirKey="20251201T140615" (NIR-only)

**When**: New files arrive in real-time:
- File 1: Normal folder "C251201T140543_0" (matches group_001)
- File 2: NIR file "20251201T140615.spc" (matches group_003)
- File 3: Normal folder "C251201T140600_0" (NEW group)

**Then**:
- File 1 updates group_001 (matched by NormalFolder)
- File 2 updates group_003 (matched by NirKey)
- File 3 creates group_004 (new NormalFolder)

### AC-2: Logging Output
**Given**: File matching process

**When**: `FindMatchingExistingGroup` is called

**Then**: Logs must show:
```
[INFO] FindMatchingExistingGroup: Looking for NormalFolder="C251201T140543_0", NirKey=null
[INFO] Found match by NormalFolder: group_001
```
OR
```
[INFO] FindMatchingExistingGroup: Looking for NormalFolder=null, NirKey="20251201T140615"
[INFO] Found match by NirKey: group_003
```
OR
```
[INFO] FindMatchingExistingGroup: No matching group found, will create new group
```

### AC-3: No Regression
**Given**: Existing test data

**When**: Initial scan is performed

**Then**:
- All groups created correctly (no change from before)
- GroupIds assigned sequentially (group_001, group_002, ...)
- All files matched correctly

## 🔗 Related Documents

- [module_file_group_matcher.md](../../architecture/module_file_group_matcher.md) - FileGroupMatcher architecture
- [module_monitoring_orchestrator.md](../../architecture/module_monitoring_orchestrator.md) - MonitoringOrchestrator architecture
- [glossary.md](../../architecture/glossary.md) - Term definitions

## 📊 Success Metrics

1. **Zero false merges**: No files merged into wrong groups
2. **Correct group count**: Number of groups matches number of unique Normal folders + NIR-only + Cam-only
3. **User validation**: User confirms files are grouped correctly in real-time

## ❓ Open Questions

1. **Q**: What should happen if a Normal folder file arrives, but a NIR-only group with matching timestamp already exists?
   - **A**: Create separate groups. Normal folders and NIR are independent identifiers.

2. **Q**: Should we validate that NormalFolder/NirKey actually came from the correct file type?
   - **A**: No, FileGroupMatcher already ensures consistency.

3. **Q**: What if the same NormalFolder appears twice (duplicate events)?
   - **A**: Existing debouncing in MonitoringOrchestrator (`_processedFiles`) handles this.

---

**Document Review**: Ready for implementation
