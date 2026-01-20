# Phase 23: Performance & Documentation - Research

**Researched:** 2026-01-20
**Domain:** UI Automation Test Performance, CLI Documentation Update
**Confidence:** HIGH

## Summary

This phase focuses on two distinct areas: (1) optimizing test execution speed by reducing unnecessary delays while preserving all safety checks, and (2) updating agent documentation to include the new setup workflow commands. Performance improvements target 20-30% speedup through timeout optimization and delay reduction between operations. Documentation updates follow the existing concise table format in test-executor.md and test-orchestrator.md.

**Primary recommendation:** Optimize existing timeout values (DefaultPollIntervalMs 200ms is appropriate), reduce inter-operation delays from 200-500ms to 100ms, and document new setup commands in existing CLI reference tables.

## Standard Stack

### Core
| Component | Current Version/Value | Purpose | Why Standard |
|-----------|----------------------|---------|--------------|
| FlaUI.UIA3 | 5.x | UI Automation framework | Existing project standard |
| System.CommandLine | Built-in | CLI parsing | Already in use for all commands |
| DefaultPollIntervalMs | 200ms | Wait loop polling interval | Balanced between responsiveness and CPU |
| DefaultDialogWaitMs | 3000ms | Settings dialog close timeout | Appropriate for dialog operations |

### Timeout Values (Current State)
| Timeout | Current Value | Usage | Recommended |
|---------|---------------|-------|-------------|
| WaitForMainWindow | 10000ms | Setup to MainWindow transition | Keep (10s appropriate) |
| WaitForWindow | 5000ms | Window detection default | Keep (5s appropriate) |
| WaitForButtonEnabled/Disabled | 5000ms | Button state change | Keep (5s appropriate) |
| WaitForOperationComplete | 30000ms | File operations (move/delete) | Keep (30s appropriate for file ops) |
| WaitForRowCountChange | 5000ms | DataGrid row count change | Keep (5s appropriate) |
| CloseWindow | 5000ms | Window close timeout | Keep (5s appropriate) |
| ClickSaveButton/CancelButton | 3000ms | Dialog close after save/cancel | Keep (3s appropriate) |

### Inter-Operation Delays (Optimization Candidates)
| Location | Current Delay | Purpose | Recommended |
|----------|---------------|---------|-------------|
| ChronoFileOperationsController | 100ms (after select/deselect) | UI update | Keep (minimal, necessary) |
| ChronoFileOperationsController | 200ms (in deselect range loop) | Between batch operations | Reduce to 100ms |
| ChronoSettingsController | 100ms (after textbox input) | UI update | Keep (minimal, necessary) |
| ChronoToolbarController | WaitMs parameter | Custom wait after click | Review usage (see below) |

## Architecture Patterns

### Command Registration Pattern
The project uses a modular CommandRegistry pattern established in Phase 11:

```csharp
// Pattern: ICommandHandler interface
public interface ICommandHandler
{
    void RegisterCommands(RootCommand rootCommand);
}

// Implementation (SetupCommands.cs)
public class SetupCommands : ICommandHandler
{
    public void RegisterCommands(RootCommand rootCommand)
    {
        var setupCommand = new Command("setup", "SetupWindow 제어");
        // Add sub-commands
        rootCommand.AddCommand(setupCommand);
    }
}

// Registration in Program.cs
var registry = new CommandRegistry();
registry.RegisterHandler(new SetupCommands());
registry.RegisterAllCommands(rootCommand);
```

### Setup Command Namespace
All setup-related commands use the `setup` prefix namespace:
- `setup verify-config` - Config comparison
- `setup complete-full` - Full workflow with optional verification

**Note:** Commands `open-settings`, `camera-states` exist in other namespaces:
- `workflow camera-states` - Already implemented
- Settings commands use `settings-dialog` namespace
- Need to verify if `setup open-settings` should be added or if existing command is sufficient

### Exit Code Convention
All command handlers use consistent exit codes:
```csharp
private const int EXIT_SUCCESS = 0;
private const int EXIT_ERROR = 1;
private const int EXIT_NOT_FOUND = 2;
private const int EXIT_TIMEOUT = 3;
private const int EXIT_INVALID_ARGUMENT = 4;
```

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Command parsing | Custom argument parsing | System.CommandLine | Already integrated, type-safe |
| Command registration | Manual switch statements | CommandRegistry + ICommandHandler | Modular, testable, already implemented |
| JSON output | Custom JSON formatting | JsonSerializer with consistent options | Already in use across all commands |
| Window finding | Direct FlaUI calls | ChronoWindowFinder | Centralized logic, handles edge cases |
| SetupWindow control | Direct FlaUI calls | ChronoSetupWindowController | Encapsulates all SetupWindow interactions |

## Common Pitfalls

### Pitfall 1: Overly Aggressive Timeout Reduction
**What goes wrong:** Reducing timeouts too low causes flaky tests due to race conditions with UI rendering.
**Why it happens:** Assumption that all UI operations complete immediately.
**How to avoid:** Keep window detection timeouts at 5-10 seconds. Only reduce inter-operation delays.
**Warning signs:** Intermittent "timeout" failures, tests passing locally but failing in CI.

### Pitfall 2: Removing Safety Checks for Speed
**What goes wrong:** Tests proceed without verifying the application state, leading to cascading failures.
**Why it happens:** Premature optimization - skipping "expensive" checks like window detection.
**How to avoid:** Per CONTEXT.md requirement - ALL connectivity checks and validations must remain. Speed comes from delay optimization only.
**Warning signs:** Tests failing with "element not found" errors, inconsistent behavior.

### Pitfall 3: Documentation Drift
**What goes wrong:** Agent documentation references commands that don't exist or have different syntax.
**Why it happens:** Commands added without updating documentation.
**How to avoid:** When adding commands, immediately update both test-executor.md and test-orchestrator.md.
**Warning signs:** Agents trying invalid commands, "command not found" errors.

### Pitfall 4: Inconsistent JSON Output Format
**What goes wrong:** Programmatic consumption of CLI output fails due to format variations.
**Why it happens:** Each handler implementing its own JSON format.
**How to avoid:** Use the established PrintJsonOutput pattern:
```csharp
private static void PrintJsonOutput(object data)
{
    Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions
    {
        WriteIndented = false
    }));
}
```

## Code Examples

### Adding a New Setup Command (Pattern)
```csharp
// Source: SetupCommands.cs, existing pattern
var newCommand = new Command("command-name", "Description");
newCommand.AddOption(jsonOption);
newCommand.SetHandler((param1, json) =>
{
    // Command logic
    if (json)
    {
        PrintJsonOutput(new { success = true, data = ... });
    }
    else
    {
        Console.WriteLine("[command-name] Success message");
    }
    Environment.Exit(EXIT_SUCCESS);
}, param1, jsonOption);
setupCommand.AddCommand(newCommand);
```

### Optimizing Inter-Operation Delay
```csharp
// Before (potentially excessive delay)
Thread.Sleep(200); // Between operations

// After (optimized while maintaining stability)
Thread.Sleep(100); // Reduced but still allows UI update

// Note: Do NOT reduce DefaultPollIntervalMs (200ms) - this is the wait loop
// polling interval and reducing it increases CPU usage without meaningful speedup.
```

### Timeout Pattern (Keep These Values)
```csharp
// Window detection - keep these timeouts
public Window? WaitForWindow(string titleSubstring, int timeoutMs = 5000)
public bool WaitForButtonEnabled(string buttonText, int timeoutMs = 5000)
public Window? WaitForMainWindow(int timeoutMs = 10000) // Longer for app transition

// File operations - keep these timeouts (files can be slow)
public bool WaitForMoveComplete(int timeoutMs = 30000)
public bool WaitForDeleteComplete(int timeoutMs = 30000)
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Linear command parsing in Program.cs | Modular CommandRegistry + ICommandHandler | Phase 11 (v1.1) | Program.cs: 3611 -> 60 lines |
| Inconsistent exit codes | Standardized exit code constants | Phase 11 | All handlers use same codes |
| Scattered timeout values | Centralized timeout constants | Ongoing | Improved consistency |

**Current best practices (established):**
- Use `setup` prefix namespace for setup-related commands
- Provide `--json` option for programmatic consumption
- Use `Environment.Exit()` for proper exit codes
- Keep safety checks - optimize delays, not timeouts

## Open Questions

### Q1: `setup open-settings` Command Status
**What we know:** CONTEXT.md mentions this as a required CLI-01 command.
**What's unclear:** Whether this command already exists or needs to be created.
**Investigation needed:** Check if `settings-dialog open` (existing in SettingsCommands.cs) serves this purpose, or if a dedicated `setup open-settings` command is needed that first finds SetupWindow then clicks its settings button.

**Current state:**
- `settings-dialog open` - Opens settings from MainWindow
- `ChronoSetupWindowController.ClickSettingsButton()` - Method exists to click SetupWindow's settings button
- Potential missing: CLI command to open settings FROM SetupWindow specifically

### Q2: `setup camera-states` Command Location
**What we know:** CONTEXT.md mentions this as required CLI-01. Currently exists as `workflow camera-states`.
**What's unclear:** Whether to add a duplicate `setup camera-states` command or if the existing `workflow camera-states` is sufficient.

**Recommendation:** The existing `workflow camera-states` returns camera states from WorkflowPanel. If setup-specific states are needed (SetupWindow button states), this would be a new command using `ChronoSetupWindowController.GetCameraStates()`.

### Q3: Documentation Update Scope
**What we know:** PERF-02 requires updating test-executor.md and test-orchestrator.md.
**What's unclear:** Exact format and location for new setup command documentation.

**Recommendation:** Follow existing CLI reference table format. Add "Setup Commands" section to both docs.

## Sources

### Primary (HIGH confidence)
- `skills_scripts/ui_automation/Commands/CommandRegistry.cs` - Registry pattern
- `skills_scripts/ui_automation/Commands/ICommandHandler.cs` - Handler interface
- `skills_scripts/ui_automation/Commands/SetupCommands.cs` - Existing setup commands
- `skills_scripts/ui_automation/Program.cs` - Registration pattern
- `skills_scripts/ui_automation/ChronoSetupWindowController.cs` - SetupWindow controller API
- `.claude/agents/test-executor.md` - Current executor documentation format
- `.claude/agents/test-orchestrator.md` - Current orchestrator documentation format
- `.planning/phases/23-performance-documentation/23-CONTEXT.md` - Phase requirements

### Secondary (MEDIUM confidence)
- `skills_scripts/ui_automation/Commands/WorkflowCommands.cs` - For command patterns
- `skills_scripts/ui_automation/Commands/WindowsCommands.cs` - For window command patterns
- `.planning/STATE.md` - Project state and Phase 22 deliverables

### Tertiary (LOW confidence)
- None - all findings verified against source code

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All values verified against source code
- Architecture: HIGH - Patterns verified against existing implementations
- Pitfalls: HIGH - Based on established UI automation best practices
- Open questions: LOW - Require verification before implementation

**Research date:** 2026-01-20
**Valid until:** 60 days (stable domain, UI automation patterns don't change rapidly)
