# Research: Auto Scroll Behavior

## 1. Current Implementation
### 1.1 `FileGroupDataGrid` Control
- Located in `ChronoView/UI/Controls/FileGroupDataGrid.xaml`.
- Wrapper around standard WPF `DataGrid`.
- Exposes `ScrollToBottom()` method:
  ```csharp
  public void ScrollToBottom()
  {
      if (MainDataGrid == null || MainDataGrid.Items.Count == 0) return;
      MainDataGrid.ScrollIntoView(MainDataGrid.Items[^1]);
  }
  ```

### 1.2 `MainWindow.xaml.cs` Logic
- Subscribes to `CollectionChanged` events of `Line1Groups` and `Line2Groups`.
- Calls `ScrollToBottom()` unconditionally on `Add` action:
  ```csharp
  private void OnLine1GroupsChanged(object? sender, NotifyCollectionChangedEventArgs e)
  {
      // ...
      Dispatcher.BeginInvoke(..., new Action(() =>
      {
          Line1DataGrid?.ScrollToBottom();
          CombinedLine1DataGrid?.ScrollToBottom();
      }));
  }
  ```

## 2. Technical Challenge
WPF `DataGrid` does not expose the internal `ScrollViewer` directly. To check `VerticalOffset` vs `ScrollableHeight` (ExtentHeight - ViewportHeight), we need access to the `ScrollViewer` in the visual tree.

### 2.1 Accessing ScrollViewer
Standard approach:
```csharp
public static T GetVisualChild<T>(DependencyObject parent) where T : Visual
{
    // Recursive VisualTreeHelper search
}
```
We can implement this helper or use an existing one if available.

### 2.2 Stick-to-Bottom Logic
We need to track "stickiness".
- **Option A**: Check just before scrolling.
  - When `ScrollToBottom()` is called:
    - Check if `VerticalOffset >= ScrollableHeight - Tolerance`.
    - If yes, proceed with `ScrollIntoView`.
    - If no, ignore.
  - **Issue**: `ScrollIntoView` might be called *after* the item is added to the collection. The `DataGrid` might have already adjusted the scrollbar distinct from the content expansion.
  - **Refinement**: We should check if we *were* at the bottom before the new item was added?
  - Actually, if we are at the bottom, adding a row usually waits. If we simply check "Are we near the bottom?", it should suffice.

## 3. Proposed Solution
Modify `FileGroupDataGrid.xaml.cs`:

1.  Add `private ScrollViewer? _scrollViewer;`
2.  Use `Loaded` event (since `FileGroupDataGrid` is a `UserControl`, not a templated control—`OnApplyTemplate` is not applicable) with `GetVisualChild` helper to retrieve it.
3.  Implement `ScrollToBottomIfAtBottom()` method (or modify `ScrollToBottom` with a parameter).
    - `bool isAtBottom = _scrollViewer.VerticalOffset >= _scrollViewer.ScrollableHeight - 1.0;`
    - If `isAtBottom`, call `MainDataGrid.ScrollIntoView(...)`.

Alternatively, since `MainWindow` is invoking it `OnCollectionChanged`, the `Add` event happens, then `Dispatcher.Invoke` happens. By the time `Invoke` runs, the grid might have updated its layout (or pending).
If the user was at the bottom (showing 10/10 items), and item 11 is added.
- ScrollableHeight increases.
- VerticalOffset usually stays same (showing 10/11 items).
- So `VerticalOffset` will clearly be *less* than `ScrollableHeight`.
- Thus, simply checking `VerticalOffset == ScrollableHeight` *after* add will return **FALSE** (because we are looking at 10/11).
- **CRITICAL**: We need to know if we *were* at the bottom, OR we need to define "at bottom" as "viewing the last item *before* this add"?
- Better approach: Track scroll state continuously.
    - Handle `ScrollChanged` event.
    - `_isUserAtBottom = (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 1.0);`
    - When `ScrollToBottom` is requested, check `_isUserAtBottom`.

## 4. Implementation Details
### 4.1 Tracking State
- Hook `Loaded` event to find `ScrollViewer`.
- Hook `ScrollViewer.ScrollChanged`.
- Update `private bool _autoScroll = true;` logic.

### 4.2 Handling "Near Bottom"
- Use a small tolerance (e.g., 1-5 pixels) to account for double arithmetic.

### 4.3 Risks
- If the user scrolls up just as data arrives, they might fight the auto-scroll. (This proposal fixes that).
- If the user is at bottom, and a large batch arrives?
  - `_isUserAtBottom` allows scrolling to the *new* bottom.

## 5. Alternatives
- **Attached Behavior**: Create `AutoScrollBehavior`. Cleaner separation, reusable.
  - Check `Behaviors` folder. `DragSelectBehavior` exists.
  - Creating `AutoScrollBehavior` is "Best Practice" in MVVM/WPF.
- **Code Behind**: Easier, localized to `FileGroupDataGrid`. Given this is a specific control `FileGroupDataGrid`, code-behind is acceptable and robust.

**Decision**: Implement in `FileGroupDataGrid.xaml.cs` using `ScrollViewer` event tracking. It's direct and reliable.
