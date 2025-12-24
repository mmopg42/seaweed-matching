---
Task: upgrade_nir_filtering_algorithm
Created: 2025-12-23
Status: Draft
Depends On: 03_plan.md
---

# Upgrade NIR Filtering Algorithm - Detailed Design

## 1. Component Designs

### 1.1 NirSpectrumFilter.AnalyzeSpectrum()

> Public method that analyzes a NIR spectrum file

#### Interface (unchanged)

```csharp
public static FilterResult AnalyzeSpectrum(string txtFilePath)
```

#### Detailed Logic

```pseudo
function AnalyzeSpectrum(txtFilePath):
    // ===== INPUT VALIDATION =====
    if txtFilePath is null or empty:
        return FilterResult { PassesFilter = false, Message = "Invalid file path" }
    
    // ===== PARSE SPECTRUM =====
    try:
        spectrum = NirSpectrumParser.Parse(txtFilePath)
        
        if spectrum is null or !spectrum.IsValid():
            return FilterResult { 
                PassesFilter = false, 
                Message = "Failed to parse spectrum or insufficient data" 
            }
        
        // ===== EVALUATE USING NEW ALGORITHM =====
        result = EvaluateSeaweedPresence(spectrum.Wavelengths, spectrum.Intensities)
        return result
        
    catch Exception ex:
        return FilterResult {
            PassesFilter = false,
            Message = "Analysis error: " + ex.Message
        }
```

---

### 1.2 NirSpectrumFilter.EvaluateSeaweedPresence() [NEW]

> Core 5-criteria algorithm (private method)

#### Interface

```csharp
private static FilterResult EvaluateSeaweedPresence(
    double[] wavelengths, 
    double[] intensities)
```

#### Detailed Logic

```pseudo
function EvaluateSeaweedPresence(wavelengths, intensities):
    // ===== STEP 1: Filter to X range (4500-6500) =====
    filteredData = []
    for i = 0 to wavelengths.length:
        if wavelengths[i] >= 4500 AND wavelengths[i] <= 6500:
            filteredData.add((wavelengths[i], intensities[i]))
    
    if filteredData.isEmpty():
        return FilterResult {
            PassesFilter = false,
            Message = "x 범위 부족 (4500-6500)",
            CriteriaPassed = 0,
            CriteriaTotal = 5
        }
    
    // Extract Y values
    yValues = filteredData.map(d => d.Y)
    
    // ===== STEP 2: Calculate Global Metrics =====
    y_range = yValues.max() - yValues.min()
    y_std = StandardDeviation(yValues)
    
    // ===== STEP 3: Sliding Window Analysis =====
    window_size = 800
    stride = 100
    
    windowRanges = []
    x_min = filteredData.min(d => d.X)
    x_max = filteredData.max(d => d.X)
    
    current_x = x_min
    while current_x + window_size <= x_max:
        // Get Y values in this window
        windowYValues = filteredData
            .where(d => d.X >= current_x AND d.X <= current_x + window_size)
            .select(d => d.Y)
        
        if windowYValues.count > 0:
            y_min_window = windowYValues.min()
            y_max_window = windowYValues.max()
            range = y_max_window - y_min_window
            windowRanges.add(range)
        
        current_x += stride
    
    if windowRanges.isEmpty():
        return FilterResult {
            PassesFilter = false,
            Message = "window 분석 실패",
            CriteriaPassed = 0,
            CriteriaTotal = 5
        }
    
    // ===== STEP 4: Calculate Window Metrics =====
    window_800_mean = Mean(windowRanges)
    window_800_std = StandardDeviation(windowRanges)
    window_800_max = Max(windowRanges)
    
    // ===== STEP 5: Evaluate 5 Criteria =====
    criteria = {
        "y_range": {
            Value: y_range,
            Threshold: 0.035,
            Passed: y_range >= 0.035
        },
        "y_std": {
            Value: y_std,
            Threshold: 0.010,
            Passed: y_std >= 0.010
        },
        "window_800_mean": {
            Value: window_800_mean,
            Threshold: 0.025,
            Passed: window_800_mean >= 0.025
        },
        "window_800_std": {
            Value: window_800_std,
            Threshold: 0.010,
            Passed: window_800_std >= 0.010
        },
        "window_800_max": {
            Value: window_800_max,
            Threshold: 0.035,
            Passed: window_800_max >= 0.035
        }
    }
    
    // ===== STEP 6: Calculate Score =====
    passed_count = criteria.count(c => c.Passed)
    total_count = 5
    
    // ===== STEP 7: Determine Pass/Fail =====
    passes_filter = passed_count >= 4
    
    message = passes_filter 
        ? $"김 있음 ({passed_count}/{total_count} 기준 통과)"
        : $"김 없음 ({passed_count}/{total_count} 기준만 통과)"
    
    return FilterResult {
        PassesFilter: passes_filter,
        Message: message,
        CriteriaPassed: passed_count,
        CriteriaTotal: total_count,
        CriteriaDetails: criteria
    }
```

---

### 1.3 FilterResult (Extended)

#### New Properties

```csharp
public class FilterResult
{
    // EXISTING (unchanged)
    public bool PassesFilter { get; set; }
    public string Message { get; set; }
    
    // REMOVED (old algorithm artifacts)
    // public List<YVariationRegion> Regions { get; set; }
    
    // NEW (scoring details)
    public int CriteriaPassed { get; set; } = 0;
    public int CriteriaTotal { get; set; } = 5;
    public Dictionary<string, CriteriaResult> CriteriaDetails { get; set; } = new();
}

public class CriteriaResult
{
    public bool Passed { get; set; }
    public double Value { get; set; }
    public double Threshold { get; set; }
}
```

---

## 2. Edge Cases & Boundary Conditions

| Case | Input | Expected Behavior |
|------|-------|-------------------|
| Empty file | No data points | `FilterResult { PassesFilter = false, Message = "Failed to parse" }` |
| No X in range 4500-6500 | All X outside range | `Message = "x 범위 부족"` |
| Exactly 4/5 pass | Criteria: [P, P, P, P, F] | PassesFilter = true (threshold met) |
| Exactly 3/5 pass | Criteria: [P, P, P, F, F] | PassesFilter = false (below threshold) |
| All zeros | All Y = 0 | All criteria fail (ranges/std all 0) |
| Very small window | X range < 800 | May have 0 windows, fail analysis |

---

## 3. Performance Considerations

| Operation | Complexity | Expected Time |
|-----------|------------|---------------|
| Parse file | O(n) | ~10ms for typical file |
| Filter to X range | O(n) | ~1ms |
| Window analysis | O(n * w) | ~5ms (n=points, w=windows) |
| Overall | O(n) | ~20ms total |

**Notes**:
- Similar complexity to old algorithm
- No performance regression expected
- Typical NIR files have ~1000-2000 data points

---

## 4. Testing Strategy

### 4.1 Unit Test Cases

| Test Name | Input | Expected Output |
|-----------|-------|-----------------|
| test_all_5_pass | Data with all criteria ≥ thresholds | PassesFilter=true, CriteriaPassed=5 |
| test_exact_4_pass | Data with exactly 4 criteria passing | PassesFilter=true, CriteriaPassed=4 |
| test_only_3_pass | Data with 3 criteria passing | PassesFilter=false, CriteriaPassed=3 |
| test_empty_file | Empty spectrum | PassesFilter=false, Message contains "parse" |
| test_no_range_data | All X outside 4500-6500 | PassesFilter=false, Message="x 범위 부족" |

### 4.2 Integration Test

```
Test: Nir2CameraLauncher uses new algorithm
1. Create test NIR .txt file with known spectrum data
2. Call Nir2CameraLauncher.StartFilteringAsync()
3. Trigger file detection
4. Verify FilterResult uses new 5-criteria logic
Expected: PassesFilter based on 4/5 rule, not old 0.05-0.1 range
```

### 4.3 Manual Verification

```
1. Setup: Run ChronoView application
2. Setup: Place test NIR files in monitored folder
3. Action: Toggle "NIR Filtering" ON in SetupWindow
4. Action: Wait for file detection
5. Verify: Check logs for new criteria messages ("김 있음 (X/5 기준 통과)")
Expected: Messages show scoring format, not old "regions found" format
```

---

## 5. Migration Notes

### Removed Code

```csharp
// DELETE these (old algorithm)
public class YVariationRegion { ... }
public static List<YVariationRegion> FindYVariationRegions(...) { ... }
```

### Updated Code

```csharp
// UPDATE FilterResult
// BEFORE: public List<YVariationRegion> Regions { get; set; }
// AFTER: public Dictionary<string, CriteriaResult> CriteriaDetails { get; set; }
```

**Backwards Compatibility**:
- `Nir2CameraLauncher` only checks `PassesFilter` boolean → unchanged contract
- New properties are additive, existing code won't break

---

## 6. Open Questions

- [x] All design questions resolved

---

## Approval

- [x] All components have detailed pseudo-code
- [x] Error handling specified
- [x] Edge cases covered
- [x] Test cases defined
- [x] No open questions

**Next Step**: 05_tasks.md
