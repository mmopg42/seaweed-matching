# Phase 24: Error Diagnosis - Research

**Researched:** 2026-01-21
**Domain:** System.CommandLine error handling, C# CLI exit code management
**Confidence:** HIGH

## Summary

The "Exit code 1" error message problem occurs because unhandled exceptions in command handlers are caught by System.CommandLine's built-in exception handler, which translates them to exit code 1 without providing detailed error information to the caller. The current codebase uses `Environment.Exit()` extensively throughout command handlers, which bypasses System.CommandLine's exception handling middleware and prevents detailed error context from being propagated.

**Primary recommendation:** Implement a centralized error handling pattern that wraps `InvokeAsync()` at the Program.cs level, capturing exceptions and writing structured error messages to stderr before returning appropriate exit codes.

## Standard Stack

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| System.CommandLine | 2.0.0-beta4.22272.1 | CLI parsing and command routing | Already in use, provides built-in exception handling middleware |
| FlaUI.UIA3 | 5.0.0 | Windows UI automation | Core dependency for UI automation operations |

**Current architecture:**
- `Program.cs` uses `System.CommandLine.RootCommand` with `InvokeAsync()`
- Commands registered via `CommandRegistry` and `ICommandHandler` pattern
- Each command handler directly calls `Environment.Exit()` with exit codes

## Architecture Patterns

### Current Error Handling Pattern (Problematic)

**What:** Each command handler directly calls `Environment.Exit()` with hardcoded exit codes
**Why it's problematic:**
- Bypasses System.CommandLine's exception handling middleware
- No centralized error logging or diagnostics
- Exceptions in automation code (FlaUI) result in generic "Exit code 1"
- No stack traces or error details propagated to caller

**Example from SetupCommands.cs:**
```csharp
// Line 93 - Direct Environment.Exit without error context
Environment.Exit(exitCode);
```

### Recommended Pattern: Centralized Exception Handler

**What:** Wrap `InvokeAsync()` in a try-catch block at Program.cs level
**When to use:** All CLI applications using System.CommandLine

**Example:**
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/standard/commandline/how-to-parse-and-invoke
static async Task<int> Main(string[] args)
{
    var rootCommand = new RootCommand("Windows UI Automation - FlaUI 5.x based CLI tool");
    // ... set up commands ...

    try
    {
        return await rootCommand.InvokeAsync(args);
    }
    catch (Exception ex)
    {
        // Write structured error to stderr
        Console.Error.WriteLine($"Error: {ex.Message}");
        Console.Error.WriteLine($"Command: {string.Join(" ", args)}");
        if (s_isVerbose)
        {
            Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        return 1; // EXIT_ERROR
    }
}
```

### Return-Based Exit Codes (Alternative Pattern)

**What:** Command handlers return `int` instead of calling `Environment.Exit()`
**When to use:** When you want System.CommandLine to manage process exit

**Example:**
```csharp
// Instead of Environment.Exit(returnCode), the handler returns the value
verifyConfigCommand.SetHandler((configPath, openSettings, strict, json) =>
{
    // ... command logic ...
    return exitCode; // Return instead of Environment.Exit()
});
```

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Exception logging middleware | Custom try-catch in every handler | Centralized handler in Main | System.CommandLine has built-in `UseExceptionHandler` middleware |
| Exit code constants scattered | Duplication across 12+ files | Shared static class | Single source of truth, easier to maintain |
| Error message formatting | Custom Console.WriteLine patterns | Console.Error with structured format | Standard error stream convention, better for scripting |

## Common Pitfalls

### Pitfall 1: Environment.Exit() Bypasses Exception Handling
**What goes wrong:** `Environment.Exit()` terminates the process immediately, preventing any cleanup or logging in outer scopes
**Why it happens:** Developers use `Environment.Exit()` as a quick way to return exit codes from handlers
**How to avoid:** Return `int` from handlers and let System.CommandLine manage exit codes
**Warning signs:** Error messages say "Exit code 1" with no additional context

### Pitfall 2: FlaUI Exceptions Swallowed
**What goes wrong:** `FlaUI.Core.Exceptions.*` exceptions occur but aren't logged with context
**Why it happens:** Try-catch blocks in automation code (e.g., `ChronoWindowFinder.cs:146`) return `null` instead of propagating
**How to avoid:** Let exceptions propagate to centralized handler or log with full context before returning null
**Warning signs:** Commands fail with "not found" when the actual error was a UI automation failure

### Pitfall 3: No stderr Usage
**What goes wrong:** Error messages written to stdout mix with normal output
**Why it happens:** Using `Console.WriteLine()` for errors instead of `Console.Error.WriteLine()`
**How to avoid:** Write errors to stderr, normal output to stdout
**Warning signs:** Piping commands breaks because error messages corrupt JSON output

## Code Examples

### Current Problematic Pattern
```csharp
// From SetupCommands.cs:93
Environment.Exit(exitCode); // Exits immediately, no error context
```

### Recommended Fix Pattern
```csharp
// In Program.cs - wrap InvokeAsync with exception handling
try
{
    return await rootCommand.InvokeAsync(args);
}
catch (FlaUI.Core.Exceptions.AutomationException ex)
{
    Console.Error.WriteLine($"[UI Automation Error] {ex.Message}");
    Console.Error.WriteLine($"Command: {string.Join(" ", args)}");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[Error] {ex.GetType().Name}: {ex.Message}");
    Console.Error.WriteLine($"Command: {string.Join(" ", args)}");
    return 1;
}
```

### Command Handler Pattern (Return-based)
```csharp
// Recommended: Return int instead of Environment.Exit
verifyConfigCommand.SetHandler((configPath, openSettings, strict, json) =>
{
    try
    {
        var actualConfigPath = configPath ?? DefaultSimulatorConfigPath;
        using var verifier = new SetupVerifier(actualConfigPath);
        var result = verifier.Verify(openSettingsIfNeeded: openSettings);

        if (json)
        {
            Console.WriteLine(result.ToJson());
        }
        else
        {
            PrintVerificationResult(result);
        }

        // Determine exit code
        int exitCode = EXIT_SUCCESS;
        if (!result.Success)
        {
            exitCode = strict ? EXIT_ERROR : EXIT_SUCCESS;
        }
        if (result.Error == "Simulator config file not found")
        {
            exitCode = EXIT_NOT_FOUND;
        }

        return exitCode; // Return instead of Environment.Exit()
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[setup verify-config] Error: {ex.Message}");
        return EXIT_ERROR;
    }
}, configPathOption, openSettingsOption, strictOption, jsonOption);
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Direct Environment.Exit() | Return-based exit codes with centralized handler | Phase 24 | Better error reporting, consistent diagnostics |
| No stderr usage | Console.Error for errors | Phase 24 | Proper separation of output streams |

**Current System.CommandLine behavior (beta4):**
- Parse errors automatically written to stderr with exit code 1
- Unhandled exceptions in handlers result in exit code 1 with no message
- `InvokeAsync()` returns `Task<int>` for exit code propagation

**Source:** [Microsoft Learn - How to parse and invoke](https://learn.microsoft.com/en-us/dotnet/standard/commandline/how-to-parse-and-invoke)

## Root Causes Identified

### Why "Exit code 1" is Generic

1. **System.CommandLine Default Behavior:** When an exception occurs in a command handler, System.CommandLine's built-in exception handler catches it and returns exit code 1, but the error message may not be displayed depending on the exception type and where it occurs.

2. **Environment.Exit() Bypass:** Many handlers call `Environment.Exit()` directly, which terminates immediately without propagating exception information.

3. **No Centralized Handler:** There's no try-catch wrapper around `InvokeAsync()` in Program.cs to capture and format exceptions.

4. **FlaUI Exception Types:** The following exceptions can occur but aren't specifically handled:
   - `FlaUI.Core.Exceptions.AutomationException`
   - `FlaUI.Core.Exceptions.PropertyNotSupportedException`
   - `FlaUI.Core.Exceptions.PatternNotSupportedExcpetion`
   - `FlaUI.Core.Exceptions.TimeExpiredException`

### Files Requiring Modification

| File | Lines | Current Issue | Fix |
|------|-------|---------------|-----|
| `Program.cs` | 60 | No exception wrapper around `InvokeAsync()` | Add try-catch with stderr output |
| `SetupCommands.cs` | 93, 145, 178, 211, 245, 272, 302, 329 | Direct `Environment.Exit()` calls | Return int or throw |
| `WindowsCommands.cs` | 70, 87, 124, 141, 170, 208, 246, 282, 299, 336, 353, 393 | Direct `Environment.Exit()` calls | Return int or throw |
| `WorkflowCommands.cs` | 46, 51, 65, 70, 84, 89, 103, 108 | Direct `Environment.Exit()` calls | Return int or throw |
| `AppLifecycleCommands.cs` | Multiple | Direct `Environment.Exit()` calls | Return int or throw |
| `DataPanelCommands.cs` | Multiple | Direct `Environment.Exit()` calls | Return int or throw |
| `FileOpsCommands.cs` | Multiple | Direct `Environment.Exit()` calls | Return int or throw |
| `SettingsCommands.cs` | Multiple | Direct `Environment.Exit()` calls | Return int or throw |
| `TestCommands.cs` | Multiple | Direct `Environment.Exit()` calls | Return int or throw |
| `ToolbarCommands.cs` | Multiple | Direct `Environment.Exit()` calls | Return int or throw |
| `UtilityCommands.cs` | Multiple | Direct `Environment.Exit()` calls | Return int or throw |

## Open Questions

1. **SetHandler Return Type:** The current codebase uses `SetHandler()` without return values. Need to verify if the beta4 version supports `SetHandler` overloads that return `int` from the handler delegate.

2. **Backward Compatibility:** Will changing from `Environment.Exit()` to return-based exit codes break existing scripts that depend on current behavior?

## Sources

### Primary (HIGH confidence)
- [Microsoft Learn - How to parse and invoke the result](https://learn.microsoft.com/en-us/dotnet/standard/commandline/how-to-parse-and-invoke) - Official System.CommandLine documentation on exit codes and exception handling
- [Stack Overflow - System.CommandLine Exception Handling](https://stackoverflow.com/questions/61604553/system-commandline-exception-handling) - Community guidance on returning exit codes from handlers

### Secondary (MEDIUM confidence)
- [GitHub Issue: Exception handling around commands](https://github.com/dotnet/command-line-api/issues/796) - Discussion on built-in exception handler middleware
- [GitHub Issue: SetHandler overrides for exit codes](https://github.com/dotnet/command-line-api/issues/1570) - Discussion on exit code return patterns

### Code Analysis (HIGH confidence)
- `Program.cs` - Current CLI entry point using `InvokeAsync()`
- `Commands/SetupCommands.cs` - Example of direct `Environment.Exit()` usage
- `Commands/WindowsCommands.cs` - Example of JSON error output pattern
- `UiAutomation.cs` - Exception handling patterns in automation code
- `ChronoWindowFinder.cs` - Error handling in window finding logic

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Direct code examination, official documentation
- Architecture: HIGH - Code analysis, System.CommandLine documentation review
- Pitfalls: HIGH - Identified through code review and documented behavior
- Root causes: HIGH - Direct examination of error handling patterns

**Research date:** 2026-01-21
**Valid until:** 60 days (stable library versions, documented patterns)
