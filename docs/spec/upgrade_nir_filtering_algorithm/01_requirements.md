---
Task: upgrade_nir_filtering_algorithm
Created: 2025-12-23
Status: Draft
Summary: Upgrade NIR filtering from simple Y-range check to 5-criteria scoring system
Research Required: No
---

# Upgrade NIR Filtering Algorithm - Requirements

## 1. Goal

### 1.1 Primary Goal

Replace the current simple NIR filtering algorithm with a comprehensive 5-criteria scoring system that evaluates y_range, y_std, window_800_mean, window_800_std, and window_800_max to improve seaweed detection accuracy.

### 1.2 Success Criteria

- [ ] Algorithm evaluates all 5 criteria: y_range, y_std, window_800_mean, window_800_std, window_800_max
- [ ] Pass threshold is 4 out of 5 criteria met
- [ ] `FilterResult` includes detailed scoring information (which criteria passed/failed)
- [ ] Existing NIR filtering integration remains functional
- [ ] Algorithm matches Python reference implementation behavior
- [ ] Build succeeds with no new errors

## 2. Constraints

### 2.1 Technical Constraints

- Must maintain existing public API of `NirSpectrumFilter.AnalyzeSpectrum()`
- Must work with existing `NirSpectrumParser` output format
- Must integrate seamlessly with `Nir2CameraLauncher`
- Window size remains 800, stride remains 100

### 2.2 Business Constraints

- Algorithm must be based on proven Python implementation
- Should complete quickly (must not delay real-time processing)

### 2.3 Non-Goals (Out of Scope)

- Changing the UI for NIR filtering control
- Modifying `NirSpectrumParser` logic
- Changing file watching or file matching logic
- Performance optimization beyond algorithm change
- Adding machine learning or AI-based classification

## 3. Background

### Current Algorithm (Simple)

The current `NirSpectrumFilter` uses a single criterion:
- Sliding window (800nm, stride 50nm) over X range 4500-6500
- Detects regions where **Y range is between 0.05 and 0.1**
- **Problem**: Too simple, likely produces false positives/negatives

### New Algorithm (5-Criteria Scoring)

Based on Python `test_folder.py`, the new algorithm evaluates:

| # | Criterion | Threshold | Description |
|---|-----------|-----------|-------------|
| 1 | `y_range` | ≥ 0.035 | Overall Y range in 4500-6500 |
| 2 | `y_std` | ≥ 0.010 | Standard deviation of Y values |
| 3 | `window_800_mean` | ≥ 0.025 | Mean of Y ranges across all 800nm windows |
| 4 | `window_800_std` | ≥ 0.010 | Std dev of Y ranges across windows |
| 5 | `window_800_max` | ≥ 0.035 | Max Y range found in any window |

**Pass Rule**: Must pass **4 out of 5** criteria to be classified as "김 있음" (seaweed present).

### Why This Matters

- More robust detection through multi-criteria evaluation
- Proven algorithm from Python research code
- Reduces false positives by requiring consensus across metrics

## 4. Assumptions

- The Python reference implementation has been validated with real data
- Thresholds (0.035, 0.010, 0.025, etc.) are already optimized
- Existing `NirSpectrumParser` correctly reads spectrum data
- The 4-out-of-5 pass rule is the correct threshold

## 5. Dependencies

### 5.1 Blocked By

None - this is a self-contained algorithm update.

### 5.2 Blocks

None - but improves NIR filtering accuracy for users.

---

## Approval

- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
- [ ] Scope boundaries are clear
- [ ] All blocking dependencies identified

**Next Step**: 03_plan.md (skipping 02_research as algorithm is defined)
