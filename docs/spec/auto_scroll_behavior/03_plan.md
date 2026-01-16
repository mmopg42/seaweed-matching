# Plan: Auto Scroll Behavior

## 1. Goal
Implement conditional auto-scroll logic in the `FileGroupDataGrid` control. The grid should only scroll to the bottom when new items are added IF the user is already viewing the bottom of the list.

## 2. Approach
We will implement "Sticky Scroll" logic directly within `FileGroupDataGrid.xaml.cs`. This involves tracking the user's scroll position relative to the bottom and using that state to decide whether to honor a `ScrollToBottom` request.

### 2.1 Key Components
- **`FileGroupDataGrid.xaml.cs`**: 
  - Add `_scrollViewer` field to hold reference to the internal `ScrollViewer`.
  - Add `_isUserAtBottom` boolean flag.
  - Override `OnApplyTemplate` (or use `Loaded` event) to find the `ScrollViewer`.
  - Handle `ScrollViewer.ScrollChanged` to update `_isUserAtBottom`.
  - Modify `ScrollToBottom()` to respect `_isUserAtBottom`.

### 2.2 Algorithm
1.  **Detection**: On `ScrollChanged`:
    ```csharp
    bool atBottom = (e.VerticalOffset + e.ViewportHeight) >= (e.ExtentHeight - 2.0); // 2.0 tolerance
    if (e.ExtentHeightChange == 0) // Only update user intent when content hasn't changed size
    {
        _isUserAtBottom = atBottom;
    }
    ```
    *Correction*: When content size triggers the scroll (e.g. new item), we shouldn't update the *intent* flag immediately based on the new offset, but relies on the state *before* the add. However, simplified logic often works: 
    - Better logic: `_isUserAtBottom` tracks if the viewport is at the end.
    - When `ScrollToBottom` is called (after data add), check the flag.
    - Note: If `ExtentHeight` grows, `_isUserAtBottom` might become false before we call `ScrollToBottom`. 
    - **Refined Strategy**: We trust the `ScrollToBottom` call is made *after* the collection change. We should essentially ignore `ScrollToBottom` if `_isUserAtBottom` is false.

2.  **Execution**:
    ```csharp
    public void ScrollToBottom()
    {
        // 1. Check if we have a valid ScrollViewer reference
        // 2. If _isUserAtBottom is true, perform ScrollIntoView
        // 3. Else, do nothing
    }
    ```

3.  **Initialization**: `_isUserAtBottom` should default to `true`.

## 3. Proposed Changes

### 3.1 `ChronoView/UI/Controls/FileGroupDataGrid.xaml.cs`
-   Add helper method `GetVisualChild<T>` to find `ScrollViewer`.
-   Subscribe to `Loaded` event to initialize `_scrollViewer`.
-   Implement `ScrollViewer_ScrollChanged`.
-   Update `ScrollToBottom` method.

## 4. Verification Plan
### 4.1 Manual Verification
1.  **Start Monitoring**: Add data continuously (simulated or real).
2.  **Scenario A (Passive)**: Do not touch scrollbar. Verify list auto-scrolls to show new items.
3.  **Scenario B (Intervention)**: Scroll up to middle of list. Verify list does **NOT** jump to bottom when new items arrive.
4.  **Scenario C (Resume)**: Scroll back to bottom. Verify auto-scroll resumes for subsequent items.

### 4.2 Automated Tests
-   Difficult to unit test UI scroll interactions without UI automation framework (e.g. Appium/Sikuli).
-   Rely on manual verification as per project standard for UI behaviors.

## 5. Tasks
-   [ ] Modify `FileGroupDataGrid.xaml.cs` to implement sticky scroll logic.
-   [ ] Verify behavior with Data Simulator.
