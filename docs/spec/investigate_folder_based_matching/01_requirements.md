---
Task: investigate_folder_based_matching
Created: 2026-01-13
Status: Draft
Summary: Investigate why file matching is triggered by folder creation 
Research Required: Yes
---

# Investigate Folder Based Matching - Requirements

## 1. Goal

### 1.1 Primary Goal
Understand and document the root cause of why the matching logic uses the folder creation timestamp instead of waiting for the actual image file, leading to potential issues when images are added late.

### 1.2 Success Criteria
- [ ] Root cause identified in the codebase (MonitoringOrchestrator, FileWatcher, etc.).
- [ ] Explanation provided for "folder matches first, image comes later".
- [ ] Research document (02_research.md) created and answers the questions.

## 2. Constraints

### 2.1 Technical Constraints
- Must not modify code during this phase.
- Must use existing logging or code analysis.

### 2.2 Business Constraints
- Investigation only.

### 2.3 Non-Goals (Out of Scope)
- Fixing the issue (this is a research task).
- Refactoring the entire matching engine.

## 3. Questions to Investigate

- [ ] Q1: How does `FileWatcherService` distinguish between folder creation and file creation events?
- [ ] Q2: Does `FileMatchingEngine` or `FileGroupMatcherService` use the folder name to extract timestamps for `Normal` cameras?
- [ ] Q3: Is there a specific logic that triggers "Group Creation" immediately upon folder detection?
- [ ] Q4: How does the system handle "Empty" folders that are matched? Do they wait for files?

## 4. Assumptions
- The user is observing a behavior where an empty folder creates a match group.
- The "General Camera" creates a folder per timestamp (or similar structure).

## 5. Dependencies
None.
