# OpenCode Agent Wrappers

## Available Custom Agents

These custom agents can be used via the Task tool by including their definitions in the prompt.

### 1. plan-validator

**Purpose**: Validate implementation plans before execution

**When to use**: After creating an implementation plan, before coding begins

**Usage**:
```
Task(
  subagent_type="general",
  prompt="You are an elite Plan Validation Architect... [paste full plan-validator definition]",
  description="Validate implementation plan"
)
```

**Key validations**:
- Code reality check
- Requirements alignment
- SSOT compliance
- Code quality assessment
- 600-line limit verification

**Critical areas**: `IConfigurationManager`, `FileGroup`, `MonitoringOrchestrator`, `ViewModelBase`

---

### 2. legacy-code-cleanup-validator

**Purpose**: Validate legacy code removal and compatibility after plan-validator approval

**When to use**: After plan-validator passes, before execution begins

**Usage**:
```
Task(
  subagent_type="general",
  prompt="You are an expert Legacy Code Cleanup and Compatibility Validator... [paste full legacy-code-cleanup-validator definition]",
  description="Validate legacy code cleanup"
)
```

**Key validations**:
- Legacy code identification
- Cleanup completeness
- Compatibility analysis
- High-risk area check

**Critical areas**: `IConfigurationManager`, `FileGroup`, `MonitoringOrchestrator`, `ViewModelBase`

---

## Typical Workflow

1. Create implementation plan
2. Run `plan-validator` → ✅/⚠️/❌
3. Run `legacy-code-cleanup-validator` → ✅/⚠️/❌
4. Begin implementation

See agent definitions in `.claude/agents/` for full details.
