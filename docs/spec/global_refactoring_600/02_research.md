# Research Report: Global Refactoring (600-line Rule)

## 0. Introduction
This document analyzes the current state of large files in the ChronoView project that exceed the 600-800 line guideline. The objective is to identify logical separation points and propose an architecture that improves maintainability and testability.

## 1. File Analysis

### 1.1 MainWindowViewModel.cs (2881 Lines)
- **Violation**: 480% of the recommended limit.
- **Responsibilities Identified**:
  - Main Window State (Tabs, ProgressBar, Status).
  - App Lifecycle Management (Start/Stop monitoring, Program launchers).
  - Collection Management (FileGroups, Line1Groups, Line2Groups).
  - Command Implementations (Move, Delete, Refresh, Setup, etc.).
  - Statistics Aggregation (Updates from multiple services).
  - Log Sinking (AddLogMessage).
- **Critical Bloat areas**:
  - `ExecuteMoveAsync` (226 lines).
  - `ExecuteDeleteAsync` (187 lines).
  - Statistics update logic (distributed throughout events).

### 1.2 FileGroupViewModel.cs (1344 Lines)
- **Violation**: 224% of the recommended limit.
- **Responsibilities Identified**:
  - Data Binding for FileGroup Model.
  - Multi-level selection state (Individual checkboxes vs Row selection).
  - Media Loading & Processing (Thumbnails for 8+ slots).
  - NIR Graph Generation (ScottPlot integration).
  - I/O Retry Mechanism (Timer-based polling for locked files).
- **Critical Bloat areas**:
  - Thumbnail loading and logic (~500 lines).
  - NIR Graph generation (~120 lines).

### 1.3 FileOperationService.cs (1080 Lines)
- **Violation**: 180% of the recommended limit.
- **Responsibilities Identified**:
  - Structured path building (Move vs Trash).
  - Copy-Verify-Delete atomic operation logic.
  - Rollback mechanism.
  - Move algorithms for Groups.
  - Delete algorithms for Groups and individual components.
- **Critical Bloat areas**:
  - `MoveFileGroupAsync` (314 lines).
  - `DeleteFileGroupAsync` (320 lines).
  - These share nearly 90% logic structure but are implemented separately.

## 2. Proposed Architecture (Extraction Plan)

### 2.1 UI Layer Refactoring
- **[NEW] `DashboardViewModel`**: Handles the DataGrid collections and active tab state.
- **[NEW] `FileOperationViewModel`**: Handles the heavy command implementations (`Move`, `Delete`, `Refresh`).
- **[NEW] `SystemControlViewModel`**: Handles monitoring lifecycle (`Start`, `Stop`) and integration with program launchers.
- **`MainWindowViewModel`**: Reduced to a thin container/coordinator of these sub-ViewModels.

### 2.2 ViewModel Layer Refactoring
- **[NEW] `MediaLoader`**: Extract all thumbnail and NIR graph loading logic from `FileGroupViewModel`.
- **[NEW] `FileGroupSelection`**: (Optional) separate selection state if needed, but primarily focus on extracting loading logic.

### 2.3 Service Layer Refactoring
- **[NEW] `AbstractFileGroupCarrier`**: Implement a base class or utility that handles the "Copy-Verify-Delete-Rollback" loop generically.
- **`FileOperationService`**: Split into `MoveService.cs` and `DeleteService.cs`.

## 3. Impact Analysis
- **Breaking Changes**: Minimal. Logic is being moved, not changed.
- **Dependency Graph**: Will become more explicit as sub-services are injected.
- **Build Performance**: Neutral, but code navigation will be significantly faster.

## 4. Recommendations
1. Proceed with `FileOperationService` first as it has high logic duplication.
2. Refactor `FileGroupViewModel` to extract media loading into a dedicated component.
3. Finally, dismantle `MainWindowViewModel` by extracting its heavy commands into feature-specific ViewModels.
