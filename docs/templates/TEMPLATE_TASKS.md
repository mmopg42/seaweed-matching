# Template: Tasks Document (06_tasks.md)

> **Purpose**: Provide ordered implementation checklist with verification steps.  
> **Core Question**: "무슨 순서로 구현하나?" (In what order do we implement?)  
> **Critical**: Once this file exists, CODE MODIFICATION IS UNLOCKED.

---

## 1. What This Document Does

- Breaks implementation into manageable, ordered tasks
- Each task has clear verification criteria
- Tracks progress during implementation
- Ensures nothing from design is forgotten

---

## 2. Key Principles

### Ordered Execution

Tasks are numbered and should be completed in order. Dependencies flow downward.

### Verifiable Tasks

Every task must have a verification step. If you can't verify it, break it down further.

### Atomic Tasks

Each task should be:
- Completable in one session
- Independently verifiable
- Small enough to review easily

### Progress Tracking

Update checkboxes and completion log as you work.

---

## 3. Template

```markdown
---
Task: [Task Name]
Created: [YYYY-MM-DD]
Status: In Progress | Complete
Depends On: 05_design.md
---

# [Task Name] - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Setup | X | X | 0 |
| Core | X | X | 0 |
| Integration | X | X | 0 |
| Documentation | X | X | 0 |
| Verification | X | X | 0 |
| **Total** | **X** | **X** | **0** |

---

## Phase 1: Setup

### 1.1 Glossary Updates

- [ ] Add new terms to `docs/architecture/glossary.md`
  - [ ] Term 1: [definition]
  - [ ] Term 2: [definition]
- [ ] Verify no naming conflicts with existing terms

**Verify**: `grep -rn "term_name" docs/` shows only glossary

---

### 1.2 Dependencies

- [ ] Install required packages
  - [ ] Package 1: `npm install package@version`
  - [ ] Package 2: `pip install package`
- [ ] Verify installation

**Verify**: `npm list package` or `pip show package` succeeds

---

### 1.3 Configuration

- [ ] Add configuration keys
  - [ ] `config.key1` in [file]
  - [ ] `config.key2` in [file]
- [ ] Add environment variables (if any)

**Verify**: Application starts without config errors

---

## Phase 2: Core Implementation

### 2.1 [Component 1 from design]

- [ ] Create file: `path/to/component.py`
- [ ] Implement class/function skeleton
- [ ] Implement core logic
  - [ ] Step 1: [description]
  - [ ] Step 2: [description]
- [ ] Add input validation
- [ ] Add error handling
- [ ] Add thread safety (if needed)

**Verify**: 
```bash
# Run unit tests
pytest tests/test_component1.py -v
```

---

### 2.2 [Component 2 from design]

- [ ] Create file: `path/to/component2.py`
- [ ] Implement according to design doc
- [ ] Add tests

**Verify**:
```bash
pytest tests/test_component2.py -v
```

---

### 2.3 [Continue for each component...]

---

## Phase 3: Integration

### 3.1 Wire Components Together

- [ ] Connect Component 1 → Component 2
- [ ] Update initialization code
- [ ] Add integration error handling

**Verify**:
```bash
pytest tests/integration/ -v
```

---

### 3.2 Update Existing Code

- [ ] Modify [existing file] to use new components
- [ ] Update imports
- [ ] Ensure backward compatibility

**Verify**: Existing tests still pass
```bash
pytest tests/ -v
```

---

## Phase 4: Documentation

### 4.1 Architecture Documentation

- [ ] Create `docs/architecture/module_[name].md`
  - [ ] Overview section
  - [ ] Contracts section
  - [ ] Dependencies section
  - [ ] VERIFY commands added
- [ ] Update `docs/architecture/README.md` index

**Verify**: VERIFY commands in new doc return expected results

---

### 4.2 Code Documentation

- [ ] Add docstrings to all public functions
- [ ] Add inline comments for complex logic
- [ ] Update README if user-facing changes

---

## Phase 5: Final Verification

### 5.1 Test Suite

- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] No regressions in existing tests

```bash
# Full test suite
pytest --tb=short

# Or equivalent for your stack
npm test
dotnet test
go test ./...
```

---

### 5.2 Linting & Formatting

- [ ] Code passes linter
- [ ] Code is formatted

```bash
# Python
ruff check .
black --check .

# JavaScript
npm run lint

# Go
go fmt ./...
go vet ./...
```

---

### 5.3 Manual Verification

- [ ] [Manual test 1]: [steps and expected result]
- [ ] [Manual test 2]: [steps and expected result]

---

### 5.4 Success Criteria Check

| Criterion (from requirements) | Status | Evidence |
|-------------------------------|--------|----------|
| [Criterion 1] | ✅ / ❌ | [Test/proof] |
| [Criterion 2] | ✅ / ❌ | [Test/proof] |
| [Criterion 3] | ✅ / ❌ | [Test/proof] |

---

## Completion Log

| Task | Completed | Duration | Notes |
|------|-----------|----------|-------|
| 1.1 Glossary | YYYY-MM-DD | 5m | Added 3 terms |
| 1.2 Dependencies | YYYY-MM-DD | 10m | pdfjs-dist installed |
| ... | ... | ... | ... |

---

## Blockers & Issues

| Issue | Impact | Resolution |
|-------|--------|------------|
| [Issue description] | [What's blocked] | [How resolved / Status] |

---

## Approval

- [ ] All tasks completed
- [ ] All verifications pass
- [ ] Success criteria met
- [ ] Ready for 07_report.md

**Next Step**: 07_report.md

---

## 4. Section Guidelines

### 4.1 Progress Summary

Update this table as you work:
- Helps track overall progress
- Easy to see at a glance what's left
- Useful for status updates

### 4.2 Task Structure

Each task should have:

```markdown
### X.X [Task Name]

- [ ] Sub-task 1
- [ ] Sub-task 2
  - [ ] Detail if needed

**Verify**: [How to confirm task is complete]
```

### 4.3 Verification Steps

Every task needs verification. Types:

| Type | When to Use | Example |
|------|-------------|---------|
| Command | Automated check possible | `pytest tests/test_x.py` |
| Manual | Requires human judgment | "Click button, verify popup appears" |
| Inspection | Check file/code exists | "File `x.py` exists with class Y" |

**If you can't verify, break the task down further.**

### 4.4 Completion Log

Fill this in as you complete tasks:
- Creates audit trail
- Helps estimate future work
- Notes capture important decisions

### 4.5 Blockers & Issues

Track problems as they arise:
- Don't let issues get lost
- Document resolutions for future reference

---

## 5. Task Sizing Guidelines

| Size | Time | Example |
|------|------|---------|
| Small | < 30 min | Add config key, write single function |
| Medium | 30-60 min | Implement component, write test suite |
| Large | > 60 min | **Break it down further** |

If a task will take more than an hour, split it into smaller tasks.

---

## 6. Common Mistakes

| Mistake | Problem | Fix |
|---------|---------|-----|
| No verification | Can't confirm completion | Add verify step to every task |
| Tasks too large | Hard to track progress | Break into < 1 hour chunks |
| Missing dependencies | Tasks blocked | Order tasks by dependencies |
| Skipping documentation | Tech debt | Include doc tasks in plan |
| No success criteria check | May miss requirements | Add Phase 5.4 check |

---

## 7. Example

```markdown
---
Task: pdf_upload_feature
Created: 2024-01-15
Status: In Progress
Depends On: 05_design.md
---

# PDF Upload Feature - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Setup | 3 | 3 | 0 |
| Core | 4 | 2 | 2 |
| Integration | 2 | 0 | 2 |
| Documentation | 2 | 0 | 2 |
| Verification | 4 | 0 | 4 |
| **Total** | **15** | **5** | **10** |

---

## Phase 1: Setup

### 1.1 Glossary Updates

- [x] Add new terms to `docs/architecture/glossary.md`
  - [x] `PDFPreview`: React component for PDF preview
  - [x] `UploadProgress`: Upload progress indicator
  - [x] `upload.maxSizeMB`: Max upload size config
- [x] Verify no naming conflicts

**Verify**: `grep -rn "PDFPreview" src/` returns 0 results (term is new)

---

### 1.2 Dependencies

- [x] Install PDF.js
  ```bash
  npm install pdfjs-dist@^3.4.0
  ```
- [x] Verify installation

**Verify**: `npm list pdfjs-dist` shows 3.4.x

---

### 1.3 Configuration

- [x] Add to `src/config.ts`:
  ```typescript
  upload: {
    maxSizeMB: 10,
    chunkSizeMB: 5,
    previewPages: 1
  }
  ```

**Verify**: `npm run build` succeeds

---

## Phase 2: Core Implementation

### 2.1 PDFPreview Component

- [x] Create `src/components/PDFPreview.tsx`
- [x] Implement lazy loading of PDF.js
- [x] Implement render logic per design doc
- [x] Add error handling
- [ ] Add loading state UI
- [ ] Write tests

**Verify**:
```bash
npm test -- --testPathPattern=PDFPreview
```

---

### 2.2 UploadProgress Component

- [x] Create `src/components/UploadProgress.tsx`
- [x] Implement progress bar
- [ ] Implement cancel functionality
- [ ] Write tests

**Verify**:
```bash
npm test -- --testPathPattern=UploadProgress
```

---

## Phase 3: Integration

### 3.1 Update FileUpload

- [ ] Import PDFPreview and UploadProgress
- [ ] Add PDF type detection
- [ ] Wire up preview on file select
- [ ] Wire up progress during upload

**Verify**: Manual test - select PDF, see preview

---

### 3.2 Backend Integration

- [ ] Update FileValidator to accept PDF MIME types
- [ ] Enable multipart upload for files > 5MB

**Verify**:
```bash
pytest tests/test_file_validator.py -v
```

---

## Phase 4: Documentation

### 4.1 Architecture Doc

- [ ] Create `docs/architecture/feature_pdf_upload.md`
- [ ] Add VERIFY commands
- [ ] Update `docs/architecture/README.md`

---

### 4.2 Code Documentation

- [ ] Add JSDoc to PDFPreview
- [ ] Add JSDoc to UploadProgress
- [ ] Update component README

---

## Phase 5: Final Verification

### 5.1 Test Suite

- [ ] `npm test` passes
- [ ] `pytest` passes

### 5.2 Linting

- [ ] `npm run lint` passes

### 5.3 Manual Verification

- [ ] Upload 1MB PDF → Preview shows first page
- [ ] Upload 10MB PDF → Progress bar shows, upload succeeds
- [ ] Upload 11MB PDF → Error message shown
- [ ] Upload .exe file → Error message shown

### 5.4 Success Criteria

| Criterion | Status | Evidence |
|-----------|--------|----------|
| PDF ≤10MB uploads | ⬜ | test_upload_10mb |
| Progress shows % | ⬜ | Manual test |
| Preview displays | ⬜ | test_preview_render |
| Invalid shows error | ⬜ | test_invalid_type |
| < 5s for 5MB | ⬜ | performance test |

---

## Completion Log

| Task | Completed | Duration | Notes |
|------|-----------|----------|-------|
| 1.1 | 2024-01-15 | 10m | 3 terms added |
| 1.2 | 2024-01-15 | 5m | pdfjs-dist 3.4.120 |
| 1.3 | 2024-01-15 | 5m | - |
| 2.1 (partial) | 2024-01-15 | 45m | Core done, tests pending |
| 2.2 (partial) | 2024-01-15 | 30m | Cancel not done |

---

## Blockers & Issues

| Issue | Impact | Resolution |
|-------|--------|------------|
| PDF.js types outdated | TS errors | Used @ts-ignore, filed issue |

---

**Next Step**: Complete remaining tasks, then 07_report.md
```

---

## 8. Checklist Before Starting Implementation

```markdown
- [ ] 05_design.md is approved
- [ ] All tasks have verification steps
- [ ] Tasks are ordered by dependency
- [ ] No task is larger than 1 hour
- [ ] Documentation tasks included
- [ ] Success criteria check included
```

---

## 9. Approval Request Format

This document typically doesn't need approval before starting—its existence unlocks implementation. However, if review is needed:

```
[문서 전체 내용]

---
이 Tasks 문서를 검토해 주세요.
승인하시면 구현을 시작하겠습니다.
수정이 필요하면 말씀해 주세요.
```

---

**Next**: Implementation, then `07_report.md`
