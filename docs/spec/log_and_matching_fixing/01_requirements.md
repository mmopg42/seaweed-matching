# Log Ambiguity and Matching Logic Analysis

## 1. Problem Description

### Issue 1: Ambiguous Logging
The system logs all camera files (Cam1 ~ Cam6) simply as "Camera" in the matching logs. This makes it impossible to distinguish which specific camera source is being processed or matched.

**Example Log:**
`Info ... GroupManager [매칭성공] Camera: ...`

**Requirement:**
- The log should clearly identify the specific camera (e.g., `Cam1`, `Cam2`, ... `Cam6`).

### Issue 2: Incorrect Group Matching Logic
A `Cam2` file (`20251203_155913_299.bmp`) was assigned to `group_002` even though `group_001` had an empty slot for `Cam2`.

**Context:**
- `group_001` (Normal: ...07_1) -> Created at T=7s
- `group_002` (Normal: ...10_1) -> Created at T=10s
- `CamX` (...084.bmp) -> Assigned to `group_001` (Diff 6.0s)
- `Cam2` (...299.bmp, T=13.3s) -> Assigned to `group_002` (Diff 3.3s)
- User expects `Cam2` to fill `group_001` first if empty.

**Requirement:**
- Investigate why `group_001` was skipped.
- confirm if this is a bug or expected behavior based on configuration (e.g., Time Difference limit).

## 2. Key Questions for Research
- **Q1:** Where in the code is the "Camera" label generated, and why does it fail to distinguish specific cameras?
- **Q2:** specific logic governs group selection? Was `group_001` rejected due to Time Difference (MaxDelay), Priority, or another constraint?
- **Q3:** Why was there no failure log (`[매칭제외]`) for `group_001` if it was rejected? Are there code paths that skip candidates silently?
