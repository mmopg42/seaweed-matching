# Refactoring Analysis: MonitoringOrchestrator.cs

**Current Size**: 2,880 Lines
**Goal**: Reduce to < 600 Lines
**Status**: Critical - High Technical Debt & Duplication

## 1. Top-Level Duplication Findings

### A. Dual Initial Scan Logic (Critical)
The `PerformInitialScanAsync` method (Lines 515-767) contains **two completely separate implementations** of scanning logic:
1.  **Legacy Batch Scan** (Lines 547-767): Manually iterates directories, builds `UnmatchedFiles`, and calls `_fileGroupMatcher`.
2.  **Sequential Scan** (Lines 775-926): Called if `DataSequenceSettings` exists. Uses `ScanFilesForDataTypeAsync`.

**Issue**: The Legacy block (lines 547-767) is effectively **dead code** in production (since `DataSequenceSettings` is loaded), but it occupies ~200 lines and duplicates the scanning logic found in `ScanFilesForDataTypeAsync` (931-998).

### B. Dead Method: `CreateNewGroupAsync`
*   **Definition**: Lines 2065-2119.
*   **Usage**: **None** (internal dead code).
*   **Context**: It seems to be a left-over from a previous refactoring where `CreateOrUpdateGroupAsync` took over.
*   **Action**: Safe to delete.

### C. Orphaned Interface Method: `ProcessFileEventsAsync`
*   **Definition**: Lines 2200-2322.
*   **Interface**: Defined in `IMonitoringOrchestrator`.
*   **Usage**: Not used in `MainWindowViewModel` or `MonitoringOrchestrator` internals.
*   **Issue**: It duplicates the event processing logic found in `ProcessSingleEventAsync` (the parallel worker pipeline). It seems to be an old synchronous/batch entry point.
*   **Action**: Remove from Interface and Class.

## 2. Structural Complexity Issues

### A. `FindMatchingExistingGroup` (Lines 1523-1860)
*   **Size**: ~340 Lines.
*   **Complexity**: Contains 3 distinct matching strategies (NormalFolder, NirKey, Timestamp/Sequence).
*   **Issue**: The "Match 3" (Timestamp) logic (Lines 1603-1854) is excessively complex and contains inline UI logging, tolerance checking, and temporal ordering logic.
*   **Refactoring**: Should be extracted to a separate `IMatchingStrategy` or `MatchingLogic` helper class.

### B. `ProcessSingleEventAsync` & `CreateOrUpdateGroupAsync`
*   **Logic Mixing**: Mixes locking, debouncing, type determination, and group creation.
*   **Inline Caching**: Lines 1125-1140 (Normal folder timestamp caching) is inline.
*   **Inline Image Loading**: Lines 871-912 (Pre-load Normal images) is inline.

## 3. Proposed Refactoring Plan

### Phase 1: Eliminate Dead & Duplicate Code (Immediate Win)
1.  **Remove Legacy Scan Block**: Delete lines 547-763 in `PerformInitialScanAsync`.
2.  **Remove `CreateNewGroupAsync`**: Delete lines 2065-2119.
3.  **Remove `ProcessFileEventsAsync`**: Delete from class and interface (safe if confirmed unused by tests/external).
4.  **Consolidate Scanning**: Ensure `PerformInitialScanAsync` only calls the Sequential strategy.

**Estimated Reduction**: ~400-500 Lines.

### Phase 2: Extract Responsibilities
1.  **Extract `InitialScanner`**: Move `PerformSequentialInitialScanAsync` and `ScanFilesForDataTypeAsync` to a helper class `InitialScanner`.
2.  **Extract `EventProcessor`**: Move `ProcessSingleEventAsync`, `OnFileChanged`, and worker logic to `EventProcessor`.
3.  **Extract `GroupManager`**: Move `CreateOrUpdateGroupAsync`, `FindMatchingExistingGroup`, `MergeGroups`, `RemoveFromGroupAsync` to `GroupManager`.

### Phase 3: Interface Cleanup
*   `IMonitoringOrchestrator` should only expose high-level control (`Start`, `Stop`, `Refresh`) and Events.
*   Detail methods like `PromoteCacheToGroupId` should likely be internal or part of a sub-service.

## 4. Dependencies to Update
*   `IMonitoringOrchestrator.cs`: Remove `ProcessFileEventsAsync`.
*   Tests: Update any tests mocking `IMonitoringOrchestrator`.

