# Feature Landscape: CLI Skill Encapsulation

**Domain:** CLI Automation Command Abstraction Layer
**Researched:** 2026-01-21
**Overall confidence:** HIGH (based on existing CLI analysis + industry patterns from AWS CLI Agent Orchestrator)

## Executive Summary

ChronoView CLI has 30+ commands spread across 11 command handlers (WindowsCommands, WorkflowCommands, ToolbarCommands, etc.). The current design requires orchestrators to know exact command syntax like `workflow select-tab "Line 2"` or `file-ops select prefix --prefix line2_`.

The **skill encapsulation layer** introduces semantic intents ("switch-to-line2", "select-line2-files") that map to appropriate CLI commands. This decouples orchestrator logic from CLI implementation details, allowing CLI evolution without breaking orchestrators.

Industry validation: [AWS CLI Agent Orchestrator](https://aws.amazon.com/blogs/opensource/introducing-cli-agent-orchestrator-transforming-developer-cli-tools-into-a-multi-agent-powerhouse/) (October 2025) formalizes orchestration patterns including **Handoff** (synchronous task transfer) and **Assign** (asynchronous parallel execution), supporting the skill abstraction approach.

---

## Table Stakes

Features users expect in a CLI skill system. Missing = orchestrator cannot function reliably.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Skill-to-CLI Command Mapping** | Core purpose: semantic intent -> CLI command | Low | Dictionary-based lookup: `skill_name -> {command, args}` |
| **Parameter Substitution** | Skills need dynamic parameters (row index, GroupId, etc.) | Low | Template-based: `workflow select-tab "{tab_name}"` |
| **Exit Code Translation** | Orchestrators need standardized success/failure signals | Low | Map CLI exit codes (0,1,2,3,4) to skill result enums |
| **JSON Output Parsing** | Orchestrators consume structured data | Low | Parse `--json` output into skill result objects |
| **Skill Registry/Discovery** | Orchestrators must enumerate available skills | Low | `ListSkills()` returning skill metadata (name, description, params) |
| **Validation** | Reject invalid skill calls before execution | Med | Parameter validation against skill schema |
| **Error Messages** | Orchestrators need actionable failure descriptions | Low | Human-readable + machine-parsable error codes |
| **Dry-Run Mode** | Orchestrators test skill calls without side effects | Med | Return CLI command that would be executed |

---

## Differentiators

Features that set this implementation apart from generic CLI wrappers.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Intent-Based Naming** | Orchestrators use "switch-to-line2" instead of "workflow select-tab 'Line 2'" | Low | Semantic names self-document and reduce LLM hallucination |
| **Skill Composition** | Combine multiple CLI commands into single skill (e.g., "start-line2-monitoring" = launch NIR2 + switch tab + start monitoring) | Med | Skills can call multiple CLI commands sequentially |
| **State Validation** | Skills verify preconditions (e.g., "is ChronoView running?") before execution | Med | Uses `test connectivity` command as guard |
| **Conditional Execution** | Skills branch based on state (e.g., "open-settings-if-closed") | Med | Enables robust workflows |
| **Fallback Chains** | Try primary command, fall back to alternative if unavailable | High | Example: Try `workflow select-tab`, fall back to `toolbar click "Line 2"` |
| **Skill Versioning** | Skills report version for compatibility checking | Low | Enables orchestrator to adapt to skill changes |
| **Telemetry/Logging** | Track skill usage for debugging and optimization | Med | Log skill invocations, duration, success rate |
| **Local-Only Execution** | No external API calls; skills encapsulate local CLI only | Low | Privacy-preserving, offline-capable |

---

## Anti-Features

Features to explicitly NOT build. Common mistakes in this domain.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| **Live CLI Command Discovery** | Parsing `--help` output is fragile; commands change | Declare skills statically in code |
| **Natural Language Parsing** | LLMs already handle intent extraction; redundant layer | Let orchestrator choose skills from semantic names |
| **Remote Skill Execution** | CLI is local; remote adds latency, security risk | Keep skills local; orchestrators coordinate remotely |
| **Dynamic Skill Generation** | Unpredictable; hard to debug; security risk | Define skills explicitly in source code |
| **CLI Argument Guessing** | Orchestrator guessing `--line 2` creates wrong commands | Skills map intent to exact syntax |
| **Stateful Skills** | Skills with hidden state are hard to reason about | Keep skills stateless; pass all parameters explicitly |
| **Skill Overriding** | Allowing runtime skill replacement breaks contracts | Use skill versioning instead |
| **Auto-Retry** | CLI failures may need human intervention; retry can cause damage | Let orchestrator decide retry strategy |

---

## Feature Dependencies

```
Skill Registry (base)
    -> Skill Definition (depends on Registry)
    -> Skill Discovery (depends on Registry)

Skill Definition
    -> Parameter Substitution (uses skill templates)
    -> Validation (validates against skill schema)

Skill Execution
    -> Command Mapping (uses skill->CLI mapping)
    -> Parameter Substitution (builds CLI args)
    -> Dry-Run Mode (optional execution path)

Result Processing
    -> Exit Code Translation (interprets CLI exit codes)
    -> JSON Output Parsing (extracts structured data)
    -> Error Messages (formats failures)
```

---

## MVP Recommendation

For MVP, prioritize:

1. **Skill-to-CLI Command Mapping** (core feature)
   - Static registry mapping skill names to CLI commands
   - Example: `switch-to-line2` -> `workflow select-tab "Line 2"`

2. **Parameter Substitution** (enables dynamic behavior)
   - Simple template replacement: `{row_index}` -> actual value

3. **Exit Code Translation** (reliable result signaling)
   - Map ExitCodes (SUCCESS=0, ERROR=1, NOT_FOUND=2, TIMEOUT=3, INVALID_ARGUMENT=4) to skill results

4. **Skill Registry/Discovery** (orchestrator integration)
   - `ListSkills()` returns skill metadata
   - `GetSkill(name)` returns skill definition

5. **JSON Output Parsing** (structured data consumption)
   - Parse `--json` output into result objects

Defer to post-MVP:

- **Skill Composition** (complex; can be handled by orchestrator calling multiple skills)
- **Fallback Chains** (orchestrator can implement retry logic)
- **Telemetry/Logging** (nice-to-have for debugging)

---

## Existing CLI Command Categories (for Skill Mapping)

Based on analysis of 258 commands across 11 handler files:

| Category | Commands | Skill Examples |
|----------|----------|----------------|
| **App Lifecycle** | `app launch`, `app stop`, `app restart`, `app status` | `ensure-running`, `restart-app`, `check-status` |
| **Windows** | `windows main`, `windows setup`, `windows settings`, `windows preview` | `find-main-window`, `open-settings-dialog` |
| **Toolbar** | `toolbar start`, `toolbar stop`, `toolbar settings`, `toolbar move` | `start-monitoring`, `stop-monitoring`, `open-settings` |
| **Workflow** | `workflow select-tab`, `workflow path get-line2`, `workflow launch-nir2` | `switch-to-line2`, `get-line2-paths`, `launch-nir2-camera` |
| **Data Panel** | `stats`, `datagrid rows`, `datagrid data` | `get-statistics`, `get-row-count`, `export-data` |
| **File Operations** | `file-ops select`, `file-ops move`, `file-ops delete` | `select-by-groupid`, `move-selected`, `delete-selected` |
| **Settings Dialog** | `settings-dialog path get-line2`, `settings-dialog checkbox set` | `get-line2-path`, `set-checkbox-state` |
| **Logs** | `logs get`, `logs tail`, `logs search` | `get-recent-logs`, `search-logs` |
| **Setup** | `setup verify-config`, `setup camera-states` | `verify-setup-config`, `get-camera-states` |
| **Test** | `test connectivity`, `test capabilities` | `check-connectivity`, `list-capabilities` |
| **Scenario** | `scenario start-monitoring`, `scenario configure-paths` | `start-full-monitoring`, `configure-all-paths` |
| **Batch** | `batch select-and-move`, `batch export-all` | `bulk-move`, `export-all-data` |

---

## Skill Interface Proposal

```csharp
// Skill definition
public class Skill
{
    public string Name { get; init; }              // e.g., "switch-to-line2"
    public string Description { get; init; }       // Human-readable description
    public SkillParameter[] Parameters { get; init; }
    public SkillCommandMapping Command { get; init; }
}

// Skill parameter
public class SkillParameter
{
    public string Name { get; init; }              // e.g., "tab_name"
    public string Type { get; init; }              // "string", "int", "bool"
    public bool Required { get; init; }
    public string DefaultValue { get; init; }
}

// Skill-to-CLI mapping
public class SkillCommandMapping
{
    public string CommandTemplate { get; init; }   // e.g., "workflow select-tab \"{tab_name}\""
    public string[] JsonOutputPath { get; init; }  // e.g., ["data", "found"] for extracting value
}

// Skill execution result
public class SkillResult
{
    public bool Success { get; init; }
    public SkillErrorCode ErrorCode { get; init; }
    public JsonElement Data { get; init; }
    public string CliCommand { get; init; }        // Actual CLI command executed
}

// Skill registry interface
public interface ISkillRegistry
{
    Skill[] ListSkills();
    Skill GetSkill(string name);
    Task<SkillResult> ExecuteAsync(string skillName, Dictionary<string, object> parameters);
    Task<string> DryRunAsync(string skillName, Dictionary<string, object> parameters);
}
```

---

## Complexity Notes

- **Low complexity**: Dictionary lookups, string templates, JSON parsing (existing infrastructure)
- **Medium complexity**: Parameter validation, conditional execution, state checks (requires careful design)
- **High complexity**: Fallback chains, skill composition (defer to post-MVP)

---

## Sources

- **Existing CLI codebase**: Analysis of `skills_scripts/ui_automation/Commands/` (11 handler files, 258 commands)
- **ExitCodes.cs**: Standardized exit codes (SUCCESS=0, ERROR=1, NOT_FOUND=2, TIMEOUT=3, INVALID_ARGUMENT=4)
- **AWS CLI Agent Orchestrator** (October 2025): [AWS Blog Post](https://aws.amazon.com/blogs/opensource/introducing-cli-agent-orchestrator-transforming-developer-cli-tools-into-a-multi-agent-powerhouse/)
- **AWS Agentic AI Patterns** (July 2025): [AWS Prescriptive Guidance](https://docs.aws.amazon.com/pdfs/prescriptive-guidance/latest/agentic-ai-patterns/agentic-ai-patterns.pdf)
- **CLI Orchestration Analysis** (January 2026): [The Age of the CLI, Part 2](https://hyperdev.matsuoka.com/p/the-age-of-the-cli-part-2)

---

## Open Questions

1. **Skill naming convention**: Should skills use kebab-case (`switch-to-line2`) or snake_case (`switch_to_line2`)?
   - Recommendation: kebab-case for consistency with CLI naming

2. **Skill granularity**: Should each CLI command have a corresponding skill, or group related commands?
   - Recommendation: Start with 1:1 mapping for common commands, add composite skills post-MVP

3. **Version compatibility**: How to handle CLI changes that break existing skills?
   - Recommendation: Include skill version in metadata; orchestrators check compatibility

4. **Parameter types**: Support complex types (arrays, objects) or only primitives?
   - Recommendation: Primitives for MVP; add complex types if needed by orchestrator

---

## Phase-Specific Flags

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Skill Registry Definition | Over-engineering with dynamic loading | Use static code-based registry; add dynamic loading later |
| Parameter Substitution | Injection attacks from unvalidated input | Sanitize all parameters; use argument-based passing |
| Error Handling | Silent failures hiding real issues | Always map exit codes; include CLI stderr in error messages |
| JSON Parsing | Fragile path-based extraction | Use robust JSON path with fallbacks for missing fields |
