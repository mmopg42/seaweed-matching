---
Task: fix_normal_suffix_usage
Created: 2026-01-08
Status: Draft
Depends On: 01_requirements.md
---

# Fix Normal Folder Suffix Usage - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| `InitialScanner` ignores suffixes when `UseFolderSuffix` is false | Modified `InitialScanner.ScanFilesForDataTypeAsync` | Manual test: Initial scan with setting disable/enable |
| `FileWatcherService` ignores suffixes when `UseFolderSuffix` is false | Modified `FileWatcherService.IsNormalFolderName` signature | Unit test: `IsNormalFolderName` logic |
| `GroupManager` determines line numbers correctly | Modified `GroupManager.DetermineLineNumber` | Integration test and manual verification |
| `FileGroup` helper methods respect setting | Modified `FileGroup.GetLineNumberFromNormalFolder` | Unit test |
| `StatisticsService` counts correctly | Modified `StatisticsService.GetFileCountsAsync` | UI verification of stats count |
| `FileMatchingEngine` line logic updated | Modified `FileMatchingEngine` signatures and logic | System integration test |
| `UseFolderSuffix` comments updated | Modified `ApplicationConfiguration` | Code review |

## 1. Architecture Overview

### 1.1 System Context

The system relies on identifying "Normal" data folders and assigning them to Line 1 or Line 2. This assignment happens during initial scanning, background polling, and real-time watching. Currently, inconsistent logic (some using suffixes, some hardcoded) causes failures when user configuration differs from assumptions.

### 1.2 Data Flow

```
[Configuration (UseFolderSuffix)]
       │
       ▼
[InitialScanner] ──► [FileMatchingEngine] ──► [GroupManager] ──► [UI]
       │                    ▲
       ▼                    │
[FileWatcherService] ───────┘
```

All components must now explicitly consume `UseFolderSuffix` to decide whether to check for `_0`/`_1` or rely purely on source path matching.

## 2. Components

### 2.1 Modified Components

| Component | Location | Changes | Breaking Change? |
|-----------|----------|---------|------------------|
| `ApplicationConfiguration` | `Models/ApplicationConfiguration.cs` | Update comments only | No |
| `InitialScanner` | `Core/FileWatching/InitialScanner.cs` | Pass config to `ScanAndSortFilesAsync` and filter logic | No |
| `FileWatcherService` | `Core/FileWatching/FileWatcherService.cs` | Update `IsNormalFolderName` to accept config/bool | No |
| `GroupManager` | `Core/FileWatching/GroupManager.cs` | Refactor `DetermineLineNumber` to use config | No |
| `FileGroup` | `Models/FileGroup.cs` | Update `GetLineNumberFromNormalFolder` to accept bool | No |
| `StatisticsService` | `Core/Analytics/StatisticsService.cs` | Condition suffix filter on config | No |
| `FileMatchingEngine` | `Core/FileMatching/FileMatchingEngine.cs` | Add `UseFolderSuffix` to strategies | No |
| `MonitoringOrchestrator` | `Core/FileWatching/MonitoringOrchestrator.cs` | Pass config settings to sub-components | No |

## 3. Interface Definitions

### 3.1 FileWatcherService

```csharp
private bool IsNormalFolderName(string name, bool useSuffix)
{
    // Checks basic pattern
    // If useSuffix=true, checks for _0/_1
    // If useSuffix=false, accepts basic pattern
}
```

### 3.2 FileGroup

```csharp
public static int GetLineNumberFromNormalFolder(string folderName, bool useSuffix, string parentPath = null, string normal1Path = null, string normal2Path = null)
{
    // Logic split based on useSuffix
}
```

### 3.3 FileMatchingEngine

```csharp
// Update BuildLineGroups signature to include config/UseFolderSuffix
private static List<FileGroup> BuildLineGroups(...)
```

## 4. Key Design Decisions

### 4.1 Path-Based Line Determination (When Suffix Disabled)

**Context**: When `UseFolderSuffix` is false, `_0` and `_1` cannot be relied upon.
**Decision**: Use `Path.StartsWith` comparison with `Normal1Path` and `Normal2Path`.
**Rationale**: This is the only distinguishing feature available when naming conventions don't separate lines.

### 4.2 Centralized vs Distributed Logic

**Context**: Line determination logic is repeated in 4-5 places.
**Decision**: We will fix each location in place first to minimize regression risk, rather than a massive refactor to a single helper, but we will make `FileGroup.GetLineNumberFromNormalFolder` more robust to serve as a shared utility where possible.

## 5. Configuration

No new keys. We are simply properly using `MatchingSettings.UseFolderSuffix`.

## 6. Glossary Updates

None.

## 7. Open Questions

- [x] Are there mixed states (Suffix enabled for Line 1 but not Line 2)? → No, global boolean.

---

## Approval

- [ ] Requirements traced
- [ ] Interfaces defined
- [ ] Design decisions documented

**Next Step**: 04_design.md
