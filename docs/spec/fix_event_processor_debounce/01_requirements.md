---
Task: fix_event_processor_debounce
Created: 2026-01-13
Status: Draft
Summary: Fix hardcoded 2s debounce in EventProcessor to respect 200ms PollingIntervalMs setting
Research Required: No
---

# Fix Event Processor Debounce Discrepancy - Requirements

## 1. Context & Root Cause Analysis

### 1.1 Problem Description
Users report that logs appear every 2 seconds, despite `PollingIntervalMs` being configured to 200ms (0.2s). The system is unresponsive to changes occurring within this 2-second window.

### 1.2 Timeline of Confusion
- **Pre-2025-12-17**: `PollingIntervalMs` correctly set to 200ms. Simple architecture.
- **2026-01-08**: `EventProcessor` introduced with **hardcoded 2-second debounce**.
  - `if ((DateTime.UtcNow - lastProcessed).TotalSeconds < 2)`
  - This effectively overrode the 200ms polling setting.
- **2026-01-08 (Docs)**: `improve_normal_folder_polling` spec requested 500ms, ignoring the existing 200ms config and the new 2s bottleneck.

### 1.3 Root Causes
1. **Magic Number**: 2-second hardcoded value in `EventProcessor.cs`.
2. **Layer Disconnect**: `EventProcessor` does not read `ApplicationConfiguration` or `FileWatcherOptions`.
3. **Configuration Duplication**: `PollingIntervalMs` exists in both `ApplicationConfiguration` and `FileWatcherOptions`.

## 2. Goal

### 2.1 Primary Goal
Align `EventProcessor`'s debounce logic with the system-wide `PollingIntervalMs` configuration (200ms default), eliminating the artificial 2-second lag.

### 2.2 Success Criteria
- [ ] `EventProcessor.ShouldSkipEvent` uses the configured polling interval (or slightly less) instead of hardcoded 2s.
- [ ] UI logs confirm "Image Check" events occurring at ~200ms intervals (when relevant).
- [ ] No regression in duplicate handling (debounce should still function, just faster).

## 3. Proposed Changes

### 3.1 Immediate Fix
Modify `EventProcessor.cs` to use a dynamic debounce interval injected or passed from configuration.

### 3.2 Long-term Refactoring (Recommended)
Inject `IConfigurationManager` or `EventProcessorOptions` into `EventProcessor` to avoid magic numbers and ensure single source of truth.

## 4. Constraints
- Must not break existing polling logic in `FileWatcherService`.
- Must keep CPU usage reasonable (200ms is 5 checks/sec).

## 5. Assumptions
- `PollingIntervalMs` in `ApplicationConfiguration` is the source of truth (200ms).
