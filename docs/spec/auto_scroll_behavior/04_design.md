# Design: Auto Scroll Behavior

## 1. System Architecture
No changes to the overall system architecture. The logic will be encapsulated entirely within the `FileGroupDataGrid` custom control in the UI layer.

## 2. Component Design
### 2.1 FileGroupDataGrid (`ChronoView.UI.Controls`)
We will enhance this control to track the user's scroll state.

#### New Fields
- `private ScrollViewer? _scrollViewer;`
- `private bool _isUserAtBottom = true;`

#### Method Logic
1.  **`Loaded` Event Handler** (since `FileGroupDataGrid` is a `UserControl`, `OnApplyTemplate` is not applicable):
    -   Find the `ScrollViewer` within the visual tree using `GetVisualChild` helper (or `VisualTreeHelper`).
    -   If found, subscribe to `ScrollChanged` event.

2.  **`OnScrollChanged`** (Event Handler):
    -   **Trigger**: Whenever the user scrolls or content size changes.
    -   **Logic**:
        ```csharp
        // Check if we are physically at the bottom
        bool atBottom = (e.VerticalOffset + e.ViewportHeight) >= (e.ExtentHeight - 2.0);
        
        // Critical: Only update the "User Intent" flag when the *user* scrolls, 
        // OR when we are at the bottom. 
        // If the ExtentHeight changed (new items added), we want to preserve the *previous* state
        // unless we were already at the bottom.
        
        if (e.ExtentHeightChange == 0) 
        {
            // User is scrolling (or resize without data change)
            _isUserAtBottom = atBottom;
        }
        else 
        {
            // Data added. 
            // If we were at bottom, we likely want to stay at bottom (handled by ScrollToBottom call).
            // We don't forcibly update _isUserAtBottom to 'false' here just because Extent grew.
        }
        ```

3.  **`ScrollToBottom`** (Public Method):
    -   **Logic**:
        ```csharp
        public void ScrollToBottom()
        {
            if (_scrollViewer == null) return;
            if (MainDataGrid.Items.Count == 0) return;
            
            // Only scroll if the user was arguably at the bottom before this update
            if (_isUserAtBottom) 
            {
                MainDataGrid.ScrollIntoView(MainDataGrid.Items[^1]);
            }
        }
        ```

### 2.2 MainWindow Interaction
-   Existing calls in `MainWindow.xaml.cs` (`ScrollToBottom()`) remain unchanged.
-   The "conditionality" is internal to the control.

## 3. Data Flow
1.  **User Action**: User scrolls up -> `ScrollChanged` fires -> `ExtenHeightChange` is 0 -> `_isUserAtBottom` becomes `false`.
2.  **System Action**: New File Group added -> `CollectionChanged` fires -> `MainWindow` calls `ScrollToBottom`.
3.  **Control Logic**: `ScrollToBottom` checks `_isUserAtBottom` (false) -> Logic acts: **Do Nothing**.
4.  **Result**: Viewport stays stable. User continues reading history.
5.  **User Action**: User scrolls to bottom -> `_isUserAtBottom` becomes `true`.
6.  **System Action**: New File Group added -> `ScrollToBottom` called -> `_isUserAtBottom` is true -> Logic acts: **Scrolls to new item**.

## 4. Security & Performance
-   **Performance**: `ScrollChanged` is high-frequency. Logic must be O(1) and avoid heavy calculations. Simple float comparison is negligible.
-   **Memory**: Event subscription requires unsubscription if control is unloaded, to prevent leaks. Since `MainWindow` lives for app duration, this is minor, but adding `Unloaded` handler is best practice.

## 5. Implementation Steps
1.  Modify `FileGroupDataGrid.xaml.cs`.
    -   Add `VisualTreeHelper` utility to find `ScrollViewer`.
    -   In `Loaded` event, find `ScrollViewer` and subscribe to `ScrollChanged`.
    -   In `Unloaded` event, unsubscribe from `ScrollChanged` to prevent memory leaks.
    -   Implement tracking logic in `OnScrollChanged` handler.
