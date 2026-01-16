---
Task: implement_group_reordering
Created: 2026-01-13
Status: Draft
Summary: Automatically reorder groups when new files should precede existing files based on delay settings
Research Required: Yes
---

# Group Reordering (Eviction) - Requirements

## 1. Goal

### 1.1 Primary Goal

When a new file cannot match an existing group due to delay constraints, and the new file's timestamp indicates it should precede an existing file, the system automatically reorders groups so the new file takes the earlier position.

### 1.2 Success Criteria

- [ ] When a new file (e.g., Cam1) arrives that should precede an existing file (e.g., Normal) based on delay settings, the new file takes the earlier group
- [ ] The displaced file (Normal) is moved to a new later group
- [ ] UI updates correctly to reflect the reordered groups
- [ ] Log messages clearly indicate when reordering/eviction occurs
- [ ] No data loss during reordering

## 2. Constraints

### 2.1 Technical Constraints

- Must work within existing `GroupManager` architecture
- Must not break existing matching logic
- Must handle concurrent file arrivals safely (thread-safety)
- Must update UI via existing `GroupCreated`/`GroupUpdated`/`GroupRemoved` events

### 2.2 Business Constraints

- Must maintain data integrity (no orphaned files)
- Reordering should be visually seamless to the user

### 2.3 Non-Goals (Out of Scope)

- Reordering based on user manual intervention
- Retroactive reordering of files that arrived long ago
- Cross-line reordering (Line 1 ↔ Line 2)

## 3. Questions to Investigate

- [ ] Q1: How to determine when a new file should precede (evict) an existing file?
  - Answer: Based on min/max delay settings - if new file's expected range doesn't include existing file, check timestamp ordering

## 4. Use Case (Concrete Example)

### 4.1 Key Concept: min as Origin Point

The `min` value acts as an **origin point** (기준선), not just a minimum:

| Setting | Meaning |
|---------|---------|
| min=0, max=5 | Files can arrive simultaneously (0s diff OK) |
| min=5, max=9 | Files must have at least 5s gap to be "same group" |

### 4.2 Scenario: min=5, max=9 (Normal → Cam1)

**Files Arrive**:
1. Normal (T=45) → `line1_022` created
2. Cam1 (T=48) arrives

**Matching Analysis**:
- Cam1 (T=48) expects Normal in range: `T=48-9 ~ T=48-5` = **T=39~43**
- Normal (T=45) is **outside** this range → No match
- Normal (T=45) expects Cam1 in range: `T=45+5 ~ T=45+9` = **T=50~54**
- Cam1 (T=48) is **outside** this range → No match

**Conclusion**: Different groups. But which file gets the earlier group?

**Ordering Logic**:
- Cam1 (T=48) with min=5 expects Normal at T=43 or earlier
- Normal (T=45) is "too late" relative to Cam1's expected sequence
- Therefore, **Cam1 logically precedes Normal** in group order

**Expected Result**:
```
line1_022: Cam1 (T=48)   ← Takes earlier group
line1_023: Normal (T=45) ← Evicted to later group
```

### 4.3 Why This Makes Sense

If a camera image expects data that should have arrived 5-9 seconds earlier:
- And we find "later" data (Normal at T=45 for Cam1 at T=48)
- The camera is likely part of an **earlier** capture sequence
- The "too late" Normal belongs to a **later** sequence

## 5. Assumptions

- Files generally arrive in approximate sequence order (delays are small)
- Reordering is a rare edge case, not the normal path
- A file can only be evicted once per reordering event (no infinite loops)

## 6. Dependencies

### 6.1 Blocked By

| Dependency | Status | Owner |
|------------|--------|-------|
| fix_sequence_time_validation | Complete | - |

### 6.2 Blocks

| Dependent Task | Impact if Delayed |
|----------------|-------------------|
| None currently | - |

---

## Approval

- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
- [ ] Scope boundaries are clear
- [ ] All blocking dependencies identified

**Next Step**: 02_research.md (Research the eviction algorithm)
