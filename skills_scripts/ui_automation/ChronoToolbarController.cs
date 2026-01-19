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
    /// High-level API for ChronoView toolbar automation.
    /// Consolidates all toolbar interaction capabilities into a cohesive, testable class.
    /// </summary>
    /// <remarks>
    /// This class provides a complete API for toolbar interactions including:
    /// - Finding the MainWindow via ChronoWindowFinder
    /// - Finding toolbar buttons by text
    /// - Clicking buttons
    /// - Checking button enabled state
    /// - Waiting for button state changes
    ///
    /// Toolbar buttons have Korean text labels:
    /// - "시작" (Start)
    /// - "중지" (Stop)
    /// - "설정" (Settings)
    /// - "새로고침" (Refresh)
    /// - "이동" (Move)
    /// - "삭제" (Delete)
    /// </remarks>
    public class ChronoToolbarController : IDisposable
    {
        private readonly UIA3Automation _automation;
        private readonly ChronoWindowFinder _windowFinder;
        private const int DefaultPollIntervalMs = 200;

        /// <summary>
        /// Initializes a new instance of the ChronoToolbarController class.
        /// </summary>
        /// <param name="automation">The UIA3Automation instance to use for UI automation</param>
        public ChronoToolbarController(UIA3Automation automation)
        {
            _automation = automation ?? throw new ArgumentNullException(nameof(automation));
            _windowFinder = new ChronoWindowFinder(automation);
        }

        /// <summary>
        /// Initializes a new instance of the ChronoToolbarController class,
        /// creating its own UIA3Automation instance.
        /// </summary>
        public ChronoToolbarController() : this(new UIA3Automation())
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
        /// Finds a toolbar button within a window by its button text.
        /// Searches in ToolBar first, then falls back to searching the entire window.
        /// </summary>
        /// <param name="mainWindow">The Window element to search within</param>
        /// <param name="buttonText">The button text to search for (e.g., "시작", "중지")</param>
        /// <returns>The first matching Button element or null if not found</returns>
        public AutomationElement? FindToolbarButton(Window? mainWindow, string buttonText)
        {
            if (mainWindow == null)
            {
                Console.WriteLine($"[ChronoToolbarController] Cannot find button: mainWindow is null");
                return null;
            }

            if (string.IsNullOrWhiteSpace(buttonText))
            {
                Console.WriteLine($"[ChronoToolbarController] Cannot find button: buttonText is null or empty");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // First, try to find ToolBar and search within it
                var toolBars = mainWindow.FindAllChildren(cf.ByControlType(ControlType.ToolBar));
                if (toolBars.Length > 0)
                {
                    Console.WriteLine($"[ChronoToolbarController] Found {toolBars.Length} ToolBar(s), searching within...");
                    foreach (var toolBar in toolBars)
                    {
                        var toolBarButtons = toolBar.FindAllChildren(cf.ByControlType(ControlType.Button));
                        Console.WriteLine($"[ChronoToolbarController] ToolBar has {toolBarButtons.Length} buttons");
                        foreach (var button in toolBarButtons)
                        {
                            // Try button.Name first
                            if (!string.IsNullOrEmpty(button.Name) &&
                                button.Name.IndexOf(buttonText, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                Console.WriteLine($"[ChronoToolbarController] Found button by Name in ToolBar: '{button.Name}'");
                                return button;
                            }

                            // Try TextBlock children
                            var textBlocks = button.FindAllChildren(cf.ByControlType(ControlType.Text));
                            foreach (var textBlock in textBlocks)
                            {
                                if (!string.IsNullOrEmpty(textBlock.Name) &&
                                    textBlock.Name.IndexOf(buttonText, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    Console.WriteLine($"[ChronoToolbarController] Found button by child Text in ToolBar: '{textBlock.Name}'");
                                    return button;
                                }
                            }
                        }
                    }
                }

                // Fallback: search all buttons in window
                var buttonCondition = cf.ByControlType(ControlType.Button);
                var buttons = mainWindow.FindAllChildren(buttonCondition);

                Console.WriteLine($"[ChronoToolbarController] Searching for button containing '{buttonText}' among {buttons.Length} buttons in window");

                int buttonIndex = 0;
                foreach (var button in buttons)
                {
                    // Debug: Print button properties
                    var bounds = button.BoundingRectangle;
                    var className = button.ClassName;
                    var automationId = button.AutomationId;
                    var isEnabled = button.IsEnabled;
                    var isOffscreen = button.IsOffscreen;

                    Console.WriteLine($"[ChronoToolbarController] Button {buttonIndex}:");
                    Console.WriteLine($"  - Name: '{button.Name ?? "(null)"}'");
                    Console.WriteLine($"  - ClassName: '{className}'");
                    Console.WriteLine($"  - AutomationId: '{automationId ?? "(null)"}'");
                    Console.WriteLine($"  - Bounds: {bounds}");
                    Console.WriteLine($"  - IsEnabled: {isEnabled}, IsOffscreen: {isOffscreen}");

                    // For WPF, try to get the text via LegacyIAccessible
                    try
                    {
                        var legacyPattern = button.Patterns.LegacyIAccessible.Pattern;
                        if (legacyPattern != null)
                        {
                            var value = legacyPattern.Value.Value;
                            Console.WriteLine($"  - LegacyIAccessible.Value: '{value ?? "(null)"}'");
                        }
                    }
                    catch { }

                    // Try button.Name first
                    if (!string.IsNullOrEmpty(button.Name) &&
                        button.Name.IndexOf(buttonText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"[ChronoToolbarController] Found button by Name: '{button.Name}'");
                        return button;
                    }

                    // Try finding TextBlock children (recursive)
                    var textBlocks = button.FindAllChildren(cf.ByControlType(ControlType.Text));
                    foreach (var textBlock in textBlocks)
                    {
                        if (!string.IsNullOrEmpty(textBlock.Name) &&
                            textBlock.Name.IndexOf(buttonText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Console.WriteLine($"[ChronoToolbarController] Found button by child Text: '{textBlock.Name}'");
                            return button;
                        }
                    }

                    // Try all descendant elements
                    var allDescendants = button.FindAllChildren();
                    foreach (var descendant in allDescendants)
                    {
                        if (!string.IsNullOrEmpty(descendant.Name) &&
                            descendant.Name.IndexOf(buttonText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Console.WriteLine($"[ChronoToolbarController] Found button by descendant '{descendant.Name}' (ControlType: {descendant.ControlType})");
                            return button;
                        }
                    }

                    buttonIndex++;
                }

                Console.WriteLine($"[ChronoToolbarController] No button found containing '{buttonText}'");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoToolbarController] Error finding toolbar button '{buttonText}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Clicks a button element using FlaUI's InvokePattern.
        /// </summary>
        /// <param name="button">The button element to click</param>
        /// <returns>True if successful, false if button is null or click failed</returns>
        public bool ClickButton(AutomationElement? button)
        {
            if (button == null)
            {
                Console.WriteLine("[ChronoToolbarController] Cannot click button: button is null");
                return false;
            }

            try
            {
                var buttonName = button.Name ?? "(unnamed)";
                Console.WriteLine($"[ChronoToolbarController] Clicking button: '{buttonName}'");

                var invokePattern = button.Patterns.Invoke.Pattern;
                if (invokePattern == null)
                {
                    Console.WriteLine($"[ChronoToolbarController] Failed to get InvokePattern for button '{buttonName}'");
                    return false;
                }

                invokePattern.Invoke();
                Console.WriteLine($"[ChronoToolbarController] Successfully clicked button: '{buttonName}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoToolbarController] Error clicking button: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generic method to click any toolbar button by its display text.
        /// </summary>
        /// <param name="buttonText">The Korean button text to search for (e.g., "시작", "중지")</param>
        /// <returns>True if the button was found and clicked successfully, false otherwise</returns>
        public bool ClickToolbarButton(string buttonText)
        {
            // Try MainWindow first
            var window = FindMainWindow();

            // Fallback to SetupWindow (for initial setup screen)
            if (window == null)
            {
                window = FindSetupWindow();
                if (window != null)
                {
                    Console.WriteLine($"[ChronoToolbarController] MainWindow not found, using SetupWindow");
                }
            }

            if (window == null)
            {
                Console.WriteLine($"[ChronoToolbarController] Cannot click '{buttonText}' button: No window found");
                return false;
            }

            var button = FindToolbarButton(window, buttonText);
            if (button == null)
            {
                Console.WriteLine($"[ChronoToolbarController] Button '{buttonText}' not found");
                return false;
            }

            return ClickButton(button);
        }

        /// <summary>
        /// Finds the ChronoView SetupWindow.
        /// </summary>
        public Window? FindSetupWindow()
        {
            return _windowFinder.FindSetupWindow();
        }

        /// <summary>
        /// Clicks the Start button in the ChronoView MainWindow toolbar.
        /// Tries multiple button text variations: "모니터링 프로그램 시작" (SetupWindow), "시작" (MainWindow).
        /// For SetupWindow, also tries finding by button size (Height=55, largest width).
        /// </summary>
        /// <returns>True if the Start button was found and clicked successfully, false otherwise</returns>
        public bool ClickStartButton()
        {
            // Try SetupWindow button text first
            if (ClickToolbarButton("모니터링 프로그램 시작"))
            {
                return true;
            }
            // Fallback to MainWindow button text
            if (ClickToolbarButton("시작"))
            {
                return true;
            }
            // Last resort: Find by button size (SetupWindow's start button is Height=55, Width=643)
            return ClickStartButtonBySize();
        }

        /// <summary>
        /// Finds and clicks the Start button by its size characteristics.
        /// SetupWindow's "모니터링 프로그램 시작" button has Height=55 and is the widest button.
        /// </summary>
        private bool ClickStartButtonBySize()
        {
            using var automation = new UIA3Automation();
            var window = FindSetupWindow() ?? FindMainWindow();
            if (window == null)
            {
                Console.WriteLine($"[ChronoToolbarController] Cannot find button by size: No window found");
                return false;
            }

            var cf = automation.ConditionFactory;
            var buttons = window.FindAllChildren(cf.ByControlType(ControlType.Button));

            Console.WriteLine($"[ChronoToolbarController] Searching for Start button by size...");

            // Find the largest button with height around 55
            AutomationElement? largestButton = null;
            double maxWidth = 0;

            foreach (var button in buttons)
            {
                var bounds = button.BoundingRectangle;
                // Look for button with height ~55 (allowing some tolerance)
                if (bounds.Height >= 50 && bounds.Height <= 60)
                {
                    Console.WriteLine($"[ChronoToolbarController] Found button with Height={bounds.Height}, Width={bounds.Width}");
                    if (bounds.Width > maxWidth)
                    {
                        maxWidth = bounds.Width;
                        largestButton = button;
                    }
                }
            }

            if (largestButton != null)
            {
                Console.WriteLine($"[ChronoToolbarController] Clicking Start button by size (Width={maxWidth})");
                return ClickButton(largestButton);
            }

            Console.WriteLine($"[ChronoToolbarController] No Start button found by size");
            return false;
        }

        /// <summary>
        /// Clicks the Stop button in the ChronoView MainWindow toolbar.
        /// </summary>
        /// <returns>True if the Stop button was found and clicked successfully, false otherwise</returns>
        public bool ClickStopButton()
        {
            return ClickToolbarButton("중지");
        }

        /// <summary>
        /// Clicks the Settings (Setup) button in the ChronoView MainWindow toolbar.
        /// </summary>
        /// <returns>True if the Settings button was found and clicked successfully, false otherwise</returns>
        public bool ClickSettingsButton()
        {
            return ClickToolbarButton("설정");
        }

        /// <summary>
        /// Clicks the Refresh button in the ChronoView MainWindow toolbar.
        /// </summary>
        /// <returns>True if the Refresh button was found and clicked successfully, false otherwise</returns>
        public bool ClickRefreshButton()
        {
            return ClickToolbarButton("새로고침");
        }

        /// <summary>
        /// Clicks the Move button in the ChronoView MainWindow toolbar.
        /// </summary>
        /// <returns>True if the Move button was found and clicked successfully, false otherwise</returns>
        public bool ClickMoveButton()
        {
            return ClickToolbarButton("이동");
        }

        /// <summary>
        /// Clicks the Delete button in the ChronoView MainWindow toolbar.
        /// </summary>
        /// <returns>True if the Delete button was found and clicked successfully, false otherwise</returns>
        public bool ClickDeleteButton()
        {
            return ClickToolbarButton("삭제");
        }

        /// <summary>
        /// Checks if a button with the given text is currently enabled.
        /// </summary>
        /// <param name="buttonText">The button text to search for</param>
        /// <returns>True if the button is enabled, false if disabled or not found</returns>
        public bool IsButtonEnabled(string buttonText)
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine($"[ChronoToolbarController] Cannot check button state: MainWindow not found");
                return false;
            }

            var button = FindToolbarButton(mainWindow, buttonText);
            if (button == null)
            {
                Console.WriteLine($"[ChronoToolbarController] Cannot check button state: Button '{buttonText}' not found");
                return false;
            }

            if (button.Properties.IsEnabled.IsSupported)
            {
                var isEnabled = button.Properties.IsEnabled.ValueOrDefault;
                Console.WriteLine($"[ChronoToolbarController] Button '{buttonText}' IsEnabled={isEnabled}");
                return isEnabled;
            }

            Console.WriteLine($"[ChronoToolbarController] Button '{buttonText}' IsEnabled property not supported");
            return false;
        }

        /// <summary>
        /// Lists all available toolbar buttons in the MainWindow.
        /// </summary>
        /// <returns>Array of button names (display text)</returns>
        public string[] GetAvailableButtons()
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine($"[ChronoToolbarController] Cannot list buttons: MainWindow not found");
                return Array.Empty<string>();
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var buttonCondition = cf.ByControlType(ControlType.Button);
                var buttons = mainWindow.FindAllChildren(buttonCondition);

                var buttonNames = new List<string>();
                Console.WriteLine($"[ChronoToolbarController] Found {buttons.Length} buttons in MainWindow:");

                foreach (var button in buttons)
                {
                    var name = button.Name ?? "(unnamed)";
                    buttonNames.Add(name);
                    Console.WriteLine($"  - '{name}' (AutomationId: '{button.AutomationId ?? "(null)"}')");
                }

                return buttonNames.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoToolbarController] Error listing buttons: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Waits for a button with the given text to become enabled.
        /// </summary>
        /// <param name="buttonText">The button text to search for</param>
        /// <param name="timeoutMs">Maximum time to wait in milliseconds (default: 5000)</param>
        /// <returns>True if the button became enabled, false if timeout</returns>
        public bool WaitForButtonEnabled(string buttonText, int timeoutMs = 5000)
        {
            var startTime = Stopwatch.StartNew();
            Console.WriteLine($"[ChronoToolbarController] Waiting for button '{buttonText}' to be enabled (timeout: {timeoutMs}ms)");

            try
            {
                while (startTime.ElapsedMilliseconds < timeoutMs)
                {
                    if (IsButtonEnabled(buttonText))
                    {
                        Console.WriteLine($"[ChronoToolbarController] Button '{buttonText}' became enabled after {startTime.ElapsedMilliseconds}ms");
                        return true;
                    }
                    Thread.Sleep(DefaultPollIntervalMs);
                }

                Console.WriteLine($"[ChronoToolbarController] Timeout waiting for button '{buttonText}' to be enabled");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoToolbarController] Error waiting for button enabled: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Waits for a button with the given text to become disabled.
        /// </summary>
        /// <param name="buttonText">The button text to search for</param>
        /// <param name="timeoutMs">Maximum time to wait in milliseconds (default: 5000)</param>
        /// <returns>True if the button became disabled, false if timeout</returns>
        public bool WaitForButtonDisabled(string buttonText, int timeoutMs = 5000)
        {
            var startTime = Stopwatch.StartNew();
            Console.WriteLine($"[ChronoToolbarController] Waiting for button '{buttonText}' to be disabled (timeout: {timeoutMs}ms)");

            try
            {
                while (startTime.ElapsedMilliseconds < timeoutMs)
                {
                    var mainWindow = FindMainWindow();
                    if (mainWindow == null)
                    {
                        Console.WriteLine($"[ChronoToolbarController] Cannot check button state: MainWindow not found");
                        return false;
                    }

                    var button = FindToolbarButton(mainWindow, buttonText);
                    if (button == null)
                    {
                        Console.WriteLine($"[ChronoToolbarController] Cannot check button state: Button '{buttonText}' not found");
                        return false;
                    }

                    if (button.Properties.IsEnabled.IsSupported)
                    {
                        var isEnabled = button.Properties.IsEnabled.ValueOrDefault;
                        if (!isEnabled)
                        {
                            Console.WriteLine($"[ChronoToolbarController] Button '{buttonText}' became disabled after {startTime.ElapsedMilliseconds}ms");
                            return true;
                        }
                    }

                    Thread.Sleep(DefaultPollIntervalMs);
                }

                Console.WriteLine($"[ChronoToolbarController] Timeout waiting for button '{buttonText}' to be disabled");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoToolbarController] Error waiting for button disabled: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clicks a button and then waits for a specified duration for UI state to settle.
        /// </summary>
        /// <param name="buttonText">The button text to search for</param>
        /// <param name="waitMs">Time to wait after clicking in milliseconds (default: 500)</param>
        /// <returns>True if the button was clicked successfully, false otherwise</returns>
        public bool ClickButtonAndWait(string buttonText, int waitMs = 500)
        {
            var result = ClickToolbarButton(buttonText);
            if (result)
            {
                Console.WriteLine($"[ChronoToolbarController] Waiting {waitMs}ms for UI to settle");
                Thread.Sleep(waitMs);
            }
            return result;
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
