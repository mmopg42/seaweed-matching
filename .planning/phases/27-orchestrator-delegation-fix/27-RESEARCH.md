# Phase 27: Orchestrator Delegation Fix - Research

**Researched:** 2026-01-21
**Domain:** Agent Coordination Pattern / Task Tool Delegation
**Confidence:** HIGH

## Summary

This phase addresses a code fix in the test-orchestrator agent to enforce strict delegation patterns. The current documentation correctly states "You are NOT an executor" but the guidance lacks explicit prohibition examples. The issue is NOT about adding new functionality—it's about strengthening existing constraints to prevent the orchestrator from bypassing its sub-agents.

The standard pattern for delegation in this codebase is established in `validate-plan.md` (lines 109-116), which shows the exact Task tool syntax with `subagent_type` parameter. The test-orchestrator currently delegates to test-executor and log-analyst but needs stronger explicit prohibitions against direct Bash tool usage for execution tasks.

**Primary recommendation:** Add explicit "NEVER use Bash for execution" section to test-orchestrator.md with concrete examples of what NOT to do, plus add "Delegation Issues" section to the reporting format for capturing delegation failures.

## Standard Stack

### Core
| Component | Version | Purpose | Why Standard |
|-----------|---------|---------|--------------|
| Task tool | Built-in | Agent delegation | Official Claude Code mechanism for sub-agent invocation |
| Bash tool | Built-in | Execution (for sub-agents only) | Must NOT be used by orchestrator for execution |
| Read/Grep | Built-in | Inspection (allowed for orchestrator) | Orchestrator can inspect files but not execute |

### Agent Architecture
| Agent | Purpose | Tool Access |
|-------|---------|-------------|
| test-orchestrator | Scenario definition + result synthesis | Task, Read, Grep (NOT Bash for execution) |
| test-executor | Build, run, UI automation, data generation | Bash, Read, Grep |
| log-analyst | Log parsing, pattern analysis, diagnosis | Read, Grep (for logs only) |

**No external libraries needed** - this is pure agent prompt engineering.

## Architecture Patterns

### Delegation Template Format

**Current test-orchestrator pattern (lines 38-41):**
```
"Execute: cd C:\workspace\seaweed\gui_kiro_v2 && dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- toolbar start"
```

**Standard validate-plan pattern (lines 109-116):**
```xml
<parameter name="subagent_type">validate-reality</parameter>
<parameter name="prompt">[Plan content + validation request]</parameter>
```

### Recommended Task Tool Delegation Pattern

```markdown
## Delegation Template

### To test-executor:
```
I need you to execute the following test scenario:

**TIER 1: [Feature Name] - Focused Feature**
- Test Case 1: [Description]
- Test Case 2: [Description]

**Execution Requirements:**
- Build the solution if needed
- Launch ChronoView application
- Execute UI automation commands for [specific operations]
- Capture log location (not log content)

**Expected Outcome:**
- [What should happen]

Report results with TIER-organized execution summary.
```

### To log-analyst:
```
I need you to analyze logs from the test session:

**Log Location:** [Path from test-executor]
**Time Range:** [Start] to [End]

**Focus Feature:** [Feature name]

**What to Look For:**
- Errors related to [specific service/component]
- Warnings about [specific behavior]
- Workflow sequence verification

**TIER Priority:** Focus 70% on TIER 1 (focus feature), 20% on TIER 2, 10% on TIER 3

Report with TIER-organized analysis summary.
```
```

### Forbidden Patterns (What to Add)

```markdown
## NEVER Use Bash Tool for Execution

**ABSOLUTELY FORBIDDEN:**

### What NOT to do:

**Do NOT build directly:**
❌ "Let me build the solution first"
❌ Bash: dotnet build ChronoView/ChronoView.csproj

**Do NOT run application directly:**
❌ "I'll launch the app now"
❌ Bash: ui_automation.exe app launch

**Do NOT execute UI automation directly:**
❌ "Let me click the start button"
❌ Bash: ui_automation.exe toolbar start

**Do NOT generate test data directly:**
❌ "I'll create some test files"
❌ Bash: python task_helper/data_test/data_simulator.py ...

**Do NOT analyze logs directly:**
❌ "Let me check the logs for errors"
❌ Grep/Read: Analyzing log patterns

### What TO do instead:

**For execution tasks:**
✅ "test-executor, build the solution and launch the application"
✅ Use Task tool with subagent_type: test-executor

**For log analysis:**
✅ "log-analyst, analyze the logs at [path]"
✅ Use Task tool with subagent_type: log-analyst

### Allowed Tool Usage (test-orchestrator ONLY):

**READ TOOLS (for scenario planning):**
- ✅ Read - To understand codebase structure
- ✅ Grep - To find relevant components
- ✅ Glob - To locate files

**FORBIDDEN:**
- ❌ Bash - For ANY execution purpose
- ❌ Bash - For ANY log analysis
- ❌ Bash - For ANY test data generation

### If Delegation Fails:

**NEVER fall back to direct execution.** Report the failure:

```
### Delegation Issues
- test-executor delegation failed: [error details]
- Retried once: [result]
- Unable to proceed without sub-agent
```
```

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Agent coordination | Custom Task tool patterns | Existing validate-plan delegation pattern | Proven subagent_type syntax |
| Delegation failure handling | Retry with fallback Bash | Explicit failure reporting only | Fallback defeats the purpose |
| Command specification | Full CLI construction in orchestrator | Operation + parameters template | test-executor knows the CLI |

## Common Pitfalls

### Pitfall 1: "I'll just do it quickly" Direct Execution
**What goes wrong:** Orchestrator bypasses delegation for "efficiency"
**Why it happens:** Urgency or perceived simplicity
**How to avoid:** Explicit prohibition section with examples
**Warning signs:** Agent says "Let me..." before using Bash tool

### Pitfall 2: Ambiguous Delegation Requests
**What goes wrong:** Sub-agent doesn't understand what to execute
**Why it happens:** Orchestrator provides insufficient context
**How to avoid:** Use structured templates with TIER organization
**Warning signs:** Sub-agent asks clarifying questions

### Pitfall 3: Fallback on Delegation Failure
**What goes wrong:** Orchestrator "helpfully" falls back to direct execution
**Why it happens:** Misplaced desire to complete the task
**How to avoid:** Explicit "NEVER fallback" rule with failure reporting format
**Warning signs:** "Since delegation failed, I'll try directly..."

### Pitfall 4: Missing Delegation Issues in Report
**What goes wrong:** User doesn't know delegation failed
**Why it happens:** No dedicated section for reporting delegation problems
**How to avoid:** Add "Delegation Issues" section to reporting format
**Warning signs:** Report shows test results but delegation problems are hidden

## Code Examples

### Current test-orchestrator Delegation Pattern (lines 135-148)
```markdown
## Delegation Pattern

When you need to execute tests, ALWAYS use the Task tool:

```
For execution:
"I need you to execute the following test scenario: [specific scenario].
Build the solution, run the application, and verify [expected behavior].
Capture logs for analysis."

For analysis:
"Analyze the logs from [location]. Look for [specific patterns/errors].
Identify any issues and provide root cause analysis."
```
```

**Issue:** This pattern uses natural language delegation without explicit `subagent_type` parameter. The Task tool invocation isn't shown explicitly.

### validate-plan Delegation Pattern (lines 109-116) - Reference Standard
```xml
Use the Task tool to invoke each sub-agent:

<parameter name="subagent_type">validate-reality</parameter>
<parameter name="prompt">[Plan content + validation request]</parameter>
```

**Note:** This is the standard pattern. However, for test-orchestrator, the delegation happens through natural language (the orchestrator describes what it needs and the system routes to the appropriate sub-agent based on the task description).

### Enhanced Reporting Format (Add Delegation Issues Section)

```markdown
### Issues Found (All Tiers)

### Delegation Issues (NEW)
- test-executor: [status - success/failed/retried]
- log-analyst: [status - success/failed/retried]

**If delegation failed:**
- Which sub-agent: [test-executor or log-analyst]
- Error: [what went wrong]
- Retry attempt: [result if retried]
- Impact: [how this affects the test results]

**TIER 1 Issues (Focus Feature):**
[... rest of existing format ...]
```

## Verification Approach

### Code Review Verification (Plan 27-01)
1. Search test-orchestrator.md for explicit Bash prohibition
2. Verify "NEVER use Bash for execution" section exists
3. Verify "Delegation Issues" section added to reporting format
4. Verify examples show both forbidden AND allowed patterns

### Test Execution Verification (Plan 27-02)
**Verification Strategy:**

```markdown
## Delegation Pattern Verification Test

**Test Scenario:** Complex feature with multiple TIERs (e.g., File Monitoring)

**Verification Steps:**

1. **Code Review Check:**
   - Grep test-orchestrator.md for "NEVER use Bash"
   - Verify explicit prohibition section exists
   - Verify "Delegation Issues" in report format

2. **Execution Check:**
   - Invoke test-orchestrator for file monitoring test
   - Monitor for any direct Bash usage
   - Verify all execution goes through test-executor
   - Verify all log analysis goes through log-analyst

3. **Failure Handling Check:**
   - Simulate delegation failure (if possible)
   - Verify orchestrator reports failure
   - Verify NO fallback to direct execution

**Success Criteria:**
- No direct Bash tool usage for execution
- All test execution delegated to test-executor
- All log analysis delegated to log-analyst
- Delegation failures reported explicitly
- "Delegation Issues" section in final report when applicable
```

### Grep Patterns for Verification

```bash
# Check for explicit Bash prohibition
grep -n "NEVER.*Bash\|ABSOLUTELY FORBIDDEN" .claude/agents/test-orchestrator.md

# Check for Delegation Issues section
grep -n "Delegation Issues" .claude/agents/test-orchestrator.md

# Check for proper subagent_type pattern (if using Task tool explicitly)
grep -n "subagent_type" .claude/agents/test-orchestrator.md

# Check for DO NOT delegate examples
grep -n "Do NOT.*build\|Do NOT.*run\|Do NOT.*execute" .claude/agents/test-orchestrator.md
```

## State of the Art

| Old Pattern | New Pattern | When Changed | Impact |
|-------------|-------------|--------------|--------|
| Direct execution in orchestrator | Strict delegation to sub-agents | Phase 27 | Clear separation of concerns |
| No explicit prohibitions | "NEVER use Bash" examples | Phase 27 | Prevents bypass attempts |
| No delegation failure reporting | "Delegation Issues" section | Phase 27 | Transparency when sub-agents fail |

**Existing patterns to maintain:**
- TIER-based testing (already implemented correctly)
- Focus feature identification (already working)
- Component mapping (already documented)
- Structured reporting (needs Delegation Issues addition)

## Open Questions

None - the requirements are clear and the existing documentation provides sufficient context. The changes are well-defined:

1. Add explicit Bash prohibition section
2. Add "Delegation Issues" to reporting format
3. Verify through code review and test execution

## Sources

### Primary (HIGH confidence)
- `.claude/agents/test-orchestrator.md` - Current agent definition (lines 8-10 show delegation intent, lines 330-332 show DO NOT rules but need strengthening)
- `.claude/agents/test-executor.md` - Sub-agent responsibilities (lines 8-14 define execution role)
- `.claude/agents/log-analyst.md` - Sub-agent responsibilities (lines 8-12 define analysis role)
- `.claude/agents/validate-plan.md` - Reference delegation pattern (lines 109-116 show standard Task tool pattern)

### Secondary (MEDIUM confidence)
- `.planning/phases/27-orchestrator-delegation-fix/27-CONTEXT.md` - User decisions and phase boundary

### Tertiary (LOW confidence)
- None - all sources are internal project documentation

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Based on actual agent files in the codebase
- Architecture: HIGH - Direct observation of existing delegation patterns
- Pitfalls: HIGH - Based on analysis of current documentation gaps

**Research date:** 2026-01-21
**Valid until:** 90 days (agent coordination patterns are stable)

---

*Phase: 27-orchestrator-delegation-fix*
*Research complete: 2026-01-21*
