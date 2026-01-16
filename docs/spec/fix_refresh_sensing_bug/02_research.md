---
Task: fix_refresh_sensing_bug
Created: 2026-01-13
Status: Draft
Depends On: 01_requirements.md
---

# Fix Refresh Sensing Bug - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: Why does `Refresh` start monitoring? | `MonitoringOrchestrator.RefreshAsync` unconditionally restarts watcher if paths exist. | High |
| Q2: Does ViewModel contribute to this? | Yes, `RefreshMonitoringAsync` explicitly calls `StartAsync` if monitoring is OFF. | High |
| Q3: Is "Deep Reset" behavior preserved without watcher start? | Yes, `CoreResetAndScanAsync` handles state clearing / scanning independently. | High |

## 2. Detailed Findings

### 2.1 Q1: Why does `Refresh` start monitoring even when OFF?

**Method**: Analyzed `MonitoringOrchestrator.cs`.

**Findings**:
- `RefreshAsync` implementation (Lines 368-443):
  1. Stops existing services.
  2. Reloads config.
  3. Calls `CoreResetAndScanAsync` (clears state, performs scan).
  4. **CRITICAL**: Checks `if (watchPaths.Count > 0)` and IMMEDIATELY calls `_fileWatcher.StartWatchingAsync` (Line 412).
  - There is NO check for `_isMonitoring` or any other state flag before starting the watcher.
  - This effectively turns "Refresh" into "Restart Monitoring" regardless of previous state.

**Evidence**:
`MonitoringOrchestrator.cs` lines 408-412:
```csharp
var watchPaths = GetWatchPaths(_currentConfig).ToList();
if (watchPaths.Count > 0)
{
    _fileWatcher.FileChanged += OnFileChanged;
    await _fileWatcher.StartWatchingAsync(...); // Unconditional start
}
```

**Conclusion**: The orchestrator's `RefreshAsync` logic implementation assumes a refresh implies active monitoring, which is incorrect for the "Data Only Refresh" use case.

---

### 2.2 Q2: Does the ViewModel logic assume `Refresh` == `Start`?

**Method**: Analyzed `SystemControlViewModel.cs`.

**Findings**:
- `RefreshMonitoringAsync` (Line 116):
```csharp
if (IsMonitoring)
{
    await _orchestrator.RefreshAsync();
}
else
{
    // Start monitoring with fresh config - this ensures new settings are applied
    await _orchestrator.StartAsync(config);
}
```
- If monitoring is OFF (`else` block), it explicitly calls `StartAsync`.
- `StartAsync` sets `_isMonitoring = true` and starts the watcher.

**Conclusion**: The ViewModel intentionally forces monitoring ON during a refresh if it was OFF, likely to ensure configuration is applied. However, since `RefreshAsync` also loads configuration, this explicit start is unnecessary if `RefreshAsync` is fixed to handle the "OFF" state correctly.

## 3. Code Analysis

### 3.1 Relevant Existing Code

| File | Component | Relevance | Notes |
|------|-----------|-----------|-------|
| `MonitoringOrchestrator.cs` | `RefreshAsync` | Core logic flaw | Unconditionally starts watcher |
| `SystemControlViewModel.cs` | `RefreshMonitoringAsync` | Caller logic flaw | Forces StartAsync when OFF |

### 3.2 Impact Analysis

| Existing Component | Potential Impact | Risk Level |
|--------------------|------------------|------------|
| `MonitoringOrchestrator` | Changing `RefreshAsync` to be conditional might break "Deep Reset" if not careful | Low (Logic is sequential) |
| `SystemControlViewModel` | Removing `StartAsync` call might leave config stale if `RefreshAsync` implementation is buggy | Low (RefreshAsync loads config) |

## 4. Options Analysis

### Option A: Fix Orchestrator & Simplify ViewModel (Recommended)

**Description**: Modify `MonitoringOrchestrator.RefreshAsync` to check `_isMonitoring` before starting watcher. Update ViewModel to always call `RefreshAsync`.

**Pros**:
- Single source of truth for state (`_isMonitoring`).
- ViewModel becomes simpler.
- Logic is encapsulated in the domain layer.

**Cons**:
- Slightly changes semantic of `RefreshAsync` (from "Reset & Restart" to "Reset & Sync").

**Effort Estimate**: Small

### Option B: Add `bool startWatcher` parameter to `RefreshAsync`

**Description**: Change interface to `RefreshAsync(bool startWatcher)`.

**Pros**:
- Explicit control from caller.

**Cons**:
- Breaking interface change.
- Leaks implementation detail (watcher control) to ViewModel.

**Effort Estimate**: Small

## 5. Recommendations

### Primary Recommendation

**Adopt Option A**. It aligns better with domain-driven design principles by keeping the state logic within the Orchestrator.

### Risks to Address in Planning

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Config not applied when OFF | Low | Med | Verify `RefreshAsync` loads config even if watcher doesn't start (Code confirms it does). |

## 6. Unanswered Questions

None.

---

## Approval

- [x] All questions from requirements addressed
- [x] Evidence provided for conclusions
- [x] Recommendations are actionable

**Next Step**: 03_plan.md
