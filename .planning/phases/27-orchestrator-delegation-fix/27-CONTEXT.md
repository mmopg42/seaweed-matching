# Phase 27: Orchestrator Delegation Fix - Context

**Gathered:** 2026-01-21
**Status:** Ready for planning

## Phase Boundary

Refactor test-orchestrator to strictly follow delegation pattern — orchestrator defines test scenarios and synthesizes results, but NEVER directly executes commands via Bash. All test execution goes through test-executor subagent, all log analysis goes through log-analyst subagent.

This is a code fix phase, not a feature addition. The delegation pattern is already documented; we need to ensure it's actually followed in the agent prompt.

## Implementation Decisions

### Delegation Boundary
- **Task tool only**: Orchestrator NEVER uses Bash tool for any execution
- **Allow direct reads**: Orchestrator can use Read/Grep tools for inspection but never Bash for execution
- **No CLI invocations**: All ui_automation.exe commands must go through test-executor delegation
- **Explicit prohibition**: Add clear "NEVER use Bash tool" section with examples of what NOT to do

### Error Handling
- **Retry once**: When test-executor delegation fails, retry one time before reporting failure
- **Retry analysis only**: If log-analyst fails but tests succeeded, retry log-analyst delegation before finalizing
- **Never fallback**: Direct execution defeats the purpose — always report delegation failure, never fall back to Bash
- **Dedicated section**: Add "Delegation Issues" section to report format for aggregation

### Command Specification
- **Hybrid approach**: Specify operation and parameters, let test-executor build full CLI command
- **Structured template**: Use template format like "Execute: [operation] with args: [key=value, ...]"
- **General guidance for analysis**: For log-analyst, describe what to look for broadly (e.g., "errors related to file monitoring") rather than exact regex patterns

### Verification Approach
- **Both code review and test execution**: Code review first, then test execution to confirm behavior
- **Both checks**: Search for Bash tool usage AND verify proper Task delegation patterns (test-executor/log-analyst subagent_type)
- **Focused feature test**: Use complex scenario with multiple tiers to exercise full delegation pattern

### Claude's Discretion
- Exact wording of delegation templates
- Which specific feature to use for verification test
- How to structure the "Delegation Issues" report section

## Specific Ideas

- The test-orchestrator.md documentation already describes the delegation pattern correctly (lines 8-10: "CRITICAL: You are NOT an executor")
- The issue may be that the guidance isn't strong enough — needs explicit "NEVER use Bash" examples
- Plan 27-01 should modify the agent file, Plan 27-02 should verify with actual test execution

## Deferred Ideas

None — discussion stayed within phase scope.

---

*Phase: 27-orchestrator-delegation-fix*
*Context gathered: 2026-01-21*
