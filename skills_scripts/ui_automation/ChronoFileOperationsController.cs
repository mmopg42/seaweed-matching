using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;

namespace SkillsScripts.UiAutomation
{
    /// <summary>
    /// High-level API for ChronoView file operation automation.
    /// Provides FileGroup selection and move/delete operation automation through UI Automation.
    /// </summary>
    /// <remarks>
    /// This class provides a complete API for file operation interactions including:
    /// - Finding the MainDataGrid for FileGroup display
    /// - Selecting/deselecting rows by index or GroupId
    /// - Selecting all rows via SelectAll checkbox
    /// - Getting selected row indices
    /// - Clicking Move/Delete buttons
    /// - Waiting for operation completion
    ///
    /// DataGrid structure:
    /// - First column: CheckBox for row selection
    /// - Second column: GroupId (Index)
    /// - Remaining columns: Status, camera-specific data
    ///
    /// Toolbar buttons have Korean text labels:
    /// - "이동" (Move)
    /// - "삭제" (Delete)
    /// </remarks>
    public class ChronoFileOperationsController : IDisposable
    {
        private readonly UIA3Automation _automation;
        private readonly ChronoWindowFinder _windowFinder;
        private readonly ChronoToolbarController _toolbarController;
        private const int DefaultPollIntervalMs = 200;

        /// <summary>
        /// Initializes a new instance of the ChronoFileOperationsController class.
        /// </summary>
        /// <param name="automation">The UIA3Automation instance to use for UI automation</param>
        public ChronoFileOperationsController(UIA3Automation automation)
        {
            _automation = automation ?? throw new ArgumentNullException(nameof(automation));
            _windowFinder = new ChronoWindowFinder(automation);
            _toolbarController = new ChronoToolbarController(automation);
        }

        /// <summary>
        /// Initializes a new instance of the ChronoFileOperationsController class,
        /// creating its own UIA3Automation instance.
        /// </summary>
        public ChronoFileOperationsController() : this(new UIA3Automation())
        {
        }

        #region DataGrid Finding

        /// <summary>
        /// Finds the ChronoView MainWindow.
        /// </summary>
        /// <returns>The MainWindow if found, null otherwise</returns>
        public Window? FindMainWindow()
        {
            return _windowFinder.FindMainWindow();
        }

        /// <summary>
        /// Finds the MainDataGrid within the ChronoView MainWindow.
        /// </summary>
        /// <remarks>
        /// The MainDataGrid displays FileGroups with columns:
        /// - CheckBox (column 0)
        /// - GroupId (column 1)
        /// - Status (column 2)
        /// - Camera-specific columns (3+)
        /// </remarks>
        /// <returns>The DataGrid AutomationElement if found, null otherwise</returns>
        public AutomationElement? FindDataGrid()
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot find DataGrid: MainWindow not found");
                return null;
            }

            return FindDataGrid(mainWindow);
        }

        /// <summary>
        /// Finds the DataGrid within a specific window.
        /// </summary>
        /// <param name="window">The window to search within</param>
        /// <returns>The DataGrid AutomationElement if found, null otherwise</returns>
        public AutomationElement? FindDataGrid(Window window)
        {
            if (window == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot find DataGrid: window is null");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // First try to find by Name "MainDataGrid"
                var nameCondition = cf.ByControlType(ControlType.DataGrid)
                    .And(cf.ByName("MainDataGrid", PropertyConditionFlags.IgnoreCase));
                var dataGrid = window.FindFirstDescendant(nameCondition);

                if (dataGrid != null)
                {
                    Console.WriteLine("[ChronoFileOperationsController] Found DataGrid by Name: 'MainDataGrid'");
                    return dataGrid;
                }

                // Fallback: find any DataGrid
                var gridCondition = cf.ByControlType(ControlType.DataGrid);
                dataGrid = window.FindFirstDescendant(gridCondition);

                if (dataGrid != null)
                {
                    Console.WriteLine($"[ChronoFileOperationsController] Found DataGrid (Name: '{dataGrid.Name ?? "(unnamed)"}')");
                    return dataGrid;
                }

                Console.WriteLine("[ChronoFileOperationsController] DataGrid not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoFileOperationsController] Error finding DataGrid: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Row Selection

        /// <summary>
        /// Gets all DataItem (row) elements from the DataGrid.
        /// </summary>
        /// <param name="dataGrid">The DataGrid element</param>
        /// <returns>Array of DataItem elements representing rows</returns>
        private AutomationElement[] GetDataRows(AutomationElement dataGrid)
        {
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot get rows: dataGrid is null");
                return Array.Empty<AutomationElement>();
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var rows = dataGrid.FindAllChildren(cf.ByControlType(ControlType.DataItem));
                return rows;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoFileOperationsController] Error getting data rows: {ex.Message}");
                return Array.Empty<AutomationElement>();
            }
        }

        /// <summary>
        /// Finds the CheckBox element within a DataGrid row.
        /// </summary>
        /// <param name="rowElement">The DataItem element representing the row</param>
        /// <returns>The CheckBox element if found, null otherwise</returns>
        private AutomationElement? FindRowCheckBox(AutomationElement rowElement)
        {
            if (rowElement == null)
            {
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // Try to find CheckBox directly
                var checkBoxCondition = cf.ByControlType(ControlType.CheckBox);
                var checkBox = rowElement.FindFirstDescendant(checkBoxCondition);

                if (checkBox != null)
                {
                    return checkBox;
                }

                // Fallback: WPF CheckBox may appear as Button with TogglePattern
                var buttonCondition = cf.ByControlType(ControlType.Button);
                var buttons = rowElement.FindAllChildren(buttonCondition);

                foreach (var button in buttons)
                {
                    // Check if button has TogglePattern (characteristic of CheckBox)
                    if (button.Patterns.Toggle.Pattern != null)
                    {
                        return button;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoFileOperationsController] Error finding row CheckBox: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Selects a single row by its index.
        /// </summary>
        /// <param name="dataGrid">The DataGrid element</param>
        /// <param name="rowIndex">Zero-based row index</param>
        /// <returns>True if the row was selected successfully, false otherwise</returns>
        public bool SelectRowByIndex(AutomationElement dataGrid, int rowIndex)
        {
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot select row: dataGrid is null");
                return false;
            }

            var rows = GetDataRows(dataGrid);
            if (rowIndex < 0 || rowIndex >= rows.Length)
            {
                Console.WriteLine($"[ChronoFileOperationsController] Row index {rowIndex} out of range (0-{rows.Length - 1})");
                return false;
            }

            var rowElement = rows[rowIndex];
            var checkBox = FindRowCheckBox(rowElement);

            if (checkBox == null)
            {
                Console.WriteLine($"[ChronoFileOperationsController] CheckBox not found for row {rowIndex}");
                return false;
            }

            return SetCheckBoxState(checkBox, true);
        }

        /// <summary>
        /// Selects a row by index (convenience method using the found DataGrid).
        /// </summary>
        /// <param name="rowIndex">Zero-based row index</param>
        /// <returns>True if the row was selected successfully, false otherwise</returns>
        public bool SelectRowByIndex(int rowIndex)
        {
            var dataGrid = FindDataGrid();
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot select row: DataGrid not found");
                return false;
            }

            return SelectRowByIndex(dataGrid, rowIndex);
        }

        /// <summary>
        /// Finds a row by its GroupId column value and selects it.
        /// </summary>
        /// <param name="dataGrid">The DataGrid element</param>
        /// <param name="groupId">The GroupId to search for</param>
        /// <returns>True if the row was found and selected, false otherwise</returns>
        public bool SelectRowByGroupId(AutomationElement dataGrid, string groupId)
        {
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot select row by GroupId: dataGrid is null");
                return false;
            }

            if (string.IsNullOrWhiteSpace(groupId))
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot select row by GroupId: groupId is null or empty");
                return false;
            }

            var rows = GetDataRows(dataGrid);
            Console.WriteLine($"[ChronoFileOperationsController] Searching for GroupId '{groupId}' among {rows.Length} rows");

            try
            {
                var cf = _automation.ConditionFactory;

                for (int i = 0; i < rows.Length; i++)
                {
                    var row = rows[i];

                    // Get all Text children (cells)
                    var cells = row.FindAllChildren(cf.ByControlType(ControlType.Text));

                    // GroupId is in column 1 (index 1, after checkbox column 0)
                    if (cells.Length > 1)
                    {
                        var cellValue = cells[1].Name ?? "";
                        if (string.Equals(cellValue, groupId, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"[ChronoFileOperationsController] Found GroupId '{groupId}' at row {i}");
                            return SelectRowByIndex(dataGrid, i);
                        }
                    }
                }

                Console.WriteLine($"[ChronoFileOperationsController] GroupId '{groupId}' not found");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoFileOperationsController] Error selecting row by GroupId: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Selects a row by GroupId (convenience method using the found DataGrid).
        /// </summary>
        /// <param name="groupId">The GroupId to search for</param>
        /// <returns>True if the row was found and selected, false otherwise</returns>
        public bool SelectRowByGroupId(string groupId)
        {
            var dataGrid = FindDataGrid();
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot select row by GroupId: DataGrid not found");
                return false;
            }

            return SelectRowByGroupId(dataGrid, groupId);
        }

        /// <summary>
        /// Selects all rows by clicking the SelectAll checkbox in the DataGrid header.
        /// </summary>
        /// <param name="dataGrid">The DataGrid element</param>
        /// <returns>True if all rows were selected successfully, false otherwise</returns>
        public bool SelectAllRows(AutomationElement dataGrid)
        {
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot select all rows: dataGrid is null");
                return false;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // Find the Header element
                var headerCondition = cf.ByControlType(ControlType.Header);
                var header = dataGrid.FindFirstDescendant(headerCondition);

                if (header == null)
                {
                    Console.WriteLine("[ChronoFileOperationsController] Header not found in DataGrid");
                    return false;
                }

                // Find the SelectAll CheckBox in the header
                var checkBoxCondition = cf.ByControlType(ControlType.CheckBox);
                var selectAllCheckBox = header.FindFirstDescendant(checkBoxCondition);

                if (selectAllCheckBox == null)
                {
                    // Fallback: try finding as Button with TogglePattern
                    var buttonCondition = cf.ByControlType(ControlType.Button);
                    var buttons = header.FindAllChildren(buttonCondition);

                    foreach (var button in buttons)
                    {
                        if (button.Patterns.Toggle.Pattern != null)
                        {
                            selectAllCheckBox = button;
                            break;
                        }
                    }
                }

                if (selectAllCheckBox == null)
                {
                    Console.WriteLine("[ChronoFileOperationsController] SelectAll CheckBox not found in header");
                    return false;
                }

                Console.WriteLine("[ChronoFileOperationsController] Clicking SelectAll CheckBox");
                return SetCheckBoxState(selectAllCheckBox, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoFileOperationsController] Error selecting all rows: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Selects all rows (convenience method using the found DataGrid).
        /// </summary>
        /// <returns>True if all rows were selected successfully, false otherwise</returns>
        public bool SelectAllRows()
        {
            var dataGrid = FindDataGrid();
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot select all rows: DataGrid not found");
                return false;
            }

            return SelectAllRows(dataGrid);
        }

        /// <summary>
        /// Clears selection by clicking the SelectAll checkbox again (toggle behavior).
        /// </summary>
        /// <param name="dataGrid">The DataGrid element</param>
        /// <returns>True if selection was cleared successfully, false otherwise</returns>
        public bool ClearSelection(AutomationElement dataGrid)
        {
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot clear selection: dataGrid is null");
                return false;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // Find the Header element
                var headerCondition = cf.ByControlType(ControlType.Header);
                var header = dataGrid.FindFirstDescendant(headerCondition);

                if (header == null)
                {
                    Console.WriteLine("[ChronoFileOperationsController] Header not found in DataGrid");
                    return false;
                }

                // Find the SelectAll CheckBox in the header
                var checkBoxCondition = cf.ByControlType(ControlType.CheckBox);
                var selectAllCheckBox = header.FindFirstDescendant(checkBoxCondition);

                if (selectAllCheckBox == null)
                {
                    // Fallback: try finding as Button with TogglePattern
                    var buttonCondition = cf.ByControlType(ControlType.Button);
                    var buttons = header.FindAllChildren(buttonCondition);

                    foreach (var button in buttons)
                    {
                        if (button.Patterns.Toggle.Pattern != null)
                        {
                            selectAllCheckBox = button;
                            break;
                        }
                    }
                }

                if (selectAllCheckBox == null)
                {
                    Console.WriteLine("[ChronoFileOperationsController] SelectAll CheckBox not found in header");
                    return false;
                }

                // Check current state and toggle if needed
                var currentState = GetCheckBoxState(selectAllCheckBox);
                if (currentState == true)
                {
                    Console.WriteLine("[ChronoFileOperationsController] Toggling SelectAll CheckBox to clear selection");
                    return SetCheckBoxState(selectAllCheckBox, false);
                }

                Console.WriteLine("[ChronoFileOperationsController] Selection already cleared");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoFileOperationsController] Error clearing selection: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clears selection (convenience method using the found DataGrid).
        /// </summary>
        /// <returns>True if selection was cleared successfully, false otherwise</returns>
        public bool ClearSelection()
        {
            var dataGrid = FindDataGrid();
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot clear selection: DataGrid not found");
                return false;
            }

            return ClearSelection(dataGrid);
        }

        /// <summary>
        /// Gets the indices of currently selected rows.
        /// </summary>
        /// <param name="dataGrid">The DataGrid element</param>
        /// <returns>List of selected row indices</returns>
        public List<int> GetSelectedRows(AutomationElement dataGrid)
        {
            var selectedIndices = new List<int>();

            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot get selected rows: dataGrid is null");
                return selectedIndices;
            }

            var rows = GetDataRows(dataGrid);
            Console.WriteLine($"[ChronoFileOperationsController] Checking selection state of {rows.Length} rows");

            try
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    var checkBox = FindRowCheckBox(rows[i]);
                    if (checkBox != null)
                    {
                        var isChecked = GetCheckBoxState(checkBox);
                        if (isChecked == true)
                        {
                            selectedIndices.Add(i);
                        }
                    }
                }

                Console.WriteLine($"[ChronoFileOperationsController] Found {selectedIndices.Count} selected row(s)");
                return selectedIndices;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoFileOperationsController] Error getting selected rows: {ex.Message}");
                return selectedIndices;
            }
        }

        /// <summary>
        /// Gets the indices of currently selected rows (convenience method).
        /// </summary>
        /// <returns>List of selected row indices</returns>
        public List<int> GetSelectedRows()
        {
            var dataGrid = FindDataGrid();
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot get selected rows: DataGrid not found");
                return new List<int>();
            }

            return GetSelectedRows(dataGrid);
        }

        #endregion

        #region CheckBox Helpers

        /// <summary>
        /// Gets the current state of a CheckBox element.
        /// </summary>
        /// <param name="checkBox">The CheckBox element</param>
        /// <returns>True if checked, false if unchecked, null if indeterminate or error</returns>
        private bool? GetCheckBoxState(AutomationElement checkBox)
        {
            if (checkBox == null)
            {
                return null;
            }

            try
            {
                var togglePattern = checkBox.Patterns.Toggle.Pattern;
                if (togglePattern != null)
                {
                    var state = togglePattern.ToggleState.Value;
                    return state == FlaUI.Core.Definitions.ToggleState.On;
                }

                // Fallback: try SelectionItemPattern
                var selectionPattern = checkBox.Patterns.SelectionItem.Pattern;
                if (selectionPattern != null)
                {
                    return selectionPattern.IsSelected.Value;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoFileOperationsController] Error getting CheckBox state: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets the state of a CheckBox element.
        /// </summary>
        /// <param name="checkBox">The CheckBox element</param>
        /// <param name="check">True to check, false to uncheck</param>
        /// <returns>True if successful, false otherwise</returns>
        private bool SetCheckBoxState(AutomationElement checkBox, bool check)
        {
            if (checkBox == null)
            {
                return false;
            }

            try
            {
                var togglePattern = checkBox.Patterns.Toggle.Pattern;
                if (togglePattern != null)
                {
                    var currentState = togglePattern.ToggleState.Value;
                    var targetState = check ? FlaUI.Core.Definitions.ToggleState.On : FlaUI.Core.Definitions.ToggleState.Off;

                    if (currentState != targetState)
                    {
                        togglePattern.Toggle();
                    }

                    return true;
                }

                // Fallback: try InvokePattern (click)
                var invokePattern = checkBox.Patterns.Invoke.Pattern;
                if (invokePattern != null)
                {
                    var currentState = GetCheckBoxState(checkBox);
                    if (currentState != check)
                    {
                        invokePattern.Invoke();
                    }
                    return true;
                }

                Console.WriteLine("[ChronoFileOperationsController] CheckBox has no supported pattern");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoFileOperationsController] Error setting CheckBox state: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Move Operations

        /// <summary>
        /// Clicks the Move button in the toolbar.
        /// </summary>
        /// <returns>True if the Move button was clicked successfully, false otherwise</returns>
        public bool ClickMoveButton()
        {
            return _toolbarController.ClickMoveButton();
        }

        /// <summary>
        /// Clicks the Delete button in the toolbar.
        /// </summary>
        /// <returns>True if the Delete button was clicked successfully, false otherwise</returns>
        public bool ClickDeleteButton()
        {
            return _toolbarController.ClickDeleteButton();
        }

        /// <summary>
        /// Selects multiple rows by indices and clicks the Move button.
        /// </summary>
        /// <param name="rowIndices">Array of row indices to select</param>
        /// <returns>True if rows were selected and Move button clicked, false otherwise</returns>
        public bool SelectAndMoveRows(int[] rowIndices)
        {
            if (rowIndices == null || rowIndices.Length == 0)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot move: no row indices provided");
                return false;
            }

            var dataGrid = FindDataGrid();
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot move: DataGrid not found");
                return false;
            }

            Console.WriteLine($"[ChronoFileOperationsController] Selecting {rowIndices.Length} row(s) for move");

            // Clear existing selection first
            ClearSelection(dataGrid);

            // Select each row
            var successCount = 0;
            foreach (var index in rowIndices)
            {
                if (SelectRowByIndex(dataGrid, index))
                {
                    successCount++;
                }
            }

            if (successCount == 0)
            {
                Console.WriteLine("[ChronoFileOperationsController] Failed to select any rows");
                return false;
            }

            Console.WriteLine($"[ChronoFileOperationsController] Selected {successCount}/{rowIndices.Length} row(s)");

            // Small delay to ensure UI updates
            Thread.Sleep(100);

            // Click Move button
            return ClickMoveButton();
        }

        /// <summary>
        /// Selects rows by GroupId and clicks the Move button.
        /// </summary>
        /// <param name="groupIds">Array of GroupId values to select</param>
        /// <returns>True if rows were selected and Move button clicked, false otherwise</returns>
        public bool SelectAndMoveByGroupIds(string[] groupIds)
        {
            if (groupIds == null || groupIds.Length == 0)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot move: no GroupIds provided");
                return false;
            }

            var dataGrid = FindDataGrid();
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot move: DataGrid not found");
                return false;
            }

            Console.WriteLine($"[ChronoFileOperationsController] Selecting {groupIds.Length} row(s) by GroupId for move");

            // Clear existing selection first
            ClearSelection(dataGrid);

            // Select each row by GroupId
            var successCount = 0;
            foreach (var groupId in groupIds)
            {
                if (SelectRowByGroupId(dataGrid, groupId))
                {
                    successCount++;
                }
            }

            if (successCount == 0)
            {
                Console.WriteLine("[ChronoFileOperationsController] Failed to select any rows by GroupId");
                return false;
            }

            Console.WriteLine($"[ChronoFileOperationsController] Selected {successCount}/{groupIds.Length} row(s) by GroupId");

            // Small delay to ensure UI updates
            Thread.Sleep(100);

            // Click Move button
            return ClickMoveButton();
        }

        /// <summary>
        /// Selects multiple rows by indices and clicks the Delete button.
        /// </summary>
        /// <param name="rowIndices">Array of row indices to select</param>
        /// <returns>True if rows were selected and Delete button clicked, false otherwise</returns>
        public bool SelectAndDeleteRows(int[] rowIndices)
        {
            if (rowIndices == null || rowIndices.Length == 0)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot delete: no row indices provided");
                return false;
            }

            var dataGrid = FindDataGrid();
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot delete: DataGrid not found");
                return false;
            }

            Console.WriteLine($"[ChronoFileOperationsController] Selecting {rowIndices.Length} row(s) for delete");

            // Clear existing selection first
            ClearSelection(dataGrid);

            // Select each row
            var successCount = 0;
            foreach (var index in rowIndices)
            {
                if (SelectRowByIndex(dataGrid, index))
                {
                    successCount++;
                }
            }

            if (successCount == 0)
            {
                Console.WriteLine("[ChronoFileOperationsController] Failed to select any rows");
                return false;
            }

            Console.WriteLine($"[ChronoFileOperationsController] Selected {successCount}/{rowIndices.Length} row(s)");

            // Small delay to ensure UI updates
            Thread.Sleep(100);

            // Click Delete button
            return ClickDeleteButton();
        }

        /// <summary>
        /// Selects rows by GroupId and clicks the Delete button.
        /// </summary>
        /// <param name="groupIds">Array of GroupId values to select</param>
        /// <returns>True if rows were selected and Delete button clicked, false otherwise</returns>
        public bool SelectAndDeleteByGroupIds(string[] groupIds)
        {
            if (groupIds == null || groupIds.Length == 0)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot delete: no GroupIds provided");
                return false;
            }

            var dataGrid = FindDataGrid();
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot delete: DataGrid not found");
                return false;
            }

            Console.WriteLine($"[ChronoFileOperationsController] Selecting {groupIds.Length} row(s) by GroupId for delete");

            // Clear existing selection first
            ClearSelection(dataGrid);

            // Select each row by GroupId
            var successCount = 0;
            foreach (var groupId in groupIds)
            {
                if (SelectRowByGroupId(dataGrid, groupId))
                {
                    successCount++;
                }
            }

            if (successCount == 0)
            {
                Console.WriteLine("[ChronoFileOperationsController] Failed to select any rows by GroupId");
                return false;
            }

            Console.WriteLine($"[ChronoFileOperationsController] Selected {successCount}/{groupIds.Length} row(s) by GroupId");

            // Small delay to ensure UI updates
            Thread.Sleep(100);

            // Click Delete button
            return ClickDeleteButton();
        }

        /// <summary>
        /// Waits for a file operation to complete by monitoring button enabled state.
        /// </summary>
        /// <param name="buttonText">The button text to monitor ("이동" for Move, "삭제" for Delete)</param>
        /// <param name="timeoutMs">Maximum time to wait in milliseconds (default: 30000)</param>
        /// <returns>True if the operation completed (button became enabled), false if timeout</returns>
        public bool WaitForOperationComplete(string buttonText, int timeoutMs = 30000)
        {
            var startTime = Stopwatch.StartNew();
            Console.WriteLine($"[ChronoFileOperationsController] Waiting for '{buttonText}' operation to complete (timeout: {timeoutMs}ms)");

            try
            {
                while (startTime.ElapsedMilliseconds < timeoutMs)
                {
                    var isEnabled = _toolbarController.IsButtonEnabled(buttonText);
                    if (isEnabled)
                    {
                        Console.WriteLine($"[ChronoFileOperationsController] Operation completed after {startTime.ElapsedMilliseconds}ms");
                        return true;
                    }

                    Thread.Sleep(DefaultPollIntervalMs);
                }

                Console.WriteLine($"[ChronoFileOperationsController] Timeout waiting for operation to complete");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoFileOperationsController] Error waiting for operation complete: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Waits for a move operation to complete.
        /// </summary>
        /// <param name="timeoutMs">Maximum time to wait in milliseconds (default: 30000)</param>
        /// <returns>True if the operation completed, false if timeout</returns>
        public bool WaitForMoveComplete(int timeoutMs = 30000)
        {
            return WaitForOperationComplete("이동", timeoutMs);
        }

        /// <summary>
        /// Waits for a delete operation to complete.
        /// </summary>
        /// <param name="timeoutMs">Maximum time to wait in milliseconds (default: 30000)</param>
        /// <returns>True if the operation completed, false if timeout</returns>
        public bool WaitForDeleteComplete(int timeoutMs = 30000)
        {
            return WaitForOperationComplete("삭제", timeoutMs);
        }

        #endregion

        #region Delete Confirmation Dialog

        /// <summary>
        /// Finds and handles the delete confirmation dialog.
        /// </summary>
        /// <remarks>
        /// Delete operations may show a confirmation dialog (MessageBox).
        /// This method searches for a dialog containing "삭제" (Delete) or "확인" (Confirm)
        /// and clicks the confirmation button ("예"/Yes or "확인"/OK).
        /// </remarks>
        /// <returns>True if confirmation dialog was found and confirmed, or no dialog was present (OK). False if error occurred.</returns>
        public bool HandleDeleteConfirmationDialog()
        {
            try
            {
                var cf = _automation.ConditionFactory;

                // Search for a dialog/window containing "삭제" (Delete) or "확인" (Confirm)
                // Confirmation dialogs are typically Window elements
                var desktop = _automation.GetDesktop();
                var allWindows = desktop.FindAllChildren(cf.ByControlType(ControlType.Window));

                Console.WriteLine($"[ChronoFileOperationsController] Searching for confirmation dialog among {allWindows.Length} windows");

                foreach (var window in allWindows)
                {
                    var windowName = window.Name ?? "";

                    // Check if this is a confirmation dialog
                    if (windowName.IndexOf("삭제", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        windowName.IndexOf("확인", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        windowName.IndexOf("Confirm", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        windowName.IndexOf("Delete", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"[ChronoFileOperationsController] Found confirmation dialog: '{windowName}'");

                        // Look for "예" (Yes) or "확인" (OK) button
                        var buttons = window.FindAllChildren(cf.ByControlType(ControlType.Button));

                        foreach (var button in buttons)
                        {
                            var buttonName = button.Name ?? "";

                            // Look for confirmation button
                            if (buttonName.IndexOf("예", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                buttonName.IndexOf("확인", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                buttonName.IndexOf("Yes", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                buttonName.IndexOf("OK", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                Console.WriteLine($"[ChronoFileOperationsController] Clicking confirmation button: '{buttonName}'");

                                var invokePattern = button.Patterns.Invoke.Pattern;
                                if (invokePattern != null)
                                {
                                    invokePattern.Invoke();
                                    Console.WriteLine("[ChronoFileOperationsController] Successfully confirmed deletion");

                                    // Wait a bit for the dialog to close
                                    Thread.Sleep(200);
                                    return true;
                                }
                            }
                        }

                        Console.WriteLine("[ChronoFileOperationsController] Confirmation button not found in dialog");
                        return false;
                    }
                }

                Console.WriteLine("[ChronoFileOperationsController] No confirmation dialog found (may not be required)");
                return true; // No dialog might be OK - operation proceeds without confirmation
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoFileOperationsController] Error handling confirmation dialog: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Verification Methods

        /// <summary>
        /// Gets the current DataGrid row count after an operation.
        /// </summary>
        /// <returns>Current number of data rows in the DataGrid</returns>
        public int GetDataRowCountAfterOperation()
        {
            var dataGrid = FindDataGrid();
            if (dataGrid == null)
            {
                return 0;
            }

            var rows = GetDataRows(dataGrid);
            return rows.Length;
        }

        /// <summary>
        /// Verifies that a group with the specified GroupId has been deleted.
        /// </summary>
        /// <param name="groupId">The GroupId to verify</param>
        /// <returns>True if the group no longer exists in the DataGrid, false if still found</returns>
        public bool VerifyGroupDeleted(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot verify deletion: groupId is null or empty");
                return false;
            }

            var dataGrid = FindDataGrid();
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoFileOperationsController] Cannot verify deletion: DataGrid not found");
                return false;
            }

            var rows = GetDataRows(dataGrid);
            Console.WriteLine($"[ChronoFileOperationsController] Verifying deletion of GroupId '{groupId}' among {rows.Length} rows");

            try
            {
                var cf = _automation.ConditionFactory;

                for (int i = 0; i < rows.Length; i++)
                {
                    var row = rows[i];

                    // Get all Text children (cells)
                    var cells = row.FindAllChildren(cf.ByControlType(ControlType.Text));

                    // GroupId is in column 1 (index 1, after checkbox column 0)
                    if (cells.Length > 1)
                    {
                        var cellValue = cells[1].Name ?? "";
                        if (string.Equals(cellValue, groupId, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"[ChronoFileOperationsController] Verification failed: GroupId '{groupId}' still exists at row {i}");
                            return false;
                        }
                    }
                }

                Console.WriteLine($"[ChronoFileOperationsController] Verified: GroupId '{groupId}' has been deleted");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoFileOperationsController] Error verifying group deletion: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Waits for the DataGrid row count to change from the original count.
        /// </summary>
        /// <param name="originalCount">The original row count before the operation</param>
        /// <param name="timeoutMs">Maximum time to wait in milliseconds (default: 5000)</param>
        /// <returns>True if row count changed, false if timeout</returns>
        public bool WaitForRowCountChange(int originalCount, int timeoutMs = 5000)
        {
            var startTime = Stopwatch.StartNew();
            Console.WriteLine($"[ChronoFileOperationsController] Waiting for row count to change from {originalCount} (timeout: {timeoutMs}ms)");

            try
            {
                while (startTime.ElapsedMilliseconds < timeoutMs)
                {
                    var currentCount = GetDataRowCountAfterOperation();
                    if (currentCount != originalCount)
                    {
                        Console.WriteLine($"[ChronoFileOperationsController] Row count changed from {originalCount} to {currentCount} after {startTime.ElapsedMilliseconds}ms");
                        return true;
                    }

                    Thread.Sleep(DefaultPollIntervalMs);
                }

                var finalCount = GetDataRowCountAfterOperation();
                Console.WriteLine($"[ChronoFileOperationsController] Timeout waiting for row count change. Original: {originalCount}, Current: {finalCount}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoFileOperationsController] Error waiting for row count change: {ex.Message}");
                return false;
            }
        }

        #endregion

        /// <summary>
        /// Releases resources used by the UIA3 automation.
        /// </summary>
        public void Dispose()
        {
            _automation?.Dispose();
        }
    }
}
