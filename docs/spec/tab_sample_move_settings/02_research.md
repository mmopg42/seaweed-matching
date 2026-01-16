---
Task: tab_sample_move_settings
Created: 2026-01-08
Status: Approved
Depends On: 01_requirements.md
---

# Tab-Specific Sample Move Settings - Research Findings

> **Note**: This document is migrated from `docs/implementation/탭별_샘플_이동_설정_조사_리포트.md`.

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: How to structure data? | Use `LineMoveSettings` model inside `MatchingSettings`. | High |
| Q2: How to structure UI? | Use `ContentControl` with `DataTemplateSelector` keying off `ActiveTabIndex`. | High |
| Q3: How to execute Combined? | Sequentially execute Line 1 then Line 2 logic. | High |

## 2. Detailed Findings

### 2.1 Data Model Structure

**Current**: Single `SampleName`, `MoveNir`, `MoveAllData` in `MatchingSettings`.
**Proposed**:
```csharp
public class LineMoveSettings {
    public string? SampleName { get; set; }
    public int? MoveNir { get; set; }
    public int? MoveAllData { get; set; }
}

public class MatchingSettings {
    public LineMoveSettings Line1Settings { get; set; } = new();
    public LineMoveSettings Line2Settings { get; set; } = new();
}
```

### 2.2 ViewModel & Save Logic

- **Properties**: `Line1SampleName`, `Line1MoveNir`, etc. backing fields in `MainWindowViewModel`.
- **Save Strategy**: A shared `SaveLineSettingsAsync` method with a single debounce timer (500ms). When timer ticks, save BOTH lines' settings to config. This simplifies concurrency.

### 2.3 UI Implementation

**Choice**: `ContentControl` + `DataTemplateSelector`.
- Keeps XAML clean.
- Avoids complex Visibility converters.
- Templates: `Line1MoveSettingsTemplate`, `Line2MoveSettingsTemplate`, `CombinedMoveSettingsTemplate` (2-column).

### 2.4 Execution Logic

**Move Execution**:
- **Line 1 Tab**: Execute standard move for `Dashboard.Line1Groups` using `Line1Settings`.
- **Line 2 Tab**: Execute standard move for `Dashboard.Line2Groups` using `Line2Settings`.
- **Combined Tab**:
  1. Prompt user with summary of BOTH lines.
  2. Execute Line 1 move.
  3. Execute Line 2 move (Sequential).

**Delete Execution**:
- Delete logic also uses `SampleName` as `subject` ("Quarantine subject").
- Must branch by tab similarly to Move logic to ensure correct subject is used for quarantine paths.

## 3. Code Analysis

### 3.1 Relevant Existing Code

| File | Component | Notes |
|------|-----------|-------|
| `ApplicationConfiguration.cs` | `MatchingSettings` | Remove old fields, add new ones. |
| `MainWindowViewModel.cs` | `LoadSettings`, `SaveSampleMoveSettings` | Refactor significantly. |
| `WorkflowPanel.xaml` | Settings UI | Replace static fields with dynamic content. |
| `FileOperationViewModel.cs` | `ExecuteMoveAsync` | Update to accept line-specific limits if needed, or rely on ViewModel passing them. |

### 3.2 Impact Analysis

- **Breaking Change**: Old settings will be lost. Accepted as per constraints.
- **Risk**: "Delete" operation might accidentally use wrong subject if not carefully branched.

## 4. Recommendations

### Primary Recommendation

Implement `LineMoveSettings` model and `DataTemplateSelector` UI as detailed in the investigation.
Fully remove legacy fields (`SampleName`, etc.) to prevent confusion (Do not keep them as fallbacks).

## 5. References

- Original Report: `docs/implementation/탭별_샘플_이동_설정_조사_리포트.md`
