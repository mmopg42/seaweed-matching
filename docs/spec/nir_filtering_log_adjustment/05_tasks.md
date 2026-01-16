# Tasks: NIR Filtering Log Adjustment

- [x] Modify `SystemControlViewModel.cs` to log status changes
    - [ ] Locate `_nirFilteringService.StatusChanged` subscription in constructor
    - [ ] Add `LogRequested?.Invoke` call inside the handler
- [ ] Verify Build [ ]
- [ ] Manual Check [ ]
