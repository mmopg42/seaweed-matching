---
Task: remove_nir_spectrum_monitor
Created: 2025-12-23
Status: Draft
Depends On: 04_design.md
---

# Remove NIR Spectrum Monitor - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------| |
| Core | 3 | 0 | 3 |
| Verification | 3 | 0 | 3 |
| **Total** | **6** | **0** | **6** |

---

## Phase 1: Core Implementation

### 1.1 Remove DI Registration

- [ ] Open `ChronoView/App.xaml.cs`
- [ ] Locate line 217: `services.AddSingleton<NirSpectrumMonitor>();`
- [ ] Delete this line
- [ ] Save file

**Verify**: 
```bash
grep -n "NirSpectrumMonitor" ChronoView/App.xaml.cs
# Expected: No results
```

---

### 1.2 Remove References from SetupWindowViewModel

- [ ] Open `ChronoView/UI/ViewModels/SetupWindowViewModel.cs`
- [ ] Delete line 28: `private readonly NirSpectrumMonitor? _nirSpectrumMonitor;`
- [ ] Delete from constructor parameter (line 53): `NirSpectrumMonitor nirSpectrumMonitor`
- [ ] Delete from constructor body (line 61): `_nirSpectrumMonitor = nirSpectrumMonitor;`
- [ ] Delete method (lines 185-212): `ExecuteNirSpectrumMonitorAsync()`
- [ ] Remove unused `using` statement if it exists for `ChronoView.Core.Nir` (only if no other Nir types are used)
- [ ] Save file

**Verify**:
```bash
grep -n "NirSpectrumMonitor" ChronoView/UI/ViewModels/SetupWindowViewModel.cs
# Expected: No results
```

---

### 1.3 Delete NirSpectrumMonitor File

- [ ] Delete file: `ChronoView/Core/NIR/NirSpectrumMonitor.cs`

**Verify**:
```powershell
Test-Path ChronoView/Core/NIR/NirSpectrumMonitor.cs
# Expected: False
```

---

## Phase 2: Verification

### 2.1 Build Verification

- [ ] Build the solution
- [ ] Verify no build errors
- [ ] Verify no build warnings related to removed code

```bash
dotnet build
# Expected: Build succeeded, 0 Error(s), 0 Warning(s) (or no new warnings)
```

---

### 2.2 Code Search Verification

- [ ] Search entire codebase for remaining references

```bash
# Windows PowerShell:
Get-ChildItem -Path . -Recurse -Include *.cs,*.xaml | Select-String "NirSpectrumMonitor" -List

# Expected: No results
```

---

### 2.3 Manual Functional Test

- [ ] Run the application
- [ ] Open SetupWindow
- [ ] Click "Toggle NIR2 Filtering" button
- [ ] Verify filtering activates (status shows "Activated" in green)
- [ ] Click "Toggle NIR2 Filtering" again
- [ ] Verify filtering deactivates (status shows "Deactivated" in red)

**Expected**: NIR filtering functionality works exactly as before (uses `Nir2CameraLauncher` → `NirSpectrumFilter`)

---

## Phase 3: Success Criteria Check

| Criterion (from requirements) | Status | Evidence |
|-------------------------------|--------|----------|
| `NirSpectrumMonitor.cs` file deleted | ⬜ | `Test-Path` returns False |
| DI registration removed | ⬜ | Grep shows no results |
| References removed from ViewModel | ⬜ | Grep shows no results |
| Application builds successfully | ⬜ | `dotnet build` succeeds |
| NIR filtering remains intact | ⬜ | Manual test passes |
| No unused code remains | ⬜ | Codebase grep returns 0 hits |

---

## Completion Log

| Task | Completed | Duration | Notes |
|------|-----------|----------|-------|
| 1.1 DI Registration | - | - | - |
| 1.2 ViewModel Cleanup | - | - | - |
| 1.3 File Deletion | - | - | - |
| 2.1 Build | - | - | - |
| 2.2 Code Search | - | - | - |
| 2.3 Manual Test | - | - | - |

---

## Blockers & Issues

| Issue | Impact | Resolution |
|-------|--------|------------|
| - | - | - |

---

## Approval

- [ ] All tasks completed
- [ ] All verifications pass
- [ ] Success criteria met
- [ ] Ready for 06_report.md

**Next Step**: Implementation, then 06_report.md
