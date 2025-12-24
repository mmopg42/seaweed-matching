---
Task: upgrade_nir_filtering_algorithm
Created: 2025-12-23
Status: Draft
Depends On: 04_design.md
---

# Upgrade NIR Filtering Algorithm - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Core | 3 | 0 | 3 |
| Testing | 2 | 0 | 2 |
| Documentation | 1 | 0 | 1 |
| Verification | 3 | 0 | 3 |
| **Total** | **9** | **0** | **9** |

---

## Phase 1: Core Implementation

### 1.1 Update FilterResult Class

- [ ] Open `ChronoView/Core/NIR/NirSpectrumFilter.cs`
- [ ] Remove `YVariationRegion` class (lines 14-19)
- [ ] Remove old `Regions` property from `FilterResult`
- [ ] Add new `CriteriaResult` class:
  ```csharp
  public class CriteriaResult
  {
      public bool Passed { get; set; }
      public double Value { get; set; }
      public double Threshold { get; set; }
  }
  ```
- [ ] Add new properties to `FilterResult`:
  ```csharp
  public int CriteriaPassed { get; set; } = 0;
  public int CriteriaTotal { get; set; } = 5;
  public Dictionary<string, CriteriaResult>? CriteriaDetails { get; set; } = new();
  ```

**Verify**: Build succeeds, no compilation errors

---

### 1.2 Implement New EvaluateSeaweedPresence Method

- [ ] Remove old `FindYVariationRegions()` method (lines 77-136)
- [ ] Add new private method `EvaluateSeaweedPresence(double[] wavelengths, double[] intensities)`
- [ ] Implement algorithm per 04_design.md pseudo-code:
  - [ ] Filter data to X range 4500-6500
  - [ ] Calculate y_range (max - min)
  - [ ] Calculate y_std (standard deviation)
  - [ ] Sliding window loop (window=800, stride=100)
  - [ ] Calculate window_800_mean, window_800_std, window_800_max
  - [ ] Evaluate all 5 criteria
  - [ ] Apply 4/5 pass rule
  - [ ] Return FilterResult with details

**Verify**: Code compiles without errors

---

### 1.3 Update AnalyzeSpectrum Method

- [ ] Replace call to `FindYVariationRegions()` with `EvaluateSeaweedPresence()`
- [ ] Remove region-based logic
- [ ] Ensure error handling remains intact

**Verify**: Build succeeds

---

## Phase 2: Testing

### 2.1 Create Unit Test

- [ ] Create `ChronoView.Tests/Core/NIR/NirSpectrumFilterTests.cs`
- [ ] Add test: `Test_AllCriteriaPass_ReturnsTrue()`
  - Create test data where all 5 criteria pass
  - Expected: PassesFilter=true, CriteriaPassed=5
- [ ] Add test: `Test_Exactly4Pass_ReturnsTrue()`
  - Create test data where exactly 4 criteria pass
  - Expected: PassesFilter=true, CriteriaPassed=4
- [ ] Add test: `Test_Only3Pass_ReturnsFalse()`
  - Create test data where only 3 criteria pass
  - Expected: PassesFilter=false, CriteriaPassed=3
- [ ] Add test: `Test_EmptyData_ReturnsFalse()`
  - Pass empty arrays
  - Expected: PassesFilter=false, appropriate error message

**Verify**:
```bash
cd ChronoView.Tests
dotnet test --filter "FullyQualifiedName~NirSpectrumFilterTests" -v:n
```

---

### 2.2 Integration Test

- [ ] Open `ChronoView.Tests/Integration/CoreServicesIntegrationTests.cs` (or create if needed)
- [ ] Add test: `Test_Nir2CameraLauncher_UsesNewAlgorithm()`
  - Create test NIR .txt file with known spectrum
  - Mock Nir2CameraLauncher to call AnalyzeSpectrum
  - Verify FilterResult uses new criteria format

**Verify**:
```bash
cd ChronoView.Tests
dotnet test --filter "FullyQualifiedName~CoreServicesIntegrationTests" -v:n
```

---

## Phase 3: Documentation

### 3.1 Update Glossary

- [ ] Open `docs/architecture/glossary.md`
- [ ] Add new terms:
  - `CriteriaResult`: Individual criterion evaluation result with value, threshold, and pass/fail status
  - `y_range`: Overall Y intensity range in wavelength window 4500-6500nm
  - `y_std`: Standard deviation of Y intensities in wavelength window
  - `window_800_mean`: Mean of Y ranges across all 800nm sliding windows
  - `window_800_std`: Standard deviation of Y ranges across windows
  - `window_800_max`: Maximum Y range found in any 800nm window

**Verify**: File saved correctly

---

## Phase 4: Final Verification

### 4.1 Build Verification

- [ ] Build solution
- [ ] Verify no build errors
- [ ] Verify no new warnings

```bash
cd c:\workspace\seaweed\gui_kiro_v2
dotnet build gui_kiro.sln --nologo -v:minimal
```

**Expected**: Exit code 0, 0 errors

---

### 4.2 Unit Tests Pass

- [ ] Run all NirSpectrumFilter unit tests

```bash
cd ChronoView.Tests
dotnet test --filter "FullyQualifiedName~NirSpectrumFilter" -v:n
```

**Expected**: All tests pass

---

### 4.3 Manual Verification

> **IMPORTANT**: User assistance required for manual testing

**Steps to manually test**:
1. Run ChronoView application
2. Open SetupWindow
3. Toggle "NIR2 Filtering" ON
4. Place test NIR .txt files in monitored folder
5. Check application logs for new message format: "김 있음 (X/5 기준 통과)"

**Expected**: 
- Logs show new scoring format (not old "regions found" messages)
- Files are correctly classified as seaweed/no-seaweed based on 4/5 rule

---

## Phase 5: Success Criteria Check

| Criterion (from requirements) | Status | Evidence |
|-------------------------------|--------|----------|
| Algorithm evaluates all 5 criteria | ⬜ | Code inspection |
| Pass threshold is 4 out of 5 | ⬜ | Code logic + unit test |
| FilterResult includes scoring details | ⬜ | FilterResult class inspection |
| Existing integration remains functional | ⬜ | Integration test + manual test |
| Matches Python reference | ⬜ | Algorithm port validation |
| Build succeeds | ⬜ | `dotnet build` exit code 0 |

---

## Completion Log

| Task | Completed | Duration | Notes |
|------|-----------|----------|-------|
| 1.1 | - | - | - |
| 1.2 | - | - | - |
| 1.3 | - | - | - |
| 2.1 | - | - | - |
| 2.2 | - | - | - |
| 3.1 | - | - | - |
| 4.1 | - | - | - |
| 4.2 | - | - | - |
| 4.3 | - | - | - |

---

## Blockers & Issues

| Issue | Impact | Resolution |
|-------|--------|------------|
| - | - | - |

---

## Approval

- [ ] All tasks completed
- [ ] All verifications pass
- [ ] Success criteria met
- [ ] Ready for 06_report.md

**Next Step**: Implementation, then 06_report.md
