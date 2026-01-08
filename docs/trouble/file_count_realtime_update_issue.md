# Real-time File Count Update Issue Analysis

## Symptom
- Real-time file processing works (groups are created).
- However, the "File Count" (Cam1, Cam2, etc.) in the UI does not increase during real-time running.
- Clicking "Refresh" updates the counts correctly.

## Root Cause Analysis

### 1. Scope Mismatch (`TopDirectoryOnly` vs `AllDirectories`)
- **FileWatcher (`MonitoringOrchestrator`)**: Configured with `IncludeSubdirectories = true`. It detects files in subfolders (e.g. `Cam1\2026\01\06\image.bmp`).
- **StatisticsService**: Configured with `SearchOption.TopDirectoryOnly`. It ONLY counts files in the root of the watched folder.

If the camera software saves images into date-based subfolders, `FileWatcher` detects them and `GroupManager` creates groups (so "Group Count" increases).
However, `StatisticsService` ignores these subfolder files, so "Cam1 Count" remains 0 (or unchanged).

### 2. Update Logic Issue (Only First Update)
- **실시간 모니터링 시나리오**: 파일이 없는 상태(0 0 0 0)에서 시작 → 데이터가 생겨나면서 스캔
- **문제**: `_lastFileCount`가 생성자에서 빈 객체(`new FileCountStatistics()`)로 초기화됨
- **결과**: 
  1. 첫 번째 루프에서 `GetFileCountsAsync()`가 `TopDirectoryOnly`로 인해 0 0 0 0... 반환
  2. `_lastFileCount.Equals(stats)`가 `true` (둘 다 0 0 0 0...)
  3. **이벤트가 발생하지 않음** (변경사항이 없다고 판단)
  4. 이후에도 계속 0이므로 변경이 없어 업데이트가 **최초 1번도 발생하지 않음**
- **새로고침이 작동하는 이유**: `ReloadStatsAsync()`는 항상 이벤트를 발생시킴 (`FileCountsUpdated?.Invoke()`)

### 3. Why Refresh Works?
- **Refresh 버튼**: `ReloadStatsAsync()`는 **항상** 이벤트를 발생시킴 (`FileCountsUpdated?.Invoke()`)
- **실시간 모니터링**: `MonitorFileCountsAsync()`는 변경사항이 있을 때만 이벤트를 발생시킴
- **핵심 차이**: 
  - Refresh는 데이터가 이미 있는 상태에서 새로고침하므로, `AllDirectories`로 변경하면 정확한 카운트가 나옴
  - 실시간 모니터링은 파일이 없는 상태에서 시작하므로, `TopDirectoryOnly`로 인해 계속 0이 반환되고, `_lastFileCount`도 0이므로 변경이 없다고 판단되어 이벤트가 발생하지 않음

## Solution

### 1. Align Scopes (Primary Fix)
- **Change `StatisticsService.CountFilesInDirectoryAsync`** to use `SearchOption.AllDirectories` to match `FileWatcher`'s behavior.
- This ensures that every file detected by the watcher is also counted by the statistics service.

### 2. Fix Initial Update Issue (Secondary Fix)
- **Reset `_lastFileCount` to `null`** in `StartMonitoringAsync()` to ensure the first update always triggers an event.
- This ensures that even if the initial count is 0, the UI will be updated at least once.

## Code Changes

### Change 1: Fix Scope Mismatch
Modify `ChronoView/Core/Analytics/StatisticsService.cs` line 403:
```csharp
// Change SearchOption.TopDirectoryOnly -> SearchOption.AllDirectories
return await Task.Run(() => Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories).Count());
```

### Change 2: Fix Initial Update
Modify `ChronoView/Core/Analytics/StatisticsService.cs` in `StartMonitoringAsync()` method (after line 77):
```csharp
_currentConfig = config ?? throw new ArgumentNullException(nameof(config));
_lastFileCount = null; // Reset to ensure first update always triggers event
_isMonitoring = true;
```
