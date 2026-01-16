---
Task: fix_sequence_time_validation
Created: 2026-01-13
Status: Draft
Depends On: 03_plan.md
---

# Fix Data Sequence Time Validation Bug - Detailed Design

## 1. Component Designs

### 1.1 GroupManager

> Updates `GroupManager` to validate timestamp order against all existing files in a group, ensuring correct data sequencing.

#### Interface (Existing)

```csharp
private FileGroup? FindMatchingExistingGroup(FileGroup newGroup, ApplicationConfiguration config)
```

#### Preconditions

- `newGroup` contains a valid Timestamp.
- `_activeGroups` contains potential candidate groups.
- `config.DataSequenceSettings` is loaded and valid.

#### Postconditions

- Returns a matching `FileGroup` ONLY if:
  1. It matches the sequence order logic (Predecessor/Successor rules).
  2. Time difference is within configured tolerances.
  3. **New**: Timestamp is logically ordered (Predecessor < New < Successor).
- Returns `null` if no valid match is found (forcing new group creation).

#### Detailed Logic

```pseudo
function FindMatchingExistingGroup(newGroup, config):
    // ... existing logic ...

    // Match 3: By Timestamp + LineNumber + Priority
    if newGroup.Timestamp is set and config has SequenceSettings:
        // ... type determination logic ...
        
        candidates = []
        
        // Backward Matching (Checking against Predecessor)
        if predecessorType exists:
             foreach candidate in FilterByLine(activeGroups, newGroup.Line).Where(NoSameType):
                 
                 // [Existing Logic: Predecessor Check]
                 check predecessor logic...
                 
                 // [NEW LOGIC: Successor Validation]
                 isSuccessorOrderValid = true
                 
                 // Iterate over all types that should come AFTER the newGroup
                 foreach succType in orderedTypes where Order(succType) > Order(newGroupType):
                     
                     if HasDataType(candidate, succType):
                         succTimestamp = GetTimestamp(candidate, succType)
                         if succTimestamp is valid:
                             
                             // 1. Calculate time difference
                             // succTimestamp SHOULD BE > newGroup.Timestamp
                             timeDiff = succTimestamp - newGroup.Timestamp
                             
                             // 2. Get Configured Delays for the Successor
                             minDelay = config.GetMinDelay(succType)
                             maxDelay = config.GetMaxDelay(succType)
                             
                             // 3. Validate Range
                             if timeDiff < minDelay or timeDiff > maxDelay:
                                 Log("Rejecting candidate {candidate.GroupId}: Time violation with Successor {succType}. Diff={timeDiff}s, Allowed={minDelay}-{maxDelay}s")
                                 isSuccessorOrderValid = false
                                 break // Stop checking other successors
                 
                 if isSuccessorOrderValid:
                     // Add to valid candidates list
                     candidates.Add(candidate)
                 else:
                     // Skip this candidate
                     continue

        // ... selection logic ...
```

#### Error Handling

| Error | Detection | Handling | Recovery |
|-------|-----------|----------|----------|
| Timestamp Missing | `GetTimestamp` returns null | Skip validation for that specific successor | Continue validation (permissive) |
| Invalid Config | `GetMin/MaxDelay` returns default | Use default (0-Infinite or 0-0) | Log warning |

---

## 2. Edge Cases & Boundary Conditions

| Case | Input | Expected Behavior | Implementation |
|------|-------|-------------------|----------------|
| **Normal Before Cam1** | Normal(T=10), Cam1(T=13), Max=5s | **Match** (Diff 3s <= 5s) | `timeDiff (3) <= max (5)` |
| **Normal After Cam1** | Normal(T=15), Cam1(T=10) | **Reject** (Diff -5s) | `timeDiff (-5) < min (0)` |
| **Normal Way Before** | Normal(T=5), Cam1(T=20), Max=5s | **Reject** (Diff 15s) | `timeDiff (15) > max (5)` |
| **Missing Successor** | Cam1 not in group yet | **Match** (Only Predecessor Checked) | Loop over successors finds nothing, valid = true |
| **Multiple Successors** | Group has Cam1(T=12) and Cam2(T=15) | **Match** if both valid | Loop checks both Cam1 and Cam2 |

---

## 3. Testing Strategy

### 3.1 Unit Test Cases

| Test Name | Setup | Input | Expected |
|-----------|-------|-------|----------|
| `Test_BackwardMatch_SuccessorValidation_Valid` | Group has Cam1 (T=100) | Normal (T=98) | **Matches** (Diff 2s < 5s) |
| `Test_BackwardMatch_SuccessorValidation_ReverseOrder` | Group has Cam1 (T=100) | Normal (T=102) | **Reject** (Diff -2s < 0) |
| `Test_BackwardMatch_SuccessorValidation_TooFar` | Group has Cam1 (T=100) | Normal (T=90) | **Reject** (Diff 10s > 5s) |
| `Test_BackwardMatch_MultipleSuccessors_Mixed` | Group has Cam1(T=100), Cam2(T=105) | Normal(T=98) | **Matches** (Checks both) |

---

## 4. Open Questions

- [x] All design questions resolved

---

## Approval

- [x] All components have detailed pseudo-code
- [x] Error handling specified for all failure modes
- [x] Edge cases covered
- [x] Test cases defined

**Next Step**: 05_tasks.md
