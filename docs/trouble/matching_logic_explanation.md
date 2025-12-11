# Matching Logic Analysis & Troubleshooting

## 1. Issue: "Options disabled but files are missing / skipped"

User reported:
1.  **Action:** Disabled "Time-based camera matching" in Advanced Settings.
2.  **Expectation:** Camera files should be assigned sequentially 1:1.
3.  **Observation:** Files are still being skipped/unmatched (Group 1 empty).

---

## 2. Root Cause: Missing Configuration Mapping (SOLVED)

The investigation revealed a code defect in `MonitoringOrchestrator.cs`.
When initializing the matching engine, the system **failed to copy the Matching Options** from your settings.
Thus, the engine always started with **Default Values (Time-Based: ON)**.

```csharp
// Previous Code (Defective)
_fileGroupMatcher.Configuration = new MatchingConfiguration
{
    // ... only paths were copied ...
    // Options were ignored!
};
```

---

## 3. Resolution (Code Fixed)

The code has been updated to correctly map all advanced options:

*   `UseCamTimeMatching`
*   `CamMatchMinDiff / MaxDiff`
*   `NirMatchTimeDiff`
*   And other related settings.

The build has **succeeded**.

---

## 4. Next Steps

**PLEASE RESTART THE APPLICATION.**

1.  Close the running application.
2.  Run `dotnet run` again.
3.  The application will now correctly load your "Time-based camera matching: OFF" setting.
4.  **Result:** Sequential matching should work as expected (Group 1 will take Cam 1).

### Note on Real-time Updates
Currently, changing these Advanced Settings requires a **restart** to take effect. We have fixed the *application* of these settings upon start. Real-time updates without restart are a future enhancement.
