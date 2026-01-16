# Tasks: Auto Scroll Behavior

## Implementation Checklist

### Task 1: Implement Sticky Scroll Logic
- [x] Add `_scrollViewer` and `_isUserAtBottom` fields to `FileGroupDataGrid`
- [x] Implement `GetVisualChild<T>` helper method
- [x] Subscribe to `ScrollChanged` in `Loaded` event
- [x] Unsubscribe from `ScrollChanged` in `Unloaded` event
- [x] Implement `OnScrollChanged` handler with state tracking
- [x] Modify `ScrollToBottom()` to respect `_isUserAtBottom`

### Task 2: Verification
- [ ] Test Scenario A: Passive auto-scroll (no user intervention)
- [ ] Test Scenario B: User scrolls up, new data arrives, position maintained
- [ ] Test Scenario C: User returns to bottom, auto-scroll resumes

## Affected Files
- `ChronoView/UI/Controls/FileGroupDataGrid.xaml.cs`
