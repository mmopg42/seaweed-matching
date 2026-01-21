# Phase 31 Context: CLI Schema Standardization

**Created:** 2026-01-21

## Phase Description

Standardize JSON response format across all CLI commands to ensure reliable parsing by test-executor agent.

## Current State

### Existing JSON Output Pattern

The CLI already has a `--json` option that outputs JSON with this pattern:

**Success responses:**
```json
{
  "success": true,
  "data": {
    "launched": true,
    "processId": 12345
  }
}
```

**Error responses:**
```json
{
  "success": false,
  "error": "MainWindow not found",
  "errorCode": 2
}
```

### Current Implementation

- Each command handler defines its own `PrintJsonOutput()` method
- JSON output uses `System.Text.Json.JsonSerializer`
- No standardized `retryable` field
- No standardized `suggestion` field
- Exit codes are defined in `ExitCodes.cs` (0=SUCCESS, 1=ERROR, 2=NOT_FOUND, 3=TIMEOUT, 4=INVALID_ARGUMENT)

### Commands with JSON Support

- AppLifecycleCommands.cs: launch, stop, restart, status
- WindowsCommands.cs: main, setup, settings, preview, all
- DataPanelCommands.cs: stats, headers, rows, data, info, cell, export
- WorkflowCommands.cs, SettingsCommands.cs, FileOpsCommands.cs, etc.

## Gaps to Address

1. **retryable field** - Error responses don't indicate if operation is retryable
2. **suggestion field** - No helpful suggestions for common failures
3. **Consistency** - Each command implements JSON output independently
4. **Documentation** - JSON schemas not formally documented per command category

## Dependencies

- Phase 28 (skill definitions include schema expectations) - Skills registry defines expected response formats
- Phase 30 (executor skill translation) - Executor needs standardized schemas for reliable parsing

## Key Files

- `skills_scripts/ui_automation/Commands/ExitCodes.cs` - Exit code constants
- `skills_scripts/ui_automation/Commands/AppLifecycleCommands.cs` - Example of current JSON pattern
- `skills_scripts/ui_automation/Program.cs` - Main entry point with error wrapper
- `.claude/agents/test-executor-skills.md` - Skills registry with expected schemas
