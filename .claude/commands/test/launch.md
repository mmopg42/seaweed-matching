---
name: test:launch
description: Launch ChronoView application and verify connectivity
argument-hint: [action: start|stop|restart|status]
allowed-tools:
  - Bash
  - AskUserQuestion
---

<objective>
Launch the ChronoView application and verify it is running and accessible via UI Automation.

This skill manages the ChronoView application lifecycle:
- **start**: Launch ChronoView and verify connectivity
- **stop**: Gracefully shutdown ChronoView
- **restart**: Stop and restart ChronoView
- **status**: Check if ChronoView is running and responsive
</objective>

<context>
Working directory: C:\workspace\seaweed\gui_kiro_v2

ChronoView project: ChronoView/ChronoView.csproj
UI Automation CLI: skills_scripts/ui_automation/bin/Debug/net10.0/ui_automation.exe

Action: $ARGUMENTS
</context>

<process>
**1. Determine action**

If no argument provided, default to "start".

**2. For start action:**

Build the project first:
```bash
dotnet build ChronoView/ChronoView.csproj --nologo
```

Launch ChronoView in background (Windows):
```bash
# Start in background using PowerShell
powershell -Command 'Start-Process -FilePath "dotnet" -ArgumentList "run --project ChronoView/ChronoView.csproj" -NoNewWindow -RedirectStandardOutput "nul" -RedirectStandardError "nul"'

# Alternative: Use start command with background execution
start /B dotnet run --project ChronoView/ChronoView.csproj > nul 2>&1
```

Wait for application to initialize (3-5 seconds).

Verify connectivity using UI Automation CLI:
```bash
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- test connectivity --json
```

Check the JSON response:
- `success: true` + `data.connected: true` → ChronoView is running and MainWindow found
- `success: false` → Check for SetupWindow and complete setup:

**Handle SetupWindow (if present):**
```bash
# Check if SetupWindow is present
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- windows setup

# If SetupWindow found, automatically complete setup
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- windows setup-complete --json
```

After setup-complete, verify MainWindow is available:
```bash
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- test connectivity --json
```

**3. For stop action:**

Find and terminate ChronoView process:
```bash
# Find ChronoView process
Get-Process | Where-Object { $_.ProcessName -like "*ChronoView*" }

# Gracefully shutdown via UI Automation (preferred - click X button)
# Or force kill:
Stop-Process -Name "ChronoView" -Force
```

Verify process is terminated.

**4. For restart action:**

Execute stop action, wait 2 seconds, then execute start action.

**5. For status action:**

Check connectivity:
```bash
dotnet run --project skills_scripts/ui_automation/ui_automation.csproj -- test connectivity --json
```

Report connection status, MainWindow title, and process info.
</process>

<success_criteria>
- [ ] Correct action determined from arguments
- [ ] Project builds successfully (for start/restart)
- [ ] ChronoView process is running (for start/restart/status)
- [ ] UI Automation connectivity confirmed (success=true, connected=true)
- [ ] Appropriate feedback provided to user

**Success output example:**
```
[launch] ChronoView started successfully
[launch] Process ID: 12345
[launch] MainWindow detected: "ChronoView"
[launch] Connectivity: CONFIRMED
```
</success_criteria>

<troubleshooting>
**ChronoView won't start:**
- Check if another instance is already running
- Verify build succeeded
- Check for port conflicts (camera ports)

**Connectivity fails:**
- ChronoView may be at SetupWindow instead of MainWindow
- Try waiting longer for app initialization
- Check if app is stuck on splash screen

**Process won't terminate:**
- Use Task Manager to verify process name
- Try graceful shutdown via UI first
- Force kill as last resort
</troubleshooting>
