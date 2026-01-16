---
Task: fix_delete_logic
Created: 2026-01-08
Status: Approved
Depends On: 03_plan.md
---

# Fix Delete Logic - Detailed Design

## Design Decisions

### Property Strategy: Option A (Keep + Add)

| Property | Status | Purpose |
|----------|--------|---------|
| `IsAnyPartialSelected` | **KEEP** | UI state (button enable/disable) - checks checkbox state only |
| `HasAnySelectedComponent` | **NEW** | Delete logic - checks checkbox AND data existence |
| `IsFullySelected` | **NEW** | Classification - all available components selected |

**Rationale**:
- `IsAnyPartialSelected` is useful for **UI responsiveness** (instant button feedback)
- `HasAnySelectedComponent` ensures **correct delete behavior** (no delete if no data)
- Clear separation of concerns: UI vs Business Logic

---

## 1. Component Designs

### 1.1 FileGroupViewModel

> From 03_plan.md: Decouple child/parent selection and add helpers.

#### Interface

```csharp
// Existing (KEEP)
public bool IsSelected { get; set; }
public bool IsNormalSelected { get; set; }
public bool IsAnyPartialSelected { get; }  // KEEP: UI state

// New (ADD)
public bool IsFullySelected { get; }       // NEW: All valid components selected
public bool HasAnySelectedComponent { get; } // NEW: At least one valid component selected
```

#### Detailed Logic

```pseudo
class FileGroupViewModel:
    // State
    _isSelected: bool
    _isNormalSelected: bool
    // ...

    property IsSelected:
        get: return _isSelected
        set(value):
            if SetProperty(ref _isSelected, value):
                // CONVENIENCE: Update all children
                IsNormalSelected = value
                IsNirSelected = value
                // ... all cams
                OnPropertyChanged(nameof(IsFullySelected))
                OnPropertyChanged(nameof(HasAnySelectedComponent))

    // Helper for Child Properties
    function SetChildProperty<T>(ref field, value, propertyName):
        if SetProperty(ref field, value, propertyName):
             OnPropertyChanged(nameof(IsFullySelected))
             OnPropertyChanged(nameof(HasAnySelectedComponent))
             // NO propagation to IsSelected
             return true
        return false

    property IsNormalSelected:
        get: return _isNormalSelected
        set(value): SetChildProperty(ref _isNormalSelected, value)

    property IsFullySelected:
        get: 
            return IsNormalSelected && 
                   (!HasNir || IsNirSelected) && 
                   (Camera1ImagePath == null || IsCam1Selected) &&
                   // ... check all valid components

    property HasAnySelectedComponent:
        get:
            return (IsNormalSelected) || 
                   (HasNir && IsNirSelected) ||
                   // ...
```

---

### 1.2 MainWindowViewModel

#### Detailed Logic

```pseudo
function ExecuteDeleteWithConfirmation():
    // 1. Filter: Only explicit Row Selection matters
    candidates = FileGroups.Where(g => g.IsSelected)
    
    // 2. Classify
    fullDeleteList = candidates.Where(g => g.IsFullySelected)
    partialDeleteList = candidates.Where(g => !g.IsFullySelected && g.HasAnySelectedComponent)
    
    // 3. Message Construction
    msg = ""
    if fullDeleteList.Any():
        msg += "Full Delete: " + fullDeleteList.Count
    
    if partialDeleteList.Any():
        msg += "Partial Delete: " + partialDeleteList.Count
        
    // 4. Execution
    // Pass ALL candidates. The DeleteService uses GetSelectedComponents() so it handles partials natively.
    Operations.ExecuteDeleteAsync(candidates)
```

---

## 4. Testing Strategy

### 4.1 Unit Test Cases

| Test Name | Input | Expected | Verifies |
|-----------|-------|----------|----------|
| `IsSelected_SetsChildren` | IsSelected=True | All Children=True | Convenience |
| `ChildUncheck_DoesNotAffectParent` | IsSelected=True, Child=False | IsSelected=True | Independence |
| `IsFullySelected_ReturnsCorrectly` | Mixed Child States | True only if ALL valid are true | Logic |

---
