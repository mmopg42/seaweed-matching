---
Task: image_display_and_line_path_bugs
Created: 2026-01-09
Status: Draft
Summary: Fix image preview bug, add image logging, fix Line2 using Line1 path data
Research Required: Yes
---

# Image Display and Line Path Bugs - Requirements

## 1. Goal

### 1.1 Primary Goal

Fix three related bugs: image preview window layout issue, missing image display logs, and Line2 incorrectly using Line1 path data.

### 1.2 Success Criteria

- [ ] Double-click image preview displays correctly without layout issues
- [ ] Warning log appears in **UI Log Panel** when folder exists but no image detected
- [ ] Debug log appears in **UI Log Panel** when image is successfully displayed in DataGrid
- [ ] Line2 tab displays only Line2 path data (nir2, normal2, cam4-6)
- [ ] Line1 tab displays only Line1 path data (nir1, normal1, cam1-3)

## 2. Constraints

### 2.1 Technical Constraints

- Must use existing logging infrastructure (LogRequested event)
- Cannot change file matching algorithm logic
- Must maintain compatibility with existing UI layout

### 2.2 Business Constraints

- Quick fix required (user-facing bugs)

### 2.3 Non-Goals (Out of Scope)

- Changing thumbnail generation algorithm
- Modifying file matching time windows
- Adding new UI components

## 3. Questions to Investigate

- [ ] Q1: What causes the image preview window layout to break? (UI element sizing, binding issue?)
- [ ] Q2: Where is Normal image loading logic? (ImageCaptureService, FileGroupMediaLoader?)
- [ ] Q3: How is line number determined when loading/displaying groups? (GroupManager, FileMatchingEngine?)
- [ ] Q4: Where does Line2 tab get its data source? (DashboardViewModel.Line2Groups?)

## 4. Assumptions

- Image preview bug is a WPF layout/binding issue
- Line path mixing is a data filtering issue in ViewModel or FileMatchingEngine
- Existing logging infrastructure supports Warning and Debug levels

## 5. Dependencies

### 5.1 Blocked By

| Dependency | Status | Owner |
|------------|--------|-------|
| None | - | - |

### 5.2 Blocks

| Dependent Task | Impact if Delayed |
|----------------|-------------------|
| Production deployment | User confusion with incorrect data display |

---

## Approval

- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
- [ ] Scope boundaries are clear
- [ ] All blocking dependencies identified

**Next Step**: 02_research.md
