---
Task: fix_nir_filtering_button_ui
Created: 2026-01-14
Status: Draft
Depends On: 01_requirements.md, 02_research.md
---

# Fix NIR Filtering Button UI - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| Button text changes color (Red/Green) based on status | `SetupWindow.xaml` (Add Binding) | Manual Visual Test |
| UI reflects actual service state on startup | `SetupWindowViewModel.cs` (Init Logic) | Manual Visual Test (Restart App) |
| UI updates immediately on toggle | `SetupWindowViewModel.cs` (Command Logic) | Manual Visual Test |

---

## 1. Architecture Overview

### 1.1 System Context

The `SetupWindow` allows users to control external programs, including the "NIR Filtering Service". This service runs in the background. The UI must accurately reflect whether this service is currently active or inactive.

### 1.2 Data Flow

```
User Clicks Button
    │
    ▼
SetupWindowViewModel (ToggleNirFilteringCommand)
    │
    ▼
NirFilteringService.Start/StopFiltering()
    │
    ▼
SetupWindowViewModel.UpdateNir2FilteringStatus()
    │ Update Properties
    ▼
[Nir2FilteringStatus] & [Nir2FilteringForeground]
    │ Binding
    ▼
SetupWindow.xaml (Button Text & Color)
```

---

## 2. Components

### 2.1 Modified Components

| Component | Location | Changes | Breaking Change? |
|-----------|----------|---------|------------------|
| `SetupWindow` | `UI/Views/SetupWindow.xaml` | Add `Foreground` binding to `Run` element inside the NIR button. | No |
| `SetupWindowViewModel` | `UI/ViewModels/SetupWindowViewModel.cs` | Add `UpdateNir2FilteringStatus()` call in constructor. | No |

---

## 3. Interface Definitions

### 3.1 SetupWindowViewModel

```csharp
public class SetupWindowViewModel : ViewModelBase
{
    // Existing Properties
    public string Nir2FilteringStatus { get; set; }
    public Brush Nir2FilteringForeground { get; set; }

    // Constructor Change
    public SetupWindowViewModel(...) 
    {
        // ... Dependencies ...
        
        // NEW: specific line to add
        UpdateNir2FilteringStatus(); 
    }
}
```

**Responsibilities**:
- Initialize UI state based on `NirFilteringService`'s current status upon creation.
- Update UI state after user toggle actions.

---

## 4. Key Design Decisions

### 4.1 Binding vs Triggers

**Context**: How to change button text color?
- **Option A**: Use XAML DataTriggers based on text content.
- **Option B**: Bind directly to a Brush property in ViewModel.

**Decision**: Option B (Direct Binding)

**Rationale**:
- ViewModel already has `Nir2FilteringForeground` property implemented.
- Keeps markup simpler and logic testable in ViewModel.
- Consistent with existing pattern in the ViewModel.

---

## 5. Configuration
No changes to configuration.

---

## 6. External Dependencies
No new dependencies.

---

## 7. Glossary Updates

### Existing Terms Check

| Checked | Existing Term | Relevance |
|---------|---------------|-----------|
| [x] | `Nir2FilteringStatus` | UI property for text |
| [x] | `Nir2FilteringForeground` | UI property for color |

---

## 8. Architecture Documentation Plan

No architecture documentation updates required as this is a bug fix within existing components.

---

## 9. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Service not initialized when ViewModel loads | Low | Low | Null check exists in `UpdateNir2FilteringStatus`. |

---

## 10. Open Questions
- [x] Should we auto-start filtering? → No, user must manually toggle.

---

## Approval

- [ ] All requirements traced to components
- [ ] Component interfaces defined
- [ ] Design decisions documented
- [ ] Glossary terms checked
- [ ] All open questions resolved
