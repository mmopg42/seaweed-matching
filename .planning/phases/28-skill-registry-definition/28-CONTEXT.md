# Phase 28: Skill Registry Definition - Context

**Gathered:** 2026-01-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Create semantic skill definitions for all 30+ existing CLI commands. The skills are an abstraction layer that maps intent-based names to exact CLI syntax, enabling AI agents to use "what to do" instead of "how to do it". This phase defines the skill registry only — orchestration updates (Phase 29) and executor translation (Phase 30) are separate phases.

</domain>

<decisions>
## Implementation Decisions

### Skill Naming Convention
- **Style**: Action-based (intent + action pairing)
- **Case**: UPPER_SNAKE_CASE (all uppercase)
- **Format**: `<CATEGORY>_<ACTION>` with hierarchical underscores
- **Prefix**: Always include category prefix
- **Mapping**: Direct CLI command translation (hyphens/spaces → underscores, camelCase → UPPER)
- **Examples**:
  - `app launch` → `APP_LAUNCH`
  - `toolbar start` → `TOOLBAR_START`
  - `settings-dialog path get-all` → `SETTINGS_DIALOG_PATH_GET_ALL`
  - `file-ops move group-ids` → `FILE_OPS_MOVE_GROUP_IDS`

### Category Organization
- **Structure**: Keep existing 13 handler-based categories
- **Names**: Match CLI exactly (e.g., `settings-dialog` → `SETTINGS_DIALOG`, `file-ops` → `FILE_OPS`)
- **Order**: Alphabetical sorting
- **Categories**:
  1. APP
  2. BATCH
  3. CONSOLE_LOGS
  4. DATA_PANEL
  5. FILE_OPS
  6. LOGS
  7. SETTINGS_DIALOG
  8. SETUP
  9. TEST
  10. TOOLBAR
  11. UTILITY
  12. WINDOWS
  13. WORKFLOW

### Parameter Schema Format
- **Style**: TypeScript interface definition
- **Required fields**:
  - `skill`: Skill ID (enum type)
  - `desc`: Description (for search/discovery)
  - `cli`: Mapped CLI command string
  - `params`: Input parameters (TS Interface)
  - `returns`: Return data structure (TS Interface)
  - `errors`: Possible error types
- **Type system**: Union types allowed (`string | number`, `{key: string}`)
- **Execution options**: Optional overrides (`timeout?: number`, `retryable?: boolean`)

### Registry Document Structure
- **Location**: `.claude/agents/test-executor-skills.md` (single file)
- **Internal structure**:
  1. Category section heading (`## APP Category`)
  2. Skills overview table (`Skill | Description | CLI Command`)
  3. Detailed skill definitions (TS interface format)
- **Table columns**: Minimal 3-column format (Skill, Description, CLI)

### Legacy & Duplicate Handling
- **Legacy commands** (`detect`, `list`, `find`, `click`): Excluded from registry (deprecated)
- **Duplicate functionality**: Keep both skills when perspectives differ
  - Example: `APP_STATUS` (process focus) vs `WINDOW_MAIN` (window targeting)
- **Cross-references**: Mention related alternatives in skill descriptions

### Claude's Discretion
- Exact TypeScript formatting and indentation style
- Whether to include usage examples in skill descriptions
- How to group related parameters within interfaces

</decisions>

<specifics>
## Specific Ideas

- Naming convention follows the TypeScript interface pattern proposed in `.planning/research/ARCHITECTURE.md`
- Registry will be referenced by both test-orchestrator (skill names only) and test-executor (full definitions)
- Each skill definition should be self-contained for easy lookup
- Consider adding "See also" references in descriptions for alternative skills

</specifics>

<deferred>
## Deferred Ideas

- Skill versioning (when CLI commands change) — note for future consideration
- Dynamic skill discovery (executor exposing skill list API) — Phase 30 consideration
- Skill composition (macros combining multiple skills) — potential future enhancement

</deferred>

---

*Phase: 28-skill-registry-definition*
*Context gathered: 2026-01-21*
