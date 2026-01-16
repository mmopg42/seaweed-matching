---
Task: fix_event_processor_debounce
Created: 2026-01-13
Status: Draft
Depends On: 02_research.md
---

# Fix Event Processor Debounce - Implementation Plan

## 1. Components to Modify

### 1.1 `EventProcessor.cs`
- **Goal**: Remove hardcoded 2s, use configured interval.
- **Change**:
  - Add `_debounceInterval` field.
  - Update constructor to accept `ApplicationConfiguration` OR add a `SetDebounceInterval(TimeSpan)` method.
  - *Decision*: Since `EventProcessor` is a Singleton, inject `ApplicationConfiguration` in constructor.

### 1.2 `ApplicationConfiguration.cs` (Reference Only)
- Confirm `PollingIntervalMs` is accessible.

### 1.3 Remove Configuration Duplication (Cleanup)
- **Goal**: Establish "Single Source of Truth" for polling settings.
- **Changes**:
  - Update `FileWatcherService`: Inject `ApplicationConfiguration` logic (or reference) instead of relying on `FileWatcherOptions.PollingIntervalMs`.
  - Remove `PollingIntervalMs` from `FileWatcherOptions` class.
  - Update `MonitoringOrchestrator` to stop populating this field.

## 2. Step-by-Step Plan

1. **Modify `EventProcessor.cs`**:
   - Inject `ApplicationConfiguration` into constructor.
   - Store `PollingIntervalMs` (converted to seconds) as `_debounceSeconds`.
   - Update `ShouldSkipEvent` to use `_debounceSeconds`.

2. **Verify DI Registration**:
   - Ensure `EventProcessor` can resolve `ApplicationConfiguration`. (It is registered as Singleton in `App.xaml.cs`, so this should work automatically).

3. **Verify Updates**:
   - Run app, check logs.
   - Confirm interval is ~200ms.

## 3. Verification Strategy
- **Manual**: Observe "Image Check" log frequency.
- **Code Review**: Ensure magic number `2` is gone.

## 4. Rollback Plan
- Revert `EventProcessor.cs` to hardcoded value if DI fails.
