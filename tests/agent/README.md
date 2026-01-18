# ChronoView Test Agent

Python-based test agent for automated ChronoView CLI testing via subprocess.

## Quick Start

```bash
# 1. Install dependencies
pip install -r requirements.txt

# 2. Build CLI (if not already built)
cd ../../skills_scripts/ui_automation
dotnet build

# 3. Run tests (non-destructive only)
cd ../../tests/agent
python run_tests.py

# 4. Run all tests including destructive
python run_tests.py --all
```

## Architecture

This test agent uses a **subprocess-based architecture**:

```
Python Test Agent (pytest)
       |
       v
subprocess.run() calls
       |
       v
ui_automation.exe (C# CLI)
       |
       v
ChronoView Application (WPF)
```

### Why Subprocess Instead of Direct Automation?

1. **Language-agnostic**: The CLI can be called from any test framework (Python, JavaScript, Go, etc.)
2. **Simple**: No need to maintain C# bindings or interop code
3. **Maintainable**: CLI already implements --json output and exit codes
4. **Isolated**: Test failures don't affect the application state

## Requirements

1. **ChronoView running** - Tests will skip if ChronoView is not detected
2. **ui_automation.exe built** - Build the CLI first:

```bash
cd ../../skills_scripts/ui_automation
dotnet build -c Release
```

3. **Python 3.10+** - Required for pytest and pydantic

4. **Test data** - Destructive tests need data in ChronoView

## Test Structure

- `test_connectivity.py` - Smoke tests, verify ChronoView is accessible
- `test_workflow.py` - Start/stop, configuration workflows
- `test_file_operations.py` - Data reading, move operations (destructive)

## Running Tests

### Using the run script (recommended)

```bash
# Run non-destructive tests only
python run_tests.py

# Run all tests including destructive operations
python run_tests.py --all

# Run with custom pytest arguments
python run_tests.py -v -k "test_connectivity"
```

### Using pytest directly

```bash
# Run all tests
pytest

# Run with verbose output
pytest -v

# Generate HTML report
pytest --html=reports/report.html --self-contained-html

# Run specific test file
pytest test_connectivity.py

# Run specific test
pytest test_connectivity.py::TestConnectivity::test_chronoview_is_running

# Skip connection checks (test without ChronoView running)
pytest -k "not require_chronoview"
```

## Test Markers

The test agent uses pytest markers to categorize tests:

| Marker | Description | Usage |
|--------|-------------|-------|
| `destructive` | Tests that modify data or application state | `pytest -m "not destructive"` to skip |
| `slow` | Long-running tests | `pytest -m "not slow"` to skip |
| `order` | Test execution order dependency | Used by test class ordering |

### Example: Skip destructive tests

```bash
# Only run non-destructive tests
pytest -m "not destructive"

# Only run destructive tests
pytest -m "destructive"
```

## HTML Reports

After running tests, an HTML report is generated at `reports/report.html`:

```bash
# Open the report
start reports/report.html  # Windows
open reports/report.html   # macOS
xdg-open reports/report.html  # Linux
```

The report includes:
- Test results summary
- Individual test details
- Error messages and stack traces
- Execution time per test

## Troubleshooting

### "ui_automation.exe not found"

**Problem:** The CLI executable cannot be found.

**Solution:** Build the CLI first:

```bash
cd ../../skills_scripts/ui_automation
dotnet build -c Release
```

Or set the `CHRONOVIEW_CLI` environment variable:

```bash
# Windows
set CHRONOVIEW_CLI=C:\path\to\ui_automation.exe

# Linux/macOS
export CHRONOVIEW_CLI=/path/to/ui_automation.exe
```

### "ChronoView not running"

**Problem:** Tests are being skipped because ChronoView is not detected.

**Solution:** Start ChronoView before running tests. The test agent uses UI Automation to find the ChronoView main window.

### "No data in DataGrid"

**Problem:** Destructive tests are being skipped.

**Solution:** Destructive tests (move, delete) require test data in ChronoView. Load test data or use non-destructive tests only:

```bash
python run_tests.py  # Non-destructive only
```

### "pytest: command not found"

**Problem:** pytest is not installed.

**Solution:** Install dependencies:

```bash
pip install -r requirements.txt
```

## Test Fixtures

### `cli` (session-scoped)

Shared CLI instance for all tests:

```python
def test_something(cli):
    result = cli.run(["stats", "get"])
    assert result["success"] is True
```

### `require_chronoview` (function-scoped)

Skips test if ChronoView is not running:

```python
def test_requires_chronoview(require_chronoview):
    # This test will be skipped if ChronoView is not running
    result = require_chronoview.get_statistics()
    assert result["data"]["matchedGroups"] >= 0
```

### `require_datagrid_rows` (function-scoped)

Skips test if DataGrid doesn't have enough rows:

```python
def test_with_data(require_datagrid_rows):
    # This test will be skipped if DataGrid has no rows
    result = require_datagrid_rows.run(["datagrid", "data"])
    assert len(result["data"]["data"]) > 0
```

## Exit Codes

The CLI uses standard exit codes:

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Error |
| 2 | Not Found |
| 3 | Timeout |
| 4 | Invalid Argument |

## JSON Output Format

Success:
```json
{
  "success": true,
  "data": { ... }
}
```

Error:
```json
{
  "success": false,
  "error": "Error message",
  "errorCode": 1
}
```

## Continuous Integration

Example GitHub Actions workflow:

```yaml
name: Test ChronoView Agent

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0'
      - name: Build CLI
        run: |
          cd skills_scripts/ui_automation
          dotnet build -c Release
      - name: Setup Python
        uses: actions/setup-python@v4
        with:
          python-version: '3.11'
      - name: Install dependencies
        run: |
          cd tests/agent
          pip install -r requirements.txt
      - name: Run non-destructive tests
        run: |
          cd tests/agent
          python run_tests.py
      - name: Upload HTML report
        uses: actions/upload-artifact@v3
        if: always()
        with:
          name: test-report
          path: tests/agent/reports/report.html
```

## Pre-flight Checklist

Before running tests:

- [ ] ChronoView application is running
- [ ] ui_automation.exe is built (`dotnet build` in skills_scripts/ui_automation)
- [ ] Python dependencies installed (`pip install -r requirements.txt`)
- [ ] For destructive tests: Test data is loaded in ChronoView
- [ ] reports/ directory exists (created automatically by run_tests.py)
