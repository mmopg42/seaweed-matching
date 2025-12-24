# Template: Design Document (04_design.md)

> **Purpose**: Define HOW each component works internally.  
> **Core Question**: "코드가 어떻게 동작할까?" (How will the code work?)  
> **Level**: Implementation details, pseudo-code, error handling, state management.

---

## 1. Plan vs Design: Reminder

| In 03_plan.md | In 04_design.md (This doc) |
|---------------|---------------------------|
| `function load(path) -> Config` | Full pseudo-code with steps |
| "Retry on failure" | "Retry 3x with 1s exponential backoff" |
| "Thread-safe" | Lock patterns, mutex usage |
| "Validates input" | Specific validation rules and error messages |
| Component boxes | Sequence diagrams, state machines |

---

## 2. Key Principles

### Complete Pseudo-code

Every public method from `03_plan.md` must have:
- Step-by-step logic
- Input validation
- Error handling
- Edge cases

### Explicit Error Handling

Document:
- What errors can occur
- How they're detected
- How they're handled
- How they propagate

### State Management

If component has state:
- List all state variables
- Document state transitions
- Specify thread safety approach

### Testability

Include test cases that verify the design works correctly.

---

## 3. Template

```markdown
---
Task: [Task Name]
Created: [YYYY-MM-DD]
Status: Draft | Approved
Depends On: 03_plan.md
---

# [Task Name] - Detailed Design

## 1. Component Designs

### 1.1 [Component Name]

> From 03_plan.md: [One-line description]

#### Interface (from Plan)

```
function_name(param1: Type, param2: Type) -> ReturnType
```

#### Preconditions

- [What must be true before calling]
- [Required state or input conditions]

#### Postconditions

- [What is guaranteed after successful return]
- [State changes that occur]

#### Detailed Logic

```pseudo
function function_name(param1, param2):
    // ========== INPUT VALIDATION ==========
    if param1 is null:
        raise InvalidArgumentError("param1 is required")
    
    if param2 < 0:
        raise InvalidArgumentError("param2 must be non-negative")
    
    // ========== CORE LOGIC ==========
    // Step 1: [Description]
    intermediate = process(param1)
    
    // Step 2: [Description]
    if intermediate.isEmpty():
        return DEFAULT_VALUE
    
    // Step 3: [Description]
    result = transform(intermediate, param2)
    
    // ========== CLEANUP & RETURN ==========
    return result
```

#### State Variables

| Variable | Type | Initial | Purpose |
|----------|------|---------|---------|
| `_state` | StateEnum | IDLE | Current component state |
| `_cache` | Map | empty | Cached results |

#### State Transitions

```
IDLE ──[start()]--> RUNNING ──[complete()]--> DONE
                        │
                        └──[error()]--> ERROR ──[reset()]--> IDLE
```

#### Thread Safety

```pseudo
// Locking strategy
function thread_safe_operation():
    acquire_lock(_mutex)
    try:
        // Critical section
        _shared_state.modify()
    finally:
        release_lock(_mutex)
```

Or: "This component is not thread-safe. Caller must synchronize."

#### Error Handling

| Error | Detection | Handling | Recovery |
|-------|-----------|----------|----------|
| Invalid input | Check at entry | Raise exception | Caller handles |
| Network timeout | Catch TimeoutError | Retry 3x | Fail after retries |
| Resource busy | Lock timeout (5s) | Log and skip | Continue with stale |

---

### 1.2 [Next Component]

[Repeat structure from 1.1]

---

## 2. Integration Points

### 2.1 [Component A] → [Component B]

#### Call Sequence

```
1. A prepares request data
2. A calls B.method(data)
3. B validates data
4. B processes and returns result
5. A handles result or error
```

#### Error Propagation

| B throws | A catches | A does |
|----------|-----------|--------|
| ValidationError | Yes | Show user message |
| NetworkError | Yes | Retry once |
| UnknownError | No | Let propagate |

#### Data Contract

```
// Request (A → B)
{
    field1: string,    // Required, max 100 chars
    field2: int        // Optional, default 0
}

// Response (B → A)
{
    success: bool,
    data: object | null,
    error: string | null
}
```

---

## 3. Edge Cases & Boundary Conditions

| Case | Input | Expected Behavior | Implementation |
|------|-------|-------------------|----------------|
| Empty input | `[]` or `null` | Return empty result | Check at line 5 |
| Single item | `[item]` | Process normally | No special case |
| Max capacity | 10,000 items | Process in batches | Chunk by 100 |
| Concurrent calls | Multiple threads | Thread-safe | Use mutex |
| Duplicate values | Same item twice | Deduplicate | Use Set |

---

## 4. Resource Management

### 4.1 Initialization

```pseudo
function initialize():
    // Allocate resources
    _connection = create_connection(config.url)
    _timer = create_timer(config.interval)
    _cache = create_cache(config.max_size)
    
    // Start background tasks
    _timer.start()
    
    _state = INITIALIZED
```

### 4.2 Cleanup / Disposal

```pseudo
function dispose():
    // Step 1: Stop accepting new work
    _state = DISPOSING
    
    // Step 2: Wait for in-progress work
    wait_for_pending(timeout: 30s)
    
    // Step 3: Release resources (reverse order)
    _cache.clear()
    _timer.stop()
    _timer.dispose()
    _connection.close()
    
    // Step 4: Final state
    _state = DISPOSED
```

### 4.3 Resource Lifecycle

```
create() → initialize() → [use] → dispose()
              │                      │
              └── INITIALIZED        └── DISPOSED
```

---

## 5. Performance Considerations

| Operation | Expected Time | Memory | Notes |
|-----------|---------------|--------|-------|
| Initialize | < 100ms | ~10MB | One-time at startup |
| Process item | < 50ms | ~1KB | Per item |
| Batch (100) | < 2s | ~100KB | Parallel processing |
| Cache lookup | < 1ms | - | O(1) hash lookup |

### Optimization Notes

- [Optimization 1 and why it matters]
- [Optimization 2 and why it matters]

---

## 6. Testing Strategy

### 6.1 Unit Test Cases

| Test Name | Input | Expected | Verifies |
|-----------|-------|----------|----------|
| test_happy_path | Valid input | Success result | Core logic |
| test_null_input | null | InvalidArgumentError | Validation |
| test_empty_list | [] | Empty result | Edge case |
| test_max_size | 10MB file | Success | Boundary |
| test_over_max | 11MB file | SizeExceededError | Limit |

### 6.2 Integration Test Cases

| Test Name | Setup | Action | Expected |
|-----------|-------|--------|----------|
| test_end_to_end | Full system | Complete flow | Success |
| test_component_failure | Mock B fails | A handles gracefully | Error logged |

### 6.3 Manual Verification Steps

```
1. Setup: [Describe setup]
2. Action: [What to do]
3. Verify: [What to check]
   Expected: [Expected outcome]
```

---

## 7. Security Considerations

| Concern | Risk | Mitigation |
|---------|------|------------|
| Input injection | High | Sanitize all inputs |
| Data exposure | Medium | Encrypt sensitive fields |
| Auth bypass | High | Validate token on every request |

---

## 8. Open Questions

- [x] All design questions resolved

> **Rule**: No open questions allowed before implementation.

---

## Approval

- [ ] All components have detailed pseudo-code
- [ ] Error handling specified for all failure modes
- [ ] State management documented (if stateful)
- [ ] Thread safety addressed
- [ ] Edge cases covered
- [ ] Test cases defined
- [ ] No open questions

**Next Step**: 05_tasks.md
```

---

## 4. Section Guidelines

### 4.1 Detailed Logic (Pseudo-code)

Write pseudo-code that:
- Is language-agnostic but specific
- Has clear sections (validation, core logic, cleanup)
- Includes comments explaining WHY, not just WHAT
- Shows all branching and error paths

**Format**:
```pseudo
function name(params):
    // ===== SECTION NAME =====
    // Purpose of this section
    
    // Step with explanation
    action()
```

### 4.2 Preconditions & Postconditions

**Preconditions**: What MUST be true before calling
- Input constraints
- Required system state
- Dependencies that must be available

**Postconditions**: What is GUARANTEED after return
- Output properties
- State changes
- Side effects

### 4.3 Error Handling Table

For each error:
- **Detection**: How do we know it happened?
- **Handling**: What do we do?
- **Recovery**: Can we continue? How?

### 4.4 Thread Safety

Options to document:
1. "Not thread-safe - caller must synchronize"
2. Lock/mutex strategy with pseudo-code
3. Lock-free with atomic operations
4. Immutable design

### 4.5 Test Cases

Every public method should have test cases for:
- Happy path (normal operation)
- Invalid inputs (validation)
- Edge cases (boundaries)
- Error conditions (failure handling)

---

## 5. Common Mistakes

| Mistake | Problem | Fix |
|---------|---------|-----|
| Vague pseudo-code | "Process the data" - what does that mean? | Be specific about steps |
| Missing error handling | Runtime failures | Cover all failure modes |
| No state documentation | Race conditions | Document state explicitly |
| Skipping edge cases | Bugs in production | List all boundary conditions |
| No test cases | Can't verify design | Add tests for each scenario |
| Implementation language | Too specific | Use pseudo-code |

---

## 6. Example

```markdown
---
Task: pdf_upload_feature
Created: 2024-01-15
Status: Draft
Depends On: 03_plan.md
---

# PDF Upload Feature - Detailed Design

## 1. Component Designs

### 1.1 PDFPreview

> React component that renders the first page of a PDF file.

#### Interface (from Plan)

```typescript
function PDFPreview(props: PDFPreviewProps): JSX.Element
```

#### Preconditions

- `props.file` is a valid File object or null
- If file is provided, it should be a PDF (caller validates)

#### Postconditions

- Renders preview of first page, or error state, or empty state
- PDF.js library is loaded (lazy load on first use)

#### Detailed Logic

```pseudo
function PDFPreview({ file, maxPages = 1, onError }):
    // ===== STATE =====
    state = {
        pdfDoc: null,
        pageImage: null,
        loading: false,
        error: null
    }
    
    // ===== EFFECT: Load PDF when file changes =====
    useEffect(() => {
        if file is null:
            state.pdfDoc = null
            state.pageImage = null
            return
        
        state.loading = true
        state.error = null
        
        try:
            // Step 1: Lazy load PDF.js
            pdfjs = await import('pdfjs-dist')
            
            // Step 2: Read file as ArrayBuffer
            buffer = await file.arrayBuffer()
            
            // Step 3: Load PDF document
            doc = await pdfjs.getDocument(buffer).promise
            state.pdfDoc = doc
            
            // Step 4: Render first page
            page = await doc.getPage(1)
            canvas = createCanvas(page.viewport)
            await page.render({ canvasContext: canvas.getContext('2d') })
            
            // Step 5: Convert to image
            state.pageImage = canvas.toDataURL()
            
        catch error:
            state.error = error
            onError?.(error)
            
        finally:
            state.loading = false
    }, [file])
    
    // ===== RENDER =====
    if state.loading:
        return <LoadingSpinner />
    
    if state.error:
        return <ErrorDisplay message="Failed to load PDF" />
    
    if state.pageImage is null:
        return <EmptyState message="No PDF selected" />
    
    return <img src={state.pageImage} alt="PDF Preview" />
```

#### Error Handling

| Error | Detection | Handling | User Sees |
|-------|-----------|----------|-----------|
| Invalid PDF | pdfjs throws | Catch, set error state | "Failed to load PDF" |
| Corrupted file | getPage fails | Catch, set error state | "Failed to load PDF" |
| Memory limit | Browser throws | Catch, call onError | Error message |

---

### 1.2 UploadProgress

#### Detailed Logic

```pseudo
function UploadProgress({ progress, status, onCancel }):
    // ===== COMPUTED VALUES =====
    progressPercent = clamp(progress, 0, 100)
    
    statusText = switch status:
        'idle' => "Ready to upload"
        'uploading' => "{progressPercent}% complete"
        'complete' => "Upload complete!"
        'error' => "Upload failed"
    
    // ===== RENDER =====
    return (
        <div class="upload-progress">
            <ProgressBar value={progressPercent} />
            <span class="status">{statusText}</span>
            
            if status == 'uploading':
                <button onClick={onCancel}>Cancel</button>
        </div>
    )
```

---

## 2. Integration Points

### 2.1 FileUpload → PDFPreview

#### Call Sequence

```
1. User selects file in FileUpload
2. FileUpload validates file type
3. If PDF, FileUpload passes file to PDFPreview
4. PDFPreview loads and renders preview
5. If error, PDFPreview calls onError
6. FileUpload displays error to user
```

#### Error Propagation

| PDFPreview | FileUpload |
|------------|------------|
| Calls onError(e) | Displays "Invalid PDF file" |

---

## 3. Edge Cases

| Case | Input | Behavior |
|------|-------|----------|
| No file | null | Show empty state |
| Non-PDF | .txt file | Caller should validate; error if passed |
| Empty PDF | 0 pages | Show "PDF has no pages" error |
| Large PDF | 100+ pages | Only render first page |
| Password PDF | Encrypted | Show "Password-protected PDF" error |

---

## 4. Testing Strategy

### Unit Tests

| Test | Input | Expected |
|------|-------|----------|
| renders_empty_state | file=null | EmptyState shown |
| renders_preview | valid PDF | img element with src |
| shows_loading | file provided | LoadingSpinner during load |
| handles_invalid_pdf | corrupted file | ErrorDisplay shown |

---

**Next Step**: 05_tasks.md
```

---

## 7. Checklist Before Approval Request

```markdown
- [ ] Every component has detailed pseudo-code
- [ ] Preconditions and postconditions documented
- [ ] All error scenarios have handling defined
- [ ] State management documented (if applicable)
- [ ] Thread safety addressed (if applicable)
- [ ] Integration points defined with contracts
- [ ] Edge cases listed with expected behavior
- [ ] Test cases cover happy path, errors, and edge cases
- [ ] No open questions remain
```

---

## 8. Approval Request Format

```
[문서 전체 내용]

---
이 Design 문서를 검토해 주세요.
승인하시면 다음 단계(05_tasks.md)로 진행하겠습니다.
수정이 필요하면 말씀해 주세요.
```

---

**Next**: `05_tasks.md` (Task Checklist)
