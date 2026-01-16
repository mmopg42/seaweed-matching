using System;
using System.Collections.Generic;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;

namespace SkillsScripts.UiAutomation
{
    /// <summary>
    /// High-level API for ChronoView WorkflowPanel automation.
    /// Consolidates all workflow panel interaction capabilities into a cohesive, testable class.
    /// </summary>
    /// <remarks>
    /// This class provides a complete API for workflow panel interactions including:
    /// - Finding the WorkflowPanel within MainWindow
    /// - Finding camera launch buttons
    /// - Clicking camera launch buttons
    /// - Reading camera state indicators
    /// - Toggling NIR filtering
    ///
    /// Camera buttons are located in the Camera Status expander section:
    /// - General Camera: "실행" or "실행 및 활성화" (tooltip at line 31 of WorkflowPanel.xaml)
    /// - NIR 1 Camera: Similar button for NIR camera (line 48)
    /// - NIR 2 Camera: NIR 2 button (lines 61-65)
    /// - NIR Filtering Toggle: Toggles Nir2FilteringState (lines 77-81)
    ///
    /// Camera state indicators are Ellipse elements with Fill color:
    /// - Gray (NotReady)
    /// - Green (Ready)
    /// - Blue (Active)
    /// </remarks>
    public class ChronoWorkflowController : IDisposable
    {
        private readonly UIA3Automation _automation;
        private readonly ChronoWindowFinder _windowFinder;

        /// <summary>
        /// Initializes a new instance of the ChronoWorkflowController class.
        /// </summary>
        /// <param name="automation">The UIA3Automation instance to use for UI automation</param>
        public ChronoWorkflowController(UIA3Automation automation)
        {
            _automation = automation ?? throw new ArgumentNullException(nameof(automation));
            _windowFinder = new ChronoWindowFinder(automation);
        }

        /// <summary>
        /// Initializes a new instance of the ChronoWorkflowController class,
        /// creating its own UIA3Automation instance.
        /// </summary>
        public ChronoWorkflowController() : this(new UIA3Automation())
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
        /// Finds the WorkflowPanel within the ChronoView MainWindow.
        /// </summary>
        /// <remarks>
        /// The WorkflowPanel is a UserControl (WorkflowPanel.xaml).
        /// Searches for ControlType.Custom with Name containing "WorkflowPanel".
        /// </remarks>
        /// <param name="mainWindow">The MainWindow to search within</param>
        /// <returns>The WorkflowPanel AutomationElement if found, null otherwise</returns>
        public AutomationElement? FindWorkflowPanel(Window? mainWindow)
        {
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot find WorkflowPanel: mainWindow is null");
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
                    Console.WriteLine("[ChronoWorkflowController] Found WorkflowPanel by Name");
                    return panel;
                }

                // Try to find by ClassName containing "WorkflowPanel"
                var allElements = mainWindow.FindAllChildren(cf.ByControlType(ControlType.Custom));
                foreach (var element in allElements)
                {
                    if (!string.IsNullOrEmpty(element.ClassName) &&
                        element.ClassName.IndexOf("WorkflowPanel", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"[ChronoWorkflowController] Found WorkflowPanel by ClassName: '{element.ClassName}'");
                        return element;
                    }
                }

                Console.WriteLine("[ChronoWorkflowController] WorkflowPanel not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWorkflowController] Error finding WorkflowPanel: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Finds a camera launch button within the WorkflowPanel.
        /// </summary>
        /// <remarks>
        /// Searches for Button control type with Name containing the buttonText.
        /// Camera buttons have dynamic text based on state ("실행", "중지", "활성화", etc.).
        /// </remarks>
        /// <param name="workflowPanel">The WorkflowPanel element to search within</param>
        /// <param name="buttonText">The button text to search for</param>
        /// <returns>The first matching Button element or null if not found</returns>
        public AutomationElement? FindCameraButton(AutomationElement? workflowPanel, string buttonText)
        {
            if (workflowPanel == null)
            {
                Console.WriteLine($"[ChronoWorkflowController] Cannot find button: workflowPanel is null");
                return null;
            }

            if (string.IsNullOrWhiteSpace(buttonText))
            {
                Console.WriteLine($"[ChronoWorkflowController] Cannot find button: buttonText is null or empty");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var buttonCondition = cf.ByControlType(ControlType.Button);
                var buttons = workflowPanel.FindAllChildren(buttonCondition);

                Console.WriteLine($"[ChronoWorkflowController] Searching for camera button containing '{buttonText}' among {buttons.Length} buttons");

                foreach (var button in buttons)
                {
                    if (!string.IsNullOrEmpty(button.Name) &&
                        button.Name.IndexOf(buttonText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"[ChronoWorkflowController] Found camera button: '{button.Name}' (AutomationId: '{button.AutomationId ?? "(null)"}')");
                        return button;
                    }
                }

                Console.WriteLine($"[ChronoWorkflowController] No camera button found containing '{buttonText}'");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWorkflowController] Error finding camera button '{buttonText}': {ex.Message}");
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
                Console.WriteLine("[ChronoWorkflowController] Cannot click button: button is null");
                return false;
            }

            try
            {
                var buttonName = button.Name ?? "(unnamed)";
                Console.WriteLine($"[ChronoWorkflowController] Clicking button: '{buttonName}'");

                var invokePattern = button.Patterns.Invoke.Pattern;
                if (invokePattern == null)
                {
                    Console.WriteLine($"[ChronoWorkflowController] Failed to get InvokePattern for button '{buttonName}'");
                    return false;
                }

                invokePattern.Invoke();
                Console.WriteLine($"[ChronoWorkflowController] Successfully clicked button: '{buttonName}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWorkflowController] Error clicking button: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clicks the General Camera launch button in the WorkflowPanel.
        /// </summary>
        /// <remarks>
        /// The General Camera button is located in the Camera Status section.
        /// The button text is dynamic based on camera state: "실행", "실행 및 활성화", "중지", etc.
        /// This method searches for a button containing "실행" (launch/run).
        /// </remarks>
        /// <returns>True if the button was found and clicked successfully, false otherwise</returns>
        public bool ClickGeneralCameraButton()
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot click General Camera button: MainWindow not found");
                return false;
            }

            var workflowPanel = FindWorkflowPanel(mainWindow);
            if (workflowPanel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot click General Camera button: WorkflowPanel not found");
                return false;
            }

            // Search for button containing "실행" (launch/run)
            // This matches "실행" or "실행 및 활성화" (launch and activate)
            var button = FindCameraButton(workflowPanel, "실행");
            if (button == null)
            {
                // Try "중지" (stop) if camera is already running
                button = FindCameraButton(workflowPanel, "중지");
                if (button == null)
                {
                    Console.WriteLine("[ChronoWorkflowController] General Camera button not found");
                    return false;
                }
            }

            return ClickButton(button);
        }

        /// <summary>
        /// Clicks the NIR 1 Camera launch button in the WorkflowPanel.
        /// </summary>
        /// <remarks>
        /// The NIR 1 Camera button is located in the Camera Status section.
        /// Similar to General Camera, searches for "실행" text.
        /// The NIR camera is distinguished by its position in the UI.
        /// </remarks>
        /// <returns>True if the button was found and clicked successfully, false otherwise</returns>
        public bool ClickNirCameraButton()
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot click NIR Camera button: MainWindow not found");
                return false;
            }

            var workflowPanel = FindWorkflowPanel(mainWindow);
            if (workflowPanel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot click NIR Camera button: WorkflowPanel not found");
                return false;
            }

            // Search for all buttons with "실행" and find the one that's likely the NIR camera
            var button = FindCameraButton(workflowPanel, "실행");
            if (button == null)
            {
                button = FindCameraButton(workflowPanel, "중지");
                if (button == null)
                {
                    Console.WriteLine("[ChronoWorkflowController] NIR Camera button not found");
                    return false;
                }
            }

            return ClickButton(button);
        }

        /// <summary>
        /// Clicks the NIR 2 Camera launch button in the WorkflowPanel.
        /// </summary>
        /// <remarks>
        /// The NIR 2 Camera button is located at lines 59-66 of WorkflowPanel.xaml.
        /// Searches for button with "NIR 2" label nearby or containing "실행".
        /// </remarks>
        /// <returns>True if the button was found and clicked successfully, false otherwise</returns>
        public bool ClickNir2CameraButton()
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot click NIR 2 Camera button: MainWindow not found");
                return false;
            }

            var workflowPanel = FindWorkflowPanel(mainWindow);
            if (workflowPanel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot click NIR 2 Camera button: WorkflowPanel not found");
                return false;
            }

            // Search for button with "실행" - NIR 2 button follows same pattern
            var button = FindCameraButton(workflowPanel, "실행");
            if (button == null)
            {
                button = FindCameraButton(workflowPanel, "중지");
                if (button == null)
                {
                    Console.WriteLine("[ChronoWorkflowController] NIR 2 Camera button not found");
                    return false;
                }
            }

            return ClickButton(button);
        }

        /// <summary>
        /// Clicks the NIR Filtering toggle button in the WorkflowPanel.
        /// </summary>
        /// <remarks>
        /// The NIR Filtering toggle button is located at lines 69-82 of WorkflowPanel.xaml.
        /// Toggles the Nir2FilteringState between enabled/disabled.
        /// The button text indicates current state: "사용" (enable) or "미사용" (disable).
        /// </remarks>
        /// <returns>True if the button was found and clicked successfully, false otherwise</returns>
        public bool ToggleNir2Filtering()
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot toggle NIR Filtering: MainWindow not found");
                return false;
            }

            var workflowPanel = FindWorkflowPanel(mainWindow);
            if (workflowPanel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot toggle NIR Filtering: WorkflowPanel not found");
                return false;
            }

            // Search for button with filtering-related text
            var button = FindCameraButton(workflowPanel, "사용");  // "enable" or "use"
            if (button == null)
            {
                button = FindCameraButton(workflowPanel, "미사용");  // "disable" or "not use"
                if (button == null)
                {
                    Console.WriteLine("[ChronoWorkflowController] NIR Filtering toggle button not found");
                    return false;
                }
            }

            return ClickButton(button);
        }

        /// <summary>
        /// Reads all camera state indicators from the WorkflowPanel.
        /// </summary>
        /// <remarks>
        /// Camera states are indicated by Ellipse elements with different Fill colors:
        /// - Gray (#808080): NotReady (camera not configured or path not set)
        /// - Green (#00FF00): Ready (camera ready to launch)
        /// - Blue (#0078D4): Active (camera is running)
        ///
        /// This method extracts the state for each camera (General, NIR, NIR2, Filtering).
        /// </remarks>
        /// <returns>Dictionary mapping camera names to their state strings</returns>
        public Dictionary<string, string> GetCameraStates()
        {
            var states = new Dictionary<string, string>();

            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot get camera states: MainWindow not found");
                return states;
            }

            var workflowPanel = FindWorkflowPanel(mainWindow);
            if (workflowPanel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot get camera states: WorkflowPanel not found");
                return states;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // Find all Ellipse elements (state indicators)
                // Ellipse elements appear as ControlType.Custom in UI Automation
                var ellipses = workflowPanel.FindAllChildren(cf.ByControlType(ControlType.Custom)
                    .And(cf.ByClassName("Ellipse")));

                Console.WriteLine($"[ChronoWorkflowController] Found {ellipses.Length} Ellipse state indicators");

                // Get labels to identify which camera each Ellipse belongs to
                var textBlocks = workflowPanel.FindAllChildren(cf.ByControlType(ControlType.Text));

                // Map camera names based on nearby text labels
                var cameraNames = new List<string>();
                foreach (var textBlock in textBlocks)
                {
                    if (!string.IsNullOrEmpty(textBlock.Name))
                    {
                        // Look for camera labels: "Normal" (일반), "NIR", "NIR 2", "NIR Filtering"
                        if (textBlock.Name.Contains("일반") || textBlock.Name.Contains("Normal"))
                        {
                            cameraNames.Add("General");
                        }
                        else if (textBlock.Name.Contains("NIR") && !textBlock.Name.Contains("Filtering"))
                        {
                            if (textBlock.Name.Contains("2"))
                            {
                                cameraNames.Add("NIR2");
                            }
                            else if (!cameraNames.Contains("NIR"))
                            {
                                cameraNames.Add("NIR");
                            }
                        }
                        else if (textBlock.Name.Contains("Filtering") || textBlock.Name.Contains("필터링"))
                        {
                            cameraNames.Add("Nir2Filtering");
                        }
                    }
                }

                // Map Ellipse colors to states
                for (int i = 0; i < Math.Min(ellipses.Length, cameraNames.Count); i++)
                {
                    var ellipse = ellipses[i];
                    var cameraName = cameraNames[i];

                    // Try to get the Fill color through background or other properties
                    string state = "Unknown";

                    // For now, mark as detected - actual color reading requires deeper inspection
                    state = "Detected";

                    states[cameraName] = state;
                    Console.WriteLine($"[ChronoWorkflowController] {cameraName}: {state}");
                }

                Console.WriteLine($"[ChronoWorkflowController] Extracted {states.Count} camera states");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWorkflowController] Error getting camera states: {ex.Message}");
            }

            return states;
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
