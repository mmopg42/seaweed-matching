# Template: Analysis Document (02_analysis.md)

> **Purpose**: Audit the existing codebase to establish "Ground Truth" before planning.
> **Core Question**: "현재 코드는 실제로 어떻게 동작하는가?" (How does the code actually work?)
> **Rule**: NO ASSUMPTIONS. meaningful "Analysis" requires reading the code.

---

## 1. When to Use

**ALWAYS REQUIRED** for any task involving code changes.

Exceptions (skip only if):
- Task is purely documentation update
- Creating a completely new isolated project (no existing code to audit)

---

## 2. Key Principles

### 2.1 Audit vs Research
- **Analysis (This Doc)**: Internal. "What do we HAVE?" (Facts, Constraints, Legacy Logic)
- **Research (Next Doc)**: External. "What could we USE?" (Libraries, Algorithms, Patterns)

### 2.2 Anti-Hallucination Protocol
- ❌ "I assume `LoginService` handles hashing."
- ✅ "`LoginService.cs:42` calls `BCrypt.HashPassword`."
- ❌ "The system likely uses a queue."
- ✅ "`MessageQueue.cs` exists but is not referenced in `.csproj`."

---

## 3. Template

```markdown
---
Task: [Task Name]
Created: [YYYY-MM-DD]
Status: Draft | Approved
Depends On: 01_requirements.md
---

# [Task Name] - Code Analysis

## 1. Analysis Summary

| Target | Status | Found In (File:Line) |
|--------|--------|----------------------|
| [Feature/Logic 1] | [Exists / Missing / Partial] | `path/to/file:L10` |
| [Feature/Logic 2] | [Exists / Missing / Partial] | `path/to/file:L25` |

## 2. Codebase Audit

### 2.1 Current Implementation (As-Is)
> Describe what the code DOES, not what it *should* do.

**[Component Name] Logic**:
- Entry Point: `[File.cs:Method]`
- Data Flow: [A] -> [B] -> [C]
- Hardcoded Values: [List any magic numbers/strings found]
- Dependencies: [List tight couplings]

**Evidence**:
```csharp
// Copy specific lines that prove the logic (max 10 lines)
var timeout = 5000; // Hardcoded timeout found in ConnectionManager.cs
```

### 2.2 Spaghetti/Legacy Detection
> Identify technical debt that might trap us.

| Type | Location | Description | Risk Level |
|------|----------|-------------|------------|
| [Duplicate/Dead Code/SSOT Violation] | `[file]` | [Description] | [High/Med] |

### 2.3 Data Structure Analysis
> Verify the actual models/schemas in use.

- **Model**: `[ModelName]` in `[File.cs]`
- **Fields**: [List key fields]
- **Difference from Requirements**: [Matches / Needs Update / Conflict]

## 3. Impact Analysis (Pre-Flight)

### 3.1 Affected Files
> List every file that will likely be modified.

- `[File 1]` (Function: [Impact])
- `[File 2]` (Function: [Impact])

### 3.2 Breaking Changes
- [ ] API Signature Change: `[Method]`
- [ ] Database Schema Change: `[Table]`
- [ ] Config File Format Change: `[Key]`

## 4. Unresolved Mysteries
> "I looked but couldn't find..." - Be honest.

- [ ] Where is variable X initialized?
- [ ] Why is there a `TODO` on line 50?

## Approval
- [ ] All "Found In" links verified
- [ ] No assumptions ("likely", "probably") in the text
- [ ] Spaghetti code identified
```

---

## 4. Tips for Filling This Out

- **Use Grep**: Don't guess. Run `grep "term" . -r` and record results.
- **Trace Execution**: Follow the method calls 2-3 levels deep.
- **Check Config**: Look at `appsettings.json` or `Constants.cs` for hidden rules.
- **Be Cynical**: Assume the variable naming is misleading until proven otherwise.
