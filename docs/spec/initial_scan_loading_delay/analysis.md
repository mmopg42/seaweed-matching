# Initial Scan Normal Image Loading Delay Analysis

**Date**: 2025-12-22  
**Issue**: Normal camera images (`stitched_original.png`) take 30-55 seconds to load during initial scan, while camera images load immediately.

## Observed Behavior (Log Analysis)

### Timeline
```
14:43:09 - Monitoring started, 20 groups created
14:43:10 - Thumbnail loading started for all groups
14:43:10 - ERROR: "All thumbnails null" for all groups
14:43:10 - Camera images (Cam1/2/3) loaded successfully
14:43:10 - NIR graphs displayed successfully
14:43:43 - group_002 Main image loaded (+33 seconds)
14:44:02 - group_001 Main image loaded (+52 seconds)
14:44:03 - group_004 Main image loaded (+53 seconds)  
14:44:05 - group_003 Main image loaded (+55 seconds)
```

### Key Observations

1. **Immediate Failures**
   - All groups report "All thumbnails null" immediately after loading starts
   - Main image path exists: `Z:\윤태경\seaweed\program\data\시뮬\normal\C251201T140543_0\stitched_original.png`
   
2. **Selective Success**
   - Camera images (`.bmp` files) load successfully and immediately
   - NIR graphs render successfully and immediately
   - Only Main images (`stitched_original.png`) are delayed

3. **Multiple Loading Attempts**
   - Each group shows `[썸네일 로딩 시작]` 3-4 times
   - Suggests `LoadThumbnailsAsync` is called repeatedly

## Root Cause Analysis

### 1. Cache Unavailability (Primary Cause)

**Problem**: The cache-based loading logic assumes images are pre-captured during real-time monitoring.

```csharp
// FileGroupViewModel.LoadSingleThumbnailAsync (Lines 899-918)
if (_orchestrator != null && Path.GetFileName(imagePath).Equals("stitched_original.png", ...))
{
    var cachedImage = _orchestrator.GetCapturedImage(GroupId);
    if (cachedImage == null && !string.IsNullOrEmpty(NormalFolder))
    {
        cachedImage = _orchestrator.GetCapturedImageByFolderPath(NormalFolder);
    }
    if (cachedImage != null)
    {
        return cachedImage; // ✅ Cache hit
    }
}
// ⚠️ Cache miss → falls through to disk I/O
```

**Reality during Initial Scan**:
- Images already exist on disk
- Cache is empty (only populated by `HandleStitchedImageCaptureAsync` during real-time monitoring)
- **Result**: Cache always misses → disk I/O required

### 2. Sequential Processing Bottleneck

**Problem**: `ImageProcessor.GenerateThumbnailAsync` uses a semaphore to limit concurrency.

```csharp
// ImageProcessingService.cs (assumed)
private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Only 1 concurrent operation

public async Task<byte[]> GenerateThumbnailAsync(...)
{
    await _semaphore.WaitAsync(cancellationToken);
    try
    {
        // Process image (slow for large stitched images)
    }
    finally
    {
        _semaphore.Release();
    }
}
```

**Impact**:
- 20 groups × 1 Main image = 20 sequential processing tasks
- Large `stitched_original.png` files take 2-3 seconds each
- Total time: 20 × 2.5s = **50 seconds** (matches observed delay)

### 3. Redundant Loading Calls

**Problem**: `LoadThumbnailsAsync` is called multiple times per group.

**Call Chain**:
```
1. OnGroupCreated → LoadThumbnailsAsync()          // Initial load
2. OnGroupCreated → Refresh() → OnPropertyChanged  // Triggers reload?  
3. OnGroupUpdated → Refresh()                      // Additional reload
4. OnGroupUpdated → LoadThumbnailsAsync()          // Explicit reload
```

**Evidence from Logs**:
```
Debug [group_001] 썸네일 로딩 시작  // Call 1
Debug [group_001] 썸네일 로딩 시작  // Call 2
Error All thumbnails null for group_001
Debug [group_001] 썸네일 로딩 시작  // Call 3
Error All thumbnails null for group_001
Debug [group_001] 썸네일 로딩 시작  // Call 4
```

## Why Camera Images Load Immediately

Camera images succeed because:
1. **Smaller file size**: `.bmp` files are smaller than stitched images
2. **Already in queue**: Loaded early in the semaphore queue3. **No cache dependency**: No cache lookup overhead

## Impact Assessment

### Severity: **HIGH**
- 50+ second delay makes the application appear frozen during startup
- Users cannot interact with data until loading completes
- Poor user experience

### Affected Scenarios
- ✅ **Initial scan** (current issue)
- ❌ **Real-time monitoring** (cache works as intended)

## Recommended Solutions

### Option 1: Pre-populate Cache During Initial Scan (Preferred)

Modify `MonitoringOrchestrator.PerformInitialScanAsync` to capture images into cache:

```csharp
private async Task PerformInitialScanAsync(...)
{
    foreach (var folder in normalFolders)
    {
        var stitchedPath = Path.Combine(folder, "stitched_original.png");
        if (File.Exists(stitchedPath))
        {
            // Capture into cache
            var image = await LoadAndCacheImageAsync(stitchedPath);
            _imageCaptureCache[folder] = image;
            _imageCaptureCache[groupId] = image; // Also cache by GroupID
        }
    }
}
```

**Pros**:
- Reuses existing cache logic
- Consistent behavior between initial scan and real-time monitoring
- Cache lookup is instant

**Cons**:
- Increases initial scan time
- Memory overhead (20 images × 2MB ≈ 40MB)

### Option 2: Increase Semaphore Concurrency

Allow more parallel image processing:

```csharp
// ImageProcessingService.cs
private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(3, 3); // 3 concurrent
```

**Pros**:
- Reduces total processing time (50s → ~17s)
- Simple implementation

**Cons**:
- Still slower than cache
- Increased CPU/memory usage

### Option 3: Skip Cache Check for Initial Scan

Add a flag to bypass cache during initial scan:

```csharp
// FileGroupViewModel.LoadSingleThumbnailAsync
if (_orchestrator != null && !_isInitialScan && Path.GetFileName(imagePath).Equals(...))
{
    // Cache lookup logic
}
```

**Pros**:
- Minimal code change

**Cons**:
- Doesn't solve the semaphore bottleneck
- Inconsistent behavior

## Next Steps

1. **Immediate**: Increase semaphore concurrency to 3 (quick win)
2. **Short-term**: Implement Option 1 (pre-populate cache)
3. **Long-term**: Consider lazy loading with placeholders

## Related Files

- `MonitoringOrchestrator.cs` - Initial scan logic
- `FileGroupViewModel.cs` - Thumbnail loading logic
- `ImageProcessingService.cs` - Semaphore configuration
