---
Task: improve_normal_folder_polling
Created: 2026-01-08
Status: Draft
Depends On: 01_requirements.md
---

# Improve Normal Folder Polling - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| Empty Normal folders skipped in `_knownFiles` | `FileWatcherService` (Existing) | Manual test: Create empty folder, observe debug logs loop |
| Delayed images detected | `FileWatcherService` (Existing) | Manual test: Add image after delay, verify detection |
| Polling interval = 500ms | `ApplicationConfiguration` default value | Verify `appsettings.json` or code default |
| No duplicate groups | `GroupManager.processedFiles` (existing) | Monitor "Group Created" logs |

---

## 1. Architecture Overview

### 1.1 System Context

The `FileWatcherService` uses a hybrid approach (FSW + Polling). The polling component currently filters out any path present in `_knownFiles`. By selectively NOT adding empty Normal folders to `_knownFiles`, we force the poller to "rediscover" them in every cycle until the required image `stitched_original.png` appears.

### 1.2 Data Flow

```
[Polling Loop (0.5s)]
    │
    ▼
scans directory
    │
    ▼
found "NormalFolder/"
    │
    ├── Has "stitched_original.png"?
    │   ├── YES ──► Add to _knownFiles ──► Fire Event "Created" (Once)
    │   └── NO  ──► SKIP _knownFiles   ──► Fire Event "Created" (Repeatedly)
    │
    ▼
[MonitoringOrchestrator]
    │
    ▼
[GroupManager]
    │
    ├── Is path in _processedFiles?
    │   ├── YES ──► Return Null (Ignore)
    │   └── NO  ──► Create Group, Add to _processedFiles
```

---

## 2. Components

### 2.1 Modified Components

| Component | Location | Changes |
|-----------|----------|---------|
| `ApplicationConfiguration` | `ChronoView/Models/ApplicationConfiguration.cs` | Change default `PollingIntervalMs` from 2000 to 500 |

### 2.2 Verified Components

| Component | Location | Status |
|-----------|----------|--------|
| `FileWatcherService` | `ChronoView/Core/FileWatching/FileWatcherService.cs` | Logic to skip `_knownFiles` for empty Normal folders is ALREADY IMPLEMENTED. Will be verified. |

---

## 3. Interface Definitions

No public interface changes. Internal logic changes only.

---

## 4. Key Design Decisions

### 4.1 Skipping _knownFiles for Empty Folders

**Context**: Need to re-evaluate folder status without complex state management.
**Decision**: Use `_knownFiles` exclusion.
**Rationale**: Simpler than maintaining a separate "pending" list and dedicated re-check logic. Leverages existing polling loop. Trade-off is increased debug log noise (acceptable).

### 4.2 Polling Interval 500ms

**Context**: User requested faster response (previous request for 0.1s rejected due to high load risk).
**Decision**: Set to 500ms.
**Rationale**:
- **Balance**: 500ms is 4x faster than current 2000ms.
- **Safety**: 100ms (0.1s) risks CPU spikes with large file counts (O(N) operation). 500ms allows sufficient time for `Directory.GetFiles` to complete even with thousands of files.
- **Goal**: Minimize the "perceived delay" after the 40s wait. A 0.5s max additional delay is negligible compared to 40s.

---

## 5. Configuration

| Key | Type | New Default | Description |
|-----|------|-------------|-------------|
| `WorkflowSettings.PollingIntervalMs` | int | 500 | Polling interval in milliseconds |

---

## 6. Glossary Updates

No new terms.



## 8. Open Questions

- [x] Is 500ms safe? -> Yes, analyzed in previous steps.
