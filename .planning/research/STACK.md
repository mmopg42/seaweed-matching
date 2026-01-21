# Technology Stack

**Project:** ChronoView CLI Skill Infrastructure
**Researched:** 2026-01-21
**Milestone:** Adding CLI Skill encapsulation layer to existing automation system

## Executive Summary

The CLI skill layer requires **NO NEW TECHNOLOGY**. The existing stack (Claude Code skills + C# CLI tool) already provides all necessary primitives. Skills are **SKILL.md files** with optional bundled resources (`scripts/`, `references/`, `assets/`) that get discovered automatically by Claude Code from `.claude/skills/` at runtime.

**Key insight:** Skills hide exact CLI syntax by providing semantic task descriptions. Orchestrators invoke skills by name; Claude Code loads the SKILL.md which contains the exact command patterns, arguments, and error handling logic.

---

## Recommended Stack

### Skill Definition Format

| Technology | Version/Format | Purpose | Why |
|------------|----------------|---------|-----|
| **SKILL.md** | Markdown with YAML frontmatter | Skill definition | Native Claude Code format - no parsing needed, auto-discovered from `.claude/skills/` |
| **scripts/** | Python 3.x | Command execution wrapper | Skills can bundle Python scripts; test-executor already uses Python |
| **references/** | Markdown | Command reference documentation | Progressive disclosure - loaded only when skill triggers |

**No JSON. No YAML files. No custom parser.** The skill definition IS the documentation.

### Skill Frontmatter Format

```yaml
---
name: chrono-cli
description: ChronoView UI automation commands. Use when orchestrators need to control ChronoView application windows, toolbar buttons, data panels, settings, workflows, or file operations through CLI.
---
```

**Required fields:**
- `name` - Skill identifier (invoked as `command: "chrono-cli"`)
- `description` - Primary triggering mechanism - Claude reads this to decide when to use

**Optional fields (explicitly NOT recommended):**
- `allowed-tools` - Skills should not execute tools directly; they guide CLI syntax selection
- `model` - Inherit session default
- `license` - Internal skill, no distribution needed

### Skill Directory Structure

```
.claude/skills/chrono-cli/
├── SKILL.md              # Core prompt: task categories, command patterns, error handling
├── references/
│   ├── commands.md       # Complete CLI command reference (grouped by category)
│   ├── exit-codes.md     # Exit code meanings and handling
│   └── examples.md       # Concrete invocation examples
└── scripts/
    └── cli_wrapper.py    # Optional: Python wrapper for complex multi-command workflows
```

**Why this structure:**
- **SKILL.md** is loaded when skill triggers - contains semantic task -> command mapping
- **references/** keeps detailed command docs out of initial context (progressive disclosure)
- **scripts/** enables Python-based workflow orchestration if needed

---

## CLI Integration Points

### Existing CLI Tool (No Changes Required)

| Component | Location | Status |
|-----------|----------|--------|
| C# CLI Entry Point | `skills_scripts/ui_automation/Program.cs` | **READY** - System.CommandLine with --json flag |
| Command Registry | `skills_scripts/ui_automation/Commands/CommandRegistry.cs` | **READY** - 12 handler classes registered |
| Exit Codes | `skills_scripts/ui_automation/Commands/ExitCodes.cs` | **READY** - SUCCESS=0, ERROR=1, NOT_FOUND=2, TIMEOUT=3 |
| JSON Output | All commands support `--json` / `-j` | **READY** - Structured output for programmatic parsing |

### CLI Command Categories (Mapped to Skills)

| Handler | Commands | Skill Category |
|---------|----------|----------------|
| `AppLifecycleCommands` | `app launch`, `app stop`, `app restart`, `app status` | Application lifecycle |
| `WindowsCommands` | `windows main`, `windows setup`, `windows settings`, `windows preview`, `windows all` | Window detection |
| `ToolbarCommands` | `toolbar start`, `toolbar stop`, `toolbar settings`, `toolbar click`, `toolbar list` | Toolbar interaction |
| `DataPanelCommands` | `datapanel read`, `datapanel select-row`, `datapanel get-count` | Data panel queries |
| `SettingsCommands` | `settings open`, `settings navigate`, `settings set-value` | Settings manipulation |
| `WorkflowCommands` | `workflow start`, `workflow stop`, `workflow check-state` | Workflow control |
| `FileOpsCommands` | `fileops move-selected`, `fileops delete-selected` | File operations |
| `SetupCommands` | `setup navigate`, `setup set-path`, `setup complete` | Setup automation |
| `TestCommands` | `test run-scenario`, `test verify-state` | Test execution |
| `UtilityCommands` | `util wait`, `util screenshot`, `util dump-ui` | Utilities |

---

## Skill Invocation Flow

```
Orchestrator Request
    "Find the ChronoView MainWindow and click the Start button"
        ↓
Claude matches request to skill description
    "chrono-cli: ChronoView UI automation commands..."
        ↓
Skill tool invoked with command: "chrono-cli"
        ↓
SKILL.md loaded into context
    "When user asks to find windows, use: windows [--json]
     When user asks to click toolbar buttons, use: toolbar click <text> [--json]"
        ↓
Claude constructs exact CLI commands
    $ dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- windows main --json
    $ dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- toolbar start --json
        ↓
JSON output parsed and returned to orchestrator
```

**Key benefit:** Orchestrators NEVER need to know CLI syntax. They describe intent; skill provides exact commands.

---

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|----------|-------------|-------------|---------|
| Skill Format | SKILL.md (native) | JSON/YAML schema files | Requires custom parser; not auto-discovered by Claude Code |
| Command Docs | references/*.md in skill | External wiki/docs | Breaks skill encapsulation; docs not available when skill triggers |
| Execution | Direct CLI invocation | MCP server wrapper | Over-engineering; CLI already has structured JSON output |
| Skill Location | `.claude/skills/` (project-scoped) | `~/.claude/skills/` (user-scoped) | Project-specific skills should travel with repo |

---

## Skill Composition Patterns

### Pattern 1: Single Command (Low Freedom)

**SKILL.md content:**
```markdown
## Finding Windows

To find ChronoView windows, use:

\`\`\`bash
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- windows <target> --json
\`\`\`

Targets: `main`, `setup`, `settings`, `preview`, `all`

Example:
\`\`\`bash
# Find MainWindow
windows main --json
# Returns: {"success": true, "data": {"found": true, "title": "ChronoView Pro", ...}}
\`\`\`
```

**When to use:** Deterministic operations with fixed command structure

### Pattern 2: Parameterized Command (Medium Freedom)

**SKILL.md content:**
```markdown
## Clicking Toolbar Buttons

To click toolbar buttons by text:

\`\`\`bash
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- toolbar click "<button-text>" --json
\`\`\`

Common button texts: "Start", "Stop", "Settings", "Refresh", "Move", "Delete"

Verify button exists first:
\`\`\`bash
toolbar list --json  # Shows available buttons
\`\`\`
```

**When to use:** Commands with parameters but predictable structure

### Pattern 3: Multi-Command Workflows (High Freedom)

**SKILL.md content:**
```markdown
## Complete Application Launch

Workflow to launch ChronoView and verify MainWindow:

1. Launch: `app launch --json`
2. Wait for MainWindow: `windows main --json` (retry up to 10s)
3. Verify toolbar: `toolbar list --json`

See [examples/launch-verify.md](examples/launch-verify.md) for complete script.
```

**When to use:** Complex workflows requiring orchestration decisions

---

## What NOT to Add

| Anti-Pattern | Why Avoid | Alternative |
|--------------|-----------|-------------|
| JSON command schema | Requires parsing; SKILL.md IS the schema | Use SKILL.md with examples |
| Python MCP server | Unnecessary layer; CLI already has structured output | Direct CLI invocation with --json |
| Duplicate docs in SKILL.md | Context bloat; references/ exists for this | Keep SKILL.md lean, link to references/ |
| Custom skill loader | Claude Code auto-discovers .claude/skills/ | Use native discovery |
| Skill registry database | Over-engineering; file system is the registry | File-based skill organization |

---

## Verification Strategy

### Skill Validation Checklist

Each skill MUST have:

- [ ] YAML frontmatter with `name` and `description`
- [ ] `description` explicitly states WHEN to use the skill
- [ ] Command examples with actual invocation syntax
- [ ] Error handling guidance (exit codes, retry logic)
- [ ] Links to reference docs for detailed information

### CLI Command Coverage

Verify each CLI command has corresponding skill documentation:

```bash
# List all CLI commands
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- --help

# For each command, verify SKILL.md describes:
# 1. When to use this command
# 2. Exact syntax with arguments
# 3. Expected JSON output format
# 4. Error conditions and handling
```

---

## Installation

**No installation required.** Skills are auto-discovered from `.claude/skills/`.

To add a new skill:

```bash
# Create skill directory
mkdir -p .claude/skills/chrono-cli/references

# Create SKILL.md with proper frontmatter
# See above for frontmatter format

# Claude Code will auto-discover on next launch
```

---

## Sources

- [Inside Claude Code Skills: Structure, prompts, invocation](https://mikhail.io/2025/10/claude-code-skills/) - HIGH confidence: Official skill structure from reverse-engineered Claude Code session
- [Claude Agent Skills: A First Principles Deep Dive](https://leehanchung.github.io/blogs/2025/10/26/claude-skills-deep-dive/) - HIGH confidence: Comprehensive skill architecture analysis
- [Anthropic official skill-creator SKILL.md](https://raw.githubusercontent.com/anthropics/skills/main/skills/skill-creator/SKILL.md) - HIGH confidence: Official frontmatter format and directory structure
- [Extend Claude with skills - Claude Code Docs](https://code.claude.com/docs/en/skills) - MEDIUM confidence: Official documentation (accessed via WebFetch summary)
- Existing codebase analysis: `skills_scripts/ui_automation/Commands/*.cs` - HIGH confidence: Verified CLI structure and capabilities
