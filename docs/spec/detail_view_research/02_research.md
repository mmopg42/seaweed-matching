---
Task: detail_view_research
Created: 2026-01-14
Status: Complete
Depends On: N/A (User Request)
Implementation Status: Completed (Detail View Removed, Image Preview Preserved)
---

# Detail View Removal (Keep Image Click Preview) - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: What is the double-click detail overlay? | `DetailPreviewViewModel` + `DetailPreviewView` overlay hosted by `MainWindow.xaml` | High |
| Q2: How is it triggered? | Double-click on `FileGroupDataGrid` row (XAML InputBinding + code-behind handler) | High |
| Q3: What must be preserved? | Image click preview: `OpenImagePreviewCommand` + `SharedResources.xaml` LeftClick bindings + `ImagePreviewWindow` | High |

## 2. Detailed Findings

### 2.0 Two separate features (Do NOT conflate)

There are **two distinct user-facing behaviors**:

1) **Double-click Detail View (to delete)**  
   - An overlay panel that shows multiple images/status for a selected group.

2) **Image click -> Large image preview (must keep)**  
   - Clicking an image opens a preview window/dialog showing a large image (with zoom/rotate in dialog code).

This separation matters: the removal work must delete (1) without breaking (2).

### 2.1 Q1: Where is the detail view implemented?

**Method**: Code search for "Detail", "View", and "Double Click" bindings.

**Findings**:
- **ViewModel**: `ChronoView.UI.ViewModels.DetailPreviewViewModel` handles the logic, image paths, and visibility.
- **View**: `ChronoView.UI.Views.DetailPreviewView` is the user control defining the UI.
- **Integration**: The View is instantiated in `MainWindow.xaml` as an overlay (ZIndex=100) and bound to `DetailPreviewVM` property in `MainWindowViewModel`.

**Evidence**:
- `MainWindow.xaml` (Line 238): `<views:DetailPreviewView DataContext="{Binding DetailPreviewVM}" Panel.ZIndex="100"/>`
- `MainWindowViewModel.cs` (Line 31): `public DetailPreviewViewModel DetailPreviewVM { get; } = new();`

**Conclusion**: The implementation is a dedicated ViewModel/View pair integrated into the MainWindow as an overlay.

---

### 2.2 Q2: How is the detail view triggered?

**Method**: Examined XAML input bindings in `FileGroupDataGrid.xaml`.

**Findings**:
1. **Primary**: `FileGroupDataGrid.xaml` contains a `MouseBinding` for `LeftDoubleClick`.
   - Executes `OpenDetailViewCommand` via Ancestor binding.
2. **Secondary (likely dead code)**: `MainWindow.xaml.cs` contains `FileGroupRow_MouseDoubleClick` handler that would invoke `OpenDetailViewCommand`.
   - However, repo-wide search found **no** XAML hookup (`MouseDoubleClick="FileGroupRow_MouseDoubleClick"`), no `EventSetter` for `MouseDoubleClick`, and no code subscription (`+=` / `AddHandler`).
   - Conclusion: the active trigger path is the XAML `LeftDoubleClick` binding in `FileGroupDataGrid.xaml`.

**Evidence**:
- `FileGroupDataGrid.xaml`:
```xml
<MouseBinding Gesture="LeftDoubleClick" 
              Command="{Binding DataContext.OpenDetailViewCommand, RelativeSource={RelativeSource AncestorType=Window}}"
              CommandParameter="{Binding ElementName=MainDataGrid, Path=SelectedItem}"/>
```
- `MainWindowViewModel.cs`:
```csharp
OpenDetailViewCommand = new RelayCommand<FileGroupViewModel>(ExecuteOpenDetailView);
```
- `MainWindow.xaml.cs`:
```csharp
private void FileGroupRow_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
{
    _viewModel.OpenDetailViewCommand.Execute(group);
}
```

**Conclusion**: Double-click opening is implemented by the XAML `LeftDoubleClick` input binding. The code-behind handler exists but appears unhooked (safe to delete during cleanup).

---

### 2.3 Q3: How is the data displayed?

**Method**: Analyzed `DetailPreviewViewModel.cs` and `ExecuteOpenDetailView`.

**Findings**:
- `ExecuteOpenDetailView` calls `DetailPreviewVM.UpdateGroup(group)`.
- `UpdateGroup` sets `IsVisible = true` and populates:
    - `MainImagePath` & `NirImagePath` from the `FileGroupViewModel`.
    - `CompositeCameras`: A collection of 3 side camera images (Line 1: Cam1-3, Line 2: Cam4-6).
    - `StatusMessage` & `StatusColor`.
- The `DetailPreviewView` binds these properties to Images and TextBlocks.

**Evidence**:
- `DetailPreviewViewModel.cs`: `UpdateGroup` method logic.

**Conclusion**: The ViewModel acts as a facade, extracting image paths from the selected `FileGroupViewModel` and preparing them for display.

---

### 2.4 What is the image click preview feature (must keep)?

**Findings**:
- `MainWindowViewModel` exposes `OpenImagePreviewCommand` and opens `ChronoView.UI.Views.ImagePreviewWindow` with a decoded bitmap.
- Image controls bind LeftClick to that command via `Resources/SharedResources.xaml` `Image.InputBindings`.

**Evidence**:
- `MainWindowViewModel.cs`: `OpenImagePreviewCommand = new RelayCommand<string>(ExecuteOpenImagePreview);`
- `Resources/SharedResources.xaml`: multiple instances of:
  - `MouseBinding MouseAction="LeftClick" Command="{Binding DataContext.OpenImagePreviewCommand, RelativeSource={RelativeSource AncestorType=Window}}" ...`
- `MainWindowViewModel.cs`: `new ChronoView.UI.Views.ImagePreviewWindow(bitmap, Path.GetFileName(path))`

**Conclusion**: Click-to-preview is implemented via a separate command (`OpenImagePreviewCommand`) and should remain untouched when removing the double-click detail overlay.

## 3. Code Analysis

### 3.1 Relevant Existing Code

| File | Component | Relevance | Notes |
|------|-----------|-----------|-------|
| `UI/Controls/FileGroupDataGrid.xaml` | `FileGroupDataGrid` | Trigger Source | Defines MouseBinding |
| `UI/ViewModels/MainWindowViewModel.cs` | `MainWindowViewModel` | Orchestrator | Holds `DetailPreviewVM` and Command |
| `UI/ViewModels/DetailPreviewViewModel.cs` | `DetailPreviewViewModel` | Logic | Manages state and visibility |
| `UI/Views/DetailPreviewView.xaml` | `DetailPreviewView` | Presentation | The actual UI overlay |
| `MainWindow.xaml` | `MainWindow` | Layout | Hosts the overlay View |
| `MainWindow.xaml.cs` | `MainWindow` (Code-behind) | Trigger Backup | Contains `FileGroupRow_MouseDoubleClick` |
| `Resources/SharedResources.xaml` | Shared Styles/Templates | MUST KEEP | LeftClick -> `OpenImagePreviewCommand` bindings |
| `UI/ViewModels/MainWindowViewModel.cs` | `OpenImagePreviewCommand` | MUST KEEP | Creates and shows `ImagePreviewWindow` |
| `UI/Views/ImagePreviewWindow.xaml(.cs)` | Preview Window | MUST KEEP | Large image display |
| `UI/Views/ImagePreviewDialog.xaml(.cs)` | Preview Dialog | Keep (if used) | Zoom/rotate implementation exists |

### 3.2 Impact Analysis

Removal must delete double-click overlay paths while preserving click-to-preview command + bindings.

## 4. Recommendations

### Primary Recommendation

Treat “detail overlay” and “image preview” as separate features. Implementation should:
- Remove `OpenDetailViewCommand` triggers (XAML + code-behind).
- Remove the overlay view (`DetailPreviewView`) host and its ViewModel (`DetailPreviewViewModel`) if no longer referenced.
- Do **not** rename or remove `OpenImagePreviewCommand`, and verify `SharedResources.xaml` bindings still work.

### Unanswered Questions

None for scope boundaries. Next step is to execute the removal plan (`03_plan.md`).
