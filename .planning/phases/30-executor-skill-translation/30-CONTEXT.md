# Phase 30: Executor Skill Translation - Context

**Gathered:** 2026-01-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Enable test-executor agent to translate skill names from orchestrator prompts into CLI commands. The executor receives prompts like "Execute skill: APP_LAUNCH with args: {...}" and must parse, validate, and translate to actual CLI execution.

This is the translation layer that completes the skill abstraction: orchestrator expresses intent (skill name), executor handles implementation (CLI command).
</domain>

<decisions>
## Implementation Decisions

### Parsing approach
- **Strict structured convention:** Parse exact format "Execute skill: SKILL_NAME with args: {...}"
- **JSON argument block:** Args use JSON format: `{"key": "value", "flag": true}`
- **Fail on syntax mismatch:** If prompt doesn't match expected format, fail with clear error
- **Document in test-executor.md:** Include parser examples and regex patterns in the agent documentation

### Validation strategy
- **Validate before execution:** Check skill exists in registry before any CLI execution
- **Check registry file:** Load and validate against test-executor-skills.md
- **Name + args schema validation:** Validate both skill name exists AND all args are in the skill's parameter schema
- **Warn with suggestions:** On unknown skill, log warning and suggest similar skill names

### Unknown skill errors
- **JSON error response format:** `{error, code, suggestions[], valid_skills[]}`
- **Similarity-based suggestions:** Use fuzzy matching (Levenshtein distance) to find similar skills
- **Return to orchestrator:** Don't skip or continue—return error and let orchestrator decide retry
- **Dedicated error code:** Create ExitCode.UnknownSkill (value: 4) in ExitCodes.cs

### Translation implementation
- **CLI field in registry:** Each skill entry has `cli: "exact command"` field, executor reads directly
- **Template substitution:** Insert args into CLI template placeholders (e.g., `{rows}`, `{key}`)
- **Always add --json:** Automatically include --json flag for structured output
- **Log translated command:** Show full translated CLI in executor logs for debugging transparency

### Claude's Discretion
- Exact regex pattern for parsing skill names and args
- Fuzzy matching threshold for similarity suggestions
- CLI template placeholder syntax
- Error message wording and helpfulness level

</decisions>

<specifics>
## Specific Ideas

- "The executor should act like a compiler—strict on syntax, helpful on errors"
- "Skills are the new API—skill names are stable, CLI details can change underneath"
- "When suggesting skills, show 3 closest matches based on string similarity"

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 30-executor-skill-translation*
*Context gathered: 2026-01-21*
