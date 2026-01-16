using System;
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
    ///
    /// SettingsDialog buttons have bilingual text labels:
    /// - "Save" or "저장"
    /// - "Cancel" or "취소"
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
        /// Finds the MainWindow, locates the Settings button (text "설정" or "Settings"),
        /// clicks it using InvokePattern, and waits for the SettingsDialog to appear.
        /// </remarks>
        /// <param name="mainWindow">The MainWindow to search within (optional, will find if null)</param>
        /// <param name="timeoutMs">Maximum time to wait for the dialog to appear in milliseconds (default: 5000)</param>
        /// <returns>True if the dialog was opened successfully, false otherwise</returns>
        public bool OpenSettingsDialog(Window? mainWindow = null, int timeoutMs = 5000)
        {
            mainWindow ??= FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoSettingsController] Cannot open SettingsDialog: MainWindow not found");
                return false;
            }

            Console.WriteLine("[ChronoSettingsController] Attempting to open SettingsDialog via Settings button");

            // Find Settings button in toolbar
            var settingsButton = FindToolbarButton(mainWindow, "설정");
            if (settingsButton == null)
            {
                // Try English button text
                settingsButton = FindToolbarButton(mainWindow, "Settings");
            }

            if (settingsButton == null)
            {
                Console.WriteLine("[ChronoSettingsController] Settings button not found in toolbar");
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
        /// Releases resources used by the UIA3 automation.
        /// </summary>
        public void Dispose()
        {
            _automation?.Dispose();
        }
    }
}
