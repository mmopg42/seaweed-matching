# ChronoView Test Agent

Python-based test agent for automated ChronoView CLI testing via subprocess.

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

## Prerequisites

1. **.NET 10.0 SDK** - Required to build the ui_automation CLI
2. **Built CLI executable** - Run the following before testing:

```bash
cd skills_scripts/ui_automation
dotnet build -c Release
```

The CLI executable will be at:
```
skills_scripts/ui_automation/bin/Release/net10.0/ui_automation.exe
```

## Setup

1. Install Python dependencies:

```bash
cd tests/agent
pip install -r requirements.txt
```

2. Verify ui_automation.exe is built:

```bash
# The agent will auto-find the CLI in the default build location
# Or set environment variable:
set CHRONOVIEW_CLI=path\to\ui_automation.exe
```

3. Start ChronoView application (the agent will skip tests if not running)

## Running Tests

Run all tests:
```bash
pytest
```

Run with verbose output:
```bash
pytest -v
```

Generate HTML report:
```bash
pytest --html=report.html
```

Run specific test file:
```bash
pytest test_connectivity.py
```

Skip connection checks (test without ChronoView running):
```bash
pytest -k "not require_chronoview"
```

## Test Structure

- `ChronoViewCLI` - Wrapper class for subprocess CLI calls
- `SuccessResponse` / `ErrorResponse` - Pydantic models for JSON validation
- `cli` fixture - Session-scoped CLI instance
- `require_chronoview` fixture - Skips tests if ChronoView not running

## Example Test

```python
def test_get_statistics(cli, require_chronoview):
    result = cli.run(["stats", "get"])
    assert result["success"] == True
    assert "data" in result
    assert result["data"]["matchedGroups"] >= 0
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
