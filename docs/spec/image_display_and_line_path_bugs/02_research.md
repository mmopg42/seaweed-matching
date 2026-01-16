---
Task: image_display_and_line_path_bugs
Created: 2026-01-09
Status: Complete
---

# Research: Image Display and Line Path Bugs

## 1. Bug #1: Image Preview Window Layout Issue

### Findings

**Location**: `ChronoView/UI/Views/ImagePreviewWindow.xaml`

**Root Cause**: Window uses `SizeToContent="WidthAndHeight"` which causes unpredictable layout when:
- Image is null or very small
- Image aspect ratio doesn't fit constraints
- `MaxWidth="1200" MaxHeight="900"` may clip content unexpectedly

**Evidence from XAML**:
```xml
<Window SizeToContent="WidthAndHeight" MaxWidth="1200" MaxHeight="900" ...>
    <Grid Grid.Row="1" Background="Transparent">
        <Image Source="{Binding DisplayImage}" Stretch="Uniform" .../>
    </Grid>
</Window>
```

**Problem**: When `DisplayImage` is null or loading fails, the window collapses to minimum size causing broken layout as shown in screenshot.

### Solution Direction
- Add fallback image or minimum size constraint
- Check if `DisplayImage` binding is valid before showing window

---

## 2. Bug #2: Missing Image Display Logs

### Findings

**Image Loading Location**: `ChronoView/UI/ViewModels/FileGroupViewModel.cs`
- `MainImagePath` is constructed from `NormalFolder + "stitched_original.png"`
- `InitializeImagePaths()` method (line 163) sets `MainImagePath`

**File Check Location**: `ChronoView/UI/ViewModels/FileGroupViewModel.cs`
- `GetNormalImageSize()` (line 115-127) checks if `stitched_original.png` exists
- Returns empty string if not found, but **no log is generated**

**Thumbnail Loading**: Uses `_mediaLoader.LoadThumbnailsAsync()`
- No debug log when image successfully displayed in DataGrid
- No warning log when folder exists but image missing

### Solution Direction
- Add Warning log: `"Normal folder exists but stitched_original.png not found: {path}"`
- Add Debug log: `"Image displayed in grid: {imagePath}"`

---

## 3. Bug #3: Line2 Using Line1 Path Data

### Findings

**Line Assignment Flow**:
1. `FileMatchingEngine.BuildLineGroups()` (line 138-174)
   - Line 1 uses: `normalKey="normal1"`, `nirKey="nir1"`, `camKeys=["cam1","cam2","cam3"]`
   - Line 2 uses: `normalKey="normal2"`, `nirKey="nir2"`, `camKeys=["cam4","cam5","cam6"]`

2. `DashboardViewModel.OnGroupCreated()` (line 139-151)
   - Uses `group.LineNumber` to add to `Line1Groups` or `Line2Groups`

**Potential Issue Locations**:
1. **UnmatchedFiles population**: If `normal2` key contains Line1 data
2. **FileWatcherService**: Path classification logic may misassign line
3. **Configuration paths**: `Normal2Path` might point to Line1 folder

**Most Likely Cause**: 
Configuration paths (`MatchingSettings.Normal2Path`, `Camera4Path`, etc.) may not be correctly configured, or file watcher is putting files in wrong key buckets.

### Solution Direction
1. Add logging to trace which paths populate which keys
2. Verify configuration paths in Settings dialog
3. Check FileWatcherService path classification logic

---

## 4. Key Files for Modification

| Bug | Files to Modify |
|-----|-----------------|
| #1 Image Preview | `ImagePreviewWindow.xaml`, `ImagePreviewWindow.xaml.cs` |
| #2 Image Logs | `FileGroupViewModel.cs`, `FileGroupMediaLoader.cs` |
| #3 Line Path | `FileWatcherService.cs`, `EventProcessor.cs` (diagnosis first) |

---

## 5. Questions Answered

| Question | Answer |
|----------|--------|
| Q1: Preview layout cause | `SizeToContent` + null/missing image binding |
| Q2: Image loading location | `FileGroupViewModel.InitializeImagePaths()`, `GetNormalImageSize()` |
| Q3: Line number assignment | `FileMatchingEngine` uses separate keys per line |
| Q4: Line2 data source | `DashboardViewModel.Line2Groups` via `group.LineNumber == 2` check |

---

**Next Step**: 03_plan.md
