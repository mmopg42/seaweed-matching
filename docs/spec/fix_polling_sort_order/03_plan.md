---
Task: fix_polling_sort_order
Created: 2026-01-09
Status: Draft
Depends On: 01_requirements.md
---

# Fix Polling Sort Order - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification |
|-------------------|--------------|--------------|
| Files sorted by extracted timestamp | `OnPollTick` modification | Unit test + manual log verification |
| NIR with earlier timestamps processed first | Timestamp extraction + sorting | Manual test with mixed file types |
| Group IDs follow timestamp order | Correct processing order | Log analysis showing sequential IDs |
| Equal timestamps: sensors in priority order | Secondary sort by sensor priority | Manual test with same-second files |
| No regression in detection | Existing logic preserved | Run existing tests |

---

## 1. Architecture Overview

### 1.1 System Context

The polling mechanism in `FileWatcherService` discovers new files and fires events. Currently it sorts by filename (lexicographic), causing incorrect processing order. This change modifies only the sorting logic within `OnPollTick()`.

### 1.2 Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    FileWatcherService                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────┐      ┌─────────────────┐      ┌────────────┐  │
│  │ OnPollTick  │─────►│ SortByTimestamp │─────►│ HandleEvent│  │
│  │             │      │   (MODIFIED)    │      │            │  │
│  └─────────────┘      └─────────────────┘      └────────────┘  │
│        │                      │                                 │
│        ▼                      ▼                                 │
│  ┌─────────────┐      ┌─────────────────┐                      │
│  │Directory.Get│      │FileNamingHelper │                      │
│  │Files/Dirs   │      │.ExtractTimestamp│                      │
│  └─────────────┘      └─────────────────┘                      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.3 Data Flow

```
Poll Timer Tick
     │
     ▼
Collect new files/folders
     │
     ▼
[BEFORE] Sort by filename (lexicographic)
[AFTER]  Sort by extracted timestamp + sensor priority
     │
     ▼
Fire events in sorted order
     │
     ▼
GroupManager receives in correct order
```

---

## 2. Components

### 2.1 New Components

| Component | Type | Location | Purpose |
|-----------|------|----------|---------|
| `GetSensorPriority` | Method | `FileWatcherService.cs` | Return sensor order from DataSequenceSettings |
| `ExtractTimestampForSorting` | Method | `FileWatcherService.cs` | Auto-detect file type and extract timestamp |

### 2.2 Modified Components

| Component | Location | Changes | Breaking? |
|-----------|----------|---------|-----------|
| `OnPollTick` | `FileWatcherService.cs` | Change sort logic | No |

---

## 3. Interface Definitions

### 3.1 ExtractTimestampForSorting

```csharp
private DateTime? ExtractTimestampForSorting(string path)
```

**Responsibilities**:
- Auto-detect file type from path/filename pattern
- Call appropriate `FileNamingHelper` method
- Return `DateTime.MaxValue` if extraction fails (sorts to end)

**Does NOT**:
- Modify file state
- Determine sensor priority

### 3.2 GetSensorPriority

```csharp
private int GetSensorPriority(string path)
```

**Responsibilities**:
- Return priority based on file type: NIR=1, Normal=2, Camera=3
- Used as secondary sort key

**Returns**:
- `1`: NIR files (`run_*.txt` or `run_*.spc`)
- `2`: Normal folders (matches `C*T*_*` pattern)
- `3`: Camera files (`.bmp`, `.jpg`, `.png`)
- `99`: Unknown (sorts last)

---

## 4. Key Design Decisions

### 4.1 Auto-Detection vs Explicit FileType Parameter

**Context**: Need to determine file type for timestamp extraction during polling.

| Option | Pros | Cons |
|--------|------|------|
| Auto-detect from path | Simple, no changes to caller | Slight overhead |
| Pass FileType explicitly | Faster | Requires caller changes |

**Decision**: Auto-detect

**Rationale**: Polling already has the path; detection is cheap (regex match). Avoids coupling with external type detection.

### 4.2 Fallback for Failed Timestamp Extraction

**Context**: What if timestamp extraction fails?

| Option | Behavior |
|--------|----------|
| `DateTime.MinValue` | Sorts to beginning (risky) |
| `DateTime.MaxValue` | Sorts to end (safe) |
| Skip the file | May miss files |

**Decision**: `DateTime.MaxValue`

**Rationale**: Unknown files should be processed last, not first. Prevents them from disrupting known-order files.

---

## 5. Configuration

No new configuration required.

---

## 6. External Dependencies

No new dependencies.

---

## 7. Glossary Updates

### Existing Terms Check

| Checked | Existing Term | Relevance |
|---------|---------------|-----------|
| [x] | `FileWatcherService` | Modified component |
| [x] | `FileNamingHelper` | Used for timestamp extraction |
| [x] | `OnPollTick` | Modified method |

### New Terms to Add

None. Using existing patterns.

---

## 8. Architecture Documentation Plan

| Document | Action |
|----------|--------|
| `glossary.md` | No changes needed |

---

## 9. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Timestamp extraction fails for edge cases | Low | Med | Fallback to `DateTime.MaxValue` |
| Performance impact from regex on every file | Low | Low | Regexes are pre-compiled |

---

## 10. Open Questions

- [x] Use existing `FileNamingHelper` methods? → Yes
- [x] Add new helper method or inline? → New private methods in `FileWatcherService`

---

## Approval

- [x] All requirements traced to components
- [x] Component interfaces defined
- [x] Design decisions documented with rationale
- [x] Glossary terms identified
- [x] All open questions resolved

**Next Step**: 04_design.md
