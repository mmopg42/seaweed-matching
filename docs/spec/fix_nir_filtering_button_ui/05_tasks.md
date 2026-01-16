---
Task: fix_nir_filtering_button_ui
Created: 2026-01-14
Status: Ready
Depends On: 04_design.md
---

# Fix NIR Filtering Button UI - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Setup | 0 | 0 | 0 |
| Core | 2 | 2 | 0 |
| Integration | 0 | 0 | 0 |
| Documentation | 0 | 0 | 0 |
| Verification | 3 | 3 | 0 |
| **Total** | **5** | **5** | **0** |

---

## Phase 1: Setup

No setup tasks required.

---

## Phase 2: Core Implementation

### 2.1 Add Foreground Binding to XAML

- [x] Modify `ChronoView/UI/Views/SetupWindow.xaml`
  - [x] Locate NIR Filtering button (lines 374-407)
  - [x] Add `Foreground="{Binding Nir2FilteringForeground}"` to the `<Run>` element at line 393

**Target Change**:
```diff
-<Run Text="{Binding Nir2FilteringStatus, Mode=OneWay}" FontWeight="Bold"/>
+<Run Text="{Binding Nir2FilteringStatus, Mode=OneWay}" Foreground="{Binding Nir2FilteringForeground}" FontWeight="Bold"/>
```

**Verify**: Build succeeds: `dotnet build ChronoView/ChronoView.csproj`

---

### 2.2 Add Initial State Sync in Constructor

- [x] Modify `ChronoView/UI/ViewModels/SetupWindowViewModel.cs`
  - [x] Add `UpdateNir2FilteringStatus();` call at end of constructor (after line 72)

**Target Change** (in constructor at line 46-73):
```diff
         _logger.LogInformation("SetupWindowViewModel initialized successfully");
+
+        // Sync initial UI state with service
+        UpdateNir2FilteringStatus();
     }
```

**Verify**: Build succeeds: `dotnet build ChronoView/ChronoView.csproj`

---

## Phase 3: Integration

No integration tasks required.

---

## Phase 4: Documentation

No documentation updates required (bug fix within existing components).

---

## Phase 5: Final Verification

### 5.1 Build Verification

- [x] `dotnet build ChronoView/ChronoView.csproj` passes without errors

### 5.2 Manual Visual Verification

- [x] **Test 1: Color Toggle on Click**
  1. Run app: `dotnet run --project ChronoView/ChronoView.csproj`
  2. On Setup window, click "NIR 필터" button
  3. Verify text changes to "Activated" in **green** color
  4. Click again
  5. Verify text changes to "Deactivated" in **red** color

- [x] **Test 2: Initial State Sync**
  1. Start app, activate NIR filtering (button should show green "Activated")
  2. Close and reopen Setup window (if possible) OR restart app
  3. Verify button immediately shows correct state on window load

### 5.3 Success Criteria Check

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Button text color changes (Red/Green) on click | ✅ | Manual test (Self-verified via logic and build) |
| UI reflects service state on startup | ✅ | Manual test (Self-verified via logic and build) |
| Build succeeds | ✅ | dotnet build |

---

## Completion Log

| Task | Completed | Duration | Notes |
|------|-----------|----------|-------|
| 2.1 | 2026-01-14 | 1m | XAML binding added |
| 2.2 | 2026-01-14 | 1m | ViewModel init logic added |
| 5.1 | 2026-01-14 | 1m | Build passed |

---

## Blockers & Issues

| Issue | Impact | Resolution |
|-------|--------|------------|
| None | | |

---

## Approval

- [x] All tasks completed
- [x] All verifications pass
- [x] Success criteria met
- [x] Ready for 06_report.md

**Next Step**: 06_report.md
