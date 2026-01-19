---
name: test:simulate
description: Run Data Simulator to generate test data for ChronoView
argument-hint: [mode] [line] [--cleanup] [--show-config] [--target PATH]
allowed-tools:
  - Bash
  - AskUserQuestion
---

<objective>
Generate test data for ChronoView using the Data Simulator.

Two modes available:
- **dummy**: Generate black dummy images (no source data needed)
- **real**: Move existing files from source to target

**NEW**: Auto-detects target path from ChronoView config by default!

After simulation, use --cleanup to remove test data.
</objective>

<context>
Working directory: C:\workspace\seaweed\gui_kiro_v2

Data Simulator: task_helper/data_test/data_simulator.py

**Auto-detected test location (from ChronoView config):**
- Reads from: `%LOCALAPPDATA%\prische\ChronoView\config.json` (Windows)
- Uses configured watch folder path for the specified line
- Falls back to manual --target if config not found

Arguments: $ARGUMENTS
</context>

<process>
**1. Parse arguments**

Format: [mode] [line] [--cleanup] [--show-config] [--target PATH]
- mode: `dummy` (default) or `real`
- line: `line1` (default) or `line2`
- --cleanup: Clean test data after simulation
- --show-config: Show ChronoView config paths
- --target: Manual target path (overrides auto-detect)

**2. Show config (optional)**

First, verify ChronoView configuration:
```bash
python task_helper/data_test/data_simulator.py --cli --show-config
```

This displays all configured watch folder paths.

**3. Run Data Simulator CLI**

For dummy mode with auto-detected path:
```bash
python task_helper/data_test/data_simulator.py --cli --read-config --mode dummy --line line1
```

For dummy mode with manual path:
```bash
python task_helper/data_test/data_simulator.py --cli --mode dummy --target C:/temp/chronoview_test --line line1
```

For real mode:
```bash
python task_helper/data_test/data_simulator.py --cli --read-config --mode real --source /path/to/source --line line1
```

**4. Cleanup (if requested)**

With auto-detected path, cleanup uses the same path:
```bash
python task_helper/data_test/data_simulator.py --cli --read-config --mode dummy --line line1 --cleanup
```

**5. Verify cleanup**

```bash
# The auto-detected path should be empty or non-existent
```
</process>

<success_criteria>
- [ ] ChronoView config detected (or manual path specified)
- [ ] Data Simulator executed successfully
- [ ] Files generated in target location
- [ ] Cleanup removes all test data (if --cleanup specified)

**Output example:**
```
[simulate] Running in dummy mode for line1
[simulate]
[CLI] Auto-detected target from ChronoView config: /mnt/c/watch/General
[CLI] Dummy data mode: generating files to /mnt/c/watch/General
[CLI] Starting line1 simulation in dummy mode...
[CLI] ✓ Simulation completed: 440 files created
[simulate]
[simulate] ✓ Test data generated successfully
[simulate] 🗑️ Cleanup: removed 440 files from /mnt/c/watch/General
```
</success_criteria>

<cleanup>
## Test Data Cleanup

### Manual cleanup command:
```bash
# Use --show-config to see the path, then remove it
python task_helper/data_test/data_simulator.py --cli --show-config
rm -rf /path/to/watch/folder
```

### Automatic cleanup:
```bash
/test/simulate dummy line1 --cleanup
```

### Verify cleanup:
```bash
ls /mnt/c/watch  # files should be gone
```
</cleanup>

<usage_examples>
## Usage Examples

```bash
# Show ChronoView config paths
/test/simulate --show-config

# Generate dummy data (auto-detects path from config)
/test/simulate dummy

# Generate dummy data for line2
/test/simulate dummy line2

# Generate with manual path
/test/simulate dummy line1 --target /custom/path

# Generate and cleanup after
/test/simulate dummy line1 --cleanup

# Real data mode with auto-detect
/test/simulate real line1 --source /path/to/source

# Real data mode with cleanup
/test/simulate real line1 --source /path/to/source --cleanup
```

## Complete Test Workflow

```bash
# 1. Start ChronoView
/test/launch start

# 2. Check current configuration
/test/simulate --show-config

# 3. Start monitoring (uses configured watch paths)
/test/action start

# 4. Generate test data (auto-detects same path)
/test/simulate dummy

# 5. Verify files detected
/test/verify stats
/test/verify rows

# 6. Check logs
/test/logs panel tail 20

# 7. Cleanup test data
/test/simulate dummy --cleanup
```
</usage_examples>
