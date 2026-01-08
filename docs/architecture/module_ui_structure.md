---
Owner: Development Team
Last Updated: 2026-01-04
Code Ref: ChronoView/MainWindow.xaml
---

# UI Module: Main Window Structure

## Overview

This module defines the structural architecture of the main application window (`MainWindow.xaml`). It adheres to a component-based design where the main window acts as a shell, orchestrating specialized UserControls for disjoint functional areas (Statistics, Workflow) and leveraging shared resources for visual consistency.

## Key Components

| Component | Type | Location | Purpose |
|-----------|------|----------|---------|
| `MainWindow` | Window | `ChronoView/MainWindow.xaml` | Application shell, layout orchestration, and DataGrid hosting. |
| `StatisticsPanel` | UserControl | `UI/Controls/StatisticsPanel.xaml` | Displays real-time file counts and matching statistics (Top Bar). |
| `WorkflowPanel` | UserControl | `UI/Controls/WorkflowPanel.xaml` | Manages workflow controls, system status, and data status (Left Sidebar). |
| `FileGroupDataGrid` | UserControl | `UI/Controls/FileGroupDataGrid.xaml` | Reusable DataGrid component for displaying file groups with dynamic columns. |
| `ImagePreviewWindow` | Window | `UI/Views/ImagePreviewWindow.xaml` | Popup window for high-resolution image viewing with drag-and-move support. |
| `SharedResources` | ResourceDictionary | `Resources/SharedResources.xaml` | Centralized styles, templates, and converters. |

## Structural Design

### 1. Resource Management
**Source of Truth**: `Resources/SharedResources.xaml`

To prevent `MainWindow.xaml` bloat and ensure visual consistency, all re-usable resources are extracted here:
- **DataTemplates**: `NormalFileTemplate`, `NirFileTemplate`, `Camera1Template`...
- **Styles**: `ChipStyle` (Statistics chips), `ToolbarButtonStyle`, `FileGroupDataGridStyle`.
- **Converters**: `BooleanToVisibilityConverter`, `NullToVisibilityConverter`.
- **Brushes**: `BackgroundBrush`, `PanelBackgroundBrush`, `BorderBrush`...

**Integration**:
Merged into `App.xaml` to be globally available across all windows and controls.

### 2. Componentization
The monolithic `MainWindow` has been decomposed:

- **Top Dock**: `StatisticsPanel`
    - *Responsibility*: Read-only display of counts (`NirCount`, `NormalCount`, `MatchRate`).
    - *Binding*: Inherits `MainWindowViewModel`.

- **Left Dock**: `WorkflowPanel`
    - *Responsibility*: User interaction for system control (Camera Status, NIR Filtering, Sample Info).
    - *Binding*: Inherits `MainWindowViewModel`.

- **Center Content**: `FileGroupDataGrid` (Line 1 / Line 2 / Combined)
    - *Responsibility*: Core data visualization. Replaces raw `DataGrid` to ensure consistent column order and behavior (via `LineNumber` property).
    - *Features*: Auto-generates columns based on settings, handles "Select All" logic.

- **Auxiliary Windows**: `ImagePreviewWindow`
    - *Responsibility*: Provide a non-modal, draggable popup for inspecting full-size images from the DataGrid.
    - *Interaction*: Triggered via `OpenImagePreviewCommand` in `MainWindowViewModel`.

## Contracts

### DataContext
All components (`MainWindow`, `StatisticsPanel`, `WorkflowPanel`) share the same `MainWindowViewModel`.
- **Implicit Contract**: These UserControls assume their `DataContext` is of type `MainWindowViewModel`.

### Event Handoff
- **User Actions** (e.g., clicking "Toggle NIR Filter") are bound via `Command` to the ViewModel.
- **No Code-Behind Logic**: The UserControls (`.xaml.cs`) are purely for initialization (`InitializeComponent()`). All logic resides in the ViewModel.

## Dependencies

### Internal
| Module | Purpose |
|--------|---------|
| `MainWindowViewModel` | Provides data and commands for all UI components. |
| `SharedResources` | Provides visual definitions (Styles, Templates). |

### External
| Library | Purpose |
|---------|---------|
| `System.Windows.Controls` | WPF Logic. **Note**: explicit namespace usage required to avoid WinForms collision. |

## Dependents

<!-- VERIFY: grep -rn "StatisticsPanel" ChronoView/ -->
<!-- VERIFY: grep -rn "WorkflowPanel" ChronoView/ -->
<!-- VERIFY: grep -rn "SharedResources.xaml" ChronoView/ -->

| Module | How It Uses This |
|--------|------------------|
| `MainWindow.xaml` | Hosts the `StatisticsPanel` and `WorkflowPanel`. |
| `App.xaml` | Merges `SharedResources.xaml`. |

## Impact / Touchpoints

<!-- VERIFY: grep -rn "MainWindowViewModel" ChronoView/ -->

When modifying this module, check:

| File | Reason |
|------|--------|
| `MainWindowViewModel.cs` | Binding sources for all panels. Renaming properties here breaks the UI. |
| `SharedResources.xaml` | Changing a key (e.g., `NormalFileTemplate`) breaks the DataGrid columns. |
| `ImagePreviewWindow.xaml` | UI definition for the image popup. |

## Changelog

| Date | Change |
|------|--------|
| 2026-01-04 | Added `ImagePreviewWindow` and non-modal popup support. |
| 2026-01-04 | Initial creation. Refactored monolithic MainWindow into component-based architecture. |
