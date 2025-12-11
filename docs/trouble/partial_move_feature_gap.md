# Partial Move Feature Gap Analysis

## Overview
The user requested verification of a feature present in the legacy Python script: **"Move only a specific number of items"** (partial move based on count input).
Upon review, it has been confirmed that this feature is **currently missing** in the C# `ChronoView` application.

## Legacy Implementation (Python)
According to `docs/modules/monitoring_app.md`, the legacy `monitoring_app.py` supported:
- **`nir_count_limit`**: Limits the number of NIR file groups processed (default: 0 = all).
- **`data_count_limit`**: Limits the total number of data groups moved (default: 0 = all).
- **UI**: Input fields for these limits were present in the toolbar/main window.
- **Logic**: 
  - `prune_nir_files_before_op(keep_count, ...)`: Prunes older NIR groups if count exceeds limit.
  - `execute_file_operation()`: Applies `data_count_limit` to the final list of matched groups before generating the move plan.

## Current Implementation (C#)
1.  **Configuration**: 
    - `ApplicationConfiguration.cs` (and specifically `WorkflowSettings`) **does not** contain any properties for `NirCountLimit` or `DataCountLimit`.
2.  **Logic**:
    - `MainWindowViewModel.ExecuteMoveAsync()` retrieves **all** selected groups (`GetSelectedGroups()`) and passes them directly to `FileOperationService`.
    - There is no filtering or `Take(n)` logic implemented to limit the count based on user input.
    - `FileOperationService.MoveFileGroupAsync()` operates on a single group at a time, but the batch orchestration in ViewModel does not support batch size limits.

## Required Changes (For Future Implementation)
To restore this logical parity ensuring the feature works as it did in the legacy app, the following would be needed:
1.  **Model**: Add `NirCountLimit` and `DataCountLimit` (int) properties to `ApplicationConfiguration` (likely in `WorkflowSettings` or a new `OperationSettings` class).
2.  **ViewModel**: 
    - Add binding properties for these limits in `MainWindowViewModel`.
    - Update `ExecuteMoveAsync` to apply `.Take(DataCountLimit)` (if > 0) to the list of groups to be moved.
    - Implement NIR pruning logic if `NirCountLimit` is set (this might be complex as it involves filtering groups based on their specific NIR timestamps).
3.  **UI**: Add numeric input fields (TextBox or UpDown control) to the main toolbar or settings area to allow users to set these limits.
