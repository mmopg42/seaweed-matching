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
                var buttonCondition = cf.ByControlType(ControlType.Button);
                var buttons = mainWindow.FindAllChildren(buttonCondition);

                Console.WriteLine($"[ChronoToolbarController] Searching for button containing '{buttonText}' among {buttons.Length} buttons");

                foreach (var button in buttons)
                {
                    if (!string.IsNullOrEmpty(button.Name) &&
                        button.Name.IndexOf(buttonText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"[ChronoToolbarController] Found button: '{button.Name}' (AutomationId: '{button.AutomationId ?? "(null)"}')");
                        return button;
                    }
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
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine($"[ChronoToolbarController] Cannot click '{buttonText}' button: MainWindow not found");
                return false;
            }

            var button = FindToolbarButton(mainWindow, buttonText);
            if (button == null)
            {
                Console.WriteLine($"[ChronoToolbarController] Button '{buttonText}' not found");
                return false;
            }

            return ClickButton(button);
        }

        /// <summary>
        /// Clicks the Start button in the ChronoView MainWindow toolbar.
        /// </summary>
        /// <returns>True if the Start button was found and clicked successfully, false otherwise</returns>
        public bool ClickStartButton()
        {
            return ClickToolbarButton("시작");
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
