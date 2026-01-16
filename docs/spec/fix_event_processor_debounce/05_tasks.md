---
Task: fix_event_processor_debounce
Created: 2026-01-13
Status: Draft
Depends On: 04_design.md
---

# Fix Event Processor Debounce - Tasks

## Implementation
- [ ] **Modify `EventProcessor.cs` constructor**:
    ```csharp
    private readonly ApplicationConfiguration _config;
    
    public EventProcessor(ILogger<EventProcessor> logger, ApplicationConfiguration config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }
    ```

- [ ] **Modify `ShouldSkipEvent` method**:
    ```csharp
    // Calculate debounce dynamically
    double debounceSeconds = _config.WorkflowSettings.PollingIntervalMs / 1000.0;
    if ((DateTime.UtcNow - lastProcessed).TotalSeconds < debounceSeconds)
    {
        return true;
    }
    ```

- [ ] **Cleanup Configuration Duplication (Optional)**:
    - [ ] Remove `PollingIntervalMs` from `FileWatcherOptions`.
    - [ ] Update `FileWatcherService` to use `ApplicationConfiguration`.

## Verification
- [ ] **Manual Test**:
    - [ ] Run app.
    - [ ] Observe log frequency for "Image Check".
    - [ ] Verify it matches ~200ms (fast stream of logs) instead of ~2s.

## Documentation
- [ ] Update `06_report.md` (post-implementation).
