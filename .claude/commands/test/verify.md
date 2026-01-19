---
name: test:verify
description: Verify ChronoView application state (stats, data, cameras)
argument-hint: [target] [expected value]
allowed-tools:
  - Bash
  - AskUserQuestion
---

<objective>
Verify the current state of ChronoView application by retrieving statistics, data, and configuration.

This skill is used after performing actions to confirm the expected state changes occurred.
</objective>

<context>
Working directory: C:\workspace\seaweed\gui_kiro_v2

UI Automation CLI: skills_scripts/ui_automation/bin/Debug/net10.0/ui_automation.csproj

Arguments: $ARGUMENTS
</context>

<process>
**1. Parse verification target**

First argument determines what to verify:
- `stats` - Overall statistics (matched groups, unmatched files, etc.)
- `rows` - DataGrid row count
- `data` - Full DataGrid data export
- `cameras` - Camera states
- `all` - Run all verifications

**2. Verify statistics**

```bash
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- stats get --json
```

Expected response:
```json
{
  "success": true,
  "data": {
    "matchedGroups": 5,
    "unmatchedFiles": 3,
    "totalImages": 45,
    "abnormalCount": 0
  }
}
```

**3. Verify DataGrid rows**

```bash
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- datagrid rows --json
```

Expected response:
```json
{
  "success": true,
  "data": {
    "rowCount": 5
  }
}
```

**4. Verify DataGrid data**

```bash
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- datagrid data --json
```

Provides full data export with all rows and columns.

**5. Verify camera states**

```bash
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- workflow camera-states --json
```

Expected response:
```json
{
  "success": true,
  "data": {
    "cameras": [
      { "name": "General", "connected": true, "capturing": false },
      { "name": "NIR 1", "connected": true, "capturing": false },
      { "name": "NIR 2", "connected": false, "capturing": false }
    ]
  }
}
```

**6. Verify SettingsDialog status**

```bash
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- settings-dialog status --json
```

**7. Compare with expected value (if provided)**

If second argument provided, compare actual vs expected:
- Numeric comparison for counts
- Boolean comparison for states
- String comparison for text values

**8. Report verification result**

Clear pass/fail indication with actual values shown.
</process>

<success_criteria>
- [ ] Verification target identified
- [ ] Data retrieved successfully
- [ ] Values compared against expected (if provided)
- [ ] Clear pass/fail result reported

**Output examples:**

```
[verify] Statistics Retrieved
[verify] ✓ Matched Groups: 5
[verify] ✓ Unmatched Files: 3
[verify] ✓ Total Images: 45
[verify] ✓ Abnormal Count: 0

[verify] Row Count Verification
[verify] Expected: 5, Actual: 5
[verify] Result: PASS ✓

[verify] Camera States
[verify] ✓ General: Connected (not capturing)
[verify] ✓ NIR 1: Connected (not capturing)
[verify] ✗ NIR 2: Disconnected
```
</success_criteria>

<troubleshooting>
**"MainWindow not found" error:**
- ChronoView may not be running
- Use /test:launch to start application

**"DataGrid not found" error:**
- MainWindow may not be fully initialized
- Wait longer after launching application
- Check if application is at SetupWindow

**Unexpected values:**
- Application state may not have settled yet
- Try waiting a few seconds and re-verify
- Check logs for errors that may explain the state

**Verification fails consistently:**
- May indicate a real bug in the application
- Use /test:logs to investigate
- Check console logs for exceptions
</troubleshooting>

<verification_patterns>
## Common Verification Patterns

| After Action | Verify Command | Expected Change |
|--------------|----------------|-----------------|
| Start monitoring | stats get | matchedGroups may increase |
| Stop monitoring | stats get | values should stabilize |
| Move groups | datagrid rows | rowCount should decrease |
| Delete groups | datagrid rows | rowCount should decrease |
| Launch camera | camera-states | camera.capturing = true |
| Stop camera | camera-states | camera.capturing = false |
| Open settings | settings-dialog status | isOpen = true |
| Close settings | settings-dialog status | isOpen = false |

## Assertion Examples

```bash
# Verify row count is exactly 5
/test/verify rows 5

# Verify no unmatched files
/test/verify unmatched 0

# Verify all cameras connected
/test/verify cameras connected

# Full verification suite
/test/verify all
```
</verification_patterns>
