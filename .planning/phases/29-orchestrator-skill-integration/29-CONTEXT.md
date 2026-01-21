# Phase 29: Orchestrator Skill Integration - Context

**Gathered:** 2026-01-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Update test-orchestrator agent documentation to use semantic skill names instead of constructing CLI commands. This phase modifies only documentation patterns — no C# code changes. The orchestrator delegates by intent (skill names), and the executor handles CLI translation.

</domain>

<decisions>
## Implementation Decisions

### Delegation Format
- **Structured delegation:** Skill name, parameters, and execution context
- **Expression style:** Tool-like format (e.g., `Skill(skill='LAUNCH_APP', args={'target': 'chrono'})`)
- **Context included:** Args plus test context (what's being tested)
- **Timeout and retry:** Optional override fields — orchestrator can specify, otherwise executor uses defaults
- **Documentation examples:** Both minimal and full examples shown side-by-side
- **Description field:** Include for audit trail and documentation

### Error Handling
- **Retry logic:** Follow the skill's `retryable` flag — auto-retry if true, consult orchestrator if false
- **Failure impact:** Criticality-based — some failures abort the test phase, others allow continuation
- **Reporting format:** Full diagnostic — console output during execution, final report inclusion, executor's suggestion field
- **Report content:** Error code, message, suggestion, plus orchestrator context (test phase, step number)

### Skill Reference Section
- **Content:** Full schema by category (category, name, description, parameter schemas)
- **Catalog approach:** Link to registry file (`test-executor-skills.md`) rather than duplicating all 90+ skills
- **Parameter docs:** Example-based — show usage patterns rather than formal schemas
- **Examples:** Pattern examples demonstrating common usage across skill categories

### Prohibition Language
- **Strictness:** Absolute prohibition — never construct CLI commands, skills only, no exceptions
- **Format:** Both callout warning box AND dedicated section for maximum visibility
- **Example structure:** Anti-pattern first — show "don't do this" (bad CLI example), then "do this" (good skill example)
- **Rationale:** Include explanation — separation of concerns (orchestrator = intent, executor = implementation), abstraction benefits, future-proofing

### Claude's Discretion
- Exact wording of callout and section text
- Which examples qualify as "common" for pattern documentation
- Specific categorization of skills (13 categories defined in Phase 28)

</decisions>

<specifics>
## Specific Ideas

- "I want the tool-like format to feel familiar to anyone who's used Claude's native tools"
- The anti-pattern examples should be realistic mistakes someone would actually make
- Link to the registry, but include enough context in test-orchestrator that it's self-contained for common cases

</specifics>

<deferred>
## Deferred Ideas

- Code changes to executor for skill translation — Phase 30
- JSON schema standardization — Phase 31
- Dry-run mode — Phase 32

</deferred>

---

*Phase: 29-orchestrator-skill-integration*
*Context gathered: 2026-01-21*
