---
Task: Global Refactoring (600-line Rule)
Created: 2024-05-16
Status: Draft
Depends On: 01_requirements.md, 02_research.md
---

# Global Refactoring - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| Files < 800 lines (Target 600) | Extraction of Sub-ViewModels and Sub-Services | Line count check for all refactored files |
| Functional Parity | Comprehensive UI and Unit testing | Comparison of Move/Delete behavior with legacy |
| No Regressions in Thumbnail Loading | MediaLoader extraction | Visual verification of UI data grid items |
| Atomic File Operations preserved | Generic FileGroupOperator with rollback | Mocked I/O failure tests for rollback |

## 0.1 MainWindowViewModel Functional Mapping

To ensure 100% parity, all sections identified in the original `MainWindowViewModel.cs` are mapped as follows:

| original Section | Proposed Destination | Components Included |
|------------------|----------------------|---------------------|
| 1. Dependencies | DI Container | Split across sub-VMs |
| 2. Properties (Collections/Stats) | `DashboardViewModel` | `FileGroups`, `MatchRate`, `NirCount`, `MatchStats`, `AbnormalCount`, `GroupSelection` |
| 2. Properties (Control/Status) | `SystemControlViewModel` | `IsMonitoring`, `ProgramStatus`, `DateInput`, `Launchers` |
| 2. Properties (UI/Window) | `MainWindowViewModel` | `WindowSize`, `ImageSizes`, `ProgressValue`, `LogSizeMgmt`, `AutoSaveSettings` |
| 3 & 4. Commands (Monitoring) | `SystemControlViewModel` | `Start`, `Stop`, `AutoPath`, `SampleFolder`, `ToggleNir2` |
| 3 & 4. Commands (Data Ops) | `FileOperationViewModel` | `Move` (inc. NIR Balance), `Delete` (inc. Partial), `Refresh`, `Conflict Resolution Caching` |
| 3 & 4. Commands (Selection) | `DashboardViewModel` | `SelectAll`, `DeselectAll`, `OpenDetailView`, `OpenImagePreview` |
| 5. Public Methods | `DashboardViewModel` | `Add/Remove/ClearGroup`, `UpdateStats`, `DiagnosticLogs`, `RowHeightCalc` |
| 5. Public Methods | `MainWindowViewModel` | `AddLogMessage` (inc. Size Capping), `RequestOpenSettings` |
| 6. Operation Helpers | `MainWindowViewModel` | `BeginOperation`, `EndOperation`, `Cancel` |
| 7. Event Handlers | Distributed | Subscriptions per VM |
| 8. Disposal | All Components | Individual `Dispose()` |
| 9. Status Tracking | `SystemControlViewModel` | `OnProgramStatusChanged`, `StatusForeground` |

---

## 1. Architecture Overview

### 1.1 System Context
The refactoring aims to dismantle three monolithic classes (`MainWindowViewModel`, `FileGroupViewModel`, `FileOperationService`) that have grown into "God Objects". High-level responsibilities will be moved to specialized components while maintaining the existing dependency injection patterns.

### 1.2 Component Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    WPF MainWindow                       │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────────────────Coordinator──────────────────┐  │
│  │               MainWindowViewModel                 │  │
│  └────────────────────────┬──────────────────────────┘  │
│        ┌──────────────────┼──────────────────┐          │
│        ▼                  ▼                  ▼          │
│  ┌────────────┐    ┌─────────────┐    ┌─────────────┐   │
│  │DashboardVM │    │FileOpVM     │    │SysControlVM │   │
│  └────────────┘    └─────────────┘    └─────────────┘   │
│                                                         │
└───────────────────────────┬─────────────────────────────┘
                            │
                            ▼
┌───────────────────────────┼─────────────────────────────┐
│                    Business Services                    │
├───────────────────────────┴─────────────────────────────┤
│                                                         │
│  ┌──────────────────┐           ┌──────────────────┐    │
│  │   MoveService    │──────────►│FileGroupOperator │    │
│  └──────────────────┘           └────────┬─────────┘    │
│                                          │              │
│  ┌──────────────────┐                    │              │
│  │  DeleteService   │◄───────────────────┘              │
│  └──────────────────┘                                   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 2. Components

### 2.1 New Components

| Component | Type | Location | Purpose |
|-----------|------|----------|---------|
| `FileGroupOperator` | Class | `Core/FileOperations/FileGroupOperator.cs` | Atomic "Copy-Verify-Delete-Rollback" loop engine |
| `MoveService` | Class | `Core/FileOperations/MoveService.cs` | Moving logic extracted from FileOperationService |
| `DeleteService` | Class | `Core/FileOperations/DeleteService.cs` | Deletion logic extracted from FileOperationService |
| `MediaLoader` | Class | `UI/ViewModels/MediaLoader.cs` | extracted logic for thumbnail and Graph loading |
| `DashboardViewModel`| Class | `UI/ViewModels/DashboardViewModel.cs` | List management (FileGroups, Line filters) |
| `FileOpViewModel` | Class | `UI/ViewModels/FileOpViewModel.cs` | Move/Delete command orchestration logic |
| `SystemControlViewModel`| Class | `UI/ViewModels/SystemControlViewModel.cs`| Start/Stop monitoring and Launchers |

### 2.2 Modified Components

| Component | Location | Changes | Breaking Change? |
|-----------|----------|---------|------------------|
| `FileOperationService` | `Core/FileOperations/FileOperationService.cs` | Proxy calls to MoveService/DeleteService | No |
| `FileGroupViewModel` | `UI/ViewModels/FileGroupViewModel.cs` | Delegate media loading to MediaLoader | No |
| `MainWindowViewModel` | `UI/ViewModels/MainWindowViewModel.cs` | Host sub-ViewModels; thin coordinator | No |

---

## 3. Interface Definitions

### 3.1 FileGroupOperator
```csharp
public class FileGroupOperator {
    public async Task<OperationResult> ExecuteStructuredOpAsync(
        FileGroup group,
        string targetBase,
        OpType type, // Move, Delete (Quarantine)
        PathSchema schema, // Detailed naming rules
        CancellationToken ct);
}
```
**Responsibilities**:
- Implement the "Move = Copy + Verify + Delete" pattern.
- Manage the rollback list if any step fails.
- Abstract the difference between "with NIR" and "without NIR" path structures.

---

## 4. Key Design Decisions

### 4.1 "Coordinator" pattern for MainWindowViewModel
**Context**: How to split MainWindowViewModel while keeping XAML bindings working?
**Decision**: Use Composition. MainWindowViewModel will expose sub-ViewModels as properties.
**Rationale**: Avoids massive changes to `MainWindow.xaml` and resource dictionaries while allowing the C# code to be cleanly split into < 600 line files.

### 4.2 Generic Operator for File I/O
**Context**: Move and Delete services share identical looping and verification logic.
**Decision**: Create a shared `FileGroupOperator`.
**Rationale**: Eliminates ~500 lines of duplicated code across FileOperationService.

---

## 10. Open Questions
- [ ] Should `DashboardViewModel` be the owner of `FileGroups` collection or should it remain in the coordinator? (Propose: Dashboard owner)
- [ ] How to handle cross-VM event orchestration (e.g. Stop triggered in SystemControlVM affecting lists in DashboardVM)? (Propose: Shared mediator or event aggregator).
