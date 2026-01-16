---
Task: fix_general_cam_thumbnail_delay
Created: 2026-01-13
Status: Draft
Depends On: 01_requirements.md, 02_research.md
---

# Fix General Camera Thumbnail Delay - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| Thumbnails appear automatically when image file is created, even if delayed | `FileWatcherService` (Event Pass-through) + `MonitoringOrchestrator` (Renamed Handler) | Manual verification using simulation script (delayed creation) |
| "No Image" placeholders are replaced by actual thumbnails | `MonitoringOrchestrator` (Fast Capture Trigger) | Visual verification in Dashboard UI |
| No manual refresh is required | `MonitoringOrchestrator` (Event-driven update) | Observe auto-update without interaction |

## 1. Architecture Overview

### 1.1 System Context

The fix involves modifying the event flow between `FileWatcherService` (the source of file system events) and `MonitoringOrchestrator` (the consumer). We are removing a "hijacking" logic in `FileWatcherService` that was masking specific image events as folder events, and enhancing `MonitoringOrchestrator` to correctly handle `Renamed` events which are common during delayed file generation.

### 1.2 Data Flow

```
[File System]
    │
    ▼
[FileWatcherService]
    │ 1. Receives 'Renamed' event for 'stitched_original.png'
    │ 2. (Current: Rewrites to Folder Created -> REMOVE THIS)
    │ 3. (New: Passes 'Renamed' event as-is)
    │
    ▼
[MonitoringOrchestrator]
    │ 1. Receives raw 'Renamed' event
    │ 2. (New: Handle 'Renamed' case)
    │ 3. Detects IsStitchedImage(NewPath) == true
    │ 4. Triggers _imageCache.HandleStitchedImageCaptureAsync()
    │
    ▼
[GroupManager]
    │ 1. Updates existing group with new image path
    │
    ▼
[DashboardViewModel]
    │ 1. Receives GroupUpdated
    │ 2. Refreshes Thumbnail
```

## 2. Components

### 2.1 Modified Components

| Component | Location | Changes | Breaking Change? |
|-----------|----------|---------|------------------|
| `FileWatcherService` | `Core/FileWatching/FileWatcherService.cs` | Remove `stitched_original.png` -> Folder rewrite logic. | No |
| `MonitoringOrchestrator` | `Core/FileWatching/MonitoringOrchestrator.cs` | Add `Renamed` case to event switch. | No |

## 3. Interface Definitions

### 3.1 FileWatcherService

```csharp
class FileWatcherService : IFileWatcher
{
    // No public interface changes.
    // Logic change: HandleEvent() will no longer modify 'stitched_original.png' events.
}
```

### 3.2 MonitoringOrchestrator

```csharp
class MonitoringOrchestrator : IMonitoringOrchestrator
{
    // No public interface changes.
    // Logic change: ProcessSingleEventAsync() will handle WatcherChangeTypes.Renamed.
}
```

## 4. Key Design Decisions

### 4.1 Removing Event Rewrite Logic

**Context**: `FileWatcherService` currently converts `stitched_original.png` events into `Created` events for the parent folder.
**Decision**: Remove this logic entirely.
**Rationale**: This logic masks the actual image arrival from the Orchestrator, preventing "Fast Capture" optimization and confusing the Orchestrator into thinking it's a duplicate folder event.

### 4.2 Handling Renamed Events

**Context**: `MonitoringOrchestrator` ignores `Renamed` events.
**Decision**: Treat `Renamed` similar to `Created`/`Changed` for the new path.
**Rationale**: `IsStitchedImage` check inside Orchestrator will identify if the renamed file is the target image. We don't need to explicitly "process the old path" (Delete) because usually the old path was a temp file we didn't track anyway. Even if it was tracked, `GroupManager.CreateOrUpdateGroupAsync` will handle the update.

## 5. Configuration

No configuration changes required.

## 6. External Dependencies

None.

## 7. Glossary Updates

No glossary updates required.

## 8. Architecture Documentation Plan

No architecture documentation updates required beyond standard code comments.

## 9. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Removing Folder Event rewrite might break some other flow? | Low | Medium | The folder event itself (when folder is created) is distinct. The rewrite was specifically for the image file. The folder logic should still work for the initial folder creation. |
| Duplicate Rows | Low | High | Confirmed in Research that Orchestrator normalizes image paths to Folder Key, preventing duplicates. |

## 10. Open Questions

- [x] Will duplicate rows appear? -> No (verified in Research).

---

## Approval

- [x] All requirements traced to components
- [x] Component interfaces defined
- [x] Design decisions documented with rationale
- [x] All open questions resolved

**Next Step**: 04_design.md
