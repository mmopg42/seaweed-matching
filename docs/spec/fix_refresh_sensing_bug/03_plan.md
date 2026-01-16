---
Task: fix_refresh_sensing_bug
Created: 2026-01-13
Status: Draft
Depends On: 01_requirements.md, 02_research.md
---

# Fix Refresh Sensing Bug - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| Refresh ALWAYS stops watcher | `MonitoringOrchestrator.RefreshAsync` | Check logs/debug: ensure `StartWatchingAsync` is NEVER called and `_isMonitoring` becomes false |
| `IsMonitoring` UI status updates to Stopped | `SystemControlViewModel.RefreshMonitoringAsync` | Manual test: UI button should switch to "START" after refresh |
| No regression in Deep Reset logic | `MonitoringOrchestrator.CoreResetAndScanAsync` | Verify `ResetState`, `Clear`, `Reset` are still called |

## 1. Architecture Overview

### 1.1 System Context

The `MonitoringOrchestrator` coordinates background file watching and UI data synchronization. Currently, the `Refresh` operation is designed as a "Deep Reset" that unconditionally restarts all services, including the `FileWatcher`. This plan modifies that behavior to respect the current monitoring state.

### 1.2 Data Flow

```
[User Clicks Refresh]
    │
    ▼
[SystemControlViewModel.RefreshMonitoringAsync]
    │
    ▼
[MonitoringOrchestrator.RefreshAsync]
    │
    ▼
[CoreResetAndScanAsync] (Clears and Rescans)
    │
    ▼
[End]
    (Monitoring is OFF, Orchestrator state updated to false)
```

## 2. Components

### 2.2 Modified Components

| Component | Location | Changes | Breaking Change? |
|-----------|----------|---------|------------------|
| `MonitoringOrchestrator` | `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs` | Modify `RefreshAsync` to NEVER call `StartWatchingAsync` and explicit set monitoring to false. | No |
| `SystemControlViewModel` | `ChronoView/UI/ViewModels/SystemControlViewModel.cs` | Update `RefreshMonitoringAsync` to use `RefreshAsync` for both states instead of calling `StartAsync`. | No |

## 3. Interface Definitions

### 3.1 MonitoringOrchestrator

```csharp
public class MonitoringOrchestrator : IMonitoringOrchestrator
{
    // No signature changes needed, only internal logic modification.
    public async Task RefreshAsync(CancellationToken cancellationToken = default);
}
```

## 4. Key Design Decisions

### 4.1 Internal State vs. External Parameter

**Context**: Should `RefreshAsync` take a parameter `bool startWatcher` or use its internal `_isMonitoring` field?

| Option | Pros | Cons |
|--------|------|------|
| Use `_isMonitoring` | Encapsulated, no interface change | Hidden state dependency |
| Add Parameter | Explicit, flexible | Breaking interface change |

**Decision**: Use internal `_isMonitoring` field and modify `SystemControlViewModel` call sites.

**Rationale**: `_isMonitoring` is the source of truth for whether the system *should* be watching. `RefreshAsync` is intended to restore the system to its "desired current state" after a reset.

---

### 4.2 SystemControlViewModel.RefreshMonitoringAsync Logic

**Context**: Currently it calls `StartAsync` if `!IsMonitoring`.

**Decision**: Change it to ALWAYS call `orchestrator.RefreshAsync()`.

**Rationale**: `RefreshAsync` already handles the full configuration reload and scan. `StartAsync` is meant for the initial activation. By unifying on `RefreshAsync`, we ensure that the orchestrator's internal logic for "conditional start" is the single point of control.

## 5. Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Configuration not applied if `StartAsync` isn't called | Low | Med | Ensure `RefreshAsync` reloads config properly (it already does). |
| Race condition during refresh | Low | High | Orchestrator uses locks and sequential task processing. |

## 6. Open Questions

- [x] Does `RefreshAsync` currently reload config from disk? → Yes, line 382 in `MonitoringOrchestrator.cs`.
- [x] Does `RefreshAsync` clear caches properly? → Yes, calls `CoreResetAndScanAsync` which clears `_imageCache`, `_abnormalDetector`, etc.

---

## Approval

- [ ] All requirements traced to components
- [ ] Component interfaces defined
- [ ] Design decisions documented with rationale
- [ ] Glossary terms identified
- [ ] All open questions resolved

**Next Step**: 04_design.md
