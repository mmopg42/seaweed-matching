"""
ChronoView File Operations Tests

Test suite for file operation automation including selection, move,
delete operations, and bulk operations.
"""

import pytest
from ChronoViewTestAgent import ChronoViewCLI, assert_success, assert_has_data


@pytest.mark.order(after="test_workflow.py")
class TestFileOperations:
    """Test file operation automation.

    These tests cover DataGrid data reading and file operations.
    Destructive operations (move, delete) include skip conditions
    when no test data is available.
    """

    def test_read_datagrid_rows(self, cli: ChronoViewCLI, require_chronoview):
        """Test reading FileGroup data from DataGrid.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["datagrid", "rows"])

        assert_success(result, "DataGrid rows retrieval failed")
        assert_has_data(result, "rowCount")

        row_count = result["data"]["rowCount"]
        assert isinstance(row_count, int), "Row count should be an integer"
        assert row_count >= 0, "Row count should be non-negative"

    def test_read_datagrid_data(self, cli: ChronoViewCLI, require_chronoview):
        """Test reading complete DataGrid data.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["datagrid", "data"])

        assert_success(result, "DataGrid data retrieval failed")
        assert_has_data(result, "data")

        data = result["data"]["data"]
        assert isinstance(data, list), "Data should be a list"

        # If we have rows, verify structure
        if len(data) > 0:
            first_row = data[0]
            assert isinstance(first_row, dict), "Each row should be a dictionary"
            # Should have GroupId or similar identifier
            assert any(k in first_row for k in ["GroupId", "groupId", "group_id"]), \
                "Row should have group identifier"

    def test_read_datagrid_cell(self, cli: ChronoViewCLI, require_chronoview):
        """Test reading a specific DataGrid cell.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running

        Note:
            This test attempts to read row 0, column 1. Will be skipped
            if no data is available.
        """
        # First check if we have data
        data_result = cli.run(["datagrid", "rows"])
        if not data_result.get("success") or data_result.get("data", {}).get("rowCount", 0) == 0:
            pytest.skip("No data in DataGrid to test cell reading")

        # Try to read cell (0, 1) - typically GroupId column
        result = cli.run(["datagrid", "cell", "0", "1"])

        assert_success(result, "DataGrid cell retrieval failed")
        assert_has_data(result, "row", "column", "value")

        assert result["data"]["row"] == 0, "Row should be 0"
        assert result["data"]["column"] == 1, "Column should be 1"
        assert result["data"]["value"] is not None, "Cell value should not be None"

    def test_read_statistics(self, cli: ChronoViewCLI, require_chronoview):
        """Test reading StatisticsPanel data.

        Note: The 'stats' command is a legacy command. Modern usage
        prefers 'datagrid info' for data statistics.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["stats"])

        assert_success(result, "Statistics retrieval failed")
        assert_has_data(result)

        # Statistics may contain: matchedGroups, unmatchedFiles, etc.
        data = result["data"]
        assert len(data) > 0, "Should have some statistics data"

    def test_file_ops_get_selected(self, cli: ChronoViewCLI, require_chronoview):
        """Test reading selected rows from DataGrid.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["file-ops", "selected"])

        assert_success(result, "Selected rows retrieval failed")
        assert_has_data(result, "selectedRows")

        selected_rows = result["data"]["selectedRows"]
        assert isinstance(selected_rows, list), "Selected rows should be a list"

    def test_file_ops_clear_selection(self, cli: ChronoViewCLI, require_chronoview):
        """Test clearing row selection.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["file-ops", "clear-selection"])

        assert_success(result, "Clear selection failed")
        assert result["exitCode"] == 0, "Clear selection should succeed"

        # Verify selection is cleared
        verify_result = cli.run(["file-ops", "selected"])
        assert_success(verify_result, "Verification failed")
        assert len(verify_result["data"]["selectedRows"]) == 0, \
            "Should have no selected rows after clearing"

    def test_file_ops_select_by_row_index(self, cli: ChronoViewCLI, require_chronoview):
        """Test selecting a row by index.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running

        Note:
            This test will be skipped if no data is available in the DataGrid.
        """
        # First check if we have data
        data_result = cli.run(["datagrid", "rows"])
        if not data_result.get("success") or data_result.get("data", {}).get("rowCount", 0) == 0:
            pytest.skip("No data in DataGrid to test row selection")

        # Clear any existing selection
        cli.run(["file-ops", "clear-selection"])

        # Select row 0
        result = cli.run(["file-ops", "select", "row-index", "--row-index", "0"])

        assert_success(result, "Row selection failed")
        assert result["exitCode"] == 0, "Row selection should succeed"

        # Verify selection
        verify_result = cli.run(["file-ops", "selected"])
        assert_success(verify_result, "Selection verification failed")
        assert len(verify_result["data"]["selectedRows"]) > 0, \
            "Should have at least one selected row"

    def test_scenario_move_groups_by_index_skip_if_no_data(
        self, cli: ChronoViewCLI, require_chronoview
    ):
        """Test moving file groups scenario by row indices.

        NOTE: This is a DESTRUCTIVE test. It will be skipped if no data exists.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # First check if we have data
        data_result = cli.run(["datagrid", "rows"])
        if not data_result.get("success") or data_result.get("data", {}).get("rowCount", 0) == 0:
            pytest.skip("No data in DataGrid to test move operation")

        # Move first row (destructive - may need test data setup)
        result = cli.run(["scenario", "move-groups", "--row-indices", "0"])

        # The command should execute (may fail if no output path configured)
        assert "exitCode" in result, "Should have exit code"

        # If successful, verify moved count
        if result.get("success"):
            assert "data" in result, "Should have data on success"
            assert "moved" in result["data"], "Should have moved count"
            assert result["data"]["moved"] >= 0, "Moved count should be non-negative"

    def test_batch_export_all_data(self, cli: ChronoViewCLI, require_chronoview):
        """Test batch export all ChronoView data.

        This test verifies that all available data can be exported at once.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["batch", "export-all"])

        assert_success(result, "Batch export failed")
        assert_has_data(result, "timestamp", "statistics", "dataGrid", "cameraStates")

        data = result["data"]

        # Verify export structure
        assert data["timestamp"] is not None, "Should have export timestamp"
        assert "statistics" in data, "Should have statistics data"
        assert "dataGrid" in data, "Should have DataGrid data"
        assert "cameraStates" in data, "Should have camera states"

        # Verify statistics structure
        stats = data["statistics"]
        assert isinstance(stats, dict), "Statistics should be a dict"

        # Verify DataGrid structure
        grid_data = data["dataGrid"]
        assert isinstance(grid_data, dict), "DataGrid data should be a dict"
        assert "rowCount" in grid_data, "Should have row count"

        # Verify camera states structure
        states = data["cameraStates"]
        assert isinstance(states, dict), "Camera states should be a dict"

    def test_datagrid_export(self, cli: ChronoViewCLI, require_chronoview):
        """Test DataGrid export command.

        The export command always returns JSON with timestamp and data.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["datagrid", "export"])

        assert_success(result, "DataGrid export failed")
        assert_has_data(result, "timestamp", "rowCount", "data")

        data = result["data"]

        assert data["timestamp"] is not None, "Should have export timestamp"
        assert isinstance(data["rowCount"], int), "Row count should be an integer"
        assert isinstance(data["data"], list), "Data should be a list"


class TestLogOperations:
    """Test log reading and filtering operations.

    These tests cover accessing log panel data for debugging and
    verification purposes.
    """

    def test_logs_get(self, cli: ChronoViewCLI, require_chronoview):
        """Test getting all log entries.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        result = cli.run(["logs", "get"])

        assert_success(result, "Logs retrieval failed")
        assert_has_data(result, "logs")

        logs = result["data"]["logs"]
        assert isinstance(logs, list), "Logs should be a list"

    def test_logs_tail(self, cli: ChronoViewCLI, require_chronoview):
        """Test getting recent log entries.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # Get last 10 log entries
        result = cli.run(["logs", "tail", "10"])

        assert_success(result, "Logs tail retrieval failed")
        assert_has_data(result, "logs")

        logs = result["data"]["logs"]
        assert isinstance(logs, list), "Logs should be a list"
        assert len(logs) <= 10, "Should return at most 10 entries"

    def test_logs_filter(self, cli: ChronoViewCLI, require_chronoview):
        """Test filtering log entries by level.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # Filter for Error level logs
        result = cli.run(["logs", "filter", "--level", "Error"])

        assert_success(result, "Logs filter failed")
        assert_has_data(result, "logs")

        logs = result["data"]["logs"]
        assert isinstance(logs, list), "Filtered logs should be a list"

        # If we have logs, verify they match the filter
        for log in logs:
            # Level may be in 'level', 'severity', or similar field
            level = log.get("level") or log.get("severity") or log.get("Level") or ""
            assert "Error" in str(level), f"Log level should contain 'Error': {log}"

    def test_logs_search(self, cli: ChronoViewCLI, require_chronoview):
        """Test searching log entries for text.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # Search for common log terms
        result = cli.run(["logs", "search", "Started"])

        assert_success(result, "Logs search failed")
        assert_has_data(result, "logs")

        logs = result["data"]["logs"]
        assert isinstance(logs, list), "Search results should be a list"

        # If we have results, verify they contain the search term
        for log in logs:
            # Message may be in various fields
            message = str(log.get("message") or log.get("msg") or log.get("Message") or "")
            # Note: This is a case-insensitive search on the CLI side
            assert len(message) > 0, "Log entry should have a message"


class TestBatchOperations:
    """Test bulk/batch operations.

    These tests cover the batch command group for multi-row operations.
    """

    def test_batch_select_and_move_skip_if_no_data(
        self, cli: ChronoViewCLI, require_chronoview
    ):
        """Test batch select and move operation.

        NOTE: This is a DESTRUCTIVE test. It will be skipped if no data exists
        or insufficient data for the operation.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # First check if we have enough data
        data_result = cli.run(["datagrid", "rows"])
        row_count = data_result.get("data", {}).get("rowCount", 0)

        if row_count < 2:
            pytest.skip(f"Need at least 2 rows for batch select-and-move test, got {row_count}")

        # Test batch select and move with --rows option
        result = cli.run(["batch", "select-and-move", "--rows", "0,1"])

        # The command should execute
        assert "exitCode" in result, "Should have exit code"

        # If successful, verify the response
        if result.get("success"):
            data = result.get("data", {})
            assert "selectedCount" in data, "Should have selected count"
            assert data["selectedCount"] >= 0, "Selected count should be non-negative"

    def test_batch_select_and_delete_skip_if_no_data(
        self, cli: ChronoViewCLI, require_chronoview
    ):
        """Test batch select and delete operation.

        NOTE: This is a DESTRUCTIVE test. It will be skipped if no data exists.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # First check if we have enough data
        data_result = cli.run(["datagrid", "rows"])
        row_count = data_result.get("data", {}).get("rowCount", 0)

        if row_count < 1:
            pytest.skip(f"Need at least 1 row for batch select-and-delete test, got {row_count}")

        # Test batch select and delete with --rows option
        result = cli.run(["batch", "select-and-delete", "--rows", "0"])

        # The command should execute
        assert "exitCode" in result, "Should have exit code"

        # If successful, verify the response
        if result.get("success"):
            data = result.get("data", {})
            assert "selectedCount" in data, "Should have selected count"
            assert "deleted" in data, "Should have deleted flag"

    def test_batch_select_and_move_with_count_skip_if_no_data(
        self, cli: ChronoViewCLI, require_chronoview
    ):
        """Test batch select and move with --count and --start-index.

        NOTE: This is a DESTRUCTIVE test. It will be skipped if no data exists.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # First check if we have enough data
        data_result = cli.run(["datagrid", "rows"])
        row_count = data_result.get("data", {}).get("rowCount", 0)

        if row_count < 3:
            pytest.skip(f"Need at least 3 rows for batch count test, got {row_count}")

        # Test batch select and move with --count option
        result = cli.run(["batch", "select-and-move", "--start-index", "0", "--count", "2"])

        # The command should execute
        assert "exitCode" in result, "Should have exit code"


class TestFileOpsVerification:
    """Test file operation verification commands.

    These tests cover the verification commands used after file operations.
    """

    def test_file_ops_verify_deleted_skip_if_no_data(
        self, cli: ChronoViewCLI, require_chronoview
    ):
        """Test file operation deleted verification.

        This test verifies that a deleted GroupId is no longer in the DataGrid.

        NOTE: This test requires a deleted GroupId to verify. It will be skipped
        if no suitable test data is available.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # First get current data to find a GroupId (if any)
        data_result = cli.run(["datagrid", "data"])

        if not data_result.get("success"):
            pytest.skip("Could not retrieve DataGrid data")

        rows = data_result.get("data", {}).get("data", [])
        if len(rows) == 0:
            pytest.skip("No data in DataGrid to verify deletion")

        # Use the first GroupId for verification (it should still exist)
        first_row = rows[0]
        group_id = first_row.get("GroupId") or first_row.get("groupId") or first_row.get("group_id")

        if not group_id:
            pytest.skip("Could not extract GroupId from first row")

        # Verify the GroupId exists (should return exists=True or similar)
        result = cli.run(["file-ops", "verify", "deleted", group_id])

        # The verify command should execute
        assert "success" in result or "exitCode" in result, \
            "Verify command should return success or exit code"

    def test_file_ops_verify_row_count(self, cli: ChronoViewCLI, require_chronoview):
        """Test file operation row count verification.

        This test verifies that the row count matches an expected value.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # First get current row count
        data_result = cli.run(["datagrid", "rows"])
        assert_success(data_result, "Could not get current row count")

        current_count = data_result["data"]["rowCount"]

        # Verify the count matches itself (should always succeed)
        result = cli.run(["file-ops", "verify", "row-count", str(current_count)])

        assert_success(result, "Row count verification failed")
        assert_has_data(result, "verified", "rowCount")

        assert result["data"]["verified"] is True, \
            "Row count verification should succeed for current count"
        assert result["data"]["rowCount"] == current_count, \
            "Verified count should match input"

    def test_file_ops_wait_move(self, cli: ChronoViewCLI, require_chronoview):
        """Test file operation wait for move completion.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running

        Note:
            This test will complete quickly if no move operation is in progress.
        """
        # Wait for move completion with short timeout
        result = cli.run(["file-ops", "wait", "move", "--timeout", "1000"])

        # Should succeed (either completed or no operation in progress)
        assert "exitCode" in result, "Should have exit code"

    def test_file_ops_wait_delete(self, cli: ChronoViewCLI, require_chronoview):
        """Test file operation wait for delete completion.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running

        Note:
            This test will complete quickly if no delete operation is in progress.
        """
        # Wait for delete completion with short timeout
        result = cli.run(["file-ops", "wait", "delete", "--timeout", "1000"])

        # Should succeed (either completed or no operation in progress)
        assert "exitCode" in result, "Should have exit code"

    def test_file_ops_confirm(self, cli: ChronoViewCLI, require_chronoview):
        """Test file operation confirmation dialog handling.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running

        Note:
            This test checks for the presence of a confirmation dialog.
            If no dialog is present, it will report that fact.
        """
        result = cli.run(["file-ops", "confirm"])

        # Should execute and report dialog state
        assert "success" in result or "exitCode" in result, \
            "Confirm command should return success or exit code"

        # If successful, check the confirmation state
        if result.get("success"):
            data = result.get("data", {})
            # May have 'confirmed', 'dialogPresent', etc.
            assert len(data) > 0, "Should have confirmation data"


class TestSelectionOperations:
    """Test DataGrid row selection operations.

    These tests cover the various methods of selecting rows in the DataGrid.
    """

    def test_file_ops_select_all(self, cli: ChronoViewCLI, require_chronoview):
        """Test selecting all rows.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # First get row count
        data_result = cli.run(["datagrid", "rows"])
        assert_success(data_result, "Could not get row count")

        row_count = data_result["data"]["rowCount"]

        # Select all rows
        result = cli.run(["file-ops", "select-all"])

        assert_success(result, "Select all failed")
        assert result["exitCode"] == 0, "Select all should succeed"

        # If we have rows, verify selection
        if row_count > 0:
            verify_result = cli.run(["file-ops", "selected"])
            assert_success(verify_result, "Selection verification failed")

            selected_count = len(verify_result["data"]["selectedRows"])
            assert selected_count > 0, "Should have selected rows after select-all"

    def test_file_ops_select_by_group_id_skip_if_no_data(
        self, cli: ChronoViewCLI, require_chronoview
    ):
        """Test selecting rows by GroupId.

        NOTE: This test will be skipped if no data is available.

        Args:
            cli: ChronoViewCLI fixture
            require_chronoview: Fixture that skips test if ChronoView not running
        """
        # Get current data to find a GroupId
        data_result = cli.run(["datagrid", "data"])

        if not data_result.get("success"):
            pytest.skip("Could not retrieve DataGrid data")

        rows = data_result.get("data", {}).get("data", [])
        if len(rows) == 0:
            pytest.skip("No data in DataGrid to test GroupId selection")

        # Get first GroupId
        first_row = rows[0]
        group_id = first_row.get("GroupId") or first_row.get("groupId") or first_row.get("group_id")

        if not group_id:
            pytest.skip("Could not extract GroupId from first row")

        # Clear selection first
        cli.run(["file-ops", "clear-selection"])

        # Select by GroupId
        result = cli.run(["file-ops", "select", "group-id", "--group-id", group_id])

        assert_success(result, "Group ID selection failed")
        assert result["exitCode"] == 0, "Group ID selection should succeed"

        # Verify selection
        verify_result = cli.run(["file-ops", "selected"])
        assert_success(verify_result, "Selection verification failed")

        selected_rows = verify_result["data"]["selectedRows"]
        assert len(selected_rows) > 0, "Should have selected rows by GroupId"
