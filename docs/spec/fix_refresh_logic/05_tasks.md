---
Task: fix_refresh_logic
Created: 2026-01-12
Status: Approved
Summary: Implementation steps for Deep Reset Refresh logic including refactoring for code deduplication and DI updates.
---

# Fix Refresh Logic - Tasks

## 1. Interface & Infrastructure

- [ ] **Dependency Injection**: 
    - Verify `IConfigurationManager` and `IAbnormalDetector` (or `IAbnormalDetectorService`) are registered in `App.xaml.cs`.
    - Update `MonitoringOrchestrator` registration to inject these two services.
- [ ] **IGroupManager**: Add `IEnumerable<string> GetAllProcessedFilePaths()` method to interface and implementation.
- [ ] **IFileWatcherService**: Add `StartWatchingAsync` overload with `IEnumerable<string> knownFiles`.
- [ ] **FileWatcherService**: Implement locking and injection logic in the new overload.

## 2. Refactoring MonitoringOrchestrator

- [ ] **Constructor**: Update `MonitoringOrchestrator` constructor to accept `IConfigurationManager` and `IAbnormalDetector`.
- [ ] **Helper Implementation**: Implement `private IEnumerable<string> GetWatchPaths(ApplicationConfiguration config)`.
- [ ] **Extract Logic**: Create `private async Task<IEnumerable<string>> CoreResetAndScanAsync(CancellationToken ct)`.
- [ ] **Update StartAsync**: Refactor to use `CoreResetAndScanAsync` and `GetWatchPaths`.
- [ ] **Rewrite RefreshAsync**:
    - Implement Stop (Watcher & Processor).
    - Reload Config: `_currentConfig = _configManager.LoadConfiguration()`.
    - Call `CoreResetAndScanAsync`.
    - Configure `FileWatcherOptions` (including `DataSequenceSettings`).
    - Restart Watcher (with Injection).
    - Restart Processor.

## 3. Abnormal Detector

- [ ] **AbnormalDetectorService.cs**: Ensure `Reset()` clears history and forces config reload.

## 4. Bug Fix: Phantom UI State (UI Sync)

- [ ] **IMonitoringOrchestrator**: Add `event EventHandler MonitoringStateReset`.
- [ ] **MonitoringOrchestrator**: Fire `MonitoringStateReset` in `CoreResetAndScanAsync` (before scanning).
- [ ] **DashboardViewModel**: Subscribe to `MonitoringStateReset` and clear `FileGroups`.

## 5. Verification

- [ ] **Test: DI Resolution**: Build and run to ensure no runtime DI errors.
- [ ] **Test: Config Reload**: Change settings (e.g. `UseFolderSuffix`) -> Refresh -> Verify groups re-organized.
- [ ] **Test: Code Shared**: Verify `StartAsync` and `RefreshAsync` sharing logic via `CoreResetAndScanAsync`.
- [ ] **Test: Ghost File**: Delete file -> Refresh -> Verify removal.
- [ ] **Test: Log Check**: Ensure "Silent scan" is skipped during Refresh (optimization check).
