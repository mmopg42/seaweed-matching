---
Task: detail_view_research
Created: 2026-01-14
Status: Draft
Depends On: 03_plan.md
---

# Detail View Removal (Keep Image Click Preview) - Detailed Design

## 1. Component Designs

### 1.1 MainWindowViewModel

> From 03_plan.md: Orchestrator that holds `DetailPreviewVM` and `OpenDetailViewCommand`. We need to remove these to sever the link to the detail overlay.

#### Interface Changes

- **Remove**: `public ICommand OpenDetailViewCommand { get; }`
- **Remove**: `public DetailPreviewViewModel DetailPreviewVM { get; }`
- **Preserve**: `public ICommand OpenImagePreviewCommand { get; }`

#### Detailed Logic (Removal)

```pseudo
class MainWindowViewModel:
    // ... existing code ...

    // [REMOVE THIS SECTION]
    // public DetailPreviewViewModel DetailPreviewVM { get; } = new();
    
    // [REMOVE THIS COMMAND]
    // OpenDetailViewCommand = new RelayCommand<FileGroupViewModel>(ExecuteOpenDetailView);
    
    // [REMOVE THIS METHOD]
    // private void ExecuteOpenDetailView(FileGroupViewModel group)
    // {
    //     if (group == null) return;
    //     DetailPreviewVM.UpdateGroup(group);
    // }
    
    // [KEEP THIS SECTION]
    // public ICommand OpenImagePreviewCommand { get; }
    // private void ExecuteOpenImagePreview(string imagePath)
    // {
    //     // ... existing logic to open ImagePreviewWindow ...
    // }
```

#### State Variables

| Variable | Type | Initial | Purpose | Change |
|----------|------|---------|---------|--------|
| `DetailPreviewVM` | `DetailPreviewViewModel` | `new()` | Manages overlay state | **DELETE** |

#### Error Handling

None required for removal. The compiler will catch any remaining references (which should be none after View updates).

---

### 1.2 FileGroupDataGrid (XAML)

> From 03_plan.md: The source of the separate double-click trigger using InputBindings.

#### Detailed Logic (XAML)

```xml
<!-- UI/Controls/FileGroupDataGrid.xaml -->

<DataGrid ...>
    <DataGrid.InputBindings>
        <!-- [REMOVE THIS BINDING] -->
        <!-- <MouseBinding Gesture="LeftDoubleClick" 
                           Command="{Binding DataContext.OpenDetailViewCommand, RelativeSource={RelativeSource AncestorType=Window}}"
                           CommandParameter="{Binding ElementName=MainDataGrid, Path=SelectedItem}"/> -->
    </DataGrid.InputBindings>
    ...
</DataGrid>
```

#### Preconditions
- The `DataGrid` exists and properly displays rows.

#### Postconditions
- Double-clicking a row does **nothing** (default DataGrid behavior, usually enters edit mode if allowed, or just selects).
- Single click still selects the row.

---

### 1.3 MainWindow (XAML + Code-behind)

> From 03_plan.md: Hosts the `DetailPreviewView` overlay and has a backup code-behind handler for double-click.

#### Detailed Logic (XAML)

```xml
<!-- MainWindow.xaml -->

<Grid>
    <!-- ... other content ... -->

    <!-- [REMOVE THIS OVERLAY] -->
    <!-- <views:DetailPreviewView DataContext="{Binding DetailPreviewVM}" Panel.ZIndex="100"/> -->

</Grid>
```

#### Detailed Logic (Code-behind)

```csharp
// MainWindow.xaml.cs

public partial class MainWindow : Window
{
    // ... existing code ...

    // [REMOVE THIS HANDLER and its subscription]
    // private void FileGroupRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    // {
    //     // ... logic invoking OpenDetailViewCommand ...
    // }
}
```

---

## 2. Integration Points

### 2.1 FileGroupDataGrid → MainWindowViewModel

#### Change Analysis
- **Before**: `FileGroupDataGrid` InputBinding -> `MainWindowViewModel.OpenDetailViewCommand`
- **After**: Link removed. No command executed on double-click.

### 2.2 Image → MainWindowViewModel (Preserved)

#### Call Sequence (Verified to remain)
1. User clicks Image in `FileGroupDataGrid`.
2. `SharedResources.xaml` `MouseBinding` (`LeftClick`) triggers key `OpenImagePreviewCommand` on `Window.DataContext`.
3. `MainWindowViewModel.ExecuteOpenImagePreview` runs.
4. `ImagePreviewWindow` opens.

#### Data Contract
- **Input**: `string imagePath` (passed as CommandParameter).
- **Validation**: `!string.IsNullOrEmpty(path)` and `File.Exists(path)`.

#### Must-keep artifacts (anti-regression)
- `ChronoView/Resources/SharedResources.xaml`: `Image.InputBindings` with `MouseAction="LeftClick"` → `OpenImagePreviewCommand`
- `ChronoView/UI/ViewModels/MainWindowViewModel.cs`: `OpenImagePreviewCommand` + `ExecuteOpenImagePreview`
- `ChronoView/UI/Views/ImagePreviewWindow.xaml(.cs)`: preview window used by the command

---

## 3. Edge Cases & Boundary Conditions

| Case | Input | Expected Behavior | Implementation |
|------|-------|-------------------|----------------|
| Double-click row | Mouse action | Row selected, NO overlay | Remove `MouseBinding` and event handler |
| Click Image | Mouse action | `ImagePreviewWindow` opens | Preserve `OpenImagePreviewCommand` |
| Image Path Invalid | Missing file | `ImagePreviewWindow` handles gracefully (or logs) | Existing logic (unchanged) |
| Detail VM code exists | Unused class | Compilation succeeds (dead code) | Can delete `DetailPreviewViewModel.cs` later or now (Plan says delete if unused) |

---

## 4. Verification Check

### 4.1 Automated/Compiler Checks
- Build the solution.
- **Pass condition**: No build errors regarding missing `OpenDetailViewCommand` or `DetailPreviewVM` (meaning all XAML bindings were correctly removed).
- Search/grep check:
  - **Pass condition**: no remaining references to `OpenDetailViewCommand`, `ExecuteOpenDetailView`, `DetailPreviewVM`, `DetailPreviewView`, `DetailPreviewViewModel` in production code paths.

### 4.2 Manual Verification Steps

```
1. Start Application.
2. Load valid data so rows appear in Main DataGrid.
3. Action: Double-click on a row (text entry area, not image).
   - Expected: Row highlights, NOTHING else happens (No overlay).
4. Action: Click on a Camera 1/2/3 or NIR image thumbnail.
   - Expected: Large Image Preview Window opens.
5. Action: Close Image Preview.
   - Expected: App remains stable.
```

---

## 5. Security Considerations

None. Removing a UI feature does not introduce security risks.

---

## 6. Open Questions

- [x] Can we safely delete `DetailPreviewViewModel.cs` immediately?
  - **Answer**: Yes, once the `MainWindowViewModel` property is removed, no verified calling path remains. We should delete it to keep the codebase clean.

- [x] Where is `FileGroupRow_MouseDoubleClick` subscribed?
  - **Answer (verified)**: Repo-wide search found **no** subscription:
    - No XAML event attribute like `MouseDoubleClick="FileGroupRow_MouseDoubleClick"`
    - No `EventSetter Event="MouseDoubleClick"` in row styles
    - No code subscription via `+=` or `AddHandler(...)`
    Therefore the active double-click trigger is the `LeftDoubleClick` `MouseBinding` in `UI/Controls/FileGroupDataGrid.xaml`, and `FileGroupRow_MouseDoubleClick` in `MainWindow.xaml.cs` is dead code (safe to delete as cleanup).

---

## Approval

- [ ] All removal points identified (ViewModel, XAML, Code-behind).
- [ ] Preservation of Image Preview logic confirmed.
- [ ] Test cases defined for manual verification.

**Next Step**: 05_tasks.md
