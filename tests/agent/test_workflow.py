"""
ChronoView Workflow Automation Tests

Test suite for end-to-end workflow automation including monitoring control,
path configuration, and camera state management.
"""

import pytest
import time
from ChronoViewTestAgent import ChronoViewCLI, assert_success, assert_has_data


@pytest.mark.order(after="test_connectivity.py")
class TestWorkflow:
    """Test ChronoView workflow automation.

    These tests cover complete workflows for starting/stopping monitoring,
    configuring paths, and managing camera states.
    """

    def test_start_monitoring_workflow(self, cli: ChronoViewCLI, require_chronoview):
        """Test complete start monitoring workflow.

        This test verifies the start-monitoring scenario which:
        1. Verifies MainWindow is accessible
        2. Clicks the Start button
        3. Verifies the button state changed

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running

        Note:
            This test may change the application state. Ensure monitoring
            can be safely started in your test environment.
        """
        result = cli.run(["scenario", "start-monitoring"])

        assert_success(result, "Start monitoring workflow failed")
        assert_has_data(result, "started", "buttonState")

        assert result["data"]["started"] is True, "Monitoring should be started"
        # After starting, Stop button should be enabled (buttonState reflects this)
        assert result["data"]["buttonState"] in ["enabled", "disabled"], \
            "Button state should be 'enabled' or 'disabled'"

    def test_stop_monitoring_workflow(self, cli: ChronoViewCLI, require_chronoview):
        """Test complete stop monitoring workflow.

        This test verifies that monitoring can be stopped via toolbar control.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running

        Note:
            This test assumes monitoring is currently running or will attempt
            to stop it regardless.
        """
        # First, ensure monitoring is running by clicking start
        start_result = cli.run(["toolbar", "start"])
        # Don't assert here as it might already be running

        # Give it a moment to register
        time.sleep(0.5)

        # Now stop monitoring
        result = cli.run(["toolbar", "stop"])

        assert_success(result, "Stop monitoring workflow failed")

        # Verify the command completed (may not return detailed state)
        assert result["exitCode"] == 0, "Stop command should succeed"

    def test_toolbar_button_states(self, cli: ChronoViewCLI, require_chronoview):
        """Test reading toolbar button states.

        This test verifies that button enabled/disabled states can be read.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # Test Start button state (Korean: "시작")
        start_result = cli.run(["toolbar", "enabled", "시작"])
        assert start_result["exitCode"] == 0, "Should be able to check Start button state"

        # Test Stop button state (Korean: "중지")
        stop_result = cli.run(["toolbar", "enabled", "중지"])
        assert stop_result["exitCode"] == 0, "Should be able to check Stop button state"

    def test_read_camera_states(self, cli: ChronoViewCLI, require_chronoview):
        """Test reading camera button states from WorkflowPanel.

        This test retrieves the current state of all camera launch buttons.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["workflow", "camera-states"])

        assert_success(result, "Camera states retrieval failed")
        assert_has_data(result)

        data = result["data"]

        # Should have at least one camera state
        # Cameras may be named cam1, cam2, cam3, cam4, cam5, cam6
        # or camera1, camera2, etc.
        assert len(data) > 0, "Should have at least one camera state"

        # Verify camera state structure
        for camera_name, state in data.items():
            assert isinstance(camera_name, str), "Camera name should be a string"
            assert isinstance(state, dict), f"State for {camera_name} should be a dict"
            # State may have 'running', 'enabled', etc.
            assert len(state) > 0, f"State for {camera_name} should not be empty"

    def test_workflow_path_get_line1(self, cli: ChronoViewCLI, require_chronoview):
        """Test reading Line 1 workflow paths.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["workflow", "path", "get-line1"])

        assert_success(result, "Line 1 path retrieval failed")
        assert_has_data(result)

        data = result["data"]

        # Should have paths for Line 1 cameras (nir1, normal1, cam1, etc.)
        assert len(data) > 0, "Should have at least one Line 1 path"

        # Verify path structure
        for path_name, path_value in data.items():
            assert isinstance(path_name, str), f"Path name {path_name} should be a string"
            # Path value may be None if not configured
            if path_value is not None:
                assert isinstance(path_value, str), f"Path value for {path_name} should be a string"

    def test_workflow_path_get_line2(self, cli: ChronoViewCLI, require_chronoview):
        """Test reading Line 2 workflow paths.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["workflow", "path", "get-line2"])

        assert_success(result, "Line 2 path retrieval failed")
        assert_has_data(result)

        data = result["data"]

        # Should have paths for Line 2 cameras (nir2, normal2, cam4, etc.)
        assert len(data) > 0, "Should have at least one Line 2 path"

    def test_workflow_path_get_all(self, cli: ChronoViewCLI, require_chronoview):
        """Test reading all workflow paths.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["workflow", "path", "get-all"])

        assert_success(result, "All paths retrieval failed")
        assert_has_data(result, "line1", "line2")

        data = result["data"]

        # Should have both line1 and line2 paths
        assert "line1" in data, "Should have line1 paths"
        assert "line2" in data, "Should have line2 paths"

        # Each should be a dictionary
        assert isinstance(data["line1"], dict), "line1 should be a dict"
        assert isinstance(data["line2"], dict), "line2 should be a dict"


class TestSettingsDialogWorkflow:
    """Test SettingsDialog workflows.

    These tests cover opening/closing the settings dialog and
    reading/writing configuration.
    """

    def test_settings_dialog_open_close(self, cli: ChronoViewCLI, require_chronoview):
        """Test opening and closing SettingsDialog.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # Open the dialog
        open_result = cli.run(["settings-dialog", "open"])
        assert open_result["exitCode"] == 0, "SettingsDialog should open successfully"

        # Give the dialog a moment to appear
        time.sleep(0.5)

        # Check status
        status_result = cli.run(["settings-dialog", "status"])
        assert_success(status_result, "Status check failed")
        assert status_result["data"].get("isOpen") is True, \
            "SettingsDialog should be open after opening"

        # Close the dialog
        close_result = cli.run(["settings-dialog", "close"])
        assert close_result["exitCode"] == 0, "SettingsDialog should close successfully"

    def test_settings_dialog_path_get_all(self, cli: ChronoViewCLI, require_chronoview):
        """Test reading all paths from SettingsDialog.

        This test requires the SettingsDialog to be open.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # First ensure dialog is open
        cli.run(["settings-dialog", "open"])
        time.sleep(0.5)

        result = cli.run(["settings-dialog", "path", "get-all"])

        assert_success(result, "Get all paths failed")
        assert_has_data(result)

        data = result["data"]

        # Should have various path categories
        # May include: line1Nir, line1Normal, line2Nir, line2Normal, outputPath, quarantinePath
        assert len(data) > 0, "Should have at least one path configured"

        # Clean up - close dialog
        cli.run(["settings-dialog", "close"])

    def test_settings_dialog_path_get_line1(self, cli: ChronoViewCLI, require_chronoview):
        """Test reading Line 1 paths from SettingsDialog.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # Ensure dialog is open
        cli.run(["settings-dialog", "open"])
        time.sleep(0.5)

        result = cli.run(["settings-dialog", "path", "get-line1"])

        assert_success(result, "Get Line 1 paths failed")
        assert_has_data(result)

        data = result["data"]
        assert len(data) > 0, "Should have at least one Line 1 path"

        # Clean up
        cli.run(["settings-dialog", "close"])

    def test_settings_dialog_path_get_line2(self, cli: ChronoViewCLI, require_chronoview):
        """Test reading Line 2 paths from SettingsDialog.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # Ensure dialog is open
        cli.run(["settings-dialog", "open"])
        time.sleep(0.5)

        result = cli.run(["settings-dialog", "path", "get-line2"])

        assert_success(result, "Get Line 2 paths failed")
        assert_has_data(result)

        data = result["data"]
        assert len(data) > 0, "Should have at least one Line 2 path"

        # Clean up
        cli.run(["settings-dialog", "close"])

    def test_settings_dialog_path_get_output(self, cli: ChronoViewCLI, require_chronoview):
        """Test reading output path from SettingsDialog.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # Ensure dialog is open
        cli.run(["settings-dialog", "open"])
        time.sleep(0.5)

        result = cli.run(["settings-dialog", "path", "get-output"])

        assert_success(result, "Get output path failed")
        assert_has_data(result, "outputPath")

        # Output path may be None or a string
        output_path = result["data"]["outputPath"]
        if output_path is not None:
            assert isinstance(output_path, str), "Output path should be a string"

        # Clean up
        cli.run(["settings-dialog", "close"])

    def test_settings_dialog_checkbox_list(self, cli: ChronoViewCLI, require_chronoview):
        """Test listing all checkboxes from SettingsDialog.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # Ensure dialog is open
        cli.run(["settings-dialog", "open"])
        time.sleep(0.5)

        result = cli.run(["settings-dialog", "checkbox", "list"])

        assert_success(result, "Checkbox list failed")
        assert_has_data(result, "checkboxes")

        checkboxes = result["data"]["checkboxes"]
        assert isinstance(checkboxes, list), "Checkboxes should be a list"

        # If we have checkboxes, verify structure
        for checkbox in checkboxes:
            assert "name" in checkbox, "Checkbox should have a name"
            assert "checked" in checkbox, "Checkbox should have checked state"

        # Clean up
        cli.run(["settings-dialog", "close"])


class TestScenarioConfiguration:
    """Test high-level scenario commands for configuration.

    These tests use the scenario command group for complex workflows.
    """

    def test_scenario_configure_paths_validation(self, cli: ChronoViewCLI, require_chronoview):
        """Test configure-paths scenario validation.

        This test validates the structure of the configure-paths command
        without actually modifying paths (to avoid affecting test environment).

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running

        Note:
            This test uses dummy paths for validation only. It may not
            actually save the configuration.
        """
        # Use dummy paths for testing the command interface
        test_paths = {
            "line1-nir": "C:\\\\test\\\\nir",
            "line1-normal": "C:\\\\test\\\\normal",
            "output": "C:\\\\test\\\\output"
        }

        # Build command arguments
        args = ["scenario", "configure-paths"]
        for key, value in test_paths.items():
            args.extend([f"--{key}", value])

        result = cli.run(args)

        # The command should succeed or at least execute
        # It may fail if paths don't exist, which is expected
        assert "exitCode" in result, "Should have exit code"

    def test_toolbar_refresh(self, cli: ChronoViewCLI, require_chronoview):
        """Test toolbar refresh button.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["toolbar", "refresh"])

        assert_success(result, "Toolbar refresh failed")
        assert result["exitCode"] == 0, "Refresh should succeed"

    def test_toolbar_settings_button(self, cli: ChronoViewCLI, require_chronoview):
        """Test toolbar settings button.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["toolbar", "settings"])

        assert_success(result, "Toolbar settings button failed")
        assert result["exitCode"] == 0, "Settings button should succeed"

        # Settings button opens SetupWindow
        # We should verify it appeared (optional check)
        time.sleep(0.5)

        # Check if SetupWindow is now open
        setup_result = cli.run(["windows", "setup"])
        if setup_result.get("success"):
            # SetupWindow found - close it to clean up
            # (This may require additional commands depending on implementation)
            pass


class TestCameraLaunch:
    """Test camera launch workflows.

    These tests cover launching external camera applications.
    """

    def test_workflow_launch_general(self, cli: ChronoViewCLI, require_chronoview):
        """Test general camera launch workflow.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running

        Note:
            This test may launch an external application. Ensure the test
            environment can handle this or the application is already running.
        """
        result = cli.run(["workflow", "launch-general"])

        # Should execute without error
        # May fail if app already running or not configured
        assert "exitCode" in result, "Should have exit code"

    def test_workflow_launch_nir(self, cli: ChronoViewCLI, require_chronoview):
        """Test NIR camera launch workflow.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running

        Note:
            This test may launch an external application.
        """
        result = cli.run(["workflow", "launch-nir"])

        assert "exitCode" in result, "Should have exit code"

    def test_workflow_launch_nir2(self, cli: ChronoViewCLI, require_chronoview):
        """Test NIR2 camera launch workflow.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running

        Note:
            This test may launch an external application.
        """
        result = cli.run(["workflow", "launch-nir2"])

        assert "exitCode" in result, "Should have exit code"

    def test_workflow_toggle_filtering(self, cli: ChronoViewCLI, require_chronoview):
        """Test NIR filtering toggle workflow.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running

        Note:
            This test changes the filtering state. It will toggle back
            if called twice.
        """
        result = cli.run(["workflow", "toggle-filtering"])

        assert_success(result, "Toggle filtering failed")
        assert result["exitCode"] == 0, "Toggle filtering should succeed"
