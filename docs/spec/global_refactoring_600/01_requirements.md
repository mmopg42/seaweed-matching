---
Task: global_refactoring_600
Created: 2026-01-05
Status: Draft
Summary: Refactor large service/viewmodel files (>600 lines) to improve maintainability and follow rules.
Research Required: Yes
---

# 600-line Rule Refactoring - Requirements

## 1. Goal

### 1.1 Primary Goal

Refactor and split large source files (`FileOperationService.cs`, `MainWindowViewModel.cs`, `FileGroupViewModel.cs`) to ensure each file contains fewer than 800 lines (target 600) while maintaining all existing functionality and external contracts.

### 1.2 Success Criteria

- [ ] `FileOperationService.cs` and its derived files each have ≤ 800 lines.
- [ ] `MainWindowViewModel.cs` and its derived files each have ≤ 800 lines.
- [ ] `FileGroupViewModel.cs` and its derived files each have ≤ 800 lines.
- [ ] All existing unit tests pass after refactoring.
- [ ] Application build is successful without warnings related to missing dependencies or broken references.
- [ ] No regression in core workflows (Start/Stop, Move, Delete, Refresh).

## 2. Constraints

### 2.1 Technical Constraints

- **Spec-Lock**: Code modification is prohibited for these modules until `05_tasks.md` is approved.
- **Contract Integrity**: External interfaces (e.g., `IFileOperationService`) should ideally remain unchanged to minimize ripple effects, or be updated consistently across the codebase.
- **WPF/XAML**: Refactoring `MainWindowViewModel` must not break XAML bindings.
- **Line Limit**: Hard limit of 800 lines, soft limit of 600 lines as per `operational-guidelines.md`.

### 2.2 Business Constraints

- Refactoring should not introduce downtime or major bugs in the monitoring process.
- User interface (UX) must remain identical or improved.

### 2.3 Non-Goals (Out of Scope)

- Implementing new features (except for required architectural changes).
- Performance optimization (unless naturally occurring from better structure).
- Complete rewrite of the logic (focus is on structural splitting).

## 3. Questions to Investigate

- [ ] Q1: How many commands in `MainWindowViewModel` can be extracted into separate `ICommand` implementations or dedicated services?
- [ ] Q2: What are the primary logical dimensions in `FileOperationService` (Move vs Delete vs Utils)?
- [ ] Q3: Can the thumbnail loading and retry logic in `FileGroupViewModel` be decoupled into a separate component?
- [ ] Q4: Are there shared utility methods that can be moved to a common `Helpers` or `Core.Utils` namespace?

## 4. Assumptions

- The current behavior is correct and serves as the baseline for verification.
- Splitting ViewModels might require `Partial` classes or extraction of sub-ViewModels; the choice depends on complexity and binding impacts.

## 5. Dependencies

### 5.1 Blocked By

| Dependency | Status | Owner |
|------------|--------|-------|
| None | - | - |

### 5.2 Blocks

| Dependent Task | Impact if Delayed |
|----------------|-------------------|
| Move Logic Refinement | High (Harder to implement in a 2,800-line file) |

---

## Approval

- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
- [ ] Scope boundaries are clear
- [ ] All blocking dependencies identified

**Next Step**: [02_research.md]
