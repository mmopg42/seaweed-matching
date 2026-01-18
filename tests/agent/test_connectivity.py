"""
ChronoView Connectivity and Smoke Tests

Test suite for verifying basic connectivity to ChronoView application
and UI element accessibility using the test agent CLI wrapper.
"""

import pytest
from ChronoViewTestAgent import ChronoViewCLI, assert_success, assert_has_data


class TestConnectivity:
    """Test ChronoView connectivity and basic UI detection.

    These tests verify that ChronoView is running and accessible.
    Tests in this class do not require data to be present in the application.
    """

    def test_chronoview_is_running(self, cli: ChronoViewCLI):
        """Verify ChronoView main window is accessible.

        This is the most basic connectivity test. It checks if the
        ChronoView MainWindow can be found via UI Automation.

        Args:
            cli: ChronoViewCLI fixture from conftest.py

        Raises:
            pytest.skip.Exception: If ChronoView is not running
        """
        result = cli.run(["test", "connectivity"])

        # Provide clear error message if connectivity fails
        assert_success(result, "ChronoView connectivity check failed")

        # Verify the response structure
        assert_has_data(result, "connected", "windowFound", "appName")

        # Verify ChronoView is actually connected
        assert result["data"]["connected"] is True, "ChronoView is not connected"
        assert result["data"]["windowFound"] is True, "MainWindow not found"
        assert result["data"]["appName"] is not None, "App name should not be None"

    def test_capabilities_available(self, cli: ChronoViewCLI, require_chronoview):
        """Verify all automation capabilities are available.

        This test checks that all UI controllers and command groups
        are properly detected and accessible.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["test", "capabilities"])

        assert_success(result, "Capabilities check failed")
        assert_has_data(result, "windows", "controllers", "commands")

        caps = result["data"]

        # Verify windows list contains at least MainWindow
        assert "windows" in caps
        assert len(caps["windows"]) >= 1, "At least MainWindow should be listed"

        # MainWindow should be accessible
        main_window = next((w for w in caps["windows"] if w["type"] == "MainWindow"), None)
        assert main_window is not None, "MainWindow not in capabilities list"
        assert main_window["accessible"] is True, "MainWindow should be accessible"

        # Verify controllers are available
        expected_controllers = [
            "ChronoToolbarController",
            "ChronoDataPanelReader",
            "ChronoWorkflowController",
            "ChronoFileOperationsController"
        ]
        for controller in expected_controllers:
            assert controller in caps["controllers"], f"{controller} not available"

        # Verify command groups are available
        expected_commands = ["toolbar", "datagrid", "workflow", "logs", "file-ops"]
        for command in expected_commands:
            assert command in caps["commands"], f"{command} command group not available"

    def test_main_window_accessible(self, cli: ChronoViewCLI, require_chronoview):
        """Verify MainWindow can be detected via windows command.

        This test uses the 'windows main' command to verify MainWindow
        is detectable with correct properties.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["windows", "main"])

        assert_success(result, "MainWindow detection failed")
        assert_has_data(result, "found", "windowType", "title")

        assert result["data"]["found"] is True, "MainWindow not found"
        assert result["data"]["windowType"] == "MainWindow", "Incorrect window type"
        assert result["data"]["title"] is not None, "Window title should not be None"

    def test_datagrid_accessible(self, cli: ChronoViewCLI, require_chronoview):
        """Verify FileGroupDataGrid is accessible.

        This test checks that the DataGrid can be accessed and basic
        information retrieved. It does not require data to be present.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["test", "datagrid"])

        assert_success(result, "DataGrid accessibility check failed")
        assert_has_data(result, "accessible")

        assert result["data"]["accessible"] is True, "DataGrid is not accessible"

        # If accessible, should have row count info
        if "rowCount" in result["data"]:
            # Row count should be a non-negative integer
            assert isinstance(result["data"]["rowCount"], int)
            assert result["data"]["rowCount"] >= 0

    def test_datagrid_headers_retrievable(self, cli: ChronoViewCLI, require_chronoview):
        """Verify DataGrid column headers can be retrieved.

        This test ensures that we can read the DataGrid column structure,
        which is needed for data operations.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["datagrid", "headers"])

        assert_success(result, "DataGrid headers retrieval failed")
        assert_has_data(result, "headers")

        headers = result["data"]["headers"]
        assert isinstance(headers, list), "Headers should be a list"
        assert len(headers) > 0, "Should have at least one column"

        # Verify expected columns exist (may vary by ChronoView version)
        # Common columns: checkbox, GroupId, MatchedCount, etc.
        assert any(h for h in headers if "GroupId" in str(h) or "Group" in str(h)), \
            "Should have GroupId column"

    def test_workflow_panel_accessible(self, cli: ChronoViewCLI, require_chronoview):
        """Verify WorkflowPanel is accessible.

        This test checks that the WorkflowPanel containing camera controls
        and path configuration is detectable.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["test", "workflow"])

        assert_success(result, "WorkflowPanel accessibility check failed")
        assert_has_data(result, "accessible")

        assert result["data"]["accessible"] is True, "WorkflowPanel is not accessible"

    def test_toolbar_list_accessible(self, cli: ChronoViewCLI, require_chronoview):
        """Verify toolbar buttons can be listed.

        This test ensures that the toolbar controller can enumerate
        all available buttons for clicking and state checks.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["toolbar", "list"])

        assert_success(result, "Toolbar list retrieval failed")
        assert_has_data(result, "buttons")

        buttons = result["data"]["buttons"]
        assert isinstance(buttons, list), "Buttons should be a list"
        assert len(buttons) > 0, "Should have at least one toolbar button"

        # Verify expected buttons exist (Korean text)
        expected_buttons = ["시작", "중지"]  # Start, Stop
        for expected in expected_buttons:
            assert any(expected in btn for btn in buttons), \
                f"Expected button '{expected}' not found in toolbar"

    def test_all_windows_enumeration(self, cli: ChronoViewCLI):
        """Verify all ChronoView windows can be enumerated.

        This test lists all ChronoView-related windows currently open.
        It should find at least the MainWindow when ChronoView is running.

        Args:
            cli: ChronoViewCLI fixture

        Note:
            This test will pass even if ChronoView is not running,
            returning an empty list or only finding other windows.
        """
        result = cli.run(["windows", "all"])

        # Should succeed even if no windows found
        assert_success(result, "Windows enumeration failed")
        assert_has_data(result, "count", "windows")

        windows = result["data"]["windows"]
        count = result["data"]["count"]

        assert isinstance(windows, list), "Windows should be a list"
        assert isinstance(count, int), "Count should be an integer"
        assert count == len(windows), "Count should match list length"

        # If we have windows, verify structure
        for window in windows:
            assert "title" in window, "Window should have title"
            assert "className" in window, "Window should have className"

    def test_log_panel_accessible(self, cli: ChronoViewCLI, require_chronoview):
        """Verify LogPanel is accessible.

        This test checks that log entries can be retrieved from the
        LogPanel for debugging and verification purposes.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["logs", "get"])

        assert_success(result, "LogPanel retrieval failed")
        assert_has_data(result, "logs")

        logs = result["data"]["logs"]
        assert isinstance(logs, list), "Logs should be a list"

        # If we have logs, verify structure
        if len(logs) > 0:
            # Log entries should have basic structure
            first_log = logs[0]
            assert "timestamp" in first_log or "time" in first_log, \
                "Log entry should have timestamp"
            assert "message" in first_log or "msg" in first_log, \
                "Log entry should have message"


class TestDataGridAccess:
    """Test DataGrid access and basic data operations.

    These tests focus on reading data from the DataGrid without
    modifying anything. They work with whatever data is present.
    """

    def test_datagrid_row_count_retrievable(self, cli: ChronoViewCLI, require_chronoview):
        """Verify DataGrid row count can be retrieved.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["datagrid", "rows"])

        assert_success(result, "DataGrid row count retrieval failed")
        assert_has_data(result, "rowCount")

        row_count = result["data"]["rowCount"]
        assert isinstance(row_count, int), "Row count should be an integer"
        assert row_count >= 0, "Row count should be non-negative"

    def test_datagrid_info_retrievable(self, cli: ChronoViewCLI, require_chronoview):
        """Verify DataGrid info can be retrieved.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["datagrid", "info"])

        assert_success(result, "DataGrid info retrieval failed")
        assert_has_data(result, "rowCount")

        # Verify basic info structure
        info = result["data"]
        assert "rowCount" in info, "Should have row count"
        assert isinstance(info["rowCount"], int), "Row count should be an integer"

        # May have additional info like column count, selected count
        if "columnCount" in info:
            assert isinstance(info["columnCount"], int)
        if "selectedCount" in info:
            assert isinstance(info["selectedCount"], int)

    def test_datagrid_data_retrievable(self, cli: ChronoViewCLI, require_chronoview):
        """Verify DataGrid data can be retrieved.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running

        Note:
            This test works with whatever data is present. It may return
            an empty list if no data is in the DataGrid.
        """
        result = cli.run(["datagrid", "data"])

        assert_success(result, "DataGrid data retrieval failed")
        assert_has_data(result, "data")

        data = result["data"]["data"]
        assert isinstance(data, list), "Data should be a list"

        # If we have rows, verify they have the expected structure
        if len(data) > 0:
            first_row = data[0]
            assert isinstance(first_row, dict), "Each row should be a dictionary"
            # Rows should have at least GroupId
            assert "GroupId" in first_row or "groupId" in first_row, \
                "Row should have GroupId field"

    def test_datagrid_export(self, cli: ChronoViewCLI, require_chronoview):
        """Verify DataGrid can be exported.

        The export command always returns JSON with timestamp and data.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["datagrid", "export"])

        assert_success(result, "DataGrid export failed")
        assert_has_data(result, "timestamp", "rowCount", "data")

        assert result["data"]["timestamp"] is not None, "Should have export timestamp"
        assert isinstance(result["data"]["rowCount"], int), "Row count should be an integer"
        assert isinstance(result["data"]["data"], list), "Data should be a list"
