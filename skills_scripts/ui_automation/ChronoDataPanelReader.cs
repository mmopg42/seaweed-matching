using System;
using System.Collections.Generic;
using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;

namespace SkillsScripts.UiAutomation
{
    /// <summary>
    /// High-level API for reading ChronoView data panel (StatisticsPanel and FileGroupDataGrid).
    /// Provides structured data extraction for automation testing and verification.
    /// </summary>
    public class ChronoDataPanelReader : IDisposable
    {
        private readonly UIA3Automation _automation;
        private readonly ChronoWindowFinder _windowFinder;
        private const int DefaultPollIntervalMs = 200;

        /// <summary>
        /// Initializes a new instance of the ChronoDataPanelReader class.
        /// </summary>
        /// <param name="automation">The UIA3Automation instance to use for element access</param>
        public ChronoDataPanelReader(UIA3Automation automation)
        {
            _automation = automation ?? throw new ArgumentNullException(nameof(automation));
            _windowFinder = new ChronoWindowFinder(automation);
        }

        /// <summary>
        /// Convenience constructor that creates its own UIA3Automation instance.
        /// </summary>
        public ChronoDataPanelReader() : this(new UIA3Automation())
        {
        }

        /// <summary>
        /// Finds the ChronoView MainWindow.
        /// </summary>
        /// <returns>The MainWindow if found, null otherwise</returns>
        public Window? FindMainWindow()
        {
            return _windowFinder.FindMainWindow();
        }

        #region StatisticsPanel

        /// <summary>
        /// Finds the StatisticsPanel within the ChronoView MainWindow.
        /// </summary>
        /// <remarks>
        /// The StatisticsPanel is a UserControl (StatisticsPanel.xaml).
        /// It contains file count statistics (NIR1, Normal1, Cam1-6, NIR2, Normal2)
        /// and matching statistics (Total, WithNIR, WithoutNIR, Failed, Abnormal).
        /// </remarks>
        /// <returns>The StatisticsPanel AutomationElement if found, null otherwise</returns>
        public AutomationElement? FindStatisticsPanel()
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoDataPanelReader] Cannot find StatisticsPanel: MainWindow not found");
                return null;
            }

            return FindStatisticsPanel(mainWindow);
        }

        /// <summary>
        /// Finds the StatisticsPanel within a specific window.
        /// </summary>
        /// <param name="window">The window to search within</param>
        /// <returns>The StatisticsPanel AutomationElement if found, null otherwise</returns>
        public AutomationElement? FindStatisticsPanel(Window window)
        {
            if (window == null)
            {
                Console.WriteLine("[ChronoDataPanelReader] Cannot find StatisticsPanel: window is null");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // Try to find by Name containing "StatisticsPanel"
                var nameCondition = cf.ByControlType(ControlType.Custom)
                    .And(cf.ByName("StatisticsPanel", PropertyConditionFlags.IgnoreCase));
                var panel = window.FindFirstDescendant(nameCondition);

                if (panel != null)
                {
                    Console.WriteLine("[ChronoDataPanelReader] Found StatisticsPanel by Name");
                    return panel;
                }

                // Try to find by ClassName containing "StatisticsPanel"
                var allElements = window.FindAllChildren(cf.ByControlType(ControlType.Custom));
                foreach (var element in allElements)
                {
                    if (!string.IsNullOrEmpty(element.ClassName) &&
                        element.ClassName.IndexOf("StatisticsPanel", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"[ChronoDataPanelReader] Found StatisticsPanel by ClassName: '{element.ClassName}'");
                        return element;
                    }
                }

                Console.WriteLine("[ChronoDataPanelReader] StatisticsPanel not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoDataPanelReader] Error finding StatisticsPanel: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts a statistics value from the StatisticsPanel by label text.
        /// </summary>
        /// <param name="panel">The StatisticsPanel element</param>
        /// <param name="labelText">The label text to find (e.g., "NIR1:", "Total:")</param>
        /// <returns>The statistics value as string, or null if not found</returns>
        public string? GetStatisticsValue(AutomationElement panel, string labelText)
        {
            if (panel == null)
            {
                Console.WriteLine("[ChronoDataPanelReader] Cannot get statistics value: panel is null");
                return null;
            }

            if (string.IsNullOrWhiteSpace(labelText))
            {
                Console.WriteLine("[ChronoDataPanelReader] Cannot get statistics value: labelText is null or empty");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var textBlocks = panel.FindAllChildren(cf.ByControlType(ControlType.Text));

                for (int i = 0; i < textBlocks.Length; i++)
                {
                    var textBlock = textBlocks[i];
                    if (!string.IsNullOrEmpty(textBlock.Name) &&
                        textBlock.Name.IndexOf(labelText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var parent = textBlock.Parent;
                        if (parent != null)
                        {
                            var siblings = parent.FindAllChildren();
                            for (int j = 0; j < siblings.Length; j++)
                            {
                                var sibling = siblings[j];
                                if (sibling.ControlType == ControlType.Text &&
                                    !string.IsNullOrEmpty(sibling.Name) &&
                                    sibling.Name.IndexOf(labelText, StringComparison.OrdinalIgnoreCase) < 0)
                                {
                                    var value = sibling.Name;
                                    Console.WriteLine($"[ChronoDataPanelReader] Found value for '{labelText}': '{value}'");
                                    return value;
                                }
                            }
                        }
                        break;
                    }
                }

                Console.WriteLine($"[ChronoDataPanelReader] Statistics value for '{labelText}' not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoDataPanelReader] Error getting statistics value for '{labelText}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts all statistics from the StatisticsPanel.
        /// </summary>
        /// <returns>Dictionary of statistic names to values, or null if panel not found</returns>
        public Dictionary<string, string>? GetAllStatistics()
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoDataPanelReader] Cannot get all statistics: MainWindow not found");
                return null;
            }

            return GetAllStatistics(mainWindow);
        }

        /// <summary>
        /// Extracts all statistics from the StatisticsPanel in a specific window.
        /// </summary>
        /// <param name="window">The window containing the StatisticsPanel</param>
        /// <returns>Dictionary of statistic names to values, or null if panel not found</returns>
        public Dictionary<string, string>? GetAllStatistics(Window window)
        {
            var panel = FindStatisticsPanel(window);
            if (panel == null)
            {
                Console.WriteLine("[ChronoDataPanelReader] StatisticsPanel not found");
                return null;
            }

            var statistics = new Dictionary<string, string>();

            // File count statistics
            var fileCounts = new[] { "NIR1:", "Normal1:", "Cam1:", "Cam2:", "Cam3:", "NIR2:", "일반2:", "Cam4:", "Cam5:", "Cam6:" };
            foreach (var label in fileCounts)
            {
                var value = GetStatisticsValue(panel, label);
                if (value != null)
                {
                    statistics[label.TrimEnd(':')] = value;
                }
            }

            // Matching status statistics
            var matchingStats = new[] { "Total:", "WithNIR:", "WithoutNIR:", "Failed:", "Abnormal:" };
            foreach (var label in matchingStats)
            {
                var value = GetStatisticsValue(panel, label);
                if (value != null)
                {
                    statistics[label.TrimEnd(':')] = value;
                }
            }

            Console.WriteLine($"[ChronoDataPanelReader] Extracted {statistics.Count} statistics values");
            return statistics;
        }

        #endregion

        #region DataGrid

        /// <summary>
        /// Finds the FileGroupDataGrid within the ChronoView MainWindow.
        /// </summary>
        /// <returns>The DataGrid AutomationElement if found, null otherwise</returns>
        public AutomationElement? FindDataGrid()
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoDataPanelReader] Cannot find DataGrid: MainWindow not found");
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
                Console.WriteLine("[ChronoDataPanelReader] Cannot find DataGrid: window is null");
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
                    Console.WriteLine("[ChronoDataPanelReader] Found DataGrid by Name: 'MainDataGrid'");
                    return dataGrid;
                }

                // Fallback: find any DataGrid
                var gridCondition = cf.ByControlType(ControlType.DataGrid);
                dataGrid = window.FindFirstDescendant(gridCondition);

                if (dataGrid != null)
                {
                    Console.WriteLine($"[ChronoDataPanelReader] Found DataGrid (Name: '{dataGrid.Name ?? "(unnamed)"}')");
                    return dataGrid;
                }

                Console.WriteLine("[ChronoDataPanelReader] DataGrid not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoDataPanelReader] Error finding DataGrid: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the column headers from the DataGrid.
        /// </summary>
        /// <returns>List of column header names, or empty list if not found</returns>
        public List<string> GetDataGridHeaders()
        {
            var dataGrid = FindDataGrid();
            if (dataGrid == null)
            {
                return new List<string>();
            }

            return GetDataGridHeaders(dataGrid);
        }

        /// <summary>
        /// Gets the column headers from a specific DataGrid.
        /// </summary>
        /// <param name="dataGrid">The DataGrid element</param>
        /// <returns>List of column header names, or empty list if not found</returns>
        public List<string> GetDataGridHeaders(AutomationElement dataGrid)
        {
            var headers = new List<string>();

            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoDataPanelReader] Cannot get DataGrid headers: dataGrid is null");
                return headers;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var headerCondition = cf.ByControlType(ControlType.Header);
                var header = dataGrid.FindFirstDescendant(headerCondition);

                if (header == null)
                {
                    Console.WriteLine("[ChronoDataPanelReader] DataGrid Header not found");
                    return headers;
                }

                var headerItems = header.FindAllChildren(cf.ByControlType(ControlType.HeaderItem));

                Console.WriteLine($"[ChronoDataPanelReader] Found {headerItems.Length} header columns");

                foreach (var item in headerItems)
                {
                    var name = item.Name ?? "(unnamed)";
                    headers.Add(name);
                    Console.WriteLine($"[ChronoDataPanelReader] Header: '{name}'");
                }

                return headers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoDataPanelReader] Error getting DataGrid headers: {ex.Message}");
                return headers;
            }
        }

        /// <summary>
        /// Gets the number of data rows in the DataGrid.
        /// </summary>
        /// <returns>Number of data rows, or 0 if not found</returns>
        public int GetDataRowCount()
        {
            var dataGrid = FindDataGrid();
            if (dataGrid == null)
            {
                return 0;
            }

            return GetDataRowCount(dataGrid);
        }

        /// <summary>
        /// Gets the number of data rows in a specific DataGrid.
        /// </summary>
        /// <param name="dataGrid">The DataGrid element</param>
        /// <returns>Number of data rows, or 0 if not found</returns>
        public int GetDataRowCount(AutomationElement dataGrid)
        {
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoDataPanelReader] Cannot get row count: dataGrid is null");
                return 0;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var rows = dataGrid.FindAllChildren(cf.ByControlType(ControlType.DataItem));

                Console.WriteLine($"[ChronoDataPanelReader] DataGrid has {rows.Length} data rows");
                return rows.Length;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoDataPanelReader] Error getting row count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the text content of a specific cell in a data row.
        /// </summary>
        /// <param name="rowElement">The DataItem element representing the row</param>
        /// <param name="columnIndex">Zero-based column index</param>
        /// <returns>The cell text content, or null if not found</returns>
        public string? GetCellText(AutomationElement rowElement, int columnIndex)
        {
            if (rowElement == null)
            {
                Console.WriteLine("[ChronoDataPanelReader] Cannot get cell text: rowElement is null");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var cells = rowElement.FindAllChildren(cf.ByControlType(ControlType.Text));

                if (columnIndex >= 0 && columnIndex < cells.Length)
                {
                    var cellText = cells[columnIndex].Name ?? "";
                    Console.WriteLine($"[ChronoDataPanelReader] Cell [{columnIndex}]: '{cellText}'");
                    return cellText;
                }

                Console.WriteLine($"[ChronoDataPanelReader] Column index {columnIndex} out of range (found {cells.Length} cells)");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoDataPanelReader] Error getting cell text: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts all data from a data row as a dictionary.
        /// </summary>
        /// <param name="rowElement">The DataItem element representing the row</param>
        /// <returns>Dictionary mapping column names to cell values</returns>
        public Dictionary<string, string> GetRowData(AutomationElement rowElement)
        {
            var rowData = new Dictionary<string, string>();

            if (rowElement == null)
            {
                Console.WriteLine("[ChronoDataPanelReader] Cannot get row data: rowElement is null");
                return rowData;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var cells = rowElement.FindAllChildren(cf.ByControlType(ControlType.Text));

                // Use cell index as key since headers may not be available
                for (int i = 0; i < cells.Length; i++)
                {
                    rowData[$"Column{i}"] = cells[i].Name ?? "";
                }

                Console.WriteLine($"[ChronoDataPanelReader] Extracted {rowData.Count} cells from row");
                return rowData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoDataPanelReader] Error getting row data: {ex.Message}");
                return rowData;
            }
        }

        /// <summary>
        /// Extracts all data from the DataGrid as structured objects.
        /// </summary>
        /// <returns>List of dictionaries representing rows, or empty list if DataGrid not found</returns>
        public List<Dictionary<string, string>> GetAllData()
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                return new List<Dictionary<string, string>>();
            }

            return GetAllData(mainWindow);
        }

        /// <summary>
        /// Extracts all data from a specific window's DataGrid as structured objects.
        /// </summary>
        /// <param name="window">The window containing the DataGrid</param>
        /// <returns>List of dictionaries representing rows, or empty list if DataGrid not found</returns>
        public List<Dictionary<string, string>> GetAllData(Window window)
        {
            var allData = new List<Dictionary<string, string>>();

            if (window == null)
            {
                Console.WriteLine("[ChronoDataPanelReader] Cannot get all data: window is null");
                return allData;
            }

            var dataGrid = FindDataGrid(window);
            if (dataGrid == null)
            {
                Console.WriteLine("[ChronoDataPanelReader] DataGrid not found");
                return allData;
            }

            var headers = GetDataGridHeaders(dataGrid);
            if (headers.Count == 0)
            {
                Console.WriteLine("[ChronoDataPanelReader] No headers found, cannot extract data");
                return allData;
            }

            var cf = _automation.ConditionFactory;
            var rows = dataGrid.FindAllChildren(cf.ByControlType(ControlType.DataItem));

            Console.WriteLine($"[ChronoDataPanelReader] Extracting data from {rows.Length} rows");

            foreach (var row in rows)
            {
                var rowData = new Dictionary<string, string>();
                var cells = row.FindAllChildren(cf.ByControlType(ControlType.Text));

                for (int i = 0; i < Math.Min(cells.Length, headers.Count); i++)
                {
                    var key = headers[i];
                    var value = cells[i].Name ?? "";
                    rowData[key] = value;
                }

                allData.Add(rowData);
            }

            Console.WriteLine($"[ChronoDataPanelReader] Extracted {allData.Count} rows with {headers.Count} columns each");
            return allData;
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
