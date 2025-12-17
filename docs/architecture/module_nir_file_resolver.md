---
Owner: Development Team
Last Updated: 2025-12-15
Code Ref: (INirFileResolver.cs, SpcTxtNirFileResolver.cs, MonitoringOrchestrator.cs)
---

# NIR File Resolver - Modular NIR File System Abstraction

## Overview

`INirFileResolver` provides a modular abstraction layer for handling different NIR (Near-Infrared) file systems. It uses the Strategy Pattern to allow easy replacement of NIR file handling logic without modifying core application code.

**Current Implementation**: `SpcTxtNirFileResolver` handles `.spc`/`.txt` file sets with 'A' suffix naming pattern.

## Key Components

- **Interface**: `ChronoView/Core/NIR/INirFileResolver.cs`
- **Implementation**: `ChronoView/Core/NIR/SpcTxtNirFileResolver.cs`
- **Related**: `MonitoringOrchestrator.cs`, `FileGroup.cs`, `App.xaml.cs`

## Problem Solved

### Before (Hardcoded Logic)
```
run_120251201T140542.spc  → Detected, new group created
run_120251201T140542A.txt → Detected, ANOTHER group created ❌
```
**Result**: Duplicate groups for single NIR file set

### After (Resolver Pattern)
```
run_120251201T140542.spc  → Ignored (not scanned)
run_120251201T140542A.txt → Detected, NirKey="run_120251201T140542", one group ✅
```
**Result**: Single group per NIR file set

## Architecture

### Interface: INirFileResolver

```csharp
public interface INirFileResolver
{
    string GetNirKey(string nirFilePath);
    string GetPrimaryFilePath(string nirKey, string nirDirectory);
    string? GetTextFilePath(string nirKey, string nirDirectory);
    IEnumerable<string> GetAllFilePaths(string nirKey, string nirDirectory);
    string GetScanPattern();
}
```

#### Method Responsibilities

| Method | Purpose | Example |
|--------|---------|---------|
| `GetNirKey` | Extract common identifier from file path | `run_...A.txt` → `run_...` |
| `GetPrimaryFilePath` | Get primary file path (stored in FileGroup) | Returns `.spc` path |
| `GetTextFilePath` | Get text file for graph generation | Returns `.txt` path |
| `GetAll FilePaths` | Get all files in the set | Returns [`.spc`, `.txt`] |
| `GetScanPattern` | Get file pattern for initial scan | Returns `"*.txt"` |

### Implementation: SpcTxtNirFileResolver

**File Format**:
```
run_YYYYMMDDTHHMMSS.spc   (Primary/binary file)
run_YYYYMMDDTHHMMSSÅ.txt  (Text file for graphing)
```

**NirKey**: `run_YYYYMMDDTHHMMSS` (A suffix removed)

**Strategy**:
1. **Scan**: Only `.txt` files (avoids duplicates from `.spc`)
2. **Store**: `.spc` path in `FileGroup.NirFilePath`
3. **Draw Graph**: Find `.txt` from NirKey  
4. **Move/Delete**: Both files via `GetAllFilePaths()`

## Contracts

### Inputs
- **nirFilePath**: Full path to NIR file (e.g., `Z:\...\run_...A.txt`)
- **nirKey**: Common identifier without extension/suffix
- **nirDirectory**: Directory containing NIR files

### Outputs
- **NirKey**: String identifier for file set
- **File paths**: Full paths to files in the set
- **Scan pattern**: Glob pattern for Directory.GetFiles

### Errors/Exceptions
- Never throws exceptions
- Returns `null` if file not found (`GetTextFilePath`)
- Returns empty enumerable if no files exist (`GetAllFilePaths`)

### Side Effects
- None (pure functions, file existence checks only)

## Logic Flow

### 1. Initial Scan (PerformInitialScanAsync)

```
1. Get scan pattern from resolver → "*.txt"
2. Scan directory for *.txt files
3. For each file:
   a. Get NirKey from resolver
   b. Get primary file path (.spc) from resolver
   c. Store primary path in UnmatchedFiles[nirKey]
4. FileGroupMatcher creates groups
   → Result: One group per NirKey
```

### 2. Real-time Detection (DetermineFileType)

```
1. File created: run_...A.txt
2. Check extension → .txt ✅ (FileType.Nir)
3. Process as NIR file
   a. Get NirKey from resolver
   b. Get primary path from resolver
   c. Match to existing group OR create new
→ Result: Group updated/created

1. File created: run_....spc
2. Check extension → .spc ❌ (FileType.Unknown)
3. Ignored
→ Result: No duplicate group
```

### 3. Move/Delete Operations (FileGroup.GetAllFilePaths)

```
1. FileGroup has NirFilePath = "Z:\...\run_....spc"
2. GetAllFilePaths() called
3. Yield NirFilePath (.spc)
4. Extract NirKey from NirFilePath
5. Calculate txtPath = NirKey + "A.txt"
6. Check File.Exists(txtPath)
7. Yield txtPath if exists
→ Result: Both files moved/deleted together
```

## Dependencies

### Internal
- `MonitoringOrchestrator`: Consumes INirFileResolver via DI
- `FileGroup`: Uses hardcoded logic in GetAllFilePaths (could be improved)
- `App.xaml.cs`: DI registration

### External
- `System.IO.Path`: Path manipulation
- `System.IO.File`: File existence checks
- `Microsoft.Extensions.DependencyInjection`: DI container

### Config/Env
- None (implementation-specific, not configurable)

## Dependents
<!-- VERIFY: grep -rn "INirFileResolver" ChronoView/ -->
<!-- VERIFY: grep -rn "SpcTxtNirFileResolver" ChronoView/ -->

- **MonitoringOrchestrator**: Injected via constructor, used in:
  - `PerformInitialScanAsync` (NIR1 & NIR2 scan)
  - Future: `CreateUnmatchedFilesForSingleFile` (real-time)
  - Future: `CreateNewGroupAsync` (real-time)

- **App.xaml.cs**: DI registration
  ```csharp
  services.AddSingleton<INirFileResolver, SpcTxtNirFileResolver>();
  ```

## Impact / Touchpoints
<!-- VERIFY: grep -rn "GetNirKey\|GetPrimaryFilePath\|GetTextFilePath" ChronoView/ -->

> [!IMPORTANT]
> **Before modifying this module:**
> 1. Understand that changing `GetScanPattern()` affects initial scan behavior
> 2. Changing `GetNirKey()` breaks existing group matching logic
> 3. Adding a new resolver requires DI registration change

### Files Modified
1. `MonitoringOrchestrator.cs`:
   - Constructor: Added INirFileResolver parameter
   - NIR1/2 scan: Uses `GetScanPattern()`, `GetNirKey()`, `GetPrimaryFilePath()`
   - `DetermineFileType`: Excluded `.spc` from real-time detection

2. `FileGroup.cs`:
   - `GetAllFilePaths()`: Returns both .spc and .txt files

3. `App.xaml.cs`:
   - Added `INirFileResolver` DI registration

> [!WARNING]
> **Changing resolver implementation will affect:**
> - Initial scan file discovery
> - NIR file set grouping
> - Move/delete operations
> - NIR graph generation (indirect)

## Extension Points

### Adding a New NIR System

**Example: BrukerNirFileResolver** (different file pattern)

```csharp
public class BrukerNirFileResolver : INirFileResolver
{
    // Bruker format: spectrum_001.0 + spectrum_001.csv
    public string GetNirKey(string nirFilePath)
    {
        // Extract: spectrum_001
        var match = Regex.Match(nirFilePath, @"spectrum_(\d+)");
        return match.Success ? $"spectrum_{match.Groups[1].Value}" : "";
    }
    
    public string GetPrimaryFilePath(string nirKey, string nirDirectory)
    {
        return Path.Combine(nirDirectory, nirKey + ".0");
    }
    
    public string? GetTextFilePath(string nirKey, string nirDirectory)
    {
        var csvPath = Path.Combine(nirDirectory, nirKey + ".csv");
        return File.Exists(csvPath) ? csvPath : null;
    }
    
    public IEnumerable<string> GetAllFilePaths(string nirKey, string nirDirectory)
    {
        var primaryPath = GetPrimaryFilePath(nirKey, nirDirectory);
        if (File.Exists(primaryPath)) yield return primaryPath;
        
        var textPath = GetTextFilePath(nirKey, nirDirectory);
        if (textPath != null) yield return textPath;
    }
    
    public string GetScanPattern()
    {
        return "*.csv";  // Scan CSV files
    }
}
```

**Registration**:
```csharp
// App.xaml.cs
services.AddSingleton<INirFileResolver, BrukerNirFileResolver>();
```

**Result**: Entire NIR system replaced with ONE line change!

## Testing Strategy

### Unit Tests (Recommended)

```csharp
[Fact]
public void GetNirKey_RemovesASuffix()
{
    var resolver = new SpcTxtNirFileResolver();
    var result = resolver.GetNirKey("run_120251201T140542A.txt");
    Assert.Equal("run_120251201T140542", result);
}

[Fact]
public void GetNirKey_NoSuffix_ReturnsUnchanged()
{
    var resolver = new SpcTxtNirFileResolver();
    var result = resolver.GetNirKey("run_120251201T140542.spc");
    Assert.Equal("run_120251201T140542", result);
}

[Fact]
public void GetAllFilePaths_ReturnsBothFiles()
{
    var resolver = new SpcTxtNirFileResolver();
    // Create temp files for testing
    var paths = resolver.GetAllFilePaths("run_test", testDirectory).ToList();
    Assert.Equal(2, paths.Count);
    Assert.Contains(paths, p => p.EndsWith(".spc"));
    Assert.Contains(paths, p => p.EndsWith("A.txt"));
}
```

### Integration Tests

```csharp
[Fact]
public async Task InitialScan_WithResolver_CreatesSingleGroupPerSet()
{
    // Arrange: Create .spc + .txt pair
    // Act: PerformInitialScanAsync()
    // Assert: One group created with correct NirKey
}

[Fact]
public void DetermineFileType_SpcFile_ReturnsUnknown()
{
    // Arrange: .spc file path
    // Act: DetermineFileType()
    // Assert: Returns FileType.Unknown
}

[Fact]
public void DetermineFileType_TxtFile_ReturnsNir()
{
    // Arrange: .txt file path
    // Act: DetermineFileType()
    // Assert: Returns FileType.Nir
}
```

## Related Docs

- `module_monitoring_orchestrator.md`: Uses INirFileResolver
- `module_file_group_matcher.md`: Creates groups from NirKey
- `glossary.md`: Definitions of NirKey, NirFilePath, etc.

## Changelog

- **2025-12-15**: Initial implementation
  - Created `INirFileResolver` interface
  - Implemented `SpcTxtNirFileResolver` for .spc/.txt sets
  - Integrated into MonitoringOrchestrator
  - Added DI registration
  - Updated FileGroup.GetAllFilePaths for NIR file sets
