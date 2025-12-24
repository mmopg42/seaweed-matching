---
Task: remove_nir_spectrum_monitor
Created: 2025-12-23
Status: Draft
Depends On: 01_requirements.md
---

# Remove NIR Spectrum Monitor - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| `NirSpectrumMonitor.cs` file is deleted | File deletion task | Verify file does not exist |
| DI registration removed from `App.xaml.cs` | Modify `App.xaml.cs` | Build succeeds, grep shows no references |
| References removed from `SetupWindowViewModel.cs` | Modify `SetupWindowViewModel.cs` | Build succeeds, grep shows no references |
| Application builds successfully | Build verification | `dotnet build` succeeds |
| NIR filtering functionality remains intact | Manual test | Toggle NIR filtering in SetupWindow works |
| No unused code remains | Code review | Grep search returns no hits |

---

## 1. Architecture Overview

### 1.1 System Context

This is a code cleanup task to remove dead code. `NirSpectrumMonitor` was injected but never used. The actual NIR filtering functionality is provided by `NirSpectrumFilter` which is actively used by `Nir2CameraLauncher`.

### 1.2 Component Diagram

```
BEFORE:
┌─────────────────────────────────────────┐
│         SetupWindowViewModel            │
├─────────────────────────────────────────┤
│  - NirSpectrumMonitor (DEAD CODE)       │
│  - Nir2CameraLauncher                   │
│      └─► uses NirSpectrumFilter ✓       │
└─────────────────────────────────────────┘

AFTER:
┌─────────────────────────────────────────┐
│         SetupWindowViewModel            │
├─────────────────────────────────────────┤
│  - Nir2CameraLauncher                   │
│      └─► uses NirSpectrumFilter ✓       │
└─────────────────────────────────────────┘
```

---

## 2. Components

### 2.1 New Components

None - this is a deletion task.

### 2.2 Modified Components

| Component | Location | Changes | Breaking Change? |
|-----------|----------|---------|------------------|
| `App.xaml.cs` | `ChronoView/App.xaml.cs` | Remove DI registration (line 217) | No |
| `SetupWindowViewModel` | `ChronoView/UI/ViewModels/SetupWindowViewModel.cs` | Remove field, constructor parameter, unused method | No |

### 2.3 Deleted Components

| Component | Location | Reason | Migration |
|-----------|----------|--------|-----------|
| `NirSpectrumMonitor` | `ChronoView/Core/NIR/NirSpectrumMonitor.cs` | Dead code, never used | None needed |

---

## 3. Interface Definitions

No new interfaces - only deletions.

---

## 4. Key Design Decisions

### 4.1 Delete vs Refactor

**Context**: Should we complete the TODO implementation or delete the class?

| Option | Pros | Cons |
|--------|------|------|
| Complete implementation | Fulfill original intention | No clear requirement, Filter already works |
| Delete the class | Clean codebase, no maintenance burden | Lose potential future code |

**Decision**: Delete

**Rationale**: 
- No active requirement for this functionality
- `NirSpectrumFilter` provides complete NIR filtering
- Dead code creates maintenance burden
- If needed in future, can be restored from git history

---

## 5. Configuration

No configuration changes needed.

---

## 6. External Dependencies

No dependency changes needed.

---

## 7. Glossary Updates

### Existing Terms Check

| Checked | Existing Term | Relevance |
|---------|---------------|-----------|
| [x] | `NirSpectrumFilter` | The correct implementation to keep |
| [x] | `Nir2CameraLauncher` | Uses NirSpectrumFilter |

### New Terms to Add

None - this is a deletion task.

### Terms to Update

None

---

## 8. Architecture Documentation Plan

### Documents to Create

None

### Documents to Update

None required - this is internal code cleanup with no contract changes.

---

## 9. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Accidental deletion of wrong file | Low | High | Double-check file paths before delete |
| Missing references | Low | Medium | Grep search before and after |
| Build fails | Low | Low | Build after each change |

---

## 10. Open Questions

- [x] All questions resolved

---

## Approval

- [x] All requirements traced to components
- [x] Component interfaces defined (N/A for deletion)
- [x] Design decisions documented with rationale
- [x] Glossary terms identified
- [x] All open questions resolved

**Next Step**: 04_design.md
