---
Task: fix_refresh_sensing_bug
Created: 2026-01-13
Status: Draft
Depends On: 03_plan.md
---

# Fix Refresh Sensing Bug - Detailed Design

## 1. Component Designs

### 1.1 MonitoringOrchestrator

> Logic for ensuring `FileSystemWatcher` is only started if monitoring is active.

#### Interface (from Plan)

```csharp
public async Task RefreshAsync(CancellationToken cancellationToken = default)
```

#### Detailed Logic

```pseudo
function RefreshAsync(cancellationToken):
    // ========== 1. STOP CURRENT SERVICES ==========
    await eventProcessor.StopAsync()
    fileWatcher.FileChanged -= OnFileChanged
    await fileWatcher.StopWatchingAsync()
    
    // ========== 2. RELOAD CONFIGURATION ==========
    currentConfig = configManager.LoadConfiguration()
    
    // ========== 3. APPLY MATCHER CONFIG ==========
    fileGroupMatcher.Configuration = mapSettings(currentConfig)
    
    // ========== 4. CORE RESET & SCAN (UI CLEARED HERE) ==========
    knownFiles = await CoreResetAndScanAsync(cancellationToken)
    
    // ========== 5. FORCE STOP (FIX HERE) ==========
    // Refresh implies resetting to a clean state. We treat it as "Sync & Stop".
    // We do NOT restart the watcher, even if it was running.
    
    // Explicitly update internal state if needed
    _isMonitoring = false
    
    // Since we called _fileWatcher.StopWatchingAsync above, 
    // and _eventProcessor.StopAsync above, we are already stopped.
    // Just ensure we don't restart them.
    
    log "Refresh completed. Sensing is stopped."
        
    uiLog.Invoke(Info, "System", "새로고침 완료")
```

#### Error Handling

| Error | Detection | Handling | Recovery |
|-------|-----------|----------|----------|
| Scan failure | `CoreResetAndScanAsync` throws | Catch, log error, notify UI | System remains in last valid state |
| Config reload failure | `configManager.Load` returns null | Log error, use previous config | Continue with old settings |

---

### 1.2 SystemControlViewModel

> Update the refresh command to avoid accidental monitoring starts.

#### Interface

```csharp
public async Task RefreshMonitoringAsync()
```

#### Detailed Logic

```pseudo
function RefreshMonitoringAsync():
    Log "Refreshing data..."
    config = await configManager.LoadConfiguration()
    
    // Refresh stats (always needed for UI counters)
    await statsService.ReloadStatsAsync(config)
    
    // Status syncing for external launchers
    await InitializeLaunchersAsync()

    // REFACTORED: Unify refresh path
    // Let orchestrator handle the conditional start logic internally
    await orchestrator.RefreshAsync()
    
    // If IsMonitoring was false in ViewModel, it STAYS false.
    // If it was true, it STAYS true.
    
    StatusChanged.Invoke("Refreshed")
```

## 2. Integration Points

### 2.1 ViewModel → Orchestrator

The ViewModel no longer needs to branch logic based on its `IsMonitoring` property. It simply delegates the "sync" request to the Orchestrator, which now correctly preserves the "Sensing" state.

## 3. Edge Cases

| Case | Scenario | Expected Behavior |
|------|----------|-------------------|
| Refresh while Start is pending | User clicks Refresh during Start monitoring | `RefreshAsync` will stop and restart the partial state. |
| Refresh with empty paths | All watch folders are deleted | `RefreshAsync` completes scan (0 files), does not start watcher. |
| Refresh while Monitoring is ON | Normal operation | Full reset and scan, watcher restarts with new config. |

## 4. Testing Strategy

### 4.1 Manual Verification Steps

**Scenario A: Refresh while Sensing is OFF**
1. Ensure Monitoring is NOT running (Start button is visible).
2. Click "Refresh".
3. Verify:
   - Data grid clears and re-fills.
   - Background sensing is NOT active (add a file to the folder, verify it does NOT appear automatically).
   - Log shows "Sensing remains OFF".

**Scenario B: Refresh while Sensing is ON**
1. Start Monitoring (Stop button is visible).
2. Click "Refresh".
3. Verify:
   - Data grid clears and re-fills.
   - Background sensing IS active (add a file to the folder, verify it DOES appear automatically).
   - Log shows "FileWatcher restarted".

---

## Approval

- [ ] All components have detailed pseudo-code
- [ ] Error handling specified for all failure modes
- [ ] State management documented
- [ ] Test cases defined

**Next Step**: 05_tasks.md
