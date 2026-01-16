---
Task: fix_sequence_time_validation
Created: 2026-01-13
Status: Draft
Depends On: (Bug Report)
---

# Fix Data Sequence Time Validation Bug - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: Why does Normal not check time against existing Cam1? | Matching logic only compares against the **predecessor type** in the sequence, ignoring other files already in the group. | High |
| Q2: Is groupTimestamp correctly used? | Partially - it's set by the **first** file that creates the group, which may not be NIR. | High |
| Q3: Is there validation against all existing files in a group? | No - only predecessor type is checked. | High |

## 2. Bug Scenario (User Report)

**Expected Sequence**: NIR → Normal → Cam1 → Cam3 → Cam2

**Anomaly Scenario**:
1. Cam1 arrives **before** Normal due to data error
2. Cam1 creates a new group `line1_022` (no NIR match found)
3. Normal arrives with timestamp close to NIR
4. Normal **matches** `line1_022` because:
   - It checks time against NIR (which doesn't exist in the group)
   - It falls back to `group.Timestamp` (which is Cam1's timestamp)
   - Cam1-Normal time difference may still fall within tolerance
5. **Result**: Normal is incorrectly matched to a Cam1-initiated group instead of creating its own group

**Real Problem**: Normal should **also** verify that its timestamp is **before** Cam1's timestamp (since Normal should arrive before Cam1 in the sequence).

## 3. Root Cause Analysis

### 3.1 Relevant Code: [FindMatchingExistingGroup](file:///C:/workspace/seaweed/gui_kiro_v2/ChronoView/Core/FileWatching/GroupManager.cs#L351-L522)

**Lines 426-470 (Backward Matching)**:
```csharp
// Backward Matching (Checking against Predecessor)
else if (predecessorType != null)
{
    var predType = predecessorType.Value;
    var minDelay = config.DataSequenceSettings.GetMinDelay(normalizedType);
    var maxDelay = config.DataSequenceSettings.GetMaxDelay(normalizedType);

    foreach (var candidate in FilterByLine(snapshot, newGroup.LineNumber))
    {
        // ...
        DateTime comparisonTimestamp = candidate.Timestamp; // Default to group anchor
        
        bool hasPred;
        if (predType == DataType.NIR)
        {
            hasPred = true; // Relax NIR check
            if (candidate.HasNir) comparisonTimestamp = candidate.Timestamp; 
        }
        else
        {
            hasPred = HasDataType(candidate, predType);
            if (hasPred)
            {
                var predTimestamp = GetTimestampForDataType(candidate, predType);
                if (predTimestamp.HasValue) comparisonTimestamp = predTimestamp.Value;
            }
        }

        var timeDiff = (newGroup.Timestamp - comparisonTimestamp).TotalSeconds;
        // ...
    }
}
```

### 3.2 The Bug

| Aspect | Current Behavior | Expected Behavior |
|--------|------------------|-------------------|
| **Time check target** | Only checks predecessor type (e.g., NIR for Normal) | Should also validate against **all existing files** that come **after** in sequence |
| **Successor validation** | None | If Cam1 exists, Normal's timestamp must be **before** Cam1's |
| **Group anchor timestamp** | Set by first file creating the group | Should be set by the **earliest-order** file in the sequence |

### 3.3 Concrete Example

**Group `line1_022` state**:
- Cam1: `155913` (timestamp)
- NIR: ❌ (missing)
- Normal: ❌ (missing)

**Incoming Normal file**: `155910` (timestamp)

**Current logic**:
1. predecessor = NIR (Order 1)
2. `hasPred = true` (relaxed NIR check)
3. `comparisonTimestamp = group.Timestamp` = `155913` (Cam1's timestamp)
4. `timeDiff = 155910 - 155913 = -3s`
5. `-3s` is negative, but `skipOrdering = true` (because NIR is relaxed)
6. **Match happens** even though Normal should be before Cam1!

## 4. Impact Analysis

| Component | Impact | Risk |
|-----------|--------|------|
| `GroupManager.FindMatchingExistingGroup` | Core logic change | Medium |
| `FileGroup` model | May need per-file timestamps | Low |
| UI display | Groups may have wrong data | Low (fixed after correction) |

## 5. Solution Options

### Option A: Validate Against All Successor Files with Min/Max Delay (Recommended)

**Description**: When matching a new file, also check that its timestamp is **before** all files that come after it in the sequence, **AND** the time difference is within the successor's min/max delay range.

**Pseudocode**:
```csharp
// After candidate validation against predecessor
// NEW: Validate against successors already in the group
foreach (var successorType in orderedTypes.Where(t => GetPriority(t, config) > newGroupOrder))
{
    if (HasDataType(candidate, successorType))
    {
        var successorTimestamp = GetTimestampForDataType(candidate, successorType);
        if (successorTimestamp.HasValue)
        {
            // Get successor's min/max delay settings
            var normalizedSuccessor = NormalizeForSequence(successorType);
            var minDelay = config.DataSequenceSettings.GetMinDelay(normalizedSuccessor);
            var maxDelay = config.DataSequenceSettings.GetMaxDelay(normalizedSuccessor);
            
            // Time difference: successor.Timestamp - newFile.Timestamp
            // (should be positive and within range)
            var timeDiff = (successorTimestamp.Value - newGroup.Timestamp).TotalSeconds;
            
            if (timeDiff < minDelay || timeDiff > maxDelay)
            {
                // Reject: time difference is out of range
                RaiseLog($"[매칭제외] {colName}: {fileName} -> {candidate.GroupId} 제외 " +
                         $"(사유: {successorType}과 시차 범위 초과, 시차: {timeDiff:F1}s, 허용: {minDelay}-{maxDelay}s)", ...);
                continue; // Skip this candidate
            }
        }
    }
}
```

**Validation Logic**:
| Scenario | Check | Outcome |
|----------|-------|---------|
| Normal (T=10) vs Cam1 (T=13), Cam1.maxDelay=5s | 13-10=3s ≤ 5s | ✅ Match |
| Normal (T=10) vs Cam1 (T=25), Cam1.maxDelay=5s | 25-10=15s > 5s | ❌ Reject |
| Normal (T=10) vs Cam1 (T=8), Cam1.minDelay=0s | 8-10=-2s < 0s | ❌ Reject (시간 역전) |

**Pros**:
- Fixes the bug completely
- Uses existing DataSequenceSettings
- Minimal code change

**Cons**:
- Adds complexity to matching loop

**Effort**: Small

### Option B: Store Per-File Timestamps in FileGroup

**Description**: Instead of a single `Timestamp` field, store `Dictionary<DataType, DateTime>` for each file.

**Pros**:
- More accurate time tracking
- Enables complex sequence validation

**Cons**:
- Model change required
- Migration needed for existing data
- Higher effort

**Effort**: Medium

---

### Comparison Matrix

| Criteria | Weight | Option A | Option B |
|----------|--------|----------|----------|
| Fixes bug | 5 | 5 | 5 |
| Implementation effort | 4 | 5 | 2 |
| Model stability | 3 | 5 | 2 |
| **Weighted Total** | | 60 | 36 |

## 6. Recommendation

**Adopt Option A**: Add successor validation in `FindMatchingExistingGroup`.

**Implementation**:
1. After checking predecessor time, iterate through successor types
2. For each successor that exists in the candidate group, verify `newFile.Timestamp < successor.Timestamp`
3. If violated, reject the candidate with a clear log message

## 7. References

- [GroupManager.FindMatchingExistingGroup](file:///C:/workspace/seaweed/gui_kiro_v2/ChronoView/Core/FileWatching/GroupManager.cs#L351-L522)
- [GroupManager.GetTimestampForDataType](file:///C:/workspace/seaweed/gui_kiro_v2/ChronoView/Core/FileWatching/GroupManager.cs#L692-L727)
- [GroupManager.GetPriority](file:///C:/workspace/seaweed/gui_kiro_v2/ChronoView/Core/FileWatching/GroupManager.cs#L586-L589)

---

## Approval

- [x] All questions from requirements addressed
- [x] Evidence provided for conclusions
- [x] Recommendations are actionable
- [x] Risks identified

**Next Step**: 03_plan.md (Awaiting approval)
