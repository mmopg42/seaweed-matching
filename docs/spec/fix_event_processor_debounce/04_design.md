---
Task: fix_event_processor_debounce
Created: 2026-01-13
Status: Draft
Depends On: 03_plan.md
---

# Fix Event Processor Debounce - Detailed Design

## 1. Class Design Changes

### 1.1 `EventProcessor`

**Current Signature**:
```csharp
public EventProcessor(ILogger<EventProcessor> logger)
```

**New Signature**:
```csharp
public EventProcessor(ILogger<EventProcessor> logger, ApplicationConfiguration config)
{
    _logger = logger;
    // Set debounce roughly equal to polling interval to avoid dropping every other poll
    // Using 0.8 * interval allows slight jitter without skipping a full cycle.
    // OR simply use the exact interval if strict.
    // Let's match the polling interval exactly for now: 200ms -> 0.2s
    _debounceSeconds = config.WorkflowSettings.PollingIntervalMs / 1000.0;
}
```

**Fields**:
- `private readonly double _debounceSeconds;`

**Logic Change (`ShouldSkipEvent`)**:
```csharp
// Old
if ((DateTime.UtcNow - lastProcessed).TotalSeconds < 2)

// New
if ((DateTime.UtcNow - lastProcessed).TotalSeconds < _debounceSeconds)
```

## 2. Dependency Injection Check

`App.xaml.cs` currently registers:
`services.AddSingleton<ApplicationConfiguration>(config);`
`services.AddSingleton<IEventProcessor, EventProcessor>();`

This change is safe and requires no `App.xaml.cs` modification.

## 3. Alternative Consideration

If `WorkflowSettings.PollingIntervalMs` is intended to be dynamic (changeable at runtime), passing it in constructor locks it to the startup value.
Given `ApplicationConfiguration` is a reference type (Singleton object), reading `config.WorkflowSettings.PollingIntervalMs` *inside* `ShouldSkipEvent` would support runtime changes.

**Revised Design**:
Store reference to `ApplicationConfiguration`.

```csharp
private readonly ApplicationConfiguration _config;

public EventProcessor(ILogger<EventProcessor> logger, ApplicationConfiguration config) {
    _config = config;
}

public bool ShouldSkipEvent(...) {
   double threshold = _config.WorkflowSettings.PollingIntervalMs / 1000.0;
   // ...
}
```

This offers better flexibility with negligible performance cost.

## 4. Safety Margin

If Polling is 200ms, and we debounce for exactly 200ms, slight timing jitters might cause a skip.
However, usually "debouncing" means "don't process IF seen recently".
If the poller fires at T and T+200ms:
- T: Processed. Last = T.
- Check at T+200ms: (T+200 - T) = 200ms.
- If threshold is 200ms: 200 < 200 is False (Pass).
- If threshold is 200.1ms: 200 < 200.1 is True (Skip).

To be safe and ensure every poll cycle potentially triggers checking (if needed), we should use the configured value directly. The poller drives the check frequency.

Actually, the `EventProcessor` debounce is to prevent *duplicate triggers* from multiple sources or rapid FSW firing.
For Polling, it fires once per interval.
If FSW fires same file, we want to debounce it against the Poll.
Using the Polling Interval as the debounce window seems logical: "Don't process more often than we poll".

**Decision**: Use `_config.WorkflowSettings.PollingIntervalMs / 1000.0` dynamically.

## 5. Cleanup: FileWatcherOptions Duplication

To resolve the duplicate definition of `PollingIntervalMs`:

### 5.1 `FileWatcherOptions`
- Remove `public int PollingIntervalMs { get; set; }`.

### 5.2 `FileWatcherService`
- **Constructor Change**:
  ```csharp
  public FileWatcherService(ILogger<FileWatcherService> logger, ApplicationConfiguration config)
  ```
- **Logic Change**: 
  Read `config.WorkflowSettings.PollingIntervalMs` directly instead of `options.PollingIntervalMs`.

### 5.3 `MonitoringOrchestrator`
- Stop assigning `PollingIntervalMs` when creating `FileWatcherOptions`.
