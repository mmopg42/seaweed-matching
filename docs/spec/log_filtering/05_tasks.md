# Task: Log Filtering

- [x] Create Spec Documents
    - [x] Requirements
    - [x] Plan
    - [x] Design
- [x] Implement Log Filtering
    - [x] Modify `LogMessage.cs`: Add `LineNumber` property
    - [x] Modify `MainWindowViewModel.cs`: Implement `InferLineNumber` and update `AddLogMessage`
    - [x] Modify `MainWindow.xaml`: Add `ActiveTabIndex` binding to LogPanel
    - [x] Modify `LogPanel.xaml.cs`: Add `ActiveTabIndex` DependencyProperty and line filtering logic
- [ ] Verify
    - [ ] Manual Verification: Check Line 1/2/Combined tab filtering
    - [ ] (Optional) Add Unit Test: `InferLineNumber` correctness

## Implementation Summary

### Files Modified
1. **LogMessage.cs** - Added `LineNumber` property (int?)
2. **MainWindowViewModel.cs** - Added `InferLineNumber()` method and updated `AddLogMessage()` to prepend `[Line X]` to messages
3. **MainWindow.xaml** - Added `ActiveTabIndex` binding to LogPanel
4. **LogPanel.xaml.cs** - Added `ActiveTabIndex` DependencyProperty and line-based filtering in `FilterLogMessage()`

### Pattern Matching
- **Line 1**: `nir1`, `normal1`, `cam[123]`, `line\s?1`, `camera[123]`
- **Line 2**: `nir2`, `normal2`, `cam[456]`, `line\s?2`, `camera[456]`
- **System/Common**: No match (LineNumber = null)

### Filter Behavior
- **Tab 0 (Line 1)**: Show Line 1 logs + System logs
- **Tab 1 (Line 2)**: Show Line 2 logs + System logs
- **Tab 2 (Combined)**: Show all logs
