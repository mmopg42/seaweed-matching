# Template: Requirements Document (01_requirements.md)

> **Purpose**: Define WHAT needs to be achieved before any implementation begins.  
> **Core Question**: "무엇을 달성해야 하나?" (What must we achieve?)

---

## 1. When to Use

This is the **first document** in the Spec Workflow. Create this when:

- Starting a new feature
- Planning architecture changes
- Modifying contracts/APIs
- Any change affecting multiple modules

---

## 2. Key Principles

### Focus on WHAT, Not HOW

| ✅ Good (WHAT) | ❌ Bad (HOW) |
|----------------|--------------|
| "Users can reset their password" | "Add a POST /reset-password endpoint" |
| "System handles 1000 requests/sec" | "Use Redis caching" |
| "Invalid input shows error message" | "Throw ValidationException" |

### Measurable Success Criteria

| ✅ Good (Measurable) | ❌ Bad (Vague) |
|----------------------|----------------|
| "Response time < 200ms" | "System should be fast" |
| "Supports 3 file formats: PDF, DOCX, TXT" | "Supports multiple formats" |
| "Zero data loss on crash" | "System should be reliable" |

### Clear Scope Boundaries

Always define what is OUT of scope to prevent scope creep.

---

## 3. Template

```markdown
---
Task: [Task Name]
Created: [YYYY-MM-DD]
Status: Draft | Approved
Summary: [ONE LINE summary - max 100 characters]
Research Required: Yes | No
---

# [Task Name] - Requirements

## 1. Goal

### 1.1 Primary Goal

[Single sentence: what does success look like?]

### 1.2 Success Criteria

- [ ] [Criterion 1 - must be measurable/verifiable]
- [ ] [Criterion 2 - must be measurable/verifiable]
- [ ] [Criterion 3 - must be measurable/verifiable]

> **Rule**: If you can't verify it, it's not a success criterion.

## 2. Constraints

### 2.1 Technical Constraints

- [Constraint 1: e.g., "Must work with Python 3.9+"]
- [Constraint 2: e.g., "Cannot modify database schema"]
- [Constraint 3: e.g., "Must maintain backward compatibility"]

### 2.2 Business Constraints

- [Constraint 1: e.g., "Must complete within 2 weeks"]
- [Constraint 2: e.g., "Cannot require user re-authentication"]

### 2.3 Non-Goals (Out of Scope)

- [What this task will NOT do - be explicit]
- [Feature X is out of scope because...]
- [We will NOT address Y in this task]

## 3. Questions to Investigate

> Skip this section if Research Required = No

- [ ] Q1: [Question that needs investigation]
- [ ] Q2: [Question that needs investigation]
- [ ] Q3: [Question that needs investigation]

## 4. Assumptions

- [Assumption 1: e.g., "Users have stable internet connection"]
- [Assumption 2: e.g., "Existing auth system will not change"]
- [Assumption 3: e.g., "Database can handle 10x current load"]

## 5. Dependencies

### 5.1 Blocked By

| Dependency | Status | Owner |
|------------|--------|-------|
| [What must be done first] | [Done/In Progress/Blocked] | [Who] |

### 5.2 Blocks

| Dependent Task | Impact if Delayed |
|----------------|-------------------|
| [What depends on this] | [Impact description] |

---

## Approval

- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
- [ ] Scope boundaries are clear
- [ ] All blocking dependencies identified

**Next Step**: [02_research.md | 03_plan.md]
```

---

## 4. Section Guidelines

### 4.1 Primary Goal

Write ONE sentence that completes: "This task is successful when..."

**Examples**:
- ✅ "Users can upload and preview PDF files before submitting"
- ✅ "System automatically retries failed API calls with exponential backoff"
- ❌ "Improve file handling" (too vague)
- ❌ "Add upload feature with preview, validation, compression, and cloud sync" (multiple goals)

### 4.2 Success Criteria

Each criterion must be:
- **Specific**: No ambiguous terms
- **Measurable**: Can be verified with a test
- **Independent**: Each criterion stands alone

**Format**: `[Action] + [Object] + [Condition/Measurement]`

**Examples**:
- ✅ "PDF files up to 10MB upload successfully within 5 seconds"
- ✅ "Invalid file types show error message without page reload"
- ✅ "Upload progress displays percentage updated every 500ms"
- ❌ "Upload works well" (not measurable)
- ❌ "Good user experience" (subjective)

### 4.3 Constraints

**Technical**: System/environment limitations
- Language/framework versions
- Compatibility requirements
- Performance boundaries
- Security requirements

**Business**: Non-technical limitations
- Timeline
- Budget
- Legal/compliance
- User impact tolerance

### 4.4 Non-Goals

Explicitly state what you're NOT doing. This prevents:
- Scope creep during implementation
- Misaligned expectations
- Wasted effort on out-of-scope features

**Format**: "[Feature/Aspect] is out of scope because [reason]"

### 4.5 Questions to Investigate

List unknowns that need research before planning. Set `Research Required: Yes` if this section has items.

**Good questions**:
- "What's the current file size distribution in production?"
- "Does the existing auth system support refresh tokens?"
- "What libraries are available for PDF parsing?"

**Bad questions**:
- "How should we implement this?" (that's for plan/design)
- "What color should the button be?" (too detailed)

### 4.6 Assumptions

State what you're taking for granted. If an assumption is wrong, the plan may need revision.

**Format**: "We assume [condition] because [reason/evidence]"

---

## 5. Common Mistakes

| Mistake | Problem | Fix |
|---------|---------|-----|
| Jumping to solutions | Constrains options prematurely | Focus on WHAT, not HOW |
| Vague success criteria | Can't verify completion | Add measurable conditions |
| Missing non-goals | Scope creep | Explicitly list exclusions |
| Too many goals | Unfocused implementation | Split into multiple specs |
| No assumptions listed | Hidden risks | Document all assumptions |
| Skipping dependencies | Blocked work discovered late | Map dependencies upfront |

---

## 6. Examples

### Example 1: Feature Addition

```markdown
---
Task: pdf_upload_feature
Created: 2024-01-15
Status: Draft
Summary: Enable users to upload and preview PDF files before form submission
Research Required: Yes
---

# PDF Upload Feature - Requirements

## 1. Goal

### 1.1 Primary Goal

Users can upload PDF files and see a preview before submitting the form.

### 1.2 Success Criteria

- [ ] PDF files (≤10MB) upload successfully
- [ ] Upload progress shows percentage
- [ ] Preview displays first page of PDF
- [ ] Invalid files show descriptive error message
- [ ] Upload completes within 5 seconds for 5MB file on 10Mbps connection

## 2. Constraints

### 2.1 Technical Constraints

- Must work in Chrome, Firefox, Safari (latest 2 versions)
- Cannot add backend dependencies larger than 5MB
- Must integrate with existing form validation system

### 2.2 Business Constraints

- Must not require user to install plugins
- Must complete within 1 sprint (2 weeks)

### 2.3 Non-Goals (Out of Scope)

- Multi-file upload (single file only for v1)
- PDF editing or annotation
- File format conversion
- Cloud storage integration

## 3. Questions to Investigate

- [ ] Q1: What PDF library options exist for browser-side preview?
- [ ] Q2: What's the current average file size users attempt to upload?
- [ ] Q3: Does current storage system support chunked uploads?

## 4. Assumptions

- Users have modern browsers with JavaScript enabled
- Server storage has sufficient capacity for uploaded files
- Existing drag-and-drop zone can be extended for PDF preview
```

### Example 2: Performance Improvement

```markdown
---
Task: api_response_optimization
Created: 2024-01-15
Status: Draft
Summary: Reduce API response time from 800ms to under 200ms
Research Required: Yes
---

# API Response Optimization - Requirements

## 1. Goal

### 1.1 Primary Goal

95th percentile API response time is under 200ms.

### 1.2 Success Criteria

- [ ] GET /users endpoint: p95 < 150ms (currently 600ms)
- [ ] GET /orders endpoint: p95 < 200ms (currently 800ms)
- [ ] No increase in error rate
- [ ] No breaking changes to API contract

## 2. Constraints

### 2.1 Technical Constraints

- Cannot change database schema
- Must maintain API backward compatibility
- Cannot add more than $100/month infrastructure cost

### 2.2 Non-Goals (Out of Scope)

- Optimizing write operations (POST/PUT/DELETE)
- Changing API response format
- Database migration or schema changes

## 3. Questions to Investigate

- [ ] Q1: Where is time spent? (DB query vs. serialization vs. network)
- [ ] Q2: What queries are most expensive?
- [ ] Q3: Is caching feasible given data freshness requirements?

## 4. Assumptions

- Current slow performance is due to unoptimized queries (not hardware)
- Read traffic is 10x write traffic (caching will be effective)
- Users can tolerate up to 30 seconds of stale data
```

---

## 7. Checklist Before Approval Request

```markdown
- [ ] Primary goal is ONE clear sentence
- [ ] Each success criterion is measurable
- [ ] Technical constraints are realistic
- [ ] Non-goals explicitly listed
- [ ] Questions flagged if research needed
- [ ] Assumptions documented
- [ ] Dependencies mapped
- [ ] No implementation details (HOW) in this document
```

---

## 8. Approval Request Format

After completing the document, request approval:

```
[문서 전체 내용]

---
이 Requirements 문서를 검토해 주세요.
승인하시면 다음 단계([02_research.md 또는 03_plan.md])로 진행하겠습니다.
수정이 필요하면 말씀해 주세요.
```

---

**Next**: If Research Required = Yes → `02_research.md`, else → `03_plan.md`
