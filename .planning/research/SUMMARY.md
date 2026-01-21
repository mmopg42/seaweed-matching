# Research Summary: CLI Skill Encapsulation for Test Automation

**Project:** ChronoView CLI Skill Infrastructure
**Research Date:** 2026-01-21
**Synthesizer:** Research Synthesis Agent
**Overall Confidence:** HIGH

---

## Executive Summary

ChronoView requires a **CLI skill encapsulation layer** to enable reliable AI agent automation. The existing `ui_automation.exe` CLI tool provides 30+ commands across 11 handler categories, but the current architecture requires orchestrator agents to know exact command syntax. This leads to **command hallucination** failures where agents generate syntactically plausible but non-existent commands.

The recommended solution is a **semantic skill abstraction layer** that maps intents (e.g., "switch-to-line2", "start-monitoring") to exact CLI commands. Orchestrators delegate using skill names only; the executor agent translates skills to CLI syntax and handles execution, retries, and error reporting. This creates a clean separation: orchestrators define **what** to test (intent), while executors handle **how** to execute it (implementation).

The technology stack requires **no new dependencies**. Skills are defined as native Claude Code `SKILL.md` files with optional bundled `references/` and `scripts/` directories. The skill registry is maintained in the executor agent prompt, ensuring a single source of truth for command syntax.

**Key risks:** (1) Orchestrators bypassing delegation boundaries to execute commands directly; (2) Command hallucination without pre-execution verification; (3) Inconsistent JSON output schemas across commands. Mitigation involves strengthening prohibitions in agent prompts, adding pre-execution validation, and standardizing CLI response formats.

---

## Key Findings

### From STACK.md

**Core Technologies:**

| Technology | Purpose | Rationale |
|------------|---------|-----------|
| **SKILL.md** | Skill definition format | Native Claude Code format - auto-discovered from `.claude/skills/`, no parser needed |
| **scripts/** | Command execution wrapper | Python 3.x for complex workflows |
| **references/** | Progressive disclosure docs | Markdown docs loaded only when skill triggers |

**Key insight:** No JSON schemas, no YAML files, no custom parser. The skill definition IS the documentation.

**CLI Integration Points (existing, ready to use):**
- C# CLI Entry Point: `skills_scripts/ui_automation/Program.cs` with System.CommandLine
- 12 handler classes with 258 total commands
- Exit codes: SUCCESS=0, ERROR=1, NOT_FOUND=2, TIMEOUT=3, INVALID_ARGUMENT=4
- All commands support `--json` for structured output

---

### From FEATURES.md

**Table Stakes (must-have for MVP):**

| Feature | Complexity | Description |
|---------|------------|-------------|
| Skill-to-CLI Command Mapping | Low | Dictionary-based lookup: intent -> CLI command |
| Parameter Substitution | Low | Template-based: `{row_index}` -> actual value |
| Exit Code Translation | Low | Map CLI exit codes to skill result enums |
| JSON Output Parsing | Low | Parse `--json` output into result objects |
| Skill Registry/Discovery | Low | `ListSkills()` returning skill metadata |
| Validation | Med | Parameter validation against skill schema |
| Error Messages | Low | Actionable failure descriptions |
| Dry-Run Mode | Med | Return CLI command without executing |

**Differentiators (value-add features):**

| Feature | Value | Complexity |
|---------|--------|------------|
| Intent-Based Naming | Self-documenting, reduces hallucination | Low |
| Skill Composition | Combine multiple commands into single skill | Med |
| State Validation | Verify preconditions before execution | Med |
| Conditional Execution | Branch based on application state | Med |
| Fallback Chains | Try primary, fall back to alternative | High |
| Local-Only Execution | Privacy-preserving, offline-capable | Low |

**Anti-Features (explicitly NOT building):**

| Anti-Feature | Why Avoid | Alternative |
|--------------|-----------|-------------|
| Live CLI Command Discovery | Parsing `--help` is fragile | Static skill registry |
| Natural Language Parsing | LLMs already handle this | Let orchestrator choose skills |
| Remote Skill Execution | Adds latency, security risk | Keep skills local |
| Dynamic Skill Generation | Unpredictable, hard to debug | Define skills explicitly |
| Stateful Skills | Hard to reason about | Keep stateless, explicit parameters |
| Auto-Retry | CLI failures may need human intervention | Let orchestrator decide |

**CLI Command Categories (for skill mapping):**

| Category | Commands | Skill Examples |
|----------|----------|----------------|
| App Lifecycle | `app launch`, `app stop`, `app restart`, `app status` | `ensure-running`, `restart-app` |
| Windows | `windows main`, `windows setup`, `windows settings`, `windows preview` | `find-main-window`, `open-settings-dialog` |
| Toolbar | `toolbar start`, `toolbar stop`, `toolbar settings`, `toolbar move` | `start-monitoring`, `stop-monitoring` |
| Workflow | `workflow select-tab`, `workflow path get-line2`, `workflow launch-nir2` | `switch-to-line2`, `launch-nir2-camera` |
| Data Panel | `stats`, `datagrid rows`, `datagrid data` | `get-statistics`, `get-row-count` |
| File Operations | `file-ops select`, `file-ops move`, `file-ops delete` | `select-by-groupid`, `move-selected` |
| Settings | `settings-dialog path get-line2`, `settings-dialog checkbox set` | `get-line2-path`, `set-checkbox-state` |
| Logs | `logs get`, `logs tail`, `logs search` | `get-recent-logs`, `search-logs` |
| Setup | `setup verify-config`, `setup camera-states` | `verify-setup-config`, `get-camera-states` |
| Test | `test connectivity`, `test capabilities` | `check-connectivity`, `list-capabilities` |
| Scenario | `scenario start-monitoring`, `scenario configure-paths` | `start-full-monitoring`, `configure-all-paths` |
| Batch | `batch select-and-move`, `batch export-all` | `bulk-move`, `export-all-data` |

---

### From ARCHITECTURE.md

**Current Architecture (Phase 27 complete):**

```
test-orchestrator (Task tool only, NO Bash)
    |
    v
test-executor (Bash, Read, Grep)
    |
    v
ui_automation.exe
```

**Agent Responsibilities:**

| Agent | Responsibility | Tool Access |
|-------|---------------|-------------|
| test-orchestrator | Test planning, result synthesis, "what" to test | Task, Read, Grep (NO Bash) |
| test-executor | Build, run, UI automation, "how" to execute | Bash, Read, Grep |
| log-analyst | Log parsing, pattern analysis | Read, Grep |

**Skill Schema:**

```typescript
interface Skill {
  name: string;           // e.g., "TOOLBAR_START"
  category: SkillCategory;
  description: string;     // What this skill does (orchestrator sees this)
  command: string;         // Actual CLI (executor only sees this)
  parameters?: Parameter[];
  preconditions?: string[];
  timeout?: number;
  retryable: boolean;
}

enum SkillCategory {
  LIFECYCLE,    // app launch, stop, restart
  WORKFLOW,     // camera launches, monitoring
  SETTINGS,     // config operations
  WINDOW,       // window management
  DATA,         // test data generation
  DIAGNOSTIC    // connectivity, status checks
}
```

**Component Boundaries:**

| test-orchestrator owns: | test-executor owns: |
|------------------------|---------------------|
| TIER test priorities | Skill Registry (skill -> CLI mapping) |
| Focus Feature selection | Execution Engine |
| Component mapping | Retry Logic |
| Test Plan (skills in order) | Evidence Capture |
| Success Criteria | Error Handling |

**Build Order:**

1. **Phase 1:** Skill Definition - Create `.claude/agents/test-executor-skills.md` with 30+ skills
2. **Phase 2:** Orchestrator Update - Update to use skill names only (no commands)
3. **Phase 3:** Executor Translation Logic - Add skill-to-CLI translation
4. **Phase 4:** Verification - Test delegation with skill names

---

### From PITFALLS.md

**Critical Pitfalls (cause automation failures):**

| # | Pitfall | Prevention Strategy |
|---|---------|---------------------|
| 1 | **Command Hallucination** - Agents generate plausible but non-existent commands | Pre-execution verification layer; intent-to-command registry; dry-run mode |
| 2 | **Orchestrator Bypassing Delegation** - Direct execution instead of delegating | ABSOLUTE PROHIBITION on Bash in orchestrator; explicit allowed/forbidden tool lists |
| 3 | **Fallback to Direct Execution** - On delegation failure, orchestrator executes directly | NEVER fall back; report failure; retry delegation only |
| 4 | **Missing Intent-to-Command Schema** - No canonical mapping source | Include explicit command reference in agent prompts |

**Moderate Pitfalls (cause technical debt):**

| # | Pitfall | Prevention |
|---|---------|------------|
| 5 | Inconsistent JSON output schemas | Standardize `{success, data/error, errorCode}` format |
| 6 | No dry-run capability | Add `--dry-run` flag to all commands |
| 7 | Poor error message disambiguation | Include `retryable`, `suggestion` fields in error responses |

**Real Example from Project:**

```bash
# Agent hallucinated:
ui_automation workflow switch-line --line 2 --json  # WRONG

# Actual correct command:
ui_automation workflow select-tab "Line 2"  # CORRECT
```

**Phase-Specific Warnings:**

| Phase | Likely Pitfall | Mitigation |
|-------|----------------|------------|
| Phase 27: Orchestrator Delegation Fix | Orchestrator bypassing delegation | Add explicit Bash prohibition with X/checkmark examples |
| Phase 28: CLI Verification Layer | Command hallucination | Pre-execution validation, intent-to-command registry |
| Phase 29: CLI Schema Standardization | Inconsistent JSON outputs | Standardize success/error response format |
| Phase 30: Dry-Run Mode | No validation without side effects | Add `--dry-run` to all commands |

---

## Implications for Roadmap

### Recommended Phase Structure

Based on dependencies and complexity, the roadmap should follow this order:

#### Phase 1: Skill Registry Definition
**Rationale:** Foundation for all other work. Cannot delegate using skills until skills are defined.

**Delivers:**
- `.claude/agents/test-executor-skills.md` with 30+ skill definitions
- Intent-to-command mappings for all existing CLI commands
- Parameter schemas for skills requiring dynamic values

**Features from FEATURES.md:**
- Skill-to-CLI Command Mapping
- Parameter Substitution
- Exit Code Translation
- Skill Registry

**Pitfalls to avoid:**
- Pitfall 4: Missing Intent-to-Command Schema (include explicit mappings)

**Research needed:** No - this is well-defined based on existing CLI analysis

---

#### Phase 2: Orchestrator Skill Integration
**Rationale:** Orchestrator must reference skills before executor can translate them. Update delegation patterns first.

**Delivers:**
- Updated `.claude/agents/test-orchestrator.md` with skill-based delegation
- "Skills Reference" section (names and descriptions only, no commands)
- "No Command Construction" prohibition with examples
- Updated delegation template using skill names

**Features from FEATURES.md:**
- Skill Registry/Discovery (orchestrator-side)
- Intent-Based Naming

**Pitfalls to avoid:**
- Pitfall 2: Orchestrator Bypassing Delegation (add Bash prohibition)
- Pitfall 3: Fallback to Direct Execution (add failure reporting requirement)

**Research needed:** No - Phase 27 already established delegation patterns

---

#### Phase 3: Executor Skill Translation
**Rationale:** Executor needs translation logic to convert skill names to CLI commands. Depends on Phase 1 (skills defined) and Phase 2 (orchestrator using skills).

**Delivers:**
- Updated `.claude/agents/test-executor.md` with skill execution section
- Skill-to-CLI translation patterns
- Error handling for unknown skills
- Retry logic based on skill `retryable` flag

**Features from FEATURES.md:**
- JSON Output Parsing
- Error Messages
- Validation

**Pitfalls to avoid:**
- Pitfall 1: Command Hallucination (validate against registry)

**Research needed:** No - translation logic is straightforward

---

#### Phase 4: CLI Schema Standardization
**Rationale:** Standardize JSON outputs for reliable parsing. Can be done in parallel with Phase 2-3 but affects executor implementation.

**Delivers:**
- Standardized JSON response format for all commands
- Consistent error reporting with `retryable` and `suggestion` fields
- Documented schemas for each command

**Features from FEATURES.md:**
- JSON Output Parsing (improved reliability)
- Error Messages (better disambiguation)

**Pitfalls to avoid:**
- Pitfall 5: Inconsistent JSON Output Schemas
- Pitfall 7: Poor Error Message Disambiguation

**Research needed:** No - schema standardization is a well-understood problem

---

#### Phase 5: Dry-Run Mode
**Rationale:** Enables validation without side effects. Builds on standardized schemas from Phase 4.

**Delivers:**
- `--dry-run` flag for all CLI commands
- Pre-execution validation in executor
- Safe command testing workflow

**Features from FEATURES.md:**
- Dry-Run Mode
- Validation

**Pitfalls to avoid:**
- Pitfall 6: No Dry-Run Capability

**Research needed:** No - dry-run is a standard CLI pattern

---

#### Phase 6: Skill Composition (Optional, Post-MVP)
**Rationale:** Higher-level skills that combine multiple commands. Defer until basic skills are working.

**Delivers:**
- Composite skills (e.g., "FULL_LAUNCH" = build + launch + verify)
- Multi-command workflow skills
- State validation chains

**Features from FEATURES.md:**
- Skill Composition
- Conditional Execution
- State Validation

**Pitfalls to avoid:**
- Over-engineering; keep orchestrator composing skills when possible

**Research needed:** Yes - determine which compositions provide value vs. orchestrator coordination

---

### Research Flags

| Phase | Research Needed | Reason |
|-------|-----------------|--------|
| Phase 1 | No | Skills map 1:1 to existing CLI commands |
| Phase 2 | No | Delegation pattern established in Phase 27 |
| Phase 3 | No | Translation is straightforward lookup |
| Phase 4 | No | Schema standardization is well-defined |
| Phase 5 | No | Dry-run is standard pattern |
| Phase 6 | Yes | Need to determine which compositions add value |

**Standard patterns (skip research):** Phases 1-5 all follow established patterns. Only Phase 6 (Skill Composition) may need `/gsd:research-phase` during planning.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| **Stack** | HIGH | Based on verified Claude Code skill documentation + existing CLI analysis |
| **Features** | HIGH | CLI has 258 documented commands; skill mapping is straightforward |
| **Architecture** | HIGH | Phase 27 established delegation pattern; this adds skill abstraction |
| **Pitfalls** | HIGH | Real project examples of command hallucination failures documented |

**Gaps to Address:**

1. **Skill composition value unclear** - Need to validate whether composite skills provide value vs. orchestrator coordinating multiple skills (Phase 6)

2. **Parameter type complexity** - Current proposal uses primitives; may need complex types if orchestrator requires (flagged for Phase 1)

3. **Skill versioning** - How to handle CLI changes that break skills (flagged as open question, can defer until CLI evolves)

4. **Naming convention** - kebab-case vs snake_case for skill names (recommendation: kebab-case to match CLI)

---

## Sources

### STACK.md Sources
- [Inside Claude Code Skills](https://mikhail.io/2025/10/claude-code-skills/) - HIGH confidence: Official skill structure
- [Claude Agent Skills Deep Dive](https://leehanchung.github.io/blogs/2025/10/26/claude-skills-deep-dive/) - HIGH confidence: Comprehensive analysis
- [Anthropic skill-creator SKILL.md](https://raw.githubusercontent.com/anthropics/skills/main/skills/skill-creator/SKILL.md) - HIGH confidence: Official format
- [Extend Claude with skills - Claude Code Docs](https://code.claude.com/docs/en/skills) - MEDIUM confidence: Official docs
- `skills_scripts/ui_automation/Commands/*.cs` - HIGH confidence: Verified CLI structure

### FEATURES.md Sources
- `skills_scripts/ui_automation/Commands/` - HIGH confidence: 11 handler files, 258 commands analyzed
- `ExitCodes.cs` - HIGH confidence: Standardized exit codes
- [AWS CLI Agent Orchestrator](https://aws.amazon.com/blogs/opensource/introducing-cli-agent-orchestrator/) - MEDIUM confidence: Industry validation
- [AWS Agentic AI Patterns](https://docs.aws.amazon.com/pdfs/prescriptive-guidance/latest/agentic-ai-patterns/agentic-ai-patterns.pdf) - MEDIUM confidence
- [The Age of the CLI, Part 2](https://hyperdev.matsuoka.com/p/the-age-of-the-cli-part-2) - MEDIUM confidence

### ARCHITECTURE.md Sources
- `.claude/agents/test-orchestrator.md` - HIGH confidence: Current orchestrator
- `.claude/agents/test-executor.md` - HIGH confidence: Current executor
- `.claude/agents/log-analyst.md` - HIGH confidence: Log analyst definition
- `.planning/phases/27-orchestrator-delegation-fix/27-RESEARCH.md` - HIGH confidence: Delegation research

### PITFALLS.md Sources
- `.planning/phases/27-orchestrator-delegation-fix/27-CONTEXT.md` - HIGH confidence: Project context
- `.planning/phases/27-orchestrator-delegation-fix/27-RESEARCH.md` - HIGH confidence: Delegation analysis
- `.claude/agents/test-orchestrator.md` - HIGH confidence: Known issues
- `docs/cli-reference.md` - HIGH confidence: CLI documentation
- [Top 7 CLI Developer Experience Mistakes 2025](https://www.techbuddies.io/2026/01/09/top-7-cli-developer-experience-mistakes-devs-still-make-in-2025/) - MEDIUM confidence
- [Tool Use in Agentic AI 2025 Overview](https://samiranama.com/posts/Tool-Use-in-Agentic-AI-A-2025-Systems-Overview/) - MEDIUM confidence
- [Security & Guardrails in AI Systems 2025](https://medium.com/@dewasheesh.rana/security-guardrails-in-ai-systems-2025-a-complete-engineering-guide-from-layman-to-professional-f9383336c8ab) - MEDIUM confidence

---

## Ready for Roadmap

All research complete. Recommended next step: `/gsd:roadmap` to generate phase-based implementation plan.

**Key decisions for roadmapper:**
- Phases 1-3 are foundational and sequential
- Phase 4 (CLI Schema) can run parallel to Phases 2-3
- Phase 5 (Dry-Run) depends on Phase 4
- Phase 6 (Composition) is optional, post-MVP
- Only Phase 6 may need `/gsd:research-phase`

---

*Synthesis completed: 2026-01-21*
*Valid for: 90 days*
