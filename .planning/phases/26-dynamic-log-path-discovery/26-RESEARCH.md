# Phase 26: Dynamic Log Path Discovery - Research

**Researched:** 2026-01-21
**Domain:** Log path discovery, WSL/Windows compatibility
**Confidence:** HIGH

## Summary

ChronoView creates daily log folders under `{LocalAppData}\prische\ChronoView\Logs\{yyyyMMdd}\` format. Currently, log discovery tools (ConsoleLogsReader, log-analyst agent, test:logs command) require manual date specification or only search the base directory. This research identifies existing patterns for date-based folder discovery, WSL path conversion, and the specific code locations requiring modification.

**Primary recommendation:** Add a `GetLatestLogDateFolder()` method to `ConsoleLogsReader.cs` that follows the existing pattern from `LogCleanupService.cs` (lines 49-67) for discovering and validating `{yyyyMMdd}` folders, then integrate this into the UI automation commands and log-analyst agent.

## Standard Stack

### Core
| Component | Location | Purpose | Why Standard |
|-----------|----------|---------|--------------|
| System.IO.Directory | .NET BCL | Directory enumeration | Built-in, no dependencies |
| DateTime.TryParseExact | System.Globalization | Date folder validation | Strict format matching (yyyyMMdd) |
| Environment.SpecialFolder.LocalApplicationData | System | Log base path resolution | Cross-platform, matches ConfigurationManager |

### Supporting
| Component | Location | Purpose | When to Use |
|-----------|----------|---------|-------------|
| SetupConfigVerifier.NormalizePath | SetupConfigVerifier.cs | WSL/Windows path conversion | Already implements `/mnt/c` → `C:\` pattern |
| PathHelper.LogsDirectory | ChronoView/Core/Configuration/PathHelper.cs | Centralized log directory access | Consistent with application config |

**No external packages required** - all functionality exists in .NET BCL.

## Architecture Patterns

### Log Folder Structure
```
%LOCALAPPDATA%\prische\ChronoView\Logs\
    ├── 20260120\
    │   ├── ChronoView_Debug_20260120_143022.log
    │   └── ChronoView_Critical_20260120_150533.log
    ├── 20260121\
    │   └── ChronoView_Debug_20260121_091234.log
    └── 20260122\
```

### Pattern 1: Date Folder Discovery (from LogCleanupService.cs)
**What:** Enumerate directories and filter by yyyyMMdd naming pattern
**When to use:** Finding the latest log folder for analysis
**Source:** ChronoView/Core/Logging/LogCleanupService.cs lines 49-67

```csharp
// HIGH CONFIDENCE: Verified from existing codebase
var dateFolders = Directory.GetDirectories(logDir);
foreach (var dateFolderPath in dateFolders)
{
    var folderName = Path.GetFileName(dateFolderPath);

    // Folder name yyyyMMdd format validation
    if (folderName.Length != 8 ||
        !DateTime.TryParseExact(folderName, "yyyyMMdd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var folderDate))
    {
        continue; // Skip non-date folders
    }
    // Use folderDate for comparison
}
```

### Pattern 2: WSL/Windows Path Normalization
**What:** Convert `/mnt/c/...` to `C:\...` for cross-platform compatibility
**When to use:** Processing paths that may come from WSL environment
**Source:** skills_scripts/ui_automation/SetupConfigVerifier.cs lines 274-303

```csharp
// HIGH CONFIDENCE: Existing implementation in codebase
public static string NormalizePath(string path)
{
    if (string.IsNullOrEmpty(path)) return string.Empty;

    // Convert WSL paths (/mnt/c/...) to Windows paths (C:\...)
    if (path.StartsWith("/mnt/", StringComparison.OrdinalIgnoreCase))
    {
        var parts = path.Substring(5).Split('/', 2);
        if (parts.Length >= 1 && parts[0].Length == 1)
        {
            var driveLetter = parts[0].ToUpperInvariant();
            var rest = parts.Length > 1 ? parts[1].Replace('/', '\\') : string.Empty;
            return $"{driveLetter}:\\{rest}";
        }
    }

    return path.TrimEnd('/', '\\').Replace('/', '\\');
}
```

### Pattern 3: Latest Log Selection (from ConsoleLogsReader.cs)
**What:** Sort log files by last write time descending
**When to use:** Default log file for tail/search operations
**Source:** skills_scripts/ui_automation/ConsoleLogsReader.cs lines 49-52

```csharp
// HIGH CONFIDENCE: Already implemented
var files = Directory.GetFiles(searchPath, "*.log", SearchOption.TopDirectoryOnly);
return files.OrderByDescending(f => File.GetLastWriteTime(f)).ToArray();
```

### Anti-Patterns to Avoid
- **Hardcoding dates**: Don't use `DateTime.Now.ToString("yyyyMMdd")` as the only option
- **File.GetLastWriteTime for folder selection**: Folder names (yyyyMMdd) are more reliable than filesystem timestamps for log folders
- **Assuming logs exist**: Always handle empty/non-existent directories gracefully

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Date parsing | Custom regex/string parsing | `DateTime.TryParseExact` | Built-in validation, culture-safe |
| Path normalization | Custom string replace | `SetupConfigVerifier.NormalizePath` | Already handles WSL edge cases |
| Log directory location | Hardcoded paths | `PathHelper.LogsDirectory` | Consistent with app config |

## Common Pitfalls

### Pitfall 1: Non-Date Folders in Logs Directory
**What goes wrong:** Directory enumeration returns non-date folders (e.g., temp files, hidden directories)
**Why it happens:** Filtering only by directory existence, not name format
**How to avoid:** Always validate folder name with `DateTime.TryParseExact(folderName, "yyyyMMdd", ...)`
**Warning signs:** `FormatException` or folders not being found

### Pitfall 2: WSL Path Conversion Bidirectional
**What goes wrong:** Converting Windows → WSL when only WSL → Windows is needed
**Why it happens:** Overgeneralizing path conversion
**How to avoid:** Focus on reading logs (Windows paths); WSL paths only needed if agent runs in WSL
**Warning signs:** Paths starting with `C:\` being converted to `/mnt/c/`

### Pitfall 3: Empty Log Directory Handling
**What goes wrong:** Returning `null` or throwing exception when no logs exist
**Why it happens:** Not handling first-run or clean-slate scenarios
**How to avoid:** Return empty array/string; let caller decide how to handle "no logs" case
**Warning signs:** NullReferenceException in log-analyst agent

### Pitfall 4: Folder Name vs. File Timestamp Confusion
**What goes wrong:** Using `Directory.GetLastWriteTime` instead of folder name for sorting
**Why it happens:** Assuming filesystem timestamps reflect folder creation intent
**How to avoid:** Parse folder name as DateTime for sorting; filenames are already timestamped
**Warning signs:** Folders being ordered incorrectly

## Code Examples

### Get Latest Date Folder
```csharp
// Pattern from LogCleanupService.cs
public static string? GetLatestLogDateFolder(string logsBaseDirectory)
{
    if (!Directory.Exists(logsBaseDirectory))
    {
        return null;
    }

    var dateFolders = Directory.GetDirectories(logsBaseDirectory);

    var latest = dateFolders
        .Select(d => Path.GetFileName(d))
        .Where(name => name.Length == 8 &&
            DateTime.TryParseExact(name, "yyyyMMdd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        .OrderByDescending(name => name)  // String comparison works for yyyyMMdd
        .FirstOrDefault();

    return latest != null ? Path.Combine(logsBaseDirectory, latest) : null;
}
```

### Get All Log Files from Latest Folder
```csharp
// Extension to ConsoleLogsReader
public string[] GetLogFilesFromLatestFolder()
{
    var logDir = GetLogDirectory();
    var latestFolder = GetLatestLogDateFolder(logDir);

    if (latestFolder == null || !Directory.Exists(latestFolder))
    {
        return Array.Empty<string>();
    }

    var files = Directory.GetFiles(latestFolder, "*.log", SearchOption.TopDirectoryOnly);
    return files.OrderByDescending(f => File.GetLastWriteTime(f)).ToArray();
}
```

### WSL-Compatible Path Resolution
```csharp
// For agent use (may run in WSL or Windows)
public static string ResolveLogPathForAgent()
{
    // In Windows: Use PathHelper
    var logDir = PathHelper.LogsDirectory;

    // If running in WSL, convert to WSL path
    if (IsRunningInWSL())
    {
        return WindowsToWslPath(logDir);
    }

    return logDir;
}

private static bool IsRunningInWSL()
{
    return File.Exists("/proc/version") &&
           File.ReadAllText("/proc/version").Contains("Microsoft");
}

private static string WindowsToWslPath(string windowsPath)
{
    // C:\ -> /mnt/c/
    if (windowsPath.Length >= 2 && windowsPath[1] == ':')
    {
        var drive = windowsPath[0].ToString().ToLowerInvariant();
        var rest = windowsPath.Substring(2).Replace('\\', '/');
        return $"/mnt/{drive}{rest}";
    }
    return windowsPath;
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual date specification | Auto-discovery of latest folder | Phase 26 | No need to specify date in most cases |
| Hardcoded log paths | PathHelper.LogsDirectory | Earlier phase | Centralized configuration |
| Windows-only paths | WSL-compatible paths | Phase 26 | Agents can run in WSL or Windows |

**Deprecated/outdated:**
- Manual date folder specification: Replaced with automatic discovery
- Search without date filter: Still useful but `--latest` flag preferred

## Implementation Locations

### Files to Modify

| File | Changes Required | Lines of Interest |
|------|------------------|-------------------|
| `ConsoleLogsReader.cs` | Add `GetLatestLogDateFolder()`, `GetLogFilesFromLatest()` | Existing: 14-23 (GetLogDirectory), 30-53 (GetLogFiles) |
| `SettingsCommands.cs` | Add `--latest` option to console-logs commands | 40-86 (console-logs list), 88-155 (console-logs tail) |
| `.claude/commands/test/logs.md` | Update documentation for auto-discovery | 26 (Console logs path), 70-98 (Console log actions) |
| `.claude/agents/log-analyst.md` | Add path discovery to Analysis Commands | 272-285 (Analysis Commands section) |

### Files to Reference (No Changes)

| File | Why Reference |
|------|---------------|
| `LogCleanupService.cs` | Source pattern for yyyyMMdd folder discovery |
| `SetupConfigVerifier.cs` | Source pattern for WSL path normalization |
| `PathHelper.cs` | Centralized log directory access |

## Open Questions

1. **WSL Agent Execution Context**
   - What we know: log-analyst agent may be invoked from WSL or Windows
   - What's unclear: Does the agent actually run in WSL environment currently?
   - Recommendation: Check actual execution environment; if Windows-only, skip WSL path conversion for now

2. **Multiple Log Bases**
   - What we know: Documentation mentions %APPDATA%, %LOCALAPPDATA%, %TEMP%
   - What's unclear: Are logs actually written to multiple locations in practice?
   - Recommendation: Verify actual log locations; PathHelper uses LocalApplicationData consistently

## Sources

### Primary (HIGH confidence)
- `ChronoView/Core/Logging/LogCleanupService.cs` - lines 49-67: yyyyMMdd folder enumeration pattern
- `skills_scripts/ui_automation/SetupConfigVerifier.cs` - lines 274-303: WSL path normalization
- `skills_scripts/ui_automation/ConsoleLogsReader.cs` - lines 14-53: Current log path and file discovery
- `ChronoView/Core/Configuration/PathHelper.cs` - lines 35-51: LogsDirectory property

### Secondary (MEDIUM confidence)
- `ChronoView/Infrastructure/Logging/FileLoggerProvider.cs` - File logging implementation
- `ChronoView/App.xaml.cs` - lines 176-201: Logging configuration and session start time
- `skills_scripts/ui_automation/Commands/SettingsCommands.cs` - console-logs command implementation

### Tertiary (LOW confidence)
- [Stack Overflow: Most recent dated file](https://stackoverflow.com/questions/77056021/how-to-get-the-most-recent-dated-file-filename-of-the-form-yyyy-mm-dd-filename) - General pattern reference
- [Unix Community: Finding latest dir by yyyymmdd](https://community.unix.com/t/finding-latest-dir-based-on-its-name-yyyymmdd/311682) - Unix equivalents
- [Microsoft Learn: Basic commands for WSL](https://learn.microsoft.com/en-us/windows/wsl/basic-commands) - WSL path documentation

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All .NET BCL, no external dependencies needed
- Architecture: HIGH - Verified patterns exist in codebase (LogCleanupService, SetupConfigVerifier)
- Pitfalls: HIGH - Identified from code analysis and common directory enumeration issues

**Research date:** 2026-01-21
**Valid until:** 2026-03-01 (stable domain, log structure unlikely to change)
