# Plan 22-03 Summary: Settings Verification

**Status:** COMPLETE
**Date:** 2026-01-20
**Commits:** 2 atomic commits

---

## Deliverables

### 1. SetupConfigVerifier.cs (518 lines)
New class for configuration verification between data simulator and ChronoView.

**Key Methods:**
- `ReadSimulatorConfig()` - Parse simulator_config.json
- `ReadChronoViewPaths()` - Read paths via ChronoSettingsController
- `ComparePaths()` - Compare simulator and ChronoView paths
- `NormalizePath()` - Handle WSL/Windows path differences
- `Verify()` - Full verification workflow

**Supporting Types:**
- `VerificationResult` - Result with match/mismatch details
- `PathComparisonDetail` - Individual path comparison
- `PathMismatch` - Mismatch details with reason

### 2. SetupCommands.cs (301 lines)
New command handler for setup window automation.

**Commands:**
- `setup verify-config` - Compare simulator and ChronoView settings
- `setup complete-full` - Full setup workflow with optional config verification

**Options:**
- `--config-path PATH` - Simulator config file path
- `--open-settings` - Open SettingsDialog if not open
- `--strict` - Fail on config mismatches
- `--json` - JSON output for automation

---

## Technical Decisions

### Path Normalization
- WSL paths (`/mnt/c/...`) converted to Windows (`C:\...`)
- Trailing slashes removed
- Case-insensitive comparison
- All separators normalized to backslash

### Mapping Strategy
Simulator keys map to ChronoView keys for comparison:
- `source_line1` → `line1_nir1`, `line1_normal1`
- `source_line2` → `line2_nir2`, `line2_normal2`
- `target_base` → `output`
- `move_folder` → `quarantine`
- `trash_folder` → `quarantine`

### Exit Codes
- `0` - Success
- `1` - Error (with `--strict`)
- `2` - Config file not found
- `4` - Invalid argument

---

## Integration Points

**Uses:**
- `ChronoSettingsController` - Read SettingsDialog paths
- `ChronoSetupWindowController` - Setup window control in complete-full

**Used By:**
- `CommandRegistry` - Registered in Program.cs
- Agents via CLI commands

---

## Testing Notes

Test scenarios:
1. Create test simulator_config.json
2. Run: `setup verify-config --config-path <path> --json`
3. Verify output shows matches/mismatches
4. Test with WSL paths (`/mnt/c/...`)
5. Test with Windows paths (`C:\...`)
6. Test with missing config file
7. Test with missing keys in config
8. Integrate with `setup complete-full --verify-config`

---

## Verification Criteria Met

- [x] SetupConfigVerifier.cs compiles without errors
- [x] Can read simulator_config.json file
- [x] Can read ChronoView paths via SettingsDialog
- [x] Path comparison works with normalized paths
- [x] `setup verify-config` command outputs valid JSON
- [x] Verification accurately reports matches and mismatches
- [x] Works with both Line 1 and Line 2 configurations

---

## Files Modified

**Created:**
- `skills_scripts/ui_automation/SetupConfigVerifier.cs`
- `skills_scripts/ui_automation/Commands/SetupCommands.cs`

**Modified:**
- `skills_scripts/ui_automation/Program.cs` - Registered SetupCommands

---

## Next Steps

Phase 22 complete. Proceed to Phase 23 (Test Reliability) or validate v1.3 milestone completion.
