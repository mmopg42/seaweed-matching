---
Task: detail_view_research
Created: 2026-01-14
Status: Draft
Depends On: 01_requirements.md, 02_research.md
---

# Detail View Removal (Keep Image Click Preview) - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| Double-click no longer opens detail overlay | Step 2 + 3 | Manual test + grep for `OpenDetailViewCommand` |
| Image click preview still works | Step 4 | Manual test (click thumbnails) |
| Identify/remove files/bindings/commands for overlay | Step 1-3 | Build + grep + runtime check |
| Document boundaries to avoid regression | `02_research.md` | Review doc section 2.0/2.4 |

## 1. Architecture Overview

### 1.1 System Context

We will remove the double-click Detail View overlay from the Main Window. Separately, we must preserve the existing behavior where clicking an image opens a large image preview window.

### 1.2 Component Diagram (Existing)

```
MainWindow (View)
   │
   ├── Overlays ──────────────────┐
   │                              ▼
   │                    DetailPreviewView
   │                           │
   │                           ▼
   └── DataContext.DetailPreviewViewModel
```

### 1.3 Component Diagram (Target)

```
MainWindow (View)
   │
   ├── FileGroupDataGrid (no double-click detail overlay)
   │
   └── Image (LeftClick) ──> OpenImagePreviewCommand ──> ImagePreviewWindow
```

## 2. Components

### 2.1 New Components
None.

### 2.2 Modified Components
- `UI/Controls/FileGroupDataGrid.xaml`: remove `LeftDoubleClick` binding to `OpenDetailViewCommand`.
- `MainWindow.xaml.cs`: remove `FileGroupRow_MouseDoubleClick` handler (dead code cleanup; no hookup found in repo search).
- `UI/ViewModels/MainWindowViewModel.cs`: remove `OpenDetailViewCommand` and `DetailPreviewVM` wiring.
- `MainWindow.xaml`: remove hosting of `<views:DetailPreviewView ... />`.

### 2.3 Deleted Components
- `UI/ViewModels/DetailPreviewViewModel.cs` (if no longer referenced)
- `UI/Views/DetailPreviewView.xaml` and `.xaml.cs` (if no longer referenced)

## 3. Interface Definitions

No public interfaces are introduced. We must preserve:
- `OpenImagePreviewCommand` signature/availability (binding contract from `SharedResources.xaml`)

## 4. Key Design Decisions

### 4.1 Preserve click-to-preview contract
**Decision**: Keep `OpenImagePreviewCommand` and its window-ancestor binding intact.
**Rationale**: `SharedResources.xaml` binds LeftClick on images to `DataContext.OpenImagePreviewCommand` on the Window.

### 4.2 Remove redundant double-click triggers
**Decision**: Remove both the XAML input binding and the code-behind double-click handler.
**Rationale**: There are two paths that open the detail overlay; both must be removed to guarantee behavior is gone.

## 5. Configuration

No configuration changes.

## 6. External Dependencies

No new dependencies.

## 7. Glossary Updates

None. Existing terms map correctly.

## 8. Architecture Documentation Plan

| Document | Action |
|----------|--------|
| `docs/spec/detail_view_research/02_research.md` | Update with boundaries: delete overlay, keep click-to-preview |

## 9. Risk Assessment

### Risks
- Breaking image click preview by removing/renaming `OpenImagePreviewCommand` or changing Window DataContext.

### Mitigations
- Do not touch `OpenImagePreviewCommand` and verify `SharedResources.xaml` bindings remain.
- Manual regression: click Main/NIR/Camera thumbnails and ensure preview opens.

## 10. Open Questions

- [x] Q1: Where is it implemented? -> `DetailPreviewViewModel` & `DetailPreviewView`
- [x] Q2: How is it triggered? -> Double-click (XAML Binding & Code-behind)
- [x] Q3: What must remain? -> `OpenImagePreviewCommand` + `SharedResources.xaml` LeftClick bindings

## Approval

- [ ] All requirements traced to code changes
- [ ] Click-to-preview regression verified
