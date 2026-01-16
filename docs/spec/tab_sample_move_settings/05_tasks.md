---
Task: tab_sample_move_settings
Created: 2026-01-08
Status: In Progress
Depends On: 04_design.md
---

# Tab-Specific Sample Move Settings - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Setup | 3 | 3 | 0 |
| Core | 2 | 2 | 0 |
| UI | 2 | 2 | 0 |
| Integration | 2 | 2 | 0 |
| Verification | 5 | 0 | 5 |
| **Total** | **14** | **9** | **5** |

---

## Phase 1: Setup

### 1.1 Model Updates

- [x] Update `ApplicationConfiguration.cs`
  - [x] Add `LineMoveSettings` class
  - [x] Update `MatchingSettings` class (Add Line1/2Settings, mark legacy as Obsolete)
- [x] Verify build errors (legacy fields marked Obsolete, build succeeds with warnings)

**Verify**: ✅ Build successful with deprecated warnings for legacy properties.

### 1.2 Resource Updates

- [x] Add/Verify string resources in `Strings.resx` (reusing existing labels).

**Verify**: ✅ Existing labels reused (Sample_Name, Sample_MoveNIR, Sample_MoveAllData).

### 1.3 ViewModel Cleanup

- [x] In `MainWindowViewModel.cs`:
  - [x] Replace `_sampleName`, `_moveNir`, `_moveAllData` with Line1/Line2 versions.
  - [x] Replace `SaveSampleMoveSettingsAsync` with `SaveLineSettingsAsync(int lineNumber)`.
- [x] In `FileOperationViewModel.cs`:
  - [x] `SaveLimitsAsync` remains for backward compatibility (saves limits passed as params).
  - [x] `ExecuteMoveAsync` accepts settings as arguments - no legacy dependency.

**Verify**: ✅ `MainWindowViewModel.cs` uses Line1/Line2 properties. Build succeeds.

---

## Phase 2: Core Implementation

### 2.1 ViewModel Logic

- [x] In `MainWindowViewModel.cs`:
  - [x] Add `Line1SampleName`, `Line1MoveNir`, `Line1MoveAllData` properties.
  - [x] Add `Line2SampleName`, `Line2MoveNir`, `Line2MoveAllData` properties.
  - [x] Implement `SaveLineSettingsAsync(int lineNumber)` (Debounced).
  - [x] Update `LoadSettings` to load Line1/2 settings from config.
  - [x] Add `IsLine1Tab`, `IsLine2Tab`, `IsCombinedTab`, `SampleMoveSettingsHeader`.

**Verify**: ✅ ViewModel compiles. Properties trigger `OnPropertyChanged` on tab switch.

### 2.2 Delete Service Logic (Ref check)

- [x] Check `FileOperationViewModel.cs` for legacy usages - none found.
- [x] `ExecuteMoveAsync` / `ExecuteDeleteAsync` accept settings as arguments.

**Verify**: ✅ No build errors in `FileOperationViewModel`.

---

## Phase 3: UI Implementation

### 3.1 Template Selector

- [x] Used visibility bindings instead of DataTemplateSelector for simplicity.
- [x] `IsLine1Tab`, `IsLine2Tab`, `IsCombinedTab` bound to Visibility with BoolToVisibility converter.

**Verify**: ✅ Simplified approach avoids need for separate TemplateSelector class.

### 3.2 XAML Updates

- [x] Update `WorkflowPanel.xaml`:
  - [x] Replaced single settings section with tab-specific panels.
  - [x] Line1 settings visible when `IsLine1Tab` is true.
  - [x] Line2 settings visible when `IsLine2Tab` is true.
  - [x] Combined settings (both Line1 and Line2) visible when `IsCombinedTab` is true.
  - [x] Header bound to `SampleMoveSettingsHeader` for dynamic labeling.

**Verify**: ✅ UI compiles. No binding errors expected.

---

## Phase 4: Integration

### 4.1 Move Logic

- [x] In `MainWindowViewModel.cs`:
  - [x] Update `ExecuteMoveWithConfirmation` to branch by tab.
  - [x] Implement `ExecuteSingleLineMoveAsync` helper.
  - [x] Implement `ExecuteCombinedMoveAsync` for Combined tab sequential logic.

**Verify**: ✅ Code implemented. Move dialog shows line-specific info.

### 4.2 Delete Logic

- [x] In `MainWindowViewModel.cs`:
  - [x] Update `ExecuteDeleteWithConfirmation` to branch by tab.
  - [x] **Critical**: Line 1 uses `Line1SampleName`, Line 2 uses `Line2SampleName`.
  - [x] **Critical**: Combined tab executes delete sequentially for both lines, filtering by `LineNumber`.

**Verify**: ✅ Delete logic correctly passes line-specific sample names.

---

## Phase 5: Verification

### 5.1 Manual Verification

- [ ] 5.1.1 Verify Line 1 settings save/load.
- [ ] 5.1.2 Verify Line 2 settings save/load.
- [ ] 5.1.3 Verify Combined tab UI shows both.
- [ ] 5.1.4 Verify "Move" logs correct subject/counts.
- [ ] 5.1.5 Verify "Combined Move" follows sequence.

---

## Completion Log

| Task | Completed | Notes |
|------|-----------|-------|
| Phase 1.1 Model Updates | 2026-01-08 | LineMoveSettings class added, legacy marked Obsolete |
| Phase 1.2 Resource Updates | 2026-01-08 | Reused existing string resources |
| Phase 1.3 ViewModel Cleanup | 2026-01-08 | Replaced with Line1/Line2 properties |
| Phase 2.1 ViewModel Logic | 2026-01-08 | All properties and helpers implemented |
| Phase 2.2 Delete Service | 2026-01-08 | No changes needed |
| Phase 3.1 Template Selector | 2026-01-08 | Used visibility bindings instead |
| Phase 3.2 XAML Updates | 2026-01-08 | WorkflowPanel updated with tab-aware UI |
| Phase 4.1 Move Logic | 2026-01-08 | Tab-branching and sequential Combined move |
| Phase 4.2 Delete Logic | 2026-01-08 | Tab-branching with correct SampleName per line |

---

## Approval

- [ ] All tasks completed
- [ ] All verifications pass
- [ ] Success criteria met

**Next Step**: Manual Verification (Phase 5)
