# Tasks: Fix Delete and Checkbox Bugs

## Implementation
- [x] **Fix Checkbox Binding**
    - [x] Open `ChronoView/UI/Controls/FileGroupDataGrid.xaml`
    - [x] **Remove** `RowStyle="{StaticResource FileGroupRowStyle}"` from `<DataGrid ...>`
    - [x] **Add** `<DataGrid.ItemContainerStyle>` block.
    - [x] Define Style TargetType=`DataGridRow` `BasedOn="{StaticResource FileGroupRowStyle}"`
    - [x] Add Setter for `IsSelected` with TwoWay binding.

- [x] **Refactor Delete Message**
    - [x] Open `ChronoView/UI/ViewModels/MainWindowViewModel.cs`
    - [x] Locate `ExecuteDeleteWithConfirmation`
    - [x] Logic: Split `selectedGroups` into `fullDelete` and `partialDelete`.
    - [x] Logic: Build message with "Category Headers" and "Top 3 Detailed List" per category.
    - [x] Logic: Retrieve and append explicit `QuarantinePath`.

- [ ] **Verify**
    - [x] Build Solution (Success - 0 errors)
    - [ ] Manual Test: Selection Sync (Header -> Row -> ViewModel)
    - [ ] Manual Test: Delete Dialog Format (Full/Partial separation + Details + Path)
    - [ ] Manual Test: Physical Deletion Success

## Definition of Done
- [ ] Header Checkbox selects all Rows AND ViewModels.
- [ ] Delete Dialog shows clean summary (Top 3 + Count) + Quarantine Path.
- [ ] Deleted files do not reappear after refresh.

## Implementation Summary

### Files Modified
1. **FileGroupDataGrid.xaml** - Removed `RowStyle` attribute, added `ItemContainerStyle` with TwoWay binding for `IsSelected`
2. **MainWindowViewModel.cs** - Refactored `ExecuteDeleteWithConfirmation()` to use Top 3 + summary pattern with explicit quarantine path

### Changes Made
- DataGridRow.IsSelected <-> FileGroupViewModel.IsSelected 양방향 바인딩 추가
- 삭제 메시지: 카테고리별 Top 3 상세 표시 + 요약
- 삭제 메시지: 실제 Quarantine 경로 표시
