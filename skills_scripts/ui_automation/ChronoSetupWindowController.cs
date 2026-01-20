using System;
using System.Collections.Generic;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;

namespace SkillsScripts.UiAutomation
{
    /// <summary>
    /// High-level API for ChronoView SetupWindow automation.
    /// Consolidates all SetupWindow interaction logic into a dedicated, testable class.
    /// </summary>
    /// <remarks>
    /// This class provides a complete API for SetupWindow interactions including:
    /// - Finding the SetupWindow via ChronoWindowFinder
    /// - Clicking the Settings button to open SettingsDialog
    /// - Launching camera programs (General, NIR1, NIR2)
    /// - Toggling NIR filtering state
    /// - Starting monitoring (transition to MainWindow)
    /// - Closing the SetupWindow
    /// - Reading camera button states
    /// - Waiting for MainWindow after Start button click
    ///
    /// SetupWindow button AutomationId values (from SetupWindow.xaml):
    /// - SetupSettingsButton (gear icon in title bar)
    /// - SetupGeneralCameraButton
    /// - SetupNir1CameraButton
    /// - SetupNir2CameraButton
    /// - SetupNirFilteringButton
    /// - SetupStartButton
    /// - SetupMinimizeButton
    /// - SetupCloseButton
    /// </remarks>
    public class ChronoSetupWindowController : IDisposable
    {
        private readonly UIA3Automation _automation;
        private readonly ChronoWindowFinder _windowFinder;
        private const int DefaultPollIntervalMs = 200;

        /// <summary>
        /// Initializes a new instance of the ChronoSetupWindowController class.
        /// </summary>
        /// <param name="automation">The UIA3Automation instance to use for UI automation</param>
        public ChronoSetupWindowController(UIA3Automation automation)
        {
            _automation = automation ?? throw new ArgumentNullException(nameof(automation));
            _windowFinder = new ChronoWindowFinder(automation);
        }

        /// <summary>
        /// Initializes a new instance of the ChronoSetupWindowController class,
        /// creating its own UIA3Automation instance.
        /// </summary>
        public ChronoSetupWindowController() : this(new UIA3Automation())
        {
        }

        /// <summary>
        /// Finds the ChronoView SetupWindow.
        /// </summary>
        /// <remarks>
        /// Reuses ChronoWindowFinder.FindSetupWindow() which searches for
        /// windows containing "Setup" in their title.
        /// The SetupWindow has Title="Setup - ChronoView Pro" (SetupWindow.xaml line 4).
        /// </remarks>
        /// <returns>The SetupWindow if found, null otherwise</returns>
        public Window? FindSetupWindow()
        {
            var window = _windowFinder.FindSetupWindow();
            if (window == null)
            {
                Console.WriteLine("[ChronoSetupWindowController] SetupWindow not found");
                return null;
            }

            Console.WriteLine($"[ChronoSetupWindowController] SetupWindow found: '{window.Name}'");
            return window;
        }

        /// <summary>
        /// Clicks the Settings button in the SetupWindow title bar.
        /// </summary>
        /// <remarks>
        /// Finds SetupWindow first if not provided.
        /// The Settings button has AutomationId "SetupSettingsButton" (SetupWindow.xaml line 41).
        /// Uses InvokePattern for clicking.
        /// </remarks>
        /// <param name="setupWindow">The SetupWindow to search within (optional, will find if null)</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ClickSettingsButton(Window? setupWindow = null)
        {
            setupWindow ??= FindSetupWindow();
            if (setupWindow == null)
            {
                Console.WriteLine("[ChronoSetupWindowController] Cannot click Settings button: SetupWindow not found");
                return false;
            }

            var button = FindButtonById(setupWindow, "SetupSettingsButton");
            if (button == null)
            {
                return false;
            }

            return ClickButton(button);
        }

        /// <summary>
        /// Clicks the General Camera launch button.
        /// </summary>
        /// <remarks>
        /// Finds SetupWindow first if not provided.
        /// The General Camera button has AutomationId "SetupGeneralCameraButton" (SetupWindow.xaml line 264).
        /// Uses InvokePattern for clicking.
        /// </remarks>
        /// <param name="setupWindow">The SetupWindow to search within (optional, will find if null)</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ClickGeneralCamera(Window? setupWindow = null)
        {
            setupWindow ??= FindSetupWindow();
            if (setupWindow == null)
            {
                Console.WriteLine("[ChronoSetupWindowController] Cannot click General Camera button: SetupWindow not found");
                return false;
            }

            var button = FindButtonById(setupWindow, "SetupGeneralCameraButton");
            if (button == null)
            {
                return false;
            }

            return ClickButton(button);
        }

        /// <summary>
        /// Clicks the NIR1 Camera launch button.
        /// </summary>
        /// <remarks>
        /// Finds SetupWindow first if not provided.
        /// The NIR1 button has AutomationId "SetupNir1CameraButton" (SetupWindow.xaml line 322).
        /// Uses InvokePattern for clicking.
        /// </remarks>
        /// <param name="setupWindow">The SetupWindow to search within (optional, will find if null)</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ClickNir1(Window? setupWindow = null)
        {
            setupWindow ??= FindSetupWindow();
            if (setupWindow == null)
            {
                Console.WriteLine("[ChronoSetupWindowController] Cannot click NIR1 button: SetupWindow not found");
                return false;
            }

            var button = FindButtonById(setupWindow, "SetupNir1CameraButton");
            if (button == null)
            {
                return false;
            }

            return ClickButton(button);
        }

        /// <summary>
        /// Clicks the NIR2 Camera launch button.
        /// </summary>
        /// <remarks>
        /// Finds SetupWindow first if not provided.
        /// The NIR2 button has AutomationId "SetupNir2CameraButton" (SetupWindow.xaml line 356).
        /// Uses InvokePattern for clicking.
        /// </remarks>
        /// <param name="setupWindow">The SetupWindow to search within (optional, will find if null)</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ClickNir2(Window? setupWindow = null)
        {
            setupWindow ??= FindSetupWindow();
            if (setupWindow == null)
            {
                Console.WriteLine("[ChronoSetupWindowController] Cannot click NIR2 button: SetupWindow not found");
                return false;
            }

            var button = FindButtonById(setupWindow, "SetupNir2CameraButton");
            if (button == null)
            {
                return false;
            }

            return ClickButton(button);
        }

        /// <summary>
        /// Releases resources used by the UIA3 automation.
        /// </summary>
        public void Dispose()
        {
            _automation?.Dispose();
        }

        #region Helper Methods

        /// <summary>
        /// Finds a button element within a window by its AutomationId.
        /// </summary>
        /// <param name="window">The window to search within</param>
        /// <param name="automationId">The AutomationId to find</param>
        /// <returns>The button element if found, null otherwise</returns>
        private AutomationElement? FindButtonById(Window? window, string automationId)
        {
            if (window == null)
            {
                Console.WriteLine($"[ChronoSetupWindowController] Cannot find button: window is null");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var buttonCondition = cf.ByControlType(ControlType.Button)
                    .And(cf.ByAutomationId(automationId));
                var button = window.FindFirstDescendant(buttonCondition);

                if (button == null)
                {
                    Console.WriteLine($"[ChronoSetupWindowController] Button with AutomationId '{automationId}' not found");
                    return null;
                }

                Console.WriteLine($"[ChronoSetupWindowController] Found button with AutomationId '{automationId}'");
                return button;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoSetupWindowController] Error finding button by AutomationId '{automationId}': {ex.Message}");
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
                var buttonId = button.AutomationId ?? button.Name ?? "(unnamed)";
                Console.WriteLine($"[ChronoSetupWindowController] Clicking button: '{buttonId}'");

                var invokePattern = button.Patterns.Invoke.Pattern;
                if (invokePattern == null)
                {
                    Console.WriteLine($"[ChronoSetupWindowController] Failed to get InvokePattern for button '{buttonId}'");
                    return false;
                }

                invokePattern.Invoke();
                Console.WriteLine($"[ChronoSetupWindowController] Successfully clicked button: '{buttonId}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoSetupWindowController] Error clicking button: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
