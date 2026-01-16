# Log Filtering - Implementation Plan

## Goal Description
Implement Line-based log filtering using a **Central Inference Strategy**. instead of modifying method signatures across the entire application, we will infer the `LineNumber` within `MainWindowViewModel.AddLogMessage` based on the log's Source and Message content. This `LineNumber` will then be used by the `LogPanel` to filter logs according to the active tab.

## User Review Required
- **Strategy Change**: We are **NOT** replacing the "Source" column. Instead, we are keeping the "Source" column and prepending `[Line X]` to the log message.
- **Inference Logic**: Line detection relies on Regex matching (e.g., "nir1", "Line 1"). If a new component naming convention is introduced, the regex must be updated.

## Proposed Changes
### Models
#### [MODIFY] LogMessage.cs
- Add `public int? LineNumber { get; set; }` (1, 2, or null).

### ViewModels
#### [MODIFY] MainWindowViewModel.cs
- **Modify `AddLogMessage(severity, source, message)`**:
    - **NO Signature Change**: Keep existing signature to maintain compatibility.
    - **Inference Logic**: Call a new helper `InferLineNumber(source, message)`.
    - **Message formatting**: If Line is inferred, prepend `[Line X]` to the `message`.
    - **Object Creation**: Create `LogMessage` setting the `LineNumber` property.

### UI
#### [MODIFY] LogPanel.xaml
- Add `ActiveTabIndex` DependencyProperty (int).
- Bind `ActiveTabIndex` to `MainWindowViewModel.ActiveTabIndex`.

#### [MODIFY] LogPanel.xaml.cs
- Add `ActiveTabIndex` Dependency Property.
- Update `FilterLogMessage` logic:
    - `ActiveTabIndex == 0` (Line 1): Show `LineNumber == 1` OR `LineNumber == null`.
    - `ActiveTabIndex == 1` (Line 2): Show `LineNumber == 2` OR `LineNumber == null`.
    - `ActiveTabIndex == 2` (Combined): Show All.
- Refresh filter when `ActiveTabIndex` changes.

## Verification Plan
### Automated Tests
- **[NEW] Unit Test `LogFilteringTests.cs`**:
    - Test `InferLineNumber`:
        - Input: "nir1 error", Expect: 1
        - Input: "System ready", Expect: null
        - Input: "Moving Line 2", Expect: 2
    - Test `LogPanel` filtering logic (if testable, otherwise manual).

### Manual Verification
- **Run App**:
    - Trigger Line 1 event.
    - Verify Log in Line 1 Tab: Visible, Message starts with `[Line 1]`.
    - Verify Log in Line 2 Tab: Hidden.
    - Verify Log in Combined Tab: Visible.
    - Verify "System" logs appear in all tabs.
