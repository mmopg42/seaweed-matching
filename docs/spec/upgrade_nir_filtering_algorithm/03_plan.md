---
Task: upgrade_nir_filtering_algorithm
Created: 2025-12-23
Status: Draft
Depends On: 01_requirements.md
---

# Upgrade NIR Filtering Algorithm - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| Algorithm evaluates all 5 criteria | New `EvaluateSeaweedPresence()` method | Unit test with sample data |
| Pass threshold is 4 out of 5 | Pass logic in `EvaluateSeaweedPresence()` | Unit test scenarios |
| FilterResult includes scoring details | Extended `FilterResult` class | Code inspection + test |
| Existing integration remains functional | Maintain `AnalyzeSpectrum()` API | Integration test with Nir2CameraLauncher |
| Matches Python reference | Algorithm port validation | Compare outputs on same data |
| Build succeeds | Code compilation | `dotnet build` |

---

## 1. Architecture Overview

### 1.1 System Context

This upgrades the core NIR filtering algorithm within `NirSpectrumFilter`. The class is used by `Nir2CameraLauncher` to determine if detected NIR files contain seaweed. The upgrade changes internal logic only—external contracts remain unchanged.

### 1.2 Component Diagram

```
NirSpectrumFilter (MODIFIED)
├─► AnalyzeSpectrum(file) [PUBLIC - unchanged API]
│   └─► NirSpectrumParser.Parse() [existing]
│   └─► EvaluateSeaweedPresence() [NEW - 5-criteria]
│       ├─► Calculate y_range
│       ├─► Calculate y_std
│       ├─► Calculate window_800_mean
│       ├─► Calculate window_800_std
│       └─► Calculate window_800_max
└─► FilterResult [EXTENDED - add scoring details]
```

---

## 2. Components

### 2.1 New Components

None - only modifying existing `NirSpectrumFilter`.

### 2.2 Modified Components

| Component | Location | Changes | Breaking? |
|-----------|----------|---------|-----------|
| `NirSpectrumFilter` | `Core/NIR/NirSpectrumFilter.cs` | Replace algorithm, add new method | No (API unchanged) |
| `FilterResult` | `Core/NIR/NirSpectrumFilter.cs` | Add criteria details | No (additive) |

### 2.3 Deleted Components

| Component | Reason |
|-----------|--------|
| `FindYVariationRegions()` | Replaced by new 5-criteria algorithm |
| `YVariationRegion` class | No longer needed |

---

## 3. Interface Definitions

### 3.1 NirSpectrumFilter (Modified)

```csharp
public static class NirSpectrumFilter
{
    // PUBLIC API - UNCHANGED
    public static FilterResult AnalyzeSpectrum(string txtFilePath)
    
    // NEW PRIVATE METHOD
    private static FilterResult EvaluateSeaweedPresence(
        double[] wavelengths, 
        double[] intensities)
}
```

**Responsibilities**:
- Parse NIR spectrum files
- Apply 5-criteria scoring algorithm
- Return pass/fail result with details

**Does NOT**:
- Modify files
- Handle file watching
- Make UI decisions

---

### 3.2 FilterResult (Extended)

```csharp
public class FilterResult
{
    // EXISTING
    public bool PassesFilter { get; set; }
    public string Message { get; set; }
    
    // NEW - detailed scoring
    public int CriteriaPassed { get; set; }
    public int CriteriaTotal { get; set; }
    public Dictionary<string, CriteriaResult> CriteriaDetails { get; set; }
}

public class CriteriaResult
{
    public bool Passed { get; set; }
    public double Value { get; set; }
    public double Threshold { get; set; }
}
```

**Backwards Compatibility**: Existing code only uses `PassesFilter` and `Message`, so adding new properties is safe.

---

## 4. Key Design Decisions

### 4.1 Keep Public API vs Create New Method

**Context**: Should we change `AnalyzeSpectrum()` signature or keep it?

| Option | Pros | Cons |
|--------|------|------|
| Keep API unchanged | No breaking changes, easy upgrade | Less explicit about new behavior |
| Add new method | Clear separation | Breaking change, migration needed |

**Decision**: Keep API unchanged

**Rationale**: 
- `Nir2CameraLauncher` already calls `AnalyzeSpectrum()`
- Internal algorithm change doesn't require API change
- New `FilterResult` properties provide details without breaking existing usage

---

### 4.2 Threshold Values

**Decision**: Use exact Python thresholds

| Criterion | Threshold |
|-----------|-----------|
| y_range | ≥ 0.035 |
| y_std | ≥ 0.010 |
| window_800_mean | ≥ 0.025 |
| window_800_std | ≥ 0.010 |
| window_800_max | ≥ 0.035 |

**Rationale**: Python code is validated with real data. Don't change proven values.

---

### 4.3 Window Parameters

**Decision**: 
- Window size: 800nm
- Stride: 100nm (changed from 50nm to match Python)
- X range: 4500-6500nm

**Rationale**: Match Python reference exactly for consistency.

---

## 5. Configuration

No new configuration needed. Algorithm parameters are hardcoded (matching Python).

---

## 6. External Dependencies

No new dependencies required.

---

## 7. Glossary Updates

### Existing Terms Check

| Checked | Term | Relevance |
|---------|------|-----------|
| [x] | `NirSpectrumFilter` | Being modified |
| [x] | `Nir2CameraLauncher` | Uses this filter |

### New Terms to Add

| Term | Type | Definition |
|------|------|------------|
| `CriteriaResult` | Class | Individual criterion evaluation result |
| `y_range` | Metric | Overall Y intensity range in wavelength window |
| `y_std` | Metric | Standard deviation of Y intensities |
| `window_800_mean` | Metric | Mean of Y ranges across 800nm windows |
| `window_800_std` | Metric | Std dev of Y ranges across windows |
| `window_800_max` | Metric | Maximum Y range found in any window |

---

## 8. Architecture Documentation Plan

### Documents to Update

| Document | Changes |
|----------|---------|
| `glossary.md` | Add 6 new terms |
| `README.md` | Update index if NIR section exists |

No new architecture documents needed (algorithm change only).

---

## 9. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Algorithm less accurate than Python | Low | High | Port algorithm exactly, test with same data |
| Breaking existing integration | Low | Medium | Maintain API, add integration test |
| Performance degradation | Low | Low | Similar complexity to old algorithm |

---

## 10. Open Questions

- [x] All questions resolved

---

## Approval

- [x] All requirements traced to components
- [x] Component interfaces defined
- [x] Design decisions documented with rationale
- [x] Glossary terms identified
- [x] All open questions resolved

**Next Step**: 04_design.md
