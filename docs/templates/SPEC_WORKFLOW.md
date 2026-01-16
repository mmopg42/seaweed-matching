# Spec Document System

> **Purpose**: Guide structured planning before implementation.  
> **Core Principle**: Planning and design MUST be complete before code modification begins.

---

## 1. What is the Spec System?

The Spec System is a **7-document workflow** that transforms a vague idea into a concrete implementation plan. It ensures:

- Requirements are understood before coding
- Architecture decisions are explicit and reviewed
- Implementation has a clear roadmap
- Changes are documented for future reference

---

## 2. When to Use Spec Workflow

### Required (YES)

| Situation | Why |
|-----------|-----|
| New feature development | Needs design before implementation |
| Architecture changes | Affects system structure |
| Contract/API changes | Impacts other modules |
| Changes affecting multiple modules | Coordination needed |

### Not Required (NO)

| Situation | Why |
|-----------|-----|
| Small bug fix (1-2 lines) | Scope is trivial |
| Documentation typo fix | No code impact |
| Pure refactoring (no behavior change) | Contract unchanged |
| Log message changes | No functional impact |

**When in doubt**: If the change could break something else, use Spec Workflow.

---

## 3. Document Sequence

```
┌─────────────────────────────────────────────────────────────────┐
│  SPEC WORKFLOW                                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  01_requirements.md  ───► [Approval] ───►                       │
│  02_analysis.md      ───► [Approval] ───►  (MANDATORY)          │
│  03_research.md      ───► [Approval] ───►  (OPTIONAL)           │
│  04_plan.md          ───► [Approval] ───►                       │
│  05_design.md        ───► [Approval] ───►                       │
│  06_tasks.md         ───► [Approval] ───►                       │
│                                                                 │
│  ════════════════════════════════════════                       │
│  CODE MODIFICATION UNLOCKED                                     │
│  ════════════════════════════════════════                       │
│                                                                 │
│  [Implementation]    ───►                                       │
│  07_report.md        ───► COMPLETE (All docs frozen)            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. Document Overview

| # | Document | Core Question | Output |
|---|----------|---------------|--------|
| 01 | requirements | "What must we achieve?" | Goals, success criteria, constraints |
| 02 | analysis | "How does it work now?" | Code audit, legacy logic, constraints |
| 03 | research | "What can we use?" | External tech, libraries, algorithms |
| 04 | plan | "What structure will we build?" | Components, interfaces, data flow |
| 05 | design | "How will the code work?" | Pseudo-code, error handling, state |
| 06 | tasks | "In what order do we implement?" | Ordered checklist with verification |
| 07 | report | "What did we actually do?" | Final record of implementation |

### Abstraction Levels

```
01_requirements  ─────►  HIGH (Goal-level)
       │
02_analysis      ─────►  Audit (Internal Reality)
       │
03_research      ─────►  Investigation (External Possibility)
       │
04_plan          ─────►  Architecture (WHAT components, HOW they connect)
       │
05_design        ─────►  Implementation (HOW code works internally)
       │
06_tasks         ─────►  Execution (WHAT order to build)
       │
07_report        ─────►  Record (WHAT was done)
```

---

## 5. Key Rules

### 5.1 Code Modification Lock (Spec-Lock)

```
IF   docs/spec/{task}/01_requirements.md EXISTS
AND  docs/spec/{task}/06_tasks.md DOES NOT EXIST
THEN code modification is PROHIBITED for that task
```

| Folder State | Code Modification |
|--------------|-------------------|
| No `01_requirements.md` | ✅ Allowed (no spec in progress) |
| `01_requirements.md` exists, no `06_tasks.md` | ❌ **PROHIBITED** |
| `06_tasks.md` exists | ✅ Allowed (planning complete) |
| `07_report.md` exists | ✅ Allowed (spec frozen) |

### 5.2 Approval Gates

**Default behavior**: User approval required after each document.

**Waiver option**: User may say "skip approvals" or "proceed without approval". If waived:
- Complete all spec documents in one pass
- Await final approval before implementation
- Still create all required documents

### 5.3 Sequential Execution

- Complete one document at a time
- Each document depends on previous ones
- Do not skip ahead (02_analysis is MANDATORY, 03_research is OPTIONAL)

### 5.4 Frozen Rule

> Once `07_report.md` is marked Complete, ALL spec documents are **FROZEN**.

- No modifications to any document in that spec folder
- New work requires a **new spec task folder**
- This preserves historical record of decisions

---

## 6. Plan vs Design: Understanding the Difference

This is a common confusion point. Here's how to distinguish:

| Aspect | 04_plan.md | 05_design.md |
|--------|------------|--------------|
| **Abstraction** | High-level | Low-level |
| **Focus** | WHAT to build | HOW it works internally |
| **Diagrams** | Component boxes, data flow | Sequence diagrams, state machines |
| **Interface** | Signatures only | + Preconditions, postconditions |
| **Logic** | "A calls B" | Pseudo-code with steps |
| **Errors** | "Retry on failure" | "Retry 3x, 1s interval, then fail" |
| **Concurrency** | "Must be thread-safe" | Lock patterns, atomic operations |

### Example

**In 04_plan.md**:
```
ConfigLoader reads YAML files and provides settings to other modules.
Interface: load(path: string) -> Config
Must handle missing files gracefully.
```

**In 05_design.md**:
```
function load(path):
    // Step 1: Validate path
    if path is null or empty:
        raise InvalidArgumentError("path required")
    
    // Step 2: Check file exists
    if not file_exists(path):
        log_warning("Config not found, using defaults")
        return DEFAULT_CONFIG
    
    // Step 3: Parse YAML
    try:
        content = read_file(path)
        config = yaml_parse(content)
    catch YamlParseError as e:
        raise ConfigError("Invalid YAML: " + e.message)
    
    // Step 4: Validate schema
    validate_config_schema(config)  // raises on invalid
    
    return config
```

---

## 7. Optional Document: Research (03_research.md)

### When to Skip

- User explicitly requests to skip
- Task is straightforward with no unknowns
- All questions from requirements can be answered immediately
- No existing code needs investigation

### When to Include

- Unknowns need investigation
- Existing codebase needs analysis
- Multiple approaches need comparison
- External constraints need verification

### Skipping Syntax

User says any of:
- "리서치 생략"
- "Skip research"
- "No investigation needed"
- "Proceed to plan"

---

## 8. Workflow Checklist

### Starting a New Spec

```markdown
1. [ ] Create folder: `docs/spec/{task_name}/`
2. [ ] Read template: `docs/templates/TEMPLATE_REQUIREMENTS.md`
3. [ ] Write `01_requirements.md`
4. [ ] Request user approval
5. [ ] Proceed through remaining documents
```

### Before Each Document

```markdown
1. [ ] Previous document approved?
2. [ ] Read corresponding template from `docs/templates/`
3. [ ] Follow template structure exactly
```

### After Implementation

```markdown
1. [ ] All tasks in `06_tasks.md` checked off
2. [ ] Tests pass (or verification commands provided)
3. [ ] Write `07_report.md`
4. [ ] Update `docs/architecture/` if needed
```

---

## 9. Folder Structure

```
docs/spec/{task_name}/
├── 01_requirements.md    # Goals, constraints, success criteria
├── 02_analysis.md        # Code coverage, audit, legacy logic (MANDATORY)
├── 03_research.md        # Investigation findings (OPTIONAL)
├── 04_plan.md            # Architecture, components, interfaces
├── 05_design.md          # Detailed logic, pseudo-code, error handling
├── 06_tasks.md           # Implementation checklist
└── 07_report.md          # Final implementation record
```

### Naming Convention

- Task name: lowercase with underscores (e.g., `user_authentication`, `api_rate_limiting`)
- Keep names concise but descriptive
- Avoid version numbers in folder names

---

## 10. Quick Reference

### Document Templates Location

```
docs/templates/
├── TEMPLATE_REQUIREMENTS.md
├── TEMPLATE_ANALYSIS.md
├── TEMPLATE_RESEARCH.md
├── TEMPLATE_PLAN.md
├── TEMPLATE_DESIGN.md
├── TEMPLATE_TASKS.md
└── TEMPLATE_REPORT.md
```

### Approval Request Format (Korean)

```
[문서 내용]

---
이 문서를 검토해 주세요. 
승인하시면 다음 단계([다음 문서명])로 진행하겠습니다.
수정이 필요하면 말씀해 주세요.
```

### Common Commands

| Action | What to Do |
|--------|------------|
| Start new feature | Create spec folder → Begin with 01_requirements |
| Check if spec-locked | `ls docs/spec/{task}/06_tasks.md` |
| Skip research | User says "리서치 생략" or "skip research" |
| Skip approvals | User says "승인 생략" or "skip approvals" |

---

**Next**: Read the specific template for the document you need to write.
