# Phase 20: App Lifecycle Commands - Research

**Researched:** 2025-01-20
**Domain:** .NET Process Management (System.Diagnostics.Process)
**Confidence:** HIGH

<research_summary>
## Summary

Researched .NET process management APIs for implementing ChronoView app lifecycle commands (launch/stop/restart/status). The standard approach uses `System.Diagnostics.Process` for process management - a stable, well-documented API that hasn't significantly changed since .NET 8.

Key finding: This is a commodity domain with established patterns. The core APIs are `Process.Start()` for launching, `Process.GetProcessesByName()` for detection, and `Process.Kill()` for termination. The main complexity is async process launching - `dotnet run` blocks by default, so proper async handling with redirected streams is required for non-blocking launch.

**Primary recommendation:** Use System.Diagnostics.Process with async Task.Run wrapper for launch, Process.GetProcessesByName() for status detection, and Process.Kill() for termination. No external libraries needed - the built-in .NET APIs are sufficient and battle-tested.
</research_summary>

<standard_stack>
## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| System.Diagnostics.Process | .NET 10 | Process launch, detection, termination | Built-in .NET API, stable for 10+ years |
| System.Threading.Tasks | .NET 10 | Async process operations | Standard C# async/await pattern |
| Microsoft.Extensions.DependencyInjection | 10.0 | DI registration of new handler | Project already uses this pattern |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.CommandLine | 2.0.0-beta4 | CLI command definition | Already in project for all commands |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Process.Start | Windows specific API | Process.Start is cross-platform, sufficient |
| Direct process killing | Named pipes/IPC | Overkill for simple lifecycle commands |

**Installation:**
```bash
# No new packages needed - all APIs are in System.Diagnostics namespace
# Already referenced in net10.0 target framework
```
</standard_stack>

<architecture_patterns>
## Architecture Patterns

### Recommended Project Structure
```
skills_scripts/ui_automation/
├── Handlers/
│   ├── AppLifecycleCommands.cs    # NEW: app lifecycle handler
│   └── [existing 10 handlers]
├── Models/
│   └── ProcessStatus.cs           # NEW: process status DTO
└── Program.cs                      # Register new handler
```

### Pattern 1: Async Process Launch with Task.Run
**What:** Wrap synchronous Process.Start in Task.Run for non-blocking async launch
**When to use:** Launching `dotnet run` or any long-running process
**Example:**
```csharp
// Source: Standard async pattern for Process.Start
public async Task<int> LaunchAsync(string projectPath, CancellationToken ct)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --project \"{projectPath}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    var process = Process.Start(startInfo);
    // Return immediately - don't wait for exit
    return process?.Id ?? 0;
}
```

### Pattern 2: Process Detection by Name
**What:** Use Process.GetProcessesByName() to find running processes
**When to use:** Status detection, checking if app is already running
**Example:**
```csharp
// Source: System.Diagnostics.Process API
public bool IsProcessRunning(string processName)
{
    // Don't include ".exe" extension
    var processes = Process.GetProcessesByName(processName);
    return processes.Length > 0;
}

public ProcessStatus GetStatus(string processName)
{
    var processes = Process.GetProcessesByName(processName);
    return new ProcessStatus
    {
        IsRunning = processes.Length > 0,
        ProcessCount = processes.Length,
        ProcessIds = processes.Select(p => p.Id).ToArray()
    };
}
```

### Pattern 3: Clean Process Termination
**What:** Kill all instances of named process, with graceful retry
**When to use:** Stop/restart commands
**Example:**
```csharp
// Source: Process.Kill() pattern with cleanup
public async Task<int> StopAsync(string processName)
{
    var processes = Process.GetProcessesByName(processName);
    int killed = 0;

    foreach (var process in processes)
    {
        try
        {
            process.Kill();
            process.WaitForExit(5000); // Wait up to 5 seconds
            killed++;
        }
        catch (Exception ex)
        {
            // Log but continue trying other instances
            Console.WriteLine($"Failed to kill process {process.Id}: {ex.Message}");
        }
    }

    return killed;
}
```

### Pattern 4: Restart = Stop + Launch
**What:** Combine stop and launch for restart functionality
**When to use:** Restart command, recovery from crashes
**Example:**
```csharp
public async Task<int> RestartAsync(string processName, string projectPath)
{
    // Stop first
    await StopAsync(processName);

    // Brief pause for cleanup
    await Task.Delay(500);

    // Launch again
    return await LaunchAsync(projectPath, CancellationToken.None);
}
```

### Anti-Patterns to Avoid
- **Using Process.WaitForExit() on launch:** Blocks indefinitely for GUI apps, only use for CLI tools that exit
- **Not checking process name format:** Process.GetProcessesByName() fails if ".exe" is included
- **Assuming single process:** Multiple instances may exist - handle arrays
- **Not disposing Process objects:** Can leak handles, use using statements or Dispose()
</architecture_patterns>

<dont_hand_roll>
## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Process launch | Custom shell execution | Process.Start / ProcessStartInfo | Handles argument escaping, working directory, environment |
| Async process handling | Manual thread management | Task.Run + async/await | Standard C# async pattern, integrates with System.CommandLine |
| Process enumeration | WMI queries | Process.GetProcessesByName() | Simpler, cross-platform, built-in |
| Process tree termination | Recursive child killing | Process.Kill() on parent | Windows handles child process cleanup |
| Output streaming | Manual buffer reading | RedirectStandardOutput/Error | Built-in async stream handling |

**Key insight:** Process management in .NET is a solved problem. The System.Diagnostics.Process API handles all the edge cases of process lifecycle. Custom implementations often miss edge cases like zombie processes, handle leaks, or argument escaping issues.
</dont_hand_roll>

<common_pitfalls>
## Common Pitfalls

### Pitfall 1: Process Name with ".exe" Extension
**What goes wrong:** `Process.GetProcessesByName("ChronoView.exe")` returns empty array
**Why it happens:** API expects process name WITHOUT extension
**How to avoid:** Always strip `.exe` before calling:
```csharp
var name = Path.GetFileNameWithoutExtension("ChronoView.exe");
var processes = Process.GetProcessesByName(name);
```
**Warning signs:** Zero matches when process is clearly running in Task Manager

### Pitfall 2: Blocking WaitForExit on GUI Application
**What goes wrong:** Calling `WaitForExit()` after launching WPF app blocks forever
**Why it happens:** GUI apps don't exit until user closes them
**How to avoid:** Don't call WaitForExit() for launch - return immediately after Process.Start()
**Warning signs:** CLI hangs after "launch" command, need Ctrl+C to break

### Pitfall 3: Not Redirecting Streams Causes Deadlock
**What goes wrong:** Process hangs when output fills buffer
**Why it happens:** If not redirected, process blocks when stdout buffer fills
**How to avoid:** Always redirect streams if you might read output:
```csharp
startInfo.RedirectStandardOutput = true;
startInfo.RedirectStandardError = true;
```
**Warning signs:** `dotnet run` starts but app never appears, process hangs

### Pitfall 4: Race Condition on Stop-Then-Launch
**What goes wrong:** Restart command fails because old process still shutting down
**Why it happens:** Process.Kill() returns immediately, but process takes time to exit
**How to avoid:** Add small delay between stop and launch:
```csharp
await StopAsync(processName);
await Task.Delay(500); // Let OS clean up
await LaunchAsync(projectPath, ct);
```
**Warning signs:** Second instance fails to start with "port in use" or file lock errors

### Pitfall 5: Process ID Reuse (TOCTOU)
**What goes wrong:** Process ID from GetProcessesByName() is reused by different process
**Why it happens:** Time-of-check-to-time-of-use between detection and Kill()
**How to avoid:** Store Process object, not just ID, and check HasExited before operations
**Warning signs:** Kill() throws "process has exited" exception randomly
</common_pitfalls>

<code_examples>
## Code Examples

Verified patterns from official sources:

### Basic Status Detection
```csharp
// Source: System.Diagnostics.Process.GetProcessesByName API
public ProcessStatus GetChronoViewStatus()
{
    // Note: ChronoView process name - verify actual process name
    var processes = Process.GetProcessesByName("ChronoView");

    return new ProcessStatus
    {
        IsRunning = processes.Length > 0,
        ProcessCount = processes.Length,
        ProcessIds = processes.Select(p => p.Id).ToArray(),
        MainWindowTitles = processes
            .Select(p => p.MainWindowTitle)
            .Where(title => !string.IsNullOrEmpty(title))
            .ToArray()
    };
}
```

### Non-Blocking Launch
```csharp
// Source: Standard async process pattern
public async Task<int> LaunchChronoViewAsync(string projectPath, CancellationToken ct)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --project \"{projectPath}\"",
        WorkingDirectory = Path.GetDirectoryName(projectPath) ?? ".",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    return await Task.Run(() =>
    {
        var process = Process.Start(startInfo);
        return process?.Id ?? 0;
    }, ct);
}
```

### Clean Stop with Error Handling
```csharp
// Source: Process.Kill() with exception handling
public async Task<StopResult> StopChronoViewAsync()
{
    var processes = Process.GetProcessesByName("ChronoView");
    var result = new StopResult();

    foreach (var process in processes)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill();
                if (process.WaitForExit(5000))
                {
                    result.KilledCount++;
                }
                else
                {
                    result.TimeoutCount++;
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited
            result.AlreadyExitedCount++;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"PID {process.Id}: {ex.Message}");
        }
        finally
        {
            process.Dispose();
        }
    }

    return result;
}
```
</code_examples>

<sota_updates>
## State of the Art (2024-2025)

What's changed recently:

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Process.Start() synchronous | Wrap in Task.Run for async | .NET Core 3.0+ | Modern async/await pattern preferred |
| WMI process queries | Process.GetProcessesByName() | .NET 5+ | Cross-platform support, simpler API |

**New tools/patterns to consider:**
- **ProcessStartInfo.ArgumentList:** .NET 5+ - safer argument handling (avoids injection)
- **CancellationTokenSource:** For timeout-based process operations

**Deprecated/outdated:**
- **Process.GetProcesses() without filter:** Still works but less efficient than GetProcessesByName()
- **WMI queries:** Overly complex for basic process management, use Process APIs instead
</sota_updates>

<open_questions>
## Open Questions

Things that couldn't be fully resolved:

1. **ChronoView Process Name**
   - What we know: WPF apps typically use assembly name as process name
   - What's unclear: Exact process name when running via `dotnet run` (could be `ChronoView` or `dotnet`)
   - Recommendation: Test with `Process.GetProcesses()` during implementation to verify actual process name

2. **Graceful Shutdown from External Process**
   - What we know: Process.Kill() is forceful, WPF apps prefer Application.Current.Shutdown()
   - What's unclear: How to trigger graceful shutdown from external CLI without IPC
   - Recommendation: Use Process.Kill() for simplicity - acceptable for test automation scenario where clean state between tests is priority over graceful shutdown

</open_questions>

<sources>
## Sources

### Primary (HIGH confidence)
- /websites/learn_microsoft_en-us_dotnet - Process.Start, dotnet run, Process.GetProcessesByName
- [Process.GetProcessesByName Method (System.Diagnostics)](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.getprocessesbyname?view=net-10.0) - Official API reference
- [Process.WaitForExit Method](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.waitforexit?view=net-10.0) - Official API reference
- [Process.ExitCode Property](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.exitcode?view=net-10.0) - Official API reference
- [Application Management Overview - WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/application-management-overview) - WPF lifecycle (updated May 2025)

### Secondary (MEDIUM confidence)
- [How to Launch and Control Processes in C#.NET](https://medium.com/c-sharp-programming/how-to-launch-and-control-processes-in-c-net-applications-4ae6565410d6) - Process control patterns (verified against docs)
- [The right way to run external process in .NET (async version)](https://gist.github.com/AlexMAS/276eed492bc989e13dcce7c78b9e179d) - Async implementation (verified pattern)
- [How do I exit a WPF application programmatically?](https://stackoverflow.com/questions/28296357/how-do-i-exit-a-wpf-application-programmatically) - WPF shutdown methods

### Tertiary (LOW confidence - needs validation)
- [zombie process when start a linux process in dotnet core](https://github.com/dotnet/runtime/issues/21661) - Linux-specific issue, Windows CLI not affected
- [Process.WaitForExit() hangs forever on Linux](https://github.com/dotnet/runtime/issues/32225) - Linux-specific, Windows CLI not affected
</sources>

<metadata>
## Metadata

**Research scope:**
- Core technology: System.Diagnostics.Process in .NET 10
- Ecosystem: async/await patterns, ProcessStartInfo configuration
- Patterns: Process detection, async launch, clean termination
- Pitfalls: Process naming, blocking operations, race conditions

**Confidence breakdown:**
- Standard stack: HIGH - built-in .NET APIs, stable for years
- Architecture: HIGH - standard async/await patterns, verified with official docs
- Pitfalls: HIGH - well-documented edge cases in Process class
- Code examples: HIGH - verified against Microsoft documentation

**Research date:** 2025-01-20
**Valid until:** 2025-07-20 (180 days - .NET process APIs are highly stable)

**Conclusion:** This is a commodity domain with established patterns. No niche research needed - standard .NET process APIs are sufficient. The RESEARCH.md exists for documentation purposes but doesn't reveal any groundbreaking discoveries. Planning can proceed with high confidence in the standard approach.
</metadata>

---

*Phase: 20-app-lifecycle-commands*
*Research completed: 2025-01-20*
*Ready for planning: yes*
