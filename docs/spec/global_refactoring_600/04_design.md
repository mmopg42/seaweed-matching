---
Task: Global Refactoring (600-line Rule)
Created: 2024-05-16
Status: Draft
Depends On: 03_plan.md
---

# Global Refactoring - Detailed Design

## 1. Component Designs

### 1.1 FileGroupOperator
> Unified engine for atomic file group operations.

#### Interface
```csharp
Task<OperationResult> ExecuteOpAsync(FileGroup group, string targetBase, OpType opType, PathSchema schema, IProgress<OperationProgress> progress, CancellationToken ct)
```

#### Detailed Logic
```pseudo
function ExecuteOpAsync(group, targetBase, opType, schema, progress, ct):
    movedItems = []
    try:
        // 1. Collect all components (Normal, Nir, Cam1-6)
        components = group.GetAvailableComponents()
        
        // 2. Iterate and process
        foreach comp in components:
            ct.ThrowIfCancellationRequested()
            
            // Build destination using schema
            destPath = BuildPath(targetBase, group, comp, schema)
            
            // Copy-Verify-Delete Pattern
            await CopyAsync(comp.Path, destPath)
            if not Verify(comp.Path, destPath):
                throw Error("Verification failed")
                
            movedItems.Add({source: comp.Path, dest: destPath})
            await DeleteAsync(comp.Path)
            
        return Success()
    catch error: 
        await Rollback(movedItems)
        return Failure(error)
```

### 1.2 FileGroupMediaLoader
> Decoupled media loading for FileGroupViewModel.

#### Responsibilities
- Manage `BitmapSource` properties for UI.
- Implement ScottPlot parsing and rendering.
- Maintain the async retry queue for locked files.

#### Logic (Thumbnail Loading)
```pseudo
async function LoadThumbnailsAsync():
    if alreadyLoading or disposed: return
    
    tasks = []
    foreach slot in [Main, Nir, Cam1...6]:
        if slot.Path exists and slot.Image == null:
            tasks.Add(LoadSingle(slot))
            
    await Task.WhenAll(tasks)
    if anyFailed: StartRetryTimer()
```

#### Detailed Logic (NIR Balancing)
```pseudo
function ExecuteMoveWithNirBalancing(groups, limit, nirLimit):
    // 1. Sort by Oldest first (Parity check)
    sortedGroups = groups.OrderBy(CreatedAt)
    
    // 2. Identify candidates
    moveCandidates = sortedGroups.Take(limit)
    
    // 3. Balancing logic (if enabled)
    if MoveNirCount > 0:
        totalNirInDest = GetCurrentNirCountInDest()
        foreach group in moveCandidates:
            if group.HasNir:
                if totalNirInDest >= nirLimit:
                    // Remove NIR from this group before move
                    group.RemoveNirComponents()
                else:
                    totalNirInDest++
                    
    // 4. Execute Move using FileGroupOperator
    foreach group in moveCandidates:
        await operator.ExecuteOpAsync(group, ...)
```

#### Detailed Logic (Partial Delete)
```pseudo
function ExecutePartialDelete(group, components):
    // 1. Validate selection
    if components.IsEmpty: return
    
    // 2. Call specialized service
    result = await deleteService.DeleteComponentsAsync(group, components, quarantinePath)
    
    // 3. UI Sync
    if result.Success:
        group.ClearStateForComponents(components)
        group.Refresh()
```

#### State Management (Conflict Cache)
```pseudo
class FileOperationViewModel:
    _cachedConflictResolution = null

    function ResolveConflict(path):
        if _cachedConflictResolution != null:
            return _cachedConflictResolution
            
        resolution = ShowConflictDialog(path)
        if resolution.ApplyToAll:
            _cachedConflictResolution = resolution
        return resolution
```

#### Resource Management
- **Log Management**: `AddLogMessage` logic: `while (LogMessages.Count > 1000) LogMessages.RemoveAt(0)`.
- **Auto-Save**: Properties for `MoveNirCount` and `MoveAllDataCount` will call `SaveSettingsAsync()` on setter change.
- **RowHeight**: Computed property in `DashboardViewModel` using `DisplayFontSize` and `Bitmap` dimensions.

---

## 2. Integration Points

### 2.1 MainWindowViewModel Coordinator
The `MainWindowViewModel` will act as a thin host and coordinator.

**Sub-ViewModel Responsibilities:**
- **DashboardViewModel**: 
  - Manages `ObservableCollection<FileGroupViewModel>`.
  - Subscribes to `IMonitoringOrchestrator` events (`GroupCreated`, `GroupUpdated`, `GroupRemoved`).
  - Implements statistics update logic (`UpdateStatistics`, `OnFileCountsUpdated`).
  - Handles row-level commands like `OpenDetailView`.

- **FileOperationViewModel**: 
  - Implements `Move`, `Delete`, `Refresh` commands.
  - Manages conflict resolution logic and user dialog interactions.
  - Handles Move-specific settings (limits, NIR balancing).

- **SystemControlViewModel**: 
  - Manages monitoring lifecycle (`StartAsync`, `StopAsync`).
  - Implements `AutoConfigurePathsAsync` and `CreateSampleFolder`.
  - Tracks program statuses (`GeneralCameraStatus`, `NirStatus`, `Nir2FilterStatus`) and manages their UI foreground/text.

**Integration Pattern:**
```csharp
public class MainWindowViewModel : ViewModelBase {
    public DashboardViewModel Dashboard { get; }
    public FileOperationViewModel Operations { get; }
    public SystemControlViewModel Control { get; }

    // AddLogMessage acts as a delegate to the Log Panel or local collection
    public void AddLogMessage(string msg, LogSeverity severity) { ... }
}
```

---

## 3. Edge Cases
| Case | Behavior |
|------|----------|
| File Locked during Move | Operator retries or triggers conflict resolution via callback |
| ScottPlot Parse Error | MediaLoader assigns "Broken Graph" placeholder image |
| Partial Deletion of missing file | Operator skips gracefully with warning log |

---

## 4. Testing Strategy
| Test | Verifies |
|------|----------|
| `test_rollback_on_partial_failure` | If Cam3 fails to move, Cam1/2 and Normal are restored |
| `test_line1_vs_line2_paths` | Ensure "일반1" vs "일반2" folders are correctly chosen |
| `test_ui_binding_propagation` | Ensure DashboardVM collection updates reflect in UI |
