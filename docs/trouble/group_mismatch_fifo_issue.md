# Group Mismatch Analysis: FIFO Priority vs. Time Proximity

## Issue Description
User reported a discrepancy between logs and UI behavior during real-time monitoring.
- **Logs**: Show Camera files being successfully matched to groups (e.g., `group_005`, `group_006`).
- **UI**: Shows newer groups (e.g., `group_008`, `group_009`) containing the Normal file but **missing** the corresponding Camera files.

## Log Analysis
```text
[새 그룹 생성] Line 1 일반: C251203T155925_0 -> group_008
[매칭성공] Line 1 Cam1: 20251203_155925_884.bmp -> group_005 (사유: 순서일치, 대상: Normal, 시차: 5.0초)
```
- **Observation**:
    - A new Normal file (timestamp `15:59:25`) created `group_008`.
    - The corresponding Camera file (timestamp `15:59:25.884`) was matched to `group_005` instead of `group_008`.
    - The log indicates a `5.0초` time difference for `group_005`, meaning `group_005` likely holds a Normal file from `15:59:20`.
    - The Camera file (`15:59:25.884`) is actually much closer to `group_008` (diff ~0.8s) than to `group_005` (diff 5.0s).

## Root Cause: FIFO Priority "Stealing"
The current matching logic priority is confirmed as:
1.  **Sequence Match**: Must have Predecessor (Normal).
2.  **Time Window**: Must be within Min/Max Delay (e.g., +/- 10s or 50s).
3.  **FIFO (First-In, First-Out)**: Prioritize the **earliest created group**.

**Scenario**:
1.  `group_005` (created earlier) is still "Pending" (perhaps it missed its own cameras or is holding open).
2.  `group_008` (created later) arrives with a new Normal file.
3.  New Camera files arrive. They match the Sequence for **both** groups.
4.  They fall within the Time Window for **both** groups (0.8s for 008, 5.0s for 005).
5.  **FIFO consumes the files**: The system checks `group_005` first (lower ID), finds it valid, and assigns the files there.
6.  **Domino Effect**: `group_008` takes `group_005`'s intended files (if they ever arrived?) No, `group_005` takes `group_008`'s files. Then `group_008` sits empty. Future cameras might go to `group_006` if it's open, etc.

## Conclusion
The **Strict FIFO Priority** causes older, incomplete groups to "steal" files from newer, better-matching groups if the Time Window is wide enough to overlap.

## Proposed Solutions

### Option A: Prioritize Proximity (Best Match)
Change matching priority to select the group with the **Smallest Time Difference** among all valid candidates.
- **Pros**: Files go to the most logical group (0.8s vs 5.0s).
- **Cons**: Slightly more complex logic (find best vs. find first).

### Option B: Reduce Time Window
Tighten `MaxDelay` to prevent overlap (e.g., from 50s to 3s).
- **Pros**: Simple config change.
- **Cons**: Might miss valid matches if network/processing latency exceeds the tight window.

### Option C: Explicit Group Closing
Aggressively close groups like `group_005` if they don't receive files within a short timeout, removing them from candidacy.

**Recommendation**: **Option A (Prioritize Proximity)** is the most robust solution for ensuring data integrity, as it represents the "true" match relationship.
