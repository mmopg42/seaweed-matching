---
Task: remove_nir_spectrum_monitor
Created: 2025-12-23
Status: Draft
Depends On: 03_plan.md
---

# Remove NIR Spectrum Monitor - Detailed Design

## 1. Component Modifications

### 1.1 App.xaml.cs

#### Modification

Remove line 217: `services.AddSingleton<NirSpectrumMonitor>();`

#### Detailed Logic

```pseudo
// CURRENT (Line 217):
services.AddSingleton<NirSpectrumMonitor>();

// DELETE THIS LINE
// No replacement needed - simply remove
```

#### Verification

Build succeeds and DI container no longer attempts to resolve `NirSpectrumMonitor`.

---

### 1.2 SetupWindowViewModel.cs

#### Modifications

Remove:
1. Line 28: Field declaration `private readonly NirSpectrumMonitor? _nirSpectrumMonitor;`
2. Line 53: Constructor parameter `NirSpectrumMonitor nirSpectrumMonitor`
3. Line 61: Field assignment `_nirSpectrumMonitor = nirSpectrumMonitor;`
4. Lines 185-212: Method `ExecuteNirSpectrumMonitorAsync()`

#### Detailed Logic

```csharp
// BEFORE (Constructor):
public SetupWindowViewModel(
    ILogger<SetupWindowViewModel> logger,
    ApplicationConfiguration config,
    IServiceProvider serviceProvider,
    GeneralCameraLauncher generalCameraLauncher,
    NirCameraLauncher nirCameraLauncher,
    Nir2CameraLauncher nir2CameraLauncher,
    NirSpectrumMonitor nirSpectrumMonitor)  // ← REMOVE THIS PARAMETER
{
    // ... other assignments ...
    _nirSpectrumMonitor = nirSpectrumMonitor;  // ← REMOVE THIS LINE
}

// AFTER (Constructor):
public SetupWindowViewModel(
    ILogger<SetupWindowViewModel> logger,
    ApplicationConfiguration config,
    IServiceProvider serviceProvider,
    GeneralCameraLauncher generalCameraLauncher,
    NirCameraLauncher nirCameraLauncher,
    Nir2CameraLauncher nir2CameraLauncher)
{
    // ... other assignments only ...
}
```

#### Impact Analysis

- **Field removal**: No impact (field was never accessed except in unused method)
- **Constructor parameter removal**: No breaking change (DI will no longer inject it)
- **Method removal**: No impact (method was never called - no command binding exists)

---

### 1.3 NirSpectrumMonitor.cs

#### Modification

Delete entire file.

#### Detailed Logic

```bash
# Simply delete the file
Remove-Item ChronoView/Core/NIR/NirSpectrumMonitor.cs
```

---

## 2. Integration Points

### 2.1 DI Container → SetupWindowViewModel

#### Before

```
App.xaml.cs (DI registration)
    ├─► NirSpectrumMonitor (singleton)
    └─► SetupWindowViewModel (receives NirSpectrumMonitor)
```

#### After

```
App.xaml.cs (DI registration)
    └─► SetupWindowViewModel (no NirSpectrumMonitor dependency)
```

#### Error Handling

No errors expected - removing unused dependency is safe.

---

## 3. Edge Cases & Boundary Conditions

| Case | Scenario | Expected Behavior |
|------|----------|-------------------|
| DI resolution | SetupWindow created | No errors, dependency not requested |
| Existing filtering | User toggles NIR filtering | Works normally (uses Nir2CameraLauncher) |
| Build | Compile project | Succeeds with no warnings |
| Grep search | Search for `NirSpectrumMonitor` | Returns 0 results |

---

## 4. Resource Management

No resource management changes needed - this is pure deletion.

---

## 5. Performance Considerations

| Impact | Before | After | Improvement |
|--------|--------|-------|-------------|
| DI startup | Registers unused singleton | One less registration | Negligible (~0.1ms) |
| Memory | Allocates unused object | Not allocated | Negligible (~few KB) |

---

## 6. Testing Strategy

### 6.1 Build Verification

| Test Name | Command | Expected |
|-----------|---------|----------|
| test_build_succeeds | `dotnet build` | Exit code 0, no errors |
| test_no_warnings | `dotnet build` | No CS warnings related to removed code |

### 6.2 Code Search Verification

| Test Name | Command | Expected |
|-----------|---------|----------|
| test_no_references | `grep -rn "NirSpectrumMonitor" ChronoView/` | 0 results |
| test_file_deleted | `Test-Path ChronoView/Core/NIR/NirSpectrumMonitor.cs` | False |

### 6.3 Manual Verification Steps

```
1. Setup: Build and run the application
2. Action: Open SetupWindow, click "Toggle NIR Filtering" button
3. Verify: Filtering activates/deactivates correctly
   Expected: Status changes between "Activated" (green) and "Deactivated" (red)
```

---

## 7. Security Considerations

None - this is code deletion with no security impact.

---

## 8. Open Questions

- [x] All design questions resolved

---

## Approval

- [x] All components have detailed pseudo-code
- [x] Error handling specified for all failure modes
- [x] State management documented (N/A)
- [x] Thread safety addressed (N/A)
- [x] Edge cases covered
- [x] Test cases defined
- [x] No open questions

**Next Step**: 05_tasks.md
