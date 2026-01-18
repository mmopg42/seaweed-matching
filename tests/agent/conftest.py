"""
Pytest Configuration and Shared Fixtures

This file contains pytest configuration and shared fixtures for the
ChronoView test agent. Fixtures defined here are automatically available
to all test modules in this directory.

Key fixtures:
- cli: Session-scoped ChronoViewCLI instance
- require_chronoview: Function-scoped fixture that skips tests if ChronoView not running
"""

import pytest
import sys
from pathlib import Path

# Add the parent directory to the path so we can import ChronoViewTestAgent
sys.path.insert(0, str(Path(__file__).parent))

from ChronoViewTestAgent import ChronoViewCLI


# =============================================================================
# Pytest Configuration
# =============================================================================

def pytest_configure(config):
    """Configure pytest with custom markers and settings.

    This function is called once at the start of the test run.

    Args:
        config: pytest config object
    """
    # Register custom markers
    config.addinivalue_line(
        "markers",
        "order: Mark tests to run in a specific order (e.g., 'order(after=\"test_xxx.py\")')"
    )
    config.addinivalue_line(
        "markers",
        "destructive: Mark tests that modify data or application state"
    )
    config.addinivalue_line(
        "markers",
        "requires_data: Mark tests that require data to be present in the application"
    )


def pytest_collection_modifyitems(config, items):
    """Modify collected test items to add markers and configure ordering.

    This function is called after test collection but before running tests.

    Args:
        config: pytest config object
        items: List of test items to be run
    """
    # Automatically mark tests that use require_chronoview fixture
    for item in items:
        if "require_chronoview" in item.fixturenames:
            item.add_marker(pytest.mark.usefixtures("require_chronoview"))

        # Add markers based on test class names
        if item.parent and item.parent.name:
            parent_name = item.parent.name
            if "Destructive" in parent_name or "FileOperations" in parent_name:
                item.add_marker(pytest.mark.destructive)
            if "Batch" in parent_name:
                item.add_marker(pytest.mark.destructive)


# =============================================================================
# CLI Path Configuration
# =============================================================================

def get_cli_path():
    """Get the path to ui_automation.exe.

    This function searches for the CLI executable in the following order:
    1. CHRONOVIEW_CLI environment variable
    2. Debug build location
    3. Release build location

    Returns:
        str: Path to ui_automation.exe

    Raises:
        FileNotFoundError: If ui_automation.exe cannot be found
    """
    import os

    # Check environment variable first
    env_path = os.environ.get("CHRONOVIEW_CLI")
    if env_path and Path(env_path).exists():
        return str(Path(env_path).resolve())

    # Get the tests/agent directory
    agent_dir = Path(__file__).parent.resolve()

    # Check Debug build location (default for development)
    debug_path = agent_dir.parent.parent / "skills_scripts" / "ui_automation" / "bin" / "Debug" / "net10.0" / "ui_automation.exe"
    if debug_path.exists():
        return str(debug_path)

    # Check Release build location
    release_path = agent_dir.parent.parent / "skills_scripts" / "ui_automation" / "bin" / "Release" / "net10.0" / "ui_automation.exe"
    if release_path.exists():
        return str(release_path)

    # As a last resort, try checking if the path exists via PATH
    import shutil
    cli_in_path = shutil.which("ui_automation.exe")
    if cli_in_path:
        return str(cli_in_path)

    raise FileNotFoundError(
        f"ui_automation.exe not found. Checked:\n"
        f"  - Environment variable CHRONOVIEW_CLI\n"
        f"  - {debug_path}\n"
        f"  - {release_path}\n"
        f"Build the CLI with: cd skills_scripts/ui_automation && dotnet build"
    )


# =============================================================================
# Pytest Fixtures
# =============================================================================

@pytest.fixture(scope="session")
def cli():
    """Shared CLI instance for all tests.

    This fixture is created once per test session and reused across all tests.
    It provides a ChronoViewCLI instance with auto-detected executable path.

    The fixture is session-scoped for efficiency - creating a new CLI instance
    for each test would be wasteful since the CLI is just a wrapper around
    subprocess calls.

    Example:
        def test_something(cli):
            result = cli.run(["stats", "get"])
            assert result["success"] is True

    Yields:
        ChronoViewCLI: Configured CLI instance
    """
    cli_path = get_cli_path()
    cli_instance = ChronoViewCLI(exe_path=cli_path)

    yield cli_instance


@pytest.fixture(scope="function")
def require_chronoview(cli):
    """Skip test if ChronoView is not running.

    This fixture tests connectivity before running the test.
    If ChronoView is not running, the test is skipped with a clear message.

    Use this fixture in tests that require ChronoView to be running:

    Example:
        def test_requires_chronoview(require_chronoview):
            # This test will be skipped if ChronoView is not running
            result = require_chronoview.get_statistics()
            assert result["data"]["matchedGroups"] >= 0

    Yields:
        ChronoViewCLI: The CLI instance (same as cli fixture)

    Raises:
        pytest.skip.Exception: If ChronoView is not running
    """
    # Test connectivity
    result = cli.run(["test", "connectivity"])

    # Check if ChronoView is running
    if not result.get("success"):
        pytest.skip(f"ChronoView not running - CLI error: {result.get('error', 'Unknown')}")
    if not result.get("data", {}).get("connected"):
        pytest.skip("ChronoView not running - MainWindow not found")

    yield cli


@pytest.fixture(scope="function")
def require_datagrid_rows(cli, require_chronoview, min_rows: int = 1):
    """Skip test if DataGrid doesn't have enough rows.

    This fixture extends require_chronoview by also checking for
    minimum row count in the DataGrid.

    Args:
        cli: CLI instance (injected)
        require_chronoview: Connectivity check (injected)
        min_rows: Minimum number of rows required (default: 1)

    Example:
        def test_with_data(require_datagrid_rows):
            # This test will be skipped if ChronoView is not running
            # or if DataGrid has no rows
            result = require_datagrid_rows.run(["datagrid", "data"])
            assert len(result["data"]["data"]) > 0

    Yields:
        ChronoViewCLI: The CLI instance

    Raises:
        pytest.skip.Exception: If ChronoView not running or insufficient data
    """
    # Get row count
    result = cli.run(["datagrid", "rows"])

    if not result.get("success"):
        pytest.skip("Could not retrieve DataGrid row count")

    row_count = result.get("data", {}).get("rowCount", 0)

    if row_count < min_rows:
        pytest.skip(f"DataGrid has {row_count} rows, need at least {min_rows}")

    yield cli


@pytest.fixture(scope="function")
def require_settings_dialog(cli, require_chronoview):
    """Ensure SettingsDialog is open for the test.

    This fixture opens the SettingsDialog before the test and closes it after.

    Example:
        def test_settings_dialog(require_settings_dialog):
            result = require_settings_dialog.run(["settings-dialog", "path", "get-all"])
            assert result["success"]

    Yields:
        ChronoViewCLI: The CLI instance

    Raises:
        pytest.skip.Exception: If dialog cannot be opened
    """
    # Try to open the dialog
    result = cli.run(["settings-dialog", "open"])

    if not result.get("success") and result.get("exitCode") != 0:
        pytest.skip("Could not open SettingsDialog")

    # Give the dialog a moment to appear
    import time
    time.sleep(0.5)

    yield cli

    # Clean up - close the dialog
    try:
        cli.run(["settings-dialog", "close"])
    except Exception:
        # Ignore errors during cleanup
        pass


# =============================================================================
# Test Helpers
# =============================================================================

@pytest.fixture(scope="session")
def cli_path():
    """Get the CLI executable path as a string.

    This is useful for tests that need the raw path.

    Example:
        def test_cli_exists(cli_path):
            assert Path(cli_path).exists()

    Returns:
        str: Path to ui_automation.exe
    """
    return get_cli_path()


@pytest.fixture(scope="session")
def cli_dir(cli_path):
    """Get the directory containing the CLI executable.

    Example:
        def test_cli_files(cli_dir):
            assert (cli_dir / "ui_automation.exe").exists()

    Returns:
        Path: Directory containing ui_automation.exe
    """
    return Path(cli_path).parent


# =============================================================================
# Hooks and Reporting
# =============================================================================

def pytest_report_header(config):
    """Add custom header to pytest output.

    This function is called to generate the header text shown at the
    start of the test run.

    Args:
        config: pytest config object

    Returns:
        str: Header text to display
    """
    try:
        cli_path_str = get_cli_path()
        return [
            f"ChronoView Test Agent",
            f"CLI Path: {cli_path_str}",
        ]
    except FileNotFoundError as e:
        return [
            f"ChronoView Test Agent",
            f"WARNING: {str(e)}",
        ]


def pytest_sessionstart(session):
    """Called after the Session object has been created.

    This function is called once at the start of the test session.

    Args:
        session: pytest Session object
    """
    # Verify CLI is available before running tests
    try:
        get_cli_path()
    except FileNotFoundError as e:
        print(f"\nWARNING: {e}")
        print("Tests will be skipped if CLI is required.")


def pytest_terminal_summary(terminalreporter, exitstatus, config):
    """Add a summary section to the terminal reporting.

    This function is called at the end of the test run to add
    additional summary information.

    Args:
        terminalreporter: TerminalReporter object
        exitstatus: Exit code
        config: pytest config object
    """
    # Add a note about skipped tests
    skipped = terminalreporter.stats.get("skipped", [])
    if skipped:
        terminalreporter.write_sep("=", "yellow")
        terminalreporter.write_line(
            f"Note: {len(skipped)} test(s) were skipped. "
            "This may be because ChronoView is not running "
            "or no test data is available.",
            yellow=True
        )
