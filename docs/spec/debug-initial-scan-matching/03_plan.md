# Fix Real-time Camera Display - Implementation Plan

## 1. Problem Analysis

### Root Cause
**File**: `MonitoringOrchestrator.cs`, Line 1606-1664 (`DetermineFileType`)

The real-time file watcher processes individual file events, but Normal data is **folder-based**:

```csharp
// Line 1617-1628: Current logic only checks FILES
if (filePath.StartsWith(settings.Normal1Path, ...))
{
    var ext = Path.GetExtension(filePath).ToLowerInvariant();
    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
    {
        return FileType.Normal;  // Only processes IMAGE FILES
    }
}
```

**Issue**: 
- Normal folders (e.g., `C251201T140543_0/`) contain image files
- `FileWatcher` fires events for files inside folders, not the folder itself
- When an image file arrives in Normal folder, `DetermineFileType` identifies it as `FileType.Normal`
- But `CreateOrUpdateGroupAsync` expects a **folder path**, not a file path
- This causes Normal image to be ignored or processed incorrectly

### Why Initial Scan Works
Initial scan uses `Directory.GetDirectories()` to find Normal **folders** directly (line 335-346, 349-362).

### Why Real-time Fails
Real-time only sees file creation events for images **inside** Normal folders, not folder creation.

## 2. Solution Design

### Fix Strategy
When `DetermineFileType` detects a Normal image file, extract the **parent folder** and use that as the Normal folder path.

### Code Changes

#### Change 1: Update `DetermineFileType` Logic

**File**: `MonitoringOrchestrator.cs`, Line 1614-1629

**Current**:
```csharp
if (filePath.StartsWith(settings.Normal1Path, ...))
{
    var ext = Path.GetExtension(filePath).ToLowerInvariant();
    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
    {
        _logger.LogInformation("File Identified as Normal: {Path}", filePath);
        return FileType.Normal;
    }
    return FileType.Unknown;
}
```

**New**:
```csharp
if (filePath.StartsWith(settings.Normal1Path, ...))
{
    var ext = Path.GetExtension(filePath).ToLowerInvariant();
    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
    {
        _logger.LogInformation("File Identified as Normal: {Path}", filePath);
        return FileType.Normal;  // FileType stays the same
    }
    return FileType.Unknown;
}
```

**Note**: `FileType.Normal` is correct—the issue is in what path gets passed to `CreateOrUpdateGroupAsync`.

#### Change 2: Update `CreateOrUpdateGroupAsync` to Extract Parent Folder

**File**: `MonitoringOrchestrator.cs`, Line 737

**Add folder extraction logic**:
```csharp
private async Task<FileGroup?> CreateOrUpdateGroupAsync(string filePath, FileType fileType)
{
    try
    {
        // NEW: If fileType is Normal and filePath is a file (not folder), use parent directory
        string processPath = filePath;
        if (fileType == FileType.Normal)
        {
            // Check if path is a file (has extension)
            if (File.Exists(filePath) && !string.IsNullOrEmpty(Path.GetExtension(filePath)))
            {
                // Use parent directory as the Normal folder
                processPath = Path.GetDirectoryName(filePath) ?? filePath;
                _logger.LogInformation("Normal file detected, using parent folder: {Folder}", processPath);
            }
        }

        _logger.LogInformation("Processing {FileType} file: {Path}", fileType, processPath);

        // Create UnmatchedFiles structure for this single file
        var unmatchedFiles = CreateUnmatchedFilesForSingleFile(processPath, fileType);
        // ... rest of logic ...
```

## 3. Alternative Solution (If Above Doesn't Work)

If the issue is that Normal folders aren't being watched at all:

### Add Folder Watcher

**File**: `MonitoringOrchestrator.cs`, Line 130-150 (in `StartAsync`)

```csharp
// After adding file paths to watchPaths, also watch for directory creation
_fileWatcher.IncludeSubdirectories = true;  // Enable if not already
_fileWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;
```

**Note**: This might already be enabled. Need to verify `FileWatcherService` configuration.

## 4. Verification Plan

### Pre-check: Review FileWatcherService Configuration
```bash
# Check what NotifyFilters are set
rg "NotifyFilter" c:\workspace\seaweed\gui_kiro\ChronoView\Core\FileWatching\FileWatcherService.cs
```

### Test 1: Build and Manual Test
1. **Build project**: `dotnet build c:\workspace\seaweed\gui_kiro\gui_kiro.sln`
2. **Run application**
3. **Manual test steps**:
   a. Start monitoring
   b. Add a new Normal folder with images (simulating real-time arrival)
   c. Verify Normal images appear in UI DataGrid
   d. Check logs for "Normal file detected, using parent folder" message

### Test 2: Check Logs
Look for these log patterns:
- ✅ `"File Identified as Normal: {Path}"` - Detection working
- ✅ `"Normal file detected, using parent folder: {Folder}"` - Fix working  
- ✅ `"Processing Normal file: {Path}"` - Path is now folder, not file
- ✅ `"Merging into existing group"` OR `"Creating new group"` - Group update working

### Test 3: Verify UI Update
- Check that `MainImagePath` is set correctly in FileGroup
- Verify thumbnail loads in DataGrid
- Confirm no "Cam path missing" messages for groups with Normal data

## 5. Risk Assessment

- **Low Risk**: Changes are localized to path transformation logic
- **Backward Compatibility**: Initial scan unchanged (still works)
- **Side Effects**: None expected—only affects real-time Normal file detection

---

**Status**: Ready for implementation
**Next Step**: Request user review, then proceed to EXECUTION mode
