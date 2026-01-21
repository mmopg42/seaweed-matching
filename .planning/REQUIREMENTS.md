# Requirements: ChronoView CLI Skill Encapsulation

**Defined:** 2026-01-21
**Core Value:** UI 요소 식별 및 조작 — ChronoView의 모든 UI 요소를 안정적으로 식별하고 조작

## v1.5 Requirements

Requirements for CLI Skill Encapsulation & Delegation Fix milestone. Each maps to roadmap phases.

### Skill Registry (SKILL)

- [ ] **SKILL-01**: All 30+ CLI commands have corresponding skill definitions
- [ ] **SKILL-02**: Each skill has semantic name (intent-based, not CLI-based)
- [ ] **SKILL-03**: Skill definition includes exact CLI command pattern
- [ ] **SKILL-04**: Skill definition includes parameter schema
- [ ] **SKILL-05**: Skill registry is documented in test-executor-skills.md
- [ ] **SKILL-06**: Skills organized by category (Lifecycle, Workflow, Settings, Window, Data, Diagnostic)

### Orchestrator Delegation (ORCH)

- [ ] **ORCH-01**: Orchestrator uses skill names only (no CLI commands in delegation)
- [ ] **ORCH-02**: test-orchestrator.md updated with skill reference section
- [ ] **ORCH-03**: "No Command Construction" prohibition added with examples
- [ ] **ORCH-04**: Delegation template updated to use skill names
- [ ] **ORCH-05**: Orchestrator cannot construct CLI commands (documentation prohibition)

### Executor Translation (EXEC)

- [ ] **EXEC-01**: test-executor.md includes skill-to-CLI translation patterns
- [ ] **EXEC-02**: Executor validates skill names against registry
- [ ] **EXEC-03**: Executor reports error for unknown skills
- [ ] **EXEC-04**: Retry logic respects skill `retryable` flag
- [ ] **EXEC-05**: Error handling includes skill context in messages

### CLI Schema Standardization (SCHEMA)

- [ ] **SCHEMA-01**: Standardized JSON response format across all commands
- [ ] **SCHEMA-02**: Consistent error reporting with `success`, `data/error`, `errorCode` fields
- [ ] **SCHEMA-03**: Error responses include `retryable` boolean
- [ ] **SCHEMA-04**: Error responses include `suggestion` string for common failures
- [ ] **SCHEMA-05**: JSON schemas documented for each command category

### Dry-Run Mode (DRYRUN)

- [ ] **DRYRUN-01**: `--dry-run` flag added to all CLI commands
- [ ] **DRYRUN-02**: Dry-run returns command that would execute without execution
- [ ] **DRYRUN-03**: Dry-run validates parameter syntax
- [ ] **DRYRUN-04**: Dry-run validates skill exists (in executor)
- [ ] **DRYRUN-05**: Dry-run documented in test-executor.md

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Skill Composition (COMPOSE)

- **COMPOSE-01**: Composite skills combine multiple commands
- **COMPOSE-02**: Multi-command workflow skills
- **COMPOSE-03**: State validation chains

### Advanced Features (ADVANCED)

- **ADV-01**: Conditional execution based on application state
- **ADV-02**: Fallback chains (try primary, fall back to alternative)
- **ADV-03**: Skill versioning for CLI evolution

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Live CLI Discovery (parsing --help) | Fragile, static registry more reliable |
| Natural Language Parsing | LLMs already handle intent-to-skill mapping |
| Remote Skill Execution | Adds latency, security risk; keep local |
| Dynamic Skill Generation | Unpredictable, hard to debug; define explicitly |
| Stateful Skills | Hard to reason about; keep stateless |
| Auto-Retry Without Orchestrator Input | CLI failures may need human intervention |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| SKILL-01 through SKILL-06 | Phase 28 | Pending |
| ORCH-01 through ORCH-05 | Phase 29 | Pending |
| EXEC-01 through EXEC-05 | Phase 30 | Pending |
| SCHEMA-01 through SCHEMA-05 | Phase 31 | Pending |
| DRYRUN-01 through DRYRUN-05 | Phase 32 | Pending |

**Coverage:**
- v1.5 requirements: 25 total
- Mapped to phases: 25
- Unmapped: 0 ✓

---
*Requirements defined: 2026-01-21*
*Last updated: 2026-01-21 after initial definition*
