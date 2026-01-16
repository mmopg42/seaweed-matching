# Technical Debt: GroupManager.cs Refactoring

## Issue Summary
`GroupManager.cs` exceeds the project's file size guidelines and requires refactoring.

## Current State
- **File**: `ChronoView/Core/FileWatching/GroupManager.cs`
- **Lines of Code**: ~735 LoC
- **Status**: 🔶 Review Required (exceeds 600-line soft limit, approaching 800-line hard limit)

## Problem
The file has grown to include multiple responsibilities:
1. Group creation and lifecycle management
2. File matching logic (3 different match types)
3. NIR file handling (pending queue management)
4. Timestamp extraction and comparison
5. Logging and event raising

## Proposed Solution
Split into smaller, focused classes:

### Option A: Extract Matching Logic
```
GroupManager.cs (Core lifecycle)
├── IGroupMatcher.cs (interface)
└── GroupMatcher.cs (FindMatchingExistingGroup logic)
```

### Option B: Strategy Pattern for Match Types
```
GroupManager.cs (Orchestrator)
├── INormalFolderMatcher.cs
├── INirKeyMatcher.cs
└── ISequenceMatcher.cs
```

## Priority
- **Severity**: Medium
- **Risk if Deferred**: Code becomes harder to maintain; new features will increase file size further

## Related
- Created during: `line_separation_fix` spec implementation
- Date: 2026-01-08

## Action Items
- [ ] Create spec folder `docs/spec/groupmanager_refactoring/`
- [ ] Document current responsibilities in detail
- [ ] Design new class structure
- [ ] Implement with full test coverage
