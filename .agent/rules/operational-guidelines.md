---
trigger: always_on
---

# Project Operational Guidelines - Quick Reference

> **Purpose**: Direct AI to the correct detailed document based on current task.  
> **Rule**: Read the relevant detailed document BEFORE starting any work.

---

## 1. Critical Rules (Always Apply)

### 1.1 Anti-Pattern Detection (Code Output)

Before outputting code, check:
- ❌ "Here's the complete/full/entire file..." → Show minimal diff instead
- ❌ Comments like `// ... existing code ...` → Show only changed lines
- ❌ Pasting entire files → Only allowed if user requests OR file is very short (<50 lines)

**Line Limits for EDIT operations:**
| Type | Limit | Action |
|------|-------|--------|
| Soft limit | 80 lines | Preferred maximum |
| Hard limit | 120 lines | Absolute maximum; break into multiple edits if exceeded |

**Always**: Show only the changed block + 2-3 context lines. Never paste entire files unless explicitly requested.

### 1.2 Template Loading Rule (MANDATORY)

> **When writing ANY Spec or Architecture document, you MUST:**
> 1. Open the corresponding template from `docs/templates/` FIRST
> 2. Follow the template structure EXACTLY
> 3. Do NOT guess or improvise document format

This rule is **non-negotiable**. Format consistency enables review, automation, and future AI understanding.

### 1.3 Code Modification Lock (Spec-Lock)

```
IF docs/spec/{task}/01_requirements.md EXISTS
AND docs/spec/{task}/06_tasks.md DOES NOT EXIST
THEN code modification is PROHIBITED for that task
```

### 1.4 Proof-of-Execution Protocol (Honesty Rule)

When reporting test/verification results:

| Situation | Required Response |
|-----------|-------------------|
| Actually executed | Report real output |
| Could NOT execute | Say "I could not execute this" + provide copy-paste command for user |
| Partial execution | Clearly state what ran and what didn't |

**Never claim "tests passed" or "verified" without actual execution.**

### 1.5 File Size Guidelines

| Threshold | Status | Action |
|-----------|--------|--------|
| ≤ 400 LoC | ✅ Good | No action needed |
| 400-600 LoC | ⚠️ Warning | Consider splitting if responsibilities are mixed |
| 600-800 LoC | 🔶 Review | Plan refactoring; document why it's large |
| ≥ 800 LoC | 🔴 Split Required | Must split unless justified exception |

**Exceptions**: Large constant tables, generated code, or data mappings → Move to `constants.*` or `generated/` folder.

### 1.6 Language Policy

- **All Documentation**: English ONLY
- **User Communication**: Korean (한국어)

---

## 2. Decision Tree: What Document to Read?

```
START
  │
  ├─► "New feature / architecture change / API change"
  │     └─► Read: docs/templates/SPEC_WORKFLOW.md
  │         Then: Create docs/spec/{task_name}/ folder
  │
  ├─► "Writing 01_requirements.md"
  │     └─► Read: docs/templates/TEMPLATE_REQUIREMENTS.md
  │
  ├─► "Writing 02_analysis.md"
  │     └─► Read: docs/templates/TEMPLATE_ANALYSIS.md
  │
  ├─► "Writing 03_research.md"
  │     └─► Read: docs/templates/TEMPLATE_RESEARCH.md
  │
  ├─► "Writing 04_plan.md"
  │     └─► Read: docs/templates/TEMPLATE_PLAN.md
  │
  ├─► "Writing 05_design.md"
  │     └─► Read: docs/templates/TEMPLATE_DESIGN.md
  │
  ├─► "Writing 06_tasks.md"
  │     └─► Read: docs/templates/TEMPLATE_TASKS.md
  │
  ├─► "Writing 07_report.md"
  │     └─► Read: docs/templates/TEMPLATE_REPORT.md
  │
  ├─► "Creating/updating architecture docs"
  │     └─► Read: docs/templates/ARCHITECTURE_DOCS.md
  │
  ├─► "Modifying existing code (Direct Edit)"
  │     └─► Do Pre-flight Checks (Section 4)
  │         Then: Read relevant docs/architecture/*.md
  │
  └─► "Small bug fix (1-2 lines, no contract change)"
        └─► No spec needed. Do Pre-flight Checks (Section 4).
```

---

## 3. Spec Workflow

### 3.1 When is Spec Required?

| Situation | Spec Required? |
|-----------|----------------|
| New feature development | ✅ YES |
| Architecture changes | ✅ YES |
| Contract/API changes | ✅ YES |
| Changes affecting multiple modules | ✅ YES |
| Small bug fix (1-2 lines) | ❌ NO |
| Documentation typo fix | ❌ NO |
| Pure refactoring (no behavior change) | ❌ NO |

### 3.2 Spec Document Sequence

```
01_requirements → 02_analysis → [03_research] → 04_plan → 05_design → 06_tasks
                   (MANDATORY)    (optional)                               │
                                                                           ▼
                                                              [CODE MODIFICATION UNLOCKED]
                                                                           │
                                                                           ▼
                                                                     07_report
```

### 3.3 Approval Rules

**Default**: User approval required between each document.

**Exception**: User may waive per-step approvals. If waived, proceed through spec docs in one pass, then await final approval before implementation.

---

## 4. Direct Edit: Pre-flight Checks (MANDATORY)

Before ANY code modification (including small fixes), complete these 3 checks:

```markdown
### Pre-flight Checklist
1. [ ] **Architecture docs exist?** → Check `docs/architecture/` for relevant modules
2. [ ] **Spec-Lock clear?** → Verify no blocking spec (01 exists but 06 doesn't)
3. [ ] **Impact scope identified?** → List affected files/modules (1 line minimum)
```

If any check fails, resolve before proceeding.

---

## 5. Output Contract (When Changes Occur)

> **Trigger**: This structure is required ONLY when code or documentation changes are made.  
> For Q&A, explanations, or no-change responses, use natural conversational format.

```markdown
## Summary
[Brief description in Korean]

## 1. Pre-flight Results
- Architecture docs: [Found/Not found]
- Spec-Lock: [Clear/Blocked]
- Impact scope: [List of files]

## 2. Code Changes
[Minimal diff - changed block + context lines only]

## 3. Documentation Updates
[New or updated architecture docs, or "None required"]

## 4. Verification
[Actual test output OR "Could not execute - run: `command here`"]

## 5. DoD Checklist
- [ ] Impact zones identified
- [ ] Architecture docs updated (if contracts changed)
- [ ] Glossary updated (if new terms introduced)
- [ ] Tests pass (or verification command provided)
```

---

## 6. Document Locations

### Templates (Read before writing docs)
```
docs/templates/
├── SPEC_WORKFLOW.md           # When & how to use spec system
├── ARCHITECTURE_DOCS.md       # Architecture doc guidelines
├── TEMPLATE_REQUIREMENTS.md   # 01_requirements.md template
├── TEMPLATE_ANALYSIS.md       # 02_analysis.md template (MANDATORY)
├── TEMPLATE_RESEARCH.md       # 03_research.md template
├── TEMPLATE_PLAN.md           # 04_plan.md template
├── TEMPLATE_DESIGN.md         # 05_design.md template
├── TEMPLATE_TASKS.md          # 06_tasks.md template
└── TEMPLATE_REPORT.md         # 07_report.md template
```

### Project Documentation
```
docs/
├── architecture/              # System state documentation
│   ├── README.md             # Index (REQUIRED)
│   ├── glossary.md           # Official naming definitions (REQUIRED)
│   └── *.md                  # Feature/module docs
│
└── spec/                     # Task-specific specs
    └── {task_name}/          # One folder per task
        ├── 01_requirements.md
        ├── 02_analysis.md    # MANDATORY
        ├── 03_research.md    # Optional
        ├── 04_plan.md
        ├── 05_design.md
        ├── 06_tasks.md
        └── 07_report.md
```

---

## 7. Quick Reference

### Before Modifying Code
1. Pre-flight checks (Section 4)
2. Read relevant `docs/architecture/*.md`
3. Run VERIFY commands if present in architecture docs

### After Modifying Code
1. Run tests (or provide command if cannot execute)
2. Update architecture docs if contracts changed
3. Complete DoD checklist

### If Unsure What To Do
1. Check if spec folder exists for current task
2. If yes → read spec docs in order
3. If no → check if spec is needed (Section 3.1)
4. Always do pre-flight checks before any code change

### If Documentation Seems Outdated
1. Run VERIFY commands in the document
2. If mismatch found → update docs FIRST
3. Then proceed with code changes

---

**For detailed instructions, read the specific template from `docs/templates/` based on your current task.**

