---
Task: Log and Matching Analysis
Created: 2026-01-08
Status: Draft
Depends On: 01_requirements.md
---

# Log and Matching Analysis - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: Why "Camera" generic log? | `DetermineDataTypeForGroup` misses `Cam4-6`, defaulting to `DataType.Camera`. | High |
| Q2: Why `group_001` skipped? | `group_001` already had a camera. Due to Bug 1, it was treated as "Camera" type occupied. | High |
| Q3: Why no failure log? | skipped silently by `if (HasDataType(...)) continue;` logic. | High |

## 2. Detailed Findings

### 2.1 Q1: Log Ambiguity (Generic "Camera" Label)

**Findings**:
- `DetermineDataTypeForGroup` only checks `cam1`, `cam2`, `cam3`.
- `cam4`, `cam5`, `cam6` fall through to `DataType.Camera`.
- `GetFriendlyColumnName` maps `DataType.Camera` -> "Camera".
- This marks all Line 2 cameras (Cam4-6) as generic "Camera".

### 2.2 Q2 & Q3: Skipping `group_001` and Missing Log

**Scenario**:
1. `group_001` (Line 2) is created.
2. `084.bmp` (likely Cam4) is assigned to `group_001`. 
   - Because of Bug 1, it is typed as `DataType.Camera`.
3. `299.bmp` (likely Cam5, which user calls "Cam2" of Line 2) arrives.
   - Because of Bug 1, it is ALSO typed as `DataType.Camera`.
4. Matching Logic for `299.bmp`:
   - Checks `group_001`.
   - Calls `HasDataType(group_001, DataType.Camera)`.
   - `group_001` has `084.bmp`. `HasDataType` returns `true` (slot occupied).
   - Loop `continue`s (Line 342). **Silent Skip**.
5. Result:
   - `group_001` is skipped without log.
   - Matches `group_002` (empty).

**Root Cause**:
- The failure to distinguish `Cam4`, `Cam5`, `Cam6` causes them to compete for the single "Camera" slot in a group.
- This is a direct consequence of Bug 1.

## 3. Code Analysis

### 3.1 Relevant Code
`GroupManager.cs` - `FindMatchingExistingGroup`:
```csharp
foreach (var candidate in snapshot...) {
    if (HasDataType(candidate, newGroupType)) continue; // Silent Skip
    // ...
}
```
`DetermineDataTypeForGroup`:
```csharp
if (group.CameraFiles.ContainsKey("cam1")) ...
// Missing Cam4-6
return DataType.Camera;
```

## 4. Recommendations

### Primary Recommendation
**Fix only `DetermineDataTypeForGroup`**. 
- Correctly detecting `Cam4-6` will solve ALL issues:
    - Logs will show "Cam4", "Cam5".
    - `group_001` will show `HasDataType(Cam5) = False` (even if it has Cam4).
    - Matching will proceed to checks, allowing correct assignment or (if delays exceed) proper failure logging.

### Verification Plan
1. Apply fix to `DetermineDataTypeForGroup`.
2. Run simulation with Cam4 and Cam5 files.
3. Verify `group_001` accepts both Cam4 and Cam5 (Line 2 Normal).
4. Verify logs show specific Camera names.

## 5. Unanswered Questions
- None.

---

## Approval
- [x] All questions answered.
- [x] Evidence provided (Code path analysis).
