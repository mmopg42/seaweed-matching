using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;

namespace SkillsScripts.UiAutomation
{
    /// <summary>
    /// High-level API for ChronoView SettingsDialog automation.
    /// Consolidates SettingsDialog lifecycle management into a cohesive, testable class.
    /// </summary>
    /// <remarks>
    /// This class provides a complete API for SettingsDialog interactions including:
    /// - Finding the MainWindow via ChronoWindowFinder
    /// - Finding SettingsDialog via ChronoWindowFinder
    /// - Opening SettingsDialog via Settings toolbar button click
    /// - Closing SettingsDialog via Cancel button click
    /// - Inspecting SettingsDialog element structure for analysis
    /// - Checking if SettingsDialog is currently open
    /// - Tab selection (Paths, Data Sequence, UI Options, Advanced, External Programs)
    /// - Path TextBox reading and writing (Line 1 and Line 2 paths)
    /// - CheckBox manipulation (Advanced tab, UI Options tab, Data Sequence tab)
    /// - Dialog action buttons (Save/Apply/Cancel/Reset)
    ///
    /// SettingsDialog buttons have bilingual text labels:
    /// - "Save" or "확인"
    /// - "Cancel" or "취소"
    /// - "Apply" or "적용"
    /// - "Reset/Defaults" or "기본값"
    ///
    /// The Settings button in the toolbar has text "설정" (Korean).
    /// </remarks>
    public class ChronoSettingsController : IDisposable
    {
        private readonly UIA3Automation _automation;
        private readonly ChronoWindowFinder _windowFinder;
        private const int DefaultPollIntervalMs = 200;

        /// <summary>
        /// Initializes a new instance of the ChronoSettingsController class.
        /// </summary>
        /// <param name="automation">The UIA3Automation instance to use for UI automation</param>
        public ChronoSettingsController(UIA3Automation automation)
        {
            _automation = automation ?? throw new ArgumentNullException(nameof(automation));
            _windowFinder = new ChronoWindowFinder(automation);
        }

        /// <summary>
        /// Initializes a new instance of the ChronoSettingsController class,
        /// creating its own UIA3Automation instance.
        /// </summary>
        public ChronoSettingsController() : this(new UIA3Automation())
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

        /// <summary>
        /// Finds the ChronoView SettingsDialog.
        /// </summary>
        /// <returns>The SettingsDialog if found, null otherwise</returns>
        public Window? FindSettingsDialog()
        {
            return _windowFinder.FindSettingsDialog();
        }

        /// <summary>
        /// Opens the SettingsDialog by clicking the Settings toolbar button.
        /// </summary>
        /// <remarks>
        /// Tries MainWindow first, then SetupWindow if MainWindow not found.
        /// Locates the Settings button (text "설정" or "Settings"),
        /// clicks it using InvokePattern, and waits for the SettingsDialog to appear.
        /// </remarks>
        /// <param name="mainWindow">The MainWindow to search within (optional, will find if null)</param>
        /// <param name="timeoutMs">Maximum time to wait for the dialog to appear in milliseconds (default: 5000)</param>
        /// <returns>True if the dialog was opened successfully, false otherwise</returns>
        public bool OpenSettingsDialog(Window? mainWindow = null, int timeoutMs = 5000)
        {
            // Check SetupWindow FIRST (before MainWindow)
            // because FindMainWindow() may return SetupWindow if both exist
            var setupWindow = _windowFinder.FindSetupWindow();

            // Only look for MainWindow if SetupWindow doesn't exist
            if (setupWindow == null && mainWindow == null)
            {
                mainWindow = FindMainWindow();
            }

            // Determine target window
            Window? targetWindow = setupWindow ?? mainWindow;
            if (targetWindow == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot open SettingsDialog: Neither MainWindow nor SetupWindow found");
                return false;
            }

            Console.WriteLine($"[ChronoSettingsController] Attempting to open SettingsDialog via '{targetWindow.Name}'");

            // Find Settings button - use different method for SetupWindow
            AutomationElement? settingsButton;
            if (setupWindow != null)
            {
                // SetupWindow: Settings button has no Name, find by Image child
                Console.WriteLine("[ChronoSettingsController] Using SetupWindow button detection");
                settingsButton = FindSetupWindowSettingsButton(setupWindow);
            }
            else
            {
                // MainWindow: Find by button text
                Console.WriteLine("[ChronoSettingsController] Using MainWindow button detection");
                settingsButton = FindToolbarButton(targetWindow, "설정");
                if (settingsButton == null)
                {
                    settingsButton = FindToolbarButton(targetWindow, "Settings");
                }
            }

            if (settingsButton == null)
            {
                Console.WriteLine($"[ChronoSettingsController] Settings button not found in {targetWindow.Name}");
                return false;
            }

            // Click the Settings button
            if (!ClickButton(settingsButton))
            {
                Console.WriteLine("[ChronoSettingsController] Failed to click Settings button");
                return false;
            }

            // Wait for SettingsDialog to appear
            var dialog = _windowFinder.WaitForWindow("Settings", timeoutMs);
            if (dialog == null)
            {
                // Try Korean title
                dialog = _windowFinder.WaitForWindow("설정", timeoutMs);
            }

            if (dialog != null)
            {
                Console.WriteLine($"[ChronoSettingsController] SettingsDialog opened successfully: '{dialog.Name}'");
                return true;
            }

            Console.WriteLine("[ChronoSettingsController] Timeout waiting for SettingsDialog to appear");
            return false;
        }

        /// <summary>
        /// Closes the SettingsDialog by clicking the Cancel button.
        /// </summary>
        /// <remarks>
        /// Finds the SettingsDialog, locates the Cancel button (text "취소" or "Cancel"),
        /// clicks it using InvokePattern, and waits for the dialog to close.
        /// </remarks>
        /// <param name="timeoutMs">Maximum time to wait for the dialog to close in milliseconds (default: 5000)</param>
        /// <returns>True if the dialog was closed successfully, false otherwise</returns>
        public bool CloseSettingsDialog(int timeoutMs = 5000)
        {
            var dialog = FindSettingsDialog();
            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot close SettingsDialog: dialog not found (already closed?)");
                return false;
            }

            Console.WriteLine("[ChronoSettingsController] Attempting to close SettingsDialog via Cancel button");

            // Find Cancel button in dialog
            var cancelButton = FindButton(dialog, "취소");
            if (cancelButton == null)
            {
                // Try English button text
                cancelButton = FindButton(dialog, "Cancel");
            }

            if (cancelButton == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cancel button not found in SettingsDialog");
                return false;
            }

            // Click the Cancel button
            if (!ClickButton(cancelButton))
            {
                Console.WriteLine("[ChronoSettingsController] Failed to click Cancel button");
                return false;
            }

            // Wait for SettingsDialog to close
            bool closed = _windowFinder.WaitForWindowToClose("Settings", timeoutMs);
            if (!closed)
            {
                // Try Korean title
                closed = _windowFinder.WaitForWindowToClose("설정", timeoutMs);
            }

            if (closed)
            {
                Console.WriteLine("[ChronoSettingsController] SettingsDialog closed successfully");
                return true;
            }

            Console.WriteLine("[ChronoSettingsController] Timeout waiting for SettingsDialog to close");
            return false;
        }

        /// <summary>
        /// Checks if the SettingsDialog is currently open.
        /// </summary>
        /// <remarks>
        /// Uses ChronoWindowFinder.IsWindowOpen to check for "Settings" (English) or "설정" (Korean).
        /// </remarks>
        /// <returns>True if the SettingsDialog is open, false otherwise</returns>
        public bool IsSettingsDialogOpen()
        {
            bool isOpen = _windowFinder.IsWindowOpen("Settings");
            if (!isOpen)
            {
                // Try Korean title
                isOpen = _windowFinder.IsWindowOpen("설정");
            }

            Console.WriteLine($"[ChronoSettingsController] SettingsDialog is {(isOpen ? "open" : "not open")}");
            return isOpen;
        }

        /// <summary>
        /// Inspects the SettingsDialog element structure for analysis.
        /// </summary>
        /// <remarks>
        /// Finds the SettingsDialog and lists its element tree at depth=2 using ListElements.
        /// Useful for understanding the dialog structure during development and testing.
        /// </remarks>
        /// <returns>True if inspection was successful, false otherwise</returns>
        public bool InspectSettingsDialog()
        {
            var dialog = FindSettingsDialog();
            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot inspect SettingsDialog: dialog not found");
                Console.WriteLine("[ChronoSettingsController] Hint: Use OpenSettingsDialog() first or ensure the dialog is open");
                return false;
            }

            Console.WriteLine("[ChronoSettingsController] SettingsDialog inspection:");
            Console.WriteLine($"  - Title: '{dialog.Name ?? "(unnamed)"}'");
            Console.WriteLine($"  - ClassName: '{dialog.ClassName ?? "(null)"}'");
            Console.WriteLine($"  - AutomationId: '{dialog.AutomationId ?? "(null)"}'");
            Console.WriteLine();
            Console.WriteLine("=== Element Tree (depth=2) ===");
            ListElements(dialog, maxDepth: 2);
            return true;
        }

        /// <summary>
        /// Finds a toolbar button within a window by its button text.
        /// </summary>
        /// <param name="window">The Window element to search within</param>
        /// <param name="buttonText">The button text to search for</param>
        /// <returns>The first matching Button element or null if not found</returns>
        private AutomationElement? FindToolbarButton(Window? window, string buttonText)
        {
            if (window == null)
            {
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var buttonCondition = cf.ByControlType(ControlType.Button);
                var buttons = window.FindAllChildren(buttonCondition);

                foreach (var button in buttons)
                {
                    if (!string.IsNullOrEmpty(button.Name) &&
                        button.Name.IndexOf(buttonText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return button;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoSettingsController] Error finding toolbar button '{buttonText}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Finds a button within a window by its button text.
        /// </summary>
        /// <param name="window">The Window element to search within</param>
        /// <param name="buttonText">The button text to search for</param>
        /// <returns>The first matching Button element or null if not found</returns>
        private AutomationElement? FindButton(Window? window, string buttonText)
        {
            return FindToolbarButton(window, buttonText);
        }

        /// <summary>
        /// Finds the Settings button in SetupWindow.
        /// </summary>
        /// <remarks>
        /// SetupWindow's Settings button has no Name, only an Image icon.
        /// It's identified by checking if it has an Image child element.
        /// </remarks>
        /// <param name="setupWindow">The SetupWindow to search within</param>
        /// <returns>The Settings button element or null if not found</returns>
        private AutomationElement? FindSetupWindowSettingsButton(Window? setupWindow)
        {
            if (setupWindow == null)
            {
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var buttons = setupWindow.FindAllChildren(cf.ByControlType(ControlType.Button));

                Console.WriteLine($"[ChronoSettingsController] Found {buttons.Length} buttons in SetupWindow");

                // Find the first button that contains an Image (Settings button has an icon)
                foreach (var button in buttons)
                {
                    try
                    {
                        var imageElement = button.FindFirstDescendant(cf.ByControlType(ControlType.Image));
                        if (imageElement != null)
                        {
                            Console.WriteLine("[ChronoSettingsController] Found SetupWindow Settings button (contains Image)");
                            return button;
                        }
                    }
                    catch
                    {
                        // Skip buttons that can't be searched
                        continue;
                    }
                }

                // Fallback: try by size if Image search fails
                foreach (var button in buttons)
                {
                    try
                    {
                        var bounds = button.BoundingRectangle;
                        if (bounds.Width > 35 && bounds.Width < 45 &&
                            bounds.Height > 35 && bounds.Height < 45)
                        {
                            Console.WriteLine($"[ChronoSettingsController] Found SetupWindow Settings button by size: {bounds.Width}x{bounds.Height}");
                            return button;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                Console.WriteLine("[ChronoSettingsController] SetupWindow Settings button not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoSettingsController] Error finding SetupWindow Settings button: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Clicks a button element using FlaUI's InvokePattern.
        /// </summary>
        /// <param name="button">The button element to click</param>
        /// <returns>True if successful, false if button is null or click failed</returns>
        private bool ClickButton(AutomationElement? button)
        {
            if (button == null)
            {
                return false;
            }

            try
            {
                var buttonName = button.Name ?? "(unnamed)";
                Console.WriteLine($"[ChronoSettingsController] Clicking button: '{buttonName}'");

                var invokePattern = button.Patterns.Invoke.Pattern;
                if (invokePattern == null)
                {
                    Console.WriteLine($"[ChronoSettingsController] Failed to get InvokePattern for button '{buttonName}'");
                    return false;
                }

                invokePattern.Invoke();
                Console.WriteLine($"[ChronoSettingsController] Successfully clicked button: '{buttonName}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoSettingsController] Error clicking button: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lists all child elements of an automation element in a hierarchical format.
        /// </summary>
        /// <param name="parent">The parent automation element</param>
        /// <param name="maxDepth">Maximum depth to traverse (default: 3)</param>
        private void ListElements(AutomationElement? parent, int maxDepth = 3)
        {
            if (parent == null)
            {
                return;
            }

            PrintElementInfo(parent, "");

            if (maxDepth <= 0)
            {
                return;
            }

            try
            {
                var children = parent.FindAllChildren();
                foreach (var child in children)
                {
                    ListElementsRecursive(child, maxDepth - 1, "  ");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [Error reading children: {ex.Message}]");
            }
        }

        /// <summary>
        /// Recursively lists elements with indentation.
        /// </summary>
        private void ListElementsRecursive(AutomationElement element, int remainingDepth, string indent)
        {
            PrintElementInfo(element, indent);

            if (remainingDepth <= 0)
            {
                return;
            }

            try
            {
                var children = element.FindAllChildren();
                foreach (var child in children)
                {
                    ListElementsRecursive(child, remainingDepth - 1, indent + "  ");
                }
            }
            catch
            {
                // Silently skip children that can't be read
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
        /// Selects a tab in the SettingsDialog TabControl by tab name.
        /// </summary>
        /// <remarks>
        /// Finds the TabControl, then searches for TabItem with Header (Name) containing tabName.
        /// Uses SelectionItemPattern.Select() to activate the tab.
        /// Supports bilingual tab headers (English first, Korean fallback).
        ///
        /// Tab names in SettingsDialog.xaml:
        /// - "Paths" / "경로"
        /// - "Data Sequence" / "데이터 순서"
        /// - "UI Options" / "UI 옵션"
        /// - "Advanced" / "고급"
        /// - "External Programs" / "외부 프로그램"
        /// </remarks>
        /// <param name="dialog">The SettingsDialog window (optional, will find if null)</param>
        /// <param name="tabName">The tab name to search for (substring match)</param>
        /// <returns>True if the tab was selected successfully, false otherwise</returns>
        public bool SelectTab(Window? dialog, string tabName)
        {
            dialog ??= FindSettingsDialog();
            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot select tab: SettingsDialog not found");
                return false;
            }

            if (string.IsNullOrWhiteSpace(tabName))
            {
                Console.WriteLine("[ChronoSettingsController] Cannot select tab: tabName is null or empty");
                return false;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // Find TabControl
                var tabControl = dialog.FindFirstDescendant(cf.ByControlType(ControlType.Tab));
                if (tabControl == null)
                {
                    Console.WriteLine("[ChronoSettingsController] TabControl not found in SettingsDialog");
                    return false;
                }

                Console.WriteLine($"[ChronoSettingsController] Searching for tab containing '{tabName}'");

                // Find all TabItem children
                var tabItems = tabControl.FindAllChildren(cf.ByControlType(ControlType.TabItem));

                // Search for matching tab
                AutomationElement? targetTab = null;
                foreach (var tabItem in tabItems)
                {
                    if (!string.IsNullOrEmpty(tabItem.Name) &&
                        tabItem.Name.IndexOf(tabName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        targetTab = tabItem;
                        Console.WriteLine($"[ChronoSettingsController] Found tab: '{tabItem.Name}'");
                        break;
                    }
                }

                if (targetTab == null)
                {
                    Console.WriteLine($"[ChronoSettingsController] Tab '{tabName}' not found");
                    Console.WriteLine($"[ChronoSettingsController] Available tabs: {string.Join(", ", GetAvailableTabNames(tabItems))}");
                    return false;
                }

                // Use SelectionItemPattern to select the tab
                var selectionPattern = targetTab.Patterns.SelectionItem.Pattern;
                if (selectionPattern == null)
                {
                    Console.WriteLine($"[ChronoSettingsController] SelectionItemPattern not available for tab '{tabName}'");
                    return false;
                }

                selectionPattern.Select();
                Console.WriteLine($"[ChronoSettingsController] Successfully selected tab: '{targetTab.Name}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoSettingsController] Error selecting tab '{tabName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the name of the currently selected tab in the SettingsDialog.
        /// </summary>
        /// <param name="dialog">The SettingsDialog window (optional, will find if null)</param>
        /// <returns>The name of the selected tab, or empty string if not found</returns>
        public string GetSelectedTab(Window? dialog)
        {
            dialog ??= FindSettingsDialog();
            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot get selected tab: SettingsDialog not found");
                return string.Empty;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // Find TabControl
                var tabControl = dialog.FindFirstDescendant(cf.ByControlType(ControlType.Tab));
                if (tabControl == null)
                {
                    Console.WriteLine("[ChronoSettingsController] TabControl not found in SettingsDialog");
                    return string.Empty;
                }

                // Find selected TabItem using SelectionItemPattern.IsSelected property
                var tabItems = tabControl.FindAllChildren(cf.ByControlType(ControlType.TabItem));
                foreach (var tabItem in tabItems)
                {
                    var selectionPattern = tabItem.Patterns.SelectionItem.Pattern;
                    if (selectionPattern != null && selectionPattern.IsSelected.Value)
                    {
                        Console.WriteLine($"[ChronoSettingsController] Selected tab: '{tabItem.Name}'");
                        return tabItem.Name ?? string.Empty;
                    }
                }

                Console.WriteLine("[ChronoSettingsController] No tab is currently selected");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoSettingsController] Error getting selected tab: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Selects the Paths tab (경로) in the SettingsDialog.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool SelectPathsTab()
        {
            return SelectTab(null, "Paths") || SelectTab(null, "경로");
        }

        /// <summary>
        /// Selects the Data Sequence tab (데이터 순서) in the SettingsDialog.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool SelectDataSequenceTab()
        {
            return SelectTab(null, "Data Sequence") || SelectTab(null, "데이터 순서");
        }

        /// <summary>
        /// Selects the UI Options tab (UI 옵션) in the SettingsDialog.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool SelectUiOptionsTab()
        {
            return SelectTab(null, "UI Options") || SelectTab(null, "UI 옵션");
        }

        /// <summary>
        /// Selects the Advanced tab (고급) in the SettingsDialog.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool SelectAdvancedTab()
        {
            return SelectTab(null, "Advanced") || SelectTab(null, "고급");
        }

        /// <summary>
        /// Selects the External Programs tab (외부 프로그램) in the SettingsDialog.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool SelectExternalProgramsTab()
        {
            return SelectTab(null, "External Programs") || SelectTab(null, "외부 프로그램");
        }

        /// <summary>
        /// Finds a path TextBox control in the SettingsDialog by its associated Label text.
        /// </summary>
        /// <remarks>
        /// In WPF, TextBox controls appear as ControlType.Edit in UI Automation.
        /// Label controls appear as ControlType.Text.
        /// This method searches for a Text element containing the labelText,
        /// then finds the sibling Edit control (typically the next child in the parent).
        ///
        /// The scopeSection parameter allows searching within a specific section
        /// (e.g., "Line 1", "Line 2") for disambiguating labels that appear multiple times.
        /// </remarks>
        /// <param name="dialog">The SettingsDialog window (optional, will find if null)</param>
        /// <param name="labelText">The label text to search for (e.g., "NIR 1 경로", "Camera1 Path")</param>
        /// <param name="scopeSection">Optional section to limit search (e.g., "Line 1", "Line 2")</param>
        /// <returns>The TextBox AutomationElement if found, null otherwise</returns>
        public AutomationElement? FindPathTextBox(Window? dialog, string labelText, string? scopeSection = null)
        {
            dialog ??= FindSettingsDialog();
            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot find TextBox: SettingsDialog not found");
                return null;
            }

            if (string.IsNullOrWhiteSpace(labelText))
            {
                Console.WriteLine("[ChronoSettingsController] Cannot find TextBox: labelText is null or empty");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                AutomationElement? searchScope = dialog;

                // If scopeSection is specified, find the section first
                if (!string.IsNullOrEmpty(scopeSection))
                {
                    var sectionTextElements = dialog.FindAllChildren(cf.ByControlType(ControlType.Text));
                    foreach (var textElement in sectionTextElements)
                    {
                        if (!string.IsNullOrEmpty(textElement.Name) &&
                            textElement.Name.IndexOf(scopeSection, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            // Found the section header, use its parent as the search scope
                            searchScope = textElement.Parent ?? dialog;
                            Console.WriteLine($"[ChronoSettingsController] Found section '{scopeSection}', limiting search to that section");
                            break;
                        }
                    }
                }

                // Find all Text elements (Labels) within the search scope
                var textElements = searchScope.FindAllChildren(cf.ByControlType(ControlType.Text));

                Console.WriteLine($"[ChronoSettingsController] Searching for label '{labelText}' in scope '{scopeSection ?? "(root)"}' among {textElements.Length} text elements");

                // Find the label TextBlock
                foreach (var textElement in textElements)
                {
                    if (!string.IsNullOrEmpty(textElement.Name) &&
                        textElement.Name.IndexOf(labelText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Found the label, now find the associated TextBox (Edit control)
                        // The TextBox is typically a sibling or descendant of the label's parent
                        var parent = textElement.Parent;
                        if (parent != null)
                        {
                            // Search for Edit control in the parent's children
                            var siblings = parent.FindAllChildren();
                            foreach (var sibling in siblings)
                            {
                                if (sibling.ControlType == ControlType.Edit)
                                {
                                    Console.WriteLine($"[ChronoSettingsController] Found TextBox for label '{labelText}' (Name: '{sibling.Name ?? "(empty)"}')");
                                    return sibling;
                                }
                            }
                        }

                        // If not found in siblings, search descendants
                        var editElements = parent?.FindAllChildren(cf.ByControlType(ControlType.Edit));
                        if (editElements != null && editElements.Length > 0)
                        {
                            Console.WriteLine($"[ChronoSettingsController] Found TextBox for label '{labelText}' in descendants");
                            return editElements[0];
                        }
                    }
                }

                Console.WriteLine($"[ChronoSettingsController] TextBox for label '{labelText}' not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoSettingsController] Error finding TextBox for label '{labelText}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Reads the current text value from a path TextBox in the SettingsDialog.
        /// </summary>
        /// <remarks>
        /// Uses ValuePattern.ValueProperty if available (standard for Edit controls).
        /// Falls back to element.Name if ValuePattern is not supported.
        /// </remarks>
        /// <param name="dialog">The SettingsDialog window (optional, will find if null)</param>
        /// <param name="labelText">The label text to search for</param>
        /// <param name="scopeSection">Optional section to limit search (e.g., "Line 1", "Line 2")</param>
        /// <returns>The current text value, or empty string if error</returns>
        public string GetPathTextBoxValue(Window? dialog, string labelText, string? scopeSection = null)
        {
            var textBox = FindPathTextBox(dialog, labelText, scopeSection);
            if (textBox == null)
            {
                return string.Empty;
            }

            try
            {
                // Try ValuePattern first (standard for TextBox/Edit controls)
                var valuePattern = textBox.Patterns.Value.Pattern;
                if (valuePattern != null)
                {
                    var value = valuePattern.Value;
                    Console.WriteLine($"[ChronoSettingsController] TextBox value (via ValuePattern): '{value}'");
                    return value;
                }

                // Fallback to Name property
                var name = textBox.Name ?? string.Empty;
                Console.WriteLine($"[ChronoSettingsController] TextBox value (via Name): '{name}'");
                return name;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoSettingsController] Error getting TextBox value: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Sets the text value in a path TextBox in the SettingsDialog.
        /// </summary>
        /// <remarks>
        /// Uses ValuePattern.SetValue() to set the text content.
        /// </remarks>
        /// <param name="dialog">The SettingsDialog window (optional, will find if null)</param>
        /// <param name="labelText">The label text to search for</param>
        /// <param name="value">The new value to set</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetPathTextBoxValue(Window? dialog, string labelText, string value)
        {
            var textBox = FindPathTextBox(dialog, labelText);
            if (textBox == null)
            {
                Console.WriteLine($"[ChronoSettingsController] Cannot set value: TextBox for label '{labelText}' not found");
                return false;
            }

            try
            {
                var valuePattern = textBox.Patterns.Value.Pattern;
                if (valuePattern == null)
                {
                    Console.WriteLine("[ChronoSettingsController] Cannot set TextBox value: ValuePattern not supported");
                    return false;
                }

                var oldValue = GetPathTextBoxValue(dialog, labelText);
                valuePattern.SetValue(value);
                Console.WriteLine($"[ChronoSettingsController] TextBox value set: '{oldValue}' -> '{value}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoSettingsController] Error setting TextBox value: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads all Line 1 paths from the SettingsDialog Paths tab.
        /// </summary>
        /// <remarks>
        /// Returns a dictionary with keys: nir1, normal1, cam1, cam2, cam3.
        /// Uses label text from SettingsDialog.xaml (Korean primary, English fallback).
        /// </remarks>
        /// <returns>Dictionary of path type to value, or empty dictionary if dialog not found</returns>
        public Dictionary<string, string> GetLine1Paths()
        {
            var paths = new Dictionary<string, string>();
            var dialog = FindSettingsDialog();

            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot get Line 1 paths: SettingsDialog not found");
                return paths;
            }

            // Ensure we're on the Paths tab
            if (!SelectPathsTab())
            {
                Console.WriteLine("[ChronoSettingsController] Cannot get Line 1 paths: Failed to select Paths tab");
                return paths;
            }

            // Label text mappings (Korean primary, English fallback)
            // From SettingsDialog.xaml
            var nir1Path = GetPathTextBoxValue(dialog, "NIR 1 경로") ?? GetPathTextBoxValue(dialog, "NIR1 Path");
            if (!string.IsNullOrEmpty(nir1Path)) paths["nir1"] = nir1Path;

            var normal1Path = GetPathTextBoxValue(dialog, "일반 1 경로") ?? GetPathTextBoxValue(dialog, "Normal1 Path");
            if (!string.IsNullOrEmpty(normal1Path)) paths["normal1"] = normal1Path;

            var cam1Path = GetPathTextBoxValue(dialog, "카메라 1 경로") ?? GetPathTextBoxValue(dialog, "Camera1 Path");
            if (!string.IsNullOrEmpty(cam1Path)) paths["cam1"] = cam1Path;

            var cam2Path = GetPathTextBoxValue(dialog, "카메라 2 경로") ?? GetPathTextBoxValue(dialog, "Camera2 Path");
            if (!string.IsNullOrEmpty(cam2Path)) paths["cam2"] = cam2Path;

            var cam3Path = GetPathTextBoxValue(dialog, "카메라 3 경로") ?? GetPathTextBoxValue(dialog, "Camera3 Path");
            if (!string.IsNullOrEmpty(cam3Path)) paths["cam3"] = cam3Path;

            Console.WriteLine($"[ChronoSettingsController] Got {paths.Count} Line 1 paths");
            return paths;
        }

        /// <summary>
        /// Reads all Line 2 paths from the SettingsDialog Paths tab.
        /// </summary>
        /// <remarks>
        /// Returns a dictionary with keys: nir2, normal2, cam4, cam5, cam6.
        /// Uses label text from SettingsDialog.xaml (Korean primary, English fallback).
        /// </remarks>
        /// <returns>Dictionary of path type to value, or empty dictionary if dialog not found</returns>
        public Dictionary<string, string> GetLine2Paths()
        {
            var paths = new Dictionary<string, string>();
            var dialog = FindSettingsDialog();

            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot get Line 2 paths: SettingsDialog not found");
                return paths;
            }

            // Ensure we're on the Paths tab
            if (!SelectPathsTab())
            {
                Console.WriteLine("[ChronoSettingsController] Cannot get Line 2 paths: Failed to select Paths tab");
                return paths;
            }

            // Label text mappings (Korean primary, English fallback)
            // Line 2 section has "Line 2" header for scoping
            var nir2Path = GetPathTextBoxValue(dialog, "NIR 2 경로", "Line 2") ?? GetPathTextBoxValue(dialog, "NIR2 Path", "Line 2");
            if (!string.IsNullOrEmpty(nir2Path)) paths["nir2"] = nir2Path;

            var normal2Path = GetPathTextBoxValue(dialog, "일반 2 경로", "Line 2") ?? GetPathTextBoxValue(dialog, "Normal2 Path", "Line 2");
            if (!string.IsNullOrEmpty(normal2Path)) paths["normal2"] = normal2Path;

            var cam4Path = GetPathTextBoxValue(dialog, "카메라 4 경로", "Line 2") ?? GetPathTextBoxValue(dialog, "Camera4 Path", "Line 2");
            if (!string.IsNullOrEmpty(cam4Path)) paths["cam4"] = cam4Path;

            var cam5Path = GetPathTextBoxValue(dialog, "카메라 5 경로", "Line 2") ?? GetPathTextBoxValue(dialog, "Camera5 Path", "Line 2");
            if (!string.IsNullOrEmpty(cam5Path)) paths["cam5"] = cam5Path;

            var cam6Path = GetPathTextBoxValue(dialog, "카메라 6 경로", "Line 2") ?? GetPathTextBoxValue(dialog, "Camera6 Path", "Line 2");
            if (!string.IsNullOrEmpty(cam6Path)) paths["cam6"] = cam6Path;

            Console.WriteLine($"[ChronoSettingsController] Got {paths.Count} Line 2 paths");
            return paths;
        }

        /// <summary>
        /// Reads the output path from the SettingsDialog Paths tab.
        /// </summary>
        /// <remarks>
        /// Uses label text from SettingsDialog.xaml (Korean primary, English fallback).
        /// </remarks>
        /// <returns>The output path value, or empty string if not found</returns>
        public string GetOutputPath()
        {
            var dialog = FindSettingsDialog();
            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot get output path: SettingsDialog not found");
                return string.Empty;
            }

            // Ensure we're on the Paths tab
            if (!SelectPathsTab())
            {
                Console.WriteLine("[ChronoSettingsController] Cannot get output path: Failed to select Paths tab");
                return string.Empty;
            }

            return GetPathTextBoxValue(dialog, "출력 경로") ?? GetPathTextBoxValue(dialog, "Output Path") ?? string.Empty;
        }

        /// <summary>
        /// Reads the quarantine/delete path from the SettingsDialog Paths tab.
        /// </summary>
        /// <remarks>
        /// Uses label text from SettingsDialog.xaml (Korean primary, English fallback).
        /// </remarks>
        /// <returns>The quarantine path value, or empty string if not found</returns>
        public string GetQuarantinePath()
        {
            var dialog = FindSettingsDialog();
            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot get quarantine path: SettingsDialog not found");
                return string.Empty;
            }

            // Ensure we're on the Paths tab
            if (!SelectPathsTab())
            {
                Console.WriteLine("[ChronoSettingsController] Cannot get quarantine path: Failed to select Paths tab");
                return string.Empty;
            }

            return GetPathTextBoxValue(dialog, "삭제 격리 경로") ?? GetPathTextBoxValue(dialog, "Delete Quarantine Path") ?? string.Empty;
        }

        /// <summary>
        /// Sets a specific Line 1 path value.
        /// </summary>
        /// <param name="pathKey">The path key: "nir1", "normal1", "cam1", "cam2", or "cam3"</param>
        /// <param name="value">The new path value to set</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetLine1Path(string pathKey, string value)
        {
            var dialog = FindSettingsDialog();
            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot set Line 1 path: SettingsDialog not found");
                return false;
            }

            // Ensure we're on the Paths tab
            if (!SelectPathsTab())
            {
                Console.WriteLine("[ChronoSettingsController] Cannot set Line 1 path: Failed to select Paths tab");
                return false;
            }

            string? labelText = pathKey.ToLowerInvariant() switch
            {
                "nir1" => "NIR 1 경로",
                "normal1" => "일반 1 경로",
                "cam1" => "카메라 1 경로",
                "cam2" => "카메라 2 경로",
                "cam3" => "카메라 3 경로",
                _ => null
            };

            if (labelText == null)
            {
                Console.WriteLine($"[ChronoSettingsController] Unknown Line 1 path key: {pathKey}");
                return false;
            }

            // Try Korean first, then English fallback
            return SetPathTextBoxValue(dialog, labelText, value) ||
                   SetPathTextBoxValue(dialog, GetEnglishLabelForPathKey(pathKey), value);
        }

        /// <summary>
        /// Sets a specific Line 2 path value.
        /// </summary>
        /// <param name="pathKey">The path key: "nir2", "normal2", "cam4", "cam5", or "cam6"</param>
        /// <param name="value">The new path value to set</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetLine2Path(string pathKey, string value)
        {
            var dialog = FindSettingsDialog();
            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot set Line 2 path: SettingsDialog not found");
                return false;
            }

            // Ensure we're on the Paths tab
            if (!SelectPathsTab())
            {
                Console.WriteLine("[ChronoSettingsController] Cannot set Line 2 path: Failed to select Paths tab");
                return false;
            }

            string? labelText = pathKey.ToLowerInvariant() switch
            {
                "nir2" => "NIR 2 경로",
                "normal2" => "일반 2 경로",
                "cam4" => "카메라 4 경로",
                "cam5" => "카메라 5 경로",
                "cam6" => "카메라 6 경로",
                _ => null
            };

            if (labelText == null)
            {
                Console.WriteLine($"[ChronoSettingsController] Unknown Line 2 path key: {pathKey}");
                return false;
            }

            // Try Korean first with Line 2 scope, then English fallback
            return SetPathTextBoxValue(dialog, labelText, value) ||
                   SetPathTextBoxValue(dialog, GetEnglishLabelForPathKey(pathKey), value);
        }

        /// <summary>
        /// Helper to get English label text for a path key.
        /// </summary>
        /// <param name="pathKey">The path key</param>
        /// <returns>English label text, or empty string if not found</returns>
        private string GetEnglishLabelForPathKey(string pathKey)
        {
            return pathKey.ToLowerInvariant() switch
            {
                "nir1" => "NIR1 Path",
                "normal1" => "Normal1 Path",
                "cam1" => "Camera1 Path",
                "cam2" => "Camera2 Path",
                "cam3" => "Camera3 Path",
                "nir2" => "NIR2 Path",
                "normal2" => "Normal2 Path",
                "cam4" => "Camera4 Path",
                "cam5" => "Camera5 Path",
                "cam6" => "Camera6 Path",
                "output" => "Output Path",
                "quarantine" => "Delete Quarantine Path",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Gets the names of all available tabs.
        /// </summary>
        /// <param name="tabItems">Array of TabItem elements</param>
        /// <returns>List of tab names</returns>
        private List<string> GetAvailableTabNames(AutomationElement[] tabItems)
        {
            var names = new List<string>();
            foreach (var tab in tabItems)
            {
                if (!string.IsNullOrEmpty(tab.Name))
                {
                    names.Add(tab.Name);
                }
            }
            return names;
        }

        #region CheckBox Automation

        /// <summary>
        /// Finds a CheckBox within the SettingsDialog by label text or AutomationId.
        /// </summary>
        /// <param name="dialog">The SettingsDialog window (optional, will find if null)</param>
        /// <param name="labelText">The label text or AutomationId to search for</param>
        /// <param name="tabName">Optional tab name to select first (e.g., "Advanced", "UI Options")</param>
        /// <returns>The first matching CheckBox element or null if not found</returns>
        /// <remarks>
        /// CheckBox detection in WPF via UI Automation:
        /// - ControlType.CheckBox (if explicitly set) OR ControlType.Button with TogglePattern
        /// - Supports bilingual labels (Korean primary, English fallback)
        /// </remarks>
        public AutomationElement? FindCheckBox(Window? dialog, string labelText, string? tabName = null)
        {
            dialog ??= FindSettingsDialog();
            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot find CheckBox: SettingsDialog not found");
                return null;
            }

            // Select tab if specified
            if (!string.IsNullOrEmpty(tabName))
            {
                SelectTab(dialog, tabName);
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // First try to find by AutomationId (exact match)
                var automationIdCondition = cf.ByAutomationId(labelText);
                var byIdElement = dialog.FindFirstDescendant(automationIdCondition);
                if (byIdElement != null && IsCheckBox(byIdElement))
                {
                    Console.WriteLine($"[ChronoSettingsController] Found CheckBox by AutomationId: '{labelText}'");
                    return byIdElement;
                }

                // Search for CheckBox with matching Name (content)
                var allCheckBoxes = dialog.FindAllChildren(cf.ByControlType(ControlType.CheckBox));
                foreach (var checkBox in allCheckBoxes)
                {
                    if (!string.IsNullOrEmpty(checkBox.Name) &&
                        checkBox.Name.IndexOf(labelText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"[ChronoSettingsController] Found CheckBox by Name: '{checkBox.Name}'");
                        return checkBox;
                    }
                }

                // Also check for Button with TogglePattern (some WPF CheckBoxes appear as Button)
                var allButtons = dialog.FindAllChildren(cf.ByControlType(ControlType.Button));
                foreach (var button in allButtons)
                {
                    if (button.Patterns.Toggle.Pattern != null &&
                        !string.IsNullOrEmpty(button.Name) &&
                        button.Name.IndexOf(labelText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"[ChronoSettingsController] Found CheckBox (as Button with TogglePattern) by Name: '{button.Name}'");
                        return button;
                    }
                }

                Console.WriteLine($"[ChronoSettingsController] CheckBox not found: '{labelText}'");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoSettingsController] Error finding CheckBox '{labelText}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the current checked state of a CheckBox.
        /// </summary>
        /// <param name="dialog">The SettingsDialog window (optional, will find if null)</param>
        /// <param name="labelText">The label text or AutomationId to search for</param>
        /// <param name="tabName">Optional tab name to select first</param>
        /// <returns>True if checked, false if unchecked or not found</returns>
        /// <remarks>
        /// Uses TogglePattern.ToggleState if supported, otherwise checks IsOffscreen property.
        /// ToggleState: On (checked), Off (unchecked)
        /// </remarks>
        public bool GetCheckBoxState(Window? dialog, string labelText, string? tabName = null)
        {
            var checkBox = FindCheckBox(dialog, labelText, tabName);
            if (checkBox == null)
            {
                return false;
            }

            try
            {
                var togglePattern = checkBox.Patterns.Toggle.Pattern;
                if (togglePattern != null)
                {
                    var state = togglePattern.ToggleState.Value;
                    bool isChecked = state == ToggleState.On;
                    Console.WriteLine($"[ChronoSettingsController] CheckBox '{labelText}' state: {(isChecked ? "Checked" : "Unchecked")}");
                    return isChecked;
                }

                // Fallback: IsOffscreen property often indicates checked state in WPF CheckBoxes
                bool isCheckedByOffscreen = !checkBox.IsOffscreen;
                Console.WriteLine($"[ChronoSettingsController] CheckBox '{labelText}' state (fallback): {(isCheckedByOffscreen ? "Checked" : "Unchecked")}");
                return isCheckedByOffscreen;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoSettingsController] Error getting CheckBox state for '{labelText}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets the checked state of a CheckBox.
        /// </summary>
        /// <param name="dialog">The SettingsDialog window (optional, will find if null)</param>
        /// <param name="labelText">The label text or AutomationId to search for</param>
        /// <param name="isChecked">The desired checked state</param>
        /// <param name="tabName">Optional tab name to select first</param>
        /// <returns>True if the state was set successfully, false otherwise</returns>
        /// <remarks>
        /// Uses TogglePattern.Toggle() if current state doesn't match target.
        /// </remarks>
        public bool SetCheckBoxState(Window? dialog, string labelText, bool isChecked, string? tabName = null)
        {
            dialog ??= FindSettingsDialog();
            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot set CheckBox state: SettingsDialog not found");
                return false;
            }

            var checkBox = FindCheckBox(dialog, labelText, tabName);
            if (checkBox == null)
            {
                Console.WriteLine($"[ChronoSettingsController] Cannot set CheckBox state: CheckBox '{labelText}' not found");
                return false;
            }

            try
            {
                var togglePattern = checkBox.Patterns.Toggle.Pattern;
                if (togglePattern == null)
                {
                    Console.WriteLine($"[ChronoSettingsController] CheckBox '{labelText}' does not support TogglePattern");
                    return false;
                }

                bool currentState = togglePattern.ToggleState.Value == ToggleState.On;

                if (currentState == isChecked)
                {
                    Console.WriteLine($"[ChronoSettingsController] CheckBox '{labelText}' already in desired state: {(isChecked ? "Checked" : "Unchecked")}");
                    return true;
                }

                Console.WriteLine($"[ChronoSettingsController] Toggling CheckBox '{labelText}' to: {(isChecked ? "Checked" : "Unchecked")}");
                togglePattern.Toggle();

                // Brief wait for state change
                Thread.Sleep(100);
                Console.WriteLine($"[ChronoSettingsController] Successfully toggled CheckBox '{labelText}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoSettingsController] Error setting CheckBox state for '{labelText}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all Advanced tab CheckBox states as a dictionary.
        /// </summary>
        /// <param name="dialog">The SettingsDialog window (optional, will find if null)</param>
        /// <returns>Dictionary of setting names to boolean states</returns>
        /// <remarks>
        /// Supported settings:
        /// - use_folder_suffix (UseFolderSuffix)
        /// - use_camera_subfolder_normal (UseCameraSubfolderNormal)
        /// - use_camera_subfolder_normal2 (UseCameraSubfolderNormal2)
        /// - use_disk_cache (UseDiskCache)
        /// - use_line_specific_group_id (UseLineSpecificGroupId)
        /// - enable_nir_graph (EnableNirGraph)
        /// - show_tooltips (ShowTooltips)
        /// - compare_to_reference_camera (CompareToReferenceCamera)
        /// </remarks>
        public Dictionary<string, bool> GetAdvancedSettings(Window? dialog = null)
        {
            var settings = new Dictionary<string, bool>();

            dialog ??= FindSettingsDialog();
            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot get advanced settings: SettingsDialog not found");
                return settings;
            }

            // Advanced tab CheckBoxes
            settings["use_folder_suffix"] = GetCheckBoxState(dialog, "UseFolderSuffix", "Advanced");
            settings["use_camera_subfolder_normal"] = GetCheckBoxState(dialog, "UseCameraSubfolderNormal", "Advanced");
            settings["use_camera_subfolder_normal2"] = GetCheckBoxState(dialog, "UseCameraSubfolderNormal2", "Advanced");
            settings["use_disk_cache"] = GetCheckBoxState(dialog, "UseDiskCache", "Advanced");
            settings["use_line_specific_group_id"] = GetCheckBoxState(dialog, "UseLineSpecificGroupId", "Advanced");

            // UI Options tab CheckBoxes
            settings["enable_nir_graph"] = GetCheckBoxState(dialog, "EnableNirGraph", "UI Options");
            settings["show_tooltips"] = GetCheckBoxState(dialog, "ShowTooltips", "UI Options");

            // Data Sequence tab CheckBox
            settings["compare_to_reference_camera"] = GetCheckBoxState(dialog, "CompareToReferenceCamera", "Data Sequence");

            return settings;
        }

        /// <summary>
        /// Sets a specific advanced setting by name.
        /// </summary>
        /// <param name="settingName">The setting name (e.g., "use_folder_suffix")</param>
        /// <param name="value">The desired boolean value</param>
        /// <param name="dialog">The SettingsDialog window (optional, will find if null)</param>
        /// <returns>True if the setting was set successfully, false otherwise</returns>
        /// <remarks>
        /// Supported setting names: use_folder_suffix, use_camera_subfolder_normal,
        /// use_camera_subfolder_normal2, use_disk_cache, use_line_specific_group_id,
        /// enable_nir_graph, show_tooltips, compare_to_reference_camera
        /// </remarks>
        public bool SetAdvancedSetting(string settingName, bool value, Window? dialog = null)
        {
            // Map setting names to their AutomationId/labels and tab names
            var (automationId, tabName) = settingName.ToLowerInvariant() switch
            {
                "use_folder_suffix" => ("UseFolderSuffix", "Advanced"),
                "use_camera_subfolder_normal" => ("UseCameraSubfolderNormal", "Advanced"),
                "use_camera_subfolder_normal2" => ("UseCameraSubfolderNormal2", "Advanced"),
                "use_disk_cache" => ("UseDiskCache", "Advanced"),
                "use_line_specific_group_id" => ("UseLineSpecificGroupId", "Advanced"),
                "enable_nir_graph" => ("EnableNirGraph", "UI Options"),
                "show_tooltips" => ("ShowTooltips", "UI Options"),
                "compare_to_reference_camera" => ("CompareToReferenceCamera", "Data Sequence"),
                _ => (null, null)
            };

            if (automationId == null)
            {
                Console.WriteLine($"[ChronoSettingsController] Unknown setting name: '{settingName}'");
                return false;
            }

            return SetCheckBoxState(dialog, automationId, value, tabName);
        }

        /// <summary>
        /// Checks if an element is a CheckBox (either ControlType.CheckBox or Button with TogglePattern).
        /// </summary>
        private bool IsCheckBox(AutomationElement element)
        {
            if (element.ControlType == ControlType.CheckBox)
            {
                return true;
            }

            // Check for Button with TogglePattern (WPF CheckBoxes often appear as Button)
            if (element.ControlType == ControlType.Button)
            {
                return element.Patterns.Toggle.Pattern != null;
            }

            return false;
        }

        #endregion

        #region Dialog Action Buttons

        private const int DefaultDialogWaitMs = 3000;

        /// <summary>
        /// Clicks the Save/OK button and waits for the dialog to close.
        /// </summary>
        /// <param name="dialog">The SettingsDialog window (optional, will find if null)</param>
        /// <param name="timeoutMs">Maximum time to wait for the dialog to close (default: 3000)</param>
        /// <returns>True if the button was clicked and dialog closed successfully, false otherwise</returns>
        /// <remarks>
        /// Finds the "확인" (OK) or "Save" button, clicks it using InvokePattern,
        /// and waits for the dialog to close.
        /// </remarks>
        public bool ClickSaveButton(Window? dialog = null, int timeoutMs = DefaultDialogWaitMs)
        {
            dialog ??= FindSettingsDialog();
            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot click Save button: SettingsDialog not found");
                return false;
            }

            Console.WriteLine("[ChronoSettingsController] Attempting to click Save/OK button");

            // Try Korean text first (확인), then English (OK, Save)
            var saveButton = FindButton(dialog, "확인") ??
                            FindButton(dialog, "OK") ??
                            FindButton(dialog, "Save");

            if (saveButton == null)
            {
                Console.WriteLine("[ChronoSettingsController] Save/OK button not found in SettingsDialog");
                return false;
            }

            if (!ClickButton(saveButton))
            {
                Console.WriteLine("[ChronoSettingsController] Failed to click Save/OK button");
                return false;
            }

            // Wait for dialog to close
            bool closed = _windowFinder.WaitForWindowToClose("Settings", timeoutMs);
            if (!closed)
            {
                closed = _windowFinder.WaitForWindowToClose("설정", timeoutMs);
            }

            if (closed)
            {
                Console.WriteLine("[ChronoSettingsController] Save/OK button clicked, dialog closed successfully");
                return true;
            }

            Console.WriteLine("[ChronoSettingsController] Save/OK button clicked, but dialog did not close");
            return false;
        }

        /// <summary>
        /// Clicks the Apply button (dialog stays open).
        /// </summary>
        /// <param name="dialog">The SettingsDialog window (optional, will find if null)</param>
        /// <returns>True if the button was clicked successfully, false otherwise</returns>
        /// <remarks>
        /// Finds the "적용" (Apply) button and clicks it using InvokePattern.
        /// Does NOT wait for the dialog to close since Apply keeps the dialog open.
        /// </remarks>
        public bool ClickApplyButton(Window? dialog = null)
        {
            dialog ??= FindSettingsDialog();
            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot click Apply button: SettingsDialog not found");
                return false;
            }

            Console.WriteLine("[ChronoSettingsController] Attempting to click Apply button");

            var applyButton = FindButton(dialog, "적용") ?? FindButton(dialog, "Apply");

            if (applyButton == null)
            {
                Console.WriteLine("[ChronoSettingsController] Apply button not found in SettingsDialog");
                return false;
            }

            if (ClickButton(applyButton))
            {
                Console.WriteLine("[ChronoSettingsController] Apply button clicked successfully (dialog remains open)");
                return true;
            }

            Console.WriteLine("[ChronoSettingsController] Failed to click Apply button");
            return false;
        }

        /// <summary>
        /// Clicks the Cancel button and waits for the dialog to close.
        /// </summary>
        /// <param name="dialog">The SettingsDialog window (optional, will find if null)</param>
        /// <param name="timeoutMs">Maximum time to wait for the dialog to close (default: 3000)</param>
        /// <returns>True if the button was clicked and dialog closed successfully, false otherwise</returns>
        /// <remarks>
        /// Finds the "취소" (Cancel) button, clicks it using InvokePattern,
        /// and waits for the dialog to close.
        /// </remarks>
        public bool ClickCancelButton(Window? dialog = null, int timeoutMs = DefaultDialogWaitMs)
        {
            dialog ??= FindSettingsDialog();
            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot click Cancel button: SettingsDialog not found");
                return false;
            }

            Console.WriteLine("[ChronoSettingsController] Attempting to click Cancel button");

            var cancelButton = FindButton(dialog, "취소") ?? FindButton(dialog, "Cancel");

            if (cancelButton == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cancel button not found in SettingsDialog");
                return false;
            }

            if (!ClickButton(cancelButton))
            {
                Console.WriteLine("[ChronoSettingsController] Failed to click Cancel button");
                return false;
            }

            // Wait for dialog to close
            bool closed = _windowFinder.WaitForWindowToClose("Settings", timeoutMs);
            if (!closed)
            {
                closed = _windowFinder.WaitForWindowToClose("설정", timeoutMs);
            }

            if (closed)
            {
                Console.WriteLine("[ChronoSettingsController] Cancel button clicked, dialog closed successfully");
                return true;
            }

            Console.WriteLine("[ChronoSettingsController] Cancel button clicked, but dialog did not close");
            return false;
        }

        /// <summary>
        /// Clicks the Reset/Defaults button (dialog stays open).
        /// </summary>
        /// <param name="dialog">The SettingsDialog window (optional, will find if null)</param>
        /// <returns>True if the button was clicked successfully, false otherwise</returns>
        /// <remarks>
        /// Finds the "기본값" (Defaults) button and clicks it using InvokePattern.
        /// Does NOT wait for the dialog to close since Reset keeps the dialog open.
        /// </remarks>
        public bool ClickResetButton(Window? dialog = null)
        {
            dialog ??= FindSettingsDialog();
            if (dialog == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot click Reset button: SettingsDialog not found");
                return false;
            }

            Console.WriteLine("[ChronoSettingsController] Attempting to click Reset/Defaults button");

            var resetButton = FindButton(dialog, "기본값") ??
                             FindButton(dialog, "Defaults") ??
                             FindButton(dialog, "Reset");

            if (resetButton == null)
            {
                Console.WriteLine("[ChronoSettingsController] Reset/Defaults button not found in SettingsDialog");
                return false;
            }

            if (ClickButton(resetButton))
            {
                Console.WriteLine("[ChronoSettingsController] Reset/Defaults button clicked successfully (dialog remains open)");
                return true;
            }

            Console.WriteLine("[ChronoSettingsController] Failed to click Reset/Defaults button");
            return false;
        }

        /// <summary>
        /// Clicks the Save/OK button and waits for the dialog to close.
        /// </summary>
        /// <param name="dialog">The SettingsDialog window (optional, will find if null)</param>
        /// <param name="timeoutMs">Maximum time to wait for the dialog to close (default: 3000)</param>
        /// <returns>True if the dialog was saved and closed successfully, false otherwise</returns>
        /// <remarks>
        /// Convenience method that combines finding the dialog, clicking Save, and waiting for close.
        /// </remarks>
        public bool SaveAndClose(Window? dialog = null, int timeoutMs = DefaultDialogWaitMs)
        {
            return ClickSaveButton(dialog, timeoutMs);
        }

        /// <summary>
        /// Clicks the Cancel button and waits for the dialog to close.
        /// </summary>
        /// <param name="dialog">The SettingsDialog window (optional, will find if null)</param>
        /// <param name="timeoutMs">Maximum time to wait for the dialog to close (default: 3000)</param>
        /// <returns>True if the dialog was cancelled and closed successfully, false otherwise</returns>
        /// <remarks>
        /// Convenience method that combines finding the dialog, clicking Cancel, and waiting for close.
        /// </remarks>
        public bool CancelAndClose(Window? dialog = null, int timeoutMs = DefaultDialogWaitMs)
        {
            return ClickCancelButton(dialog, timeoutMs);
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
