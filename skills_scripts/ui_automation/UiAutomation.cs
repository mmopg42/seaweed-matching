using System;
using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;

namespace SkillsScripts.UiAutomation
{
    /// <summary>
    /// FlaUI-based UI automation helper for window finding and element inspection.
    /// Designed for ChronoView automation testing.
    /// </summary>
    public class UiAutomation : IDisposable
    {
        private readonly UIA3Automation _automation;

        /// <summary>
        /// Initializes a new instance of the UiAutomation class.
        /// </summary>
        public UiAutomation()
        {
            _automation = new UIA3Automation();
        }

        /// <summary>
        /// Finds the main window of a process by its name.
        /// </summary>
        /// <param name="processName">Name of the process (e.g., "ChronoView")</param>
        /// <returns>The Window element if found, null otherwise</returns>
        public Window? FindWindowByProcess(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                Console.WriteLine($"[UiAutomation] Process name is null or empty");
                return null;
            }

            try
            {
                // Get processes by name
                Process[] processes = Process.GetProcessesByName(processName);

                if (processes.Length == 0)
                {
                    Console.WriteLine($"[UiAutomation] No process found with name: {processName}");
                    return null;
                }

                // Use the first process found
                int processId = processes[0].Id;
                Console.WriteLine($"[UiAutomation] Found process '{processName}' with ID: {processId}");

                // Find the window using UIA3 with lambda expression
                var cf = _automation.ConditionFactory;
                var windowCondition = cf.ByControlType(ControlType.Window).And(cf.ByProcessId(processId));

                var window = _automation.GetDesktop().FindFirstDescendant(windowCondition)?.AsWindow();

                if (window == null)
                {
                    Console.WriteLine($"[UiAutomation] No window found for process '{processName}' (ID: {processId})");
                    return null;
                }

                var handle = "N/A";
                if (window.Properties.NativeWindowHandle.IsSupported)
                {
                    var windowHandle = window.Properties.NativeWindowHandle.ValueOrDefault;
                    handle = windowHandle.ToString();
                }
                Console.WriteLine($"[UiAutomation] Found window: '{window.Name}' (Handle: {handle})");
                return window;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UiAutomation] Error finding window by process '{processName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Finds a window by its title text.
        /// </summary>
        /// <param name="title">The exact or partial window title</param>
        /// <param name="substring">If true, matches windows containing the title; if false, requires exact match</param>
        /// <returns>The Window element if found, null otherwise</returns>
        public Window? FindWindowByTitle(string title, bool substring = false)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                Console.WriteLine($"[UiAutomation] Title is null or empty");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var desktop = _automation.GetDesktop();

                if (substring)
                {
                    // Find all windows and filter by substring
                    var windowCondition = cf.ByControlType(ControlType.Window);
                    var windows = desktop.FindAllChildren(windowCondition);

                    foreach (var window in windows)
                    {
                        if (!string.IsNullOrEmpty(window.Name) &&
                            window.Name.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Console.WriteLine($"[UiAutomation] Found window by substring '{title}': '{window.Name}'");
                            return window.AsWindow();
                        }
                    }

                    Console.WriteLine($"[UiAutomation] No window found containing title: {title}");
                    return null;
                }
                else
                {
                    // Exact match with case insensitivity
                    var windowCondition = cf.ByControlType(ControlType.Window)
                        .And(cf.ByName(title, PropertyConditionFlags.IgnoreCase));

                    var window = desktop.FindFirstDescendant(windowCondition)?.AsWindow();

                    if (window == null)
                    {
                        Console.WriteLine($"[UiAutomation] No window found with exact title: {title}");
                        return null;
                    }

                    Console.WriteLine($"[UiAutomation] Found window by exact title '{title}': '{window.Name}'");
                    return window;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UiAutomation] Error finding window by title '{title}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lists all child elements of an automation element in a hierarchical format.
        /// Useful for inspecting UI structure during development.
        /// </summary>
        /// <param name="parent">The parent automation element</param>
        /// <param name="maxDepth">Maximum depth to traverse (default: 3)</param>
        /// <param name="currentDepth">Current depth in recursion (internal use)</param>
        /// <param name="indent">Indentation string for current level (internal use)</param>
        public void ListElements(AutomationElement? parent, int maxDepth = 3, int currentDepth = 0, string indent = "")
        {
            if (parent == null)
            {
                Console.WriteLine("[UiAutomation] Parent element is null");
                return;
            }

            // Base element info
            if (currentDepth == 0)
            {
                Console.WriteLine($"[UiAutomation] UI Tree for '{parent.Name ?? "(unnamed)"}' (max depth: {maxDepth}):");
                PrintElementInfo(parent, indent);
            }

            // Check max depth
            if (currentDepth >= maxDepth)
            {
                return;
            }

            // Find children
            AutomationElement[] children;
            try
            {
                children = parent.FindAllChildren();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{indent}  [Error reading children: {ex.Message}]");
                return;
            }

            if (children.Length == 0)
            {
                Console.WriteLine($"{indent}  (No children)");
                return;
            }

            // Print each child
            foreach (var child in children)
            {
                PrintElementInfo(child, indent + "  ");

                // Recursively traverse child elements
                ListElements(child, maxDepth, currentDepth + 1, indent + "  ");
            }
        }

        /// <summary>
        /// Prints information about an automation element.
        /// </summary>
        private void PrintElementInfo(AutomationElement element, string indent)
        {
            var controlType = element.ControlType.ToString();
            var name = element.Name ?? "(unnamed)";
            var automationId = element.AutomationId ?? "";
            var className = element.ClassName ?? "";

            var info = $"{indent}[{controlType}]";
            if (!string.IsNullOrEmpty(name))
            {
                info += $" Name: '{name}'";
            }
            if (!string.IsNullOrEmpty(automationId))
            {
                info += $" AutomationId: '{automationId}'";
            }
            if (!string.IsNullOrEmpty(className))
            {
                info += $" Class: '{className}'";
            }

            Console.WriteLine(info);
        }

        /// <summary>
        /// Finds the ChronoView SetupWindow.
        /// </summary>
        /// <remarks>
        /// The SetupWindow has Title="Setup - ChronoView Pro" (SetupWindow.xaml line 4).
        /// It uses WindowStyle="None" and AllowsTransparency="True" which may affect detection.
        /// Uses substring search for "Setup" to ensure reliability.
        /// </remarks>
        /// <returns>The SetupWindow if found, null otherwise</returns>
        public Window? FindSetupWindow()
        {
            return FindWindowByTitle("Setup", substring: true);
        }

        /// <summary>
        /// Finds the ChronoView SettingsDialog.
        /// </summary>
        /// <remarks>
        /// The title comes from localization resource {x:Static res:Strings.Dialog_Settings}.
        /// Uses substring search for "Settings" (English) or "설정" (Korean) fallback.
        /// The dialog is shown via SettingsDialog.xaml with WindowStartupLocation="CenterOwner".
        /// </remarks>
        /// <returns>The SettingsDialog if found, null otherwise</returns>
        public Window? FindSettingsDialog()
        {
            // Try English first
            var window = FindWindowByTitle("Settings", substring: true);
            if (window != null)
            {
                return window;
            }

            // Fallback to Korean title
            return FindWindowByTitle("설정", substring: true);
        }

        /// <summary>
        /// Finds the ChronoView ImagePreviewWindow.
        /// </summary>
        /// <remarks>
        /// The ImagePreviewWindow has Title="Image Preview" (ImagePreviewWindow.xaml line 4).
        /// It uses WindowStyle="None" which may affect detection.
        /// Uses substring search for "Image Preview" to ensure reliability.
        /// </remarks>
        /// <returns>The ImagePreviewWindow if found, null otherwise</returns>
        public Window? FindImagePreviewWindow()
        {
            return FindWindowByTitle("Image Preview", substring: true);
        }

        /// <summary>
        /// Finds all ChronoView windows (main window, dialogs, and preview windows).
        /// </summary>
        /// <remarks>
        /// Searches for windows containing "ChronoView" in their title.
        /// Returns all matching windows as a List<Window> and logs their count and titles.
        /// </remarks>
        /// <returns>List of all ChronoView windows found</returns>
        public List<Window> FindAllChronoViewWindows()
        {
            var result = new List<Window>();

            try
            {
                var cf = _automation.ConditionFactory;
                var desktop = _automation.GetDesktop();
                var windowCondition = cf.ByControlType(ControlType.Window);
                var windows = desktop.FindAllChildren(windowCondition);

                foreach (var window in windows)
                {
                    if (!string.IsNullOrEmpty(window.Name) &&
                        window.Name.IndexOf("ChronoView", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result.Add(window.AsWindow());
                    }
                }

                Console.WriteLine($"[UiAutomation] Found {result.Count} ChronoView window(s):");
                foreach (var window in result)
                {
                    Console.WriteLine($"  - '{window.Name}'");
                }

                if (result.Count == 0)
                {
                    Console.WriteLine("[UiAutomation] No ChronoView windows found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UiAutomation] Error finding ChronoView windows: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Finds the ChronoView MainWindow by title.
        /// </summary>
        /// <remarks>
        /// The MainWindow title is "ChronoView Pro - Desktop Application" (MainWindow.xaml line 14).
        /// Uses substring search for "ChronoView Pro" to ensure reliable detection.
        /// </remarks>
        /// <returns>The Window element if found, null otherwise</returns>
        public Window? FindChronoViewMainWindow()
        {
            const string titleSubstring = "ChronoView Pro";
            var window = FindWindowByTitle(titleSubstring, substring: true);

            if (window == null)
            {
                Console.WriteLine($"[UiAutomation] ChronoView MainWindow not found (title containing '{titleSubstring}')");
                return null;
            }

            // Log window properties for verification
            var handle = "N/A";
            if (window.Properties.NativeWindowHandle.IsSupported)
            {
                var windowHandle = window.Properties.NativeWindowHandle.ValueOrDefault;
                handle = windowHandle.ToString();
            }

            Console.WriteLine($"[UiAutomation] ChronoView MainWindow found:");
            Console.WriteLine($"  - Name: '{window.Name}'");
            Console.WriteLine($"  - ClassName: '{window.ClassName ?? "(null)"}'");
            Console.WriteLine($"  - AutomationId: '{window.AutomationId ?? "(null)"}'");
            Console.WriteLine($"  - NativeWindowHandle: {handle}");

            return window;
        }

        /// <summary>
        /// Extracts and prints detailed properties of a window.
        /// </summary>
        /// <param name="window">The window to inspect</param>
        public void GetWindowProperties(Window? window)
        {
            if (window == null)
            {
                Console.WriteLine("[UiAutomation] Cannot get properties: window is null");
                return;
            }

            Console.WriteLine("[UiAutomation] Window Properties:");
            Console.WriteLine($"  - Name (Title): '{window.Name ?? "(null)"}'");
            Console.WriteLine($"  - ClassName: '{window.ClassName ?? "(null)"}'");
            Console.WriteLine($"  - AutomationId: '{window.AutomationId ?? "(null)"}'");

            // NativeWindowHandle
            if (window.Properties.NativeWindowHandle.IsSupported)
            {
                var handle = window.Properties.NativeWindowHandle.ValueOrDefault;
                Console.WriteLine($"  - NativeWindowHandle: {handle}");
            }
            else
            {
                Console.WriteLine($"  - NativeWindowHandle: Not supported");
            }

            // Bounds (position and size)
            if (window.Properties.BoundingRectangle.IsSupported)
            {
                var bounds = window.Properties.BoundingRectangle.ValueOrDefault;
                Console.WriteLine($"  - Bounds: X={bounds.X}, Y={bounds.Y}, Width={bounds.Width}, Height={bounds.Height}");
            }
            else
            {
                Console.WriteLine($"  - Bounds: Not supported");
            }

            // Enabled state
            if (window.Properties.IsEnabled.IsSupported)
            {
                Console.WriteLine($"  - IsEnabled: {window.Properties.IsEnabled.ValueOrDefault}");
            }

            // Offscreen state
            if (window.Properties.IsOffscreen.IsSupported)
            {
                Console.WriteLine($"  - IsOffscreen: {window.Properties.IsOffscreen.ValueOrDefault}");
            }
        }

        /// <summary>
        /// Finds ChronoView MainWindow and prints its properties and UI tree.
        /// </summary>
        public void PrintMainWindowInfo()
        {
            Console.WriteLine("[UiAutomation] === ChronoView MainWindow Detection ===");

            var window = FindChronoViewMainWindow();

            if (window == null)
            {
                Console.WriteLine("[UiAutomation] Failed to find ChronoView MainWindow");
                Console.WriteLine("[UiAutomation] Make sure ChronoView is running before using this command");
                return;
            }

            Console.WriteLine();
            GetWindowProperties(window);

            Console.WriteLine();
            Console.WriteLine("[UiAutomation] === UI Tree (depth=2) ===");
            ListElements(window, maxDepth: 2);
        }

        /// <summary>
        /// Gets the underlying UIA3Automation instance for use with ChronoWindowFinder.
        /// </summary>
        /// <returns>The UIA3Automation instance</returns>
        public UIA3Automation GetAutomation()
        {
            return _automation;
        }

        /// <summary>
        /// Finds a toolbar button within a window by its button text.
        /// </summary>
        /// <remarks>
        /// The toolbar buttons have Name text like "시작", "중지", "설정", "새로고침", "이동", "삭제"
        /// (from MainWindow.xaml lines 38, 46, 55, 63, 72, 78).
        /// This method searches for Button control type within the window and filters by Name property
        /// containing the buttonText (case-insensitive).
        /// </remarks>
        /// <param name="mainWindow">The Window element to search within</param>
        /// <param name="buttonText">The button text to search for (e.g., "시작", "중지")</param>
        /// <returns>The first matching Button element or null if not found</returns>
        public AutomationElement? FindToolbarButton(Window? mainWindow, string buttonText)
        {
            if (mainWindow == null)
            {
                Console.WriteLine($"[UiAutomation] Cannot find button: mainWindow is null");
                return null;
            }

            if (string.IsNullOrWhiteSpace(buttonText))
            {
                Console.WriteLine($"[UiAutomation] Cannot find button: buttonText is null or empty");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var buttonCondition = cf.ByControlType(ControlType.Button);
                var buttons = mainWindow.FindAllChildren(buttonCondition);

                Console.WriteLine($"[UiAutomation] Searching for button containing '{buttonText}' among {buttons.Length} buttons");

                foreach (var button in buttons)
                {
                    if (!string.IsNullOrEmpty(button.Name) &&
                        button.Name.IndexOf(buttonText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"[UiAutomation] Found button: '{button.Name}' (AutomationId: '{button.AutomationId ?? "(null)"}')");
                        return button;
                    }
                }

                Console.WriteLine($"[UiAutomation] No button found containing '{buttonText}'");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UiAutomation] Error finding toolbar button '{buttonText}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Clicks a button element using FlaUI's InvokePattern.
        /// </summary>
        /// <remarks>
        /// Uses button.Patterns.Invoke.Pattern to get InvokePattern and calls pattern.Invoke().
        /// This follows FlaUI's recommended pattern for button clicking.
        /// </remarks>
        /// <param name="button">The button element to click</param>
        /// <returns>True if successful, false if button is null or click failed</returns>
        public bool ClickButton(AutomationElement? button)
        {
            if (button == null)
            {
                Console.WriteLine("[UiAutomation] Cannot click button: button is null");
                return false;
            }

            try
            {
                var buttonName = button.Name ?? "(unnamed)";
                Console.WriteLine($"[UiAutomation] Clicking button: '{buttonName}'");

                var invokePattern = button.Patterns.Invoke.Pattern;
                if (invokePattern == null)
                {
                    Console.WriteLine($"[UiAutomation] Failed to get InvokePattern for button '{buttonName}'");
                    return false;
                }

                invokePattern.Invoke();
                Console.WriteLine($"[UiAutomation] Successfully clicked button: '{buttonName}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UiAutomation] Error clicking button: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generic helper to find and click a toolbar button by its display text.
        /// </summary>
        /// <remarks>
        /// This consolidates the logic for finding and clicking toolbar buttons.
        /// The specific methods (ClickStartButton, ClickStopButton, etc.) call this internally.
        /// This DRY approach reduces code duplication and ensures consistent behavior.
        /// </remarks>
        /// <param name="mainWindow">The MainWindow to search within</param>
        /// <param name="buttonText">The Korean button text to search for (e.g., "시작", "중지", "이동", "삭제")</param>
        /// <param name="buttonName">English button name for logging (e.g., "Start", "Stop", "Move", "Delete")</param>
        /// <returns>True if the button was found and clicked successfully, false otherwise</returns>
        public bool ClickToolbarButton(Window? mainWindow, string buttonText, string? buttonName = null)
        {
            if (mainWindow == null)
            {
                Console.WriteLine($"[UiAutomation] Cannot click {buttonName ?? buttonText} button: mainWindow is null");
                return false;
            }

            var logName = string.IsNullOrEmpty(buttonName) ? buttonText : buttonName;
            Console.WriteLine($"[UiAutomation] Attempting to click {logName} button");

            var button = FindToolbarButton(mainWindow, buttonText);
            if (button == null)
            {
                Console.WriteLine($"[UiAutomation] {logName} button not found");
                return false;
            }

            return ClickButton(button);
        }

        /// <summary>
        /// Finds and clicks the Start button in the ChronoView MainWindow toolbar.
        /// </summary>
        /// <remarks>
        /// The Start button is located at MainWindow.xaml line 33 with Command="{Binding StartCommand}".
        /// Its text is "시작" (line 38). This method finds the button by text and invokes it.
        /// </remarks>
        /// <param name="mainWindow">The MainWindow to search within</param>
        /// <returns>True if the Start button was found and clicked successfully, false otherwise</returns>
        public bool ClickStartButton(Window? mainWindow)
        {
            return ClickToolbarButton(mainWindow, "시작", "Start");
        }

        /// <summary>
        /// Finds and clicks the Stop button in the ChronoView MainWindow toolbar.
        /// </summary>
        /// <remarks>
        /// The Stop button is located at MainWindow.xaml line 41 with Command="{Binding StopCommand}".
        /// Its text is "중지" (line 46). This method finds the button by text and invokes it.
        /// </remarks>
        /// <param name="mainWindow">The MainWindow to search within</param>
        /// <returns>True if the Stop button was found and clicked successfully, false otherwise</returns>
        public bool ClickStopButton(Window? mainWindow)
        {
            return ClickToolbarButton(mainWindow, "중지", "Stop");
        }

        /// <summary>
        /// Finds and clicks the Settings (Setup) button in the ChronoView MainWindow toolbar.
        /// </summary>
        /// <remarks>
        /// The Settings button is located at MainWindow.xaml line 50 with Click="Setup_Click".
        /// Its text is "설정" (line 55). This method finds the button by text and invokes it.
        /// Note: This opens SetupWindow, not SettingsDialog. SettingsDialog is opened from within SetupWindow.
        /// </remarks>
        /// <param name="mainWindow">The MainWindow to search within</param>
        /// <returns>True if the Settings button was found and clicked successfully, false otherwise</returns>
        public bool ClickSettingsButton(Window? mainWindow)
        {
            return ClickToolbarButton(mainWindow, "설정", "Settings");
        }

        /// <summary>
        /// Finds and clicks the Refresh button in the ChronoView MainWindow toolbar.
        /// </summary>
        /// <remarks>
        /// The Refresh button is located at MainWindow.xaml line 58 with Command="{Binding RefreshCommand}".
        /// Its text is "새로고침" (line 63). This method finds the button by text and invokes it.
        /// </remarks>
        /// <param name="mainWindow">The MainWindow to search within</param>
        /// <returns>True if the Refresh button was found and clicked successfully, false otherwise</returns>
        public bool ClickRefreshButton(Window? mainWindow)
        {
            return ClickToolbarButton(mainWindow, "새로고침", "Refresh");
        }

        /// <summary>
        /// Finds and clicks the Move button in the ChronoView MainWindow toolbar.
        /// </summary>
        /// <remarks>
        /// The Move button is located at MainWindow.xaml line 67 with Command="{Binding MoveCommand}".
        /// Its text is "이동" (line 72). This method finds the button by text and invokes it.
        /// </remarks>
        /// <param name="mainWindow">The MainWindow to search within</param>
        /// <returns>True if the Move button was found and clicked successfully, false otherwise</returns>
        public bool ClickMoveButton(Window? mainWindow)
        {
            return ClickToolbarButton(mainWindow, "이동", "Move");
        }

        /// <summary>
        /// Finds and clicks the Delete button in the ChronoView MainWindow toolbar.
        /// </summary>
        /// <remarks>
        /// The Delete button is located at MainWindow.xaml line 75 with Command="{Binding DeleteCommand}".
        /// Its text is "삭제" (line 78). This method finds the button by text and invokes it.
        /// </remarks>
        /// <param name="mainWindow">The MainWindow to search within</param>
        /// <returns>True if the Delete button was found and clicked successfully, false otherwise</returns>
        public bool ClickDeleteButton(Window? mainWindow)
        {
            return ClickToolbarButton(mainWindow, "삭제", "Delete");
        }

        /// <summary>
        /// Finds the StatisticsPanel within the ChronoView MainWindow.
        /// </summary>
        /// <remarks>
        /// The StatisticsPanel is a UserControl (StatisticsPanel.xaml).
        /// It contains file count statistics (NIR1, Normal1, Cam1-6, NIR2, Normal2)
        /// and matching statistics (Total, WithNIR, WithoutNIR, Failed, Abnormal).
        /// This method searches for a UserControl with Name or ClassName containing "StatisticsPanel".
        /// </remarks>
        /// <param name="mainWindow">The MainWindow to search within</param>
        /// <returns>The StatisticsPanel AutomationElement if found, null otherwise</returns>
        public AutomationElement? FindStatisticsPanel(Window? mainWindow)
        {
            if (mainWindow == null)
            {
                Console.WriteLine("[UiAutomation] Cannot find StatisticsPanel: mainWindow is null");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // Try to find by Name containing "StatisticsPanel"
                var nameCondition = cf.ByControlType(ControlType.Custom)
                    .And(cf.ByName("StatisticsPanel", PropertyConditionFlags.IgnoreCase));
                var panel = mainWindow.FindFirstDescendant(nameCondition);

                if (panel != null)
                {
                    Console.WriteLine("[UiAutomation] Found StatisticsPanel by Name");
                    return panel;
                }

                // Try to find by ClassName containing "StatisticsPanel"
                var allElements = mainWindow.FindAllChildren(cf.ByControlType(ControlType.Custom));
                foreach (var element in allElements)
                {
                    if (!string.IsNullOrEmpty(element.ClassName) &&
                        element.ClassName.IndexOf("StatisticsPanel", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"[UiAutomation] Found StatisticsPanel by ClassName: '{element.ClassName}'");
                        return element;
                    }
                }

                Console.WriteLine("[UiAutomation] StatisticsPanel not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UiAutomation] Error finding StatisticsPanel: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Finds the DataGrid within the ChronoView MainWindow.
        /// </summary>
        /// <remarks>
        /// The FileGroupDataGrid is the main data grid displaying file groups.
        /// It appears as ControlType.DataGrid in UI Automation.
        /// Can be filtered by Name ("MainDataGrid" from FileGroupDataGrid.xaml line 18).
        /// </remarks>
        /// <param name="mainWindow">The MainWindow to search within</param>
        /// <returns>The DataGrid AutomationElement if found, null otherwise</returns>
        public AutomationElement? FindDataGrid(Window? mainWindow)
        {
            if (mainWindow == null)
            {
                Console.WriteLine("[UiAutomation] Cannot find DataGrid: mainWindow is null");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // First try to find by Name "MainDataGrid"
                var nameCondition = cf.ByControlType(ControlType.DataGrid)
                    .And(cf.ByName("MainDataGrid", PropertyConditionFlags.IgnoreCase));
                var dataGrid = mainWindow.FindFirstDescendant(nameCondition);

                if (dataGrid != null)
                {
                    Console.WriteLine("[UiAutomation] Found DataGrid by Name: 'MainDataGrid'");
                    return dataGrid;
                }

                // Fallback: find any DataGrid
                var gridCondition = cf.ByControlType(ControlType.DataGrid);
                dataGrid = mainWindow.FindFirstDescendant(gridCondition);

                if (dataGrid != null)
                {
                    Console.WriteLine($"[UiAutomation] Found DataGrid (Name: '{dataGrid.Name ?? "(unnamed)"}')");
                    return dataGrid;
                }

                Console.WriteLine("[UiAutomation] DataGrid not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UiAutomation] Error finding DataGrid: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Finds the WorkflowPanel within the ChronoView MainWindow.
        /// </summary>
        /// <remarks>
        /// The WorkflowPanel is a UserControl (WorkflowPanel.xaml).
        /// It contains camera status buttons (General, NIR, NIR2, NIR Filtering),
        /// sample move settings (path TextBox controls for Line1/Line2),
        /// and data status displays.
        /// This method searches for a Custom control with Name or ClassName containing "WorkflowPanel".
        /// </remarks>
        /// <param name="mainWindow">The MainWindow to search within</param>
        /// <returns>The WorkflowPanel AutomationElement if found, null otherwise</returns>
        public AutomationElement? FindWorkflowPanel(Window? mainWindow)
        {
            if (mainWindow == null)
            {
                Console.WriteLine("[UiAutomation] Cannot find WorkflowPanel: mainWindow is null");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // Try to find by Name containing "WorkflowPanel"
                var nameCondition = cf.ByControlType(ControlType.Custom)
                    .And(cf.ByName("WorkflowPanel", PropertyConditionFlags.IgnoreCase));
                var panel = mainWindow.FindFirstDescendant(nameCondition);

                if (panel != null)
                {
                    Console.WriteLine("[UiAutomation] Found WorkflowPanel by Name:");
                    Console.WriteLine($"  - ControlType: {panel.ControlType}");
                    Console.WriteLine($"  - Name: '{panel.Name ?? "(unnamed)"}'");
                    Console.WriteLine($"  - AutomationId: '{panel.AutomationId ?? "(null)"}'");
                    Console.WriteLine($"  - ClassName: '{panel.ClassName ?? "(null)"}'");
                    return panel;
                }

                // Try to find by ClassName containing "WorkflowPanel"
                var allElements = mainWindow.FindAllChildren(cf.ByControlType(ControlType.Custom));
                foreach (var element in allElements)
                {
                    if (!string.IsNullOrEmpty(element.ClassName) &&
                        element.ClassName.IndexOf("WorkflowPanel", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"[UiAutomation] Found WorkflowPanel by ClassName:");
                        Console.WriteLine($"  - ControlType: {element.ControlType}");
                        Console.WriteLine($"  - Name: '{element.Name ?? "(unnamed)"}'");
                        Console.WriteLine($"  - AutomationId: '{element.AutomationId ?? "(null)"}'");
                        Console.WriteLine($"  - ClassName: '{element.ClassName}'");
                        return element;
                    }
                }

                Console.WriteLine("[UiAutomation] WorkflowPanel not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UiAutomation] Error finding WorkflowPanel: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts a statistics value from the StatisticsPanel by label text.
        /// </summary>
        /// <remarks>
        /// The statistics use a Border with StackPanel containing a label TextBlock
        /// and a value TextBlock (e.g., lines 19-27 of StatisticsPanel.xaml).
        /// This method finds the Border containing the label and extracts the value.
        /// </remarks>
        /// <param name="panel">The StatisticsPanel element</param>
        /// <param name="labelText">The label text to find (e.g., "NIR1:", "Total:")</param>
        /// <returns>The statistics value as string, or null if not found</returns>
        public string? GetStatisticsValue(AutomationElement? panel, string labelText)
        {
            if (panel == null)
            {
                Console.WriteLine("[UiAutomation] Cannot get statistics value: panel is null");
                return null;
            }

            if (string.IsNullOrWhiteSpace(labelText))
            {
                Console.WriteLine("[UiAutomation] Cannot get statistics value: labelText is null or empty");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // Find all TextBlocks in the panel
                var textBlocks = panel.FindAllChildren(cf.ByControlType(ControlType.Text));

                Console.WriteLine($"[UiAutomation] Searching for statistics label '{labelText}' among {textBlocks.Length} text elements");

                // Find the label TextBlock
                for (int i = 0; i < textBlocks.Length; i++)
                {
                    var textBlock = textBlocks[i];
                    if (!string.IsNullOrEmpty(textBlock.Name) &&
                        textBlock.Name.IndexOf(labelText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // The value should be the next TextBlock (sibling with bold text)
                        // Try to get it from the parent's children
                        var parent = textBlock.Parent;
                        if (parent != null)
                        {
                            var siblings = parent.FindAllChildren();
                            for (int j = 0; j < siblings.Length; j++)
                            {
                                var sibling = siblings[j];
                                // Look for the next TextBlock with actual content (not just the label)
                                if (sibling.ControlType == ControlType.Text &&
                                    !string.IsNullOrEmpty(sibling.Name) &&
                                    sibling.Name.IndexOf(labelText, StringComparison.OrdinalIgnoreCase) < 0)
                                {
                                    var value = sibling.Name;
                                    Console.WriteLine($"[UiAutomation] Found value for '{labelText}': '{value}'");
                                    return value;
                                }
                            }
                        }
                        break;
                    }
                }

                Console.WriteLine($"[UiAutomation] Statistics value for '{labelText}' not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UiAutomation] Error getting statistics value for '{labelText}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts all statistics from the StatisticsPanel.
        /// </summary>
        /// <remarks>
        /// Returns a dictionary containing all file counts and matching statistics.
        /// File Counts: NIR1, Normal1, Cam1, Cam2, Cam3, NIR2, Normal2, Cam4, Cam5, Cam6
        /// Matching Status: Total, WithNIR, WithoutNIR, Failed, Abnormal
        /// </remarks>
        /// <param name="mainWindow">The MainWindow to search within</param>
        /// <returns>Dictionary of statistic names to values, or null if panel not found</returns>
        public Dictionary<string, string>? GetAllStatistics(Window? mainWindow)
        {
            if (mainWindow == null)
            {
                Console.WriteLine("[UiAutomation] Cannot get all statistics: mainWindow is null");
                return null;
            }

            var panel = FindStatisticsPanel(mainWindow);
            if (panel == null)
            {
                Console.WriteLine("[UiAutomation] StatisticsPanel not found");
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
                    // Remove colon for key
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

            Console.WriteLine($"[UiAutomation] Extracted {statistics.Count} statistics values");
            return statistics;
        }

        /// <summary>
        /// Gets the column headers from the DataGrid.
        /// </summary>
        /// <remarks>
        /// DataGrid headers are typically in a DataGridItemsControl with ControlType.Header.
        /// This method finds the header row and extracts all column names.
        /// </remarks>
        /// <param name="dataGrid">The DataGrid element</param>
        /// <returns>List of column header names, or empty list if not found</returns>
        public List<string> GetDataGridHeaders(AutomationElement? dataGrid)
        {
            var headers = new List<string>();

            if (dataGrid == null)
            {
                Console.WriteLine("[UiAutomation] Cannot get DataGrid headers: dataGrid is null");
                return headers;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // Find Header element
                var headerCondition = cf.ByControlType(ControlType.Header);
                var header = dataGrid.FindFirstDescendant(headerCondition);

                if (header == null)
                {
                    Console.WriteLine("[UiAutomation] DataGrid Header not found");
                    return headers;
                }

                // Get all header items
                var headerItems = header.FindAllChildren(cf.ByControlType(ControlType.HeaderItem));

                Console.WriteLine($"[UiAutomation] Found {headerItems.Length} header columns");

                foreach (var item in headerItems)
                {
                    var name = item.Name ?? "(unnamed)";
                    headers.Add(name);
                    Console.WriteLine($"[UiAutomation] Header: '{name}'");
                }

                return headers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UiAutomation] Error getting DataGrid headers: {ex.Message}");
                return headers;
            }
        }

        /// <summary>
        /// Gets the number of data rows in the DataGrid.
        /// </summary>
        /// <remarks>
        /// Counts DataRow elements (ControlType.DataItem) in the grid.
        /// </remarks>
        /// <param name="dataGrid">The DataGrid element</param>
        /// <returns>Number of data rows, or 0 if not found</returns>
        public int GetDataRowCount(AutomationElement? dataGrid)
        {
            if (dataGrid == null)
            {
                Console.WriteLine("[UiAutomation] Cannot get row count: dataGrid is null");
                return 0;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var rows = dataGrid.FindAllChildren(cf.ByControlType(ControlType.DataItem));

                Console.WriteLine($"[UiAutomation] DataGrid has {rows.Length} data rows");
                return rows.Length;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UiAutomation] Error getting row count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the text content of a specific cell in a data row.
        /// </summary>
        /// <remarks>
        /// DataGrid cells are typically TextBlock elements within DataItem children.
        /// This method finds the cell at the specified column index within a row.
        /// </remarks>
        /// <param name="rowElement">The DataItem element representing the row</param>
        /// <param name="columnIndex">Zero-based column index</param>
        /// <returns>The cell text content, or null if not found</returns>
        public string? GetCellText(AutomationElement? rowElement, int columnIndex)
        {
            if (rowElement == null)
            {
                Console.WriteLine("[UiAutomation] Cannot get cell text: rowElement is null");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var cells = rowElement.FindAllChildren(cf.ByControlType(ControlType.Text));

                if (columnIndex >= 0 && columnIndex < cells.Length)
                {
                    var cellText = cells[columnIndex].Name ?? "";
                    Console.WriteLine($"[UiAutomation] Cell [{columnIndex}]: '{cellText}'");
                    return cellText;
                }

                Console.WriteLine($"[UiAutomation] Column index {columnIndex} out of range (found {cells.Length} cells)");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UiAutomation] Error getting cell text: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts all data from a data row as a dictionary.
        /// </summary>
        /// <remarks>
        /// Returns column names mapped to cell values for the specified row.
        /// Requires headers to be known for proper key mapping.
        /// </remarks>
        /// <param name="rowElement">The DataItem element representing the row</param>
        /// <param name="headers">List of column header names for keys</param>
        /// <returns>Dictionary mapping column names to cell values</returns>
        public Dictionary<string, string> GetRowData(AutomationElement? rowElement, List<string> headers)
        {
            var rowData = new Dictionary<string, string>();

            if (rowElement == null)
            {
                Console.WriteLine("[UiAutomation] Cannot get row data: rowElement is null");
                return rowData;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var cells = rowElement.FindAllChildren(cf.ByControlType(ControlType.Text));

                for (int i = 0; i < Math.Min(cells.Length, headers.Count); i++)
                {
                    var key = headers[i];
                    var value = cells[i].Name ?? "";
                    rowData[key] = value;
                }

                Console.WriteLine($"[UiAutomation] Extracted {rowData.Count} cells from row");
                return rowData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UiAutomation] Error getting row data: {ex.Message}");
                return rowData;
            }
        }

        /// <summary>
        /// Extracts all data from the DataGrid.
        /// </summary>
        /// <remarks>
        /// Returns a list of dictionaries, each representing a row with column headers as keys.
        /// This is the primary method for bulk data extraction from the grid.
        /// </remarks>
        /// <param name="mainWindow">The MainWindow to search within</param>
        /// <returns>List of row dictionaries, or empty list if DataGrid not found</returns>
        public List<Dictionary<string, string>> GetAllDataGridData(Window? mainWindow)
        {
            var allData = new List<Dictionary<string, string>>();

            if (mainWindow == null)
            {
                Console.WriteLine("[UiAutomation] Cannot get all data: mainWindow is null");
                return allData;
            }

            var dataGrid = FindDataGrid(mainWindow);
            if (dataGrid == null)
            {
                Console.WriteLine("[UiAutomation] DataGrid not found");
                return allData;
            }

            var headers = GetDataGridHeaders(dataGrid);
            if (headers.Count == 0)
            {
                Console.WriteLine("[UiAutomation] No headers found, cannot extract data");
                return allData;
            }

            var cf = _automation.ConditionFactory;
            var rows = dataGrid.FindAllChildren(cf.ByControlType(ControlType.DataItem));

            Console.WriteLine($"[UiAutomation] Extracting data from {rows.Length} rows");

            foreach (var row in rows)
            {
                var rowData = GetRowData(row, headers);
                allData.Add(rowData);
            }

            Console.WriteLine($"[UiAutomation] Extracted {allData.Count} rows with {headers.Count} columns each");
            return allData;
        }

        /// <summary>
        /// Releases resources used by the UIA3 automation.
        /// </summary>
        public void Dispose()
        {
            _automation?.Dispose();
        }
    }
}
