---
Task: fix_event_processor_debounce
Created: 2026-01-13
Status: Draft
Depends On: 01_requirements.md
---

# Fix Event Processor Debounce - Research Findings

## 1. Discrepancy Analysis

### 1.1 Code vs. Documentation
| Component | Setting | Value | Status |
|-----------|---------|-------|--------|
| `ApplicationConfiguration.cs` | `PollingIntervalMs` | **200** | ✅ Source of Truth |
| `FileWatcherOptions.cs` | `PollingIntervalMs` | **200** | ⚠️ Duplicate Definition |
| `FileWatcherService.cs` | `_pollingTimer` | Uses Config | ✅ Correct |
| `EventProcessor.cs` | `ShouldSkipEvent` | **2.0** (Hardcoded) | ❌ **ROOT CAUSE** |
| `improve_normal_folder_polling` Spec | Requirement | **500** | ❌ Outdated/Incorrect assumption |

### 1.2 Impact Flow
1. **FileWatcherService**: Polls every **200ms**. Generates 5 events/sec for checking.
   ↓
2. **EventProcessor**: Checks `(Now - Last) < 2.0s`.
   ↓
3. **Result**: 
   - 1st event (0ms): **Passed**
   - 2nd event (200ms): **Blocked** (< 2s)
   - 3rd event (400ms): **Blocked** (< 2s)
   - ...
   - 11th event (2200ms): **Passed**
   
   **Observed Behavior**: Logs appear every ~2.2 seconds, ignoring 90% of checks.

## 2. Refactoring Strategy

### 2.1 Option A: Direct Injection (Chosen)
Inject `IConfigurationManager` or `FileWatcherOptions` into `EventProcessor` constructor.

### 2.2 Option B: Property Setter
Add `public double DebounceSeconds { get; set; }` to `EventProcessor` and set it from `MonitoringOrchestrator` or `FileWatcherService`.

**Recommendation**: **Option B** is less invasive for a quick fix, but **Option A** is better architecture. Given the urgency and "Spaghetti" warning, we will proceed with **Option B** (configuring via start method or property) to minimize dependency injection changes that might ripple.

Actually, looking at `EventProcessor` usage in `MonitoringOrchestrator.cs`:
It is instantiated via DI `services.AddSingleton<IEventProcessor, EventProcessor>();`.
So injecting `ApplicationConfiguration` (which is also a singleton) is the clean, correct way.

## 3. Glossary Updates
None required.
