---
name: test:scenario
description: Run end-to-end test scenarios on ChronoView
argument-hint: [scenario name]
allowed-tools:
  - Bash
  - AskUserQuestion
---

<objective>
Execute comprehensive end-to-end test scenarios that verify complete workflows in ChronoView.

Each scenario performs a sequence of actions and verifies the results.
</objective>

<context>
Working directory: C:\workspace\seaweed\gui_kiro_v2

UI Automation CLI: skills_scripts/ui_automation/bin/Debug/net10.0/ui_automation.csproj

Arguments: $ARGUMENTS
</context>

<process>
**1. Parse scenario name**

Available scenarios:
- `start-monitoring` - Complete workflow: setup → start → verify
- `configure-paths` - Configure all monitoring paths
- `camera-test` - Test all camera launching
- `file-operations` - Test file move/delete operations

**2. Scenario: start-monitoring**

Step-by-step workflow:
1. Launch ChronoView (if not running)
2. Configure paths (SetupWindow or SettingsDialog)
3. Click Start button
4. Wait for monitoring to begin
5. Verify monitoring state
6. Check for errors in logs

Commands:
```bash
# Ensure app is running
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- test connectivity

# Start monitoring
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- toolbar start

# Wait and verify
sleep 3
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- logs tail 10

# Check for errors
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- logs filter --level ERROR
```

**3. Scenario: configure-paths**

1. Open SettingsDialog
2. Configure Line 1 paths (General, Cam1, Cam2, Cam3)
3. Configure Line 2 paths (Cam4, Cam5, Cam6)
4. Configure Output path
5. Save settings
6. Verify paths were saved

Commands:
```bash
# Open settings
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- settings-dialog open

# Set paths (example)
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- settings-dialog path set --type line1-general --path "/mnt/c/watch/General"

# Save and close
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- settings-dialog action save

# Verify
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- settings-dialog path get-all
```

**4. Scenario: camera-test**

1. Verify initial camera states (all disconnected)
2. Launch General Camera
3. Verify General Camera state
4. Launch NIR 1 Camera
5. Verify NIR 1 Camera state
6. Launch NIR 2 Camera
7. Verify NIR 2 Camera state
8. Check logs for any errors

Commands:
```bash
# Initial state
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- workflow camera-states

# Launch each camera
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- workflow launch-general
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- workflow launch-nir
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- workflow launch-nir2

# Verify each launch
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- workflow camera-states

# Check for errors
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- console-logs search "Camera" --max 20
```

**5. Scenario: file-operations**

1. Verify initial row count
2. Select rows (if available)
3. Execute move operation
4. Verify row count decreased
5. Check logs for success/error
6. Execute delete operation (if applicable)
7. Verify final state

Commands:
```bash
# Initial state
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- datagrid rows

# Move operation (requires row selection first)
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- file-ops select-all
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- file-ops move

# Wait for completion
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- file-ops wait move --timeout 30

# Verify
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- datagrid rows
```

**6. Report scenario results**

For each scenario:
- List steps executed
- Show pass/fail for each verification
- Highlight any errors found
- Provide overall verdict
</process>

<success_criteria>
- [ ] Scenario identified from arguments
- [ ] All steps executed in order
- [ ] Verifications performed after each step
- [ ] Errors logged and reported
- [ ] Final verdict provided

**Output example:**

```
[scenario] Running: start-monitoring
[scenario]
[scenario] Step 1: Check connectivity... PASS ✓
[scenario] Step 2: Click Start button... PASS ✓
[scenario] Step 3: Wait for initialization... PASS ✓
[scenario] Step 4: Verify monitoring state... PASS ✓
[scenario] Step 5: Check for errors... PASS ✓ (0 errors)
[scenario]
[scenario] Overall Result: PASS ✓
[scenario] Monitoring is running successfully
```
</success_criteria>

<troubleshooting>
**Scenario fails at step 1 (connectivity):**
- Use /test:launch to start ChronoView first
- Check if app is stuck on SetupWindow

**Scenario fails at step 2 (button click):**
- Button may be disabled in current state
- Check logs for context on why action failed

**Scenario fails at verification:**
- Wait time may be insufficient (increase delays)
- Application may have different behavior than expected
- Check console logs for underlying errors

**Scenario partial success:**
- Some steps may have passed while others failed
- Review logs to understand what worked and what didn't
- May indicate intermittent issue or timing problem
</troubleshooting>

<scenario_reference>
## Available Scenarios

| Scenario | Description | Duration | Prerequisites |
|----------|-------------|----------|---------------|
| start-monitoring | Start monitoring workflow | ~30s | Paths configured |
| configure-paths | Configure all paths | ~2min | App running |
| camera-test | Test all camera launches | ~1min | App running |
| file-operations | Test move/delete operations | ~1min | Data in grid |

## Creating Custom Scenarios

To create a custom scenario:
1. Define the objective
2. List step-by-step actions
3. Add verification after each step
4. Define success criteria
5. Handle error cases

Example framework:
```
1. Pre-condition verification
2. Execute action sequence
3. Post-condition verification
4. Log analysis
5. Report generation
```
</scenario_reference>
