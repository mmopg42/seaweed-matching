"""
ChronoView Test Agent - CLI Wrapper

Python wrapper for ui_automation.exe CLI commands.
Provides subprocess execution with JSON output parsing.
"""

import subprocess
import json
import os
from pathlib import Path
from typing import Optional, Dict, Any, List

import pytest
from pydantic import BaseModel, Field


class ChronoViewCLI:
    """Wrapper for ui_automation.exe CLI commands.

    This class wraps the ChronoView UI Automation CLI, allowing Python tests
    to call CLI commands via subprocess and parse JSON output.

    The CLI is automatically found in the default build location:
    skills_scripts/ui_automation/bin/Debug/net10.0/ui_automation.exe

    Example:
        cli = ChronoViewCLI()
        result = cli.run(["test", "connectivity"])
        if result["success"]:
            print(result["data"])
    """

    # Exit code constants (must match CLI constants)
    EXIT_SUCCESS = 0
    EXIT_ERROR = 1
    EXIT_NOT_FOUND = 2
    EXIT_TIMEOUT = 3
    EXIT_INVALID_ARGUMENT = 4

    def __init__(self, exe_path: Optional[str] = None):
        """Initialize the CLI wrapper.

        Args:
            exe_path: Optional path to ui_automation.exe. If not provided,
                     the wrapper will search in the default build location.
                     Can also be set via CHRONOVIEW_CLI environment variable.

        Raises:
            FileNotFoundError: If ui_automation.exe cannot be found.
        """
        self.exe_path = exe_path or self._find_exe()

    def _find_exe(self) -> str:
        """Find ui_automation.exe in default build location.

        Search order:
        1. CHRONOVIEW_CLI environment variable
        2. Debug build: ../../skills_scripts/ui_automation/bin/Debug/net10.0/ui_automation.exe
        3. Release build: ../../skills_scripts/ui_automation/bin/Release/net10.0/ui_automation.exe

        Returns:
            Absolute path to ui_automation.exe

        Raises:
            FileNotFoundError: If ui_automation.exe cannot be found.
        """
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

        raise FileNotFoundError(
            f"ui_automation.exe not found. Checked:\n"
            f"  - Environment variable CHRONOVIEW_CLI\n"
            f"  - {debug_path}\n"
            f"  - {release_path}\n"
            f"Build the CLI with: cd skills_scripts/ui_automation && dotnet build"
        )

    def run(self, args: List[str], timeout: int = 30) -> Dict[str, Any]:
        """Run CLI command and return parsed JSON result.

        Args:
            args: List of command arguments (e.g., ["test", "connectivity"])
            timeout: Timeout in seconds (default: 30)

        Returns:
            Dictionary with keys:
                - success (bool): True if command succeeded
                - data (dict): Response data on success
                - error (str): Error message on failure
                - exitCode (int): Process exit code

        Example:
            result = cli.run(["stats", "get"])
            if result["success"]:
                print(result["data"]["matchedGroups"])
        """
        # Build command with --json flag
        cmd = [self.exe_path] + args + ["--json"]

        try:
            result = subprocess.run(
                cmd,
                capture_output=True,
                text=True,
                timeout=timeout,
                check=False  # We handle exit codes manually
            )

            output = result.stdout.strip()

            # Parse JSON output
            try:
                response = json.loads(output) if output else {}
            except json.JSONDecodeError:
                response = {
                    "success": False,
                    "error": output or "No output from CLI",
                    "errorCode": self.EXIT_ERROR
                }

            # Add exit code to response
            response["exitCode"] = result.returncode

            # If no success field in response, infer from exit code
            if "success" not in response:
                response["success"] = result.returncode == self.EXIT_SUCCESS

            return response

        except subprocess.TimeoutExpired:
            return {
                "success": False,
                "error": f"Command timed out after {timeout} seconds",
                "errorCode": self.EXIT_TIMEOUT,
                "exitCode": self.EXIT_TIMEOUT
            }
        except FileNotFoundError:
            return {
                "success": False,
                "error": f"CLI executable not found: {self.exe_path}",
                "errorCode": self.EXIT_NOT_FOUND,
                "exitCode": self.EXIT_NOT_FOUND
            }
        except Exception as e:
            return {
                "success": False,
                "error": f"Unexpected error: {str(e)}",
                "errorCode": self.EXIT_ERROR,
                "exitCode": self.EXIT_ERROR
            }

    # Convenience methods for common CLI commands

    def test_connectivity(self, timeout: int = 10) -> Dict[str, Any]:
        """Test connectivity to ChronoView application.

        Returns:
            Response with connected=True if ChronoView is running.
        """
        return self.run(["test", "connectivity"], timeout=timeout)

    def get_statistics(self, timeout: int = 10) -> Dict[str, Any]:
        """Get current statistics from ChronoView.

        Returns:
            Response with data containing matchedGroups, unmatchedFiles, etc.
        """
        return self.run(["stats", "get"], timeout=timeout)

    def get_datagrid_rows(self, timeout: int = 10) -> Dict[str, Any]:
        """Get DataGrid rows from ChronoView.

        Returns:
            Response with data containing rows array.
        """
        return self.run(["datagrid", "rows"], timeout=timeout)

    def get_logs(self, tail: int = 10, timeout: int = 10) -> Dict[str, Any]:
        """Get log entries from ChronoView.

        Args:
            tail: Number of recent log entries to retrieve

        Returns:
            Response with data containing logs array.
        """
        return self.run(["logs", "get", "--tail", str(tail)], timeout=timeout)


# Module-level convenience function
def run_cli(args: List[str], timeout: int = 30) -> Dict[str, Any]:
    """Convenience function to run CLI command without instantiating class.

    Args:
        args: List of command arguments
        timeout: Timeout in seconds (default: 30)

    Returns:
        Same as ChronoViewCLI.run()

    Example:
        result = run_cli(["test", "connectivity"])
    """
    cli = ChronoViewCLI()
    return cli.run(args, timeout=timeout)


# =============================================================================
# Pydantic Models for Output Validation
# =============================================================================

class SuccessResponse(BaseModel):
    """Pydantic model for successful CLI responses.

    Provides type-safe validation of JSON output from the CLI.
    """
    success: bool = Field(default=True, description="Always true for success responses")
    data: Dict[str, Any] = Field(default_factory=dict, description="Response data")
    exitCode: int = Field(default=0, description="Process exit code")

    class Config:
        # Allow extra fields for flexibility
        extra = "allow"


class ErrorResponse(BaseModel):
    """Pydantic model for error CLI responses.

    Provides type-safe validation of error output from the CLI.
    """
    success: bool = Field(default=False, description="Always false for error responses")
    error: str = Field(description="Error message")
    errorCode: int = Field(description="Error code matching exit codes")
    exitCode: int = Field(description="Process exit code")

    class Config:
        extra = "allow"


# =============================================================================
# Pytest Fixtures
# =============================================================================

@pytest.fixture(scope="session")
def cli():
    """Shared CLI instance for all tests.

    This fixture is created once per test session and reused across all tests.
    It provides a ChronoViewCLI instance with auto-detected executable path.

    Example:
        def test_something(cli):
            result = cli.run(["stats", "get"])
            assert result["success"] is True
    """
    return ChronoViewCLI()


@pytest.fixture(scope="function")
def require_chronoview(cli):
    """Skip test if ChronoView is not running.

    This fixture tests connectivity before running the test.
    If ChronoView is not running, the test is skipped with a clear message.

    Example:
        def test_requires_chronoview(require_chronoview):
            # This test will be skipped if ChronoView is not running
            result = require_chronoview.get_statistics()
            assert result["data"]["matchedGroups"] >= 0
    """
    result = cli.run(["test", "connectivity"])

    # Check if ChronoView is running
    if not result.get("success") or not result.get("data", {}).get("connected"):
        pytest.skip("ChronoView not running - test skipped")

    yield cli


# =============================================================================
# Test Helpers
# =============================================================================

def assert_success(result: Dict[str, Any], message: str = "") -> None:
    """Assert that CLI command was successful.

    Args:
        result: Result dictionary from ChronoViewCLI.run()
        message: Optional message to include in assertion error

    Raises:
        AssertionError: If result["success"] is False
    """
    if not result.get("success"):
        error = result.get("error", "Unknown error")
        exit_code = result.get("exitCode", "N/A")
        fail_msg = f"CLI command failed (exit code: {exit_code}): {error}"
        if message:
            fail_msg = f"{message}: {fail_msg}"
        raise AssertionError(fail_msg)


def assert_has_data(result: Dict[str, Any], *keys: str) -> None:
    """Assert that result contains specified data keys.

    Args:
        result: Result dictionary from ChronoViewCLI.run()
        *keys: Data keys to check for existence

    Raises:
        AssertionError: If any key is missing from result["data"]
    """
    assert_success(result, "Cannot check data on failed response")

    data = result.get("data", {})
    missing = [k for k in keys if k not in data]

    if missing:
        raise AssertionError(f"Missing data keys: {', '.join(missing)}")
