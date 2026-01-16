---
Task: cam_comparison_option
Created: 2026-01-14
Status: Draft
Depends On: 01_requirements.md, 02_research.md
---

# Camera Comparison Option - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| Add "Compare to Cam1/4" option in Settings | `SettingsDialog` + `SettingsDialogViewModel` | Manual verification in Settings UI |
| Condition matches Cam2/3 to Cam1 (if enabled) | `FileMatchingEngine` | Unit Test logic simulation |
| Condition matches Cam5/6 to Cam4 (if enabled) | `FileMatchingEngine` | Unit Test logic simulation |
| Fallback to sequential if Cam1/4 disabled/missing | `FileMatchingEngine` (Fallback logic) | Unit Test with disabled Cam1 |
| Use configured Item Delay against Reference | `FileMatchingEngine` (Delay calc) | Unit Test with custom delays |
| Persist setting | `DataSequenceSettings` + `xml/json` persistence | Restart app and check setting |

---

## 1. Architecture Overview

### 1.1 System Context

The `FileMatchingEngine` currently matches files based on the defined sequence order. Line 2 (Cam4/5/6) reuses `DataType.Cam1/2/3` internally. This change introduces a conditional branch where `Cam2` and `Cam3` types can be matched directly against `Cam1` (the reference) instead of their immediate predecessor.

### 1.2 Data Flow

```
[Unmatched Files]
    │
    ▼
[FileMatchingEngine.BuildLineGroupsWithOrder]
    │
    ├─► Iterate Ordered Sequence Items
    │      │
    │      ▼
    │   [Process Item (e.g. Cam2)]
    │      │
    │      ├─► Legacy/Default: Reference = Prev Item (Cam1)
    │      │
    │      │
    │      └─► NEW Option Enabled:
    │             ├─► Check Reference (Cam1) Availability in OrderedTypes
    │             │      ├─► Cam1 Available in Sequence: Reference = Cam1
    │             │      └─► Else: Reference = nearest enabled preceding item (index-based)
    │             │
    │             └─► Calculate Diff (Item.Timestamp - Reference.Timestamp)
    │
    ▼
[Matched Groups]
```

---

## 2. Components

### 2.1 Modified Components

| Component | Location | Changes | Breaking Change? |
|-----------|----------|---------|------------------|
| `DataSequenceSettings` | `Models/DataSequenceSettings.cs` | Add `bool CompareToReferenceCamera` property | No |
| `SettingsDialogViewModel` | `UI/ViewModels/SettingsDialogViewModel.cs` | Add binding property `CompareToReferenceCamera` | No |
| `SettingsDialog` | `UI/Views/SettingsDialog.xaml` | Add CheckBox to Sequence Tab | No |
| `ApplicationConfiguration` | `Models/ApplicationConfiguration.cs` | No change needed (uses DataSequenceSettings) | No |
| `FileMatchingEngine` | `Core/FileMatching/FileMatchingEngine.cs` | Implement reference logic in `BuildLineGroupsWithOrder` | No |

---

## 3. Interface Definitions

### 3.1 DataSequenceSettings

```csharp
public class DataSequenceSettings
{
    // ... existing properties ...

    /// <summary>
    /// If true, Cam2/Cam3 will be compared to Cam1 (and Cam5/6 to Cam4)
    /// instead of their immediate predecessor in the sequence.
    /// Default: false
    /// </summary>
    public bool CompareToReferenceCamera { get; set; } = false;
}
```

### 3.2 SettingsDialogViewModel

```csharp
public class SettingsDialogViewModel : ViewModelBase
{
    // ... existing properties ...
    
    public bool CompareToReferenceCamera
    {
        get => _configuration.DataSequenceSettings.CompareToReferenceCamera;
        set 
        {
             _configuration.DataSequenceSettings.CompareToReferenceCamera = value;
             OnPropertyChanged();
        }
    }
}
```

---

## 4. Key Design Decisions

### 4.1 Global vs Per-Item Setting

**Context**: Should this be a global flag "Compare All to Reference" or a per-item setting "Compare THIS to Cam1"?

**Decision**: Global Flag (`CompareToReferenceCamera`) in `DataSequenceSettings`.

**Rationale**: 
1. The requirement is specific to the "Cam2/3 vs Cam1" and "Cam5/6 vs Cam4" pattern.
2. It simplifies the UI (one checkbox vs multiple).
3. Covers the user's requested use case without over-engineering per-item comparisons.

### 4.2 Handling "Cam1 Disabled" Case

**Context**: What if the user checks the box but disables Cam1?

**Decision**: Fall back to "Sequential Matching" (Predecessor) behavior.

**Rationale**: 
1. Cannot compare to a non-existent reference.
2. Silent failure or skipping validation would be worse.
3. Allows "graceful degradation".
4. User requested this logic explicitly.

---

## 5. Configuration

### 5.1 New Configuration Keys

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `DataSequenceSettings.CompareToReferenceCamera` | bool | false | Enable reference camera comparison |

---

## 6. External Dependencies

None.

---

## 7. Glossary Updates

None required.

---

## 8. Verification Plan

### 8.1 Automated Unit Tests (New Tests in `FileMatchingEngineTests.cs`)

We will create a new test file or add methods to existing tests.

**Test Case 1: Option Disabled (Default)**
- Setup: Sequence [Normal, Cam1, Cam2], Option=False.
- Data: Normal@0s, Cam1@2s, Cam2@4s. (Diffs: Cam1-N=2, Cam2-C1=2).
- Delays: Min 0, Max 3.
- Verify: All matched. (Cam2 matched because 2s <= 3s).

**Test Case 2: Option Enabled (Reference Match)**
- Setup: Sequence [Normal(1), Cam1(2), Cam2(3)], Option=True.
- Delays: Cam2 has Min 3, Max 5. (Expects to be 3-5s after Reference).
- Data: Normal@0s, Cam1@2s, Cam2@6s.
- Logic:
  - Cam2 vs Prev(Cam1): Diff = 4s.
  - Cam2 vs Ref(Cam1): Diff = 4s. (Wait, this example is bad because Ref==Prev).
- **Better Scenario**: 
  - Sequence: Cam1 -> Cam2 -> Cam3.
  - Option=True. Ref for Cam3 is Cam1.
  - Data: Cam1@0s, Cam2@2s, Cam3@4s.
  - Cam3 Settings: Min=3, Max=5.
  - **With Option=False (Sequential)**: Cam3 vs Cam2 (Diff=2s). OUT of range (3-5). Match FAIL.
  - **With Option=True (Ref)**: Cam3 vs Cam1 (Diff=4s). IN range (3-5). Match SUCCESS.
- Verify: Cam3 matches only when Option=True.

**Test Case 3: Option Enabled but Cam1 Missing (Fallback)**
- Setup: Sequence [Normal, Cam3, Cam2], Option=True (Cam1 missing).
- Logic:
  - Cam3 fallbacks to Normal (Nearest Preceding).
  - Cam2 fallbacks to Normal (Nearest Preceding).
- Verify: Comparison logic respects the valid preceding item as the reference when the primary reference (Cam1) is missing.

---

## 9. Open Questions

- [x] UI Label? -> "Compare Cam2/3 (or Cam5/6) timestamps directly to Cam1 (or Cam4)"

---

## Approval

- [ ] All requirements traced
- [ ] Interface changes defined
- [ ] Fallback logic clear

**Next Step**: 04_design.md
