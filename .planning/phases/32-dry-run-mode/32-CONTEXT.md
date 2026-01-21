# Phase 32: Dry-Run Mode - Context

**Gathered:** 2026-01-22
**Status:** Ready for planning

<domain>
## Phase Boundary

Add `--dry-run` flag to all CLI commands for safe command validation. Dry-run validates skill existence, parameter syntax, and schema compliance without executing commands or checking runtime state. Returns structured JSON showing what command would execute.
</domain>

<decisions>
## Implementation Decisions

### Output format
- **Structured JSON response** matching SuccessResponse<T> format with standard fields (success, data, timestamp)
- **Mark as dry-run:** Include `dryRun: true` field in response to distinguish from real executions
- **Data fields:** Return `skill`, `cli`, and `args` — full context for debugging and verification
- **ISO 8601 timestamps:** Include timestamp in 'o' format for audit trail (dry-run is still an event)

**Success response schema:**
```json
{
  "success": true,
  "dryRun": true,
  "timestamp": "2026-01-22T00:30:20Z",
  "data": {
    "skill": "APP_LAUNCH",
    "cli": "ui_automation.exe app launch --json",
    "args": {}
  }
}
```

### Validation depth
- **Syntax validation only** — dry-run does NOT check runtime state (app running, windows available, file existence)
- **Schema validation:** Validate argument types, required vs optional fields against skill schema
- **Skill existence:** Check skill name exists in test-executor-skills.md registry
- **No runtime overhead:** Dry-run must be fast — no UI automation, no app state queries

**Validates:**
- Skill name exists in registry
- Required arguments are provided
- Argument types match schema (int, string, bool, array)
- Flag names are valid

**Skips:**
- Is ChronoView running?
- Does MainWindow exist?
- Do row indices exist in DataGrid?
- Are files/folders accessible?

### Error behavior
- **Same ErrorResponse format** as real executions for consistency
- **retryable: always false** — dry-run errors are syntax errors, not transient failures
- **All validation errors reported:**
  - Unknown skill (errorCode: 4)
  - Missing required arguments (errorCode: 4)
  - Type mismatch (errorCode: 4)
  - Invalid flag name (errorCode: 4)
- **Helpful suggestions:** Include suggestion field with specific guidance for each error type

**Error response schema:**
```json
{
  "success": false,
  "dryRun": true,
  "error": "Unknown skill: APP_LAUNCHX",
  "errorCode": 4,
  "retryable": false,
  "suggestion": "Did you mean APP_LAUNCH? See test-executor-skills.md",
  "timestamp": "2026-01-22T00:30:20Z"
}
```

### Claude's Discretion
- Exact error message wording and helpfulness level
- Whether to include argument value in error messages (e.g., "Row index 99 out of range")
- Timestamp precision (milliseconds vs seconds)

</decisions>

<specifics>
## Specific Ideas

- "Dry-run is like a compiler — it validates syntax without running the program"
- "The dryRun: true flag prevents orchestrators from mistakenly treating validation as execution"
- "Schema validation catches typos and missing args before any real execution"

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 32-dry-run-mode*
*Context gathered: 2026-01-22*
