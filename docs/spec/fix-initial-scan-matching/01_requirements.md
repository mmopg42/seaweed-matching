# Fix Initial Scan & Real-time Scan Matching - Requirements

---
Task: Fix Initial Scan & Real-time Scan Matching
Created: 2025-12-15
Status: Draft
---

## 1. Problem Statement

### Current Issues

**Issue 1: Initial Scan Doesn't Use Match 3**
- When Start button is pressed, `PerformInitialScanAsync()` scans all existing files
- Files are grouped by `FileGroupMatcher.MatchGroups()` which does NOT call `FindMatchingExistingGroup()`
- **Result**: NIR and Camera files create separate groups instead of matching by timestamp

**Issue 2: Real-time Scan Not Working**
- After initial scan, `FileWatcher` should detect new files
- New files should trigger `CreateOrUpdateGroupAsync()` → `FindMatchingExistingGroup()` → Match 3
- **Current behavior**: No real-time detection logs appear

### Expected Behavior

```
User presses Start:
1. Initial Scan (existing files)
   → Use Match 3 to group files by timestamp
   → NIR + Camera files with similar timestamps = same group
   
2. Real-time Scan (new files)
   → FileWatcher detects new files
   → Use Match 3 to match with existing groups
   → Continue grouping by timestamp
```

## 2. Success Criteria

### Initial Scan
- [ ] NIR file at T=10:00:00 + Camera at T=10:00:01 → **Same group** (matched via Match 3)
- [ ] All existing files processed using DataSequenceSettings order and tolerances
- [ ] Logs show: `FindMatchingExistingGroup`, `Match 3: Searching for timestamp match`

### Real-time Scan
- [ ] After Start, new files trigger `FileWatcher` events
- [ ] Logs show: `File detected: ...`, `FindMatchingExistingGroup`, `Match 3: ...`
- [ ] New NIR file matches with existing Camera group (or vice versa)

## 3. Technical Requirements

### 3.1 Initial Scan Architecture

**New Flow**:
```csharp
PerformInitialScanAsync():
1. Get DataSequenceSettings.GetOrderedTypes()
2. For each dataType in order (e.g., NIR → Normal → Cam1...):
   a. Scan files of that type
   b. For FIRST type: CreateNewGroupAsync() (no matching needed)
   c. For LATER types: CreateOrUpdateGroupAsync() (uses Match 3)
3. StartAsync() FileWatcher for real-time detection
```

**Key Changes**:
- Replace `FileGroupMatcher.MatchGroups()` with sequential `CreateOrUpdateGroupAsync()`
- Process files in DataSequenceSettings priority order
- Use same matching logic as real-time (consistency)

### 3.2 Real-time Scan Verification

**Ensure FileWatcher is Active**:
```csharp
StartAsync():
1. PerformInitialScanAsync()
2. _fileWatcher.StartAsync() ← Must be called!
3. Subscribe to FileWatcher events
```

**Event Flow**:
```
FileWatcher detects new file
→ OnFileChanged event
→ CreateOrUpdateGroupAsync(filePath)
→ FindMatchingExistingGroup() 
→ Match 3 logic
→ Merge or Create group
```

### 3.3 Logging Requirements

**Initial Scan Logs**:
```
INFO: Starting initial scan
DEBUG: Processing NIR files (priority 1)
DEBUG: Processing Normal files (priority 2)
DEBUG: Match 3: Searching for timestamp match...
INFO: Initial scan complete: X groups created, Y files matched
```

**Real-time Scan Logs**:
```
INFO: File detected: Z:\path\to\file.txt
DEBUG: FindMatchingExistingGroup: NormalFolder=null, NirKey=xxx
DEBUG: Match 3: Searching for timestamp match...
INFO: Match 3: Found timestamp match! GroupId=group_010
```

## 4. Implementation Phases

### Phase 1: Fix Initial Scan (Priority 1)
- [ ] Modify `PerformInitialScanAsync` to use sequential matching
- [ ] Process files by DataSequenceSettings order
- [ ] Call `CreateOrUpdateGroupAsync` for each file (except first type)
- [ ] Add diagnostic logging

### Phase 2: Verify Real-time Scan (Priority 2)
- [ ] Confirm `FileWatcher.StartAsync()` is called after initial scan
- [ ] Verify event handlers are subscribed
- [ ] Test file detection with new files
- [ ] Ensure `CreateOrUpdateGroupAsync` is triggered

### Phase 3: Testing
- [ ] Test initial scan with NIR + Camera files (same timestamp)
- [ ] Test real-time: Add NIR, wait, add Camera → should match
- [ ] Test real-time: Add Camera, wait, add NIR → should match (if tolerance allows)
- [ ] Verify logs show Match 3 in both modes

## 5. Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Initial scan: No files | Skip scan, start FileWatcher |
| Initial scan: Only NIR files | Create groups, wait for Camera files in real-time |
| Real-time: File arrives before tolerance window | Create new group |
| Real-time: File deleted immediately | Handle gracefully (log warning) |

## 6. Validation

**Before Fix**:
- Initial scan: Separate groups for NIR and Camera
- Real-time: No logs when new file added

**After Fix**:
- Initial scan: NIR + Camera in same group (if timestamps match)
- Real-time: Logs show file detection and Match 3 execution

---

**Status**: Ready for design
**Next Step**: Create detailed design document
