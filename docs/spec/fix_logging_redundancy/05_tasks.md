---
Task: fix_logging_redundancy
Created: 2026-01-13
Status: In Progress
Depends On: 04_design.md
---

# fix_logging_redundancy - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Setup | 1 | 0 | 1 |
| Core | 1 | 0 | 1 |
| Integration | 1 | 0 | 1 |
| Verification | 3 | 0 | 3 |
| **Total** | **6** | **0** | **6** |

---

## Phase 1: Setup

### 1.1 Pre-flight Checks

- [ ] Verify `Infrastructure/Logging/FileLoggerProvider.cs` is the only place implementing this logic.
- [ ] Ensure `App.xaml.cs` generates the path correctly.

**Verify**: Code inspection.

---

## Phase 2: Core Implementation

### 2.1 FileLogger Fix

- [ ] Modify `ChronoView/Infrastructure/Logging/FileLoggerProvider.cs`.
  - [ ] Remove `baseLogDir`, `fileName`, `baseName`, `today`, `todayDateFolder`, `todayLogFile` calculation.
  - [ ] Use `_logFilePath` directly in `File.AppendAllText`.
  - [ ] Ensure directory exists before writing.

**Verify**: Code compiles.

---

## Phase 3: Integration

### 3.1 Verify Initialization

- [ ] Ensure `FileLoggerProvider` constructor correctly receives the path from `App.xaml.cs`.

**Verify**: Inspection of `App.xaml.cs:206`.

---

## Phase 4: Verification

### 4.1 Manual Test

- [ ] Run application.
- [ ] Verify log file creation in `%APPDATA%\ChronoView\Logs\{Today}\`.
- [ ] Check for correctly named session file and absence of "double-dated" files.

---

## Phase 5: Success Criteria Check

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Logs written to session file | ⬜ | Manual check |
| No double-dated files | ⬜ | Manual check |
| Session header preserved | ⬜ | Manual check |

---

## Completion Log

| Task | Completed | Duration | Notes |
|------|-----------|----------|-------|
| - | - | - | - |

---

## Approval

- [ ] All tasks completed
- [ ] All verifications pass
- [ ] Success criteria met

**Next Step**: 06_report.md
