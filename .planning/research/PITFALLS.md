# Domain Pitfalls: CLI Skill Encapsulation for Automation

**Domain:** CLI automation skill layer
**Researched:** 2026-01-21
**Focus:** Common mistakes when creating CLI skill encapsulation and delegation patterns

---

## Executive Summary

This document catalogs pitfalls specific to building CLI skill abstraction layers for AI agent automation. The research draws from both industry patterns (2025) and our project's real experiences with `ui_automation.exe` command hallucination failures.

**The core problem:** When AI agents interface with CLI tools, they "hallucinate" commands that seem plausible but do not exist. Without a verification layer between intent and execution, these failures cascade through the automation pipeline.

---

## Critical Pitfalls

Mistakes that cause automation failures and require significant rework.

### Pitfall 1: Command Hallucination Without Verification

**What goes wrong:**

The agent generates commands that seem syntactically correct but do not exist in the actual CLI tool.

**Real example from our project:**

```bash
# Agent hallucinated this:
ui_automation workflow switch-line --line 2 --json
# Error: 'switch-line' doesn't exist

# Actual correct command:
ui_automation workflow select-tab "Line 2"
```

**Why it happens:**

1. LLMs pattern-match from training data (many CLIs have `switch-*` commands)
2. The agent has no way to verify command existence before execution
3. Error messages arrive AFTER execution, wasting time and breaking automation flow

**Consequences:**

- Automation chains break mid-execution
- Downstream agents receive unexpected error outputs
- Time lost on debugging non-existent commands
- Loss of trust in automation system

**Prevention:**

```markdown
# Strategy 1: Pre-execution Verification Layer

Before executing any CLI command:

1. Run discovery command to get available commands
   ui_automation.exe --help [command-group]

2. Validate subcommand existence against discovered list

3. Only execute if validation passes

# Strategy 2: Intent-to-Command Registry

Maintain explicit intent-to-command mappings in agent prompts:

| Intent | Exact Command |
|--------|--------------|
| Switch to Line 2 | workflow select-tab "Line 2" |
| Start monitoring | toolbar start |
| Get camera states | workflow camera-states --json |

# Strategy 3: Dry-run Mode

Add --dry-run flag that validates syntax without execution
```

**Detection:**

- Agent tries commands that fail with "command not found" errors
- Test logs show repeated trial-and-error command attempts
- Exit code 4 (invalid argument) appears frequently

**Phase to address:** Phase 28 (CLI Verification Layer)

---

### Pitfall 2: Orchestrator Bypassing Delegation Boundaries

**What goes wrong:**

Orchestrator agents "helpfully" execute commands directly instead of delegating to specialized sub-agents, violating the separation of concerns.

**Real example from our project:**

```markdown
# Orchestrator SHOULD delegate:
"test-executor, execute UI automation to start monitoring. Use: ui_automation.exe toolbar start"

# Orchestrator sometimes does directly:
Bash: ui_automation.exe toolbar start  # VIOLATES delegation pattern
```

**Why it happens:**

1. Orchestrator perceives direct execution as "faster"
2. LLM's helpfulness bias overrides architectural constraints
3. Prohibition language in prompts isn't strong enough

**Consequences:**

- Sub-agent expertise is bypassed
- Test-executor's error handling patterns aren't applied
- Logs aren't captured in expected format
- Reporting becomes inconsistent
- Architecture erodes over time

**Prevention:**

```markdown
# In orchestrator agent prompts:

## ABSOLUTELY FORBIDDEN

**Do NOT execute UI automation directly**
- Bash: ui_automation.exe toolbar start
- Bash: dotnet run --project skills_scripts/ui_automation/...

**What TO Do Instead (Delegate via Task Tool):**
- "test-executor, execute UI automation to start monitoring. Use: ui_automation.exe toolbar start"

# Add explicit examples with X/ checkmarks:

## Allowed Tool Usage
READ TOOLS (for planning):
- Read - For reading code files to understand implementation
- Grep - For searching codebase to map features
- Glob - For finding files

FORBIDDEN:
- Bash - For ANY execution
```

**Detection:**

- Orchestrator agent logs show Bash tool usage for execution tasks
- Test reports missing expected delegation metadata
- Inconsistent error handling formats

**Phase to address:** Phase 27 (Orchestrator Delegation Fix)

---

### Pitfall 3: Fallback to Direct Execution on Delegation Failure

**What goes wrong:**

When delegation to a sub-agent fails, the orchestrator "helpfully" falls back to direct execution as a recovery mechanism.

**Why it happens:**

1. Agent's desire to complete the task overrides architectural constraints
2. No explicit prohibition against fallback patterns
3. Error handling guidance emphasizes "try alternatives" rather than "report failure"

**Consequences:**

- Silent architectural violations
- Failures that should be visible become hidden
- Root cause analysis becomes difficult
- The entire point of having specialized sub-agents is undermined

**Prevention:**

```markdown
## If Delegation Fails

When delegation to a sub-agent fails:

1. NEVER fall back to direct execution - This is a critical failure mode
2. Retry once - Attempt delegation again with clearer instructions
3. Report failure - Document in "Delegation Issues" section of your report
4. Continue with other tasks - If partial execution is possible

## Delegation Issues (Add to Report Format)

test-executor: [success / failed / retried]
log-analyst: [success / failed / retried]

If delegation failed:
- Which sub-agent: [test-executor or log-analyst]
- Error: [what went wrong]
- Retry attempt: [result if retried]
- Impact: [how this affects test results]
```

**Detection:**

- Delegation failures are not visible in reports
- "Delegation Issues" section is always empty or missing
- Tests mysteriously complete despite known sub-agent failures

**Phase to address:** Phase 27 (Orchestrator Delegation Fix)

---

### Pitfall 4: Missing Intent-to-Command Schema

**What goes wrong:**

Agent prompts lack explicit mappings between user intents and exact CLI commands, leading to guesswork.

**Real example from our project:**

```markdown
# WITHOUT explicit schema, agent guesses:
User: "Switch to Line 2"
Agent: ui_automation workflow switch-line --line 2  # WRONG

# WITH explicit schema:
User: "Switch to Line 2"
Agent: (looks up in schema) workflow select-tab "Line 2"  # CORRECT
```

**Why it happens:**

1. CLI reference documentation is separate from agent prompts
2. Agents rely on general CLI patterns rather than specific tool knowledge
3. No canonical source of truth for intent-to-command mapping

**Consequences:**

- Repeated command hallucinations
- Inconsistent command usage across different agent invocations
- Difficulty maintaining documentation parity

**Prevention:**

```markdown
# Include explicit command reference in agent prompts:

## UI Automation CLI Command Reference

| Intent | Exact Command |
|--------|--------------|
| Start monitoring | toolbar start |
| Stop monitoring | toolbar stop |
| Switch to Line 1 | workflow select-tab "Line 1" |
| Switch to Line 2 | workflow select-tab "Line 2" |
| Get camera states | workflow camera-states --json |

## DO NOT Invent Commands

Only use commands from this reference table.
If an intent is not listed, report it as unsupported.
```

**Detection:**

- Agent attempts commands not in reference documentation
- Multiple variations of similar commands are attempted
- "Command not found" errors with plausible-sounding names

**Phase to address:** Phase 28 (CLI Verification Layer)

---

## Moderate Pitfalls

Mistakes that cause delays or technical debt but are recoverable.

### Pitfall 5: Inconsistent JSON Output Schemas

**What goes wrong:**

Different commands return JSON in different formats, requiring per-command parsing logic.

**Why it happens:**

- CLI commands were added incrementally without schema standardization
- JSON structure evolved organically
- No canonical schema documentation

**Consequences:**

- Agent code has special-case parsing for each command
- Schema changes break agent integration
- Difficult to build generic command wrappers

**Prevention:**

```markdown
# Standardize all JSON output to:

{
  "success": true | false,
  "data": { ... },      // On success
  "error": "...",       // On failure
  "errorCode": 0-4      // Exit code
}

# Document schema for each command in CLI reference
```

**Detection:**

- Agent code has multiple JSON parsing branches
- Tests fail with JSON parsing errors
- Inconsistent error checking patterns

**Phase to address:** Phase 29 (CLI Schema Standardization)

---

### Pitfall 6: No Dry-Run Capability

**What goes wrong:**

Agents must execute commands to discover if they're valid, causing side effects during validation.

**Why it happens:**

- CLI tool was designed for human use (who read docs first)
- No consideration for automated validation workflows
- Dry-run not included in initial requirements

**Consequences:**

- Testing requires real execution with real side effects
- Cannot safely validate command sequences
- Difficult to test automation without modifying system state

**Prevention:**

```bash
# Add --dry-run flag to all commands
ui_automation.exe --dry-run toolbar start
# Returns: Would execute: toolbar start
# Without actually clicking the button
```

**Detection:**

- Tests require full application state setup
- Cannot validate commands without side effects
- Test fixtures are complex due to state requirements

**Phase to address:** Phase 30 (Dry-Run Mode)

---

### Pitfall 7: Poor Error Message Disambiguation

**What goes wrong:**

Error messages don't clearly distinguish between different failure modes (not found vs. timeout vs. permission).

**Why it happens:**

- Generic error handling wraps specific failures
- Exit codes aren't consistently used
- Error messages designed for humans, not parsers

**Consequences:**

- Agents cannot reliably determine retry eligibility
- Error recovery logic is heuristic rather than deterministic
- Difficult to build robust automation

**Prevention:**

```json
// Standard error response:
{
  "success": false,
  "error": "MainWindow not found",
  "errorCode": 2,  // EXIT_NOT_FOUND
  "retryable": false,
  "suggestion": "Start ChronoView.exe first"
}
```

**Detection:**

- Agent error handling has string matching on error messages
- Same exit code maps to multiple failure types
- Retry logic fails or retries inappropriately

**Phase to address:** Phase 29 (CLI Schema Standardization)

---

## Minor Pitfalls

Mistakes that cause annoyance but are fixable.

### Pitfall 8: Discoverability Through Trial and Error

**What goes wrong:**

Agents discover available commands by trying them and seeing if they work.

**Prevention:**

```bash
# Add command discovery command
ui_automation.exe --list-commands
# Returns structured list of all available commands

# Or per-group
ui_automation.exe workflow --help
```

**Phase to address:** Phase 28 (CLI Verification Layer)

---

### Pitfall 9: Missing --json Flag Consistency

**What goes wrong:**

Some commands support `--json`, others don't, requiring agents to track which commands support it.

**Prevention:**

```bash
# All commands should support --json
# Document which commands output JSON by default
```

**Phase to address:** Phase 29 (CLI Schema Standardization)

---

## Phase-Specific Warnings

| Phase | Likely Pitfall | Mitigation |
|-------|----------------|------------|
| Phase 27: Orchestrator Delegation Fix | Orchestrator bypassing delegation | Add explicit Bash prohibition with examples |
| Phase 28: CLI Verification Layer | Command hallucination | Pre-execution validation, intent-to-command registry |
| Phase 29: CLI Schema Standardization | Inconsistent JSON outputs | Standardize success/error response format |
| Phase 30: Dry-Run Mode | No validation without side effects | Add --dry-run to all commands |
| Phase 31: Agent Integration | Fallback on delegation failure | Explicit failure reporting, no fallback |

---

## Cross-References

**Related Research:**

- **[Top 7 CLI Developer Experience Mistakes Devs Still Make in 2025](https://www.techbuddies.io/2026/01/09/top-7-cli-developer-experience-mistakes-devs-still-make-in-2025/)** - Covers discoverability and documentation gaps

- **[Security & Guardrails in AI Systems 2025](https://medium.com/@dewasheesh.rana/%EF%B8%8F-security-guardrails-in-ai-systems-2025-a-complete-engineering-guide-from-layman-to-professional-f9383336c8ab)** - Tool sandboxing and validation checkpoints

- **[AI Function Calling in 2025](https://www.kidsil.net/2025/07/ai-function-calling/)** - Schema definition and error handling patterns

- **[LLM-based Agents Suffer from Hallucinations Survey](https://arxiv.org/html/2509.18970v1)** - Techniques for detecting and mitigating hallucinations

- **[Tool Use in Agentic AI: 2025 Overview](https://samiranama.com/posts/Tool-Use-in-Agentic-AI-A-2025-Systems-Overview/)** - Verification layers and pre-call requirements

**Internal Project References:**

- `.planning/phases/27-orchestrator-delegation-fix/27-CONTEXT.md` - Our delegation boundary decisions
- `.planning/phases/27-orchestrator-delegation-fix/27-RESEARCH.md` - Delegation pattern research
- `.claude/agents/test-orchestrator.md` - Current orchestrator implementation
- `docs/cli-reference.md` - Complete CLI command documentation

---

## Sources

### Primary (HIGH confidence)
- `.planning/phases/27-orchestrator-delegation-fix/27-CONTEXT.md` - Project context on delegation issues
- `.planning/phases/27-orchestrator-delegation-fix/27-RESEARCH.md` - Detailed delegation pattern analysis
- `.claude/agents/test-orchestrator.md` - Current agent implementation with known issues
- `docs/cli-reference.md` - Complete CLI command reference
- `skills_scripts/ui_automation/README.md` - CLI tool documentation

### Secondary (MEDIUM confidence)
- [Top 7 CLI Developer Experience Mistakes 2025](https://www.techbuddies.io/2026/01/09/top-7-cli-developer-experience-mistakes-devs-still-make-in-2025/)
- [Tool Use in Agentic AI 2025 Overview](https://samiranama.com/posts/Tool-Use-in-Agentic-AI-A-2025-Systems-Overview/)
- [Security & Guardrails in AI Systems 2025](https://medium.com/@dewasheesh.rana/%EF%B8%8F-security-guardrails-in-ai-systems-2025-a-complete-engineering-guide-from-layman-to-professional-f9383336c8ab)
- [AI Function Calling in 2025](https://www.kidsil.net/2025/07/ai-function-calling/)
- [In-Depth Guide to Tool Parameter Validation 2025](https://sparkco.ai/blog/in-depth-guide-to-tool-parameter-validation-in-2025)

### Tertiary (LOW confidence)
- [LLM-based Agents Hallucinations Survey](https://arxiv.org/html/2509.18970v1)
- [How to Build AI Agent Guardrails 2025](https://www.superteams.ai/blog/newletter-july-2025-how-to-build-strong-guardrails-for-ai-agents)
- [Unified Tool Calling Architecture](https://www.scalekit.com/blog/unified-tool-calling-architecture-langchain-crewai-mcp)

---

**Research Date:** 2026-01-21
**Valid Until:** 90 days (agent coordination patterns evolve rapidly)
