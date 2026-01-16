---
Task: detail_view_research
Created: 2026-01-14
Status: Draft
Summary: Remove double-click Detail View while preserving click-to-image-preview
Research Required: Yes
---

# Detail View Removal (Keep Image Click Preview) - Requirements

## 1. Goal

### 1.1 Primary Goal

Remove the existing "Detail View" overlay feature triggered by **double-clicking** a file group, while ensuring the **image click -> large image preview** feature continues to work unchanged.

### 1.2 Success Criteria

- [ ] Double-clicking a file group row no longer opens any "detail overlay" UI.
- [ ] Clicking any image thumbnail still opens a large image preview window/dialog (must not regress).
- [ ] Identify and remove the exact files/bindings/commands responsible for the double-click Detail View.
- [ ] Document findings and boundaries so we don't accidentally remove the click-to-preview feature (`02_research.md`).
- [ ] Produce an implementation plan with explicit “keep vs delete” scope (`03_plan.md`).

## 2. Constraints

### 2.1 Technical Constraints

- Must preserve the existing image click preview mechanism and command names/bindings (unless explicitly re-wired with equivalent behavior).
- Investigation must cover both XAML and Code-behind because the double-click trigger can exist in both.

### 2.2 Non-Goals (Out of Scope)

- Redesigning image preview UX (zoom/rotate/etc.)
- Adding new features to the file group list
- Refactoring unrelated UI/viewmodels beyond what is necessary to remove the detail overlay safely

## 3. Questions to Investigate

- [ ] Q1: Which code/XAML implements the double-click Detail View overlay?
- [ ] Q2: What is the click-to-image-preview feature implementation, and where is it bound?
- [ ] Q3: What must remain to ensure click-to-preview still works after removing the overlay?

## 4. Assumptions

- The double-click detail overlay exists and is currently working.
- The image click preview feature exists and must remain.

## 5. Dependencies

None.
