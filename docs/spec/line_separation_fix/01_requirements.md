# Requirements: Line-Separated Group Management

## 1. Overview
The current `GroupManager` handles file groups (Normal, NIR, Cameras) in a single unified collection (`_activeGroups`). This has led to critical issues where data from "Line 1" and "Line 2" are mixed, causing incorrect group matching (e.g., Line 2 Camera matching to a Line 1 Group) and sequence enforcement failures.
The goal is to strictly separate the management of Line 1 and Line 2 groups to ensure data integrity and correct matching behavior.

## 2. Problem Statement
- **Mixed Grouping**: Line 2 cameras (e.g., Cam4) are occasionally matched to Line 1 groups (e.g., created from `..._0` folder), or vice versa.
- **Shared State Conflict**: `_lastAssignedGroup` keys are based on `DataType` (e.g., `Normal`), which is shared across lines. This causes "Sequence Check" logic to enforce order across lines incorrectly (e.g., Line 2 group must be "after" the last Line 1 group).
- **Log Ambiguity**: Logs do not always clearly distinguish which line context is performing the operation.

## 3. Goals
1.  **Strict Isolation**: Line 1 and Line 2 matching processes must be completely isolated. A Line 1 file must NEVER be matched to a Line 2 group.
2.  **Independent Sequencing**: The "Last Assigned Group" logic must track sequence independently for each line.
3.  **Clear Logging**: Log messages must explicitly state the Line context.

## 4. Functional Requirements

### 4.1 Group Management
- The system MUST maintain separate collections for active groups for Line 1 and Line 2.
- Alternatively, search/matching scope MUST be strictly filtered by Line ID with no possibility of cross-over.

### 4.2 Matching Logic
- `DetermineLineNumber` logic must be verified and strictly enforced before any matching attempts.
- When matching a new file (e.g., Camera, NIR), the system must ONLY search candidates within the same Line.
- `_lastAssignedGroup` state must be scoped by Line (e.g., `Dictionary<(DataType, LineNumber), GroupId>`).

### 4.3 Data Structures
- Modify `GroupManager` to support multi-line strict separation.
- Verify `NormalFolderHelper` uses correct suffix/path logic (`_0` -> Line 1, `_1` -> Line 2).

## 5. Non-Functional Requirements
- **Backward Compatibility**: Existing configuration files should work without major schema changes if possible.
- **Performance**: Splitting the search space should not degrade performance; it may improve it by reducing candidate count.

## 6. Constraints
- The solution should fit within the existing `GroupManager` architecture (refactoring internal state) rather than rewriting the entire engine, if possible.
