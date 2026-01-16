---
Task: fix_anomaly_detection
Created: 2026-01-13
Status: Draft
Depends On: 03_plan.md
---

# Fix Anomaly Detection - Design

## 1. Algorithm: Independent Percent Deviation

Instead of complex Z-scores, we use a simple, stable logic that checks if the current dimensions deviate significantly from a "Clean" baseline.

### Formula
$$
Deviation\% = \frac{|x_i - \tilde{x}|}{\tilde{x}} \times 100
$$
Where:
- $\tilde{x}$ = Median of the window variables in the **Clean History**.

### Pollution Prevention Policy
To maintain a stable baseline, we strictly control what enters the history:
- $If \ Deviation\% > Threshold$:
    - Result: **ABNORMAL**.
    - Action: **Discard** (do not add to history).
- $Else$:
    - Result: **Normal**.
    - Action: **Add** to history, update baseline.

### Adaptive Reset (Drift Handling)
To handle legitimate camera changes (e.g. repositioning), the system resets if **10 consecutive** abnormalities occur.
- Count consecutive abnormalities.
- If $Count \ge 10$:
    - Clear history.
    - Start new baseline using the current value.

## 2. Component Design

### AbnormalDetectorService

```csharp
public (bool IsAbnormal, double? DevPctW, double? DevPctH) AddAndCheckImage(int width, int height, string context)
{
    // 1. Get Median from History (Context-aware)
    double medW = GetMedian(historyW[context]);
    double medH = GetMedian(historyH[context]);

    // 2. Calculate Deviation
    double devW = (Math.Abs(width - medW) / medW) * 100;
    double devH = (Math.Abs(height - medH) / medH) * 100;

    bool isAbnormal = devW > Threshold || devH > Threshold;

    // 3. Update History only if Normal OR Reset triggered
    if (!isAbnormal) 
    {
        consecutiveAbnormal[context] = 0;
        _historyManager.AddAndGet(context, width, height, WindowSize);
    }
    else 
    {
        consecutiveAbnormal[context]++;
        if (consecutiveAbnormal[context] >= 10) 
        {
            _historyManager.ClearContext(context);
            _historyManager.AddAndGet(context, width, height, WindowSize);
            isAbnormal = false; // Reset treated as new normal
        }
    }
    
    return (isAbnormal, devW, devH);
}
```

## 3. Configuration

- **Threshold**: Re-interpreted as Percentage (e.g., 12.0 means 12%).
- **WindowSize**: Default changed to **40** (Increased for better stability). *Configurable via UI*.
- **ConsecutiveLimit**: 10 (Fixed internal constant for drift detection).
