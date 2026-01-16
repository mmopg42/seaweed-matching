---
Task: fix_nir_filtering_button_ui
Created: 2026-01-14
Status: Draft
Depends On: 03_plan.md
---

# Fix NIR Filtering Button UI - Detailed Design

## 1. Component Designs

### 1.1 SetupWindow.xaml

> UI View for System Setup, containing the NIR filtering control button.

#### Interface (from Plan)
No code interface changes (XAML modification only).

#### Preconditions
- `SetupWindowViewModel` is set as DataContext.
- `Nir2FilteringForeground` property is available in ViewModel.

#### Postconditions
- The "NIR Filtering" button text displays in the color specified by `Nir2FilteringForeground`.

#### Detailed Logic

```xml
<!-- In SetupWindow.xaml -->
<Button Command="{Binding ToggleNirFilteringCommand}" ...>
    <Button.Template>
        <ControlTemplate TargetType="Button">
            <!-- ... border ... -->
            <StackPanel ...>
                <TextBlock Text="⚡" .../>
                <TextBlock VerticalAlignment="Center">
                    <Run Text="NIR 필터: "/>
                    <!-- MODIFICATION START -->
                    <Run Text="{Binding Nir2FilteringStatus, Mode=OneWay}" 
                         Foreground="{Binding Nir2FilteringForeground}" 
                         FontWeight="Bold"/>
                    <!-- MODIFICATION END -->
                </TextBlock>
            </StackPanel>
        </ControlTemplate>
    </Button.Template>
</Button>
```

#### State Variables
None (Stateless View).

---

### 1.2 SetupWindowViewModel

> ViewModel managing the logic for the Setup Window.

#### Interface (from Plan)

```csharp
public SetupWindowViewModel(
    ILogger<SetupWindowViewModel> logger,
    ApplicationConfiguration config,
    IServiceProvider serviceProvider,
    GeneralCameraLauncher generalCameraLauncher,
    NirCameraLauncher nirCameraLauncher,
    Nir2CameraLauncher nir2CameraLauncher,
    NirFilteringService nirFilteringService)
```

#### Preconditions
- `nirFilteringService` is injected and valid.

#### Postconditions
- `Nir2FilteringStatus` and `Nir2FilteringForeground` reflect the actual state of `nirFilteringService` immediately after construction.

#### Detailed Logic

```csharp
// Constructor
public SetupWindowViewModel(...)
{
    // ... dependency assignments ...
    
    _logger.LogInformation("SetupWindowViewModel constructor called");

    // Initialize Commands
    LaunchGeneralCameraCommand = ...
    // ... other commands ...
    
    // ========== CORE LOGIC CHANGE ==========
    // Step 1: Sync initial state from service
    // This ensures UI is correct if service is already running (singleton)
    UpdateNir2FilteringStatus(); 
    
    _logger.LogInformation("SetupWindowViewModel initialized successfully");
}
```

#### State Variables

| Variable | Type | Initial | Purpose |
|----------|------|---------|---------|
| `_nir2FilteringStatus` | string | "Deactivated" | Backing field for status text |
| `_nir2FilteringForeground` | Brush | Red | Backing field for status color |

#### State Transitions
No change to state machine, just initialization timing.

#### Thread Safety
- ViewModel is created on UI thread (via DI in `App.xaml.cs`).
- `UpdateNir2FilteringStatus` accesses mostly thread-safe or local properties.
- No new locking needed.

#### Error Handling
- `UpdateNir2FilteringStatus` handles null `_nirFilteringService` gracefully (returns early).

---

## 2. Integration Points

### 2.1 NirFilteringService → SetupWindowViewModel

#### Call Sequence

1. `App.xaml.cs` creates `SetupWindowViewModel` (Transient).
2. DI injects existing `NirFilteringService` (Singleton).
3. `SetupWindowViewModel` constructor calls `UpdateNir2FilteringStatus()`.
4. `UpdateNir2FilteringStatus` reads `_nirFilteringService.IsFilteringActive`.
5. Properties `Nir2FilteringStatus` and `Nir2FilteringForeground` are updated.
6. `SetupWindow` binds to these properties and displays correct state.

---

## 3. Edge Cases & Boundary Conditions

| Case | Input | Expected Behavior | Implementation |
|------|-------|-------------------|----------------|
| Service Null | `_nirFilteringService` is null | UI stays "Deactivated" (default) | Null check in update method |
| Re-opening Window | Window closed then opened again | UI reflects current running state | Constructor sync logic covers this |

---

## 4. Resource Management
No new resources to manage.

---

## 5. Performance Considerations
Negligible impact. One property check on constructor.

---

## 6. Testing Strategy

### 6.1 Unit Test Cases (Manual)

| Test Name | Setup | Action | Expected |
|-----------|-------|--------|----------|
| verify_color_binding | App Running | Open Setup Window | Button Text "Deactivated" is Red |
| verify_toggle_color | Setup Window Open | Click NIR Filter Button | Text becomes "Activated" (Green) |
| verify_init_sync | NIR Filter ON | Restart App (simulated or actual) | Text starts as "Activated" (Green) immediately |

---

## 7. Security Considerations
None.

---

## 8. Open Questions
- [x] All design questions resolved.

---

## Approval

- [ ] All components have detailed pseudo-code
- [ ] Error handling specified
- [ ] Edge cases covered
- [ ] Test cases defined
