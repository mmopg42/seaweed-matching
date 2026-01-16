---
Task: tab_sample_move_settings
Created: 2026-01-08
Status: Draft
Depends On: 01_requirements.md, 02_research.md
---

# Tab-Specific Sample Move Settings - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| Dynamic UI for Line 1/2 | DataTemplateSelector + ContentControl | Switch tabs, verify UI fields update |
| Combined View | CombinedMoveSettingsTemplate | Switch to Combined tab, verify 2-column layout |
| Independent Persistence | LineMoveSettings model in AppConfig | Restart app, verify values persist per line |
| Correct Move Execution | Branched logic in ExecuteMove | Run "Move", verify output log for correct subject/count |
| Correct Delete Execution | Branched logic in ExecuteDelete | Run "Delete", verify quarantine path uses correct SampleName |
| Sequential Combined Move | Sequential async calls in ViewModel | Run "Move" in Combined, verify log order |
| Legacy Clean-up | Deletion of old fields & methods | Compile check (FileOperationViewModel Clean) |

---

## 1. Architecture Overview

### 1.1 System Context

The "Sample Move" feature allows users to move processed files to an output directory. Currently, it uses a single global setting. This change introduces context-aware settings that change based on the active production line (Tab).

### 1.2 Component Diagram

```
┌─────────────────────────────┐
│      MainWindowViewModel    │
│ (ActiveTabIndex, Settings)  │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐      ┌─────────────────────┐
│       WorkflowPanel         │◄────►│ DataTemplateSelector│
│      (ContentControl)       │      └──────────┬──────────┘
└─────────────────────────────┘                 │
                                                ▼
                                     ┌─────────────────────┐
                                     │    DataTemplates    │
                                     │ (Line1/Line2/Comb)  │
                                     └─────────────────────┘
```

---

## 2. Components

### 2.1 New Components

| Component | Type | Location | Purpose |
|-----------|------|----------|---------|
| `LineMoveSettings` | Class | `Models/LineMoveSettings.cs` | Data model for per-line settings |
| `SampleMoveSettingsTemplateSelector` | Class | `UI/Controls/SampleMoveSettingsTemplateSelector.cs` | Selects UI template based on ActiveTabIndex |

### 2.2 Modified Components

| Component | Location | Changes | Breaking Change? |
|-----------|----------|---------|------------------|
| `ApplicationConfiguration` | `Models` | Add `Line1/2Settings`, remove legacy | **Yes** (Generic settings lost) |
| `MainWindowViewModel` | `ViewModels` | Add line-specific logic, update Move/Delete flows | No |
| `FileOperationViewModel` | `ViewModels` | Remove `SaveLimitsAsync` legacy saving | No |
| `WorkflowPanel.xaml` | `UI` | Replace static fields with ContentControl | No |

### 2.3 Deleted Components

| Component | Location | Reason | Migration |
|-----------|----------|--------|-----------|
| `MatchingSettings.SampleName` etc. | `Models` | Replaced by LineMoveSettings | Users re-enter data |

---

## 3. Interface Definitions

### 3.1 LineMoveSettings

```csharp
public class LineMoveSettings {
    public string? SampleName { get; set; }
    public int? MoveNir { get; set; }
    public int? MoveAllData { get; set; }
}
```

### 3.2 MatchingSettings (Config)

```csharp
public class MatchingSettings {
    // New
    public LineMoveSettings Line1Settings { get; set; }
    public LineMoveSettings Line2Settings { get; set; }
    
    // Deleted: SampleName, MoveNir, MoveAllData
}
```

---

## 4. Key Design Decisions

### 4.1 UI Switching Mechanism

**Context**: Need to show different input fields based on the selected tab (Line 1 vs Line 2 vs Combined).

**Options Considered**:
| Option | Pros | Cons |
|--------|------|------|
| A. Visibility Converters | Simple to implement initially | XAML becomes cluttered with 3 copies of fields |
| B. DataTemplateSelector | Clean XAML, reusable templates | Requires a selector class |

**Decision**: Option B (DataTemplateSelector)
**Rationale**: Keeps `WorkflowPanel.xaml` readable and scalable. Combined view is fundamentally different (2 columns) from single views, making templates cleaner.

### 4.2 Settings Persistence

**Context**: Settings change frequently (typing). Need to save without freezing UI.

**Decision**: Shared Debounce Timer
**Rationale**: Whether user types in Line 1 or Line 2 fields, we just need to save "Configuration". A single 500ms debounce timer that saves the entire config object is sufficient and avoids race conditions.

### 4.3 Delete Operation Branching

**Context**: The "Delete" operation (Quarantine) builds a file path using a `subject` (Sample Name). If this is missing/wrong, files go to "UnknownSubject" folder.

**Decision**: Branch Delete Logic by Tab
**Rationale**: Like "Move", "Delete" must use the Sample Name corresponding to the files being deleted.
- Line 1 Tab -> Use `Line1SampleName`
- Line 2 Tab -> Use `Line2SampleName`
- Combined -> Execute Line 1 then Line 2 separately with correct names.

---

## 5. Configuration

### 5.1 New Configuration Keys

`settings.json` structure changes:

```json
"MatchingSettings": {
  "Line1Settings": { "SampleName": "...", "MoveNir": 10, "MoveAllData": 100 },
  "Line2Settings": { "SampleName": "...", "MoveNir": 20, "MoveAllData": 200 }
  // Old "SampleName" etc. removed
}
```

---

## 6. External Dependencies

None.

---

## 7. Glossary Updates

### Existing Terms Check

| Checked | Existing Term | Relevance |
|---------|---------------|-----------|
| [x] | `FileGroup` | Represents the data items being moved |
| [x] | `Dashboard` | Holds the collections of FileGroups |

### New Terms to Add

| Term | Type | Definition |
|------|------|------------|
| `LineMoveSettings` | Class | Settings model specifically for move limits/naming per line |

---

## 8. Architecture Documentation Plan

### Documents to Update

| Document | Changes |
|----------|---------|
| `design_guidelines.md` | (Optional) Add note about TemplateSelectors if this becomes a pattern |

---

## 9. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| User confusion on "Combined" move | Med | Med | Add explicit confirmation dialog showing summary of BOTH lines |
| Data loss (old settings) | High | Low | Accepted constraint. Announce in release notes. |

---

## 10. Open Questions

- [x] Q: Should we support "Draft" settings? A: No, instant save (debounced) is the existing pattern.

---

## Approval

- [ ] All requirements traced to components
- [ ] Component interfaces defined
- [ ] Design decisions documented with rationale
- [ ] No open questions

**Next Step**: 04_design.md
