---
Task: Change Abnormal Detection to Ratio Based
Created: 2026-01-14
Status: Approved
Depends On: 03_plan.md
---

# Change Abnormal Detection to Ratio Based - Detailed Design

## 1. Component Designs

### 1.1 AbnormalDetectorService

> Service responsible for tracking image history and detecting anomalies based on Aspect Ratio differences.

#### Interface (from Plan)

```csharp
// Overload 2 removed. Service only requires checking with Context.
(bool IsAbnormal, double? RatioDiff) AddAndCheckImage(int width, int height, string context);
```

#### Preconditions

- `width` > 0 and `height` > 0.
- `context` is a valid non-empty string.

#### Postconditions

- Aspect Ratio (`W/H`) is added to history.
- Returns `IsAbnormal=true` if difference > Threshold.

#### Detailed Logic

```pseudo
function AddAndCheckImage(width, height, context):
    // ========== INPUT VALIDATION ==========
    if width <= 0 or height <= 0:
        return (false, null)
        
    if string.IsNullOrWhiteSpace(context):
         context = "Global" // Fallback

    // ========== CORE LOGIC ==========
    currentRatio = width / (double)height
    history = GetHistoryForContext(context)
    
    lock (history.Lock):
        history.Add(currentRatio)
        if history.Count > WindowSize:
            history.RemoveOldest()
            
        if history.Count < MinSamples:
             return (false, 0.0) 
             
        medianRatio = CalculateMedian(history)
        
    ratioDiff = Abs(currentRatio - medianRatio)
    isAbnormal = ratioDiff > Threshold
    
    if isAbnormal:
        Log.Info($"Abnormal Ratio: {currentRatio} vs Median {medianRatio}")
        
    return (isAbnormal, ratioDiff)
```

**Note**: "Adaptive Reset" logic (auto-reset after N failures) is **REMOVED** in this new version to simplify behavior. Permanent ratio changes will require manual config reset or will stabilize slowly over time if WindowSize permits.

#### Thread Safety
- **History Access**: `AddAndCheckImage` uses `lock (history.Lock)` when adding items or reading for Median calculation. This prevents race conditions where one thread writes while another reads the `List<double>`.
- **Context Creation**: `GetHistoryForContext` uses `ConcurrentDictionary.GetOrAdd`, ensuring thread-safe context creation.

---

### 1.2 ImageProcessingService

#### New Method: GetImageDimensions

> **Goal**: Avoid duplicate image parsing logic (removing `BitmapFrame` usage in VM).

```pseudo
function (int Width, int Height) GetImageDimensions(string imagePath):
    if !File.Exists(imagePath):
        return (0, 0)
        
    try:
        using stream = File.OpenRead(imagePath)
        // Use ImageSharp's lightweight Identify (reads header only)
        info = Image.Identify(stream)
        return (info.Width, info.Height)
    catch:
        return (0, 0)
```

---

### 1.3 AbnormalHistoryManager (Refactor)

#### Change Storage Structure

> **Goal**: Prevent storing unused Width/Height data. Store **Ratio** directly.

- **Old**: `Dictionary<string, List<(int W, int H)>>`
- **New**: `Dictionary<string, List<double>>` (Stores Ratio)

#### Logic Updates
- `AddAndGet(context, w, h, size)` -> `Add(context, ratio, size)`
- `GetHistory(context)` returns `List<double>`

---

### 1.4 FileGroupViewModel

#### Update CheckAbnormalStatus

> **Goal**: Use centralized helper, remove dead fields.

```pseudo
function CheckAbnormalStatus(imagePath):
    // 1. Use Service Helper (No more BitmapFrame here)
    (width, height) = _imageProcessor.GetImageDimensions(imagePath)
    
    if width == 0 or height == 0:
         return

    context = DetermineContext() 
    (isAbnormal, ratioDiff) = _abnormalDetector.AddAndCheckImage(width, height, context)
    
    // 2. Cache & Label
    _cachedWidth = width
    _cachedHeight = height
    _cachedRatioDiff = ratioDiff 
    
    if isAbnormal:
        IsAbnormal = true
        _abnormalReason = $"Abnormal Ratio Conflict (Diff: {ratioDiff:F3})"
    else:
        IsAbnormal = false
```

#### Cleanup
- Rename `_cachedHeightDeviation` -> `_cachedRatioDiff`
- Remove dependent logic on percentage fields.

---

### 1.5 Configuration Manager (Migration Logic)

#### Detailed Logic (Loading)

```pseudo
function LoadConfiguration():
    config = ReadJson()
    if config.AbnormalRatioThreshold is default (0.0):
        config.AbnormalRatioThreshold = 0.3
    return config
```

---

## 2. Integration Points

### 2.1 FileGroupVM -> ImageProcessor -> AbnormalDetector

```
1. FileGroupVM calls ImageProcessor.GetImageDimensions()
2. FileGroupVM calls AbnormalDetector.AddAndCheckImage()
```

## 3. Edge Cases

| Case | Input | Expected |
|------|-------|----------|
| **Zoomed Image** | Same Ratio | Normal (Diff 0.0) |
| **Rotated Image** | Ratio Inverted | Abnormal (Diff > 0.3) |

## 4. Testing Strategy

### 4.1 Unit Tests
- `AbnormalDetectorServiceTests`: Verify Ratio logic.
- `AbnormalHistoryManagerTests`: Verify Ratio storage.

### 4.2 Manual Verification
- Check UI for new Label format ("Ratio Diff").
- Verify no errors when old history file load attempts (might need try/catch in HistoryManager load).

---

## 5. Open Questions
- [x] All design questions resolved.

## Approval
- [x] Detailed pseudo-code provided
- [x] Thread safety addressed
- [x] Edge cases covered
- [x] **Spaghetti/Duplication risks addressed** (Centralized Image Load, Clean History)

**Next Step**: 05_tasks.md
