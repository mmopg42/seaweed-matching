# Phase 23: Performance & Documentation - Context

**Gathered:** 2026-01-20
**Status:** Ready for planning

<vision>
## How This Should Work

This phase completes v1.3 by making tests faster and documenting the new setup capabilities.

**Performance (Plan 23-01):**
Tests currently have unnecessary delays between operations. By optimizing these delays while keeping all safety checks intact, we can achieve 20-30% speedup. The goal is reliability with moderate improvement — not aggressive optimization that might break things.

**Documentation (Plan 23-02):**
The test-executor and test-orchestrator docs need to document the new setup workflow commands. Use a concise table format with commands grouped by use case (basic setup, verification, camera operations). The setup verification step should be integrated into the workflow description, not a separate section.

**CLI Registration (Plan 23-03):**
Four setup commands need to be verified as registered in CommandRegistry:
- `setup open-settings`
- `setup complete-full`
- `setup verify-config`
- `setup camera-states`

All use the `setup` prefix namespace. Follow the existing registry pattern from Phase 11-19.

</vision>

<essential>
## What Must Be Nailed

1. **Safety preserved** — All connectivity checks and validations must remain. Speed comes from delay optimization, not removing safeguards.

2. **Concise reference format** — Documentation should be tables/bulleted lists, not lengthy prose. Quick lookup for experienced users.

3. **Use case grouping** — Commands should be documented by how they're used (basic setup, verification, camera ops), not just alphabetical or technical groupings.

4. **Registry verification** — Confirm setup commands are registered using the standard CommandRegistry pattern. If not already registered, add them following the established pattern.

</essential>

<specifics>
## Specific Ideas

**Performance:**
- Target timeouts: 2-5 seconds (medium — balanced for reliability)
- Optimize delays between operations, not the operations themselves
- Keep all pre-checks (connectivity, window detection, etc.)

**Documentation:**
- Append to existing structure — don't reorganize everything
- Group commands by use case:
  - Basic Setup: open-settings, start-monitoring
  - Camera Operations: camera-general, camera-nir1, camera-nir2, toggle-nir-filtering
  - Verification: verify-config, camera-states
  - Full Workflow: complete-full (with verify-config integrated as a step)
- Format: Concise tables, not prose

**CLI Registration:**
- Use `setup` prefix for all commands
- Follow CommandRegistry pattern (not custom registration)
- May already be done — verification needed

</specifics>

<notes>
## Additional Context

Phase 23 is the final phase of Milestone v1.3. Phase 22 delivered:
- ChronoSetupWindowController (10 methods)
- SetupCommands.cs (8 CLI commands)
- SetupConfigVerifier (config comparison)

This phase focuses on polishing — making the setup workflow faster to use in tests and properly documented.

User explicitly wants to keep all safety checks while optimizing. The speed gains should come from reducing unnecessary waits between operations, not from skipping validations.

The documentation update is for test-executor.md and test-orchestrator.md — these are agent-facing docs that need to show how to use the new setup commands.

</notes>

---

*Phase: 23-performance-documentation*
*Context gathered: 2026-01-20*
