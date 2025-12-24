# Architecture Documentation Guide

> **Purpose**: Enable safe future modifications by documenting contracts, dependencies, and impact zones.  
> **Core Principle**: Documentation is the Source of Truth—code must follow it, not the other way around.

---

## 1. Why Architecture Documents Exist

### The Problem They Solve

```
AI adds feature A
  → Modifies variable B without knowing context
  → Variable B is used in 10+ places
  → Errors cascade everywhere
```

### The Solution

```
AI reads architecture doc BEFORE modifying
  → Learns "Variable B is used in X, Y, Z locations"
  → Either: Change all locations at once
  → Or: Keep B unchanged, adapt new feature around it
```

### Goals

| Goal | Description |
|------|-------------|
| **System State Visibility** | Any AI can quickly understand how the current system works |
| **Downstream Protection** | Prevent breaking existing functionality when adding new features |
| **Source of Truth** | Documents define contracts—code must follow |
| **Change Safety** | Make impact zones visible BEFORE modification, not discovered AFTER errors |

---

## 2. Document Types

### 2.1 Required Documents

| Document | Purpose |
|----------|---------|
| `README.md` | Index of all architecture documents |
| `glossary.md` | Official naming definitions for variables, functions, config keys |

### 2.2 Optional Documents (Create as Needed)

| Pattern | When to Use | Example |
|---------|-------------|---------|
| `feature_{name}.md` | Document a user-facing feature | `feature_authentication.md` |
| `module_{name}.md` | Document a code module/service | `module_config_loader.md` |
| `impact_{name}.md` | Document cross-cutting concerns | `impact_database_schema.md` |

---

## 3. When to Create/Update

### Create New Document When:

- New feature or module added
- New cross-cutting concern identified
- Existing functionality not yet documented

### Update Existing Document When:

| Trigger | Action |
|---------|--------|
| File/function/class **renamed** | Update Impact + Glossary |
| Config key **added/removed/renamed** | Update Impact + Glossary |
| Input/output **schema changed** | Update relevant module/feature doc |
| Exception/error handling **changed** | Update relevant doc |
| New **dependency** added | Update Dependencies section |
| **Public API** signature changed | Update Contracts + Impact |
| **New term/concept** introduced | Add to Glossary FIRST |

### No Update Needed For:

- Pure formatting changes (linting, whitespace)
- Comment-only changes
- Log message wording changes

---

## 4. VERIFY Commands (Critical)

> **WARNING**: Outdated documentation is worse than no documentation.  
> If AI trusts old docs that don't match current code, it causes MORE errors.

### What Are VERIFY Commands?

Every `Dependents` and `Impact/Touchpoints` section MUST include search commands that validate the documentation matches current code.

### Format

```markdown
## Dependents
<!-- VERIFY: grep -rn "FunctionName" src/ -->
<!-- VERIFY: grep -rn "ClassName" src/ -->

- **Used By**: `module_a.py`, `module_b.py`
```

### Search Command Examples

| Platform | Command |
|----------|---------|
| Unix/macOS | `grep -rn "pattern" src/` |
| Windows PowerShell | `Select-String -Path "src\*" -Pattern "pattern" -Recurse` |
| Cross-platform | `rg "pattern" src/` (ripgrep) |
| IDE | Use "Find in Files" feature |

### Usage Rule

**AI MUST run VERIFY commands BEFORE modifying code** to confirm documentation matches current state.

If mismatch found:
1. Update documentation FIRST
2. Then proceed with code changes

---

## 5. Glossary: Single Source of Truth

### Purpose

Prevent naming conflicts across documents by defining official names for:
- Variables and constants
- Functions and methods
- Classes and types
- Configuration keys
- Environment variables

### Rule

> All architecture documents MUST use terms exactly as defined in `glossary.md`.  
> If a term doesn't exist in glossary, ADD IT FIRST before using in any document.

### Glossary Template

```markdown
# Glossary

> All architecture documents MUST use terms exactly as defined here.

## Variables & Constants

| Official Name | Type | Description | Defined In |
|---------------|------|-------------|------------|
| `MAX_RETRIES` | int | Maximum retry attempts | `config.py` |

## Functions & Methods

| Official Name | Signature | Description | Defined In |
|---------------|-----------|-------------|------------|
| `load_config` | `(path: str) -> Config` | Load configuration from file | `config_loader.py` |

## Classes / Types

| Official Name | Description | Defined In |
|---------------|-------------|------------|
| `Config` | Configuration data container | `models/config.py` |

## Configuration Keys

| Official Name | Type | Default | Description |
|---------------|------|---------|-------------|
| `api.timeout` | int | 30 | API request timeout in seconds |

## Environment Variables

| Official Name | Description | Required |
|---------------|-------------|----------|
| `DATABASE_URL` | Database connection string | Yes |

## Deprecated Terms

| Deprecated | Use Instead | Reason | Date |
|------------|-------------|--------|------|
| `get_config` | `load_config` | Naming standardization | 2024-01-15 |
```

---

## 6. Architecture Document Template

```markdown
---
Owner: [Team or Person]
Last Updated: [YYYY-MM-DD]
Code Ref: [File path or commit hash]
---

# [Feature/Module Name]

## Overview

[2-3 sentences describing what this component does and why it exists]

## Key Components

| Component | Type | Location | Purpose |
|-----------|------|----------|---------|
| `ComponentName` | Class/Function | `path/to/file` | [Brief purpose] |

## Contracts

### Inputs

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `param1` | string | Yes | [Description] |

### Outputs

| Field | Type | Description |
|-------|------|-------------|
| `result` | object | [Description] |

### Errors / Exceptions

| Exception | Condition | Handling |
|-----------|-----------|----------|
| `InvalidInputError` | When input validation fails | Caller should catch and display message |

### Side Effects

- [File system changes, database writes, network calls, or "None"]

## Logic Flow

```
1. [First step]
2. [Second step]
3. [Third step]
```

## Dependencies

### Internal

| Module | Purpose |
|--------|---------|
| `module_name` | [Why needed] |

### External

| Library | Version | Purpose |
|---------|---------|---------|
| `library_name` | ^1.0.0 | [Why needed] |

### Configuration

| Key | Type | Default | Purpose |
|-----|------|---------|---------|
| `config.key` | type | value | [Why needed] |

## Dependents

<!-- VERIFY: grep -rn "ComponentName" src/ -->
<!-- VERIFY: grep -rn "function_name" src/ -->

| Module | How It Uses This |
|--------|------------------|
| `dependent_module` | [Description of usage] |

### Shared State

| Variable | Accessed By | Notes |
|----------|-------------|-------|
| `shared_var` | `module_a`, `module_b` | [Concurrency notes if any] |

## Impact / Touchpoints

<!-- VERIFY: grep -rn "related_pattern" src/ -->

When modifying this component, check:

| File | Reason |
|------|--------|
| `file_1.py` | [Why it might be affected] |
| `file_2.py` | [Why it might be affected] |

## Related Docs

| Document | Relationship |
|----------|--------------|
| `other_doc.md` | [How related] |

## Changelog

| Date | Change |
|------|--------|
| YYYY-MM-DD | Initial creation |
```

---

## 7. README Index Template

```markdown
# Architecture Documentation Index

> Last Updated: [YYYY-MM-DD]

## Overview

[1-2 sentences about the project]

## Document Index

### Core Documents

| Document | Description | Last Updated |
|----------|-------------|--------------|
| [glossary.md](glossary.md) | Official naming definitions | YYYY-MM-DD |

### Features

| Document | Description | Last Updated |
|----------|-------------|--------------|
| [feature_xxx.md](feature_xxx.md) | [Description] | YYYY-MM-DD |

### Modules

| Document | Description | Last Updated |
|----------|-------------|--------------|
| [module_xxx.md](module_xxx.md) | [Description] | YYYY-MM-DD |

### Cross-Cutting Concerns

| Document | Description | Last Updated |
|----------|-------------|--------------|
| [impact_xxx.md](impact_xxx.md) | [Description] | YYYY-MM-DD |

## Quick Links

- **New to codebase?** Start with [glossary.md](glossary.md)
- **Adding a feature?** Check Impact documents first
- **Debugging?** Find the relevant module document
```

---

## 8. Minimum Content Rules

| Section | Minimum Requirement |
|---------|---------------------|
| Overview | 2 sentences |
| Contracts | All 4 subsections (Inputs, Outputs, Errors, Side Effects) |
| Logic Flow | 3 steps |
| Dependencies | At least "None" if empty |
| Dependents | At least 1 VERIFY command |
| Impact/Touchpoints | 3 items (or justified "None") |
| Related Docs | 2 links (or "None - standalone") |

---

## 9. Workflow

### Creating New Architecture Document

```
1. CONSISTENCY CHECK
   └── Search existing docs for related terms
   └── Check glossary.md for existing definitions

2. UPDATE GLOSSARY
   └── Add any new terms BEFORE using them

3. CREATE DOCUMENT
   └── Use template from Section 6
   └── Fill all required sections

4. ADD VERIFY COMMANDS
   └── Include search commands for Dependents and Impact

5. UPDATE INDEX
   └── Add entry to docs/architecture/README.md
```

### Updating Existing Architecture Document

```
1. RUN VERIFY COMMANDS
   └── Confirm documentation matches current code

2. MAKE UPDATES
   └── Update relevant sections

3. UPDATE CHANGELOG
   └── Add dated entry describing changes

4. UPDATE GLOSSARY
   └── If any terms changed

5. UPDATE INDEX
   └── If document scope changed significantly
```

---

## 10. Common Mistakes to Avoid

| Mistake | Why It's Bad | Correct Approach |
|---------|--------------|------------------|
| No VERIFY commands | Can't validate if docs match code | Always add search commands |
| Using undefined terms | Causes confusion, naming conflicts | Add to glossary first |
| Skipping Impact section | Future changes may break things | List all touchpoints |
| Outdated changelog | Can't track document evolution | Update on every change |
| Missing error documentation | Callers don't know what to catch | Document all exceptions |
| "See code" instead of documenting | Defeats purpose of documentation | Write explicit contracts |

---

## 11. Quick Reference

### File Locations

```
docs/architecture/
├── README.md          # Index (REQUIRED)
├── glossary.md        # Naming definitions (REQUIRED)
├── feature_*.md       # Feature documentation
├── module_*.md        # Module documentation
└── impact_*.md        # Cross-cutting concerns
```

### Before Modifying Any Code

1. Find relevant architecture doc
2. Run VERIFY commands
3. Read Dependents and Impact sections
4. Identify all affected locations
5. Plan changes to cover all impact zones

### After Modifying Code

1. Update architecture doc if contracts changed
2. Update glossary if terms changed
3. Update changelog
4. Update README index if needed

---

**Remember**: Architecture documents protect future modifications. Invest time now to save debugging time later.
