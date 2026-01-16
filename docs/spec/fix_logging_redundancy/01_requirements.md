---
Task: fix_logging_redundancy
Created: 2026-01-13
Status: Approved
Summary: Fix console/debug log file redundancy and dummy file issues by ensuring session-specific logic.
Research Required: Yes
---

# fix_logging_redundancy - Requirements

## 1. Goal

### 1.1 Primary Goal

The application correctly writes debug logs to a single, session-specific timestamped file without creating redundant, incorrectly named daily logs.

### 1.2 Success Criteria

- [ ] Logs are written to the session file created at startup (e.g., `ChronoView_Debug_YYYYMMDD_HHmmss.log`).
- [ ] No redundant "double-dated" files (e.g., `ChronoView_Debug_YYYYMMDD_YYYYMMDD.log`) are created.
- [ ] The session header (Session Started: ...) is preserved and visible at the start of the log file.
- [ ] Log entries include timestamp, level, category, and message.
- [ ] Log entries are appended to the same file throughout the application session.

## 2. Constraints

### 2.1 Technical Constraints

- Must maintain `Microsoft.Extensions.Logging` integration.
- Must continue using `%APPDATA%\ChronoView\Logs\{YYYYMMDD}\` as the base directory structure.
- Should not block the UI thread during file I/O (existing `lock` is acceptable for thread safety, but I/O should be efficient).

### 2.2 Business Constraints

- Debugging logs are critical for field troubleshooting; consistency is paramount.

### 2.3 Non-Goals (Out of Scope)

- Changing the overall logging framework (stay with `FileLoggerProvider`).
- Implementing advanced log rotation (beyond the existing daily folder structure).
- Modifying UI panel logging (which is reported as working correctly).

## 3. Questions to Investigate

- [x] Q1: Why are redundant log files being created while the session file remains empty (except for the header)?
- [ ] Q2: Does the current implementation handle date transitions correctly if the app stays open over midnight?

## 4. Assumptions

- `App.xaml.cs` successfully initializes the session file with a header.
- `FileLoggerProvider` correctly receives the path to this initialized session file.

## 5. Dependencies

### 5.1 Blocked By

| Dependency | Status | Owner |
|------------|--------|-------|
| None | - | - |

### 5.2 Blocks

| Dependent Task | Impact if Delayed |
|----------------|-------------------|
| Field Troubleshooting | Difficulty in tracking session-specific issues |

---

## Approval

- [x] Requirements reviewed and approved
- [x] Success criteria are measurable
- [x] Scope boundaries are clear
- [x] All blocking dependencies identified

**Next Step**: 02_research.md
