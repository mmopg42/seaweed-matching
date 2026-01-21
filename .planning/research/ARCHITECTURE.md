# Architecture: Delegation Pattern in Test Automation

**Research Date:** 2026-01-21
**Domain:** Agent Coordination / "What vs How" Separation
**Confidence:** HIGH

## Executive Summary

This document defines the clean architecture for orchestrator to executor delegation in the ChronoView test automation system. The core principle is separation of concerns: the orchestrator defines "what" to test (intent), while the executor handles "how" to execute it (implementation details).

The current architecture (Phase 27 complete) has strong prohibitions against direct execution in the orchestrator, but the skill definition and translation layer needs formalization. This research defines how skills should be structured to prevent command guessing while maintaining flexibility.

## Current Architecture

```
+-------------------+         +------------------+         +------------------+
| test-orchestrator | ------> |  test-executor   | ------> | ui_automation.exe |
|                   | Task    |                  | Bash    |                  |
+-------------------+         +------------------+         +------------------+
        |                                                              |
        | Task                                                         |
        v                                                              v
+-------------------+         +------------------+         +------------------+
|   log-analyst     |         |  data_simulator  | <-- lots -->  Test Data   |
|                   |         |                  |         +------------------+
+-------------------+         +------------------+
```

**Agent Responsibilities:**

| Agent | Responsibility | Tool Access | What It Knows |
|-------|---------------|-------------|---------------|
| test-orchestrator | Test planning, result synthesis | Task, Read, Grep (NO Bash) | Test objectives, feature mappings, TIER priorities |
| test-executor | Build, run, UI automation, data generation | Bash, Read, Grep | CLI commands, execution patterns, retry logic |
| log-analyst | Log parsing, pattern analysis | Read, Grep | Log formats, service categories, workflow traces |

## The Delegation Interface

### Current State (Phase 27)

The orchestrator delegates via natural language prompts to the Task tool:

```markdown
"test-executor, build the solution and launch the application.
Use: dotnet build ChronoView/ChronoView.csproj then ui_automation.exe app launch"
```

**Problems identified:**
1. Orchestrator must know exact CLI syntax (violates "what not how")
2. Orchestrator can "guess" wrong commands
3. No verification that requested operation maps to valid command
4. Command knowledge duplicated across orchestrator and executor

### Recommended State: Skill-Based Delegation

```markdown
"test-executor, execute the BUILD skill followed by the APP_LAUNCH skill"
```

The test-executor translates skill names to actual CLI commands.

## Skill Definition Structure

### Skill Schema

```typescript
interface Skill {
  name: string;           // e.g., "BUILD", "APP_LAUNCH"
  category: SkillCategory;
  description: string;     // What this skill does (orchestrator sees this)
  command: string;         // Actual CLI (executor only sees this)
  parameters?: Parameter[];
  preconditions?: string[]; // Requirements before execution
  timeout?: number;        // Max execution time (ms)
  retryable: boolean;      // Whether to retry on failure
}

interface Parameter {
  name: string;
  type: 'string' | 'number' | 'boolean' | 'enum';
  required: boolean;
  description: string;
  enum?: string[];         // For enum types
}

enum SkillCategory {
  LIFECYCLE = 'lifecycle',    // app launch, stop, restart
  WORKFLOW = 'workflow',      // camera launches, monitoring
  SETTINGS = 'settings',      // config operations
  WINDOW = 'window',          // window management
  DATA = 'data',              // test data generation
  DIAGNOSTIC = 'diagnostic'   // connectivity, status checks
}
```

### Example Skill Definitions

```yaml
# test-executor skill registry
skills:
  BUILD:
    category: lifecycle
    description: Build the ChronoView solution
    command: "dotnet build ChronoView/ChronoView.csproj"
    retryable: true
    timeout: 120000

  APP_LAUNCH:
    category: lifecycle
    description: Launch ChronoView application (non-blocking)
    command: "ui_automation.exe app launch"
    retryable: true
    timeout: 10000

  APP_STOP:
    category: lifecycle
    description: Stop all ChronoView processes
    command: "ui_automation.exe app stop"
    retryable: false
    timeout: 15000

  TOOLBAR_START:
    category: workflow
    description: Start file monitoring
    command: "ui_automation.exe toolbar start"
    retryable: true
    timeout: 5000

  TOOLBAR_STOP:
    category: workflow
    description: Stop file monitoring
    command: "ui_automation.exe toolbar stop"
    retryable: false
    timeout: 5000

  DATA_GENERATE_DUMMY:
    category: data
    description: Generate dummy test data (black images)
    command: "python task_helper/data_test/data_simulator.py --cli --read-config --mode dummy --line {line}"
    parameters:
      - name: line
        type: enum
        enum: [line1, line2]
        required: true
        description: Target camera line
    retryable: true
    timeout: 300000

  CONNECTIVITY_CHECK:
    category: diagnostic
    description: Verify UI automation can connect to ChronoView
    command: "ui_automation.exe test connectivity"
    retryable: true
    timeout: 10000
```

## Delegation Protocol

### From Orchestrator to Executor

```markdown
## Task Delegation Format

**To:** test-executor
**TIER:** [1/2/3]
**Focus Feature:** [feature name]

### Skills to Execute

1. **BUILD** - Build the solution
2. **APP_LAUNCH** - Launch ChronoView
3. **TOOLBAR_START** - Start monitoring

### Parameters
- None (or specific skill parameters)

### Expected Outcome
- Application running
- Monitoring active
- Logs being written to %APPDATA%\ChronoView\Logs\

### Evidence to Capture
- Log location (path only, not content)
- Application status
- Any errors observed

Report with TIER-organized execution summary.
```

### Executor Skill Translation

```typescript
// Internal to test-executor (orchestrator never sees this)
function executeSkillRequest(request: SkillRequest): ExecutionResult {
  const results: SkillResult[] = [];

  for (const skillName of request.skills) {
    const skill = skillRegistry.get(skillName);

    if (!skill) {
      throw new Error(`Unknown skill: ${skillName}. Valid skills: ${[...skillRegistry.keys()].join(', ')}`);
    }

    // Check preconditions
    if (skill.preconditions) {
      for (const pre of skill.preconditions) {
        if (!checkPrecondition(pre)) {
          throw new Error(`Precondition failed for ${skillName}: ${pre}`);
        }
      }
    }

    // Execute command with retry logic
    const result = executeWithRetry(skill.command, skill.retryable, skill.timeout);
    results.push({ skill: skillName, ...result });

    if (!result.success && !skill.retryable) {
      break; // Stop sequence on non-retryable failure
    }
  }

  return {
    tier: request.tier,
    focusFeature: request.focusFeature,
    results,
    logLocation: captureLogLocation()
  };
}
```

## Orchestrator Constraints (Reinforced)

The following must remain in test-orchestrator.md:

1. **ABSOLUTE PROHIBITION on Bash for execution** (already in Phase 27)
2. **Prohibition on command construction** - NEW
3. **Skill-only delegation** - NEW
4. **Delegation failure reporting** (already in Phase 27)

### New Prohibition: No Command Construction

```markdown
## Do NOT Construct CLI Commands

**FORBIDDEN:**
- ❌ Specifying full CLI commands in delegation
- ❌ Combining command fragments
- ❌ Guessing command syntax

**ALLOWED:**
- ✅ Referencing skills by name
- ✅ Passing skill parameters defined in skill schema
- ✅ Specifying execution order (sequence of skills)

**Example:**
❌ "Execute: ui_automation.exe toolbar start"
✅ "Execute the TOOLBAR_START skill"
```

## Component Boundaries

### test-orchestrator owns:
| Concept | Definition |
|---------|------------|
| TIER | Test priority framework (T1=focus, T2=related, T3=smoke) |
| Focus Feature | The primary feature under test |
| Component Mapping | Which services implement a feature |
| Test Plan | Which skills to execute in what order |
| Success Criteria | Expected outcomes |

### test-executor owns:
| Concept | Definition |
|---------|------------|
| Skill Registry | Mapping of skill names to CLI commands |
| Execution Engine | Running Bash commands with timeouts |
| Retry Logic | When and how to retry failed commands |
| Evidence Capture | Collecting log locations, timestamps |
| Error Handling | Translating exit codes to meaningful messages |

### log-analyst owns:
| Concept | Definition |
|---------|------------|
| Log Discovery | Finding log files (using --latest flag) |
| Pattern Recognition | Identifying errors, warnings, anomalies |
| Service Categories | Which log categories belong to which features |
| Root Cause Analysis | Tracing issues through the workflow |

## Build Order

### Phase 1: Skill Definition (New Component)
**File:** `.claude/agents/test-executor-skills.md`

1. Create comprehensive skill registry
2. Document all existing CLI commands as skills
3. Define parameter schemas
4. Add to test-executor prompt as reference section

**Deliverable:** skill registry with 30+ skills covering all existing CLI commands

### Phase 2: Orchestrator Update
**File:** `.claude/agents/test-orchestrator.md`

1. Add "Skills Reference" section (skill names and descriptions only, no commands)
2. Update delegation template to use skill names
3. Add "No Command Construction" prohibition
4. Update examples to show skill-based delegation

**Deliverable:** Orchestrator that references skills, not commands

### Phase 3: Executor Translation Logic
**File:** `.claude/agents/test-executor.md`

1. Add "Skill Execution" section explaining how to translate skill → CLI
2. Update execution patterns to show skill-first approach
3. Add error handling for unknown skills

**Deliverable:** Executor that validates and translates skills

### Phase 4: Verification
1. Update Phase 27 verification patterns
2. Test actual delegation with skill names
3. Verify orchestrator cannot construct commands

## Integration Points

| Point | From | To | Interface | Data |
|-------|------|----|-----------|------|
| Skill Request | test-orchestrator | test-executor | Task tool with skill names | TIER, skills[], parameters |
| Execution Result | test-executor | test-orchestrator | Task return | results[], logLocation, status |
| Log Location | test-executor | log-analyst | Via orchestrator | Path string |
| Analysis | log-analyst | test-orchestrator | Task return | findings[], issues[] |

## Anti-Patterns to Avoid

### Anti-Pattern 1: Orchestrator Guesses Commands

```typescript
// WRONG - Orchestrator constructs command
"Bash: ui_automation.exe toolbar start"

// RIGHT - Orchestrator references skill
"Execute TOOLBAR_START skill"
```

### Anti-Pattern 2: Skills Without Validation

```typescript
// WRONG - Executor executes arbitrary strings
executeSkill(command: string) {
  bash(command); // No validation!
}

// RIGHT - Executor validates against registry
executeSkill(skillName: string) {
  const skill = skillRegistry.get(skillName);
  if (!skill) throw new Error(`Unknown skill: ${skillName}`);
  bash(skill.command);
}
```

### Anti-Pattern 3: Skills Duplicate Knowledge

```typescript
// WRONG - Command syntax duplicated in orchestrator
// test-orchestrator.md:
"Execute: ui_automation.exe toolbar start"

// test-executor.md:
"Command: ui_automation.exe toolbar start"

// RIGHT - Only executor knows CLI
// test-orchestrator.md:
"Execute TOOLBAR_START"
// (no command shown)

// test-executor.md:
"TOOLBAR_START: ui_automation.exe toolbar start"
```

## Migration Path

### Step 1: Create Skill Registry (Non-Breaking)
- Add skills alongside existing command reference in test-executor
- Both approaches work during transition

### Step 2: Update Orchestrator Examples
- Change delegation examples to use skills
- Keep command reference table for "information only"

### Step 3: Strengthen Prohibitions
- Add explicit "no command construction" rule
- Add verification patterns

### Step 4: Remove Command Knowledge from Orchestrator
- Delete exact command strings from orchestrator
- Keep only skill names and descriptions

## Open Questions

1. **Skill versioning:** When CLI commands change, how do we version skills?
   - Recommendation: Skills are tied to ui_automation.csproj version

2. **Dynamic skill discovery:** Should executor expose skill list?
   - Recommendation: No, keep skills static in executor prompt

3. **Skill composition:** Can skills contain sub-skills?
   - Recommendation: Yes, but define explicitly (e.g., "FULL_SETUP" = ["VERIFY_CONFIG", "COMPLETE_FULL"])

4. **Error recovery:** Should executor suggest alternative skills on failure?
   - Recommendation: Yes, include "suggestedSkills" in error response

## Recommended Next Steps

1. **Create test-executor-skills.md** with complete skill registry
2. **Update test-orchestrator.md** with skill-based delegation examples
3. **Add verification patterns** to enforce "no command construction"
4. **Run test execution** to validate skill translation works

## Confidence Assessment

| Area | Confidence | Reason |
|------|------------|--------|
| Current architecture | HIGH | Based on actual agent files |
| Delegation pattern | HIGH | Phase 27 already implemented core pattern |
| Skill schema | HIGH | Standard TypeScript interface pattern |
| Build order | HIGH | Logical dependency ordering |
| Migration path | MEDIUM | Requires testing in real scenarios |

## Sources

### Primary (HIGH confidence)
- `.claude/agents/test-orchestrator.md` - Orchestrator definition with delegation rules
- `.claude/agents/test-executor.md` - Executor definition with CLI commands
- `.claude/agents/log-analyst.md` - Log analyst definition
- `.planning/phases/27-orchestrator-delegation-fix/27-RESEARCH.md` - Delegation pattern research

### Secondary (MEDIUM confidence)
- `.planning/phases/27-orchestrator-delegation-fix/27-CONTEXT.md` - Implementation decisions
- `.planning/phases/27-orchestrator-delegation-fix/27-01-SUMMARY.md` - Implementation summary
- `.planning/phases/27-orchestrator-delegation-fix/27-VERIFICATION.md` - Verification report

### Tertiary (LOW confidence)
- None - all sources are internal project documentation

---

*Research complete: 2026-01-21*
*Valid for: 90 days (agent coordination patterns are stable)*
