# Template: Plan Document (03_plan.md)

> **Purpose**: Define WHAT components to build and HOW they connect.  
> **Core Question**: "어떤 구조로 만들까?" (What structure will we build?)  
> **Level**: Architecture decisions, NOT implementation details.

---

## 1. Plan vs Design: Critical Distinction

| Aspect | 03_plan.md (This doc) | 04_design.md (Next doc) |
|--------|----------------------|-------------------------|
| **Abstraction** | High-level | Low-level |
| **Focus** | WHAT to build | HOW it works internally |
| **Diagrams** | Component boxes, data flow | Sequence diagrams, state machines |
| **Interfaces** | Signatures only | + Preconditions, postconditions |
| **Logic** | "A calls B" | Pseudo-code with steps |
| **Errors** | "Retry on failure" | "Retry 3x, 1s interval, then fail" |
| **Concurrency** | "Must be thread-safe" | Lock patterns, atomic operations |

### Rule of Thumb

If you're writing pseudo-code or step-by-step logic, it belongs in **04_design.md**, not here.

---

## 2. Key Principles

### Requirements Traceability

Every success criterion from `01_requirements.md` MUST map to a component or approach in this document.

### Decisions, Not Details

- ✅ "Use Redis for caching with 5-minute TTL"
- ❌ "Initialize Redis client with `redis.createClient({host: 'localhost', port: 6379})`"

### Clear Interfaces

Define function/method signatures, but leave internal logic for design doc.

### Explicit Decisions

Document WHY you chose each approach, especially when alternatives exist.

---

## 3. Template

```markdown
---
Task: [Task Name]
Created: [YYYY-MM-DD]
Status: Draft | Approved
Depends On: 01_requirements.md[, 02_research.md]
---

# [Task Name] - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| [Criterion 1 from requirements] | [Component/approach] | [How to verify] |
| [Criterion 2 from requirements] | [Component/approach] | [How to verify] |
| [Criterion 3 from requirements] | [Component/approach] | [How to verify] |

> **Rule**: If any criterion is NOT addressed, this plan is INCOMPLETE.

---

## 1. Architecture Overview

### 1.1 System Context

[2-3 sentences: How does this feature fit into the overall system?]

### 1.2 Component Diagram

```
┌─────────────────────────────────────────────────────────┐
│                      [System Name]                       │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────┐      ┌──────────┐      ┌──────────┐      │
│  │  Comp A  │─────►│  Comp B  │─────►│  Comp C  │      │
│  └──────────┘      └──────────┘      └──────────┘      │
│       │                                    │            │
│       │            ┌──────────┐            │            │
│       └───────────►│  Comp D  │◄───────────┘            │
│                    └──────────┘                         │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 1.3 Data Flow

```
[Input Source]
    │
    ▼
[Step 1: Description]
    │
    ▼
[Step 2: Description]
    │
    ▼
[Output/Storage]
```

---

## 2. Components

### 2.1 New Components

| Component | Type | Location | Purpose |
|-----------|------|----------|---------|
| `NewService` | Class | `src/services/new_service.py` | [Purpose] |
| `NewModel` | Class | `src/models/new_model.py` | [Purpose] |
| `helper_func` | Function | `src/utils/helpers.py` | [Purpose] |

### 2.2 Modified Components

| Component | Location | Changes | Breaking Change? |
|-----------|----------|---------|------------------|
| `ExistingService` | `src/services/existing.py` | Add new method | No |
| `Config` | `src/config.py` | Add new keys | No |

### 2.3 Deleted Components

| Component | Location | Reason | Migration |
|-----------|----------|--------|-----------|
| `OldHelper` | `src/utils/old.py` | Replaced by NewService | Update 3 call sites |

---

## 3. Interface Definitions

### 3.1 [Component Name]

```
class ComponentName:
    """[One-line description]"""
    
    def method_name(param1: Type, param2: Type) -> ReturnType:
        """[Brief description]"""
        
    def another_method(param: Type) -> ReturnType:
        """[Brief description]"""
```

**Responsibilities**:
- [What this component does]
- [What this component does]

**Does NOT**:
- [What this component does NOT do]

**Errors Raised**:
- `ErrorType1`: When [condition]
- `ErrorType2`: When [condition]

---

### 3.2 [Next Component]

[Repeat structure from 3.1]

---

## 4. Key Design Decisions

### 4.1 [Decision Topic]

**Context**: [What problem needs a decision?]

**Options Considered**:

| Option | Pros | Cons |
|--------|------|------|
| Option A | [Pros] | [Cons] |
| Option B | [Pros] | [Cons] |

**Decision**: Option [X]

**Rationale**: [Why this option was chosen]

---

### 4.2 [Next Decision Topic]

[Repeat structure]

---

## 5. Configuration

### 5.1 New Configuration Keys

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `feature.enabled` | bool | false | Enable new feature |
| `feature.timeout` | int | 30 | Timeout in seconds |

### 5.2 Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `NEW_API_KEY` | Yes | API key for external service |

---

## 6. External Dependencies

### 6.1 New Libraries

| Library | Version | Purpose | Size Impact |
|---------|---------|---------|-------------|
| `library-name` | ^1.2.0 | [Purpose] | +500KB |

### 6.2 External Services

| Service | Purpose | Failure Handling |
|---------|---------|------------------|
| [Service Name] | [Purpose] | [What happens if unavailable] |

---

## 7. Glossary Updates

### Existing Terms Check

> **BEFORE defining new terms**, check `docs/architecture/glossary.md` for:
> - Existing names that might conflict
> - Established patterns to follow
> - Terms you should reuse instead of creating new ones

| Checked | Existing Term | Relevance |
|---------|---------------|-----------|
| [ ] | [term from glossary] | [How it relates to this task] |

### New Terms to Add

| Term | Type | Definition |
|------|------|------------|
| `NewService` | Class | [Definition] |
| `feature.enabled` | Config | [Definition] |

### Terms to Update

| Term | Change |
|------|--------|
| `ExistingTerm` | [What changed] |

---

## 8. Architecture Documentation Plan

### Documents to Create

| Document | Purpose |
|----------|---------|
| `module_new_service.md` | Document NewService contracts |

### Documents to Update

| Document | Changes |
|----------|---------|
| `glossary.md` | Add new terms |
| `README.md` | Add index entry |
| `module_existing.md` | Update dependencies |

---

## 9. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| [Risk 1] | High/Med/Low | High/Med/Low | [Mitigation] |
| [Risk 2] | High/Med/Low | High/Med/Low | [Mitigation] |

---

## 10. Open Questions

- [x] [Resolved question] → [Answer]
- [ ] [Open question - must resolve before design]

> **Rule**: All questions must be resolved before moving to 04_design.md

---

## Approval

- [ ] All requirements traced to components
- [ ] Component interfaces defined
- [ ] Design decisions documented with rationale
- [ ] Glossary terms identified
- [ ] All open questions resolved

**Next Step**: 04_design.md
```

---

## 4. Section Guidelines

### 4.1 Requirements Traceability (Section 0)

This is the FIRST thing to complete. It ensures nothing from requirements is forgotten.

**Format**: Map each success criterion to:
- Which component addresses it
- How you'll verify it works

If you can't map a criterion → the plan is incomplete.

### 4.2 Component Diagram

Use ASCII art for simplicity. Show:
- Main components as boxes
- Arrows for data/control flow
- External systems at boundaries

**Keep it simple**: If you need more than 10 boxes, you're including too much detail.

### 4.3 Interface Definitions

Define the PUBLIC contract only:
- Method signatures with types
- What the component is responsible for
- What errors it can raise

**Do NOT include**:
- Internal implementation
- Private methods
- Step-by-step logic (that's for design doc)

### 4.4 Key Design Decisions

Document every non-obvious choice:
- What alternatives were considered
- Why you chose this approach
- What trade-offs were made

This prevents "why did we do it this way?" questions later.

### 4.5 Glossary Updates

> **Check existing terms FIRST, then add new ones.**

1. **Before defining anything new**: Open `docs/architecture/glossary.md`
2. **Look for conflicts**: Is there already a similar name?
3. **Reuse existing terms**: Don't create `UserService` if `UserManager` exists
4. **Only then add new terms**: To Section 7 of this plan

---

## 5. Common Mistakes

| Mistake | Problem | Fix |
|---------|---------|-----|
| Missing traceability | Requirements may be forgotten | Fill Section 0 first |
| Too much detail | Belongs in design doc | Keep to signatures and structure |
| Pseudo-code in plan | Wrong abstraction level | Move to 04_design.md |
| No decision rationale | Can't understand choices later | Document WHY, not just WHAT |
| Undefined terms | Naming conflicts | Check glossary first |
| Unresolved questions | Blocks design phase | Resolve all before approval |

---

## 6. Example

```markdown
---
Task: pdf_upload_feature
Created: 2024-01-15
Status: Draft
Depends On: 01_requirements.md, 02_research.md
---

# PDF Upload Feature - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification |
|-------------------|--------------|--------------|
| PDF ≤10MB uploads successfully | FileValidator + StorageService | Upload 10MB file, verify in S3 |
| Progress shows percentage | UploadProgress component | Visual test during upload |
| Preview displays first page | PDFPreview component | Upload PDF, verify preview |
| Invalid files show error | FileValidator | Upload .exe, verify error message |
| Upload < 5s for 5MB | Chunked upload | Performance test on 10Mbps |

---

## 1. Architecture Overview

### 1.1 System Context

PDF upload extends the existing file upload system. It adds client-side preview using PDF.js and leverages existing S3 multipart upload for large files.

### 1.2 Component Diagram

```
┌─────────────────────────────────────────────────────────┐
│                     Frontend                            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────────┐    ┌──────────────┐                  │
│  │  FileUpload  │───►│ PDFPreview   │  (new)           │
│  │  (modified)  │    │              │                  │
│  └──────────────┘    └──────────────┘                  │
│         │                                               │
│         ▼                                               │
│  ┌──────────────┐                                      │
│  │UploadProgress│                                      │
│  │   (new)      │                                      │
│  └──────────────┘                                      │
│                                                         │
└─────────────────────────────────────────────────────────┘
         │
         ▼ API
┌─────────────────────────────────────────────────────────┐
│                     Backend                             │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────────┐    ┌──────────────┐                  │
│  │FileValidator │───►│StorageService│                  │
│  │  (modified)  │    │  (existing)  │                  │
│  └──────────────┘    └──────────────┘                  │
│                             │                           │
└─────────────────────────────────────────────────────────┘
                              │
                              ▼
                         [ AWS S3 ]
```

### 1.3 Data Flow

```
User selects PDF
    │
    ▼
FileValidator checks type/size (client)
    │
    ▼
PDFPreview renders first page (PDF.js)
    │
    ▼
User clicks Upload
    │
    ▼
UploadProgress shows percentage
    │
    ▼
StorageService.uploadMultipart() → S3
    │
    ▼
Success confirmation
```

---

## 2. Components

### 2.1 New Components

| Component | Type | Location | Purpose |
|-----------|------|----------|---------|
| `PDFPreview` | React | `src/components/PDFPreview.tsx` | Render PDF first page |
| `UploadProgress` | React | `src/components/UploadProgress.tsx` | Show upload percentage |

### 2.2 Modified Components

| Component | Location | Changes | Breaking? |
|-----------|----------|---------|-----------|
| `FileUpload` | `src/components/FileUpload.tsx` | Add PDF handling | No |
| `FileValidator` | `src/validators/file.py` | Add PDF MIME types | No |

---

## 3. Interface Definitions

### 3.1 PDFPreview

```typescript
interface PDFPreviewProps {
  file: File | null;
  maxPages?: number;  // default: 1
  onError?: (error: Error) => void;
}

function PDFPreview(props: PDFPreviewProps): JSX.Element
```

**Responsibilities**:
- Load PDF.js dynamically (lazy load)
- Render specified number of pages
- Handle loading and error states

**Does NOT**:
- Upload files
- Validate file types (caller's responsibility)

**Errors**:
- Renders error state if PDF cannot be parsed

---

### 3.2 UploadProgress

```typescript
interface UploadProgressProps {
  progress: number;  // 0-100
  status: 'idle' | 'uploading' | 'complete' | 'error';
  onCancel?: () => void;
}

function UploadProgress(props: UploadProgressProps): JSX.Element
```

**Responsibilities**:
- Display progress bar
- Show status text
- Provide cancel button during upload

---

## 4. Key Design Decisions

### 4.1 Client-side vs Server-side Preview

**Context**: Need to show PDF preview before upload.

| Option | Pros | Cons |
|--------|------|------|
| Client (PDF.js) | Instant, no server load | 500KB bundle |
| Server (ImageMagick) | Smaller client | Latency, server cost |

**Decision**: Client-side (PDF.js)

**Rationale**: Better UX (instant preview), lower infrastructure cost. Bundle size acceptable with lazy loading.

---

### 4.2 Chunked Upload Threshold

**Context**: When to use multipart upload?

**Decision**: Use multipart for files > 5MB

**Rationale**: S3 multipart minimum is 5MB. Below that, single PUT is faster.

---

## 5. Configuration

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `upload.maxSizeMB` | int | 10 | Maximum PDF size |
| `upload.chunkSizeMB` | int | 5 | Multipart chunk size |
| `upload.previewPages` | int | 1 | Pages to show in preview |

---

## 6. External Dependencies

### 6.1 New Libraries

| Library | Version | Purpose | Size |
|---------|---------|---------|------|
| `pdfjs-dist` | ^3.4.0 | PDF rendering | 500KB (lazy) |

---

## 7. Glossary Updates

### Existing Terms Check

| Checked | Existing Term | Relevance |
|---------|---------------|-----------|
| [x] | `StorageService` | Will use for upload |
| [x] | `FileUpload` | Will extend this component |
| [x] | `validate_file` | Will add PDF types |

### New Terms to Add

| Term | Type | Definition |
|------|------|------------|
| `PDFPreview` | Component | React component for PDF preview |
| `UploadProgress` | Component | Upload progress indicator |
| `upload.maxSizeMB` | Config | Max upload size in MB |

---

## 8. Architecture Documentation Plan

| Document | Action |
|----------|--------|
| `glossary.md` | Add new terms |
| `feature_file_upload.md` | Update with PDF support |

---

## 9. Open Questions

- [x] Which PDF library? → PDF.js (from research)
- [x] Chunk size? → 5MB (S3 minimum)

---

**Next Step**: 04_design.md
```

---

## 7. Checklist Before Approval Request

```markdown
- [ ] All success criteria mapped to components (Section 0)
- [ ] Component diagram is clear and simple
- [ ] All new/modified/deleted components listed
- [ ] Interface signatures defined (no implementation)
- [ ] Design decisions documented with rationale
- [ ] Configuration keys specified
- [ ] Glossary terms identified
- [ ] All open questions resolved
```

---

## 8. Approval Request Format

```
[문서 전체 내용]

---
이 Plan 문서를 검토해 주세요.
승인하시면 다음 단계(04_design.md)로 진행하겠습니다.
수정이 필요하면 말씀해 주세요.
```

---

**Next**: `04_design.md` (Detailed Design)
