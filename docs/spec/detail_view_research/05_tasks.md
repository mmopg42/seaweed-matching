---
Task: detail_view_research
Created: 2026-01-14
Status: In Progress
Depends On: 04_design.md
---

# Detail View Removal (Keep Image Click Preview) - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Setup | 0 | 0 | 0 |
| Core | 4 | 0 | 4 |
| Integration | 0 | 0 | 0 |
| Documentation | 1 | 0 | 1 |
| Verification | 4 | 0 | 4 |
| **Total** | **9** | **0** | **9** |

---

## Phase 1: Setup

None required for this task.

---

## Phase 2: Core Implementation

### 2.1 FileGroupDataGrid Input Binding Removal

- [ ] Modify `ChronoView/UI/Controls/FileGroupDataGrid.xaml`
  - [ ] Remove `MouseBinding` for `LeftDoubleClick` associated with `OpenDetailViewCommand`.
- [ ] Verify build passes.

**Verify**:
```bash
dotnet build ChronoView/ChronoView.csproj
```

### 2.2 MainWindow Loop Cleanup

- [ ] Modify `ChronoView/MainWindow.xaml`
  - [ ] Remove `<views:DetailPreviewView ... />` overlay.
- [ ] Modify `ChronoView/MainWindow.xaml.cs`
  - [ ] Remove `FileGroupRow_MouseDoubleClick` handler (dead code).
- [ ] Verify build passes.

**Verify**:
```bash
dotnet build ChronoView/ChronoView.csproj
```

### 2.3 ViewModel Cleanup

- [ ] Modify `ChronoView/UI/ViewModels/MainWindowViewModel.cs`
  - [ ] Remove `DetailPreviewVM` property.
  - [ ] Remove `OpenDetailViewCommand` property.
  - [ ] Remove `ExecuteOpenDetailView` method.
  - [ ] **CRITICAL**: Do NOT touch `OpenImagePreviewCommand` or `ExecuteOpenImagePreview`.
- [ ] Verify build passes.

**Verify**:
```bash
dotnet build ChronoView/ChronoView.csproj
```

### 2.4 Component Deletion

- [ ] Delete `ChronoView/UI/ViewModels/DetailPreviewViewModel.cs`
- [ ] Delete `ChronoView/UI/Views/DetailPreviewView.xaml`
- [ ] Delete `ChronoView/UI/Views/DetailPreviewView.xaml.cs`
- [ ] Verify build passes (ensures no other references existed).

**Verify**:
```bash
dotnet build ChronoView/ChronoView.csproj
```

---

## Phase 3: Integration

No new integration required. Removal task.

---

## Phase 4: Documentation

### 4.1 Update Research Doc

- [ ] Update `docs/spec/detail_view_research/02_research.md`
  - [ ] Add note that removal is complete.
  - [ ] Confirm "MUST KEEP" items were preserved.

---

## Phase 5: Final Verification

### 5.1 Test Suite

- [ ] Run all tests to ensure no regression.

```bash
dotnet test ChronoView.Tests/ChronoView.Tests.csproj
```

### 5.2 Manual Verification

- [ ] **Test 1: Double-click Row**
  - [ ] Launch App -> Load Data -> Double-click row text -> Verify **NOTHING** happens (no overlay).
- [ ] **Test 2: Click Image**
  - [ ] Launch App -> Load Data -> Click Camera/NIR image -> Verify **Image Preview Window OPENS**.

### 5.3 Success Criteria Check

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Double-click no longer opens detail overlay | ⬜ | Manual Test 1 |
| Image click preview still works | ⬜ | Manual Test 2 |
| Identify and remove files/bindings/commands | ⬜ | Build & Grep |
| Document findings and boundaries | ⬜ | 02_research.md updated |

---

## Completion Log

| Task | Completed | Duration | Notes |
|------|-----------|----------|-------|
| ... | ... | ... | ... |

---

## Blockers & Issues

| Issue | Impact | Resolution |
|-------|--------|------------|
| ... | ... | ... |
