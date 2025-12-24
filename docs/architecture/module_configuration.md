# Module: Configuration Management

> **Purpose**: Manages application configuration persistence, validation, and change notifications.  
> **Owner**: `ChronoView.Core.Configuration`  
> **Last Updated**: 2025-12-18  
> **Status**: ✅ Active

---

## Overview

The Configuration Management module provides centralized configuration storage and retrieval for the ChronoView application. It handles JSON serialization/deserialization, platform-specific storage paths, and configuration validation.

### Key Components

| Component | Type | Purpose |
|-----------|------|---------|
| `ConfigurationManager` | Class | Main configuration service implementation |
| `IConfigurationManager` | Interface | Configuration service contract |
| `ApplicationConfiguration` | Model | Root configuration model |
| `MatchingSettings` | Model | File matching configuration |

---

## Configuration File Location

### Storage Path

Configuration is stored in a platform-specific user data directory:

**Windows**:
```
C:\Users\[Username]\AppData\Local\ChronoView\ChronoView\config.json
```

**Quick Access**: 
- Press `Win + R`
- Type: `%LOCALAPPDATA%\ChronoView\ChronoView`
- Open `config.json`

**macOS**:
```
~/Library/Application Support/ChronoView/ChronoView/config.json
```

**Linux**:
```
~/.local/share/ChronoView/ChronoView/config.json
```

### Path Determination Logic

```csharp
// ChronoView/Core/Configuration/ConfigurationManager.cs:42-62
private string GetUserDataDirectory()
{
    string baseDir;

    if (OperatingSystem.IsWindows())
    {
        baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }
    else if (OperatingSystem.IsMacOS())
    {
        baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library",
            "Application Support"
        );
    }
    else // Linux
    {
        var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        baseDir = !string.IsNullOrEmpty(xdgDataHome)
            ? xdgDataHome
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
    }

    return Path.Combine(baseDir, _appAuthor, _appName);
}
```

---

## Critical Configuration Fields

### MatchingSettings

These paths are used by `StatisticsService` and `MonitoringOrchestrator`:

| Field | Purpose | Expected Content |
|-------|---------|------------------|
| `nir1Path` | NIR data files (Line 1) | Directory containing `.txt` or `.csv` files |
| `normal1Path` | Normal camera folders (Line 1) | Directory containing `C*` subdirectories |
| `camera1Path` | Camera 1 images | Directory containing `.jpg`/`.png` files |
| `camera2Path` | Camera 2 images | Directory containing `.jpg`/`.png` files |
| `camera3Path` | Camera 3 images | Directory containing `.jpg`/`.png` files |
| `nir2Path` | NIR data files (Line 2) | Directory containing `.txt` or `.csv` files |
| `normal2Path` | Normal camera folders (Line 2) | Directory containing `C*` subdirectories |
| `camera4Path` | Camera 4 images | Directory containing `.jpg`/`.png` files |
| `camera5Path` | Camera 5 images | Directory containing `.jpg`/`.png` files |
| `camera6Path` | Camera 6 images | Directory containing `.jpg`/`.png` files |

**Example**:
```json
{
  "matchingSettings": {
    "nir1Path": "Z:\\data\\nir",
    "normal1Path": "Z:\\data\\normal",
    "camera1Path": "Z:\\data\\cam1",
    "camera2Path": "Z:\\data\\cam2",
    "camera3Path": "Z:\\data\\cam3"
  }
}
```

---

## Common Configuration Errors

### ❌ Error 1: Swapped NIR and Normal Paths

**Problem**: NIR path points to Normal folder and vice versa

```json
// INCORRECT - paths are swapped!
"nir1Path": "Z:\\data\\normal",      // ❌ Points to normal folder
"normal1Path": "Z:\\data\\nir",      // ❌ Points to nir folder
```

**Symptoms**:
- File counts show 0 or incorrect values
- NIR data not matching correctly
- Normal folders not detected

**Fix**: Swap the paths back to correct values

```json
// CORRECT
"nir1Path": "Z:\\data\\nir",         // ✅ Points to nir folder
"normal1Path": "Z:\\data\\normal",  // ✅ Points to normal folder
```

**Reference**: `docs/trouble/file_count_path_swap_issue.md`

### ❌ Error 2: Empty Paths

**Problem**: Paths are empty strings

```json
"nir1Path": "",  // ❌ Empty
"normal1Path": "",  // ❌ Empty
```

**Symptoms**:
- Monitoring starts but no files detected
- All file counts show 0

**Fix**: Set valid absolute paths

### ❌ Error 3: Invalid Network Paths

**Problem**: Network drive not accessible

```json
"nir1Path": "Z:\\data\\nir",  // Z: drive not mapped
```

**Symptoms**:
- Monitoring fails to start with path validation error
- Or monitoring starts but counts remain 0

**Fix**: Verify network drive is mapped and accessible

---

## Configuration Validation

### Current Validation (Startup)

Performed in `MainWindowViewModel.ExecuteStartAsync()`:

```csharp
// ChronoView/UI/ViewModels/MainWindowViewModel.cs:764-831
// Validate configuration paths exist
var missingPaths = new List<string>();
if (!string.IsNullOrEmpty(config.MatchingSettings.Nir1Path) && !Directory.Exists(config.MatchingSettings.Nir1Path))
    missingPaths.Add($"NIR1: {config.MatchingSettings.Nir1Path}");
if (!string.IsNullOrEmpty(config.MatchingSettings.Normal1Path) && !Directory.Exists(config.MatchingSettings.Normal1Path))
    missingPaths.Add($"Normal1: {config.MatchingSettings.Normal1Path}");
// ... (NIR2, Normal2)

if (missingPaths.Count > 0)
{
    // Show error and abort monitoring
}
```

**Limitations**:
- ❌ Does NOT validate camera paths
- ❌ Does NOT check if NIR path contains files
- ❌ Does NOT check if Normal path contains subdirectories
- ❌ Cannot detect swapped paths

### Recommended Enhancements

**Add semantic validation** to verify path contents match expected type:

```csharp
public static class ConfigurationValidator
{
    public static ValidationResult ValidateMatchingPaths(MatchingSettings settings)
    {
        var warnings = new List<string>();
        
        // Validate NIR path contains .txt or .csv files
        if (!string.IsNullOrEmpty(settings.Nir1Path) && Directory.Exists(settings.Nir1Path))
        {
            var hasNirFiles = Directory.EnumerateFiles(settings.Nir1Path, "*.txt")
                .Concat(Directory.EnumerateFiles(settings.Nir1Path, "*.csv"))
                .Any();
            
            if (!hasNirFiles)
            {
                warnings.Add($"Nir1Path does not contain .txt/.csv files. Path: {settings.Nir1Path}");
            }
        }
        
        // Validate Normal path contains C* subdirectories
        if (!string.IsNullOrEmpty(settings.Normal1Path) && Directory.Exists(settings.Normal1Path))
        {
            var hasNormalFolders = Directory.EnumerateDirectories(settings.Normal1Path)
                .Any(d => Path.GetFileName(d)?.StartsWith("C") == true);
            
            if (!hasNormalFolders)
            {
                warnings.Add($"Normal1Path does not contain C* subdirectories. Path: {settings.Normal1Path}");
            }
        }
        
        return new ValidationResult(warnings);
    }
}
```

**Benefits**:
- Early detection of swapped paths
- Helps users correct configuration errors
- Reduces support burden

---

## API Contract

### Load Configuration

```csharp
// Async
Task<T> LoadConfigurationAsync<T>() where T : class, new()

// Sync
T LoadConfiguration<T>() where T : class, new()
```

**Behavior**:
- Returns default instance `new T()` if file doesn't exist
- Throws `ConfigurationException` on JSON parse error
- Throws `ConfigurationException` on I/O error

### Save Configuration

```csharp
Task SaveConfigurationAsync<T>(T configuration) where T : class
```

**Behavior**:
- Validates configuration before saving
- Raises `ConfigurationChanged` event after successful save
- Throws `ConfigurationException` on validation or I/O error

### Events

```csharp
event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
```

**Raised**: After successful `SaveConfigurationAsync()`

**Use Case**: Notify other services to reload configuration

---

## Dependencies

### Consumed By

| Consumer | Usage | Impact if Changed |
|----------|-------|-------------------|
| `MainWindowViewModel` | Loads configuration at startup | High - monitoring won't start |
| `MonitoringOrchestrator` | Reads matching paths, settings | High - affects file detection |
| `StatisticsService` | Reads matching paths for counting | High - affects file counts |
| `SettingsDialogViewModel` | Loads/saves user settings | High - settings won't persist |

\u003c!-- VERIFY: grep -rn "IConfigurationManager" ChronoView/ --\u003e

### Dependencies

| Dependency | Purpose |
|------------|---------|
| `System.Text.Json` | JSON serialization |
| File system API | Path operations, directory checks |

---

## Testing Considerations

### Manual Testing

1. **Path Validation**:
   - Set invalid paths in `config.json`
   - Start application
   - Verify error message and monitoring aborts

2. **Path Swap Detection** (future):
   - Swap NIR and Normal paths
   - Start application
   - Should show warning (after validation added)

3. **Network Drive**:
   - Set paths to network drive
   - Disconnect network
   - Start application
   - Verify graceful error handling

### Unit Testing Needs

- [ ] Load configuration with missing file → returns default
- [ ] Load configuration with invalid JSON → throws exception
- [ ] Save configuration → writes to correct path
- [ ] Validate configuration → detects swapped paths
- [ ] Validate configuration → detects empty required paths

---

## Path Auto-Configuration Feature

### Intended Behavior (Python Reference Implementation)

The **Path Auto-Configuration** feature should allow users to update date-based paths in their configuration by replacing date patterns (YYYYMMDD) in existing paths.

**Correct Implementation** (as in Python version `script/apps/monitoring_app.py:path_auto_setting_edit_config`):

1. **User Input**: Takes date from user input field (`today_edit`) in YYYYMMDD format
2. **Pattern Matching**: Finds 8-digit date patterns (YYYYMMDD) in existing configuration paths
3. **Path Replacement**: Replaces found date patterns with the new date
4. **Folder Creation**: Automatically creates folders if they don't exist
5. **Confirmation**: Shows preview of changes before applying

**Example**:
```
Existing paths:
  nir1Path: "D:/Data/2025/12/18/NIR1"
  normal1Path: "D:/Data/2025/12/18/Normal1"

User enters new date: "20251219"

Result:
  nir1Path: "D:/Data/2025/12/19/NIR1"  (date replaced)
  normal1Path: "D:/Data/2025/12/19/Normal1"  (date replaced)
  Folders created automatically if missing
```

### ❌ Current C# Implementation (INCORRECT)

**Status**: 🔴 **BROKEN** - Does not match intended behavior

**Location**: `ChronoView/UI/ViewModels/MainWindowViewModel.cs:ExecutePathAutoConfigAsync()`

**Problems**:

1. ❌ **Ignores user input**: Uses `DateTime.Today` instead of user-provided date
2. ❌ **Ignores existing paths**: Generates completely new paths using `PathManagementService.GeneratePathsFromDate()` instead of replacing date patterns in existing paths
3. ❌ **No folder creation**: Does not create folders automatically
4. ❌ **Different logic**: Uses `BasePath/{YYYY}/{MM}/{DD}/NIR1` pattern generation instead of pattern replacement

**Current Implementation**:
```csharp
// WRONG: Uses today's date, ignores existing paths
var targetDate = DateTime.Today;
var dateString = targetDate.ToString("yyyyMMdd");
var generatedPaths = _pathManagementService.GeneratePathsFromDate(dateString, config);
// Overwrites all paths with new generated paths
```

**What It Should Do**:
```csharp
// CORRECT: Use user input, replace date patterns in existing paths
var userDate = DateInput.Text; // From user input field
// Find YYYYMMDD patterns in existing paths and replace with userDate
// Create folders if missing
```

**Impact**:
- Users cannot specify custom dates
- Existing path structure is lost
- Folders must be created manually
- Causes "Configuration paths do not exist" warnings

**Fix Required**:
- Implement date pattern replacement logic (similar to Python `re.compile(r'\d{8}').sub()`)
- Add user date input field to UI
- Add automatic folder creation
- Preserve existing path structure

**Reference**: 
- Python implementation: `script/apps/monitoring_app.py:639-780`
- Python path utils: `script/utils/path_utils.py:auto_update_paths_with_date()`

---

## Known Issues

### Issue: Swapped Paths Not Detected

**Severity**: Medium  
**Status**: Won't Fix (pending validation enhancement)

**Description**: Configuration allows NIR and Normal paths to be swapped without warning.

**Workaround**: Users must manually verify path configuration.

**Long-term Fix**: Implement semantic path validation (see Recommended Enhancements above).

**Reference**: `docs/trouble/file_count_path_swap_issue.md`

---

## Related Documents

- `docs/trouble/file_count_path_swap_issue.md` - Configuration path swap issue
- `docs/spec/file_count_fix/01_requirements.md` - File count fix requirements
- `docs/spec/file_count_fix/02_research.md` - Configuration investigation
- `docs/architecture/glossary.md` - Official term definitions

---

## Change Log

| Date | Change | Impact |
|------|--------|--------|
| 2025-12-18 | Initial documentation | - |
| 2025-12-18 | Added path swap error documentation | Medium - helps prevent configuration errors |
| 2025-12-19 | Documented Path Auto-Configuration feature and C# implementation issues | High - clarifies broken functionality |

---

\u003c!-- VERIFY: 
grep -rn "ConfigurationManager" ChronoView/
grep -rn "config.json" ChronoView/
--\u003e
