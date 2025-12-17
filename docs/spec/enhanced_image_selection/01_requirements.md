---
Task: Enhanced Image Selection
Created: 2025-12-15
Status: Draft
Summary: Implement granular selection checkboxes under image thumbnails for individual file deletion using C# (WPF).
Research Required: No
---

# Enhanced Image Selection - Requirements

## 1. Goal

### Primary Goal
Enable users to select and delete individual images (Normal, NIR, Cam 1-6) within a data row using checkboxes displayed under each thumbnail in the WPF application.

### Success Criteria
- [ ] Checkboxes appear under all image thumbnails in the DataGrid.
- [ ] **Row Selection Checkbox** supports Tri-State (Checked, Unchecked, Indeterminate).
    - [ ] Checked: All individual images selected.
    - [ ] Unchecked: All individual images deselected.
    - [ ] Indeterminate (Partial): Some images selected.
- [ ] **Individual Checkboxes**: Toggling them updates the Row Checkbox state automatically.
- [ ] "Delete Selected" action removes ONLY the checked files.
- [ ] Visual design matches the "pretty" aesthetic (modern, clean, custom style).

## 2. Constraints

### Technical Constraints
- **Language**: C# (.NET)
- **Framework**: WPF (MVVM pattern)
- **Data Binding**: Must use `INotifyPropertyChanged` for real-time UI updates.
- **Components**: Integrate with `FileGroupViewModel` (or equivalent) and `MonitoringOrchestrator`.

### Non-Goals
- Python implementation (Python docs are for logic reference only).
- Changing the fundamental file matching logic.

## 3. UI Requirements

### Visual Design
- **Placement**: Checkbox centered below each image thumbnail.
- **Style**: Modern, non-intrusive design.
    - Custom styled checkbox (using `ControlTemplate`).
    - Visible even on dark/light backgrounds.

### Functional Requirements
- **Granular Selection**:
    - Users can select specific images (e.g., only Cam 3, or only NIR).
- **Logic Synchronization**:
    - **Row -> Item**: Clicking Row Checkbox toggles ALL items in that row.
    - **Item -> Row**: Clicking Item Checkbox updates Row Checkbox to Checked/Unchecked/Indeterminate.

## 4. Assumptions
- The `FileGroup` model or ViewModel can be extended to hold properties like `IsNirSelected`, `IsCam1Selected`, etc.
- `DeleteManager` logic in C# needs to be updated to inspect these properties.

---
**Status**: Draft
**Next Step**: 03_plan.md
