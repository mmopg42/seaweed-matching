---
Owner: Development Team
Last Updated: 2024-12-14
Related PRs: []
Code Ref: (MainWindow.xaml)
---

# UI Display: Group Monitoring

## Overview
This document defines how File Groups (Normal Camera + NIR + Additional Cameras) are visualized in the main monitoring dashboard. It bridges the gap between the `FileGroup` data model and the user interface.

## 1. Data Structure vs UI Representation

The backend `FileGroup` is wrapped in `FileGroupViewModel` for display.

| Data Concept | Backend Model (`FileGroup`) | UI ViewModel (`FileGroupViewModel`) | UI Control (`DataGrid`) |
|--------------|-----------------------------|-------------------------------------|-------------------------|
| **Identifier** | `GroupId` (e.g. "group_001") | `GroupId` | Text Column "Index" |
| **Normal Data** | `MainImagePath` | `MainImageThumbnail` (BitmapImage) | Image Column "Main Img" |
| **NIR Data** | `NirFilePath` | `NirGraphThumbnail` (BitmapImage) | Image Column "NIR Graph" |
| **Status** | Logic-based | `StatusText` / `IsAbnormal` | Text / Row Style |
| **Cameras** | `CameraFiles["cam1"]` | `Camera1Thumbnail` | Image Column "Cam 1" |

### 1.1 Data Source Mapping

Defines exactly which physical files are visualized for each component.

| UI Component | Source File Rule | Description |
|--------------|------------------|-------------|
| **Main Image** | `{NormalFolder}/stitched_original.png` | The stitched result image inside the Normal folder |
| **NIR Graph** | `{NirKey}.spc` (or `.csv`) | Spectral data file, parsed and plotted as a graph |
| **Cam 1-6** | `{Timestamp}_*.bmp` (or `.jpg`) | Raw capture image from individual camera folders |

## 2. Visual Layout (Monitoring Grid)

The monitoring grid is divided into tabs: **Line 1**, **Line 2**, and **Combined**.

### Column Definitions

| Header | Width | Binding | Visual | Description |
|--------|-------|---------|--------|-------------|
| **(Check)** | 40px | `IsSelected` | CheckBox | Selection for batch operations |
| **Index** | 80px | `GroupId` | Text | Sequential ID (e.g., `group_005`) |
| **Status** | 100px | `StatusText` | Text | "Complete", "Incomplete", etc. |
| **Main Img** | Configurable | `MainImageThumbnail` | **Image** | Stitched image from Normal Folder |
| **NIR Graph** | Configurable | `NirGraphThumbnail` | **Graph** | Visualized NIR spectral graph |
| **Cam 1..3** | Configurable | `CameraXThumbnail` | **Image** | Additional camera images (Line 1) |
| **Cam 4..6** | Configurable | `CameraXThumbnail` | **Image** | Additional camera images (Line 2) |

### Configuration
- **Image Size**: Controlled by `DisplayImageWidth` / `DisplayImageHeight` settings.
- **NIR Size**: Controlled by dedicated `NirDisplayWidth` / `NirDisplayHeight` settings.

## 3. Visual States

### Row Highlighting
The entire row changes appearance based on the group's state.

| State | Condition (`FileGroupViewModel`) | Visual Style |
|-------|----------------------------------|--------------|
| **Normal** | `IsAbnormal == false` | White background |
| **Abnormal** | `IsAbnormal == true` | **Light Yellow** (`#fff3cd`) background<br>**Amber** (`#ffc107`) left border (2px) |
| **Selected** | `IsSelected == true` | Standard System Blue highlight |

### Image Loading State
Images are loaded asynchronously to prevent UI freezing.

- **Loading**: Shows an indeterminate **ProgressBar** at the bottom of the image cell.
- **Loaded**: Displays the image (`Stretch="Uniform"`).
- **Missing/Null**: Empty cell (or placeholder if implemented).

## 4. NIR Graph Visualization

NIR data is unique because it is raw data (`.spc` or `.csv`) that must be converted to an image.

1. **Source**: `.spc` file from `NirFilePath`
2. **Processing**: `NirGraphGenerator` converts creates a plot
3. **Output**: `BitmapImage` displayed in the "NIR Graph" column
4. **Interaction**: Double-clicking a row opens `DetailView`, showing the interactive NIR graph.

## 5. Line Separation

UI strictly separates data by Production Line:

- **Line 1 Tab**: Shows groups where `LineNumber == 1` (Cam 1, 2, 3)
- **Line 2 Tab**: Shows groups where `LineNumber == 2` (Cam 4, 5, 6)
- **Combined Tab**: Shows both lines side-by-side with a splitter.

## Dependents
<!-- VERIFY: grep -rn "FileGroupViewModel" ChronoView/ -->
<!-- VERIFY: grep -rn "MainImageThumbnail" ChronoView/ -->
<!-- VERIFY: grep -rn "NirGraphThumbnail" ChronoView/ -->

- `MainWindow.xaml`: The view consuming these properties
- `MainWindowViewModel.cs`: Manages the collections (`Line1Groups`, `Line2Groups`)
