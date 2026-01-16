# Template: Research Document (03_research.md)

> **Purpose**: Document investigation findings before making architecture decisions.  
> **Core Question**: "무엇을 알아냈나?" (What did we discover?)  
> **Note**: This document is OPTIONAL. Skip if no investigation needed.

---

## 1. When to Use

Create this document when:

- Questions were listed in `01_requirements.md`
- Unknowns need investigation before planning
- Multiple approaches need comparison
- Existing codebase needs analysis
- External constraints need verification

### When to Skip

- User explicitly says "리서치 생략" or "skip research"
- All questions from requirements can be answered immediately
- Task is straightforward with no unknowns
- Scope is small and well-understood

---

## 2. Key Principles

### Evidence-Based Conclusions

| ✅ Good | ❌ Bad |
|---------|--------|
| "Library X is 3x faster (benchmark link)" | "Library X seems faster" |
| "Found 47 usages of this function" | "This function is used in many places" |
| "API docs confirm rate limit is 100/min" | "API probably has rate limits" |

### Answer the Questions

Every question from `01_requirements.md` must have:
- Clear answer OR
- "Cannot determine" with explanation

### Actionable Recommendations

End with concrete recommendations that feed into `03_plan.md`.

---

## 3. Template

```markdown
---
Task: [Task Name]
Created: [YYYY-MM-DD]
Status: Draft | Approved
Depends On: 01_requirements.md, 02_analysis.md
---

# [Task Name] - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: [Question] | [Brief answer] | High/Medium/Low |
| Q2: [Question] | [Brief answer] | High/Medium/Low |
| Q3: [Question] | [Brief answer] | High/Medium/Low |

## 2. Detailed Findings

### 2.1 Q1: [Question from requirements]

**Method**: [How did you investigate?]

**Findings**:
- [Finding 1]
- [Finding 2]
- [Finding 3]

**Evidence**:
- [Link, code reference, or data source]

**Conclusion**: [Clear answer to the question]

---

### 2.2 Q2: [Question from requirements]

**Method**: [How did you investigate?]

**Findings**:
- [Finding 1]
- [Finding 2]

**Evidence**:
- [Link, code reference, or data source]

**Conclusion**: [Clear answer to the question]

---

### 2.3 Q3: [Question from requirements]

**Method**: [How did you investigate?]

**Findings**:
- [Finding 1]
- [Finding 2]

**Evidence**:
- [Link, code reference, or data source]

**Conclusion**: [Clear answer to the question]

---

## 3. Code Analysis

### 3.1 Relevant Existing Code

| File | Component | Relevance | Notes |
|------|-----------|-----------|-------|
| `path/to/file` | `ClassName/function` | [Why relevant] | [Key observations] |

### 3.2 Glossary Check

**Existing Terms to Reuse**:
- `term1`: [definition from glossary]
- `term2`: [definition from glossary]

**New Terms Needed**:
- `new_term1`: [proposed definition]
- `new_term2`: [proposed definition]

### 3.3 Impact Analysis

| Existing Component | Potential Impact | Risk Level |
|--------------------|------------------|------------|
| [Component] | [How it might be affected] | High/Medium/Low |

## 4. Options Analysis

> Include this section if multiple approaches were considered.

### Option A: [Name]

**Description**: [Brief description]

**Pros**:
- [Pro 1]
- [Pro 2]

**Cons**:
- [Con 1]
- [Con 2]

**Effort Estimate**: [Small/Medium/Large]

---

### Option B: [Name]

**Description**: [Brief description]

**Pros**:
- [Pro 1]
- [Pro 2]

**Cons**:
- [Con 1]
- [Con 2]

**Effort Estimate**: [Small/Medium/Large]

---

### Comparison Matrix

| Criteria | Weight | Option A | Option B |
|----------|--------|----------|----------|
| [Criterion 1] | [1-5] | [Score 1-5] | [Score 1-5] |
| [Criterion 2] | [1-5] | [Score 1-5] | [Score 1-5] |
| **Weighted Total** | | [Total] | [Total] |

## 5. Recommendations

### Primary Recommendation

[Clear statement of recommended approach and why]

### Secondary Recommendations

- [Additional recommendation 1]
- [Additional recommendation 2]

### Risks to Address in Planning

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| [Risk 1] | High/Med/Low | High/Med/Low | [How to mitigate] |

## 6. Unanswered Questions

> List anything that couldn't be determined and must be addressed later.

| Question | Why Unanswered | When to Resolve |
|----------|----------------|-----------------|
| [Question] | [Reason] | [During planning/implementation] |

## 7. References

- [Link 1: Description]
- [Link 2: Description]
- [Code reference: `path/to/file:line`]

---

## Approval

- [ ] All questions from requirements addressed
- [ ] Evidence provided for conclusions
- [ ] Recommendations are actionable
- [ ] Risks identified

**Next Step**: 04_plan.md
```

---

## 4. Section Guidelines

### 4.1 Investigation Summary

Quick reference table for all questions. Allows reviewer to see status at a glance.

**Confidence Levels**:
- **High**: Direct evidence, verified data
- **Medium**: Indirect evidence, reasonable inference
- **Low**: Best guess, needs verification during implementation

### 4.2 Detailed Findings

For each question, document:

| Element | Purpose |
|---------|---------|
| **Method** | How you investigated (code search, API testing, documentation review) |
| **Findings** | What you discovered (facts, not opinions) |
| **Evidence** | Proof (links, code refs, test results) |
| **Conclusion** | Clear answer to the question |

### 4.3 Code Analysis

Essential for understanding existing system:

- **Relevant Code**: What existing code matters for this task
- **Glossary Check**: Ensure naming consistency
- **Impact Analysis**: What might break

### 4.4 Options Analysis

Use when multiple valid approaches exist:

1. Describe each option objectively
2. List pros/cons without bias
3. Use comparison matrix for complex decisions
4. Make clear recommendation with reasoning

### 4.5 Recommendations

Must be:
- **Specific**: Not "consider using caching" but "use Redis with 5-minute TTL"
- **Justified**: Linked to findings
- **Actionable**: Can be directly used in planning

---

## 5. Common Mistakes

| Mistake | Problem | Fix |
|---------|---------|-----|
| No evidence | Conclusions seem like guesses | Add links, code refs, data |
| Unanswered questions | Blocking issues for planning | Mark as unanswered with resolution plan |
| Biased options analysis | May miss better approach | Present all options objectively first |
| Too much detail | Hard to extract key points | Lead with summary, detail below |
| Missing code analysis | May conflict with existing code | Always check existing codebase |
| No glossary check | Naming conflicts later | Check glossary before using terms |

---

## 6. Example

```markdown
---
Task: pdf_upload_feature
Created: 2024-01-15
Status: Draft
Depends On: 01_requirements.md
---

# PDF Upload Feature - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: PDF library options? | PDF.js recommended | High |
| Q2: Current file sizes? | 90% under 2MB | High |
| Q3: Chunked upload support? | Yes, S3 multipart | High |

## 2. Detailed Findings

### 2.1 Q1: What PDF library options exist for browser-side preview?

**Method**: NPM search, GitHub comparison, bundle size analysis

**Findings**:
- PDF.js (Mozilla): 500KB, full-featured, active maintenance
- pdfmake: 300KB, primarily for generation not viewing
- react-pdf: Wrapper around PDF.js, adds 50KB

**Evidence**:
- Bundle comparison: https://bundlephobia.com/...
- PDF.js GitHub: 40K stars, updated 2 days ago
- Browser support matrix verified in docs

**Conclusion**: PDF.js is the best choice - industry standard, well-maintained, reasonable size.

---

### 2.2 Q2: What's the current average file size users attempt to upload?

**Method**: Database query on last 30 days of uploads

**Findings**:
- Average: 1.2MB
- Median: 800KB
- 90th percentile: 2.1MB
- 99th percentile: 8.5MB
- Max attempted: 47MB (failed)

**Evidence**:
```sql
SELECT percentile_cont(0.9) WITHIN GROUP (ORDER BY file_size)
FROM uploads WHERE created_at > NOW() - INTERVAL '30 days';
```

**Conclusion**: 10MB limit is appropriate. 99% of users won't be affected.

---

### 2.3 Q3: Does current storage system support chunked uploads?

**Method**: AWS S3 documentation review, existing code analysis

**Findings**:
- S3 multipart upload supported for files > 5MB
- Existing `StorageService` has `uploadMultipart()` method (unused)
- Current implementation uses single PUT for all files

**Evidence**:
- Code ref: `src/services/storage.py:145` - `uploadMultipart()`
- AWS docs: https://docs.aws.amazon.com/...

**Conclusion**: Yes, infrastructure exists. Need to wire it to frontend.

---

## 3. Code Analysis

### 3.1 Relevant Existing Code

| File | Component | Relevance | Notes |
|------|-----------|-----------|-------|
| `src/services/storage.py` | `StorageService` | Upload handling | Has unused multipart method |
| `src/components/FileUpload.tsx` | `FileUpload` | UI component | Needs PDF preview addition |
| `src/validators/file.py` | `validate_file` | Validation | Add PDF MIME type |

### 3.2 Glossary Check

**Existing Terms**:
- `StorageService`: File storage abstraction
- `FileUpload`: React upload component

**New Terms Needed**:
- `PDFPreview`: New component for preview display
- `ALLOWED_PDF_TYPES`: Constant for valid MIME types

## 4. Options Analysis

### Option A: Client-side preview (PDF.js)

**Pros**:
- No server load for preview
- Instant preview after selection
- Works offline

**Cons**:
- 500KB added to bundle
- Client CPU usage for large PDFs

**Effort**: Medium (3-4 days)

---

### Option B: Server-side preview (ImageMagick)

**Pros**:
- Smaller client bundle
- Consistent rendering

**Cons**:
- Server load
- Network round-trip delay
- Requires backend changes

**Effort**: Large (5-7 days)

---

### Comparison Matrix

| Criteria | Weight | Option A | Option B |
|----------|--------|----------|----------|
| User experience | 5 | 5 | 3 |
| Implementation effort | 4 | 4 | 2 |
| Server cost | 3 | 5 | 2 |
| **Weighted Total** | | 54 | 27 |

## 5. Recommendations

### Primary Recommendation

Use **Option A (PDF.js client-side preview)**. Better UX, lower effort, no server cost increase.

### Secondary Recommendations

- Lazy-load PDF.js only when PDF selected (reduce initial bundle)
- Use existing `uploadMultipart()` for files > 5MB
- Add `PDFPreview` to component library for reuse

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Large PDF crashes browser | Low | Medium | Add 50-page preview limit |
| PDF.js security vulnerability | Low | High | Monitor advisories, pin version |

## 6. Unanswered Questions

None - all questions resolved.

---

**Next Step**: 04_plan.md
```

---

## 7. Checklist Before Approval Request

```markdown
- [ ] All questions from requirements have answers or "unanswered" status
- [ ] Evidence provided for each conclusion
- [ ] Existing code analyzed for conflicts
- [ ] Glossary checked for term consistency
- [ ] Options compared objectively (if multiple)
- [ ] Clear recommendation with justification
- [ ] Risks identified with mitigations
```

---

## 8. Approval Request Format

```
[문서 전체 내용]

---
이 Research 문서를 검토해 주세요.
승인하시면 다음 단계(04_plan.md)로 진행하겠습니다.
수정이 필요하면 말씀해 주세요.
```

---

**Next**: `04_plan.md` (Architecture Plan)
