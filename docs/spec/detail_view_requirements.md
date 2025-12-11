# Detail Preview View Requirements

## Overview
A detailed view panel that displays comprehensive information and enlarged images for a selected file group. This view helps users inspect the quality and matching status of files in detail.

## 1. User Interaction (Trigger)
- **Action**: The user **double-clicks** a row in the DataGrid (applicable to Line 1, Line 2, and Combined tabs).
- **Result**: A Detail Preview panel appears at the bottom of the main window or updates its content if already open.

## 2. UI Layout & Content
The Detail View should generally follow the layout provided in the reference image.

### 2.1 Header Section
- **Group Info**: Displays the Group ID (e.g., "Group #144").
- **Status Indicator**: Displays the current status of the group with an icon and text.
    - Example: "⚠️ NIR File Missing" (Yellow/Orange), "✅ Complete" (Green).

### 2.2 Image Gallery Section
A horizontal layout displaying the images associated with the group. All images in this view must be **larger** than the thumbnails in the DataGrid.

#### A. Main Image (Normal)
- **Label**: "Main Image"
- **Content**: The image from the Normal Camera folder.
- **Display**: High-quality preview.

#### B. NIR Image
- **Label**: "NIR Image"
- **Content**: The visual representation of the NIR file (if available) or a placeholder.
- **Missing State**: If the NIR file is missing, display a distinct placeholder (e.g., a box with a red "X" icon and text "No NIR").

#### C. Composite Cameras
- **Label**: "Composite Cams" (e.g., "Composite Cams (3)")
- **Context Awareness**:
    - **Line 1 Groups**: Display **Cam 1, Cam 2, Cam 3**.
    - **Line 2 Groups**: Display **Cam 4, Cam 5, Cam 6**.
- **Layout**: These images should be arranged logically (e.g., a 2x2 grid or a horizontal list) next to the NIR image.
- **Sub-labels**: Each camera image should have a small label indicating its source (e.g., "[1]", "[2]", "[3]" or "[4]", "[5]", "[6]").

## 3. Functional Requirements

### 3.1 Dynamic Content Loading
- The view must dynamically load the correct images based on the selected `FileGroup`.
- It must handle `LineNumber` (1 or 2) to decide which set of Composite Cameras to show.
- It must handle missing files gracefully (show placeholders).

### 3.2 Integration
- The view should be integrated into the `MainWindow` (likely as a collapsible bottom row or a dedicated region).
- It binds to a `SelectedDetailGroup` property in the ViewModel, which is updated upon the double-click command.

## 4. Design References
- **Theme**: Consistent with the application's dark/light theme.
- **Typography**: Clear labels, clearly distinct status text.
